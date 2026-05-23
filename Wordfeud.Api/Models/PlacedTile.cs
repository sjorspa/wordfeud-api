namespace Wordfeud.Api.Models;

/// <summary>
/// Represents a placed tile on the board during a game move.
/// </summary>
public class PlacedTile
{
    /// <summary>
    /// The tile being placed.
    /// </summary>
    public Tile Tile { get; set; } = new();

    /// <summary>
    /// The row position on the board (0-14).
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// The column position on the board (0-14).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// The direction of placement (0 = horizontal, 1 = vertical).
    /// </summary>
    public int Direction { get; set; }
}
