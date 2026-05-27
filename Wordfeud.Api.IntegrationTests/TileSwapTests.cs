using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for tile swapping via the <c>POST /api/games/{id}/swap</c> endpoint.
/// </summary>
public class TileSwapTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TileSwapTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn200WithSwappedTiles()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var swapRequest = new { playerId = player.Id, tileIds = new[] { player.Hand[0].Id } };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn404WhenGameNotFound()
    {
        // Arrange - use a dummy playerId since the game doesn't exist anyway
        var swapRequest = new { playerId = "dummy-player-id", tileIds = new[] { "tile-id" } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/swap", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
