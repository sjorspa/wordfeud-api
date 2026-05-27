using Microsoft.AspNetCore.Mvc;
using Wordfeud.Api.Interfaces;

namespace Wordfeud.Api.Controllers;

/// <summary>
/// Provides health check endpoints for container orchestration and monitoring.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IDutchDictionaryService _dictionaryService;

    public HealthController(IDutchDictionaryService dictionaryService)
    {
        _dictionaryService = dictionaryService;
    }

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

    /// <summary>
    /// Returns the number of words loaded in the Dutch dictionary.
    /// </summary>
    /// <returns>A 200 OK response with the word count.</returns>
    [HttpGet("wordcount")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult WordCount()
    {
        return Ok(new { wordCount = _dictionaryService.WordCount, isInitialized = _dictionaryService.IsInitialized });
    }

    /// <summary>
    /// Returns a paginated list of words from the Dutch dictionary.
    /// </summary>
    /// <param name="skip">Number of words to skip (default 0).</param>
    /// <param name="take">Number of words to take (max 1000, default 100).</param>
    /// <returns>A 200 OK response with a list of words.</returns>
    [HttpGet("words")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetWords([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        if (take < 1 || take > 1000)
            return BadRequest(new { error = "Take must be between 1 and 1000." });

        if (skip < 0)
            return BadRequest(new { error = "Skip must be non-negative." });

        var words = _dictionaryService.GetWords(skip, take).ToList();
        return Ok(new { skip, take, count = words.Count, words });
    }
}
