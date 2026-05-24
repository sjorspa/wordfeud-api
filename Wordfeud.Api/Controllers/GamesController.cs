using Microsoft.AspNetCore.Mvc;
using Wordfeud.Api.Interfaces;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Controllers;

/// <summary>
/// REST API controller for Wordfeud game management.
/// </summary>
[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ILogger<GamesController> _logger;

    /// <summary>
    /// Creates a new GamesController.
    /// </summary>
    public GamesController(
        IGameService gameService,
        ILogger<GamesController> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new game with the specified player.
    /// </summary>
    /// <param name="request">The player information for the first player.</param>
    /// <returns>The newly created game.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Game), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request)
    {
        _logger.LogInformation("Creating new game with player '{PlayerName}'", request.PlayerName);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "One or more validation failures occurred.",
                Extensions = { ["errors"] = GetValidationErrors() }
            });
        }

        try
        {
            var game = await _gameService.CreateGameAsync(request.PlayerName);
            _logger.LogInformation("Game {GameId} created successfully", game.Id);
            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Adds a second player to an existing game.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <param name="request">The player information for the joining player.</param>
    /// <returns>The updated game state.</returns>
    [HttpPost("{id}/join")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> JoinGame(string id, [FromBody] JoinGameRequest request)
    {
        _logger.LogInformation("Player '{PlayerName}' joining game {GameId}", request.PlayerName, id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "One or more validation failures occurred.",
                Extensions = { ["errors"] = GetValidationErrors() }
            });
        }

        try
        {
            var game = await _gameService.JoinGameAsync(id, request.PlayerName);
            _logger.LogInformation("Player '{PlayerName}' joined game {GameId}", request.PlayerName, id);
            return Ok(game);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error joining game {GameId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the current state of a game.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <returns>The current game state.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGame(string id)
    {
        _logger.LogInformation("Getting game state for game {GameId}", id);

        var game = await _gameService.GetGameAsync(id);
        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }

        return Ok(game);
    }

    /// <summary>
    /// Places tiles on the board.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <param name="playerId">The ID of the player placing tiles.</param>
    /// <param name="request">The tile placement details.</param>
    /// <returns>The updated game state.</returns>
    [HttpPost("{id}/place")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PlaceTiles(string id, [FromQuery] string playerId, [FromBody] PlaceTilesRequest request)
    {
        _logger.LogInformation("Player {PlayerId} placing tiles in game {GameId}", playerId, id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "One or more validation failures occurred.",
                Extensions = { ["errors"] = GetValidationErrors() }
            });
        }

        try
        {
            var game = await _gameService.PlaceTilesAsync(id, playerId, request);
            _logger.LogInformation("Tiles placed successfully in game {GameId}", id);
            return Ok(game);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized tile placement in game {GameId}", id);
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in game {GameId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid tile placement in game {GameId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the scores for all players in a game.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <returns>The game state with scores.</returns>
    [HttpGet("{id}/scores")]
    [ProducesResponseType(typeof(GameScoresDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScores(string id)
    {
        _logger.LogInformation("Getting scores for game {GameId}", id);

        try
        {
            var scores = await _gameService.GetScoresAsync(id);
            return Ok(scores);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }
    }

    /// <summary>
    /// Gets the board state.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <returns>The board state.</returns>
    [HttpGet("{id}/board")]
    [ProducesResponseType(typeof(BoardStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoard(string id)
    {
        _logger.LogInformation("Getting board state for game {GameId}", id);

        try
        {
            var board = await _gameService.GetBoardAsync(id);
            return Ok(board);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }
    }

    /// <summary>
    /// Passes the current turn.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <param name="playerId">The ID of the player passing.</param>
    /// <returns>The updated game state.</returns>
    [HttpPost("{id}/pass")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PassTurn(string id, [FromQuery] string playerId)
    {
        _logger.LogInformation("Player {PlayerId} passing turn in game {GameId}", playerId, id);

        try
        {
            var game = await _gameService.PassTurnAsync(id, playerId);
            _logger.LogInformation("Turn passed in game {GameId}", id);
            return Ok(game);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized pass in game {GameId}", id);
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in game {GameId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Swaps tiles from a player's hand back to the bag.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <param name="playerId">The ID of the player swapping tiles.</param>
    /// <param name="request">The tiles to swap.</param>
    /// <returns>The updated game state.</returns>
    [HttpPost("{id}/swap")]
    [ProducesResponseType(typeof(Game), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SwapTiles(string id, [FromQuery] string playerId, [FromBody] SwapTilesRequest request)
    {
        _logger.LogInformation("Player {PlayerId} swapping tiles in game {GameId}", playerId, id);

        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = "One or more validation failures occurred.",
                Extensions = { ["errors"] = GetValidationErrors() }
            });
        }

        try
        {
            var game = await _gameService.SwapTilesAsync(id, playerId, request);
            _logger.LogInformation("Tiles swapped in game {GameId}", id);
            return Ok(game);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized swap in game {GameId}", id);
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation in game {GameId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Error",
                Detail = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid swap in game {GameId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the move history for a game.
    /// </summary>
    /// <param name="id">The game ID.</param>
    /// <returns>The move history.</returns>
    [HttpGet("{id}/moves")]
    [ProducesResponseType(typeof(List<MoveHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMoveHistory(string id)
    {
        _logger.LogInformation("Getting move history for game {GameId}", id);

        try
        {
            var moveHistory = await _gameService.GetMoveHistoryAsync(id);
            return Ok(moveHistory);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Game {GameId} not found", id);
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Game '{id}' not found."
            });
        }
    }

    /// <summary>
    /// Extracts validation errors from ModelState.
    /// </summary>
    private Dictionary<string, string[]> GetValidationErrors()
    {
        return ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
    }
}
