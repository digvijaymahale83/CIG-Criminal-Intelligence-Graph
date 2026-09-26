using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/cases")]
public class CasesController : ControllerBase
{
    private readonly ICaseService _caseService;
    private readonly IInvestigationGraphService _graphService;
    private readonly IEntityResolutionService _resolutionService;
    private readonly IGraphAnalyticsService _analyticsService;
    private readonly ILogger<CasesController> _logger;

    public CasesController(
        ICaseService caseService,
        IInvestigationGraphService graphService,
        IEntityResolutionService resolutionService,
        IGraphAnalyticsService analyticsService,
        ILogger<CasesController> logger)
    {
        _caseService = caseService;
        _graphService = graphService;
        _resolutionService = resolutionService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCases([FromQuery] string? status, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var cases = await _caseService.GetCasesAsync(status, search, cancellationToken);
        return Ok(cases);
    }

    [HttpGet("stats/summary")]
    [ProducesResponseType(typeof(CaseStatsSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatsSummary(CancellationToken cancellationToken)
    {
        var stats = await _caseService.GetStatsSummaryAsync(cancellationToken);
        return Ok(stats);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCaseById(string id, CancellationToken cancellationToken)
    {
        var item = await _caseService.GetCaseByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(new { error = $"Investigation case with ID '{id}' was not found." });
        }

        return Ok(item);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCase([FromBody] CreateCaseDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest(new { error = "Case title is required." });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var createdCase = await _caseService.CreateCaseAsync(dto, actorId, actorName, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(GetCaseById), new { id = createdCase.Id }, createdCase);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCase(string id, [FromBody] UpdateCaseDto dto, CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var updated = await _caseService.UpdateCaseAsync(id, dto, actorId, actorName, ipAddress, cancellationToken);
        if (updated == null)
        {
            return NotFound(new { error = $"Investigation case with ID '{id}' was not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCase(string id, CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var success = await _caseService.DeleteCaseAsync(id, actorId, actorName, ipAddress, cancellationToken);
        if (!success)
        {
            return NotFound(new { error = $"Investigation case with ID '{id}' was not found." });
        }

        return NoContent();
    }

    [HttpGet("{id}/graph")]
    [ProducesResponseType(typeof(CaseGraphResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCaseGraph(
        string id,
        [FromQuery] string? entityType,
        [FromQuery] string? relationshipType,
        [FromQuery] int depth = 1,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var graph = await _graphService.GetCaseGraphAsync(
                id,
                entityType,
                relationshipType,
                depth,
                search,
                limit,
                userId,
                userRole,
                cancellationToken);

            return Ok(graph);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves approved cross-case connections involving the authorized case.
    /// </summary>
    [HttpGet("{id}/cross-case-connections")]
    [ProducesResponseType(typeof(List<CrossCaseConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCrossCaseConnections(
        string id,
        [FromQuery] string? connectionType,
        [FromQuery] double? minConfidence,
        CancellationToken cancellationToken)
    {
        var connections = await _resolutionService.GetCrossCaseConnectionsAsync(
            id,
            connectionType,
            minConfidence,
            cancellationToken);

        return Ok(connections);
    }

    /// <summary>
    /// Returns a cross-case subgraph representing connections between this case and other authorized cases.
    /// </summary>
    [HttpGet("{id}/cross-case-network")]
    [ProducesResponseType(typeof(CrossCaseNetworkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCrossCaseNetwork(
        string id,
        [FromQuery] string? connectionType,
        [FromQuery] double? minConfidence,
        CancellationToken cancellationToken)
    {
        var network = await _resolutionService.GetCrossCaseNetworkAsync(
            id,
            connectionType,
            minConfidence,
            cancellationToken);

        return Ok(network);
    }

    /// <summary>
    /// Returns graph analytics summary statistics for the specified case.
    /// </summary>
    [HttpGet("{id}/analytics-summary")]
    [ProducesResponseType(typeof(NetworkStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalyticsSummary(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";
        var stats = await _analyticsService.GetNetworkStatisticsAsync(id, userId, userRole, cancellationToken);
        return Ok(stats);
    }
}
