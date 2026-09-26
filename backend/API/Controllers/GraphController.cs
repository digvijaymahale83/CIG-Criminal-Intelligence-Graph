using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/graph")]
public class GraphController : ControllerBase
{
    private readonly INeo4jService _neo4jService;
    private readonly IInvestigationGraphService _graphService;
    private readonly ILogger<GraphController> _logger;

    public GraphController(
        INeo4jService neo4jService,
        IInvestigationGraphService graphService,
        ILogger<GraphController> logger)
    {
        _neo4jService = neo4jService;
        _graphService = graphService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GraphDataDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGraph(
        [FromQuery] string? caseId,
        [FromQuery] string? investigation_id,
        CancellationToken cancellationToken)
    {
        var targetCaseId = !string.IsNullOrEmpty(caseId) ? caseId : investigation_id;
        var graph = await _neo4jService.GetGraphAsync(targetCaseId, cancellationToken);
        return Ok(graph);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<GraphSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchGraph(
        [FromQuery] string query,
        [FromQuery] string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var results = await _graphService.SearchGraphAsync(query, caseId, userId, userRole, cancellationToken);
            return Ok(results);
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

    [HttpGet("path")]
    [ProducesResponseType(typeof(ShortestPathDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetShortestPath(
        [FromQuery] string startEntityId,
        [FromQuery] string endEntityId,
        [FromQuery] string? caseId = null,
        [FromQuery] int maxHops = 4,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(startEntityId) || string.IsNullOrWhiteSpace(endEntityId))
        {
            return BadRequest(new { error = "startEntityId and endEntityId are required." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var path = await _graphService.GetShortestPathAsync(
                startEntityId,
                endEntityId,
                caseId,
                maxHops,
                userId,
                userRole,
                cancellationToken);

            return Ok(path);
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

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(GraphStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var stats = await _graphService.GetGraphStatisticsAsync(caseId, userId, userRole, cancellationToken);
            return Ok(stats);
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

    [HttpGet("relationship/{relationshipId}")]
    [ProducesResponseType(typeof(RelationshipDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelationshipDetails(
        string relationshipId,
        [FromQuery] string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var details = await _graphService.GetRelationshipDetailsAsync(relationshipId, caseId, userId, userRole, cancellationToken);
            if (details == null)
            {
                return NotFound(new { error = $"Relationship '{relationshipId}' was not found." });
            }

            return Ok(details);
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

    [HttpPost("probe")]
    [ProducesResponseType(typeof(Neo4jProbeResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunProbe(CancellationToken cancellationToken)
    {
        var result = await _neo4jService.RunProbeAsync(cancellationToken);
        return Ok(result);
    }
}
