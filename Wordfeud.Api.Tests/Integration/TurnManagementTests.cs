using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.Tests.Integration;

/// <summary>
/// Integration tests for turn management via the <c>POST /api/games/{id}/pass</c> endpoint.
/// </summary>
public class TurnManagementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TurnManagementTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostPassTurn_ShouldReturn200WithNextPlayer()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var passRequest = new { PlayerId = currentGame!.CurrentPlayerId };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/pass", passRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var passedGame = await TestHelpers.ReadAsGameAsync(response);
        passedGame!.CurrentPlayerId.Should().NotBe(currentGame.CurrentPlayerId);
    }

    [Fact]
    public async Task PostPassTurn_ShouldReturn404WhenGameNotFound()
    {
        // Arrange
        var passRequest = new { PlayerId = "player-id" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/pass", passRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
