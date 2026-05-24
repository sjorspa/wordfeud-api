using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for joining a game via the <c>POST /api/games/{id}/join</c> endpoint.
/// </summary>
public class GameJoinTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GameJoinTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn200WithSecondPlayer()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        var joinRequest = new { PlayerName = "Player2" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", joinRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var updatedGame = await TestHelpers.ReadAsGameAsync(response);
        updatedGame!.Players.Should().HaveCount(2);
        updatedGame.Status.Should().Be(GameStatus.InProgress);
        updatedGame.CurrentPlayerId.Should().NotBeNull();
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn404WhenGameNotFound()
    {
        // Arrange
        var joinRequest = new { PlayerName = "Player2" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/join", joinRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn400WhenGameFull()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);

        // Join once
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        // Try to join again
        var thirdJoinRequest = new { PlayerName = "Player3" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/join", thirdJoinRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
