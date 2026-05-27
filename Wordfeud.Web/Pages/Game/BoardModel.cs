using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wordfeud.Web.Pages.Game;

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
    public List<TileDataViewModel> CurrentPlayerHand => Game?.Players
        .FirstOrDefault(p => p.Id == GetCurrentPlayerIdInternal())?.Hand ?? new List<TileDataViewModel>();

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
                Game = await response.Content.ReadFromJsonAsync<GameDataViewModel>();
                CurrentPlayerId = GetCurrentPlayerIdInternal();
                MoveHistory = Game?.Moves ?? new List<MoveHistoryItem>();
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

    private string GetCurrentPlayerIdInternal()
    {
        // Get from localStorage or session
        return "local-player";
    }

    /// <summary>
    /// Helper to get current player ID for CurrentPlayerHand.
    /// </summary>
    public string GetCurrentPlayerId()
    {
        return GetCurrentPlayerIdInternal();
    }
}

/// <summary>
/// ViewModel for game data returned from the API.
/// </summary>
public class GameDataViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CurrentPlayerId { get; set; }
    public List<PlayerDataViewModel> Players { get; set; } = new();
    public List<List<TileDataViewModel?>>? Board { get; set; }
    public int BagCount { get; set; }
    public int ConsecutivePasses { get; set; }
    public int MoveNumber { get; set; }
    public List<MoveHistoryItem>? Moves { get; set; }
}

/// <summary>
/// ViewModel for player data.
/// </summary>
public class PlayerDataViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public List<TileDataViewModel> Hand { get; set; } = new();
}

/// <summary>
/// ViewModel for tile data.
/// </summary>
public class TileDataViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Letter { get; set; } = string.Empty;
    public string BlankRepresentation { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool IsBlank { get; set; }
}

/// <summary>
/// Model for move history.
/// </summary>
public class MoveHistoryItem
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public bool IsHorizontal { get; set; }
}
