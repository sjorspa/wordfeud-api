using Wordfeud.Api.Models;

namespace Wordfeud.Api.Models;

/// <summary>
/// Represents the current status of a game.
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// Game is waiting for players to join.
    /// </summary>
    Waiting,

    /// <summary>
    /// Game is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Game has ended.
    /// </summary>
    Finished
}

/// <summary>
/// Represents a Wordfeud game session.
/// </summary>
public class Game
{
    /// <summary>
    /// The unique identifier of the game.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The current status of the game.
    /// </summary>
    public GameStatus Status { get; set; } = GameStatus.Waiting;

    /// <summary>
    /// The players in this game.
    /// </summary>
    public List<Player> Players { get; set; } = new();

    /// <summary>
    /// The current player whose turn it is.
    /// </summary>
    public string? CurrentPlayerId { get; set; }

    /// <summary>
    /// The bag of remaining tiles.
    /// </summary>
    public List<Tile> TileBag { get; set; } = new();

    /// <summary>
    /// The board state.
    /// </summary>
    public Board Board { get; set; } = new Board();

    /// <summary>
    /// The list of all words formed in the game (for validation history).
    /// </summary>
    public List<string> FormedWords { get; set; } = new();

    /// <summary>
    /// The list of all moves made in the game.
    /// </summary>
    public List<MoveHistory> MoveHistory { get; set; } = new();

    /// <summary>
    /// The number of consecutive passes in the current turn.
    /// </summary>
    public int ConsecutivePasses { get; set; }

    /// <summary>
    /// The move number (incremented each turn).
    /// </summary>
    public int MoveNumber { get; set; }

    /// <summary>
    /// Gets the number of tiles remaining in the bag (for web compatibility).
    /// </summary>
    public int BagCount => TileBag.Count;

    /// <summary>
    /// Gets the board tiles as a list (for web compatibility).
    /// </summary>
    public List<BoardTileDto> BoardTiles => Board.Tiles;

    /// <summary>
    /// The timestamp when the game was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The timestamp when the game was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a player in a Wordfeud game.
/// </summary>
public class Player
{
    /// <summary>
    /// The unique identifier of the player.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The name of the player.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The tiles currently in the player's hand.
    /// </summary>
    public List<Tile> Hand { get; set; } = new();

    /// <summary>
    /// The player's current score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// The number of tiles the player has drawn so far.
    /// </summary>
    public int TilesDrawn { get; set; }
}
