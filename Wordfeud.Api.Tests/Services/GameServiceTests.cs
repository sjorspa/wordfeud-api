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
        game.CurrentPlayerId.Should().BeNull();
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
        result.TileBag.Should().HaveCount(83); // 102 - 7 (create) - 12 (join)
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.Board[7, 7].Should().NotBeNull();
        placedGame.Players.First(p => p.Id == playerId).Hand.Should().HaveCount(6);
        placedGame.ConsecutivePasses.Should().Be(0);
        placedGame.MoveNumber.Should().Be(1);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldThrowWhenGameNotFound()
    {
        // Arrange
        var request = new PlaceTilesRequest
        {
            Tiles = new List<TilePlacementDto>(),
            StartRow = 0,
            StartColumn = 0,
            Direction = 0
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
            Tiles = new List<TilePlacementDto>(),
            StartRow = 0,
            StartColumn = 0,
            Direction = 0
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
            Tiles = new List<TilePlacementDto>(),
            StartRow = 0,
            StartColumn = 0,
            Direction = 0
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
            Tiles = new List<TilePlacementDto>(),
            StartRow = 0,
            StartColumn = 0,
            Direction = 0
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };
        await _service.PlaceTilesAsync(game.Id, playerId, firstRequest);

        // After first placement, turn passes to the other player
        var updatedGame = await _service.GetGameAsync(game.Id);
        var currentPlayerId = updatedGame.CurrentPlayerId!;
        var currentHand = updatedGame.Players.First(p => p.Id == currentPlayerId).Hand;

        // Try to place at same position (now with the player who has the turn)
        var secondRequest = new PlaceTilesRequest
        {
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
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
            StartRow = 7,
            StartColumn = 8,
            Direction = 0
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
            StartRow = 7,
            StartColumn = 8,
            Direction = 0
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("Blank tile must have a letter");
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
            StartRow = 0,
            StartColumn = 0,
            Direction = 0
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };
        await _service.PlaceTilesAsync(game.Id, playerId, firstRequest);

        // Get next player's turn
        result = await _service.GetGameAsync(game.Id);
        var nextPlayerId = result!.CurrentPlayerId!;
        var nextHand = result.Players.First(p => p.Id == nextPlayerId).Hand;

        // Place tiles far from center (not connecting)
        var secondRequest = new PlaceTilesRequest
        {
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
            StartRow = 0,
            StartColumn = 0,
            Direction = 0
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

        // Mock dictionary to reject a specific word
        _dictionaryMock.Setup(s => s.Contains("XYZ")).Returns(false);

        var request = new PlaceTilesRequest
        {
            Tiles = new List<TilePlacementDto>
            {
                new()
                {
                    Letter = "X",
                    IsBlank = false,
                    TileId = hand[0].Id,
                    Row = 7,
                    Column = 7
                },
                new()
                {
                    Letter = "Y",
                    IsBlank = false,
                    TileId = hand[1].Id,
                    Row = 7,
                    Column = 8
                },
                new()
                {
                    Letter = "Z",
                    IsBlank = false,
                    TileId = hand[2].Id,
                    Row = 7,
                    Column = 9
                }
            },
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () => await _service.PlaceTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("not a valid Dutch word");
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 1
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);

        // Assert
        placedGame.CurrentPlayerId.Should().NotBe(playerId);
    }

    [Fact]
    public async Task PlaceTilesAsync_ShouldAddScoreToPlayer()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var hand = result.Players.First(p => p.Id == playerId).Hand;

        var request = new PlaceTilesRequest
        {
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
        };

        // Act
        var placedGame = await _service.PlaceTilesAsync(game.Id, playerId, request);
        var updatedPlayer = placedGame.Players.First(p => p.Id == playerId);

        // Assert - E is worth 1 point, placed on center (no bonus)
        updatedPlayer.Score.Should().BeGreaterOrEqualTo(1);
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
            StartRow = 7,
            StartColumn = 7,
            Direction = 0
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
        var request = new SwapTilesRequest { TileIds = new List<string> { "tile-id" } };

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
        var request = new SwapTilesRequest { TileIds = new List<string> { "tile-id" } };

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
        var request = new SwapTilesRequest { TileIds = new List<string> { "tile-id" } };

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () => await _service.SwapTilesAsync(game.Id, wrongPlayerId, request));
    }

    [Fact]
    public async Task SwapTilesAsync_ShouldThrowWhenNotEnoughTilesInBag()
    {
        // Arrange
        var game = await _service.CreateGameAsync("Player1");
        await _service.JoinGameAsync(game.Id, "Player2");
        var result = await _service.GetGameAsync(game.Id);
        var playerId = result!.CurrentPlayerId!;
        var player = result.Players.First(p => p.Id == playerId);

        // Try to swap more tiles than are in the bag
        var tileIds = player.Hand.Select(t => t.Id).ToList();
        var request = new SwapTilesRequest { TileIds = tileIds };

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(async () => await _service.SwapTilesAsync(game.Id, playerId, request));
        ex.Message.ShouldContain("Not enough tiles");
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

        var request = new SwapTilesRequest { TileIds = new List<string> { player.Hand[0].Id } };

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

        var request = new SwapTilesRequest { TileIds = new List<string> { result.Players.First(p => p.Id == playerId).Hand[0].Id } };

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
        result.Board.Should().NotBeNull();
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
        for (var r = 0; r < 15; r++)
            for (var c = 0; c < 15; c++)
                result!.Board[r, c].Should().BeNull();
    }

    #endregion
}
