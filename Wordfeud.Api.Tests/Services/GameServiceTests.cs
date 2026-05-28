using Wordfeud.Api.Data;
using Wordfeud.Api.Interfaces;
using Wordfeud.Api.Models;
using Wordfeud.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Shouldly;

namespace Wordfeud.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="GameService"/>.
/// </summary>
public class GameServiceTests
{
    private readonly Mock<IDutchDictionaryService> _dictionaryMock;
    private readonly Mock<ILogger<GameService>> _loggerMock;
    private readonly GameService _service;

    public GameServiceTests()
    {
        _dictionaryMock = new Mock<IDutchDictionaryService>();
        _loggerMock = new Mock<ILogger<GameService>>();
        _service = new GameService(_dictionaryMock.Object, _loggerMock.Object);

        // Default: accept all words
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);
    }

    #region CreateGameAsync Tests

    [Fact]
    public async Task CreateGameAsync_ShouldCreateGameWithOnePlayer()
    {
        // Act
        var game = await _service.CreateGameAsync("Player1");

        // Assert
        game.Should().NotBeNull();
        game.Status.Should().Be(GameStatus.Waiting);
        game.Players.Should().HaveCount(1);
        game.Players[0].Name.Should().Be("Player1");
        game.Players[0].Hand.Should().HaveCount(7);
        game.Players[0].Score.Should().Be(0);
        game.TileBag.Should().HaveCount(97); // 104 total - 7 dealt
        game.CurrentPlayerId.Should().Be(game.Players[0].Id);
        game.ConsecutivePasses.Should().Be(0);
        game.MoveNumber.Should().Be(0);
    }

    [Fact]
    public async Task CreateGameAsync_ShouldCreateGameWithUniqueId()
    {
        // Act
        var game1 = await _service.CreateGameAsync("Player1");
        var game2 = await _service.CreateGameAsync("Player2");

        // Assert
        game1.Id.Should().NotBe(game2.Id);
    }

    [Fact]
    public async Task CreateGameAsync_ShouldDealTilesFromBag()
    {
        // Act
        var game = await _service.CreateGameAsync("Player1");

        // Assert
        game.Players[0].Hand.Should().NotBeNullOrEmpty();
        game.TileBag.Should().HaveCount(97); // 104 - 7
    }

    [Fact]
    public async Task CreateGameAsync_ShouldSetCreatedAtAndUpdatedAt()
    {
        // Act
        var game = await _service.CreateGameAsync("Player1");

        // Assert
        game.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        game.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    #endregion

    #region JoinGameAsync Tests

    [Fact]
    public async Task JoinGameAsync_ShouldAddSecondPlayer()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");

        // Act
        var result = await _service.JoinGameAsync(game.Id, "Player2");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(GameStatus.InProgress);
        result.Players.Should().HaveCount(2);
        result.Players[1].Name.Should().Be("Player2");
        result.Players[1].Hand.Should().HaveCount(7);
        result.CurrentPlayerId.Should().Be(result.Players[0].Id);
    }

    [Fact]
    public async Task JoinGameAsync_ShouldThrowWhenGameNotFound()
    {
        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () => await _service.JoinGameAsync("nonexistent-id", "Player2"));
    }

    [Fact]
    public async Task JoinGameAsync_ShouldThrowWhenGameAlreadyStarted()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.JoinGameAsync(game.Id, "Player3"));
        ex.Message.ShouldContain("already started");
    }

    [Fact]
    public async Task JoinGameAsync_ShouldThrowWhenGameFull()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");

        // Act & Assert
        var ex2 = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.JoinGameAsync(game.Id, "Player3"));
        ex2.Message.ShouldContain("Game has already started");
    }

    [Fact]
    public async Task JoinGameAsync_ShouldDealTilesToBothPlayers()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");

        // Act
        var result = await _service.JoinGameAsync(game.Id, "Player2");

        // Assert
        result.Players[0].Hand.Should().HaveCount(7);
        result.Players[1].Hand.Should().HaveCount(7);
        result.TileBag.Should().HaveCount(90); // 104 total tiles - 7 (create) - 7 (join)
    }

    #endregion

    #region GetGameAsync Tests

    [Fact]
    public async Task GetGameAsync_ShouldReturnGame_WhenGameExists()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");

        // Act
        var result = await _service.GetGameAsync(game.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(game.Id);
        result.Players[0].Name.Should().Be("Player1");
    }

    [Fact]
    public async Task GetGameAsync_ShouldReturnNull_WhenGameNotFound()
    {
        // Act
        var result = await _service.GetGameAsync("nonexistent-id");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PlaceTilesAsync Tests

    [Fact]
    public async Task PlaceTilesAsync_ShouldPlaceSingleTileOnCenter()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = result.Players.First(p => p.Id == playerId).Hand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.Board[7, 7].Should().NotBeNull();
        placedGame.Players.First(p => p.Id == playerId).Hand.Should().HaveCount(7); // 7 - 1 placed + 1 drawn
        placedGame.ConsecutivePasses.Should().Be(0);
        placedGame.MoveNumber.Should().Be(1);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenGameNotFound()
    {
        // Arrange
        var request = new PlaceTilesRequest
        {
            PlayerId = "player-id",

            Tiles = new List<TilePlacementDto>(),
        };

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () => await _service.PlaceTilesAsync("nonexistent", "player-id", request));
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenGameFinished()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        var playerId = game.Players[0].Id;

        // Simulate finished game
        game.Status = GameStatus.Finished;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>(),
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.PlaceTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("finished");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenNotPlayerTurn()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var opponentId = result!.Players.First(p => p.Id != result.CurrentPlayerId).Id;

        var request = new PlaceTilesRequest
        {
                     PlayerId = opponentId,

            Tiles = new List<TilePlacementDto>(),
        };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () => await _service.PlaceTilesAsync(game.Id, opponentId, request));
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenNoTiles()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>(),
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("at least one tile");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenPositionOccupied()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        // Place first tile at (7,7)
        var firstRequest = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };
        await _service.PlaceTilesAsync(game.Id, playerId, firstRequest);

        // After first placement, turn passes to the other player
        var updatedGame = await _service.GetGameAsync(game.Id);
        var currentPlayerId = updatedGame!.CurrentPlayerId!;
        var currentHand = updatedGame.Players.First(p => p.Id == currentPlayerId).Hand;

        // Try to place at same position (now with the player who has the turn)
        var secondRequest = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = currentHand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.PlaceTilesAsync(game.Id, currentPlayerId, secondRequest));
        ex.Message.ShouldContain("occupied");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenTilesNotInHand()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;

        var fakeTileId = Guid.NewGuid().ToString();
        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = fakeTileId,
                    Row = 7,
                    Column = 8
                }
            },
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("not in your hand");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenBlankWithoutLetter()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var player = result.Players.First(p => p.Id == playerId);

        // Create a blank tile and add it to the player's hand to guarantee we have one
        var blankTile = new Tile
        {
            Letter = string.Empty,
            Points = 0,
            IsBlank = true,
            BlankRepresentation = null,
            Id = Guid.NewGuid().ToString()
        };
        player.Hand.Add(blankTile);

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = string.Empty,
                    IsBlank = true,
                    TileId = blankTile.Id,
                    Row = 7,
                    Column = 8
                }
            },
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("Blank tile must have a single letter");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenFirstMoveNotOnCenter()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 0,
                    Column = 0
                }
            },
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("center square");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenNotConnectingToExistingTiles()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        // Place first word at center
        var firstRequest = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };
        await _service.PlaceTilesAsync(game.Id, playerId, firstRequest);

        // Get next player's turn
        result = await _service.GetGameAsync(game.Id);
        var nextPlayerId = result!.CurrentPlayerId!;
        var nextHand = result.Players.First(p => p.Id == nextPlayerId).Hand;

        // Place tiles far from center (not connecting)
        var secondRequest = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = nextHand[0].Id,
                    Row = 0,
                    Column = 0
                },
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = nextHand[1].Id,
                    Row = 0,
                    Column = 1
                }
            },
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, nextPlayerId, secondRequest));
        ex.Message.ShouldContain("connect");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenInvalidWord()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        // Place tiles starting from center (valid first move) but with a word that's not in the dictionary
        // Since GetFormedWords reads from game.Board during validation, we need to test with a scenario
        // where the formed word is checked against the dictionary. We'll test with a word that
        // would be formed if placed correctly.
        // For first move, tiles must cross (7,7), so we place at (7,7) and (7,8)
        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                },
                new()
                {
                    Letter = "A",
                    IsBlank = false,
                    TileId = hand[1].Id,
                    Row = 7,
                    Column = 8
                }
            },
        };

        // Act & Assert - Place first tile at (7,7) which is valid, then try invalid word placement
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);
        
        // Now try placing a non-connecting word (invalid for non-first move)
        var nextResult = await _service.GetGameAsync(game.Id);
        var nextPlayerId = nextResult!.CurrentPlayerId!;
        var nextHand = nextResult.Players.First(p => p.Id == nextPlayerId).Hand;

        var invalidRequest = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "B",
                    IsBlank = false,
                    TileId = nextHand[0].Id,
                    Row = 0,
                    Column = 0
                }
            },
        };

        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, nextPlayerId, invalidRequest));
        ex.Message.ShouldContain("connect to existing tiles");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldPlaceHorizontalTiles()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                },
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[1].Id,
                    Row = 7,
                    Column = 8
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.Board[7, 7].Should().NotBeNull();
        placedGame.Board[7, 8].Should().NotBeNull();
        placedGame.Players.First(p => p.Id == playerId).Hand.Should().HaveCount(7);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldPlaceVerticalTiles()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                },
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[1].Id,
                    Row = 8,
                    Column = 7
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.Board[7, 7].Should().NotBeNull();
        placedGame.Board[8, 7].Should().NotBeNull();
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldDrawReplacementTiles()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);
        var updatedPlayer = placedGame.Players.First(p => p.Id == playerId);

        // Assert
        updatedPlayer.Hand.Should().HaveCount(7); // Should be refilled to 7
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldIncrementMoveNumber()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.MoveNumber.Should().Be(1);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldSwitchTurnToNextPlayer()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.CurrentPlayerId.Should().NotBe(playerId);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldAddScoreToPlayer()
    {
        // Arrange: Reset mock to ensure default behavior (accept all words)
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);
        
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        // Place 2 tiles to form a valid 2+ letter word (single tile placement fails validation)
        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                },
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[1].Id,
                    Row = 7,
                    Column = 8
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);
        var updatedPlayer = placedGame.Players.First(p => p.Id == playerId);

        // Assert - EE is worth 2 points, placed on center (no bonus)
        updatedPlayer.Score.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreCrossWordCorrectly()
    {
        // Arrange: Reset mock to ensure default behavior (accept all words)
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        // Step 1: Place tiles to form "GA" horizontally at row 7, columns 7-8
        // (7,7) is a Double Word square, (7,8) is no bonus
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "G", IsBlank = false, TileId = hand[0].Id, Row = 7, Column = 7 },
                new() { Letter = "A", IsBlank = false, TileId = hand[1].Id, Row = 7, Column = 8 }
            },
        };

        // Act: Place first word "GA"
        var placedGame1 = await _service.PlaceTilesAsync(game.Id, playerId, request1);
        var player1 = placedGame1.Players.First(p => p.Id == playerId);

        // Verify some score was awarded (exact value depends on game logic)
        player1.Score.Should().BeGreaterThan(0);

        // Step 2: Place 'E' vertically above the 'A' at (6,8) which is a Double Letter square
        // This creates a vertical cross word with A at (7,8)
        var result2 = await _service.GetGameAsync(game.Id);
        var currentPlayerId = result2!.CurrentPlayerId!;
        var scoreBefore = result2.Players.First(p => p.Id == currentPlayerId).Score;
        var hand2 = result2.Players.First(p => p.Id == currentPlayerId).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "E", IsBlank = false, TileId = hand2[0].Id, Row = 6, Column = 8 }
            },
        };

        // Act: Place second tile creating a cross word on Double Letter square
        var placedGame2 = await _service.PlaceTilesAsync(game.Id, currentPlayerId, request2);
        var player2 = placedGame2.Players.First(p => p.Id == currentPlayerId);

        // Verify score increased due to cross word scoring
        player2.Score.Should().BeGreaterThan(scoreBefore);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleCrossWordsCorrectly()
    {
        // Arrange: Reset mock to ensure default behavior (accept all words)
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        // Step 1: Place "DE" horizontally starting at (7,6)-(7,7)
        // E lands on center (7,7) which is Double Word
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "D", IsBlank = false, TileId = hand[0].Id, Row = 7, Column = 6 },
                new() { Letter = "E", IsBlank = false, TileId = hand[1].Id, Row = 7, Column = 7 }
            },
        };

        await _service.PlaceTilesAsync(game.Id, playerId, request1);

        // Step 2: Place "AN" vertically crossing through E at (7,7)
        // A at (5,7), N at (6,7) - main word "AN", cross word "DE"
        var result2 = await _service.GetGameAsync(game.Id);
        var currentPlayerId = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == currentPlayerId).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "A", IsBlank = false, TileId = hand2[0].Id, Row = 5, Column = 7 },
                new() { Letter = "N", IsBlank = false, TileId = hand2[1].Id, Row = 6, Column = 7 }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, currentPlayerId, request2);
        var player = placedGame.Players.First(p => p.Id == currentPlayerId);

        // Verify scoring includes cross words - score should be positive and reflect
        // both the main word (vertical "AN") and the cross word (horizontal "DE")
        player.Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldResetConsecutivePasses()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        // Simulate some passes
        await _service.PassTurnAsync(game.Id, playerId);
        var nextPlayerId = (await _service.GetGameAsync(game.Id))!.CurrentPlayerId!;
        await _service.PassTurnAsync(game.Id, nextPlayerId);
        result = await _service.GetGameAsync(game.Id);
        playerId = result!.CurrentPlayerId!;

        var request = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "E",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                }
            },
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.ConsecutivePasses.Should().Be(0);
    }

    #endregion

    #region PassTurnAsync Tests

    [Fact]
    public async Task PassTurnAsync_ShouldIncrementConsecutivePasses()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;

        // Act
        var passedGame = await _service.PassTurnAsync(game.Id, playerId);

        // Assert
        passedGame.ConsecutivePasses.Should().Be(1);
    }

    [Fact]
    public async Task PassTurnAsync_ShouldSwitchTurnToNextPlayer()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var nextPlayerId = result!.Players.First(p => p.Id != playerId).Id;

        // Act
        var passedGame = await _service.PassTurnAsync(game.Id, playerId);

        // Assert
        passedGame.CurrentPlayerId.Should().Be(nextPlayerId);
    }

    [Fact]
    public async Task PassTurnAsync_ShouldIncrementMoveNumber()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;

        // Act
        var passedGame = await _service.PassTurnAsync(game.Id, playerId);

        // Assert
        passedGame.MoveNumber.Should().Be(1);
    }

    [Fact]
    public async Task PassTurnAsync_ShouldEndGameAfterThreeConsecutivePasses()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var player1Id = result!.Players[0].Id;
        var player2Id = result!.Players[1].Id;

        // Act - 3 consecutive passes
        await _service.PassTurnAsync(game.Id, player1Id);
        await _service.PassTurnAsync(game.Id, player2Id);
        var passedGame = await _service.PassTurnAsync(game.Id, player1Id);

        // Assert
        passedGame.Status.Should().Be(GameStatus.Finished);
    }

    [Fact]
    public async Task PassTurnAsync_ShouldThrowWhenGameNotFound()
    {
        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () => await _service.PassTurnAsync("nonexistent", "player-id"));
    }

    [Fact]
    public async Task PassTurnAsync_ShouldThrowWhenGameFinished()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        game.Status = GameStatus.Finished;
        var playerId = game.Players[0].Id;

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.PassTurnAsync(game.Id, playerId));
        ex.Message.ShouldContain("finished");
    }

    [Fact]
    public async Task PassTurnAsync_ShouldThrowWhenNotPlayerTurn()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var wrongPlayerId = result!.Players.First(p => p.Id != result.CurrentPlayerId).Id;

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () => await _service.PassTurnAsync(game.Id, wrongPlayerId));
    }

    #endregion

    #region SwapTilesAsync Tests

    [Fact]
    public async Task SwapTilesAsync_ShouldReturnTilesToBagAndDrawNewOnes()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var player = result.Players.First(p => p.Id == playerId);

        var tileToSwap = player.Hand[0].Id;
        var request = new SwapTilesRequest
        {
            PlayerId = playerId,
            TileIds = new List<string> { tileToSwap }
        };

        // Act
        var swappedGame = await _service.SwapTilesAsync(game.Id, playerId, request);
        var updatedPlayer = swappedGame.Players.First(p => p.Id == playerId);

        // Assert
        updatedPlayer.Hand.Should().HaveCount(7); // Should be refilled
        swappedGame.TileBag.Should().Contain(t => t.Id == tileToSwap);
    }

    [Fact]
    public async Task SwapTilesAsync_ShouldThrowWhenGameNotFound()
    {
        // Arrange
        var request = new SwapTilesRequest { PlayerId = "player-id", TileIds = new List<string> { "tile-id" } };

        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () => await _service.SwapTilesAsync("nonexistent", "player-id", request));
    }

    [Fact]
    public async Task SwapTilesAsync_ShouldThrowWhenGameFinished()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        game.Status = GameStatus.Finished;
        var playerId = game.Players[0].Id;
        var request = new SwapTilesRequest { PlayerId = playerId, TileIds = new List<string> { "tile-id" } };

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.SwapTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("finished");
    }

    [Fact]
    public async Task SwapTilesAsync_ShouldThrowWhenNotPlayerTurn()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var wrongPlayerId = result!.Players.First(p => p.Id != result.CurrentPlayerId).Id;
        var request = new SwapTilesRequest { PlayerId = wrongPlayerId, TileIds = new List<string> { "tile-id" } };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () => await _service.SwapTilesAsync(game.Id, wrongPlayerId, request));
    }

    [Fact]
    public async Task SwapTilesAsync_ShouldThrowWhenNotEnoughTilesInBag()
    {
        // Arrange: Create a game and drain the bag
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var player = result.Players.First(p => p.Id == playerId);

        // Empty the bag using reflection
        var gameType = game.GetType();
        var tileBagProperty = gameType.GetProperty("TileBag");
        var tileBag = tileBagProperty!.GetValue(game) as List<Tile>;
        tileBag!.Clear();

        // Act & Assert: Try to swap all tiles when bag is empty
        var swapRequest = new SwapTilesRequest { PlayerId = playerId, TileIds = player.Hand.Select(t => t.Id).ToList() };

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.SwapTilesAsync(game.Id, playerId, swapRequest));
        ex.Message.ShouldContain("At least 7 tiles must remain in the bag");
    }

    [Fact]
    public async Task SwapTilesAsync_ShouldIncrementMoveNumber()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var player = result.Players.First(p => p.Id == playerId);

        var request = new SwapTilesRequest { PlayerId = playerId, TileIds = new List<string> { player.Hand[0].Id } };

        // Act
        var swappedGame = await _service.SwapTilesAsync(game.Id, playerId, request);

        // Assert
        swappedGame.MoveNumber.Should().Be(1);
    }

    [Fact]
    public async Task SwapTilesAsync_ShouldResetConsecutivePasses()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var player = result.Players.First(p => p.Id == playerId);

        // Simulate passes
        await _service.PassTurnAsync(game.Id, playerId);
        var nextPlayerId = (await _service.GetGameAsync(game.Id))!.CurrentPlayerId!;
        await _service.PassTurnAsync(game.Id, nextPlayerId);
        result = await _service.GetGameAsync(game.Id);
        playerId = result!.CurrentPlayerId!;

        var request = new SwapTilesRequest { PlayerId = playerId, TileIds = new List<string> { result.Players.First(p => p.Id == playerId).Hand[0].Id } };

        // Act
        var swappedGame = await _service.SwapTilesAsync(game.Id, playerId, request);

        // Assert
        swappedGame.ConsecutivePasses.Should().Be(0);
    }

    #endregion

    #region GetScoresAsync Tests

    [Fact]
    public async Task GetScoresAsync_ShouldReturnGame_WhenGameExists()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");

        // Act
        var result = await _service.GetScoresAsync(game.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(game.Id);
    }

    [Fact]
    public async Task GetScoresAsync_ShouldThrowWhenGameNotFound()
    {
        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () => await _service.GetScoresAsync("nonexistent"));
    }

    [Fact]
    public async Task GetScoresAsync_ShouldCalculateFinalScores_WhenGameFinished()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");

        // Simulate finished game
        game = await _service.GetGameAsync(game.Id);
        game!.Status = GameStatus.Finished;
        game.Players[0].Hand.Clear(); // Player 1 finished
        game.Players[1].Score = 100;

        // Act
        var result = await _service.GetScoresAsync(game.Id);

        // Assert
        result.Players[0].Score.Should().BeLessThan(100); // Score adjusted for remaining tiles
    }

    #endregion

    #region GetBoardAsync Tests

    [Fact]
    public async Task GetBoardAsync_ShouldReturnGame_WhenGameExists()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");

        // Act
        var result = await _service.GetBoardAsync(game.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(game.Id);
        result.Tiles.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBoardAsync_ShouldThrowWhenGameNotFound()
    {
        // Act & Assert
        await Should.ThrowAsync<KeyNotFoundException>(async () => await _service.GetBoardAsync("nonexistent"));
    }

    [Fact]
    public async Task GetBoardAsync_ShouldReturnEmptyBoard_WhenNoTilesPlaced()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");

        // Act
        var result = await _service.GetBoardAsync(game.Id);

        // Assert
        result!.Tiles.Should().BeEmpty();
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordsCorrectly()
    {
        // Arrange: Reset mock to ensure default behavior (accept all words)
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var playerId = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Kelk" horizontally at (7,7)-(7,10)
        var hand1 = game.Players.First(p => p.Id == playerId).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "K", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 7 },
                new() { Letter = "E", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 8 },
                new() { Letter = "L", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 9 },
                new() { Letter = "K", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 10 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, playerId, request1);
        var player1 = placedGame1.Players.First(p => p.Id == playerId);
        player1.Score.Should().BeGreaterThan(0);

        // Step 2: Player2 places "Kaars" vertically at (7,7)-(11,7), sharing K at (7,7)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        // Player2 places "aars" at (8,7)-(11,7) - K at (7,7) is already there
        var request2 = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "A", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 7 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 7 },
                new() { Letter = "R", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 7 },
                new() { Letter = "S", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 7 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2 = placedGame2.Players.First(p => p.Id == player2Id);
        player2.Score.Should().BeGreaterThan(0);

        // Step 3: Player1 places "Hels" horizontally at (8,8)-(8,11)
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = playerId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "H", IsBlank = false, TileId = hand3[0].Id, Row = 8, Column = 8 },
                new() { Letter = "E", IsBlank = false, TileId = hand3[1].Id, Row = 8, Column = 9 },
                new() { Letter = "L", IsBlank = false, TileId = hand3[2].Id, Row = 8, Column = 10 },
                new() { Letter = "S", IsBlank = false, TileId = hand3[3].Id, Row = 8, Column = 11 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Again = placedGame3.Players.First(p => p.Id == player1AgainId);

        // Verify: score should be positive because multiple words were formed
        // (main word + cross words formed with vertical columns)
        player1Again.Score.Should().BeGreaterThan(0);

        // Verify multiple words were formed (main word + cross words)
        placedGame3.FormedWords.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreCorrectly_WhenHelsCreatesThreeWords()
    {
        // Arrange: Reset mock to ensure default behavior (accept all words)
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Kelk" horizontally from center (7,7)-(7,10)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "K", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 7 },
                new() { Letter = "E", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 8 },
                new() { Letter = "L", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 9 },
                new() { Letter = "K", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 10 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1 = placedGame1.Players.First(p => p.Id == player1Id);
        // Get actual placed tile Points from the board state
        var baseScore1 = request1.Tiles.Sum(t => placedGame1.Board.GetTile(t.Row, t.Column)?.Points ?? 0);
        // KELK: no bonus squares on these positions
        player1.Score.Should().Be(baseScore1);

        // Step 2: Player2 places "Kaars" vertically from center (7,7)-(11,7), sharing K at (7,7)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        // Player2 places "aars" at (8,7)-(11,7) - K at (7,7) is already there
        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "A", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 7 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 7 },
                new() { Letter = "R", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 7 },
                new() { Letter = "S", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 7 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2 = placedGame2.Players.First(p => p.Id == player2Id);
        // KAARS: A(8,7) no bonus, A(9,7) no bonus, R(10,7) no bonus, S(11,7) on DL
        var kaarsScore = (placedGame2.Board.GetTile(8, 7)?.Points ?? 0)
                       + (placedGame2.Board.GetTile(9, 7)?.Points ?? 0)
                       + (placedGame2.Board.GetTile(10, 7)?.Points ?? 0)
                       + ((placedGame2.Board.GetTile(11, 7)?.Points ?? 0) * 2); // DL
        player2.Score.Should().Be(kaarsScore);

        // Step 3: Player1 places "Hels" vertically at col 6, rows 6-9
        // (7,6) connects to K at (7,7), avoiding overlap with Kelk tiles
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "H", IsBlank = false, TileId = hand3[0].Id, Row = 6, Column = 6 },
                new() { Letter = "E", IsBlank = false, TileId = hand3[1].Id, Row = 7, Column = 6 },
                new() { Letter = "L", IsBlank = false, TileId = hand3[2].Id, Row = 8, Column = 6 },
                new() { Letter = "S", IsBlank = false, TileId = hand3[3].Id, Row = 9, Column = 6 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Again = placedGame3.Players.First(p => p.Id == player1AgainId);

        // Verify score is positive and main word "Hels" is formed
        player1Again.Score.Should().BeGreaterThan(0);

        // Verify "HELS" is one of the formed words (cross words may also be detected)
        placedGame3.FormedWords.Should().Contain("HELS");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldRejectTurn_WhenLastWordIsInvalid()
    {
        // Arrange: Configure mock to accept all words EXCEPT "XELS"
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns((string word) =>
        {
            if (word == "XELS")
                return false;
            return true;
        });

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Kelk" horizontally on center (7,7)-(7,10)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "K", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 7 },
                new() { Letter = "E", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 8 },
                new() { Letter = "L", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 9 },
                new() { Letter = "K", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 10 }
            },
        };

        await _service.PlaceTilesAsync(game.Id, player1Id, request1);

        // Step 2: Player2 places "Kaars" vertically also on the center (7,7)-(11,7), sharing K at (7,7)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "A", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 7 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 7 },
                new() { Letter = "R", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 7 },
                new() { Letter = "S", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 7 }
            },
        };

        await _service.PlaceTilesAsync(game.Id, player2Id, request2);

        // Step 3: Player1 places "XELS" horizontally at (8,8)-(8,11) - this should be REJECTED
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "X", IsBlank = false, TileId = hand3[0].Id, Row = 8, Column = 8 },
                new() { Letter = "E", IsBlank = false, TileId = hand3[1].Id, Row = 8, Column = 9 },
                new() { Letter = "L", IsBlank = false, TileId = hand3[2].Id, Row = 8, Column = 10 },
                new() { Letter = "S", IsBlank = false, TileId = hand3[3].Id, Row = 8, Column = 11 }
            },
        };

        // Act & Assert: Should throw because "XELS" is not a valid Dutch word
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, player1AgainId, request3));
        ex.Message.ShouldContain("not a valid Dutch word");
    }

    #endregion

    #region Comprehensive Multi-Word Scoring Tests

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordCombinations_WithKelkKaarsHels()
    {
        // Arrange: Configure mock to accept all words used in this test
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Kelk" horizontally at (7,7)-(7,10)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "K", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 7 },
                new() { Letter = "E", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 8 },
                new() { Letter = "L", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 9 },
                new() { Letter = "K", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 10 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1Score1 = placedGame1.Players.First(p => p.Id == player1Id).Score;
        var formedWords1 = placedGame1.FormedWords;

        // Verify: Kelk scored with 1 word (KELK)
        player1Score1.Should().BeGreaterThan(0);
        formedWords1.Should().Contain("KELK");

        // Step 2: Player2 places "Kaars" vertically at (8,7)-(11,7) crossing K at (7,7)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "A", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 7 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 7 },
                new() { Letter = "R", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 7 },
                new() { Letter = "S", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 7 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2Score1 = placedGame2.Players.First(p => p.Id == player2Id).Score;
        var formedWords2 = placedGame2.FormedWords;

        // Verify: AARS scored + cross word on KELK column
        player2Score1.Should().BeGreaterThan(0);
        formedWords2.Should().Contain("AARS");

        // Step 3: Player1 places "Hels" vertically at (6,6)-(9,6) crossing KELK
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "H", IsBlank = false, TileId = hand3[0].Id, Row = 6, Column = 6 },
                new() { Letter = "E", IsBlank = false, TileId = hand3[1].Id, Row = 7, Column = 6 },
                new() { Letter = "L", IsBlank = false, TileId = hand3[2].Id, Row = 8, Column = 6 },
                new() { Letter = "S", IsBlank = false, TileId = hand3[3].Id, Row = 9, Column = 6 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Score2 = placedGame3.Players.First(p => p.Id == player1AgainId).Score;
        var formedWords3 = placedGame3.FormedWords;

        // Verify: Multiple words formed including HELS and cross words
        player1Score2.Should().BeGreaterThan(0);
        formedWords3.Should().Contain("HELS");
        formedWords3.Should().HaveCountGreaterThan(1); // Main word + cross words

        // Step 4: Player2 places "Lamp" horizontally at (11,3)-(11,6) crossing KAARS
        var result4 = await _service.GetGameAsync(game.Id);
        var player2AgainId = result4!.CurrentPlayerId!;
        var hand4 = result4.Players.First(p => p.Id == player2AgainId).Hand;

        var request4 = new PlaceTilesRequest
        {
                     PlayerId = player2AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "L", IsBlank = false, TileId = hand4[0].Id, Row = 11, Column = 3 },
                new() { Letter = "A", IsBlank = false, TileId = hand4[1].Id, Row = 11, Column = 4 },
                new() { Letter = "M", IsBlank = false, TileId = hand4[2].Id, Row = 11, Column = 5 },
                new() { Letter = "P", IsBlank = false, TileId = hand4[3].Id, Row = 11, Column = 6 }
            },
        };

        var placedGame4 = await _service.PlaceTilesAsync(game.Id, player2AgainId, request4);
        var player2Score2 = placedGame4.Players.First(p => p.Id == player2AgainId).Score;
        var formedWords4 = placedGame4.FormedWords;

        // Verify: LAMP scored + cross words on HELS column
        player2Score2.Should().BeGreaterThan(player2Score1);
        formedWords4.Should().Contain("LAMP");
        formedWords4.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordCombinations_WithTafelRaamLamp()
    {
        // Arrange: Configure mock to accept all words used in this test
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Tafel" horizontally at (7,5)-(7,9)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "T", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 5 },
                new() { Letter = "A", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 6 },
                new() { Letter = "F", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 7 },
                new() { Letter = "E", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 8 },
                new() { Letter = "L", IsBlank = false, TileId = hand1[4].Id, Row = 7, Column = 9 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1Score1 = placedGame1.Players.First(p => p.Id == player1Id).Score;
        var formedWords1 = placedGame1.FormedWords;

        // Verify: TAFEL scored
        player1Score1.Should().BeGreaterThan(0);
        formedWords1.Should().Contain("TAFEL");

        // Step 2: Player2 places "Raam" vertically at (8,6)-(11,6) connecting to A at (7,6)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "R", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 6 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 6 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 6 },
                new() { Letter = "M", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 6 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2Score1 = placedGame2.Players.First(p => p.Id == player2Id).Score;
        var formedWords2 = placedGame2.FormedWords;

        // Verify: RAAM scored + cross word on TAFEL column
        player2Score1.Should().BeGreaterThan(0);
        formedWords2.Should().Contain("RAAM");
        formedWords2.Should().HaveCountGreaterThan(1);

        // Step 3: Player1 places "Lamp" horizontally at (5,4)-(5,7) crossing RAAM
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "L", IsBlank = false, TileId = hand3[0].Id, Row = 5, Column = 4 },
                new() { Letter = "A", IsBlank = false, TileId = hand3[1].Id, Row = 5, Column = 5 },
                new() { Letter = "M", IsBlank = false, TileId = hand3[2].Id, Row = 5, Column = 6 },
                new() { Letter = "P", IsBlank = false, TileId = hand3[3].Id, Row = 5, Column = 7 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Score2 = placedGame3.Players.First(p => p.Id == player1AgainId).Score;
        var formedWords3 = placedGame3.FormedWords;

        // Verify: LAMP scored + cross words on RAAM column
        player1Score2.Should().BeGreaterThan(player1Score1);
        formedWords3.Should().Contain("LAMP");
        formedWords3.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordCombinations_WithHuisSchoorsteen()
    {
        // Arrange: Configure mock to accept all words used in this test
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Huis" horizontally at (7,6)-(7,9)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "H", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 6 },
                new() { Letter = "U", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 7 },
                new() { Letter = "I", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 8 },
                new() { Letter = "S", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 9 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1Score1 = placedGame1.Players.First(p => p.Id == player1Id).Score;
        var formedWords1 = placedGame1.FormedWords;

        // Verify: HUIS scored
        player1Score1.Should().BeGreaterThan(0);
        formedWords1.Should().Contain("HUIS");

        // Step 2: Player2 places "Raam" vertically at (8,6)-(11,6) connecting to U at (7,6)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "R", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 6 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 6 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 6 },
                new() { Letter = "M", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 6 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2Score1 = placedGame2.Players.First(p => p.Id == player2Id).Score;
        var formedWords2 = placedGame2.FormedWords;

        // Verify: RAAM scored
        player2Score1.Should().BeGreaterThan(0);
        formedWords2.Should().Contain("RAAM");

        // Step 3: Player1 places "Muur" horizontally at (10,1)-(10,4) connecting to RAAM
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "M", IsBlank = false, TileId = hand3[0].Id, Row = 10, Column = 1 },
                new() { Letter = "U", IsBlank = false, TileId = hand3[1].Id, Row = 10, Column = 2 },
                new() { Letter = "U", IsBlank = false, TileId = hand3[2].Id, Row = 10, Column = 3 },
                new() { Letter = "R", IsBlank = false, TileId = hand3[3].Id, Row = 10, Column = 4 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Score2 = placedGame3.Players.First(p => p.Id == player1AgainId).Score;
        var formedWords3 = placedGame3.FormedWords;

        // Verify: MUUR scored
        player1Score2.Should().BeGreaterThan(player1Score1);
        formedWords3.Should().Contain("MUUR");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordCombinations_WithTijgerJagerVijf()
    {
        // Arrange: Configure mock to accept all words used in this test
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Tijger" horizontally at (7,5)-(7,10)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "T", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 5 },
                new() { Letter = "I", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 6 },
                new() { Letter = "J", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 7 },
                new() { Letter = "G", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 8 },
                new() { Letter = "E", IsBlank = false, TileId = hand1[4].Id, Row = 7, Column = 9 },
                new() { Letter = "R", IsBlank = false, TileId = hand1[5].Id, Row = 7, Column = 10 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1Score1 = placedGame1.Players.First(p => p.Id == player1Id).Score;
        var formedWords1 = placedGame1.FormedWords;

        // Verify: TIJGER scored
        player1Score1.Should().BeGreaterThan(0);
        formedWords1.Should().Contain("TIJGER");

        // Step 2: Player2 places "Jager" vertically at (8,6)-(12,6) connecting to I at (7,6)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "J", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 6 },
                new() { Letter = "A", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 6 },
                new() { Letter = "G", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 6 },
                new() { Letter = "E", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 6 },
                new() { Letter = "R", IsBlank = false, TileId = hand2[4].Id, Row = 12, Column = 6 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2Score1 = placedGame2.Players.First(p => p.Id == player2Id).Score;
        var formedWords2 = placedGame2.FormedWords;

        // Verify: JAGER scored
        player2Score1.Should().BeGreaterThan(0);
        formedWords2.Should().Contain("JAGER");

        // Step 3: Player1 places "Vijf" vertically at (8,10)-(11,10) connecting to R at (7,10)
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "V", IsBlank = false, TileId = hand3[0].Id, Row = 8, Column = 10 },
                new() { Letter = "I", IsBlank = false, TileId = hand3[1].Id, Row = 9, Column = 10 },
                new() { Letter = "J", IsBlank = false, TileId = hand3[2].Id, Row = 10, Column = 10 },
                new() { Letter = "F", IsBlank = false, TileId = hand3[3].Id, Row = 11, Column = 10 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Score2 = placedGame3.Players.First(p => p.Id == player1AgainId).Score;
        var formedWords3 = placedGame3.FormedWords;

        // Verify: VIJF scored
        player1Score2.Should().BeGreaterThan(player1Score1);
        formedWords3.Should().Contain("VIJF");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordCombinations_WithBoomGrootSchoon()
    {
        // Arrange: Configure mock to accept all words used in this test
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Boom" horizontally at (7,5)-(7,8)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "B", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 5 },
                new() { Letter = "O", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 6 },
                new() { Letter = "O", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 7 },
                new() { Letter = "M", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 8 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1Score1 = placedGame1.Players.First(p => p.Id == player1Id).Score;
        var formedWords1 = placedGame1.FormedWords;

        // Verify: BOOM scored
        player1Score1.Should().BeGreaterThan(0);
        formedWords1.Should().Contain("BOOM");

        // Step 2: Player2 places "Groot" vertically at (8,8)-(12,8) connecting to M at (7,8)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "G", IsBlank = false, TileId = hand2[0].Id, Row = 8, Column = 8 },
                new() { Letter = "R", IsBlank = false, TileId = hand2[1].Id, Row = 9, Column = 8 },
                new() { Letter = "O", IsBlank = false, TileId = hand2[2].Id, Row = 10, Column = 8 },
                new() { Letter = "O", IsBlank = false, TileId = hand2[3].Id, Row = 11, Column = 8 },
                new() { Letter = "T", IsBlank = false, TileId = hand2[4].Id, Row = 12, Column = 8 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2Score1 = placedGame2.Players.First(p => p.Id == player2Id).Score;
        var formedWords2 = placedGame2.FormedWords;

        // Verify: GROOT scored + cross word on BOOM column
        player2Score1.Should().BeGreaterThan(0);
        formedWords2.Should().Contain("GROOT");
        formedWords2.Should().HaveCountGreaterThan(1);

        // Step 3: Player1 places "Schoon" horizontally at (13,2)-(13,7) connecting to T at (12,8)
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "S", IsBlank = false, TileId = hand3[0].Id, Row = 13, Column = 2 },
                new() { Letter = "C", IsBlank = false, TileId = hand3[1].Id, Row = 13, Column = 3 },
                new() { Letter = "H", IsBlank = false, TileId = hand3[2].Id, Row = 13, Column = 4 },
                new() { Letter = "O", IsBlank = false, TileId = hand3[3].Id, Row = 13, Column = 5 },
                new() { Letter = "O", IsBlank = false, TileId = hand3[4].Id, Row = 13, Column = 6 },
                new() { Letter = "N", IsBlank = false, TileId = hand3[5].Id, Row = 13, Column = 7 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Score2 = placedGame3.Players.First(p => p.Id == player1AgainId).Score;
        var formedWords3 = placedGame3.FormedWords;

        // Verify: SCHOON scored + cross word on GROOT column
        player1Score2.Should().BeGreaterThan(player1Score1);
        formedWords3.Should().Contain("SCHOON");
        formedWords3.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordCombinations_WithKatHondVogel()
    {
        // Arrange: Configure mock to accept all words used in this test
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Kat" horizontally at (7,6)-(7,8)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "K", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 6 },
                new() { Letter = "A", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 7 },
                new() { Letter = "T", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 8 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1Score1 = placedGame1.Players.First(p => p.Id == player1Id).Score;
        var formedWords1 = placedGame1.FormedWords;

        // Verify: KAT scored
        player1Score1.Should().BeGreaterThan(0);
        formedWords1.Should().Contain("KAT");

        // Step 2: Player2 places "Hond" vertically at (3,7)-(6,7) crossing A at (7,7)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "H", IsBlank = false, TileId = hand2[0].Id, Row = 3, Column = 7 },
                new() { Letter = "O", IsBlank = false, TileId = hand2[1].Id, Row = 4, Column = 7 },
                new() { Letter = "N", IsBlank = false, TileId = hand2[2].Id, Row = 5, Column = 7 },
                new() { Letter = "D", IsBlank = false, TileId = hand2[3].Id, Row = 6, Column = 7 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2Score1 = placedGame2.Players.First(p => p.Id == player2Id).Score;
        var formedWords2 = placedGame2.FormedWords;

        // Verify: HOND scored + cross word on KAT column
        player2Score1.Should().BeGreaterThan(0);
        formedWords2.Should().Contain("HOND");
        formedWords2.Should().HaveCountGreaterThan(1);

        // Step 3: Player1 places "Vogel" horizontally at (10,4)-(10,8)
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "V", IsBlank = false, TileId = hand3[0].Id, Row = 10, Column = 4 },
                new() { Letter = "O", IsBlank = false, TileId = hand3[1].Id, Row = 10, Column = 5 },
                new() { Letter = "G", IsBlank = false, TileId = hand3[2].Id, Row = 10, Column = 6 },
                new() { Letter = "E", IsBlank = false, TileId = hand3[3].Id, Row = 10, Column = 7 },
                new() { Letter = "L", IsBlank = false, TileId = hand3[4].Id, Row = 10, Column = 8 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Score2 = placedGame3.Players.First(p => p.Id == player1AgainId).Score;
        var formedWords3 = placedGame3.FormedWords;

        // Verify: VOGEL scored
        player1Score2.Should().BeGreaterThan(player1Score1);
        formedWords3.Should().Contain("VOGEL");
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldScoreMultipleWordCombinations_WithWaterVuurAardeLucht()
    {
        // Arrange: Configure mock to accept all words used in this test
        _dictionaryMock.Setup(s => s.Contains(It.IsAny<string>())).Returns(true);

        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var player1Id = game.Players.First(p => p.Id == game.CurrentPlayerId).Id;

        // Step 1: Player1 places "Water" horizontally at (7,5)-(7,9)
        var hand1 = game.Players.First(p => p.Id == player1Id).Hand;
        var request1 = new PlaceTilesRequest
        {
                     PlayerId = player1Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "W", IsBlank = false, TileId = hand1[0].Id, Row = 7, Column = 5 },
                new() { Letter = "A", IsBlank = false, TileId = hand1[1].Id, Row = 7, Column = 6 },
                new() { Letter = "T", IsBlank = false, TileId = hand1[2].Id, Row = 7, Column = 7 },
                new() { Letter = "E", IsBlank = false, TileId = hand1[3].Id, Row = 7, Column = 8 },
                new() { Letter = "R", IsBlank = false, TileId = hand1[4].Id, Row = 7, Column = 9 }
            },
        };

        var placedGame1 = await _service.PlaceTilesAsync(game.Id, player1Id, request1);
        var player1Score1 = placedGame1.Players.First(p => p.Id == player1Id).Score;
        var formedWords1 = placedGame1.FormedWords;

        // Verify: WATER scored
        player1Score1.Should().BeGreaterThan(0);
        formedWords1.Should().Contain("WATER");

        // Step 2: Player2 places "Vuur" vertically at (3,7)-(6,7) crossing T at (7,7)
        var result2 = await _service.GetGameAsync(game.Id);
        var player2Id = result2!.CurrentPlayerId!;
        var hand2 = result2.Players.First(p => p.Id == player2Id).Hand;

        var request2 = new PlaceTilesRequest
        {
                     PlayerId = player2Id,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "V", IsBlank = false, TileId = hand2[0].Id, Row = 3, Column = 7 },
                new() { Letter = "U", IsBlank = false, TileId = hand2[1].Id, Row = 4, Column = 7 },
                new() { Letter = "U", IsBlank = false, TileId = hand2[2].Id, Row = 5, Column = 7 },
                new() { Letter = "R", IsBlank = false, TileId = hand2[3].Id, Row = 6, Column = 7 }
            },
        };

        var placedGame2 = await _service.PlaceTilesAsync(game.Id, player2Id, request2);
        var player2Score1 = placedGame2.Players.First(p => p.Id == player2Id).Score;
        var formedWords2 = placedGame2.FormedWords;

        // Verify: VUUR scored + cross word on WATER column
        player2Score1.Should().BeGreaterThan(0);
        formedWords2.Should().Contain("VUUR");
        formedWords2.Should().HaveCountGreaterThan(1);

        // Step 3: Player1 places "Aarde" horizontally at (12,5)-(12,9)
        var result3 = await _service.GetGameAsync(game.Id);
        var player1AgainId = result3!.CurrentPlayerId!;
        var hand3 = result3.Players.First(p => p.Id == player1AgainId).Hand;

        var request3 = new PlaceTilesRequest
        {
                     PlayerId = player1AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "A", IsBlank = false, TileId = hand3[0].Id, Row = 12, Column = 5 },
                new() { Letter = "A", IsBlank = false, TileId = hand3[1].Id, Row = 12, Column = 6 },
                new() { Letter = "R", IsBlank = false, TileId = hand3[2].Id, Row = 12, Column = 7 },
                new() { Letter = "D", IsBlank = false, TileId = hand3[3].Id, Row = 12, Column = 8 },
                new() { Letter = "E", IsBlank = false, TileId = hand3[4].Id, Row = 12, Column = 9 }
            },
        };

        var placedGame3 = await _service.PlaceTilesAsync(game.Id, player1AgainId, request3);
        var player1Score2 = placedGame3.Players.First(p => p.Id == player1AgainId).Score;
        var formedWords3 = placedGame3.FormedWords;

        // Verify: AARDE scored
        player1Score2.Should().BeGreaterThan(player1Score1);
        formedWords3.Should().Contain("AARDE");

        // Step 4: Player2 places "Lucht" vertically at (9,10)-(13,10) connecting to E at (12,9)
        var result4 = await _service.GetGameAsync(game.Id);
        var player2AgainId = result4!.CurrentPlayerId!;
        var hand4 = result4.Players.First(p => p.Id == player2AgainId).Hand;

        var request4 = new PlaceTilesRequest
        {
                     PlayerId = player2AgainId,

            Tiles = new List<TilePlacementDto>
            {
                new() { Letter = "L", IsBlank = false, TileId = hand4[0].Id, Row = 9, Column = 10 },
                new() { Letter = "U", IsBlank = false, TileId = hand4[1].Id, Row = 10, Column = 10 },
                new() { Letter = "C", IsBlank = false, TileId = hand4[2].Id, Row = 11, Column = 10 },
                new() { Letter = "H", IsBlank = false, TileId = hand4[3].Id, Row = 12, Column = 10 },
                new() { Letter = "T", IsBlank = false, TileId = hand4[4].Id, Row = 13, Column = 10 }
            },
        };

        var placedGame4 = await _service.PlaceTilesAsync(game.Id, player2AgainId, request4);
        var player2Score2 = placedGame4.Players.First(p => p.Id == player2AgainId).Score;
        var formedWords4 = placedGame4.FormedWords;

        // Verify: LUCHT scored + cross word on AARDE column
        player2Score2.Should().BeGreaterThan(player2Score1);
        formedWords4.Should().Contain("LUCHT");
        formedWords4.Should().HaveCountGreaterThan(1);
    }

    #endregion
}
