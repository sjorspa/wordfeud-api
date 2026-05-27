using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace Wordfeud.Web.Pages;

/// <summary>
/// Page model for the home page.
/// </summary>
public class IndexModel : PageModel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IndexModel> _logger;

    public List<GameSummaryDto>? ActiveGames { get; set; }

    public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Api");
        _logger = logger;
    }

    public void OnGet()
    {
        // For the home page, we just show the welcome screen.
        // Active games would come from a real backend service with user accounts.
        ActiveGames = new List<GameSummaryDto>();
    }
}

/// <summary>
/// Summary DTO for a game on the home page.
/// </summary>
public class GameSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
}
