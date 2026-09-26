using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class GeospatialController : ControllerBase
{
    private readonly IGeospatialService _geospatialService;
    private readonly IAuditService _auditService;
    private readonly ILogger<GeospatialController> _logger;

    public GeospatialController(
        IGeospatialService geospatialService,
        IAuditService auditService,
        ILogger<GeospatialController> logger)
    {
        _geospatialService = geospatialService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all recorded locations associated with an investigation.
    /// </summary>
    [HttpGet("cases/{caseId}/locations")]
    public async Task<ActionResult<List<LocationDto>>> GetCaseLocations(
        [FromRoute] string caseId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var result = await _geospatialService.GetCaseLocationsAsync(caseId, search, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves structured map data (markers, activity density, clusters, active signals) for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/map")]
    public async Task<ActionResult<CaseMapDto>> GetCaseMapData(
        [FromRoute] string caseId,
        [FromQuery] string? eventType,
        [FromQuery] string? entityId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] bool verifiedOnly = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _geospatialService.GetCaseMapDataAsync(
            caseId, eventType, entityId, startDate, endDate, verifiedOnly, cancellationToken);

        // Audit map viewed
        var actor = User.Identity?.Name ?? "system";
        await _auditService.LogAsync(
            actorId: actor,
            actorName: actor,
            action: "MAP_VIEWED",
            resourceType: "Case",
            resourceId: caseId,
            details: $"Investigation map viewed for case '{caseId}'. Total locations: {result.TotalLocations}.",
            metadataJson: null,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of a specific location.
    /// </summary>
    [HttpGet("locations/{locationId}")]
    public async Task<ActionResult<LocationDto>> GetLocationDetails(
        [FromRoute] string locationId,
        CancellationToken cancellationToken)
    {
        var result = await _geospatialService.GetLocationDetailsAsync(locationId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Location with ID '{locationId}' not found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Retrieves comprehensive activity stream, visiting entities, and evidence citations at a specific location.
    /// </summary>
    [HttpGet("locations/{locationId}/activity")]
    public async Task<ActionResult<LocationActivityDto>> GetLocationActivity(
        [FromRoute] string locationId,
        [FromQuery] string? caseId,
        CancellationToken cancellationToken)
    {
        var result = await _geospatialService.GetLocationActivityAsync(locationId, caseId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Location with ID '{locationId}' not found." });
        }

        // Audit location viewed
        var actor = User.Identity?.Name ?? "system";
        await _auditService.LogAsync(
            actorId: actor,
            actorName: actor,
            action: "LOCATION_VIEWED",
            resourceType: "Location",
            resourceId: locationId,
            details: $"Viewed activity for location '{result.Location.Name}'. Total events: {result.Events.Count}.",
            metadataJson: null,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken: cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves all recorded locations associated with a target entity.
    /// </summary>
    [HttpGet("entities/{entityId}/locations")]
    public async Task<ActionResult<List<LocationDto>>> GetEntityLocations(
        [FromRoute] string entityId,
        [FromQuery] string? caseId,
        CancellationToken cancellationToken)
    {
        var result = await _geospatialService.GetEntityLocationsAsync(entityId, caseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Reconstructs the chronological travel and activity sequence of an entity with geodesic distances and velocity checks.
    /// </summary>
    [HttpGet("entities/{entityId}/travel-sequence")]
    public async Task<ActionResult<TravelSequenceDto>> GetEntityTravelSequence(
        [FromRoute] string entityId,
        [FromQuery] string? caseId,
        CancellationToken cancellationToken)
    {
        var result = await _geospatialService.GetEntityTravelSequenceAsync(entityId, caseId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Performs an exact geodesic radius search from a given coordinate within a case.
    /// </summary>
    [HttpGet("cases/{caseId}/spatial-analysis/proximity")]
    public async Task<ActionResult<List<SpatialProximityResultDto>>> GetSpatialProximity(
        [FromRoute] string caseId,
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm = 10.0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _geospatialService.GetSpatialProximityAsync(caseId, latitude, longitude, radiusKm, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves the latest completed spatial analysis execution results for a case.
    /// </summary>
    [HttpGet("cases/{caseId}/spatial-analysis")]
    public async Task<ActionResult<SpatialAnalysisResultDto>> GetLatestSpatialAnalysis(
        [FromRoute] string caseId,
        CancellationToken cancellationToken)
    {
        var result = await _geospatialService.GetLatestSpatialAnalysisAsync(caseId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"No spatial analysis run found for case '{caseId}'." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Triggers an automated spatial intelligence run (spatial-temporal overlaps, clustering, implausible velocity checks).
    /// </summary>
    [HttpPost("cases/{caseId}/spatial-analysis/run")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR,ANALYST")]
    public async Task<ActionResult<SpatialAnalysisResultDto>> RunSpatialAnalysis(
        [FromRoute] string caseId,
        [FromBody] RunSpatialAnalysisRequestDto? request,
        CancellationToken cancellationToken)
    {
        var req = request ?? new RunSpatialAnalysisRequestDto();
        var executedBy = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "investigator";

        await _auditService.LogAsync(
            actorId: executedBy,
            actorName: executedBy,
            action: "SPATIAL_ANALYSIS_STARTED",
            resourceType: "Case",
            resourceId: caseId,
            details: $"Started spatial analysis run for case '{caseId}'.",
            metadataJson: null,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken: cancellationToken);

        var result = await _geospatialService.RunSpatialAnalysisAsync(caseId, req, executedBy, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves surfaced spatial signals (overlaps, clusters, velocity warnings) for investigator review.
    /// </summary>
    [HttpGet("cases/{caseId}/spatial-signals")]
    public async Task<ActionResult<List<SpatialSignalDto>>> GetSpatialSignals(
        [FromRoute] string caseId,
        [FromQuery] string? status,
        [FromQuery] string? signalType,
        CancellationToken cancellationToken)
    {
        var result = await _geospatialService.GetSpatialSignalsAsync(caseId, status, signalType, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Processes investigator review decision (CONFIRMED / DISMISSED) for a candidate spatial signal.
    /// Zero knowledge graph mutation guarantee: updates review metadata and records audit trail without altering graph edges.
    /// </summary>
    [HttpPost("cases/{caseId}/spatial-signals/{signalId}/review")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR")]
    public async Task<ActionResult<SpatialSignalDto>> ReviewSpatialSignal(
        [FromRoute] string caseId,
        [FromRoute] string signalId,
        [FromBody] ReviewSpatialSignalRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status) ||
            (request.Status != "CONFIRMED" && request.Status != "DISMISSED"))
        {
            return BadRequest(new { message = "Status must be either 'CONFIRMED' or 'DISMISSED'." });
        }

        var reviewer = User.Identity?.Name ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "investigator";

        try
        {
            var result = await _geospatialService.ReviewSpatialSignalAsync(caseId, signalId, request, reviewer, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
