using Microsoft.AspNetCore.Mvc;

namespace Wordfeud.Api.Controllers;

/// <summary>
/// Provides health check endpoints for container orchestration and monitoring.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Performs a liveness probe check.
    /// Returns 200 OK if the application is running.
    /// </summary>
    /// <returns>A 200 OK response with a status message.</returns>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Liveness()
    {
        return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Performs a readiness probe check.
    /// Returns 200 OK if the application is ready to serve requests.
    /// Returns 503 Service Unavailable if the application is not ready.
    /// </summary>
    /// <returns>A 200 OK or 503 Service Unavailable response with a status message.</returns>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Readiness()
    {
        // In a production application, you might check database connectivity,
        // external service availability, etc. For this in-memory API,
        // the application is ready as long as it is running.
        return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }
}
