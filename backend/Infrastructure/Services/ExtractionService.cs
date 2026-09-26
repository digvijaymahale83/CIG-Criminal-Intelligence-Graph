using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ExtractionService : IExtractionService
{
    private readonly IAppDbContext _dbContext;
    private readonly INeo4jService _neo4jService;
    private readonly IFileStorageService _storageService;
    private readonly IAuditService _auditService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExtractionService> _logger;
    private readonly string _aiServiceBaseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public ExtractionService(
        IAppDbContext dbContext,
        INeo4jService neo4jService,
        IFileStorageService storageService,
        IAuditService auditService,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ExtractionService> logger)
    {
        _dbContext = dbContext;
        _neo4jService = neo4jService;
        _storageService = storageService;
        _auditService = auditService;
        _httpClient = httpClient;
        _logger = logger;
        _aiServiceBaseUrl = configuration["AiService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<string> QueueExtractionJobAsync(string evidenceId, string actorId, string actorName, CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.EvidenceItems.FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
        if (evidence == null)
        {
            throw new ArgumentException($"Evidence item '{evidenceId}' not found.");
        }

        var job = new ExtractionJob
        {
            Id = $"job-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}",
            EvidenceId = evidence.Id,
            CaseId = evidence.CaseId,
            Status = "QUEUED",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedById = actorId,
            CreatedByName = actorName
        };

        evidence.ProcessingStatus = "QUEUED";

        _dbContext.ExtractionJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "EVIDENCE_PROCESSING_STARTED",
            "Evidence",
            evidence.Id,
            $"Evidence '{evidence.FileName}' queued for AI extraction (Job: {job.Id})",
            JsonSerializer.Serialize(new { jobId = job.Id, evidenceId = evidence.Id, caseId = evidence.CaseId }),
            null,
            cancellationToken);

        return job.Id;
    }

    public async Task ProcessJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.ExtractionJobs
            .Include(j => j.Evidence)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null || job.Evidence == null)
        {
            _logger.LogWarning("Extraction job '{JobId}' or its evidence item was not found.", jobId);
            return;
        }

        job.Status = "PROCESSING";
        job.StartedAtUtc = DateTime.UtcNow;
        job.Evidence.ProcessingStatus = "PROCESSING";
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // Resolve file path
            var absolutePath = _storageService.GetAbsolutePath(job.Evidence.StoragePath);

            var requestPayload = new
            {
                evidence_id = job.Evidence.Id,
                case_id = job.Evidence.CaseId,
                file_path = absolutePath,
                mime_type = job.Evidence.MimeType,
                original_filename = job.Evidence.FileName
            };

            var requestUri = $"{_aiServiceBaseUrl.TrimEnd('/')}/api/v1/process/evidence";
            var response = await _httpClient.PostAsJsonAsync(requestUri, requestPayload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"AI service returned HTTP {response.StatusCode}: {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<AiExtractionResponseModel>(JsonOptions, cancellationToken: cancellationToken);
            if (result == null || result.Status == "FAILED")
            {
                var errMsg = result?.Metadata?.GetValueOrDefault("error")?.ToString() ?? "AI service extraction failed.";
                throw new InvalidOperationException(errMsg);
            }

            // Staging Extracted Entities
            var entityIdMap = new Dictionary<string, ExtractedEntity>();
            foreach (var ent in result.Entities)
            {
                var entityRecord = new ExtractedEntity
                {
                    Id = $"ext-ent-{Guid.NewGuid().ToString("N")[..8]}",
                    ExtractionJobId = job.Id,
                    EvidenceId = job.Evidence.Id,
                    CaseId = job.Evidence.CaseId,
                    EntityType = ent.Type.ToUpperInvariant(),
                    RawValue = ent.RawValue,
                    NormalizedValue = ent.NormalizedValue,
                    Confidence = ent.Confidence,
                    SourceLocation = ent.Source?.LocationLabel ?? (ent.Source?.Page != null ? $"Page {ent.Source.Page}" : "Document"),
                    ReviewStatus = "PENDING",
                    CreatedAtUtc = DateTime.UtcNow
                };
                _dbContext.ExtractedEntities.Add(entityRecord);
                entityIdMap[ent.Id] = entityRecord;
            }

            // Staging Extracted Relationships
            foreach (var rel in result.Relationships)
            {
                var sourceRecord = entityIdMap.GetValueOrDefault(rel.Source);
                var targetRecord = entityIdMap.GetValueOrDefault(rel.Target);

                var relRecord = new ExtractedRelationship
                {
                    Id = $"ext-rel-{Guid.NewGuid().ToString("N")[..8]}",
                    ExtractionJobId = job.Id,
                    EvidenceId = job.Evidence.Id,
                    CaseId = job.Evidence.CaseId,
                    SourceExtractedEntityId = sourceRecord?.Id,
                    TargetExtractedEntityId = targetRecord?.Id,
                    SourceNormalizedValue = sourceRecord?.NormalizedValue ?? rel.Source,
                    TargetNormalizedValue = targetRecord?.NormalizedValue ?? rel.Target,
                    RelationshipType = rel.Relationship.ToUpperInvariant(),
                    Confidence = rel.Confidence,
                    SourceLocation = rel.SourceLocation ?? "Document",
                    ReviewStatus = "PENDING",
                    CreatedAtUtc = DateTime.UtcNow
                };
                _dbContext.ExtractedRelationships.Add(relRecord);
            }

            // Staging Extracted Events
            foreach (var ev in result.Events)
            {
                var evRecord = new ExtractedEvent
                {
                    Id = $"ext-ev-{Guid.NewGuid().ToString("N")[..8]}",
                    ExtractionJobId = job.Id,
                    EvidenceId = job.Evidence.Id,
                    CaseId = job.Evidence.CaseId,
                    EventType = ev.EventType,
                    Location = ev.Location,
                    RelatedEntitiesJson = JsonSerializer.Serialize(ev.RelatedEntities),
                    Confidence = ev.Confidence,
                    SourceLocation = ev.SourceLocation ?? "Document",
                    ReviewStatus = "PENDING",
                    CreatedAtUtc = DateTime.UtcNow
                };
                _dbContext.ExtractedEvents.Add(evRecord);
            }

            job.Status = "COMPLETED";
            job.CompletedAtUtc = DateTime.UtcNow;
            job.RawTextSnippet = result.TextSnippet;
            job.TotalEntitiesFound = result.Entities.Count;
            job.TotalRelationshipsFound = result.Relationships.Count;
            job.TotalEventsFound = result.Events.Count;

            job.Evidence.ProcessingStatus = "REVIEW_REQUIRED";

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                job.CreatedById,
                job.CreatedByName,
                "EVIDENCE_EXTRACTION_COMPLETED",
                "Evidence",
                job.Evidence.Id,
                $"Extraction completed: {result.Entities.Count} entities, {result.Relationships.Count} relationships staged for review.",
                JsonSerializer.Serialize(new { jobId = job.Id, entitiesCount = result.Entities.Count, relationshipsCount = result.Relationships.Count }),
                null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process extraction job '{JobId}' for evidence '{EvidenceId}'", jobId, job.Evidence.Id);
            job.Status = "FAILED";
            job.ErrorMessage = ex.Message;
            job.Evidence.ProcessingStatus = "FAILED";
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                job.CreatedById,
                job.CreatedByName,
                "EVIDENCE_PROCESSING_FAILED",
                "Evidence",
                job.Evidence.Id,
                $"Extraction job failed: {ex.Message}",
                JsonSerializer.Serialize(new { jobId = job.Id, error = ex.Message }),
                null,
                cancellationToken);
        }
    }

    public async Task<ExtractionResultDto> GetExtractionByEvidenceIdAsync(string evidenceId, CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.EvidenceItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);

        if (evidence == null)
        {
            throw new ArgumentException($"Evidence '{evidenceId}' not found.");
        }

        var latestJob = await _dbContext.ExtractionJobs
            .AsNoTracking()
            .Where(j => j.EvidenceId == evidenceId)
            .OrderByDescending(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var entities = await _dbContext.ExtractedEntities
            .AsNoTracking()
            .Where(e => e.EvidenceId == evidenceId)
            .OrderBy(e => e.EntityType).ThenBy(e => e.RawValue)
            .Select(e => new ExtractedEntityDto
            {
                Id = e.Id,
                ExtractionJobId = e.ExtractionJobId,
                EvidenceId = e.EvidenceId,
                CaseId = e.CaseId,
                EntityType = e.EntityType,
                RawValue = e.RawValue,
                NormalizedValue = e.NormalizedValue,
                Confidence = e.Confidence,
                SourceLocation = e.SourceLocation,
                ReviewStatus = e.ReviewStatus,
                PromotedEntityId = e.PromotedEntityId,
                ReviewedBy = e.ReviewedBy,
                ReviewedAtUtc = e.ReviewedAtUtc,
                CreatedAtUtc = e.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var relationships = await _dbContext.ExtractedRelationships
            .AsNoTracking()
            .Where(r => r.EvidenceId == evidenceId)
            .OrderBy(r => r.RelationshipType)
            .Select(r => new ExtractedRelationshipDto
            {
                Id = r.Id,
                ExtractionJobId = r.ExtractionJobId,
                EvidenceId = r.EvidenceId,
                CaseId = r.CaseId,
                SourceExtractedEntityId = r.SourceExtractedEntityId,
                TargetExtractedEntityId = r.TargetExtractedEntityId,
                SourceNormalizedValue = r.SourceNormalizedValue,
                TargetNormalizedValue = r.TargetNormalizedValue,
                RelationshipType = r.RelationshipType,
                Confidence = r.Confidence,
                SourceLocation = r.SourceLocation,
                ReviewStatus = r.ReviewStatus,
                PromotedRelationshipId = r.PromotedRelationshipId,
                ReviewedBy = r.ReviewedBy,
                ReviewedAtUtc = r.ReviewedAtUtc,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var events = await _dbContext.ExtractedEvents
            .AsNoTracking()
            .Where(ev => ev.EvidenceId == evidenceId)
            .Select(ev => new ExtractedEventDto
            {
                Id = ev.Id,
                ExtractionJobId = ev.ExtractionJobId,
                EvidenceId = ev.EvidenceId,
                CaseId = ev.CaseId,
                EventType = ev.EventType,
                EventTimestampUtc = ev.EventTimestampUtc,
                Location = ev.Location,
                RelatedEntitiesJson = ev.RelatedEntitiesJson,
                Confidence = ev.Confidence,
                SourceLocation = ev.SourceLocation,
                ReviewStatus = ev.ReviewStatus,
                CreatedAtUtc = ev.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new ExtractionResultDto
        {
            EvidenceId = evidence.Id,
            CaseId = evidence.CaseId,
            Status = evidence.ProcessingStatus,
            ErrorMessage = latestJob?.ErrorMessage,
            RawTextSnippet = latestJob?.RawTextSnippet,
            Entities = entities,
            Relationships = relationships,
            Events = events,
            Metadata = new Dictionary<string, object>
            {
                ["jobId"] = latestJob?.Id ?? string.Empty,
                ["totalEntities"] = entities.Count,
                ["totalRelationships"] = relationships.Count,
                ["pendingReviews"] = entities.Count(e => e.ReviewStatus == "PENDING") + relationships.Count(r => r.ReviewStatus == "PENDING")
            }
        };
    }

    public async Task<bool> ApproveEntityAsync(
        string evidenceId,
        string entityId,
        string? correctedRawValue,
        string? correctedNormalizedValue,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExtractedEntities
            .FirstOrDefaultAsync(e => e.Id == entityId && e.EvidenceId == evidenceId, cancellationToken);

        if (entity == null) return false;

        if (!string.IsNullOrWhiteSpace(correctedRawValue))
        {
            entity.RawValue = correctedRawValue.Trim();
        }
        if (!string.IsNullOrWhiteSpace(correctedNormalizedValue))
        {
            entity.NormalizedValue = correctedNormalizedValue.Trim();
        }

        entity.ReviewStatus = "APPROVED";
        entity.ReviewedBy = actorName;
        entity.ReviewedAtUtc = DateTime.UtcNow;

        // 1. Create or link canonical EntityItem in PostgreSQL
        var canonical = await _dbContext.Entities
            .FirstOrDefaultAsync(e => e.Type == entity.EntityType && e.NormalizedValue == entity.NormalizedValue, cancellationToken);

        if (canonical == null)
        {
            canonical = new EntityItem
            {
                Id = $"ent-can-{Guid.NewGuid().ToString("N")[..8]}",
                CaseId = entity.CaseId,
                Type = entity.EntityType,
                CanonicalName = entity.RawValue,
                NormalizedValue = entity.NormalizedValue,
                Confidence = entity.Confidence,
                VerificationStatus = "VERIFIED",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            _dbContext.Entities.Add(canonical);
        }

        entity.PromotedEntityId = canonical.Id;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // 2. CRITICAL: Promote APPROVED entity to Neo4j
        try
        {
            var nodeProps = new Dictionary<string, object>
            {
                ["id"] = canonical.Id,
                ["name"] = entity.RawValue,
                ["normalized_name"] = entity.NormalizedValue,
                ["type"] = entity.EntityType,
                ["confidence"] = entity.Confidence,
                ["source_evidence_id"] = entity.EvidenceId,
                ["verified_by"] = actorName,
                ["verified_at"] = DateTime.UtcNow.ToString("o"),
                ["created_at"] = DateTime.UtcNow.ToString("o")
            };
            if (!string.IsNullOrEmpty(entity.CaseId))
            {
                nodeProps["caseId"] = entity.CaseId;
            }

            await _neo4jService.CreateNodeAsync(entity.EntityType, nodeProps, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to promote approved entity '{EntityId}' to Neo4j: {Message}", entity.Id, ex.Message);
        }

        // 3. Audit Log
        await _auditService.LogAsync(
            actorId,
            actorName,
            "ENTITY_APPROVED",
            "ExtractedEntity",
            entity.Id,
            $"Entity '{entity.RawValue}' ({entity.EntityType}) approved and promoted to graph.",
            JsonSerializer.Serialize(new { entityId = entity.Id, canonicalId = canonical.Id, normalizedValue = entity.NormalizedValue }),
            ipAddress,
            cancellationToken);

        return true;
    }

    public async Task<bool> RejectEntityAsync(
        string evidenceId,
        string entityId,
        string? reason,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExtractedEntities
            .FirstOrDefaultAsync(e => e.Id == entityId && e.EvidenceId == evidenceId, cancellationToken);

        if (entity == null) return false;

        entity.ReviewStatus = "REJECTED";
        entity.ReviewedBy = actorName;
        entity.ReviewedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "ENTITY_REJECTED",
            "ExtractedEntity",
            entity.Id,
            $"Entity '{entity.RawValue}' ({entity.EntityType}) rejected. Reason: {reason ?? "None provided"}",
            JsonSerializer.Serialize(new { entityId = entity.Id, reason }),
            ipAddress,
            cancellationToken);

        return true;
    }

    public async Task<bool> EditEntityAsync(
        string evidenceId,
        string entityId,
        string newRawValue,
        string? newNormalizedValue,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExtractedEntities
            .FirstOrDefaultAsync(e => e.Id == entityId && e.EvidenceId == evidenceId, cancellationToken);

        if (entity == null) return false;

        var previousRaw = entity.RawValue;
        var previousNorm = entity.NormalizedValue;

        entity.RawValue = newRawValue.Trim();
        entity.NormalizedValue = !string.IsNullOrWhiteSpace(newNormalizedValue) ? newNormalizedValue.Trim() : newRawValue.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "ENTITY_VALUE_CORRECTED",
            "ExtractedEntity",
            entity.Id,
            $"Entity value edited from '{previousRaw}' to '{entity.RawValue}'.",
            JsonSerializer.Serialize(new { entityId = entity.Id, previousRaw, previousNorm, newRaw = entity.RawValue, newNorm = entity.NormalizedValue }),
            ipAddress,
            cancellationToken);

        return true;
    }

    public async Task<bool> ApproveRelationshipAsync(
        string evidenceId,
        string relationshipId,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var rel = await _dbContext.ExtractedRelationships
            .FirstOrDefaultAsync(r => r.Id == relationshipId && r.EvidenceId == evidenceId, cancellationToken);

        if (rel == null) return false;

        rel.ReviewStatus = "APPROVED";
        rel.ReviewedBy = actorName;
        rel.ReviewedAtUtc = DateTime.UtcNow;

        // Find or create source and target canonical entities
        var sourceCanonical = await _dbContext.Entities.FirstOrDefaultAsync(e => e.NormalizedValue == rel.SourceNormalizedValue, cancellationToken);
        var targetCanonical = await _dbContext.Entities.FirstOrDefaultAsync(e => e.NormalizedValue == rel.TargetNormalizedValue, cancellationToken);

        var sourceId = sourceCanonical?.Id ?? $"ent-src-{Guid.NewGuid().ToString("N")[..8]}";
        var targetId = targetCanonical?.Id ?? $"ent-tgt-{Guid.NewGuid().ToString("N")[..8]}";

        var canonicalRel = new Relationship
        {
            Id = $"rel-can-{Guid.NewGuid().ToString("N")[..8]}",
            CaseId = rel.CaseId,
            SourceEntityId = sourceId,
            TargetEntityId = targetId,
            Type = rel.RelationshipType,
            Confidence = rel.Confidence,
            SourceEvidenceId = rel.EvidenceId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Relationships.Add(canonicalRel);
        rel.PromotedRelationshipId = canonicalRel.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Promote to Neo4j with full provenance
        try
        {
            var relProps = new Dictionary<string, object>
            {
                ["id"] = canonicalRel.Id,
                ["evidence_id"] = rel.EvidenceId,
                ["confidence"] = rel.Confidence,
                ["source_location"] = rel.SourceLocation,
                ["verified_by"] = actorName,
                ["verified_at"] = DateTime.UtcNow.ToString("o"),
                ["created_at"] = DateTime.UtcNow.ToString("o")
            };
            if (!string.IsNullOrEmpty(rel.CaseId))
            {
                relProps["caseId"] = rel.CaseId;
            }

            await _neo4jService.CreateRelationshipAsync(sourceId, targetId, rel.RelationshipType, relProps, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to promote approved relationship '{RelId}' to Neo4j: {Message}", rel.Id, ex.Message);
        }

        await _auditService.LogAsync(
            actorId,
            actorName,
            "RELATIONSHIP_APPROVED",
            "ExtractedRelationship",
            rel.Id,
            $"Relationship '{rel.SourceNormalizedValue} --{rel.RelationshipType}--> {rel.TargetNormalizedValue}' approved and promoted to graph.",
            JsonSerializer.Serialize(new { relationshipId = rel.Id, canonicalRelId = canonicalRel.Id }),
            ipAddress,
            cancellationToken);

        return true;
    }

    public async Task<bool> RejectRelationshipAsync(
        string evidenceId,
        string relationshipId,
        string? reason,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var rel = await _dbContext.ExtractedRelationships
            .FirstOrDefaultAsync(r => r.Id == relationshipId && r.EvidenceId == evidenceId, cancellationToken);

        if (rel == null) return false;

        rel.ReviewStatus = "REJECTED";
        rel.ReviewedBy = actorName;
        rel.ReviewedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "RELATIONSHIP_REJECTED",
            "ExtractedRelationship",
            rel.Id,
            $"Relationship '{rel.SourceNormalizedValue} --{rel.RelationshipType}--> {rel.TargetNormalizedValue}' rejected. Reason: {reason ?? "None"}",
            JsonSerializer.Serialize(new { relationshipId = rel.Id, reason }),
            ipAddress,
            cancellationToken);

        return true;
    }

    public async Task<int> ProcessPendingQueueAsync(CancellationToken cancellationToken = default)
    {
        var queuedJobs = await _dbContext.ExtractionJobs
            .Where(j => j.Status == "QUEUED")
            .OrderBy(j => j.CreatedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        int processed = 0;
        foreach (var job in queuedJobs)
        {
            try
            {
                await ProcessJobAsync(job.Id, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued extraction job '{JobId}': {Message}", job.Id, ex.Message);
            }
        }

        return processed;
    }

    // Helper model for deserializing response from Python AI Service
    private class AiExtractionResponseModel
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("text_snippet")]
        public string? TextSnippet { get; set; }

        [JsonPropertyName("entities")]
        public List<AiEntityModel> Entities { get; set; } = new();

        [JsonPropertyName("relationships")]
        public List<AiRelationshipModel> Relationships { get; set; } = new();

        [JsonPropertyName("events")]
        public List<AiEventModel> Events { get; set; } = new();

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    private class AiEntityModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("raw_value")]
        public string RawValue { get; set; } = string.Empty;

        [JsonPropertyName("normalized_value")]
        public string NormalizedValue { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("source")]
        public AiSourceModel? Source { get; set; }
    }

    private class AiRelationshipModel
    {
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("relationship")]
        public string Relationship { get; set; } = string.Empty;

        [JsonPropertyName("target")]
        public string Target { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("source_location")]
        public string? SourceLocation { get; set; }
    }

    private class AiEventModel
    {
        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("related_entities")]
        public List<string>? RelatedEntities { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("source_location")]
        public string? SourceLocation { get; set; }
    }

    private class AiSourceModel
    {
        [JsonPropertyName("page")]
        public int? Page { get; set; }

        [JsonPropertyName("row")]
        public int? Row { get; set; }

        [JsonPropertyName("location_label")]
        public string? LocationLabel { get; set; }
    }
}
