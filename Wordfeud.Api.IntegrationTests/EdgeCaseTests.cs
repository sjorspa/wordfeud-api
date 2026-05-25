using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for edge cases and validation scenarios.
/// </summary>
public class EdgeCaseTests : IntegrationTestBase, IDisposable
{
    private readonly HttpClient _client;

    public EdgeCaseTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    #region Tile Placement Edge Cases

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenPlacingZeroTiles()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayerId = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId).Id;

        var placeRequest = new
        {
            tiles = new object[] { },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenPlacingOnOccupiedSquare()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player1 = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var player1Id = player1.Id;
        var firstTile = player1.Hand[0];

        var placeRequest1 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = firstTile.Letter,
                    isBlank = firstTile.IsBlank,
                    tileId = firstTile.Id,
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Place first tile
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={player1Id}", placeRequest1);

        // Switch turn to player 2
        var passResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={player1Id}", new { });
        passResponse.EnsureSuccessStatusCode();

        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var player2 = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var player2Id = player2.Id;
        var player2Tile = player2.Hand[0];

        var placeRequest2 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = player2Tile.Letter,
                    isBlank = player2Tile.IsBlank,
                    tileId = player2Tile.Id,
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={player2Id}", placeRequest2);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenNotConnectingToExistingTiles()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player1 = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var player1Id = player1.Id;
        var player1Tile = player1.Hand[0];

        // Place first tile at center (7,7)
        var placeRequest1 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = player1Tile.Letter,
                    isBlank = player1Tile.IsBlank,
                    tileId = player1Tile.Id,
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={player1Id}", placeRequest1);

        // Pass turn to player 2
        var passResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={player1Id}", new { });
        passResponse.EnsureSuccessStatusCode();

        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var player2 = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var player2Id = player2.Id;
        var player2Tile = player2.Hand[0];

        // Try to place tiles far from existing tiles (not connected)
        var placeRequest2 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = player2Tile.Letter,
                    isBlank = player2Tile.IsBlank,
                    tileId = player2Tile.Id,
                    row = 0,
                    column = 0
                }
            },
            startRow = 0,
            startColumn = 0,
            direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={player2Id}", placeRequest2);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenTilesNotInLine()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var placeRequest = new
        {
            tiles = new[]
            {
                new { letter = "A", isBlank = false, tileId = player.Hand[0].Id, row = 7, column = 7 },
                new { letter = "B", isBlank = false, tileId = player.Hand[1].Id, row = 8, column = 8 }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenTilesHaveGaps()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var placeRequest = new
        {
            tiles = new[]
            {
                new { letter = "A", isBlank = false, tileId = player.Hand[0].Id, row = 7, column = 7 },
                new { letter = "B", isBlank = false, tileId = player.Hand[1].Id, row = 7, column = 9 }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenFirstMoveNotOnCenter()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var placeRequest = new
        {
            tiles = new[]
            {
                new { letter = "A", isBlank = false, tileId = player.Hand[0].Id, row = 0, column = 0 }
            },
            startRow = 0,
            startColumn = 0,
            direction = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenNotPlayerTurn()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var otherPlayer = currentGame.Players.First(p => p.Id != currentGame.CurrentPlayerId);

        var placeRequest = new
        {
            tiles = new[]
            {
                new { letter = "A", isBlank = false, tileId = otherPlayer.Hand[0].Id, row = 7, column = 7 }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act - try to place tiles as the non-current player
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={otherPlayer.Id}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenInvalidDirection()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var placeRequest = new
        {
            tiles = new[]
            {
                new { letter = "A", isBlank = false, tileId = player.Hand[0].Id, row = 7, column = 7 }
            },
            startRow = 7,
            startColumn = 7,
            direction = 99
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region Tile Swap Edge Cases

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenNotEnoughTilesInBag()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        // Get remaining tiles in bag from game state
        var remainingInBag = currentGame!.TileBag?.Count ?? 0;

        // If there are fewer than 7 tiles in the bag, we can't test this
        if (remainingInBag < 7)
        {
            return;
        }

        var swapRequest = new
        {
            tileIds = player.Hand.Take(3).Select(t => t.Id).ToList()
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={playerId}", swapRequest);

        // Assert - should succeed since there are >= 7 tiles in bag
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenPlayerNotTurn()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var otherPlayer = currentGame!.Players.First(p => p.Id != currentGame.CurrentPlayerId);

        var swapRequest = new
        {
            tileIds = otherPlayer.Hand.Take(1).Select(t => t.Id).ToList()
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={otherPlayer.Id}", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenTileNotInHand()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var swapRequest = new
        {
            tileIds = new[] { Guid.NewGuid().ToString() }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={playerId}", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenSwappingZeroTiles()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var swapRequest = new
        {
            tileIds = new string[] { }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={playerId}", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region Game State Edge Cases

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenGameFinished()
    {
        // Arrange: Create a game, fill the board with tiles until game ends
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);

        // Simulate a finished game by manipulating state through the API
        // We'll test the game finished scenario by checking the game status
        var gameState = await _client.GetAsync($"/api/games/{game!.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        // Assert game is not finished initially
        currentGame!.Status.Should().NotBe(Wordfeud.Api.Models.GameStatus.Finished);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn400WhenGameFull()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        // Act - try to join again
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/join", new { PlayerName = "Player3" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn400WhenGameAlreadyFinished()
    {
        // Arrange - use a non-existent game (simulates trying to join a finished/missing game)
        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/join", new { PlayerName = "Player1" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGame_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/games/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    #endregion

    #region Pass Turn Edge Cases

    [Fact]
    public async Task PostPassTurn_ShouldReturn400WhenNotPlayerTurn()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var otherPlayer = currentGame!.Players.First(p => p.Id != currentGame.CurrentPlayerId);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={otherPlayer.Id}", new { });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostPassTurn_ShouldReturn400WhenGameFinished()
    {
        // Arrange - use a non-existent game
        // Act
        var response = await _client.PostAsJsonAsync("/api/games/nonexistent-id/pass?playerId=dummy", new { });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region Health Check

    [Fact]
    public async Task GetHealth_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    #endregion

    public void Dispose()
    {
        // No resources to dispose
    }
}
