using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace Wordfeud.Web.Pages;

/// <summary>
/// Page model for the main game page.
/// Handles game creation, joining, and state management.
/// </summary>
public class IndexModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Gets or sets the current game state.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public GameViewModel? CurrentGame { get; set; }

    /// <summary>
    /// Gets or sets the player name for creating/joining.
    /// </summary>
    [BindProperty]
    public string? PlayerName { get; set; }

    /// <summary>
    /// Gets or sets the game ID.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? GameId { get; set; }

    /// <summary>
    /// Gets the API base URL.
    /// </summary>
    private string ApiBaseUrl => _configuration["ApiUrl"] ?? "http://localhost:8080" + "/api";

    public IndexModel(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        this._httpClient = httpClientFactory.CreateClient("Api");
        this._configuration = configuration;
    }

    /// <summary>
    /// Creates a new game with the given player name.
    /// </summary>
    public async Task<IActionResult> OnPostCreateGame()
    {
        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            ModelState.AddModelError("PlayerName", "Player name is required.");
            return Page();
        }

        var response = await _httpClient.PostAsJsonAsync(
            $"{ApiBaseUrl}/games",
            new { playerName = PlayerName });

        if (response.IsSuccessStatusCode)
        {
            var game = await response.Content.ReadFromJsonAsync<GameViewModel>();
            CurrentGame = game;
            GameId = game!.Id;
            return RedirectToPage("/Index", new { gameId = game.Id });
        }

        var error = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError("Error", $"Failed to create game: {error}");
        return Page();
    }

    /// <summary>
    /// Joins the game as a second player.
    /// </summary>
    public async Task<IActionResult> OnPostJoinGame()
    {
        if (string.IsNullOrWhiteSpace(PlayerName) || string.IsNullOrWhiteSpace(GameId))
        {
            ModelState.AddModelError("PlayerName", "Player name and game ID are required.");
            return Page();
        }

        var response = await _httpClient.PostAsJsonAsync(
            $"{ApiBaseUrl}/games/{GameId}/join",
            new { playerName = PlayerName });

        if (response.IsSuccessStatusCode)
        {
            var game = await response.Content.ReadFromJsonAsync<GameViewModel>();
            CurrentGame = game;
            return RedirectToPage("/Index", new { gameId = game!.Id });
        }

        var error = await response.Content.ReadAsStringAsync();
        ModelState.AddModelError("Error", $"Failed to join game: {error}");
        return Page();
    }

    /// <summary>
    /// Refreshes the current game state.
    /// </summary>
    public async Task<IActionResult> OnGetRefresh()
    {
        if (!string.IsNullOrWhiteSpace(GameId))
        {
            CurrentGame = await _httpClient.GetFromJsonAsync<GameViewModel>($"{ApiBaseUrl}/games/{GameId}");
        }
        return Page();
    }

    /// <summary>
    /// Passes the current turn.
    /// </summary>
    public async Task<IActionResult> OnPostPass()
    {
        if (string.IsNullOrWhiteSpace(GameId) || string.IsNullOrWhiteSpace(CurrentGame?.CurrentPlayerId))
        {
            return Page();
        }

        var response = await _httpClient.PostAsync(
            $"{ApiBaseUrl}/games/{GameId}/pass?playerId={CurrentGame.CurrentPlayerId}",
            null);

        if (response.IsSuccessStatusCode)
        {
            CurrentGame = await response.Content.ReadFromJsonAsync<GameViewModel>();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("Error", $"Failed to pass turn: {error}");
        }

        return RedirectToPage("/Index", new { gameId = GameId });
    }

    /// <summary>
    /// Swaps tiles from the player's hand.
    /// </summary>
    public async Task<IActionResult> OnPostSwap([FromForm] SwapTilesFormModel model)
    {
        if (string.IsNullOrWhiteSpace(GameId) || string.IsNullOrWhiteSpace(CurrentGame?.CurrentPlayerId))
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(model.SelectedTiles) || model.SelectedTiles.Split(',').Length == 0)
        {
            ModelState.AddModelError("SelectedTiles", "Select at least one tile to swap.");
            return Page();
        }

        var tileIds = model.SelectedTiles.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var response = await _httpClient.PostAsJsonAsync(
            $"{ApiBaseUrl}/games/{GameId}/swap?playerId={CurrentGame.CurrentPlayerId}",
            new { tileIds = tileIds });

        if (response.IsSuccessStatusCode)
        {
            CurrentGame = await response.Content.ReadFromJsonAsync<GameViewModel>();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("Error", $"Failed to swap tiles: {error}");
        }

        return RedirectToPage("/Index", new { gameId = GameId });
    }

    /// <summary>
    /// Places tiles on the board.
    /// </summary>
    public async Task<IActionResult> OnPostPlace([FromForm] PlaceTilesFormModel model)
    {
        if (string.IsNullOrWhiteSpace(GameId) || string.IsNullOrWhiteSpace(CurrentGame?.CurrentPlayerId))
        {
            return Page();
        }

        if (string.IsNullOrWhiteSpace(model.SelectedTiles))
        {
            ModelState.AddModelError("SelectedTiles", "Select at least one tile to place.");
            return Page();
        }

        var tileIds = model.SelectedTiles.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (tileIds.Length == 0 || tileIds.Length > 7)
        {
            ModelState.AddModelError("SelectedTiles", "Select between 1 and 7 tiles.");
            return Page();
        }

        var direction = model.Direction == "vertical" ? 1 : 0;
        var tiles = tileIds.Select(id => new
        {
            tileId = id,
            letter = "",
            isBlank = false,
            row = model.StartRow,
            column = model.StartColumn + (direction == 0 ? Array.IndexOf(tileIds, id) : 0)
        }).ToList();

        var request = new
        {
            tiles,
            startRow = model.StartRow,
            startColumn = model.StartColumn,
            direction,
            blankAssignments = model.BlankAssignments
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{ApiBaseUrl}/games/{GameId}/place?playerId={CurrentGame.CurrentPlayerId}",
            request);

        if (response.IsSuccessStatusCode)
        {
            CurrentGame = await response.Content.ReadFromJsonAsync<GameViewModel>();
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("Error", $"Failed to place tiles: {error}");
        }

        return RedirectToPage("/Index", new { gameId = GameId });
    }

    /// <summary>
    /// Gets the bonus type for a board position.
    /// </summary>
    public static string GetBonusType(int row, int col)
    {
        var bonusSquares = new (int Row, int Col, string Type)[]
        {
            // Triple word score (corners and center)
            (0, 0, "TWS"), (0, 7, "TWS"), (0, 14, "TWS"),
            (7, 0, "TWS"), (7, 7, "TWS"), (7, 14, "TWS"),
            (14, 0, "TWS"), (14, 7, "TWS"), (14, 14, "TWS"),
            // Double word score
            (1, 1, "DWS"), (2, 2, "DWS"), (3, 3, "DWS"), (4, 4, "DWS"),
            (1, 13, "DWS"), (2, 12, "DWS"), (3, 11, "DWS"), (4, 10, "DWS"),
            (13, 1, "DWS"), (12, 2, "DWS"), (11, 3, "DWS"), (10, 4, "DWS"),
            (13, 13, "DWS"), (12, 12, "DWS"), (11, 11, "DWS"), (10, 10, "DWS"),
            // Double letter score
            (1, 5, "DLS"), (1, 9, "DLS"),
            (5, 1, "DLS"), (5, 5, "DLS"), (5, 9, "DLS"), (5, 13, "DLS"),
            (9, 1, "DLS"), (9, 5, "DLS"), (9, 9, "DLS"), (9, 13, "DLS"),
            (13, 5, "DLS"), (13, 9, "DLS"),
            // Triple letter score
            (2, 6, "TLS"), (6, 2, "TLS"), (8, 6, "TLS"), (6, 8, "TLS"),
        };

        var bonus = bonusSquares.FirstOrDefault(b => b.Row == row && b.Col == col);
        return bonus.Type;
    }
}

