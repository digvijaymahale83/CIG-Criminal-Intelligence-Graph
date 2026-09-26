using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IGraphAnalyticsService
{
    Task<GraphAnalysisRunDto> RunCaseAnalyticsAsync(
        string caseId,
        RunAnalyticsRequestDto request,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<GraphAnalysisRunDto?> GetLatestAnalysisRunAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<CentralityResultsDto> GetCentralityMetricsAsync(
        string caseId,
        string? sortBy,
        int limit,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<List<CommunityClusterDto>> GetCommunitiesAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<List<ConnectedComponentDetailDto>> GetComponentsAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<NetworkStatisticsDto> GetNetworkStatisticsAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<List<GraphAnalyticalLeadDto>> GetModelLeadsAsync(
        string caseId,
        string? status,
        double? minScore,
        int limit,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<EntityAnalyticsProfileDto> GetEntityAnalyticsAsync(
        string entityId,
        string? caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);

    Task<GraphAnalyticalLeadDto> ReviewModelLeadAsync(
        string leadId,
        LeadReviewRequestDto review,
        string userId,
        string userRole,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
