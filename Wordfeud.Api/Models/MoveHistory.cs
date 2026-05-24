namespace Wordfeud.Api.Models;

/// <summary>
/// Represents a recorded move in a Wordfeud game.
/// </summary>
public class MoveHistory
{
    /// <summary>
    /// The sequential move number.
    /// </summary>
    public int MoveNumber { get; set; }

    /// <summary>
    /// The ID of the player who made the move.
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the player who made the move.
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// The type of action: 'place', 'pass', or 'swap'.
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// The word formed by the move (for tile placements).
    /// </summary>
    public string? Word { get; set; }

    /// <summary>
    /// The score earned from this move.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// The tiles placed or swapped (for tile placements/swaps).
    /// </summary>
    public List<MoveTileDto>? Tiles { get; set; }

    /// <summary>
    /// The timestamp when the move was made.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a tile involved in a move.
/// </summary>
public class MoveTileDto
{
    /// <summary>
    /// The letter of the tile.
    /// </summary>
    public string Letter { get; set; } = string.Empty;

    /// <summary>
    /// The row position on the board.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// The column position on the board.
    /// </summary>
    public int Column { get; set; }
}
