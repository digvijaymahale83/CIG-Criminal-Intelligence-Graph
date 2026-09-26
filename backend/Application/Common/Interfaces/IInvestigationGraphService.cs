namespace Application.Common.Interfaces;

using Application.DTOs;

public interface IInvestigationGraphService
{
    Task<CaseGraphResponseDto> GetCaseGraphAsync(
        string caseId,
        string? entityType,
        string? relationshipType,
        int depth,
        string? search,
        int limit,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<EntityNeighborhoodDto> GetEntityNeighborhoodAsync(
        string entityId,
        int depth,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<List<GraphEdgeDto>> GetEntityRelationshipsAsync(
        string entityId,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<List<GraphSearchResultDto>> SearchGraphAsync(
        string query,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<ShortestPathDto> GetShortestPathAsync(
        string startEntityId,
        string endEntityId,
        string? caseId,
        int maxHops,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<GraphStatisticsDto> GetGraphStatisticsAsync(
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<RelationshipDetailDto?> GetRelationshipDetailsAsync(
        string relationshipId,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);
}
