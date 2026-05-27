using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;

namespace Wordfeud.Web.Pages.Game;

/// <summary>
/// Page model for the new game creation page.
/// </summary>
public class NewGameModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewGameModel> _logger;

    [BindProperty]
    public string? PlayerName { get; set; }

    public string? GameId { get; set; }
    public bool ShowWaiting { get; set; }
    public string? ErrorMessage { get; set; }

    public NewGameModel(IHttpClientFactory httpClientFactory, ILogger<NewGameModel> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
        _logger = logger;
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            ModelState.AddModelError("PlayerName", "Player name is required.");
            return Page();
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/games", new { playerName = PlayerName });

            if (response.IsSuccessStatusCode)
            {
                var game = await response.Content.ReadFromJsonAsync<GameDataViewModel>(System.Text.Json.JsonSerializerOptions.Default);
                GameId = game?.Id;
                
                // Store player ID in session for player 1
                if (game?.Players != null && game.Players.Count > 0)
                {
                    var firstPlayer = game.Players[0];
                    HttpContext.Session.SetString("Wordfeud_PlayerId", firstPlayer.Id);
                    HttpContext.Session.SetString("Wordfeud_PlayerName", PlayerName);
                }
                
                ShowWaiting = !string.IsNullOrEmpty(GameId);
                
                // Redirect to the game board
                return RedirectToPage("/Game/Board", new { GameId = GameId });
            }

            var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var detail = problem?["detail"]?.ToString() ?? "Unknown error";
            ModelState.AddModelError("Error", $"Failed to create game: {detail}");
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            ModelState.AddModelError("Error", $"Failed to create game: {ex.Message}");
            return Page();
        }
    }
}
