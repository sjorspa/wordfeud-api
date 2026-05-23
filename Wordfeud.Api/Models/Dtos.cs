namespace Wordfeud.Api.Models;

/// <summary>
/// Request DTO for creating a new game.
/// </summary>
public class CreateGameRequest
{
    /// <summary>
    /// The name of the first player.
    /// </summary>
    public required string PlayerName { get; set; }
}

/// <summary>
/// Request DTO for joining a game.
/// </summary>
public class JoinGameRequest
{
    /// <summary>
    /// The name of the player joining.
    /// </summary>
    public required string PlayerName { get; set; }
}

/// <summary>
/// Request DTO for placing tiles on the board.
/// </summary>
public class PlaceTilesRequest
{
    /// <summary>
    /// The tiles to place.
    /// </summary>
    public required List<TilePlacementDto> Tiles { get; set; }

    /// <summary>
    /// The starting row position.
    /// </summary>
    public required int StartRow { get; set; }

    /// <summary>
    /// The starting column position.
    /// </summary>
    public required int StartColumn { get; set; }

    /// <summary>
    /// The direction of placement: 0 = horizontal, 1 = vertical.
    /// </summary>
    public required int Direction { get; set; }

    /// <summary>
    /// Blank tile assignments (tile ID → letter representation).
    /// </summary>
    public Dictionary<string, string>? BlankAssignments { get; set; }
}

/// <summary>
/// Request DTO for swapping tiles.
/// </summary>
public class SwapTilesRequest
{
    /// <summary>
    /// The IDs of tiles to swap.
    /// </summary>
    public required List<string> TileIds { get; set; }
}

/// <summary>
/// Request DTO for passing a turn.
/// </summary>
public class PassTurnRequest
{
    // Intentionally empty — no body needed for pass
}

/// <summary>
/// DTO for a tile placement in the request.
/// </summary>
public class TilePlacementDto
{
    /// <summary>
    /// The letter of the tile.
    /// </summary>
    public required string Letter { get; set; }

    /// <summary>
    /// Whether this is a blank tile.
    /// </summary>
    public bool IsBlank { get; set; }

    /// <summary>
    /// The ID of the tile in the player's hand.
    /// </summary>
    public required string TileId { get; set; }

    /// <summary>
    /// The row position on the board.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// The column position on the board.
    /// </summary>
    public int Column { get; set; }
}
