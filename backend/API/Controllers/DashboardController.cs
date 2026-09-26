using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("api/v1/cases")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a consolidated, case-scoped, authorization-aware dashboard operational snapshot.
    /// Orchestrates real persisted data across entities, relationships, network analytics, alerts,
    /// timeline events, geospatial locations, cross-case linkages, and evidence integrity ledgers.
    /// </summary>
    /// <param name="caseId">Unique case identifier or official case number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{caseId}/dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCaseDashboard(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(caseId))
        {
            return BadRequest(new { error = "Case identifier is required." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var dashboard = await _dashboardService.GetCaseDashboardAsync(
                caseId,
                userId,
                userRole,
                cancellationToken);

            return Ok(dashboard);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build case dashboard for '{CaseId}': {Message}", caseId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An internal error occurred while generating the dashboard." });
        }
    }
}
