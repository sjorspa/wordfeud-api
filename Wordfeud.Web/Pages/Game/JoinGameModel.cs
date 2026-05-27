using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace Wordfeud.Web.Pages.Game;

/// <summary>
/// Page model for the join game page.
/// </summary>
public class JoinGameModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JoinGameModel> _logger;

    [BindProperty]
    public string? GameId { get; set; }

    [BindProperty]
    public string? PlayerName { get; set; }

    public string? ErrorMessage { get; set; }

    public JoinGameModel(IHttpClientFactory httpClientFactory, ILogger<JoinGameModel> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
        _logger = logger;
    }

    public async Task<IActionResult> OnPost()
    {
        if (string.IsNullOrWhiteSpace(GameId))
        {
            ModelState.AddModelError("GameId", "Game ID is required.");
            return Page();
        }

        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            ModelState.AddModelError("PlayerName", "Player name is required.");
            return Page();
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/games/{GameId}/join", new { playerName = PlayerName });

            if (response.IsSuccessStatusCode)
            {
                // Get the game data from the response and store player ID in session
                var gameData = await response.Content.ReadFromJsonAsync<GameDataViewModel>(System.Text.Json.JsonSerializerOptions.Default);
                if (gameData?.Players != null && gameData.Players.Count > 0)
                {
                    // Find the player that just joined (by name match)
                    var joinedPlayer = gameData.Players.FirstOrDefault(p => 
                        p.Name.Equals(PlayerName, StringComparison.OrdinalIgnoreCase)) 
                        ?? gameData.Players[0];
                    
                    HttpContext.Session.SetString("Wordfeud_PlayerId", joinedPlayer.Id);
                    HttpContext.Session.SetString("Wordfeud_PlayerName", PlayerName);
                }
                
                // Redirect to the game board
                return RedirectToPage("/Game/Board", new { GameId = GameId });
            }

            var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            var detail = problem?["detail"]?.ToString() ?? "Unknown error";
            ModelState.AddModelError("Error", detail);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game {GameId}", GameId);
            ModelState.AddModelError("Error", $"Failed to join game: {ex.Message}");
            return Page();
        }
    }
}
