using System.ComponentModel.DataAnnotations;

namespace Wordfeud.Api.Models;

/// <summary>
/// Request DTO for creating a new game.
/// </summary>
public class CreateGameRequest
{
    /// <summary>
    /// The name of the first player.
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Player name must not exceed 50 characters.")]
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
    [Required]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Player name must not exceed 50 characters.")]
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
    [Required]
    [MinLength(1, ErrorMessage = "At least one tile must be placed.")]
    [MaxLength(7, ErrorMessage = "Cannot place more than 7 tiles at once.")]
    public required List<TilePlacementDto> Tiles { get; set; }

    /// <summary>
    /// The starting row position.
    /// </summary>
    [Range(0, 14, ErrorMessage = "StartRow must be between 0 and 14.")]
    public required int StartRow { get; set; }

    /// <summary>
    /// The starting column position.
    /// </summary>
    [Range(0, 14, ErrorMessage = "StartColumn must be between 0 and 14.")]
    public required int StartColumn { get; set; }

    /// <summary>
    /// The direction of placement: 0 = horizontal, 1 = vertical.
    /// </summary>
    [Required]
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
    [Required]
    [MinLength(1, ErrorMessage = "At least one tile must be swapped.")]
    [MaxLength(7, ErrorMessage = "Cannot swap more than 7 tiles at once.")]
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
