using Application.DTOs;

namespace Application.Common.Interfaces;

public interface INeo4jService
{
    Task<bool> VerifyConnectivityAsync(CancellationToken cancellationToken = default);
    Task<Neo4jProbeResultDto> RunProbeAsync(CancellationToken cancellationToken = default);
    Task<GraphDataDto> GetGraphAsync(string? caseId, CancellationToken cancellationToken = default);
    Task<GraphDataDto> GetCaseGraphAsync(string caseId, string? entityType, string? relationshipType, int depth, string? search, int limit, CancellationToken cancellationToken = default);
    Task<EntityNeighborhoodDto> GetNeighborhoodAsync(string entityId, int depth, string? caseId, CancellationToken cancellationToken = default);
    Task<ShortestPathDto> GetShortestPathAsync(string startEntityId, string endEntityId, string? caseId, int maxHops, CancellationToken cancellationToken = default);
    Task<List<GraphSearchResultDto>> SearchNodesAsync(string query, string? caseId, CancellationToken cancellationToken = default);
    Task CreateNodeAsync(string label, Dictionary<string, object> properties, CancellationToken cancellationToken = default);
    Task CreateRelationshipAsync(string sourceId, string targetId, string relationshipType, Dictionary<string, object> properties, CancellationToken cancellationToken = default);
    Task CreateCrossCaseLinkAsync(string connectionId, string sourceId, string targetId, string connectionType, double confidence, string sourceCaseId, string targetCaseId, string verifiedBy, CancellationToken cancellationToken = default);
    Task<CrossCaseNetworkDto> GetCrossCaseNetworkAsync(string caseId, double minConfidence, CancellationToken cancellationToken = default);
}
