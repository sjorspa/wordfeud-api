using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.Tests.Integration;

/// <summary>
/// Integration tests for placing tiles via the <c>POST /api/games/{id}/place</c> endpoint.
/// </summary>
public class TilePlacementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TilePlacementTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn200WithPlacedTiles()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var firstTile = player.Hand[0];

        var placeRequest = new
        {
            Tiles = new[]
            {
                new
                {
                    Letter = firstTile.Letter,
                    IsBlank = firstTile.IsBlank,
                    TileId = firstTile.Id,
                    Row = 7,
                    Column = 7
                }
            },
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var placedGame = await TestHelpers.ReadAsGameAsync(response);
        placedGame!.Board[7, 7].Should().NotBeNull();
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenGameNotFound()
    {
        // Arrange
        var placeRequest = new
        {
            Tiles = new object[0],
            StartRow = 0,
            StartColumn = 0,
            Direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/place", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenTilesNotInHand()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });

        var placeRequest = new
        {
            Tiles = new[]
            {
                new
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = Guid.NewGuid().ToString(),
                    Row = 7,
                    Column = 7
                }
            },
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenBlankWithoutLetter()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var blankTile = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId).Hand.First(t => t.IsBlank);

        var placeRequest = new
        {
            Tiles = new[]
            {
                new
                {
                    Letter = string.Empty,
                    IsBlank = true,
                    TileId = blankTile.Id,
                    Row = 7,
                    Column = 8
                }
            },
            StartRow = 7,
            StartColumn = 8,
            Direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }
}
