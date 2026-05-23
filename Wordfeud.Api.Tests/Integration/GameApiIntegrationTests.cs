using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Wordfeud.Api.Data;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.Tests.Integration;

/// <summary>
/// Integration tests for the Wordfeud API using <see cref="WebApplicationFactory{T}"/>.
/// </summary>
public class GameApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GameApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region CreateGame Tests

    [Fact]
    public async Task PostCreateGame_ShouldReturn201WithGame()
    {
        // Arrange
        var request = new { Name = "TestPlayer" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var game = await response.Content.ReadFromJsonAsync<Game>();
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
        var request1 = new { Name = "Player1" };
        var request2 = new { Name = "Player2" };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/games", request1);
        var response2 = await _client.PostAsJsonAsync("/api/games", request2);

        // Assert
        response1.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var game1 = await response1.Content.ReadFromJsonAsync<Game>();
        var game2 = await response2.Content.ReadFromJsonAsync<Game>();

        game1!.Id.Should().NotBe(game2!.Id);
    }

    [Fact]
    public async Task PostCreateGame_ShouldReturn400WhenNameMissing()
    {
        // Arrange
        var request = new { Name = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region JoinGame Tests

    [Fact]
    public async Task PostJoinGame_ShouldReturn200WithSecondPlayer()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();
        var joinRequest = new { Name = "Player2" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", joinRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var updatedGame = await response.Content.ReadFromJsonAsync<Game>();
        updatedGame!.Players.Should().HaveCount(2);
        updatedGame.Status.Should().Be(GameStatus.InProgress);
        updatedGame.CurrentPlayerId.Should().NotBeNull();
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn404WhenGameNotFound()
    {
        // Arrange
        var joinRequest = new { Name = "Player2" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/join", joinRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn400WhenGameFull()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();

        // Join once
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });

        // Try to join again
        var thirdJoinRequest = new { Name = "Player3" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/join", thirdJoinRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetGame Tests

    [Fact]
    public async Task GetGame_ShouldReturnGame()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();

        // Act
        var response = await _client.GetAsync($"/api/games/{game!.Id}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var retrievedGame = await response.Content.ReadFromJsonAsync<Game>();
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

    #endregion

    #region PlaceTiles Tests

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn200WithPlacedTiles()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await gameState.Content.ReadFromJsonAsync<Game>();

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
        var placedGame = await response.Content.ReadFromJsonAsync<Game>();
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
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();
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
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await gameState.Content.ReadFromJsonAsync<Game>();

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

    #endregion

    #region PassTurn Tests

    [Fact]
    public async Task PostPassTurn_ShouldReturn200WithNextPlayer()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await gameState.Content.ReadFromJsonAsync<Game>();

        var passRequest = new { PlayerId = currentGame!.CurrentPlayerId };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/pass", passRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var passedGame = await response.Content.ReadFromJsonAsync<Game>();
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

    #endregion

    #region SwapTiles Tests

    [Fact]
    public async Task PostSwapTiles_ShouldReturn200WithSwappedTiles()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { Name = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await gameState.Content.ReadFromJsonAsync<Game>();

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var swapRequest = new { TileIds = new[] { player.Hand[0].Id } };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn404WhenGameNotFound()
    {
        // Arrange
        var swapRequest = new { TileIds = new[] { "tile-id" } };

        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/swap", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    #endregion

    #region GetScores Tests

    [Fact]
    public async Task GetScores_ShouldReturn200WithScores()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();

        // Act
        var response = await _client.GetAsync($"/api/games/{game!.Id}/scores");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var scores = await response.Content.ReadFromJsonAsync<Game>();
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

    #endregion

    #region GetBoard Tests

    [Fact]
    public async Task GetBoard_ShouldReturn200WithBoard()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { Name = "Player1" });
        var game = await createResponse.Content.ReadFromJsonAsync<Game>();

        // Act
        var response = await _client.GetAsync($"/api/games/{game!.Id}/board");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var board = await response.Content.ReadFromJsonAsync<Game>();
        board.Should().NotBeNull();
        board!.Board.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBoard_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/games/nonexistent-id/board");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    #endregion
}
