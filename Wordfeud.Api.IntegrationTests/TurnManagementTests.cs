using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for turn management via the <c>POST /api/games/{id}/pass</c> endpoint.
/// </summary>
public class TurnManagementTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TurnManagementTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostPassTurn_ShouldReturn200WithNextPlayer()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/pass", new { playerId = currentGame!.CurrentPlayerId });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var passedGame = await TestHelpers.ReadAsGameAsync(response);
        passedGame!.CurrentPlayerId.Should().NotBe(currentGame.CurrentPlayerId);
    }

    [Fact]
    public async Task PostPassTurn_ShouldReturn404WhenGameNotFound()
    {
        // Arrange
        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/pass", new { playerId = "player-id" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
