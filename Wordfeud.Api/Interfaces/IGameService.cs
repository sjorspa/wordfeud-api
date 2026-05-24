using Wordfeud.Api.Models;

namespace Wordfeud.Api.Interfaces;

/// <summary>
/// Service for managing Wordfeud games.
/// </summary>
public interface IGameService
{
    /// <summary>
    /// Creates a new game with the specified player.
    /// </summary>
    Task<Game> CreateGameAsync(string playerName);

    /// <summary>
    /// Adds a second player to an existing game.
    /// </summary>
    Task<Game> JoinGameAsync(string gameId, string playerName);

    /// <summary>
    /// Gets the current state of a game.
    /// </summary>
    Task<Game?> GetGameAsync(string gameId);

    /// <summary>
    /// Places tiles on the board and validates the move.
    /// </summary>
    Task<Game> PlaceTilesAsync(string gameId, string playerId, PlaceTilesRequest request);

    /// <summary>
    /// Gets the scores for all players in a game.
    /// </summary>
    Task<GameScoresDto> GetScoresAsync(string gameId);

    /// <summary>
    /// Gets the current board state.
    /// </summary>
    Task<BoardStateDto> GetBoardAsync(string gameId);

    /// <summary>
    /// Passes the current turn.
    /// </summary>
    Task<Game> PassTurnAsync(string gameId, string playerId);

    /// <summary>
    /// Swaps tiles from a player's hand back to the bag.
    /// </summary>
    Task<Game> SwapTilesAsync(string gameId, string playerId, SwapTilesRequest request);

    /// <summary>
    /// Gets the move history for a game.
    /// </summary>
    /// <param name="gameId">The game ID.</param>
    /// <returns>The move history as a list of MoveHistory records.</returns>
    Task<List<MoveHistory>> GetMoveHistoryAsync(string gameId);
}
