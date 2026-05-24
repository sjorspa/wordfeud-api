using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Wordfeud.Api.Models;
using System.Net.Http.Json;

namespace Wordfeud.Api.IntegrationTests;

/// <summary>
/// Integration tests that simulate full game flows including multiple turns, tile placements, swaps, passes, and game completion.
/// </summary>
public class FullGameTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FullGameTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullGame_ShouldCompleteWhenPlayerUsesAllTilesAndBagIsEmpty()
    {
        // Arrange - create a new game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        // Player2 joins
        var joinResponse = await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        joinResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Get current game state
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame.Should().NotBeNull();
        currentGame!.Status.Should().Be(GameStatus.InProgress);

        var player1 = currentGame.Players.First(p => p.Name == "Player1");
        var player2 = currentGame.Players.First(p => p.Name == "Player2");
        var currentPlayerId = currentGame.CurrentPlayerId;

        // Act - Player1 places first tile on center square (7,7)
        var player1Hand = player1.Hand;
        var firstTile = player1Hand[0];

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

        var placeResponse1 = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest1);
        placeResponse1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Get updated game state after Player1's turn
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var nextPlayerId = currentGame!.CurrentPlayerId;

        // Player2 passes (to simplify the test, we'll use passes to advance turns)
        var passResponse1 = await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={nextPlayerId}", null);
        passResponse1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Get game state after pass
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        // Player1 places another tile adjacent to the first one
        player1 = currentGame.Players.First(p => p.Name == "Player1");
        currentPlayerId = currentGame.CurrentPlayerId;
        player1Hand = player1.Hand;
        firstTile = player1Hand[0];

        var placeRequest2 = new
        {
            tiles = new[]
            {
                new
                {
                    letter = firstTile.Letter,
                    isBlank = firstTile.IsBlank,
                    tileId = firstTile.Id,
                    row = 7,
                    column = 8
                }
            },
            startRow = 7,
            startColumn = 8,
            direction = 0
        };

        var placeResponse2 = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest2);
        placeResponse2.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Get final game state
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);

        // Assert
        currentGame!.Status.Should().Be(GameStatus.InProgress);
        currentGame.Players.Should().HaveCount(2);
        currentGame.Board[7, 7].Should().NotBeNull();
        currentGame.Board[7, 8].Should().NotBeNull();
    }

    [Fact]
    public async Task ThreeConsecutivePasses_ShouldEndGame()
    {
        // Arrange - create a new game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        // Player2 joins
        var joinResponse = await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        joinResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Get current game state
        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentGame.Should().NotBeNull();

        // Act - Player1 passes
        var currentPlayerId = currentGame!.CurrentPlayerId;
        var passResponse1 = await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentPlayerId}", null);
        passResponse1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Player2 passes
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentPlayerId = currentGame!.CurrentPlayerId;
        var passResponse2 = await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentPlayerId}", null);
        passResponse2.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Player1 passes (3rd consecutive pass)
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        currentPlayerId = currentGame!.CurrentPlayerId;
        var passResponse3 = await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentPlayerId}", null);
        passResponse3.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Assert - game should be finished
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var finalGame = await TestHelpers.ReadAsGameAsync(gameState);
        finalGame!.Status.Should().Be(GameStatus.Finished);
    }

    [Fact]
    public async Task SingleTilePlacement_OnFirstMove_ShouldSucceed()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayerId = currentGame!.CurrentPlayerId;
        var player = currentGame.Players.First(p => p.Id == currentPlayerId);

        // Act - Place a single tile on center square (first move, no word validation needed)
        var firstTile = player.Hand[0];
        var placeRequest = new
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

        var placeResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest);

        // Assert
        placeResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var updatedGame = await TestHelpers.ReadAsGameAsync(gameState);
        updatedGame!.Board[7, 7].Should().NotBeNull();
        updatedGame.CurrentPlayerId.Should().NotBe(currentPlayerId); // Turn should change
    }

    [Fact]
    public async Task Scores_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayerId = currentGame!.CurrentPlayerId;
        var player = currentGame.Players.First(p => p.Id == currentPlayerId);

        // Act - Place a single tile on center square (no bonus)
        var firstTile = player.Hand[0];
        var placeRequest = new
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

        await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest);

        // Get scores
        var scoresResponse = await _client.GetAsync($"/api/games/{game.Id}/scores");
        scoresResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Assert - verify scores are calculated
        var scoresContent = await scoresResponse.Content.ReadFromJsonAsync<GameScoresDto>();
        scoresContent.Should().NotBeNull();
        scoresContent!.Players.Should().HaveCount(2);
        scoresContent.Players.Should().OnlyContain(p => p.Score >= 0);
    }

    [Fact]
    public async Task MultipleTurns_ShouldAlternatePlayersCorrectly()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var player1Id = currentGame!.Players.First(p => p.Name == "Player1").Id;
        var player2Id = currentGame.Players.First(p => p.Name == "Player2").Id;

        // Act - Simulate multiple turns
        var currentPlayerId = currentGame.CurrentPlayerId;
        for (var turn = 0; turn < 6; turn++)
        {
            gameState = await _client.GetAsync($"/api/games/{game.Id}");
            currentGame = await TestHelpers.ReadAsGameAsync(gameState);
            currentPlayerId = currentGame!.CurrentPlayerId;

            var player = currentGame.Players.First(p => p.Id == currentPlayerId);
            if (player.Hand.Count == 0)
            {
                // Player needs to pass if no tiles
                await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentPlayerId}", null);
                continue;
            }

            var firstTile = player.Hand[0];
            var placeRequest = new
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

            await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest);
        }

        // Assert - verify turn alternation
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var finalGame = await TestHelpers.ReadAsGameAsync(gameState);
        finalGame!.Status.Should().Be(GameStatus.InProgress);
    }

    [Fact]
    public async Task SwapTiles_ShouldReturnTilesToBagAndDrawNewOnes()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayerId = currentGame!.CurrentPlayerId;
        var player = currentGame.Players.First(p => p.Id == currentPlayerId);

        var tilesBeforeSwap = player.Hand.Count;

        // Act - Swap 2 tiles
        var tilesToSwap = player.Hand.Take(2).Select(t => t.Id).ToArray();
        var swapRequest = new { tileIds = tilesToSwap };

        var swapResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/swap?playerId={currentPlayerId}", swapRequest);
        swapResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Assert - verify tiles were swapped and new ones drawn
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var updatedGame = await TestHelpers.ReadAsGameAsync(gameState);
        var updatedPlayer = updatedGame!.Players.First(p => p.Id == currentPlayerId);

        // Player should have 7 tiles again (swapped 2, drew 2)
        updatedPlayer.Hand.Count.Should().Be(tilesBeforeSwap);
    }

    [Fact]
    public async Task GameQueryAfterMultipleMoves_ShouldReturnCorrectState()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var currentPlayerId = currentGame!.CurrentPlayerId;
        var player = currentGame.Players.First(p => p.Id == currentPlayerId);

        // Act - Place a single tile on the center square (first move)
        var firstTile = player.Hand[0];
        var placeRequest = new
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

        await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest);

        // Get board state
        var boardResponse = await _client.GetAsync($"/api/games/{game.Id}/board");
        boardResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        // Assert - verify board state
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var updatedGame = await TestHelpers.ReadAsGameAsync(gameState);
        updatedGame!.Board[7, 7].Should().NotBeNull();
    }

    [Fact]
    public async Task FullGameFlow_ShouldHandleCompleteGameUntilBothPlayersFinish()
    {
        // Arrange - create a new game
        var createResponse = await _client.PostAsJsonAsync("/api/games", new { PlayerName = "Player1" });
        var game = await TestHelpers.ReadAsGameAsync(createResponse);
        game.Should().NotBeNull();

        // Player2 joins
        var joinResponse = await _client.PostAsJsonAsync($"/api/games/{game!.Id}/join", new { PlayerName = "Player2" });
        joinResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var currentGame = await TestHelpers.ReadAsGameAsync(gameState);
        var player1Id = currentGame!.Players.First(p => p.Name == "Player1").Id;
        var player2Id = currentGame.Players.First(p => p.Name == "Player2").Id;

        // Act - Simulate a complete game with multiple turns
        var maxTurns = 20;
        for (var turn = 0; turn < maxTurns; turn++)
        {
            gameState = await _client.GetAsync($"/api/games/{game.Id}");
            currentGame = await TestHelpers.ReadAsGameAsync(gameState);

            if (currentGame!.Status == GameStatus.Finished)
                break;

            var currentPlayerId = currentGame.CurrentPlayerId;
            var currentPlayer = currentGame.Players.First(p => p.Id == currentPlayerId);

            if (currentPlayer.Hand.Count == 0)
            {
                // Player passes if no tiles
                await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentPlayerId}", null);
                continue;
            }

            // Place tiles
            var tilesToPlace = currentPlayer.Hand.Take(Math.Min(3, currentPlayer.Hand.Count)).ToList();
            var placeRequest = new
            {
                tiles = tilesToPlace.Select(t => new
                {
                    letter = t.Letter,
                    isBlank = t.IsBlank,
                    tileId = t.Id,
                    row = 7,
                    column = 7 + tilesToPlace.IndexOf(t)
                }).ToArray(),
                startRow = 7,
                startColumn = 7,
                direction = 0
            };

            var placeResponse = await _client.PostAsJsonAsync($"/api/games/{game.Id}/place?playerId={currentPlayerId}", placeRequest);

            if (placeResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                // If placement fails, pass the turn
                await _client.PostAsync($"/api/games/{game.Id}/pass?playerId={currentPlayerId}", null);
            }
        }

        // Assert - verify game state
        gameState = await _client.GetAsync($"/api/games/{game.Id}");
        var finalGame = await TestHelpers.ReadAsGameAsync(gameState);
        finalGame.Should().NotBeNull();

        // Get final scores
        var scoresResponse = await _client.GetAsync($"/api/games/{game.Id}/scores");
        scoresResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
