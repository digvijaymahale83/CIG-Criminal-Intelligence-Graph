using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EntityResolutionService : IEntityResolutionService
{
    private readonly IAppDbContext _dbContext;
    private readonly INeo4jService _neo4jService;
    private readonly IAuditService _auditService;
    private readonly ILogger<EntityResolutionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EntityResolutionService(
        IAppDbContext dbContext,
        INeo4jService neo4jService,
        IAuditService auditService,
        ILogger<EntityResolutionService> logger)
    {
        _dbContext = dbContext;
        _neo4jService = neo4jService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<BatchResolutionResultDto> RunBatchResolutionAsync(
        string? caseId,
        List<string>? entityTypes,
        double minimumScore,
        string actorId,
        string actorName,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new BatchResolutionResultDto();

        // 1. Fetch verified canonical entities
        var query = _dbContext.Entities
            .AsNoTracking()
            .Include(e => e.Case)
            .Where(e => e.VerificationStatus != "REJECTED" && !string.IsNullOrEmpty(e.CaseId));

        if (entityTypes != null && entityTypes.Count > 0)
        {
            var upperTypes = entityTypes.Select(t => t.ToUpperInvariant()).ToList();
            query = query.Where(e => upperTypes.Contains(e.Type.ToUpper()));
        }

        var allEntities = await query.ToListAsync(cancellationToken);
        if (allEntities.Count == 0)
        {
            sw.Stop();
            result.Duration = sw.Elapsed;
            return result;
        }

        // Fetch supporting evidence citations for entities
        var evidenceCitations = await _dbContext.ExtractedEntities
            .AsNoTracking()
            .Include(ee => ee.Evidence)
            .Where(ee => ee.PromotedEntityId != null && ee.ReviewStatus == "APPROVED")
            .ToListAsync(cancellationToken);

        var citationsByEntityId = evidenceCitations
            .GroupBy(ee => ee.PromotedEntityId!)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ee => new SupportingEvidenceCitationDto
                {
                    EvidenceId = ee.EvidenceId,
                    CaseId = ee.CaseId ?? string.Empty,
                    FileName = ee.Evidence?.FileName ?? "Evidence",
                    EvidenceType = ee.Evidence?.MimeType ?? "Document",
                    SourceLocation = ee.SourceLocation,
                    ExtractedQuote = ee.RawValue
                }).ToList()
            );

        // Fetch connected relationships for context similarity
        var relationships = await _dbContext.Relationships
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var relationsByEntity = relationships
            .GroupBy(r => r.SourceEntityId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.TargetEntityId).ToHashSet());

        // Split source candidates and target candidates based on caseId filter
        List<EntityItem> sourceList;
        List<EntityItem> targetList;

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            sourceList = allEntities.Where(e => e.CaseId == caseId || e.Case?.CaseNumber == caseId).ToList();
            targetList = allEntities.Where(e => e.CaseId != caseId && e.Case?.CaseNumber != caseId).ToList();
        }
        else
        {
            sourceList = allEntities;
            targetList = allEntities;
        }

        int comparedCount = 0;
        int matchesFound = 0;
        int highConfidenceCount = 0;
        int createdCount = 0;

        // Group target entities by Type for blocking (never compare incompatible types)
        var targetsByType = targetList.GroupBy(e => e.Type.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        var candidatesToSave = new List<EntityMatchCandidate>();

        // Query existing candidates to ensure idempotency
        var existingCandidates = await _dbContext.EntityMatchCandidates
            .Select(c => new { c.SourceEntityId, c.TargetEntityId, c.Id })
            .ToListAsync(cancellationToken);

        var existingPairs = new HashSet<string>();
        foreach (var ec in existingCandidates)
        {
            existingPairs.Add($"{ec.SourceEntityId}:{ec.TargetEntityId}");
            existingPairs.Add($"{ec.TargetEntityId}:{ec.SourceEntityId}");
        }

        for (int i = 0; i < sourceList.Count; i++)
        {
            var source = sourceList[i];
            string srcType = source.Type.ToUpperInvariant();

            if (!targetsByType.TryGetValue(srcType, out var potentialTargets))
                continue;

            foreach (var target in potentialTargets)
            {
                // Never compare entity with itself or within the exact same case
                if (source.Id == target.Id || source.CaseId == target.CaseId)
                    continue;

                // If doing full matrix, avoid duplicate reverse comparisons
                if (string.IsNullOrWhiteSpace(caseId) && string.CompareOrdinal(source.Id, target.Id) >= 0)
                    continue;

                comparedCount++;

                // Multi-signal matching evaluation
                var (score, method, factors) = EvaluateMatch(source, target, citationsByEntityId, relationsByEntity);

                if (score >= minimumScore)
                {
                    matchesFound++;
                    if (score >= 0.85) highConfidenceCount++;

                    string pairKey = $"{source.Id}:{target.Id}";
                    if (!existingPairs.Contains(pairKey))
                    {
                        var candidate = new EntityMatchCandidate
                        {
                            Id = $"emc-{Guid.NewGuid().ToString("N")[..8]}",
                            SourceEntityId = source.Id,
                            TargetEntityId = target.Id,
                            SourceCaseId = source.CaseId ?? string.Empty,
                            TargetCaseId = target.CaseId ?? string.Empty,
                            EntityType = source.Type,
                            MatchStatus = "PENDING",
                            MatchScore = Math.Round(score, 2),
                            MatchMethod = method,
                            MatchExplanationJson = JsonSerializer.Serialize(factors),
                            CreatedAtUtc = DateTime.UtcNow
                        };

                        candidatesToSave.Add(candidate);
                        existingPairs.Add(pairKey);
                        createdCount++;
                    }
                }
            }
        }

        if (candidatesToSave.Count > 0)
        {
            _dbContext.EntityMatchCandidates.AddRange(candidatesToSave);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Audit candidate generation
            await _auditService.LogAsync(
                actorId,
                actorName,
                "MATCH_CANDIDATE_CREATED",
                "EntityResolution",
                caseId ?? "ALL_CASES",
                $"Generated {candidatesToSave.Count} potential cross-case entity match candidates.",
                JsonSerializer.Serialize(new { comparedCount, matchesFound, createdCount }),
                null,
                cancellationToken);
        }

        sw.Stop();
        result.EntitiesCompared = comparedCount;
        result.PotentialMatchesFound = matchesFound;
        result.HighConfidenceCandidates = highConfidenceCount;
        result.CandidatesCreated = createdCount;
        result.Duration = sw.Elapsed;

        return result;
    }

    /// <summary>
    /// Evaluates multi-signal matching with explainable weights, factors, and false-positive protection.
    /// </summary>
    private static (double Score, string Method, List<MatchFactorDto> Factors) EvaluateMatch(
        EntityItem source,
        EntityItem target,
        Dictionary<string, List<SupportingEvidenceCitationDto>> citationsByEntityId,
        Dictionary<string, HashSet<string>> relationsByEntity)
    {
        var factors = new List<MatchFactorDto>();
        string entityType = source.Type.ToUpperInvariant();

        // 1. Incompatible Types Guard (e.g. PERSON vs VEHICLE)
        if (!string.Equals(source.Type, target.Type, StringComparison.OrdinalIgnoreCase))
        {
            return (0.0, "TYPE_MISMATCH", factors);
        }

        citationsByEntityId.TryGetValue(source.Id, out var srcCitations);
        citationsByEntityId.TryGetValue(target.Id, out var tgtCitations);
        var combinedCitations = (srcCitations ?? new List<SupportingEvidenceCitationDto>())
            .Concat(tgtCitations ?? new List<SupportingEvidenceCitationDto>())
            .ToList();

        // Signal A: Exact Normalized Identifier (e.g. Phone, Vehicle, Account, Email)
        bool hasExactIdentifier = false;
        if (!string.IsNullOrWhiteSpace(source.PhoneNumber) && !string.IsNullOrWhiteSpace(target.PhoneNumber))
        {
            var p1 = Regex.Replace(source.PhoneNumber, @"[^\d]", "");
            var p2 = Regex.Replace(target.PhoneNumber, @"[^\d]", "");
            if (p1.Length >= 10 && p2.Length >= 10 && (p1 == p2 || p1.EndsWith(p2) || p2.EndsWith(p1)))
            {
                factors.Add(new MatchFactorDto
                {
                    Type = "SHARED_PHONE",
                    Weight = 0.45,
                    Score = 1.0,
                    Description = $"Identical normalized phone number: {source.PhoneNumber.Trim()}",
                    EvidenceCitations = combinedCitations.Where(c => c.EvidenceType.Contains("csv", StringComparison.OrdinalIgnoreCase) || c.FileName.Contains("cdr", StringComparison.OrdinalIgnoreCase)).ToList()
                });
                hasExactIdentifier = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(source.VehicleNumber) && !string.IsNullOrWhiteSpace(target.VehicleNumber))
        {
            var v1 = Regex.Replace(source.VehicleNumber, @"[^a-zA-Z0-9]", "").ToUpperInvariant();
            var v2 = Regex.Replace(target.VehicleNumber, @"[^a-zA-Z0-9]", "").ToUpperInvariant();
            if (!string.IsNullOrEmpty(v1) && v1 == v2)
            {
                factors.Add(new MatchFactorDto
                {
                    Type = "SHARED_VEHICLE",
                    Weight = 0.35,
                    Score = 1.0,
                    Description = $"Identical vehicle registration: {source.VehicleNumber.Trim()}",
                    EvidenceCitations = combinedCitations.Where(c => c.FileName.Contains("surveillance", StringComparison.OrdinalIgnoreCase) || c.FileName.Contains("report", StringComparison.OrdinalIgnoreCase)).ToList()
                });
                hasExactIdentifier = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(source.AccountNumber) &&
            !string.IsNullOrWhiteSpace(target.AccountNumber) &&
            string.Equals(source.AccountNumber.Trim(), target.AccountNumber.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            factors.Add(new MatchFactorDto
            {
                Type = "SHARED_ACCOUNT",
                Weight = 0.35,
                Score = 1.0,
                Description = $"Identical financial account: {source.AccountNumber.Trim()}",
                EvidenceCitations = combinedCitations.Where(c => c.FileName.Contains("bank", StringComparison.OrdinalIgnoreCase) || c.FileName.Contains("account", StringComparison.OrdinalIgnoreCase)).ToList()
            });
            hasExactIdentifier = true;
        }

        // Exact NormalizedValue for PHONE, VEHICLE, ACCOUNT types
        if (entityType is "PHONE" or "VEHICLE" or "ACCOUNT" or "EMAIL")
        {
            if (!string.IsNullOrWhiteSpace(source.NormalizedValue) &&
                string.Equals(source.NormalizedValue.Trim(), target.NormalizedValue.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                factors.Add(new MatchFactorDto
                {
                    Type = $"EXACT_{entityType}",
                    Weight = 0.80,
                    Score = 1.0,
                    Description = $"Exact matching normalized identifier: {source.NormalizedValue.Trim()}",
                    EvidenceCitations = combinedCitations
                });
                hasExactIdentifier = true;
            }
        }

        // Signal B: Name Similarity (Deterministic Jaro-Winkler + Token-Set)
        double nameSim = StringSimilarity.ComparePersonNames(source.CanonicalName, target.CanonicalName);
        if (nameSim >= 0.70)
        {
            factors.Add(new MatchFactorDto
            {
                Type = "NAME_SIMILARITY",
                Weight = 0.30,
                Score = Math.Round(nameSim, 2),
                Description = $"High name similarity ({Math.Round(nameSim * 100)}%): '{source.CanonicalName}' vs '{target.CanonicalName}'",
                EvidenceCitations = combinedCitations.Take(2).ToList()
            });
        }

        // Signal C: Alias Match
        var srcAliases = source.GetAliases();
        var tgtAliases = target.GetAliases();
        bool aliasMatch = false;

        foreach (var sa in srcAliases)
        {
            if (string.Equals(sa, target.CanonicalName, StringComparison.OrdinalIgnoreCase) ||
                tgtAliases.Any(ta => string.Equals(sa, ta, StringComparison.OrdinalIgnoreCase)))
            {
                aliasMatch = true;
                break;
            }
        }
        if (!aliasMatch)
        {
            foreach (var ta in tgtAliases)
            {
                if (string.Equals(ta, source.CanonicalName, StringComparison.OrdinalIgnoreCase))
                {
                    aliasMatch = true;
                    break;
                }
            }
        }

        if (aliasMatch)
        {
            factors.Add(new MatchFactorDto
            {
                Type = "ALIAS_MATCH",
                Weight = 0.25,
                Score = 1.0,
                Description = "Documented alias matches target identity.",
                EvidenceCitations = combinedCitations.Take(2).ToList()
            });
        }

        // Signal D: Context Similarity (shared relationships / neighbors)
        relationsByEntity.TryGetValue(source.Id, out var srcRels);
        relationsByEntity.TryGetValue(target.Id, out var tgtRels);
        if (srcRels != null && tgtRels != null)
        {
            var intersection = new HashSet<string>(srcRels);
            intersection.IntersectWith(tgtRels);
            if (intersection.Count > 0)
            {
                factors.Add(new MatchFactorDto
                {
                    Type = "CONTEXT_SIMILARITY",
                    Weight = 0.20,
                    Score = Math.Min(1.0, intersection.Count * 0.5),
                    Description = $"Entities share {intersection.Count} contextual network connection(s).",
                    EvidenceCitations = combinedCitations.Take(2).ToList()
                });
            }
        }

        // Signal E: Contradictory Attributes (Penalty)
        if (!string.IsNullOrWhiteSpace(source.District) &&
            !string.IsNullOrWhiteSpace(target.District) &&
            !string.Equals(source.District, target.District, StringComparison.OrdinalIgnoreCase) &&
            !hasExactIdentifier && nameSim < 0.90)
        {
            factors.Add(new MatchFactorDto
            {
                Type = "CONTRADICTORY_ATTRIBUTE",
                Weight = -0.20,
                Score = 1.0,
                Description = $"Conflicting geographic jurisdiction: '{source.District}' vs '{target.District}' with no shared unique identifier.",
                EvidenceCitations = new List<SupportingEvidenceCitationDto>()
            });
        }

        // If no positive factors found
        if (factors.Count == 0)
        {
            return (0.0, "NO_MATCH", factors);
        }

        // Calculate explainable multi-signal score
        double normalizedScore;
        if (hasExactIdentifier)
        {
            double score = 0.88;
            int positiveCount = factors.Count(f => f.Weight > 0);
            if (positiveCount > 1)
            {
                score += (positiveCount - 1) * 0.05;
            }
            if (factors.Any(f => f.Type == "CONTRADICTORY_ATTRIBUTE"))
            {
                score -= 0.15;
            }
            normalizedScore = Math.Min(0.99, Math.Max(0.60, score));
        }
        else if (aliasMatch)
        {
            double score = 0.75;
            if (nameSim >= 0.85) score += 0.10;
            if (factors.Any(f => f.Type == "CONTEXT_SIMILARITY")) score += 0.08;
            if (factors.Any(f => f.Type == "CONTRADICTORY_ATTRIBUTE")) score -= 0.15;
            normalizedScore = Math.Min(0.95, Math.Max(0.50, score));
        }
        else
        {
            double score = nameSim;
            if (factors.Any(f => f.Type == "CONTEXT_SIMILARITY"))
            {
                var ctx = factors.First(f => f.Type == "CONTEXT_SIMILARITY");
                score = Math.Min(0.95, score + ctx.Score * 0.15);
            }
            if (factors.Any(f => f.Type == "CONTRADICTORY_ATTRIBUTE"))
            {
                score -= 0.15;
            }

            // FALSE-POSITIVE PROTECTION:
            // For PERSON entities, if the ONLY signal is name similarity (no phone, no vehicle, no account, no alias, no context)
            // do NOT allow high score. Cap strictly at 0.55 so it strictly requires further evidence and cannot be auto-confirmed.
            if (entityType == "PERSON" && !hasExactIdentifier && !aliasMatch && factors.All(f => f.Type == "NAME_SIMILARITY"))
            {
                score = Math.Min(score, 0.55);
                factors.Add(new MatchFactorDto
                {
                    Type = "SAFEGUARD_CAP",
                    Weight = 0.0,
                    Score = 0.0,
                    Description = "Name Similarity (Uncorroborated - Common Name Safeguard Applied)"
                });
            }
            normalizedScore = Math.Clamp(score, 0.0, 1.0);
        }

        string method = hasExactIdentifier && factors.Count > 1 ? "MULTI_SIGNAL" :
                        hasExactIdentifier ? "EXACT_IDENTIFIER" :
                        aliasMatch ? "ALIAS_MATCH" : "FUZZY_NAME";

        return (normalizedScore, method, factors);
    }

    public async Task<List<EntityMatchCandidateDto>> GetCandidatesAsync(
        string? caseId,
        string? status,
        string? entityType,
        double? minScore,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EntityMatchCandidates
            .AsNoTracking()
            .Include(c => c.SourceEntity)
            .Include(c => c.TargetEntity)
            .Include(c => c.SourceCase)
            .Include(c => c.TargetCase)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            query = query.Where(c => c.SourceCaseId == caseId || c.TargetCaseId == caseId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.MatchStatus.ToUpper() == status.ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(c => c.EntityType.ToUpper() == entityType.ToUpper());
        }

        if (minScore.HasValue)
        {
            query = query.Where(c => c.MatchScore >= minScore.Value);
        }

        var candidates = await query
            .OrderByDescending(c => c.MatchScore)
            .ThenByDescending(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return candidates.Select(MapToCandidateDto).ToList();
    }

    public async Task<EntityMatchCandidateDto?> GetCandidateByIdAsync(string candidateId, CancellationToken cancellationToken = default)
    {
        var c = await _dbContext.EntityMatchCandidates
            .AsNoTracking()
            .Include(c => c.SourceEntity)
            .Include(c => c.TargetEntity)
            .Include(c => c.SourceCase)
            .Include(c => c.TargetCase)
            .FirstOrDefaultAsync(item => item.Id == candidateId, cancellationToken);

        return c != null ? MapToCandidateDto(c) : null;
    }

    public async Task<CandidateComparisonDto?> GetCandidateComparisonAsync(string candidateId, CancellationToken cancellationToken = default)
    {
        var candidate = await _dbContext.EntityMatchCandidates
            .AsNoTracking()
            .Include(c => c.SourceEntity)
            .Include(c => c.TargetEntity)
            .Include(c => c.SourceCase)
            .Include(c => c.TargetCase)
            .FirstOrDefaultAsync(item => item.Id == candidateId, cancellationToken);

        if (candidate == null || candidate.SourceEntity == null || candidate.TargetEntity == null)
            return null;

        // Fetch supporting evidence for Side A and Side B
        var sideAEvidence = await GetEntityEvidenceCitationsAsync(candidate.SourceEntityId, candidate.SourceCaseId, cancellationToken);
        var sideBEvidence = await GetEntityEvidenceCitationsAsync(candidate.TargetEntityId, candidate.TargetCaseId, cancellationToken);

        // Fetch relationship summaries
        var sideARels = await GetEntityRelationshipSummariesAsync(candidate.SourceEntityId, cancellationToken);
        var sideBRels = await GetEntityRelationshipSummariesAsync(candidate.TargetEntityId, cancellationToken);

        var factors = ParseFactors(candidate.MatchExplanationJson);

        return new CandidateComparisonDto
        {
            CandidateId = candidate.Id,
            MatchScore = candidate.MatchScore,
            MatchStatus = candidate.MatchStatus,
            EntityType = candidate.EntityType,
            Factors = factors,
            CreatedAtUtc = candidate.CreatedAtUtc,
            ReviewedBy = candidate.ReviewedBy,
            ReviewedAtUtc = candidate.ReviewedAtUtc,
            ReviewNotes = candidate.ReviewNotes,
            SideA = new EntityComparisonSideDto
            {
                EntityId = candidate.SourceEntity.Id,
                CaseId = candidate.SourceCaseId,
                CaseNumber = candidate.SourceCase?.CaseNumber ?? candidate.SourceCaseId,
                CaseTitle = candidate.SourceCase?.Title ?? "Case File",
                CanonicalName = candidate.SourceEntity.CanonicalName,
                NormalizedValue = candidate.SourceEntity.NormalizedValue,
                EntityType = candidate.SourceEntity.Type,
                Aliases = candidate.SourceEntity.GetAliases(),
                PhoneNumber = candidate.SourceEntity.PhoneNumber,
                VehicleNumber = candidate.SourceEntity.VehicleNumber,
                AccountNumber = candidate.SourceEntity.AccountNumber,
                BankName = candidate.SourceEntity.BankName,
                Location = candidate.SourceEntity.Location,
                District = candidate.SourceEntity.District,
                SupportingEvidence = sideAEvidence,
                ConnectedRelationships = sideARels
            },
            SideB = new EntityComparisonSideDto
            {
                EntityId = candidate.TargetEntity.Id,
                CaseId = candidate.TargetCaseId,
                CaseNumber = candidate.TargetCase?.CaseNumber ?? candidate.TargetCaseId,
                CaseTitle = candidate.TargetCase?.Title ?? "Case File",
                CanonicalName = candidate.TargetEntity.CanonicalName,
                NormalizedValue = candidate.TargetEntity.NormalizedValue,
                EntityType = candidate.TargetEntity.Type,
                Aliases = candidate.TargetEntity.GetAliases(),
                PhoneNumber = candidate.TargetEntity.PhoneNumber,
                VehicleNumber = candidate.TargetEntity.VehicleNumber,
                AccountNumber = candidate.TargetEntity.AccountNumber,
                BankName = candidate.TargetEntity.BankName,
                Location = candidate.TargetEntity.Location,
                District = candidate.TargetEntity.District,
                SupportingEvidence = sideBEvidence,
                ConnectedRelationships = sideBRels
            }
        };
    }

    public async Task<bool> ReviewCandidateAsync(
        string candidateId,
        CandidateReviewRequestDto review,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var candidate = await _dbContext.EntityMatchCandidates
            .Include(c => c.SourceEntity)
            .Include(c => c.TargetEntity)
            .FirstOrDefaultAsync(c => c.Id == candidateId, cancellationToken);

        if (candidate == null) return false;

        string normalizedDecision = review.Status.Trim().ToUpperInvariant();
        if (normalizedDecision != "APPROVED" && normalizedDecision != "REJECTED")
        {
            throw new ArgumentException("Review decision must be either APPROVED or REJECTED.");
        }

        candidate.MatchStatus = normalizedDecision;
        candidate.ReviewedBy = actorName;
        candidate.ReviewedAtUtc = DateTime.UtcNow;
        candidate.ReviewNotes = review.ReviewNotes?.Trim();

        if (normalizedDecision == "APPROVED")
        {
            // 1. Create or update CrossCaseConnection
            var connection = await _dbContext.CrossCaseConnections
                .FirstOrDefaultAsync(ccc => ccc.CandidateId == candidate.Id, cancellationToken);

            var sideAEvid = await GetEntityEvidenceCitationsAsync(candidate.SourceEntityId, candidate.SourceCaseId, cancellationToken);
            var sideBEvid = await GetEntityEvidenceCitationsAsync(candidate.TargetEntityId, candidate.TargetCaseId, cancellationToken);
            var allEvid = sideAEvid.Concat(sideBEvid).ToList();
            string evidencePayload = allEvid.Count > 0
                ? JsonSerializer.Serialize(allEvid, JsonOptions)
                : candidate.MatchExplanationJson;

            if (connection == null)
            {
                connection = new CrossCaseConnection
                {
                    Id = $"ccc-{Guid.NewGuid().ToString("N")[..8]}",
                    CandidateId = candidate.Id,
                    SourceCaseId = candidate.SourceCaseId,
                    TargetCaseId = candidate.TargetCaseId,
                    SourceEntityId = candidate.SourceEntityId,
                    TargetEntityId = candidate.TargetEntityId,
                    ConnectionType = candidate.MatchMethod == "EXACT_IDENTIFIER" ? $"SHARED_{candidate.EntityType.ToUpper()}" : "MULTI_SIGNAL_CONNECTION",
                    Confidence = candidate.MatchScore,
                    Status = "APPROVED",
                    Explanation = $"Investigator-approved cross-case match between {candidate.SourceEntity?.CanonicalName ?? "Source"} and {candidate.TargetEntity?.CanonicalName ?? "Target"} ({Math.Round(candidate.MatchScore * 100)}% confidence).",
                    SupportingEvidenceJson = evidencePayload,
                    CreatedAtUtc = DateTime.UtcNow,
                    ReviewedBy = actorName,
                    ReviewedAtUtc = DateTime.UtcNow
                };
                _dbContext.CrossCaseConnections.Add(connection);
            }
            else
            {
                connection.Status = "APPROVED";
                connection.SupportingEvidenceJson = evidencePayload;
                connection.ReviewedBy = actorName;
                connection.ReviewedAtUtc = DateTime.UtcNow;
            }

            // 2. Promote to Neo4j cross-case graph
            try
            {
                await _neo4jService.CreateCrossCaseLinkAsync(
                    connection.Id,
                    candidate.SourceEntityId,
                    candidate.TargetEntityId,
                    connection.ConnectionType,
                    candidate.MatchScore,
                    candidate.SourceCaseId,
                    candidate.TargetCaseId,
                    actorName,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to synchronize approved cross-case link '{ConnectionId}' to Neo4j.", connection.Id);
            }

            // 3. Audit Logging
            await _auditService.LogAsync(
                actorId,
                actorName,
                "MATCH_APPROVED",
                "EntityMatchCandidate",
                candidate.Id,
                $"Approved cross-case candidate between Entity '{candidate.SourceEntityId}' and '{candidate.TargetEntityId}'.",
                JsonSerializer.Serialize(new { candidateId = candidate.Id, score = candidate.MatchScore, notes = review.ReviewNotes }),
                ipAddress,
                cancellationToken);

            await _auditService.LogAsync(
                actorId,
                actorName,
                "CROSS_CASE_CONNECTION_CREATED",
                "CrossCaseConnection",
                connection.Id,
                $"Created verified cross-case link between Case '{candidate.SourceCaseId}' and '{candidate.TargetCaseId}'.",
                JsonSerializer.Serialize(new { connectionId = connection.Id, sourceCase = candidate.SourceCaseId, targetCase = candidate.TargetCaseId }),
                ipAddress,
                cancellationToken);
        }
        else // REJECTED
        {
            var connection = await _dbContext.CrossCaseConnections
                .FirstOrDefaultAsync(ccc => ccc.CandidateId == candidate.Id, cancellationToken);

            if (connection != null)
            {
                connection.Status = "REJECTED";
                connection.ReviewedBy = actorName;
                connection.ReviewedAtUtc = DateTime.UtcNow;
            }

            await _auditService.LogAsync(
                actorId,
                actorName,
                "MATCH_REJECTED",
                "EntityMatchCandidate",
                candidate.Id,
                $"Rejected cross-case candidate between Entity '{candidate.SourceEntityId}' and '{candidate.TargetEntityId}'.",
                JsonSerializer.Serialize(new { candidateId = candidate.Id, score = candidate.MatchScore, notes = review.ReviewNotes }),
                ipAddress,
                cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<EntityMatchCandidateDto>> GetCrossCaseMatchesForEntityAsync(string entityId, CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.EntityMatchCandidates
            .AsNoTracking()
            .Include(c => c.SourceEntity)
            .Include(c => c.TargetEntity)
            .Include(c => c.SourceCase)
            .Include(c => c.TargetCase)
            .Where(c => c.SourceEntityId == entityId || c.TargetEntityId == entityId)
            .OrderByDescending(c => c.MatchScore)
            .ToListAsync(cancellationToken);

        return candidates.Select(MapToCandidateDto).ToList();
    }

    public async Task<List<CrossCaseConnectionDto>> GetCrossCaseConnectionsAsync(
        string caseId,
        string? connectionType,
        double? minConfidence,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CrossCaseConnections
            .AsNoTracking()
            .Include(ccc => ccc.SourceEntity)
            .Include(ccc => ccc.TargetEntity)
            .Include(ccc => ccc.SourceCase)
            .Include(ccc => ccc.TargetCase)
            .Where(ccc => (ccc.SourceCaseId == caseId || ccc.TargetCaseId == caseId) && ccc.Status == "APPROVED");

        if (!string.IsNullOrWhiteSpace(connectionType))
        {
            query = query.Where(ccc => ccc.ConnectionType.ToUpper() == connectionType.ToUpper());
        }

        if (minConfidence.HasValue)
        {
            query = query.Where(ccc => ccc.Confidence >= minConfidence.Value);
        }

        var list = await query
            .OrderByDescending(ccc => ccc.Confidence)
            .ToListAsync(cancellationToken);

        return list.Select(c => new CrossCaseConnectionDto
        {
            Id = c.Id,
            CandidateId = c.CandidateId,
            SourceCaseId = c.SourceCaseId,
            TargetCaseId = c.TargetCaseId,
            SourceCaseNumber = c.SourceCase?.CaseNumber ?? c.SourceCaseId,
            TargetCaseNumber = c.TargetCase?.CaseNumber ?? c.TargetCaseId,
            SourceEntityId = c.SourceEntityId,
            TargetEntityId = c.TargetEntityId,
            SourceEntityName = c.SourceEntity?.CanonicalName ?? "Source Entity",
            TargetEntityName = c.TargetEntity?.CanonicalName ?? "Target Entity",
            EntityType = c.SourceEntity?.Type ?? "PERSON",
            ConnectionType = c.ConnectionType,
            Confidence = c.Confidence,
            Status = c.Status,
            Explanation = c.Explanation,
            CreatedAtUtc = c.CreatedAtUtc,
            ReviewedAtUtc = c.ReviewedAtUtc,
            ReviewedBy = c.ReviewedBy
        }).ToList();
    }

    public async Task<CrossCaseNetworkDto> GetCrossCaseNetworkAsync(
        string caseId,
        string? connectionType,
        double? minConfidence,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch authorized connections involving caseId
        var connections = await GetCrossCaseConnectionsAsync(caseId, connectionType, minConfidence, cancellationToken);

        var result = new CrossCaseNetworkDto
        {
            FocusCaseId = caseId,
            CrossCaseLinks = connections,
            TotalCrossCaseLinks = connections.Count
        };

        if (connections.Count == 0) return result;

        var connectedCaseIds = connections
            .SelectMany(c => new[] { c.SourceCaseId, c.TargetCaseId })
            .Distinct()
            .ToList();

        result.ConnectedCasesCount = connectedCaseIds.Count;

        var involvedEntityIds = connections
            .SelectMany(c => new[] { c.SourceEntityId, c.TargetEntityId })
            .Distinct()
            .ToList();

        // 2. Fetch involved entities
        var entities = await _dbContext.Entities
            .AsNoTracking()
            .Where(e => involvedEntityIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        result.Nodes = entities.Select(e => new GraphNodeDto
        {
            Id = e.Id,
            Label = e.CanonicalName,
            Name = e.CanonicalName,
            Type = e.Type,
            CaseId = e.CaseId ?? string.Empty,
            ConnectionsCount = 1,
            EvidenceCount = 1,
            Verified = true,
            Properties = new Dictionary<string, object>
            {
                ["caseId"] = e.CaseId ?? string.Empty,
                ["phone"] = e.PhoneNumber ?? string.Empty,
                ["vehicle"] = e.VehicleNumber ?? string.Empty,
                ["district"] = e.District,
                ["canonicalValue"] = e.NormalizedValue
            }
        }).ToList();

        // 3. Assemble cross-case edges
        result.Edges = connections.Select(c => new GraphEdgeDto
        {
            Id = c.Id,
            Source = c.SourceEntityId,
            Target = c.TargetEntityId,
            Type = c.ConnectionType,
            Confidence = c.Confidence,
            SupportingEvidenceCount = 2,
            Properties = new Dictionary<string, object>
            {
                ["sourceCase"] = c.SourceCaseNumber,
                ["targetCase"] = c.TargetCaseNumber,
                ["status"] = c.Status,
                ["explanation"] = c.Explanation
            }
        }).ToList();

        return result;
    }

    private static EntityMatchCandidateDto MapToCandidateDto(EntityMatchCandidate c)
    {
        var factors = ParseFactors(c.MatchExplanationJson);

        return new EntityMatchCandidateDto
        {
            Id = c.Id,
            SourceEntityId = c.SourceEntityId,
            TargetEntityId = c.TargetEntityId,
            SourceCaseId = c.SourceCaseId,
            TargetCaseId = c.TargetCaseId,
            SourceCaseNumber = c.SourceCase?.CaseNumber ?? c.SourceCaseId,
            TargetCaseNumber = c.TargetCase?.CaseNumber ?? c.TargetCaseId,
            EntityType = c.EntityType,
            MatchStatus = c.MatchStatus,
            MatchScore = c.MatchScore,
            MatchMethod = c.MatchMethod,
            Factors = factors,
            CreatedAtUtc = c.CreatedAtUtc,
            ReviewedAtUtc = c.ReviewedAtUtc,
            ReviewedBy = c.ReviewedBy,
            ReviewNotes = c.ReviewNotes,
            SourceEntity = new EntitySummaryDto
            {
                Id = c.SourceEntity?.Id ?? c.SourceEntityId,
                CaseId = c.SourceCaseId,
                CaseNumber = c.SourceCase?.CaseNumber ?? c.SourceCaseId,
                CanonicalName = c.SourceEntity?.CanonicalName ?? string.Empty,
                NormalizedValue = c.SourceEntity?.NormalizedValue ?? string.Empty,
                EntityType = c.SourceEntity?.Type ?? c.EntityType,
                Aliases = c.SourceEntity?.GetAliases() ?? new List<string>(),
                PhoneNumber = c.SourceEntity?.PhoneNumber,
                VehicleNumber = c.SourceEntity?.VehicleNumber,
                AccountNumber = c.SourceEntity?.AccountNumber,
                Location = c.SourceEntity?.Location ?? string.Empty
            },
            TargetEntity = new EntitySummaryDto
            {
                Id = c.TargetEntity?.Id ?? c.TargetEntityId,
                CaseId = c.TargetCaseId,
                CaseNumber = c.TargetCase?.CaseNumber ?? c.TargetCaseId,
                CanonicalName = c.TargetEntity?.CanonicalName ?? string.Empty,
                NormalizedValue = c.TargetEntity?.NormalizedValue ?? string.Empty,
                EntityType = c.TargetEntity?.Type ?? c.EntityType,
                Aliases = c.TargetEntity?.GetAliases() ?? new List<string>(),
                PhoneNumber = c.TargetEntity?.PhoneNumber,
                VehicleNumber = c.TargetEntity?.VehicleNumber,
                AccountNumber = c.TargetEntity?.AccountNumber,
                Location = c.TargetEntity?.Location ?? string.Empty
            }
        };
    }

    private static List<MatchFactorDto> ParseFactors(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<MatchFactorDto>();
        try
        {
            return JsonSerializer.Deserialize<List<MatchFactorDto>>(json, JsonOptions) ?? new List<MatchFactorDto>();
        }
        catch
        {
            return new List<MatchFactorDto>();
        }
    }

    private async Task<List<SupportingEvidenceCitationDto>> GetEntityEvidenceCitationsAsync(string entityId, string caseId, CancellationToken cancellationToken)
    {
        var extractions = await _dbContext.ExtractedEntities
            .AsNoTracking()
            .Include(ee => ee.Evidence)
            .Where(ee => ee.PromotedEntityId == entityId && ee.ReviewStatus == "APPROVED")
            .ToListAsync(cancellationToken);

        return extractions.Select(ee => new SupportingEvidenceCitationDto
        {
            EvidenceId = ee.EvidenceId,
            CaseId = caseId,
            FileName = ee.Evidence?.FileName ?? "Evidence Document",
            EvidenceType = ee.Evidence?.MimeType ?? "Document",
            SourceLocation = ee.SourceLocation,
            ExtractedQuote = ee.RawValue
        }).ToList();
    }

    private async Task<List<string>> GetEntityRelationshipSummariesAsync(string entityId, CancellationToken cancellationToken)
    {
        var rels = await _dbContext.Relationships
            .AsNoTracking()
            .Where(r => r.SourceEntityId == entityId || r.TargetEntityId == entityId)
            .Take(5)
            .Select(r => $"{r.Type} -> {(r.SourceEntityId == entityId ? r.TargetEntityId : r.SourceEntityId)}")
            .ToListAsync(cancellationToken);

        return rels;
    }
}
