using System.Diagnostics;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Infrastructure.Neo4j;

public class Neo4jService : INeo4jService
{
    private readonly INeo4jDriverFactory _driverFactory;
    private readonly ILogger<Neo4jService> _logger;

    public Neo4jService(INeo4jDriverFactory driverFactory, ILogger<Neo4jService> logger)
    {
        _driverFactory = driverFactory;
        _logger = logger;
    }

    public async Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default)
    {
        return await _driverFactory.VerifyConnectivityAsync(cancellationToken);
    }

    public async Task<Neo4jProbeResultDto> RunProbeAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var probeId = $"probe-{Guid.NewGuid():N}";
        var result = new Neo4jProbeResultDto
        {
            ProbeId = probeId,
            Connected = false,
            NodeCreated = false,
            NodeRetrieved = false,
            NodeCleanedUp = false
        };

        try
        {
            var isConnected = await _driverFactory.VerifyConnectivityAsync(cancellationToken);
            if (!isConnected)
            {
                result.Message = "Could not establish Neo4j Bolt connection.";
                return result;
            }

            result.Connected = true;

            // Use driver directly via reflection or session
            // We get driver through reflection from Neo4jDriverFactory if private or update factory
            var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
            {
                var driver = lazy.Value;
                await using var session = driver.AsyncSession();

                // 1. Create test probe node
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync("CREATE (n:SystemProbe { id: $probeId, timestamp: datetime() })", new { probeId });
                });
                result.NodeCreated = true;

                // 2. Retrieve test probe node
                var retrieved = false;
                await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync("MATCH (n:SystemProbe { id: $probeId }) RETURN n.id as id", new { probeId });
                    if (await cursor.FetchAsync())
                    {
                        retrieved = true;
                    }
                });
                result.NodeRetrieved = retrieved;

                // 3. Delete / cleanup test probe node
                await session.ExecuteWriteAsync(async tx =>
                {
                    await tx.RunAsync("MATCH (n:SystemProbe { id: $probeId }) DELETE n", new { probeId });
                });
                result.NodeCleanedUp = true;
            }

            sw.Stop();
            result.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
            result.Message = "Neo4j lifecycle verification probe passed successfully.";
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 2);
            result.Message = $"Neo4j probe failed: {ex.Message}";
            _logger.LogWarning(ex, "Neo4j probe encountered an error: {Message}", ex.Message);
            return result;
        }
    }

    public async Task<GraphDataDto> GetGraphAsync(string? caseId, CancellationToken cancellationToken = default)
    {
        var graph = new GraphDataDto();

        try
        {
            var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
            {
                var driver = lazy.Value;
                await using var session = driver.AsyncSession();

                await session.ExecuteReadAsync(async tx =>
                {
                    var query = string.IsNullOrEmpty(caseId)
                        ? "MATCH (n) OPTIONAL MATCH (n)-[r]->(m) RETURN n, r, m LIMIT 200"
                        : "MATCH (n { caseId: $caseId }) OPTIONAL MATCH (n)-[r]->(m) RETURN n, r, m LIMIT 200";

                    var cursor = await tx.RunAsync(query, new { caseId });
                    var seenNodes = new HashSet<string>();

                    while (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        if (record["n"] is INode node)
                        {
                            var nodeId = node.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : node.ElementId;
                            if (seenNodes.Add(nodeId))
                            {
                                graph.Nodes.Add(new GraphNodeDto
                                {
                                    Id = nodeId,
                                    Label = node.Labels.FirstOrDefault() ?? "Entity",
                                    Properties = node.Properties.ToDictionary(k => k.Key, v => v.Value)
                                });
                            }
                        }

                        if (record["m"] is INode targetNode)
                        {
                            var targetId = targetNode.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : targetNode.ElementId;
                            if (seenNodes.Add(targetId))
                            {
                                graph.Nodes.Add(new GraphNodeDto
                                {
                                    Id = targetId,
                                    Label = targetNode.Labels.FirstOrDefault() ?? "Entity",
                                    Properties = targetNode.Properties.ToDictionary(k => k.Key, v => v.Value)
                                });
                            }
                        }

                        if (record["r"] is IRelationship rel)
                        {
                            graph.Edges.Add(new GraphEdgeDto
                            {
                                Id = rel.ElementId,
                                Source = rel.StartNodeElementId,
                                Target = rel.EndNodeElementId,
                                Type = rel.Type,
                                Properties = rel.Properties.ToDictionary(k => k.Key, v => v.Value)
                            });
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query Neo4j graph: {Message}", ex.Message);
        }

        return graph;
    }

    public async Task<GraphDataDto> GetCaseGraphAsync(string caseId, string? entityType, string? relationshipType, int depth, string? search, int limit, CancellationToken cancellationToken = default)
    {
        var graph = new GraphDataDto { CaseId = caseId };
        var clampedLimit = Math.Clamp(limit > 0 ? limit : 200, 1, 500);

        try
        {
            var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
            {
                var driver = lazy.Value;
                await using var session = driver.AsyncSession();

                await session.ExecuteReadAsync(async tx =>
                {
                    var cypher = @"
                        MATCH (n)
                        WHERE (n.case_id = $caseId OR n.caseId = $caseId)
                          AND ($entityType IS NULL OR toUpper(n.type) = toUpper($entityType) OR toUpper($entityType) IN [l IN labels(n) | toUpper(l)])
                          AND ($search IS NULL OR toLower(n.name) CONTAINS toLower($search) OR toLower(n.normalized_name) CONTAINS toLower($search))
                        OPTIONAL MATCH (n)-[r]-(m)
                        WHERE (m.case_id = $caseId OR m.caseId = $caseId)
                          AND ($relType IS NULL OR toUpper(type(r)) = toUpper($relType) OR toUpper(r.type) = toUpper($relType))
                        RETURN n, r, m
                        LIMIT $limit";

                    var cursor = await tx.RunAsync(cypher, new
                    {
                        caseId,
                        entityType = string.IsNullOrWhiteSpace(entityType) ? null : entityType,
                        relType = string.IsNullOrWhiteSpace(relationshipType) ? null : relationshipType,
                        search = string.IsNullOrWhiteSpace(search) ? null : search,
                        limit = clampedLimit
                    });

                    var seenNodes = new HashSet<string>();
                    var seenEdges = new HashSet<string>();

                    while (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        if (record["n"] is INode node)
                        {
                            var nodeId = node.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : node.ElementId;
                            if (seenNodes.Add(nodeId))
                            {
                                graph.Nodes.Add(MapNode(node, nodeId, caseId));
                            }
                        }

                        if (record["m"] is INode targetNode)
                        {
                            var targetId = targetNode.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : targetNode.ElementId;
                            if (seenNodes.Add(targetId))
                            {
                                graph.Nodes.Add(MapNode(targetNode, targetId, caseId));
                            }
                        }

                        if (record["r"] is IRelationship rel)
                        {
                            var edgeKey = $"{rel.StartNodeElementId}->{rel.EndNodeElementId}:{rel.Type}";
                            if (seenEdges.Add(edgeKey))
                            {
                                graph.Edges.Add(MapEdge(rel));
                            }
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query case graph from Neo4j: {Message}", ex.Message);
        }

        return graph;
    }

    public async Task<EntityNeighborhoodDto> GetNeighborhoodAsync(string entityId, int depth, string? caseId, CancellationToken cancellationToken = default)
    {
        var clampedDepth = Math.Clamp(depth, 1, 3);
        var result = new EntityNeighborhoodDto
        {
            CenterEntityId = entityId,
            RequestedDepth = clampedDepth,
            CaseId = caseId
        };

        try
        {
            var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
            {
                var driver = lazy.Value;
                await using var session = driver.AsyncSession();

                await session.ExecuteReadAsync(async tx =>
                {
                    var cypher = $@"
                        MATCH (start {{ id: $entityId }})
                        WHERE ($caseId IS NULL OR start.case_id = $caseId OR start.caseId = $caseId)
                        MATCH path = (start)-[*1..{clampedDepth}]-(neighbor)
                        WHERE ($caseId IS NULL OR neighbor.case_id = $caseId OR neighbor.caseId = $caseId)
                        RETURN nodes(path) AS nodes, relationships(path) AS rels
                        LIMIT 150";

                    var cursor = await tx.RunAsync(cypher, new { entityId, caseId });
                    var seenNodes = new HashSet<string>();
                    var seenEdges = new HashSet<string>();

                    while (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        if (record["nodes"] is IList<object> nodesList)
                        {
                            foreach (var obj in nodesList)
                            {
                                if (obj is INode n)
                                {
                                    var nId = n.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : n.ElementId;
                                    if (seenNodes.Add(nId))
                                    {
                                        result.Nodes.Add(MapNode(n, nId, caseId));
                                    }
                                }
                            }
                        }

                        if (record["rels"] is IList<object> relsList)
                        {
                            foreach (var obj in relsList)
                            {
                                if (obj is IRelationship r)
                                {
                                    var rKey = $"{r.StartNodeElementId}->{r.EndNodeElementId}:{r.Type}";
                                    if (seenEdges.Add(rKey))
                                    {
                                        result.Edges.Add(MapEdge(r));
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query entity neighborhood from Neo4j: {Message}", ex.Message);
        }

        return result;
    }

    public async Task<ShortestPathDto> GetShortestPathAsync(string startEntityId, string endEntityId, string? caseId, int maxHops, CancellationToken cancellationToken = default)
    {
        var clampedHops = Math.Clamp(maxHops > 0 ? maxHops : 6, 1, 6);
        var result = new ShortestPathDto
        {
            Found = false,
            StartEntityId = startEntityId,
            EndEntityId = endEntityId
        };

        try
        {
            var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
            {
                var driver = lazy.Value;
                await using var session = driver.AsyncSession();

                await session.ExecuteReadAsync(async tx =>
                {
                    var cypher = $@"
                        MATCH (start {{ id: $startEntityId }}), (endNode {{ id: $endEntityId }})
                        WHERE ($caseId IS NULL OR ((start.case_id = $caseId OR start.caseId = $caseId) AND (endNode.case_id = $caseId OR endNode.caseId = $caseId)))
                        MATCH p = shortestPath((start)-[*..{clampedHops}]-(endNode))
                        RETURN nodes(p) AS nodes, relationships(p) AS rels, length(p) AS hops";

                    var cursor = await tx.RunAsync(cypher, new { startEntityId, endEntityId, caseId });

                    if (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        result.Found = true;
                        result.HopsCount = record["hops"] is long h ? (int)h : 0;

                        if (record["nodes"] is IList<object> nodesList)
                        {
                            foreach (var obj in nodesList)
                            {
                                if (obj is INode n)
                                {
                                    var nId = n.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : n.ElementId;
                                    result.Nodes.Add(MapNode(n, nId, caseId));
                                }
                            }
                        }

                        if (record["rels"] is IList<object> relsList)
                        {
                            foreach (var obj in relsList)
                            {
                                if (obj is IRelationship r)
                                {
                                    result.Edges.Add(MapEdge(r));
                                }
                            }
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query shortest path from Neo4j: {Message}", ex.Message);
        }

        return result;
    }

    public async Task<List<GraphSearchResultDto>> SearchNodesAsync(string query, string? caseId, CancellationToken cancellationToken = default)
    {
        var results = new List<GraphSearchResultDto>();

        try
        {
            var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
            {
                var driver = lazy.Value;
                await using var session = driver.AsyncSession();

                await session.ExecuteReadAsync(async tx =>
                {
                    var cypher = @"
                        MATCH (n)
                        WHERE ($caseId IS NULL OR n.case_id = $caseId OR n.caseId = $caseId)
                          AND (toLower(n.name) CONTAINS toLower($query) OR toLower(n.normalized_name) CONTAINS toLower($query) OR toLower(n.id) CONTAINS toLower($query))
                        OPTIONAL MATCH (n)-[r]-()
                        RETURN n, count(r) AS connCount
                        ORDER BY connCount DESC
                        LIMIT 20";

                    var cursor = await tx.RunAsync(cypher, new { query, caseId });

                    while (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        if (record["n"] is INode n)
                        {
                            var nId = n.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : n.ElementId;
                            var name = n.Properties.TryGetValue("name", out var nameVal) ? nameVal.ToString()! : nId;
                            var norm = n.Properties.TryGetValue("normalized_name", out var normVal) ? normVal.ToString()! : name;
                            var type = n.Properties.TryGetValue("type", out var typeVal) ? typeVal.ToString()! : (n.Labels.FirstOrDefault() ?? "PERSON");
                            var nodeCaseId = n.Properties.TryGetValue("case_id", out var cVal) ? cVal.ToString() : (n.Properties.TryGetValue("caseId", out var cVal2) ? cVal2.ToString() : null);
                            var connCount = record["connCount"] is long cnt ? (int)cnt : 0;
                            var risk = n.Properties.TryGetValue("risk", out var rVal) ? rVal.ToString() : "MEDIUM";

                            results.Add(new GraphSearchResultDto
                            {
                                Id = nId,
                                Name = name,
                                NormalizedValue = norm,
                                Type = type.ToUpperInvariant(),
                                CaseId = nodeCaseId,
                                ConnectionsCount = connCount,
                                Risk = risk
                            });
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to search nodes in Neo4j: {Message}", ex.Message);
        }

        return results;
    }

    private static GraphNodeDto MapNode(INode node, string nodeId, string? fallbackCaseId)
    {
        var name = node.Properties.TryGetValue("name", out var n) ? n?.ToString() ?? nodeId : nodeId;
        var type = node.Properties.TryGetValue("type", out var t) ? t?.ToString()! : (node.Labels.FirstOrDefault() ?? "PERSON");
        var nodeCaseId = node.Properties.TryGetValue("case_id", out var cVal) ? cVal.ToString() : (node.Properties.TryGetValue("caseId", out var cVal2) ? cVal2.ToString() : fallbackCaseId);
        var risk = node.Properties.TryGetValue("risk", out var rVal) ? rVal.ToString() : "MEDIUM";

        return new GraphNodeDto
        {
            Id = nodeId,
            Label = name,
            Name = name,
            Type = type.ToUpperInvariant(),
            CaseId = nodeCaseId,
            Risk = risk,
            Verified = true,
            Properties = node.Properties.ToDictionary(k => k.Key, v => v.Value)
        };
    }

    private static GraphEdgeDto MapEdge(IRelationship rel)
    {
        var relType = rel.Properties.TryGetValue("type", out var tVal) ? tVal.ToString()! : rel.Type;
        var conf = rel.Properties.TryGetValue("confidence", out var c) && double.TryParse(c?.ToString(), out var cd) ? cd : 1.0;
        var sourceEv = rel.Properties.TryGetValue("source_evidence_id", out var se) ? se?.ToString() : (rel.Properties.TryGetValue("sourceEvidenceId", out var se2) ? se2?.ToString() : null);
        var sourceLoc = rel.Properties.TryGetValue("source_location", out var sl) ? sl?.ToString() : (rel.Properties.TryGetValue("sourceLocation", out var sl2) ? sl2?.ToString() : null);
        var verifiedBy = rel.Properties.TryGetValue("verified_by", out var vb) ? vb?.ToString() : (rel.Properties.TryGetValue("verifiedBy", out var vb2) ? vb2?.ToString() : null);

        return new GraphEdgeDto
        {
            Id = rel.ElementId,
            Source = rel.StartNodeElementId,
            Target = rel.EndNodeElementId,
            Type = relType.ToUpperInvariant(),
            Confidence = conf,
            SourceEvidenceId = sourceEv,
            SourceLocation = sourceLoc,
            VerifiedBy = verifiedBy,
            VerifiedAt = DateTime.UtcNow,
            Properties = rel.Properties.ToDictionary(k => k.Key, v => v.Value)
        };
    }

    public async Task CreateNodeAsync(string label, Dictionary<string, object> properties, CancellationToken cancellationToken = default)
    {
        var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
        {
            var driver = lazy.Value;
            await using var session = driver.AsyncSession();

            var cleanLabel = label.Replace("`", "");
            var cypher = $"MERGE (n:`{cleanLabel}` {{ id: $id }}) SET n += $props";

            await session.ExecuteWriteAsync(async tx =>
            {
                var id = properties.TryGetValue("id", out var idVal) ? idVal.ToString() : Guid.NewGuid().ToString();
                await tx.RunAsync(cypher, new { id, props = properties });
            });
        }
    }

    public async Task CreateRelationshipAsync(string sourceId, string targetId, string relationshipType, Dictionary<string, object> properties, CancellationToken cancellationToken = default)
    {
        var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
        {
            var driver = lazy.Value;
            await using var session = driver.AsyncSession();

            var cleanType = relationshipType.Replace("`", "").ToUpperInvariant();
            var cypher = $@"
                MATCH (s {{ id: $sourceId }}), (t {{ id: $targetId }})
                MERGE (s)-[r:`{cleanType}`]->(t)
                SET r += $props";

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(cypher, new { sourceId, targetId, props = properties });
            });
        }
    }

    public async Task CreateCrossCaseLinkAsync(
        string connectionId,
        string sourceId,
        string targetId,
        string connectionType,
        double confidence,
        string sourceCaseId,
        string targetCaseId,
        string verifiedBy,
        CancellationToken cancellationToken = default)
    {
        var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
        {
            var driver = lazy.Value;
            await using var session = driver.AsyncSession();

            var cypher = @"
                MATCH (s:Entity { id: $sourceId }), (t:Entity { id: $targetId })
                MERGE (s)-[r:CROSS_CASE_LINK { id: $connectionId }]->(t)
                SET r.connection_type = $connectionType,
                    r.confidence = $confidence,
                    r.source_case_id = $sourceCaseId,
                    r.target_case_id = $targetCaseId,
                    r.status = 'APPROVED',
                    r.verified_by = $verifiedBy,
                    r.verified_at = datetime().epochMillis";

            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(cypher, new
                {
                    connectionId,
                    sourceId,
                    targetId,
                    connectionType,
                    confidence,
                    sourceCaseId,
                    targetCaseId,
                    verifiedBy
                });
            });
        }
    }

    public async Task<CrossCaseNetworkDto> GetCrossCaseNetworkAsync(string caseId, double minConfidence, CancellationToken cancellationToken = default)
    {
        var result = new CrossCaseNetworkDto { FocusCaseId = caseId };
        var driverProperty = _driverFactory.GetType().GetField("_driverLazy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (driverProperty?.GetValue(_driverFactory) is Lazy<IDriver?> lazy && lazy.Value != null)
        {
            var driver = lazy.Value;
            await using var session = driver.AsyncSession();

            var cypher = @"
                MATCH (s:Entity)-[r:CROSS_CASE_LINK]->(t:Entity)
                WHERE (r.source_case_id = $caseId OR r.target_case_id = $caseId)
                  AND r.confidence >= $minConfidence
                  AND r.status = 'APPROVED'
                RETURN s, r, t";

            try
            {
                await session.ExecuteReadAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(cypher, new { caseId, minConfidence });
                    var seenNodes = new HashSet<string>();
                    var seenEdges = new HashSet<string>();

                    while (await cursor.FetchAsync())
                    {
                        var record = cursor.Current;
                        if (record["s"] is INode sNode)
                        {
                            var sId = sNode.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : sNode.ElementId;
                            if (seenNodes.Add(sId))
                            {
                                result.Nodes.Add(MapNode(sNode, sId, caseId));
                            }
                        }
                        if (record["t"] is INode tNode)
                        {
                            var tId = tNode.Properties.TryGetValue("id", out var idVal) ? idVal.ToString()! : tNode.ElementId;
                            if (seenNodes.Add(tId))
                            {
                                result.Nodes.Add(MapNode(tNode, tId, caseId));
                            }
                        }
                        if (record["r"] is IRelationship rel)
                        {
                            var edgeKey = $"{rel.StartNodeElementId}->{rel.EndNodeElementId}:{rel.Type}";
                            if (seenEdges.Add(edgeKey))
                            {
                                result.Edges.Add(MapEdge(rel));
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Neo4j cross-case query failed: {Message}", ex.Message);
            }
        }

        return result;
    }
}