/// <summary>
/// Model for the game state returned from the API.
/// </summary>
public class GameViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CurrentPlayerId { get; set; }
    public List<PlayerViewModel> Players { get; set; } = new();
    public List<BoardTileViewModel> BoardTiles { get; set; } = new();
    public int BagCount { get; set; }
    public int ConsecutivePasses { get; set; }
    public int MoveNumber { get; set; }
}

/// <summary>
/// Model for a player.
/// </summary>
public class PlayerViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<TileViewModel> Hand { get; set; } = new();
}

/// <summary>
/// Model for a tile.
/// </summary>
public class TileViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Letter { get; set; } = string.Empty;
    public string BlankRepresentation { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsBlank { get; set; }
}

/// <summary>
/// Model for a board tile.
/// </summary>
public class BoardTileViewModel
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string Letter { get; set; } = string.Empty;
    public bool IsBlank { get; set; }
    public string BlankRepresentation { get; set; } = string.Empty;
    public int Points { get; set; }
    public string BonusType { get; set; } = string.Empty;
}

/// <summary>
/// Form model for swapping tiles.
/// </summary>
public class SwapTilesFormModel
{
    [BindProperty]
    public string? SelectedTiles { get; set; }
}

/// <summary>
/// Form model for placing tiles.
/// </summary>
public class PlaceTilesFormModel
{
    [BindProperty]
    public string? SelectedTiles { get; set; }

    [BindProperty]
    public int StartRow { get; set; }

    [BindProperty]
    public int StartColumn { get; set; }

    [BindProperty]
    public string Direction { get; set; } = "horizontal";

    [BindProperty]
    public Dictionary<string, string>? BlankAssignments { get; set; }
}
