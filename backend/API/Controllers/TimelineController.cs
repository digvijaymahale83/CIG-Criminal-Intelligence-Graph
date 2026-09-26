using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TimelineController : ControllerBase
{
    private readonly ITemporalService _temporalService;
    private readonly ILogger<TimelineController> _logger;

    public TimelineController(
        ITemporalService temporalService,
        ILogger<TimelineController> logger)
    {
        _temporalService = temporalService;
        _logger = logger;
    }

    private string GetUserName() =>
        User.FindFirst(ClaimTypes.Name)?.Value
        ?? User.FindFirst("name")?.Value
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? "Investigator";

    /// <summary>
    /// Retrieves chronological timeline, active period, activity clusters, and sequence highlights for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/timeline")]
    public async Task<ActionResult<TimelineResponseDto>> GetCaseTimeline(
        [FromRoute] string caseId,
        [FromQuery] TimelineQueryDto query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _temporalService.GetCaseTimelineAsync(caseId, query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves paginated timeline events for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/timeline/events")]
    public async Task<ActionResult<List<TimelineEventDto>>> GetCaseTimelineEvents(
        [FromRoute] string caseId,
        [FromQuery] TimelineQueryDto query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _temporalService.GetCaseTimelineEventsAsync(caseId, query, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves entity activity timeline across cases or for a specific case.
    /// </summary>
    [HttpGet("entities/{entityId}/timeline")]
    public async Task<ActionResult<List<TimelineEventDto>>> GetEntityTimeline(
        [FromRoute] string entityId,
        [FromQuery] string? caseId,
        CancellationToken cancellationToken)
    {
        var result = await _temporalService.GetEntityTimelineAsync(entityId, caseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves chronological events associated with a relationship.
    /// </summary>
    [HttpGet("relationships/{relationshipId}/timeline")]
    public async Task<ActionResult<List<TimelineEventDto>>> GetRelationshipTimeline(
        [FromRoute] string relationshipId,
        [FromQuery] string? caseId,
        CancellationToken cancellationToken)
    {
        var result = await _temporalService.GetRelationshipTimelineAsync(relationshipId, caseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the active temporal date boundaries (earliest/latest) for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/timeline/range")]
    public async Task<ActionResult<TimelineDateRangeDto>> GetTimelineDateRange(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var result = await _temporalService.GetTimelineDateRangeAsync(caseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the latest temporal analysis result and findings for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/temporal-analysis")]
    public async Task<ActionResult<TemporalAnalysisResultDto>> GetLatestTemporalAnalysis(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var result = await _temporalService.GetLatestTemporalAnalysisAsync(caseId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"No temporal analysis run found for case '{caseId}'." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Executes temporal analysis (overlap detection, sequence analysis, clustering) for a case.
    /// </summary>
    [HttpPost("cases/{caseId}/temporal-analysis/run")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR,ANALYST")]
    public async Task<ActionResult<TemporalAnalysisResultDto>> RunTemporalAnalysis(
        [FromRoute] string caseId,
        [FromBody] RunTemporalAnalysisRequestDto? request,
        CancellationToken cancellationToken)
    {
        var req = request ?? new RunTemporalAnalysisRequestDto();
        try
        {
            var result = await _temporalService.RunTemporalAnalysisAsync(caseId, req, GetUserName(), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves detected temporal overlap signals for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/temporal-overlaps")]
    public async Task<ActionResult<List<TemporalOverlapDto>>> GetTemporalOverlaps(
        [FromRoute] string caseId,
        [FromQuery] string? entityId,
        [FromQuery] string? location,
        [FromQuery] double minDurationMinutes,
        CancellationToken cancellationToken)
    {
        var result = await _temporalService.GetTemporalOverlapsAsync(caseId, entityId, location, minDurationMinutes, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves chronological sequence flow for an entity with time deltas.
    /// </summary>
    [HttpGet("entities/{entityId}/sequence")]
    public async Task<ActionResult<TemporalSequenceDto>> GetEntitySequence(
        [FromRoute] string entityId,
        [FromQuery] string? caseId,
        CancellationToken cancellationToken)
    {
        var result = await _temporalService.GetEntitySequenceAsync(entityId, caseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves case dashboard temporal summary metrics.
    /// </summary>
    [HttpGet("cases/{caseId}/temporal-summary")]
    public async Task<ActionResult<TemporalSummaryDto>> GetTemporalSummary(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var result = await _temporalService.GetTemporalSummaryAsync(caseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Submits investigator confirmation or dismissal for a temporal signal.
    /// </summary>
    [HttpPost("cases/{caseId}/temporal-signals/{signalId}/review")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR,ANALYST")]
    public async Task<ActionResult<TemporalOverlapDto>> ReviewTemporalSignal(
        [FromRoute] string caseId,
        [FromRoute] string signalId,
        [FromBody] ReviewTemporalSignalRequestDto review,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _temporalService.ReviewTemporalSignalAsync(signalId, review, GetUserName(), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
