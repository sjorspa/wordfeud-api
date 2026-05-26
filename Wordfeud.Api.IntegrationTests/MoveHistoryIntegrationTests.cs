using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for the move history feature (GET /api/games/{id}/moves).
/// </summary>
public class MoveHistoryIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MoveHistoryIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMoveHistory_WhenGameNotFound_ShouldReturnNotFound()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/games/nonexistent/moves");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMoveHistory_WhenGameCreated_ShouldReturnEmptyList()
    {
        // Arrange: Create a new game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        // Act
        var response = await _client.GetAsync($"/api/games/{game.Id}/moves");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var moveHistory = await response.Content.ReadFromJsonAsync<List<MoveHistory>>();
        moveHistory.Should().NotBeNull();
        moveHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMoveHistory_AfterPlaceTiles_ShouldRecordMove()
    {
        // Arrange: Create game, join, and make a tile placement
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        var joinResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/join", new { PlayerName = "Player2" });
        joinResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame.Should().NotBeNull();
        currentGame!.CurrentPlayerId.Should().NotBeNullOrEmpty();

        var currentPlayer = currentGame.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var tile = currentPlayer.Hand[0];

        var placeRequest = new
        {
            tiles = new[]
            {
                new
                {
                    letter = tile.Letter,
                    isBlank = tile.IsBlank,
                    tileId = tile.Id,
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentGame.CurrentPlayerId}", placeRequest);

        // Act
        var response = await _client.GetAsync($"/api/games/{game.Id}/moves");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var moveHistory = await response.Content.ReadFromJsonAsync<List<MoveHistory>>();
        moveHistory.Should().NotBeNull();
        moveHistory.Should().HaveCount(1);

        var move = moveHistory![0];
        move.ActionType.Should().Be("place");
        move.Score.Should().BeGreaterThanOrEqualTo(0);
        move.Tiles.Should().NotBeNull();
        move.Tiles.Should().HaveCount(1);
        move.Tiles![0].Row.Should().Be(7);
        move.Tiles[0].Column.Should().Be(7);
        move.PlayerId.Should().Be(currentPlayer.Id);
    }

    [Fact]
    public async Task GetMoveHistory_AfterPassTurn_ShouldRecordPassMove()
    {
        // Arrange: Create game, join, and make a pass
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        var joinResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/join", new { PlayerName = "Player2" });
        joinResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame.Should().NotBeNull();

        // Act: Pass turn
        await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentGame.CurrentPlayerId}", null);

        // Assert
        var response = await _client.GetAsync($"/api/games/{game.Id}/moves");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var moveHistory = await response.Content.ReadFromJsonAsync<List<MoveHistory>>();
        moveHistory.Should().NotBeNull();
        moveHistory.Should().HaveCount(1);

        var move = moveHistory![0];
        move.ActionType.Should().Be("pass");
        move.Score.Should().Be(0);
        move.PlayerId.Should().Be(currentGame.CurrentPlayerId);
    }

    [Fact]
    public async Task GetMoveHistory_AfterSwapTiles_ShouldRecordSwapMove()
    {
        // Arrange: Create game, join, and get a player with tiles to swap
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/games/{game.Id}/join", new { PlayerName = "Player2" });

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame.Should().NotBeNull();
        currentGame!.CurrentPlayerId.Should().NotBeNullOrEmpty();

        var currentPlayer = currentGame.Players.First(p => p.Id == currentGame.CurrentPlayerId);

        // Act: Swap 2 tiles
        var swapTileIds = currentPlayer.Hand.Take(2).Select(t => t.Id).ToArray();
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={currentGame.CurrentPlayerId}", new { tileIds = swapTileIds });

        // Assert
        var response = await _client.GetAsync($"/api/games/{game.Id}/moves");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var moveHistory = await response.Content.ReadFromJsonAsync<List<MoveHistory>>();
        moveHistory.Should().NotBeNull();
        moveHistory.Should().HaveCount(1);

        var move = moveHistory![0];
        move.ActionType.Should().Be("swap");
        move.Score.Should().Be(0);
        move.Tiles.Should().NotBeNull();
        move.Tiles.Should().HaveCount(2);
        move.PlayerId.Should().Be(currentPlayer.Id);
    }

    [Fact]
    public async Task GetMoveHistory_AfterMultipleMoves_ShouldRecordAllMovesInOrder()
    {
        // Arrange: Create game, join, and make multiple moves
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        createResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/games/{game.Id}/join", new { PlayerName = "Player2" });

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame.Should().NotBeNull();

        // Move 1: Place a tile
        var currentPlayer = currentGame.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var tile = currentPlayer.Hand[0];
        var placeRequest = new
        {
            tiles = new[]
            {
                new
                {
                    letter = tile.Letter,
                    isBlank = tile.IsBlank,
                    tileId = tile.Id,
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentGame.CurrentPlayerId}", placeRequest);

        // Move 2: Pass turn
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentGame.CurrentPlayerId}", null);

        // Move 3: Place another tile
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentPlayer = currentGame.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        tile = currentPlayer.Hand[0];
        placeRequest = new
        {
            tiles = new[]
            {
                new
                {
                    letter = tile.Letter,
                    isBlank = tile.IsBlank,
                    tileId = tile.Id,
                    row = 7,
                    column = 8
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 1
        };
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentGame.CurrentPlayerId}", placeRequest);

        // Act
        var response = await _client.GetAsync($"/api/games/{game.Id}/moves");
        var moveHistory = await response.Content.ReadFromJsonAsync<List<MoveHistory>>();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        moveHistory.Should().NotBeNull();
        moveHistory.Should().HaveCount(3);

        // Verify move ordering
        moveHistory![0].MoveNumber.Should().Be(1);
        moveHistory[0].ActionType.Should().Be("place");

        moveHistory[1].MoveNumber.Should().Be(2);
        moveHistory[1].ActionType.Should().Be("pass");

        moveHistory[2].MoveNumber.Should().Be(3);
        moveHistory[2].ActionType.Should().Be("place");

        // Verify timestamps are in order
        for (var i = 1; i < moveHistory.Count; i++)
        {
            moveHistory[i].Timestamp.Should().BeOnOrAfter(moveHistory[i - 1].Timestamp);
        }
    }
}
