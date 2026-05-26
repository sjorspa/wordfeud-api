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

/// <summary>
/// Response DTO for game scores.
/// </summary>
public class GameScoresDto
{
    /// <summary>
    /// Gets the game ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the game status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets the list of player scores.
    /// </summary>
    public List<PlayerScoreDto> Players { get; set; } = new();
}

/// <summary>
/// Response DTO for a player's score.
/// </summary>
public class PlayerScoreDto
{
    /// <summary>
    /// Gets the player ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the player name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the current score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Gets the number of tiles drawn.
    /// </summary>
    public int TilesDrawn { get; set; }
}

/// <summary>
/// Response DTO for board state.
/// </summary>
public class BoardStateDto
{
    /// <summary>
    /// Gets the game ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the board size.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets the tiles on the board as a list of placed tiles.
    /// </summary>
    public List<BoardTileDto> Tiles { get; set; } = new();
}

/// <summary>
/// Response DTO for a tile on the board.
/// </summary>
public class BoardTileDto
{
    /// <summary>
    /// Gets the row position.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// Gets the column position.
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets the letter of the tile.
    /// </summary>
    public string Letter { get; set; } = string.Empty;

    /// <summary>
    /// Gets whether this is a blank tile.
    /// </summary>
    public bool IsBlank { get; set; }

    /// <summary>
    /// Gets the point value of the tile.
    /// </summary>
    public int Points { get; set; }

    /// <summary>
    /// Gets the bonus type for this position.
    /// </summary>
    public string BonusType { get; set; } = string.Empty;
}
