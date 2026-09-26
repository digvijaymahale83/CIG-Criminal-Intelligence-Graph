using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IGraphAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IGraphAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? "USR-ANON";

    private string GetUserRole() =>
        User.FindFirst(ClaimTypes.Role)?.Value
        ?? User.FindFirst("role")?.Value
        ?? "INVESTIGATOR";

    /// <summary>
    /// Executes full graph intelligence pipeline (Centrality, Components, Communities, and GAT representation learning) for a case.
    /// </summary>
    [HttpPost("cases/{caseId}/analytics/run")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR,ANALYST")]
    public async Task<ActionResult<GraphAnalysisRunDto>> RunCaseAnalytics(
        [FromRoute] string caseId,
        [FromBody] RunAnalyticsRequestDto? request,
        CancellationToken cancellationToken)
    {
        var runReq = request ?? new RunAnalyticsRequestDto();
        var result = await _analyticsService.RunCaseAnalyticsAsync(
            caseId,
            runReq,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves the latest analysis run metadata for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/analytics")]
    public async Task<ActionResult<GraphAnalysisRunDto>> GetLatestAnalysis(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var run = await _analyticsService.GetLatestAnalysisRunAsync(
            caseId,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        if (run == null)
        {
            return NotFound(new { message = $"No analysis runs found for case '{caseId}'. Run an analysis first." });
        }

        return Ok(run);
    }

    /// <summary>
    /// Retrieves ranked centrality metrics (degree, betweenness, closeness, pageRank) for entities in a case.
    /// </summary>
    [HttpGet("cases/{caseId}/analytics/centrality")]
    public async Task<ActionResult<CentralityResultsDto>> GetCentrality(
        [FromRoute] string caseId,
        [FromQuery] string? sortBy,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var results = await _analyticsService.GetCentralityMetricsAsync(
            caseId,
            sortBy,
            limit,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        return Ok(results);
    }

    /// <summary>
    /// Retrieves detected association clusters (communities) for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/analytics/communities")]
    public async Task<ActionResult<List<CommunityClusterDto>>> GetCommunities(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var communities = await _analyticsService.GetCommunitiesAsync(
            caseId,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        return Ok(communities);
    }

    /// <summary>
    /// Retrieves connected components topology for a case graph.
    /// </summary>
    [HttpGet("cases/{caseId}/analytics/components")]
    public async Task<ActionResult<List<ConnectedComponentDetailDto>>> GetComponents(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var components = await _analyticsService.GetComponentsAsync(
            caseId,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        return Ok(components);
    }

    /// <summary>
    /// Retrieves overall graph topology statistics for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/analytics/statistics")]
    public async Task<ActionResult<NetworkStatisticsDto>> GetStatistics(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var stats = await _analyticsService.GetNetworkStatisticsAsync(
            caseId,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        return Ok(stats);
    }

    /// <summary>
    /// Retrieves model-generated investigative leads (potential links) predicted by the GAT model.
    /// </summary>
    [HttpGet("cases/{caseId}/analytics/leads")]
    public async Task<ActionResult<List<GraphAnalyticalLeadDto>>> GetLeads(
        [FromRoute] string caseId,
        [FromQuery] string? status,
        [FromQuery] double? minScore,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var leads = await _analyticsService.GetModelLeadsAsync(
            caseId,
            status,
            minScore,
            limit,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        return Ok(leads);
    }

    /// <summary>
    /// Retrieves graph analytical profile for a specific entity.
    /// </summary>
    [HttpGet("entities/{entityId}/analytics")]
    public async Task<ActionResult<EntityAnalyticsProfileDto>> GetEntityAnalytics(
        [FromRoute] string entityId,
        [FromQuery] string? caseId,
        CancellationToken cancellationToken)
    {
        var profile = await _analyticsService.GetEntityAnalyticsAsync(
            entityId,
            caseId,
            GetUserId(),
            GetUserRole(),
            cancellationToken);

        return Ok(profile);
    }

    /// <summary>
    /// Submits human investigator review decision (CONFIRM or DISMISS) for a model-generated lead.
    /// Confirming creates a documented relationship; dismissing causes zero graph mutation.
    /// </summary>
    [HttpPost("analytics/leads/{leadId}/review")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR,ANALYST")]
    public async Task<ActionResult<GraphAnalyticalLeadDto>> ReviewLead(
        [FromRoute] string leadId,
        [FromBody] LeadReviewRequestDto review,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var reviewed = await _analyticsService.ReviewModelLeadAsync(
            leadId,
            review,
            GetUserId(),
            GetUserRole(),
            ip,
            cancellationToken);

        return Ok(reviewed);
    }
}
