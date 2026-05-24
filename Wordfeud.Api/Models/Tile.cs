namespace Wordfeud.Api.Models;

/// <summary>
/// Represents a tile on the Wordfeud board.
/// </summary>
public class Tile
{
    /// <summary>
    /// The letter represented by this tile (empty string for blank tiles).
    /// </summary>
    public string Letter { get; set; } = string.Empty;

    /// <summary>
    /// The point value of this tile.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Whether this tile is a blank tile.
    /// </summary>
    public bool IsBlank { get; set; }

    /// <summary>
    /// The letter that a blank tile represents (once assigned).
    /// </summary>
    public string? BlankRepresentation { get; set; }

    /// <summary>
    /// The unique identifier of this tile instance.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
}
