using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Lists investigative alerts for a case with filtering and pagination.
    /// </summary>
    [HttpGet("cases/{caseId}/alerts")]
    public async Task<ActionResult<List<AlertDto>>> GetCaseAlerts(
        [FromRoute] string caseId,
        [FromQuery] AlertQueryDto query,
        CancellationToken cancellationToken)
    {
        var alerts = await _alertService.GetCaseAlertsAsync(caseId, query, cancellationToken);
        return Ok(alerts);
    }

    /// <summary>
    /// Retrieves full details for a specific investigative alert.
    /// </summary>
    [HttpGet("cases/{caseId}/alerts/{alertId}")]
    public async Task<ActionResult<AlertDto>> GetAlertDetails(
        [FromRoute] string caseId,
        [FromRoute] string alertId,
        CancellationToken cancellationToken)
    {
        var alert = await _alertService.GetAlertDetailsAsync(caseId, alertId, cancellationToken);
        if (alert == null) return NotFound(new { error = $"Alert with ID '{alertId}' not found." });
        return Ok(alert);
    }

    /// <summary>
    /// Retrieves summary counts (by status, severity, and type) for dashboard integration.
    /// </summary>
    [HttpGet("cases/{caseId}/alerts/summary")]
    public async Task<ActionResult<AlertSummaryDto>> GetAlertSummary(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var summary = await _alertService.GetAlertSummaryAsync(caseId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Executes the alert and anomaly detection engine over case data.
    /// </summary>
    [HttpPost("cases/{caseId}/alerts/run")]
    public async Task<ActionResult<AlertRunResultDto>> RunAlertDetection(
        [FromRoute] string caseId,
        [FromBody] RunAlertDetectionRequestDto? request,
        CancellationToken cancellationToken)
    {
        var actorName = User.Identity?.Name ?? "investigator";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _alertService.RunAlertDetectionAsync(
            caseId,
            request ?? new RunAlertDetectionRequestDto(),
            actorName,
            ip,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Transitions alert status from NEW to ACKNOWLEDGED.
    /// </summary>
    [HttpPost("alerts/{alertId}/acknowledge")]
    public async Task<ActionResult<AlertDto>> AcknowledgeAlert(
        [FromRoute] string alertId,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "investigator";
        var actorName = User.Identity?.Name ?? "investigator";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var updated = await _alertService.AcknowledgeAlertAsync(alertId, actorId, actorName, ip, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Transitions alert status to UNDER_REVIEW.
    /// </summary>
    [HttpPost("alerts/{alertId}/start-review")]
    public async Task<ActionResult<AlertDto>> StartReview(
        [FromRoute] string alertId,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "investigator";
        var actorName = User.Identity?.Name ?? "investigator";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var updated = await _alertService.StartReviewAsync(alertId, actorId, actorName, ip, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Resolves an investigative alert with required investigator notes.
    /// Does NOT mutate or synthesize knowledge graph edges.
    /// </summary>
    [HttpPost("alerts/{alertId}/resolve")]
    public async Task<ActionResult<AlertDto>> ResolveAlert(
        [FromRoute] string alertId,
        [FromBody] ReviewAlertRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "investigator";
        var actorName = User.Identity?.Name ?? "investigator";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var updated = await _alertService.ResolveAlertAsync(
                alertId,
                request.Notes ?? "Resolved by investigator.",
                actorId,
                actorName,
                ip,
                cancellationToken);

            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Dismisses an alert (e.g. false positive, routine scheduled activity) with explanation.
    /// </summary>
    [HttpPost("alerts/{alertId}/dismiss")]
    public async Task<ActionResult<AlertDto>> DismissAlert(
        [FromRoute] string alertId,
        [FromBody] ReviewAlertRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "investigator";
        var actorName = User.Identity?.Name ?? "investigator";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var updated = await _alertService.DismissAlertAsync(
                alertId,
                request.Notes ?? "Dismissed as expected activity.",
                actorId,
                actorName,
                ip,
                cancellationToken);

            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
