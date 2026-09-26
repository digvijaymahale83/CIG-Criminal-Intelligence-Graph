using Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly ISystemHealthService _healthService;

    public HealthController(ISystemHealthService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>
    /// Liveness probe: returns 200 OK as long as the API server process is running.
    /// </summary>
    [HttpGet("health/live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLiveness()
    {
        return Ok(new { status = "Healthy", checkedAtUtc = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe: returns 200 OK when all infrastructure services are connected.
    /// Returns 503 Service Unavailable when any critical service is degraded.
    /// </summary>
    [HttpGet("health/ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
    {
        var isReady = await _healthService.IsReadyAsync(cancellationToken);
        if (!isReady)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Degraded", checkedAtUtc = DateTime.UtcNow });
        }

        return Ok(new { status = "Healthy", checkedAtUtc = DateTime.UtcNow });
    }

    /// <summary>
    /// General health endpoint alias for container orchestration.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> GetGeneralHealth(CancellationToken cancellationToken)
    {
        return GetReadiness(cancellationToken);
    }
}
