using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using Wordfeud.Api.Serialization;
using Xunit;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for querying game state via the <c>GET /api/games/{id}</c>, <c>GET /api/games/{id}/scores</c>, and <c>GET /api/games/{id}/board</c> endpoints.
/// </summary>
public class GameQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GameQueryTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGame_ShouldReturnGame()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);

        // Debug: print IDs
        Console.WriteLine($"Created game ID: {game?.Id}");

        // Act
        var response = await _client.GetAsync($"/api/games/{game!.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var retrievedGame = await TestHelpers.ReadAsGameAsync(response);
        retrievedGame.Should().NotBeNull();
        retrievedGame!.Id.Should().Be(game.Id);
        retrievedGame.Players[0].Name.Should().Be("Player1");
    }

    [Fact]
    public async Task GetGame_ShouldReturn404WhenNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/games/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScores_ShouldReturn200WithScores()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);

        // Act
        var response = await _client.GetAsync($"/api/games/{game!.Id}/scores");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var scores = JsonSerializer.Deserialize<GameScoresDto>(json, new JsonSerializerOptions
        {
            Converters = { new BoardConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        scores.Should().NotBeNull();
        scores!.Players.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetScores_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/games/nonexistent-id/scores");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBoard_ShouldReturn200WithBoard()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);

        // Act
        var response = await _client.GetAsync($"/api/games/{game!.Id}/board");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var board = JsonSerializer.Deserialize<BoardStateDto>(json, new JsonSerializerOptions
        {
            Converters = { new BoardConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        board.Should().NotBeNull();
        board!.Tiles.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBoard_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/games/nonexistent-id/board");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }
}
