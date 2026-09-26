using Application.Common.Interfaces;
using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/system")]
public class SystemController : ControllerBase
{
    private readonly ISystemHealthService _healthService;
    private readonly ILogger<SystemController> _logger;

    public SystemController(ISystemHealthService healthService, ILogger<SystemController> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Live health endpoint verifying actual PostgreSQL, Neo4j, and AI Service operational status.
    /// Returns 200 OK when all services are healthy, or 503 Service Unavailable when degraded.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(SystemHealthSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SystemHealthSummaryDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var summary = await _healthService.GetHealthSummaryAsync(cancellationToken);
        if (summary.Status == "degraded")
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, summary);
        }

        return Ok(summary);
    }

    /// <summary>
    /// Comprehensive system status endpoint consumed by the investigator UI dashboard and status monitors.
    /// Returns 200 when all systems are healthy, or 503 with degraded breakdown when any dependency fails.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetStatus([FromQuery] string? simulate, CancellationToken cancellationToken)
    {
        if (string.Equals(simulate, "malformed", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { corruptedField = true, timestamp = 999999 });
        }

        var status = await _healthService.GetSystemStatusAsync(simulate, cancellationToken);

        if (!status.IsReady || status.Status == "degraded")
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, status);
        }

        return Ok(status);
    }

    /// <summary>
    /// Diagnostics simulation toggle allowing investigators and testers to verify UI degradation states.
    /// </summary>
    [HttpPost("status/simulate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SetSimulation([FromBody] SimulationModeRequest request)
    {
        _healthService.SetSimulationMode(request.Mode);
        return Ok(new { mode = request.Mode, message = $"Simulation mode set to '{request.Mode}'." });
    }
}
