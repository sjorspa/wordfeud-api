using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

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
    /// Gets or sets the error message when API call fails.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ErrorMessage { get; set; }

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
    private string ApiBaseUrl => (_configuration["ApiUrl"] ?? "http://localhost:8080") + "/api";

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
    /// Loads the game state on page load if a GameId is provided.
    /// </summary>
    public async Task OnGet()
    {
        if (!string.IsNullOrWhiteSpace(GameId))
        {
            try
            {
                CurrentGame = await _httpClient.GetFromJsonAsync<GameViewModel>($"{ApiBaseUrl}/games/{GameId}");
                if (CurrentGame == null)
                {
                    ErrorMessage = "Failed to load game state: API returned no data.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load game state: {ex.Message}";
            }
        }
    }

    /// <summary>
    /// Refreshes the current game state.
    /// </summary>
    public async Task<IActionResult> OnGetRefresh()
    {
        if (!string.IsNullOrWhiteSpace(GameId))
        {
            try
            {
                CurrentGame = await _httpClient.GetFromJsonAsync<GameViewModel>($"{ApiBaseUrl}/games/{GameId}");
                if (CurrentGame == null)
                {
                    ErrorMessage = "Failed to refresh game state: API returned no data.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to refresh game state: {ex.Message}";
            }
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
    /// Places tiles on the board via drag-and-drop.
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

        // Parse TilePositions JSON: [{"tileId":"...","row":0,"column":0},...]
        var positions = new List<(string TileId, int Row, int Column)>();
        if (!string.IsNullOrWhiteSpace(model.TilePositions))
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<(string TileId, int Row, int Column)>>(model.TilePositions);
            if (parsed != null)
            {
                positions = parsed;
            }
        }

        // Build the request with each tile having its own row/column
        var tiles = positions.Count > 0
            ? positions.Select(p => new
            {
                tileId = p.TileId,
                letter = "",
                isBlank = false,
                row = p.Row,
                column = p.Column
            }).ToList()
            : tileIds.Select((id, index) => new
            {
                tileId = id,
                letter = "",
                isBlank = false,
                row = 7,
                column = 7 + index
            }).ToList();

        var request = new
        {
            tiles,
            blankAssignments = new Dictionary<string, string>()
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
/// The API serializes the board as a 2D array under the key "board",
/// not as a flat list of BoardTileViewModel objects.
/// Properties use [JsonPropertyName] to match the API's camelCase JSON.
/// </summary>
public class GameViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the game.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the game.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the current player whose turn it is.
    /// </summary>
    [JsonPropertyName("currentPlayerId")]
    public string? CurrentPlayerId { get; set; }

    /// <summary>
    /// Gets or sets the players in this game.
    /// </summary>
    [JsonPropertyName("players")]
    public List<PlayerViewModel> Players { get; set; } = new();

    /// <summary>
    /// Gets or sets the board as a 2D array (15x15) from the API.
    /// The API serializes the Board using BoardConverter as a 2D array under the key "board".
    /// </summary>
    [JsonPropertyName("board")]
    public List<List<TileViewModel?>>? Board { get; set; }

    /// <summary>
    /// Gets the board tiles as a flat list, converted from the 2D board array.
    /// This provides a convenient interface for the Razor template to access placed tiles.
    /// </summary>
    [JsonIgnore]
    public List<BoardTileViewModel> BoardTiles
    {
        get
        {
            if (Board == null)
            {
                return new List<BoardTileViewModel>();
            }

            var result = new List<BoardTileViewModel>();
            for (var row = 0; row < Board.Count; row++)
            {
                var rowTiles = Board[row];
                if (rowTiles == null)
                {
                    continue;
                }

                for (var col = 0; col < rowTiles.Count; col++)
                {
                    var tile = rowTiles[col];
                    if (tile != null)
                    {
                        result.Add(new BoardTileViewModel
                        {
                            Row = row,
                            Column = col,
                            Letter = tile.Letter,
                            IsBlank = tile.IsBlank,
                            Points = tile.Points,
                            BonusType = string.Empty
                        });
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Gets or sets the number of tiles remaining in the bag.
    /// </summary>
    [JsonPropertyName("bagCount")]
    public int BagCount { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive passes.
    /// </summary>
    [JsonPropertyName("consecutivePasses")]
    public int ConsecutivePasses { get; set; }

    /// <summary>
    /// Gets or sets the current move number.
    /// </summary>
    [JsonPropertyName("moveNumber")]
    public int MoveNumber { get; set; }
}

/// <summary>
/// Model for a player.
/// Properties use [JsonPropertyName] to match the API's camelCase JSON.
/// </summary>
public class PlayerViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("hand")]
    public List<TileViewModel> Hand { get; set; } = new();
}

/// <summary>
/// Model for a tile.
/// Properties use [JsonPropertyName] to match the API's camelCase JSON.
/// </summary>
public class TileViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("letter")]
    public string Letter { get; set; } = string.Empty;

    [JsonPropertyName("blankRepresentation")]
    public string BlankRepresentation { get; set; } = string.Empty;

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("isBlank")]
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
/// Form model for placing tiles (drag-and-drop).
/// </summary>
public class PlaceTilesFormModel
{
    [BindProperty]
    public string? SelectedTiles { get; set; }

    [BindProperty]
    public string? TilePositions { get; set; }
}
