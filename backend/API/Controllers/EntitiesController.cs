using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/entities")]
public class EntitiesController : ControllerBase
{
    private readonly IEntityService _entityService;
    private readonly IInvestigationGraphService _graphService;
    private readonly IEntityResolutionService _resolutionService;
    private readonly ILogger<EntitiesController> _logger;

    public EntitiesController(
        IEntityService entityService,
        IInvestigationGraphService graphService,
        IEntityResolutionService resolutionService,
        ILogger<EntitiesController> logger)
    {
        _entityService = entityService;
        _graphService = graphService;
        _resolutionService = resolutionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EntityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntities(
        [FromQuery] string? caseId,
        [FromQuery] string? investigation_id,
        [FromQuery] string? type,
        [FromQuery] string? risk,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var targetCaseId = !string.IsNullOrEmpty(caseId) ? caseId : investigation_id;
        var entities = await _entityService.GetEntitiesAsync(targetCaseId, type, risk, search, cancellationToken);
        return Ok(entities);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntityById(string id, CancellationToken cancellationToken)
    {
        var entity = await _entityService.GetEntityByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return NotFound(new { error = $"Entity with ID '{id}' was not found." });
        }

        return Ok(entity);
    }

    [HttpGet("{id}/neighborhood")]
    [ProducesResponseType(typeof(EntityNeighborhoodDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetNeighborhood(
        string id,
        [FromQuery] int depth = 1,
        [FromQuery] string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var neighborhood = await _graphService.GetEntityNeighborhoodAsync(
                id,
                depth,
                caseId,
                userId,
                userRole,
                cancellationToken);

            return Ok(neighborhood);
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

    [HttpGet("{id}/relationships")]
    [ProducesResponseType(typeof(List<GraphEdgeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRelationships(
        string id,
        [FromQuery] string? caseId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        try
        {
            var rels = await _graphService.GetEntityRelationshipsAsync(
                id,
                caseId,
                userId,
                userRole,
                cancellationToken);

            return Ok(rels);
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

    [HttpPost]
    [ProducesResponseType(typeof(EntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEntity([FromBody] CreateEntityDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) && string.IsNullOrWhiteSpace(dto.CanonicalName))
        {
            return BadRequest(new { error = "Entity name is required." });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var created = await _entityService.CreateEntityAsync(dto, actorId, actorName, ipAddress, cancellationToken);
        return CreatedAtAction(nameof(GetEntityById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Retrieves candidate cross-case entity matches for a specific entity.
    /// </summary>
    [HttpGet("{id}/cross-case-matches")]
    [ProducesResponseType(typeof(List<EntityMatchCandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCrossCaseMatches(string id, CancellationToken cancellationToken)
    {
        var matches = await _resolutionService.GetCrossCaseMatchesForEntityAsync(id, cancellationToken);
        return Ok(matches);
    }
}
