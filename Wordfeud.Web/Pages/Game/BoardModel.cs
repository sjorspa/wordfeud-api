using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wordfeud.Web.Pages.Game;

/// <summary>
/// JsonSerializerOptions configured for camelCase matching against the API response.
/// </summary>
public static class ApiJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Page model for the game board page.
/// </summary>
public class BoardModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BoardModel> _logger;

    [FromRoute]
    public required string GameId { get; set; }

    public GameDataViewModel? Game { get; set; }
    public string? CurrentPlayerId { get; set; }
    public List<MoveHistoryItem>? MoveHistory { get; set; }
    public string? ErrorMessage { get; set; }

    public BoardModel(IHttpClientFactory httpClientFactory, ILogger<BoardModel> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
        _logger = logger;
    }

    public async Task OnGet()
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/games/{GameId}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                Game = JsonSerializer.Deserialize<GameDataViewModel>(json, ApiJsonOptions.Default);
                CurrentPlayerId = GetCurrentPlayerId();
                MoveHistory = Game?.MoveHistory ?? new List<MoveHistoryItem>();
            }
            else
            {
                ErrorMessage = $"Failed to load game: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game {GameId}", GameId);
            ErrorMessage = "Failed to load game. Please try again.";
        }
    }

    /// <summary>
    /// Gets the current player ID from the session (the logged-in player).
    /// Falls back to the first player if not set in session.
    /// </summary>
    public string GetCurrentPlayerId()
    {
        // First, try to get the logged-in player ID from session
        var sessionId = HttpContext?.Session?.GetString("Wordfeud_PlayerId");
        if (!string.IsNullOrEmpty(sessionId))
            return sessionId;

        // Fallback: use the current turn player (for player 1 who created the game)
        if (Game?.CurrentPlayerId != null)
            return Game.CurrentPlayerId;

        if (Game?.Players.Count > 0)
            return Game.Players[0].Id;

        return string.Empty;
    }
}

/// <summary>
/// ViewModel for game data returned from the API.
/// </summary>
public class GameDataViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("currentPlayerId")]
    public string? CurrentPlayerId { get; set; }

    [JsonPropertyName("players")]
    public List<PlayerDataViewModel> Players { get; set; } = new();

    [JsonPropertyName("board")]
    public List<List<TileDataViewModel?>>? Board { get; set; }

    [JsonPropertyName("bagCount")]
    public int BagCount { get; set; }

    [JsonPropertyName("consecutivePasses")]
    public int ConsecutivePasses { get; set; }

    [JsonPropertyName("moveNumber")]
    public int MoveNumber { get; set; }

    [JsonPropertyName("moveHistory")]
    public List<MoveHistoryItem>? MoveHistory { get; set; }
}

/// <summary>
/// ViewModel for player data.
/// </summary>
public class PlayerDataViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("hand")]
    public List<TileDataViewModel> Hand { get; set; } = new();
}

/// <summary>
/// ViewModel for tile data.
/// </summary>
public class TileDataViewModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("letter")]
    public string Letter { get; set; } = string.Empty;

    [JsonPropertyName("blankRepresentation")]
    public string? BlankRepresentation { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("isBlank")]
    public bool IsBlank { get; set; }
}

/// <summary>
/// Model for move history - matches the API's MoveHistory DTO.
/// </summary>
public class MoveHistoryItem
{
    [JsonPropertyName("moveNumber")]
    public int MoveNumber { get; set; }

    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    [JsonPropertyName("playerName")]
    public string PlayerName { get; set; } = string.Empty;

    [JsonPropertyName("actionType")]
    public string ActionType { get; set; } = string.Empty;

    [JsonPropertyName("word")]
    public string? Word { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("tiles")]
    public List<MoveTileDto>? Tiles { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Model for a tile in a move history entry.
/// </summary>
public class MoveTileDto
{
    [JsonPropertyName("tileId")]
    public string TileId { get; set; } = string.Empty;

    [JsonPropertyName("letter")]
    public string Letter { get; set; } = string.Empty;

    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("column")]
    public int Column { get; set; }
}
