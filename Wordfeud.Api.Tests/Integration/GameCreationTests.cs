using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.Tests.Integration;

/// <summary>
/// Integration tests for game creation via the <c>POST /api/games</c> endpoint.
/// </summary>
public class GameCreationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GameCreationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostCreateGame_ShouldReturn201WithGame()
    {
        // Arrange
        var request = new { PlayerName = "TestPlayer" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games", request);
        var rawJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"RAW JSON: {rawJson}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var game = await TestHelpers.ReadAsGameAsync(response);
        Console.WriteLine($"Game Players count: {game?.Players?.Count ?? 0}");
        game.Should().NotBeNull();
        game!.Players[0].Name.Should().Be("TestPlayer");
        game.Players.Should().HaveCount(1);
        game.Players[0].Hand.Should().HaveCount(7);
        game.Status.Should().Be(GameStatus.Waiting);
    }

    [Fact]
    public async Task PostCreateGame_ShouldReturn201WithDifferentGameIds()
    {
        // Arrange
        var request1 = new { PlayerName = "Player1" };
        var request2 = new { PlayerName = "Player2" };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/games", request1);
        var response2 = await _client.PostAsJsonAsync("/api/games", request2);

        // Assert
        response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var game1 = await TestHelpers.ReadAsGameAsync(response1);
        var game2 = await TestHelpers.ReadAsGameAsync(response2);

        game1!.Id.Should().NotBe(game2!.Id);
    }

    [Fact]
    public async Task PostCreateGame_ShouldReturn400WhenNameMissing()
    {
        // Arrange
        var request = new { PlayerName = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
