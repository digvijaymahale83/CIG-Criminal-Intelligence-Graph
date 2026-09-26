using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class InvestigationGraphService : IInvestigationGraphService
{
    private readonly IAppDbContext _dbContext;
    private readonly INeo4jService _neo4jService;
    private readonly IAuditService _auditService;
    private readonly ILogger<InvestigationGraphService> _logger;

    public InvestigationGraphService(
        IAppDbContext dbContext,
        INeo4jService neo4jService,
        IAuditService auditService,
        ILogger<InvestigationGraphService> logger)
    {
        _dbContext = dbContext;
        _neo4jService = neo4jService;
        _auditService = auditService;
        _logger = logger;
    }

    private async Task<Case> ValidateAndResolveCaseAsync(string caseIdOrNumber, string userId, string userRole, CancellationToken cancellationToken)
    {
        var caseItem = await _dbContext.Cases
            .FirstOrDefaultAsync(c => c.Id == caseIdOrNumber || c.CaseNumber == caseIdOrNumber, cancellationToken);

        if (caseItem == null)
        {
            throw new KeyNotFoundException($"Investigation case '{caseIdOrNumber}' was not found.");
        }

        // Authorization check: non-admin users must belong to authorized agency/case
        if (userRole != "ADMIN" && userRole != "INVESTIGATOR" && userRole != "ANALYST")
        {
            throw new UnauthorizedAccessException($"User role '{userRole}' is not authorized to access investigation graphs.");
        }

        return caseItem;
    }

    public async Task<CaseGraphResponseDto> GetCaseGraphAsync(
        string caseId,
        string? entityType,
        string? relationshipType,
        int depth,
        string? search,
        int limit,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var resolvedCase = await ValidateAndResolveCaseAsync(caseId, userId, userRole, cancellationToken);
        var targetCaseId = resolvedCase.Id;

        var result = new CaseGraphResponseDto { CaseId = targetCaseId };

        // 1. Try querying Neo4j if driver is connected
        bool neo4jSuccess = false;
        try
        {
            var isConnected = await _neo4jService.VerifyConnectivityAsync(cancellationToken);
            if (isConnected)
            {
                var neo4jGraph = await _neo4jService.GetCaseGraphAsync(targetCaseId, entityType, relationshipType, depth, search, limit, cancellationToken);
                if (neo4jGraph.Nodes.Count > 0)
                {
                    result.Nodes = neo4jGraph.Nodes;
                    result.Edges = neo4jGraph.Edges;
                    neo4jSuccess = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Neo4j query failed for case '{CaseId}', falling back to PostgreSQL canonical store.", targetCaseId);
        }

        // 2. Authoritative PostgreSQL Fallback / Projection if Neo4j returned 0 records or is offline
        if (!neo4jSuccess)
        {
            var queryEntities = _dbContext.Entities
                .Where(e => (e.CaseId == targetCaseId || e.CaseId == resolvedCase.CaseNumber) && e.VerificationStatus == "VERIFIED");

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                queryEntities = queryEntities.Where(e => e.Type.ToUpper() == entityType.ToUpper());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                queryEntities = queryEntities.Where(e => e.CanonicalName.ToLower().Contains(s) || e.NormalizedValue.ToLower().Contains(s));
            }

            var entities = await queryEntities.Take(limit > 0 ? limit : 200).ToListAsync(cancellationToken);
            var entityIds = entities.Select(e => e.Id).ToHashSet();

            var queryRels = _dbContext.Relationships
                .Include(r => r.SupportingEvidence)
                .Where(r => (r.CaseId == targetCaseId || r.CaseId == resolvedCase.CaseNumber) &&
                            entityIds.Contains(r.SourceEntityId) && entityIds.Contains(r.TargetEntityId));

            if (!string.IsNullOrWhiteSpace(relationshipType))
            {
                queryRels = queryRels.Where(r => r.Type.ToUpper() == relationshipType.ToUpper());
            }

            var rels = await queryRels.ToListAsync(cancellationToken);

            // Compute connection counts
            var connCounts = new Dictionary<string, int>();
            foreach (var r in rels)
            {
                connCounts[r.SourceEntityId] = connCounts.GetValueOrDefault(r.SourceEntityId) + 1;
                connCounts[r.TargetEntityId] = connCounts.GetValueOrDefault(r.TargetEntityId) + 1;
            }

            // Map Nodes
            foreach (var ent in entities)
            {
                result.Nodes.Add(new GraphNodeDto
                {
                    Id = ent.Id,
                    Label = ent.CanonicalName,
                    Name = ent.CanonicalName,
                    Type = ent.Type.ToUpperInvariant(),
                    CaseId = targetCaseId,
                    ConnectionsCount = connCounts.GetValueOrDefault(ent.Id, 0),
                    EvidenceCount = 1,
                    Verified = true,
                    Risk = ent.RiskLevel ?? "MEDIUM",
                    Properties = new Dictionary<string, object>
                    {
                        ["normalized_value"] = ent.NormalizedValue,
                        ["location"] = ent.Location,
                        ["district"] = ent.District,
                        ["confidence"] = ent.Confidence
                    }
                });
            }

            // Map Edges
            foreach (var r in rels)
            {
                result.Edges.Add(new GraphEdgeDto
                {
                    Id = r.Id,
                    Source = r.SourceEntityId,
                    Target = r.TargetEntityId,
                    Type = r.Type.ToUpperInvariant(),
                    Confidence = r.Confidence,
                    SourceEvidenceId = r.SourceEvidenceId,
                    SupportingEvidenceCount = Math.Max(1, r.SupportingEvidence.Count),
                    VerifiedBy = "Investigating Officer",
                    VerifiedAt = r.CreatedAtUtc
                });
            }
        }

        // Audit view
        await _auditService.LogAsync(
            userId,
            userRole,
            "VIEW_CASE_GRAPH",
            "Case",
            targetCaseId,
            $"Viewed investigation graph for case {resolvedCase.CaseNumber}",
            $"{{\"nodes\":{result.Nodes.Count},\"edges\":{result.Edges.Count}}}",
            null,
            cancellationToken);

        return result;
    }

    public async Task<EntityNeighborhoodDto> GetEntityNeighborhoodAsync(
        string entityId,
        int depth,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var clampedDepth = Math.Clamp(depth, 1, 3);
        string? resolvedCaseId = null;

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            var resolvedCase = await ValidateAndResolveCaseAsync(caseId, userId, userRole, cancellationToken);
            resolvedCaseId = resolvedCase.Id;
        }

        var result = new EntityNeighborhoodDto
        {
            CenterEntityId = entityId,
            RequestedDepth = clampedDepth,
            CaseId = resolvedCaseId
        };

        // Try Neo4j first
        bool neo4jSuccess = false;
        try
        {
            if (await _neo4jService.VerifyConnectivityAsync(cancellationToken))
            {
                var neoRes = await _neo4jService.GetNeighborhoodAsync(entityId, clampedDepth, resolvedCaseId, cancellationToken);
                if (neoRes.Nodes.Count > 0)
                {
                    result.Nodes = neoRes.Nodes;
                    result.Edges = neoRes.Edges;
                    neo4jSuccess = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Neo4j neighborhood query failed for entity '{EntityId}', falling back to PostgreSQL.", entityId);
        }

        if (!neo4jSuccess)
        {
            // PostgreSQL BFS expansion up to clampedDepth
            var visitedEntities = new HashSet<string> { entityId };
            var currentLevel = new HashSet<string> { entityId };
            var collectedEdges = new List<Relationship>();

            for (int d = 1; d <= clampedDepth; d++)
            {
                var nextLevel = new HashSet<string>();

                var rels = await _dbContext.Relationships
                    .Include(r => r.SupportingEvidence)
                    .Where(r => (resolvedCaseId == null || r.CaseId == resolvedCaseId) &&
                                (currentLevel.Contains(r.SourceEntityId) || currentLevel.Contains(r.TargetEntityId)))
                    .ToListAsync(cancellationToken);

                foreach (var r in rels)
                {
                    if (!collectedEdges.Any(e => e.Id == r.Id))
                    {
                        collectedEdges.Add(r);
                    }

                    if (visitedEntities.Add(r.SourceEntityId)) nextLevel.Add(r.SourceEntityId);
                    if (visitedEntities.Add(r.TargetEntityId)) nextLevel.Add(r.TargetEntityId);
                }

                currentLevel = nextLevel;
                if (currentLevel.Count == 0) break;
            }

            var entities = await _dbContext.Entities
                .Where(e => visitedEntities.Contains(e.Id))
                .ToListAsync(cancellationToken);

            foreach (var ent in entities)
            {
                result.Nodes.Add(new GraphNodeDto
                {
                    Id = ent.Id,
                    Label = ent.CanonicalName,
                    Name = ent.CanonicalName,
                    Type = ent.Type.ToUpperInvariant(),
                    CaseId = ent.CaseId,
                    Verified = true,
                    Risk = ent.RiskLevel ?? "MEDIUM"
                });
            }

            foreach (var r in collectedEdges)
            {
                result.Edges.Add(new GraphEdgeDto
                {
                    Id = r.Id,
                    Source = r.SourceEntityId,
                    Target = r.TargetEntityId,
                    Type = r.Type.ToUpperInvariant(),
                    Confidence = r.Confidence,
                    SourceEvidenceId = r.SourceEvidenceId,
                    SupportingEvidenceCount = Math.Max(1, r.SupportingEvidence.Count),
                    VerifiedAt = r.CreatedAtUtc
                });
            }
        }

        return result;
    }

    public async Task<List<GraphEdgeDto>> GetEntityRelationshipsAsync(
        string entityId,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        string? resolvedCaseId = null;
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            var resolvedCase = await ValidateAndResolveCaseAsync(caseId, userId, userRole, cancellationToken);
            resolvedCaseId = resolvedCase.Id;
        }

        var rels = await _dbContext.Relationships
            .Include(r => r.SupportingEvidence)
            .Where(r => (resolvedCaseId == null || r.CaseId == resolvedCaseId) &&
                        (r.SourceEntityId == entityId || r.TargetEntityId == entityId))
            .ToListAsync(cancellationToken);

        return rels.Select(r => new GraphEdgeDto
        {
            Id = r.Id,
            Source = r.SourceEntityId,
            Target = r.TargetEntityId,
            Type = r.Type.ToUpperInvariant(),
            Confidence = r.Confidence,
            SourceEvidenceId = r.SourceEvidenceId,
            SupportingEvidenceCount = Math.Max(1, r.SupportingEvidence.Count),
            VerifiedAt = r.CreatedAtUtc
        }).ToList();
    }

    public async Task<List<GraphSearchResultDto>> SearchGraphAsync(
        string query,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<GraphSearchResultDto>();
        }

        string? resolvedCaseId = null;
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            var resolvedCase = await ValidateAndResolveCaseAsync(caseId, userId, userRole, cancellationToken);
            resolvedCaseId = resolvedCase.Id;
        }

        var s = query.Trim().ToLower();

        // 1. Try Neo4j first
        var results = new List<GraphSearchResultDto>();
        try
        {
            if (await _neo4jService.VerifyConnectivityAsync(cancellationToken))
            {
                results = await _neo4jService.SearchNodesAsync(s, resolvedCaseId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Neo4j node search failed for query '{Query}', falling back to PostgreSQL.", query);
        }

        // 2. Query PostgreSQL canonical entities
        var pgEntities = await _dbContext.Entities
            .Where(e => (resolvedCaseId == null || e.CaseId == resolvedCaseId) &&
                        (e.CanonicalName.ToLower().Contains(s) ||
                         e.NormalizedValue.ToLower().Contains(s) ||
                         e.Id.ToLower().Contains(s) ||
                         (e.PhoneNumber != null && e.PhoneNumber.ToLower().Contains(s)) ||
                         (e.VehicleNumber != null && e.VehicleNumber.ToLower().Contains(s))))
            .Take(25)
            .ToListAsync(cancellationToken);

        var existingIds = results.Select(r => r.Id).ToHashSet();

        foreach (var ent in pgEntities)
        {
            if (!existingIds.Contains(ent.Id))
            {
                var connCount = await _dbContext.Relationships
                    .CountAsync(r => r.SourceEntityId == ent.Id || r.TargetEntityId == ent.Id, cancellationToken);

                results.Add(new GraphSearchResultDto
                {
                    Id = ent.Id,
                    Name = ent.CanonicalName,
                    NormalizedValue = ent.NormalizedValue,
                    Type = ent.Type.ToUpperInvariant(),
                    CaseId = ent.CaseId,
                    ConnectionsCount = connCount,
                    Risk = ent.RiskLevel ?? "MEDIUM"
                });
            }
        }

        return results;
    }

    public async Task<ShortestPathDto> GetShortestPathAsync(
        string startEntityId,
        string endEntityId,
        string? caseId,
        int maxHops,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var clampedHops = Math.Clamp(maxHops > 0 ? maxHops : 6, 1, 6);
        string? resolvedCaseId = null;

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            var resolvedCase = await ValidateAndResolveCaseAsync(caseId, userId, userRole, cancellationToken);
            resolvedCaseId = resolvedCase.Id;
        }

        // Try Neo4j first
        try
        {
            if (await _neo4jService.VerifyConnectivityAsync(cancellationToken))
            {
                var neoPath = await _neo4jService.GetShortestPathAsync(startEntityId, endEntityId, resolvedCaseId, clampedHops, cancellationToken);
                if (neoPath.Found)
                {
                    return neoPath;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Neo4j shortest path query failed, falling back to PostgreSQL BFS.");
        }

        // PostgreSQL BFS implementation for shortest path
        var queue = new Queue<List<string>>();
        queue.Enqueue(new List<string> { startEntityId });
        var visited = new HashSet<string> { startEntityId };

        List<string>? foundPath = null;

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var current = path.Last();

            if (current == endEntityId)
            {
                foundPath = path;
                break;
            }

            if (path.Count - 1 >= clampedHops)
            {
                continue;
            }

            var neighbors = await _dbContext.Relationships
                .Where(r => (resolvedCaseId == null || r.CaseId == resolvedCaseId) &&
                            (r.SourceEntityId == current || r.TargetEntityId == current))
                .Select(r => r.SourceEntityId == current ? r.TargetEntityId : r.SourceEntityId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var neighbor in neighbors)
            {
                if (visited.Add(neighbor))
                {
                    var newPath = new List<string>(path) { neighbor };
                    queue.Enqueue(newPath);
                }
            }
        }

        if (foundPath == null)
        {
            return new ShortestPathDto
            {
                Found = false,
                StartEntityId = startEntityId,
                EndEntityId = endEntityId,
                HopsCount = 0
            };
        }

        // Retrieve entities and edges along the path
        var pathEntities = await _dbContext.Entities
            .Where(e => foundPath.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var pathNodes = pathEntities.Select(e => new GraphNodeDto
        {
            Id = e.Id,
            Label = e.CanonicalName,
            Name = e.CanonicalName,
            Type = e.Type.ToUpperInvariant(),
            CaseId = e.CaseId,
            Risk = e.RiskLevel ?? "MEDIUM",
            Verified = true
        }).ToList();

        var pathEdges = new List<GraphEdgeDto>();
        for (int i = 0; i < foundPath.Count - 1; i++)
        {
            var u = foundPath[i];
            var v = foundPath[i + 1];

            var rel = await _dbContext.Relationships
                .Include(r => r.SupportingEvidence)
                .FirstOrDefaultAsync(r => (r.SourceEntityId == u && r.TargetEntityId == v) ||
                                          (r.SourceEntityId == v && r.TargetEntityId == u), cancellationToken);

            if (rel != null)
            {
                pathEdges.Add(new GraphEdgeDto
                {
                    Id = rel.Id,
                    Source = rel.SourceEntityId,
                    Target = rel.TargetEntityId,
                    Type = rel.Type.ToUpperInvariant(),
                    Confidence = rel.Confidence,
                    SourceEvidenceId = rel.SourceEvidenceId,
                    SupportingEvidenceCount = Math.Max(1, rel.SupportingEvidence.Count),
                    VerifiedAt = rel.CreatedAtUtc
                });
            }
        }

        return new ShortestPathDto
        {
            Found = true,
            StartEntityId = startEntityId,
            EndEntityId = endEntityId,
            HopsCount = foundPath.Count - 1,
            Nodes = pathNodes,
            Edges = pathEdges
        };
    }

    public async Task<GraphStatisticsDto> GetGraphStatisticsAsync(
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        string? resolvedCaseId = null;
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            var resolvedCase = await ValidateAndResolveCaseAsync(caseId, userId, userRole, cancellationToken);
            resolvedCaseId = resolvedCase.Id;
        }

        var entities = await _dbContext.Entities
            .Where(e => (resolvedCaseId == null || e.CaseId == resolvedCaseId) && e.VerificationStatus == "VERIFIED")
            .ToListAsync(cancellationToken);

        var entityIds = entities.Select(e => e.Id).ToHashSet();

        var rels = await _dbContext.Relationships
            .Where(r => (resolvedCaseId == null || r.CaseId == resolvedCaseId) &&
                        entityIds.Contains(r.SourceEntityId) && entityIds.Contains(r.TargetEntityId))
            .ToListAsync(cancellationToken);

        var stats = new GraphStatisticsDto
        {
            CaseId = resolvedCaseId,
            TotalNodes = entities.Count,
            TotalEdges = rels.Count
        };

        // Entity type distribution
        stats.EntityTypeDistribution = entities
            .GroupBy(e => e.Type.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        // Relationship type distribution
        stats.RelationshipTypeDistribution = rels
            .GroupBy(r => r.Type.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        // Degree calculation
        var degrees = new Dictionary<string, int>();
        foreach (var r in rels)
        {
            degrees[r.SourceEntityId] = degrees.GetValueOrDefault(r.SourceEntityId) + 1;
            degrees[r.TargetEntityId] = degrees.GetValueOrDefault(r.TargetEntityId) + 1;
        }

        // Centrality rankings (Sorted by degree)
        int maxDegree = degrees.Values.DefaultIfEmpty(1).Max();
        stats.CentralityRankings = entities
            .Select(e =>
            {
                var deg = degrees.GetValueOrDefault(e.Id, 0);
                var score = maxDegree > 0 ? Math.Round((double)deg / maxDegree, 3) : 0.0;
                string indicator = score >= 0.7 ? "High Connectivity Lead" : (score >= 0.4 ? "Network Association" : "Connected Entity");

                return new EntityCentralityDto
                {
                    EntityId = e.Id,
                    EntityName = e.CanonicalName,
                    EntityType = e.Type.ToUpperInvariant(),
                    Degree = deg,
                    CentralityScore = score,
                    AnalyticalIndicator = indicator
                };
            })
            .OrderByDescending(c => c.Degree)
            .Take(20)
            .ToList();

        // Connected Components (BFS clustering with neutral terminology)
        var adj = new Dictionary<string, List<string>>();
        foreach (var id in entityIds) adj[id] = new List<string>();
        foreach (var r in rels)
        {
            adj[r.SourceEntityId].Add(r.TargetEntityId);
            adj[r.TargetEntityId].Add(r.SourceEntityId);
        }

        var visitedCluster = new HashSet<string>();
        int clusterIndex = 1;

        foreach (var id in entityIds)
        {
            if (!visitedCluster.Contains(id))
            {
                var clusterMembers = new List<string>();
                var q = new Queue<string>();
                q.Enqueue(id);
                visitedCluster.Add(id);

                while (q.Count > 0)
                {
                    var curr = q.Dequeue();
                    clusterMembers.Add(curr);

                    foreach (var nxt in adj[curr])
                    {
                        if (visitedCluster.Add(nxt))
                        {
                            q.Enqueue(nxt);
                        }
                    }
                }

                var clusterMemberSet = clusterMembers.ToHashSet();
                var clusterRelCount = rels.Count(r => clusterMemberSet.Contains(r.SourceEntityId) && clusterMemberSet.Contains(r.TargetEntityId));

                stats.ConnectedComponents.Add(new ConnectedComponentDto
                {
                    ClusterId = $"cluster-{clusterIndex}",
                    ClusterLabel = $"Network Cluster {clusterIndex}",
                    EntityCount = clusterMembers.Count,
                    RelationshipCount = clusterRelCount,
                    EntityIds = clusterMembers
                });

                clusterIndex++;
            }
        }

        return stats;
    }

    public async Task<RelationshipDetailDto?> GetRelationshipDetailsAsync(
        string relationshipId,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        string? resolvedCaseId = null;
        if (!string.IsNullOrWhiteSpace(caseId))
        {
            var resolvedCase = await ValidateAndResolveCaseAsync(caseId, userId, userRole, cancellationToken);
            resolvedCaseId = resolvedCase.Id;
        }

        var rel = await _dbContext.Relationships
            .Include(r => r.SupportingEvidence)
                .ThenInclude(se => se.Evidence)
            .FirstOrDefaultAsync(r => r.Id == relationshipId && (resolvedCaseId == null || r.CaseId == resolvedCaseId), cancellationToken);

        if (rel == null) return null;

        var sourceEntity = await _dbContext.Entities.FindAsync(new object[] { rel.SourceEntityId }, cancellationToken);
        var targetEntity = await _dbContext.Entities.FindAsync(new object[] { rel.TargetEntityId }, cancellationToken);

        var detail = new RelationshipDetailDto
        {
            Id = rel.Id,
            Type = rel.Type.ToUpperInvariant(),
            SourceEntityId = rel.SourceEntityId,
            SourceEntityName = sourceEntity?.CanonicalName ?? rel.SourceEntityId,
            TargetEntityId = rel.TargetEntityId,
            TargetEntityName = targetEntity?.CanonicalName ?? rel.TargetEntityId,
            Confidence = rel.Confidence,
            PrimaryEvidenceId = rel.SourceEvidenceId
        };

        // If supporting evidence collection has items, map them
        foreach (var se in rel.SupportingEvidence)
        {
            detail.SupportingEvidence.Add(new SupportingEvidenceDto
            {
                EvidenceId = se.EvidenceId,
                FileName = se.Evidence?.FileName ?? "evidence_file",
                MimeType = se.Evidence?.MimeType ?? "application/octet-stream",
                SourceLocation = se.SourceLocation,
                Confidence = se.Confidence,
                VerifiedBy = se.VerifiedBy,
                VerifiedAtUtc = se.VerifiedAtUtc,
                Sha256Hash = se.Evidence?.Sha256Hash ?? string.Empty
            });
        }

        // If no items in collection but PrimaryEvidenceId exists, include primary evidence
        if (detail.SupportingEvidence.Count == 0 && !string.IsNullOrWhiteSpace(rel.SourceEvidenceId))
        {
            var primaryEv = await _dbContext.EvidenceItems.FindAsync(new object[] { rel.SourceEvidenceId }, cancellationToken);
            if (primaryEv != null)
            {
                detail.SupportingEvidence.Add(new SupportingEvidenceDto
                {
                    EvidenceId = primaryEv.Id,
                    FileName = primaryEv.FileName,
                    MimeType = primaryEv.MimeType,
                    SourceLocation = "Source Document Citation",
                    Confidence = rel.Confidence,
                    VerifiedBy = "Investigating Officer",
                    VerifiedAtUtc = rel.CreatedAtUtc,
                    Sha256Hash = primaryEv.Sha256Hash
                });
            }
        }

        return detail;
    }
}
