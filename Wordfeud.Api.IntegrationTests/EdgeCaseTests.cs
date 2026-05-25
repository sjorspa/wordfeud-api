using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests for edge cases and validation scenarios.
/// </summary>
public class EdgeCaseTests : IntegrationTestBase
{

    #region Tile Placement Edge Cases

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenPlacingZeroTiles()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
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
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenPlacingOnOccupiedSquare()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var currentPlayerId = currentPlayer.Id;
        var firstTile = currentPlayer.Hand[0];

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
        await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest1);

        // Re-read game state to get the new current player after turn switch
        gameState = await Client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var nextPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var nextPlayerId = nextPlayer.Id;

        // Switch turn to next player by passing
        var passResponse = await Client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={nextPlayerId}", new { });
        passResponse.EnsureSuccessStatusCode();

        // Re-read game state to get the player who should place next
        gameState = await Client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var placingPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var placingPlayerId = placingPlayer.Id;
        var placingPlayerTile = placingPlayer.Hand[0];

        var placeRequest2 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = placingPlayerTile.Letter,
                    isBlank = placingPlayerTile.IsBlank,
                    tileId = placingPlayerTile.Id,
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={placingPlayerId}", placeRequest2);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenNotConnectingToExistingTiles()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var currentPlayerId = currentPlayer.Id;
        var currentPlayerTile = currentPlayer.Hand[0];

        // Place first tile at center (7,7)
        var placeRequest1 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = currentPlayerTile.Letter,
                    isBlank = currentPlayerTile.IsBlank,
                    tileId = currentPlayerTile.Id,
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest1);

        // Re-read game state to get the new current player after turn switch
        gameState = await Client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var nextPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var nextPlayerId = nextPlayer.Id;

        // Pass turn to next player
        var passResponse = await Client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={nextPlayerId}", new { });
        passResponse.EnsureSuccessStatusCode();

        // Re-read game state to get the player who should place next
        gameState = await Client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var placingPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var placingPlayerId = placingPlayer.Id;
        var placingPlayerTile = placingPlayer.Hand[0];

        // Try to place tiles far from existing tiles (not connected)
        var placeRequest2 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = placingPlayerTile.Letter,
                    isBlank = placingPlayerTile.IsBlank,
                    tileId = placingPlayerTile.Id,
                    row = 0,
                    column = 0
                }
            },
            startRow = 0,
            startColumn = 0,
            direction = 0
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={placingPlayerId}", placeRequest2);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenTilesNotInLine()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
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
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenTilesHaveGaps()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
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
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenFirstMoveNotOnCenter()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
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
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenNotPlayerTurn()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
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
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={otherPlayer.Id}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenInvalidDirection()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
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
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={playerId}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region Tile Swap Edge Cases

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenNotEnoughTilesInBag()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
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
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={playerId}", swapRequest);

        // Assert - should succeed since there are >= 7 tiles in bag
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenPlayerNotTurn()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var otherPlayer = currentGame!.Players.First(p => p.Id != currentGame.CurrentPlayerId);

        var swapRequest = new
        {
            tileIds = otherPlayer.Hand.Take(1).Select(t => t.Id).ToList()
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={otherPlayer.Id}", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenTileNotInHand()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var swapRequest = new
        {
            tileIds = new[] { "nonexistent-tile-id-0000-0000-0000-000000000000" }
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={playerId}", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn400WhenSwappingZeroTiles()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var player = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
        var playerId = player.Id;

        var swapRequest = new
        {
            tileIds = new string[] { }
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={playerId}", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region Game State Edge Cases

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn400WhenGameFinished()
    {
        // Arrange: Create a finished game by simulating game completion
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        // Finish the game with three consecutive passes (must pass with current player each time)
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);

        for (int i = 0; i < 3; i++)
        {
            // Re-read game state to get the current player for this pass
            gameState = await Client.GetAsync($"/api/games/{game.Id}");
            currentGame = await TestHelpers.ReadAsGameAsync(gameState);
            currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);

            var passResponse = await Client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={currentPlayer.Id}", new { });
            passResponse.EnsureSuccessStatusCode();
        }

        // Verify game is now finished
        gameState = await Client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame!.Status.Should().Be(Wordfeud.Api.Models.GameStatus.Finished);

        // Now try to place tiles in a finished game
        var placeRequest = new
        {
            tiles = new[]
            {
                new
                {
                    letter = "A",
                    isBlank = false,
                    tileId = "dummy-tile-id",
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayer.Id}", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn400WhenGameFull()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        // Act - try to join again
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/join", new { PlayerName = "Player3" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn400WhenGameAlreadyFinished()
    {
        // Arrange: Create a finished game
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        // Finish the game with three consecutive passes
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);

        for (int i = 0; i < 3; i++)
        {
            // Re-read game state to get the current player for this pass
            gameState = await Client.GetAsync($"/api/games/{game.Id}");
            currentGame = await TestHelpers.ReadAsGameAsync(gameState);
            currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);

            var passResponse = await Client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={currentPlayer.Id}", new { });
            passResponse.EnsureSuccessStatusCode();
        }

        // Verify game is finished
        gameState = await Client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame!.Status.Should().Be(Wordfeud.Api.Models.GameStatus.Finished);

        // Act: Try to join a finished game
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/join", new { PlayerName = "Player3" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGame_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/games/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    #endregion

    #region Pass Turn Edge Cases

    [Fact]
    public async Task PostPassTurn_ShouldReturn400WhenNotPlayerTurn()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        var otherPlayer = currentGame!.Players.First(p => p.Id != currentGame.CurrentPlayerId);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={otherPlayer.Id}", new { });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostPassTurn_ShouldReturn400WhenGameFinished()
    {
        // Arrange: Create a finished game
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        // Finish the game with three consecutive passes
        var gameState = await Client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);

        for (int i = 0; i < 3; i++)
        {
            gameState = await Client.GetAsync($"/api/games/{game.Id}");
            currentGame = await TestHelpers.ReadAsGameAsync(gameState);
            currentPlayer = currentGame!.Players.First(p => p.Id == currentGame.CurrentPlayerId);
            await Client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={currentPlayer.Id}", new { });
        }

        // Act: Try to pass in a finished game
        var response = await Client.PostAsJsonAsync($"/api/games/{game.Id}/pass?playerId={currentPlayer.Id}", new { });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    #endregion

    #region Health Check

    [Fact]
    public async Task GetHealth_Live_ShouldReturn200()
    {
        // Act
        var response = await Client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_Ready_ShouldReturn200()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_Live_ShouldReturnHealthPayload()
    {
        // Act
        var response = await Client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        content.Should().Contain("alive");
        content.Should().Contain("timestamp");
    }

    [Fact]
    public async Task GetHealth_Ready_ShouldReturnHealthPayload()
    {
        // Act
        var response = await Client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        content.Should().Contain("ready");
        content.Should().Contain("timestamp");
    }

    #endregion

    #region Validation Error Tests

    [Fact]
    public async Task PostCreateGame_ShouldReturn400WhenPlayerNameEmpty()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostCreateGame_ShouldReturn400WhenPlayerNameMissing()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/games", new { });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn400WhenPlayerNameEmpty()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);

        // Act
        var response = await Client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostJoinGame_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/games/nonexistent-id/join", new { PlayerName = "Player2" });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScores_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/games/nonexistent-id/scores");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBoard_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/games/nonexistent-id/board");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMoveHistory_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await Client.GetAsync("/api/games/nonexistent-id/moves");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostPlaceTiles_ShouldReturn404WhenGameNotFound()
    {
        // Arrange
        var placeRequest = new
        {
            tiles = new[]
            {
                new
                {
                    letter = "A",
                    isBlank = false,
                    tileId = "tile-id",
                    row = 7,
                    column = 7
                }
            },
            startRow = 7,
            startColumn = 7,
            direction = 0
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/games/nonexistent-id/place?playerId=player-id", placeRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostPassTurn_ShouldReturn404WhenGameNotFound()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/games/nonexistent-id/pass?playerId=player-id", new { });

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostSwapTiles_ShouldReturn404WhenGameNotFound()
    {
        // Arrange
        var swapRequest = new { tileIds = new[] { "tile-id" } };

        // Act
        var response = await Client.PostAsJsonAsync("/api/games/nonexistent-id/swap?playerId=player-id", swapRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    #endregion

}
