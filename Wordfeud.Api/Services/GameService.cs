using Wordfeud.Api.Interfaces;
using Wordfeud.Api.Models;
using Wordfeud.Api.Data;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Wordfeud.Api.Services;

/// <summary>
/// Core game service implementing Wordfeud business logic.
/// </summary>
public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IDutchDictionaryService _dictionaryService;
    private readonly ILogger<GameService> _logger;

    /// <summary>
    /// Creates a new GameService.
    /// </summary>
    public GameService(IGameRepository gameRepository, IDutchDictionaryService dictionaryService, ILogger<GameService> logger)
    {
        _gameRepository = gameRepository;
        _dictionaryService = dictionaryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Game> CreateGameAsync(string playerName)
    {
        _logger.LogInformation("Creating new game with player '{PlayerName}'", playerName);

        var game = new Game
        {
            Status = GameStatus.Waiting,
            Players = new List<Player>
            {
                new Player
                {
                    Name = playerName,
                    Hand = new List<Tile>(),
                    Score = 0
                }
            },
            TileBag = BoardConfiguration.CreateTileBag(),
            ConsecutivePasses = 0,
            MoveNumber = 0
        };

        // Deal 7 tiles to the first player
        game.Players[0].Hand = DrawTiles(game, 7);
        game.Players[0].TilesDrawn = 7;

        // Set the creator as the first player (current player)
        game.CurrentPlayerId = game.Players[0].Id;

        // Persist to database
        var entity = GameEntity.FromGame(game);
        await _gameRepository.CreateAsync(entity);

        _logger.LogInformation("Game {GameId} created with player '{PlayerName}'", game.Id, playerName);
        return game;
    }

    /// <inheritdoc />
    public async Task<Game> JoinGameAsync(string gameId, string playerName)
    {
        _logger.LogInformation("Player '{PlayerName}' joining game {GameId}", playerName, gameId);

        var entity = await _gameRepository.GetByIdAsync(gameId);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Game '{gameId}' not found.");
        }

        var game = entity.ToGame();

        if (game.Status != GameStatus.Waiting)
        {
            throw new InvalidOperationException("Game has already started and cannot accept new players.");
        }

        if (game.Players.Count >= 2)
        {
            throw new InvalidOperationException("Game is full (max 2 players).");
        }

        var secondPlayer = new Player
        {
            Name = playerName,
            Hand = new List<Tile>(),
            Score = 0
        };

        game.Players.Add(secondPlayer);
        game.Status = GameStatus.InProgress;
        game.CurrentPlayerId = game.Players[0].Id;
        game.Players[1].Hand = DrawTiles(game, 7);
        game.Players[1].TilesDrawn = 7;
        game.UpdatedAt = DateTime.UtcNow;

        // Persist to database
        entity = GameEntity.FromGame(game);
        await _gameRepository.UpdateAsync(entity);

        _logger.LogInformation("Player '{PlayerName}' joined game {GameId}", playerName, gameId);
        return game;
    }

    /// <inheritdoc />
    public async Task<Game?> GetGameAsync(string gameId)
    {
        var entity = await _gameRepository.GetByIdAsync(gameId);

        if (entity == null)
        {
            _logger.LogWarning("Game {GameId} not found", gameId);
            return null;
        }

        var game = entity.ToGame();
        return game;
    }

    /// <inheritdoc />
    public async Task<Game> PlaceTilesAsync(string gameId, string playerId, PlaceTilesRequest request)
    {
        _logger.LogInformation("Player {PlayerId} placing {TileCount} tiles in game {GameId}",
            playerId, request.Tiles?.Count ?? 0, gameId);

        var entity = await _gameRepository.GetByIdAsync(gameId);
        if (entity == null)
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        var game = entity.ToGame();

        if (game.Status == GameStatus.Finished)
            throw new InvalidOperationException("Game is already finished.");

        if (game.CurrentPlayerId != playerId)
            throw new UnauthorizedAccessException("It is not your turn.");

        if (request.Tiles == null || request.Tiles.Count == 0)
            throw new ArgumentException("At least one tile must be placed.");

        // Validate board bounds
        foreach (var tileDto in request.Tiles)
        {
            if (tileDto.Row < 0 || tileDto.Row >= BoardConfiguration.BoardSize ||
                tileDto.Column < 0 || tileDto.Column >= BoardConfiguration.BoardSize)
            {
                throw new ArgumentException($"Position ({tileDto.Row}, {tileDto.Column}) is out of bounds.");
            }
        }

        // Check that all positions are empty
        foreach (var tileDto in request.Tiles)
        {
            if (game.Board[tileDto.Row, tileDto.Column] != null)
                throw new InvalidOperationException($"Position ({tileDto.Row}, {tileDto.Column}) is already occupied.");
        }

        // Verify tiles are in the player's hand
        var player = game.Players.First(p => p.Id == playerId);
        var missingTiles = request.Tiles
            .Where(dto => !player.Hand.Any(t => t.Id == dto.TileId))
            .ToList();

        if (missingTiles.Any())
            throw new ArgumentException("One or more tiles are not in your hand.");

        // Validate blank assignments
        foreach (var tileDto in request.Tiles.Where(t => t.IsBlank))
        {
            if (string.IsNullOrEmpty(tileDto.Letter) || tileDto.Letter.Length != 1 || !char.IsLetter(tileDto.Letter[0]))
                throw new ArgumentException("Blank tile must have a single letter assigned.");
        }

        // Validate BlankAssignments dictionary keys match tile IDs
        if (request.BlankAssignments != null)
        {
            var tileIds = request.Tiles.Where(t => t.IsBlank).Select(t => t.TileId).ToHashSet();
            foreach (var kvp in request.BlankAssignments)
            {
                if (!tileIds.Contains(kvp.Key))
                    throw new ArgumentException($"Blank assignment key '{kvp.Key}' does not match any placed tile.");
                if (kvp.Value.Length != 1 || !char.IsLetter(kvp.Value[0]))
                    throw new ArgumentException($"Blank assignment for tile '{kvp.Key}' must be a single letter.");
            }
        }

        // Apply blank assignments to the tiles in hand
        if (request.BlankAssignments != null)
        {
            foreach (var kvp in request.BlankAssignments)
            {
                var handTile = player.Hand.FirstOrDefault(t => t.Id == kvp.Key);
                if (handTile != null && handTile.IsBlank)
                {
                    handTile.BlankRepresentation = kvp.Value.ToUpperInvariant();
                    handTile.Letter = kvp.Value.ToUpperInvariant();
                }
            }
        }

        // Validate the placement
        var validationResult = ValidatePlacement(game, player, request);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.ErrorMessage);

        // Place the tiles
        foreach (var tileDto in request.Tiles)
        {
            var handTile = player.Hand.First(t => t.Id == tileDto.TileId);
            game.Board[tileDto.Row, tileDto.Column] = handTile;
            player.Hand.Remove(handTile);
        }

        // Calculate score
        var direction = DeriveDirection(request.Tiles);
        var scoreResult = CalculateScore(game, request, direction);
        player.Score += scoreResult.TotalScore;

        // Draw new tiles
        var tilesDrawn = Math.Min(7 - player.Hand.Count, game.TileBag.Count);
        if (tilesDrawn > 0)
        {
            var newTiles = DrawTiles(game, tilesDrawn);
            player.Hand.AddRange(newTiles);
            player.TilesDrawn += tilesDrawn;
        }

        // Track formed words
        game.FormedWords.AddRange(scoreResult.FormedWords);

        // Update consecutive passes
        game.ConsecutivePasses = 0;
        game.MoveNumber++;
        game.CurrentPlayerId = GetNextPlayerId(game, playerId);
        game.UpdatedAt = DateTime.UtcNow;

        // Check for game end conditions
        CheckGameEnd(game);

        // Record move history
        game.MoveHistory.Add(new MoveHistory
        {
            MoveNumber = game.MoveNumber,
            PlayerId = playerId,
            PlayerName = player.Name,
            ActionType = "place",
            Word = scoreResult.FormedWords.Count > 0 ? string.Join(", ", scoreResult.FormedWords) : null,
            Score = scoreResult.TotalScore,
            Tiles = request.Tiles.Select(t => new MoveTileDto
            {
                TileId = t.TileId,
                Letter = t.Letter,
                Row = t.Row,
                Column = t.Column
            }).ToList(),
            Timestamp = DateTime.UtcNow
        });

        // Persist to database
        entity = GameEntity.FromGame(game);
        await _gameRepository.UpdateAsync(entity);

        _logger.LogInformation("Player {PlayerId} scored {Score} points in game {GameId}",
            playerId, scoreResult.TotalScore, gameId);

        return game;
    }

    /// <inheritdoc />
    public async Task<GameScoresDto> GetScoresAsync(string gameId)
    {
        var entity = await _gameRepository.GetByIdAsync(gameId);
        if (entity == null)
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        var game = entity.ToGame();

        // Calculate final scores if game is finished
        if (game.Status == GameStatus.Finished)
        {
            CalculateFinalScores(game);
        }

        var dto = new GameScoresDto
        {
            Id = game.Id,
            Status = game.Status.ToString(),
            Players = game.Players.Select(p => new PlayerScoreDto
            {
                Id = p.Id,
                Name = p.Name,
                Score = p.Score,
                TilesDrawn = p.TilesDrawn
            }).ToList()
        };

        return dto;
    }

    /// <inheritdoc />
    public async Task<BoardStateDto> GetBoardAsync(string gameId)
    {
        var entity = await _gameRepository.GetByIdAsync(gameId);
        if (entity == null)
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        var game = entity.ToGame();

        var board = game.Board;
        var tiles = new List<BoardTileDto>();

        for (var row = 0; row < board.Size; row++)
        {
            for (var col = 0; col < board.Size; col++)
            {
                var tile = board.GetTile(row, col);
                if (tile != null)
                {
                    tiles.Add(new BoardTileDto
                    {
                        Row = row,
                        Column = col,
                        Letter = tile.Letter,
                        IsBlank = tile.IsBlank,
                        BonusType = Data.BoardConfiguration.GetBonusType(row, col).ToString()
                    });
                }
            }
        }

        var dto = new BoardStateDto
        {
            Id = gameId,
            Size = board.Size,
            Tiles = tiles
        };

        return dto;
    }

    /// <inheritdoc />
    public async Task<Game> PassTurnAsync(string gameId, string playerId)
    {
        _logger.LogInformation("Player {PlayerId} passing turn in game {GameId}", playerId, gameId);

        var entity = await _gameRepository.GetByIdAsync(gameId);
        if (entity == null)
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        var game = entity.ToGame();

        if (game.Status == GameStatus.Finished)
            throw new InvalidOperationException("Game is already finished.");

        if (game.CurrentPlayerId != playerId)
            throw new UnauthorizedAccessException("It is not your turn.");

        game.ConsecutivePasses++;
        game.MoveNumber++;
        game.CurrentPlayerId = GetNextPlayerId(game, playerId);
        game.UpdatedAt = DateTime.UtcNow;

        // Three consecutive passes ends the game
        if (game.ConsecutivePasses >= 3)
        {
            game.Status = GameStatus.Finished;
            CalculateFinalScores(game);
            _logger.LogInformation("Game {GameId} ended due to three consecutive passes", gameId);
        }

        // Record move history
        var passingPlayer = game.Players.First(p => p.Id == playerId);
        game.MoveHistory.Add(new MoveHistory
        {
            MoveNumber = game.MoveNumber,
            PlayerId = playerId,
            PlayerName = passingPlayer.Name,
            ActionType = "pass",
            Score = 0,
            Timestamp = DateTime.UtcNow
        });

        // Persist to database
        entity = GameEntity.FromGame(game);
        await _gameRepository.UpdateAsync(entity);

        return game;
    }

    /// <inheritdoc />
    public async Task<Game> SwapTilesAsync(string gameId, string playerId, SwapTilesRequest request)
    {
        _logger.LogInformation("Player {PlayerId} swapping {TileCount} tiles in game {GameId}",
            playerId, request.TileIds?.Count ?? 0, gameId);

        var entity = await _gameRepository.GetByIdAsync(gameId);
        if (entity == null)
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        var game = entity.ToGame();

        if (game.Status == GameStatus.Finished)
            throw new InvalidOperationException("Game is already finished.");

        if (game.CurrentPlayerId != playerId)
            throw new UnauthorizedAccessException("It is not your turn.");

        var player = game.Players.First(p => p.Id == playerId);

        var tilesToSwap = request.TileIds!.Count;

        // Check if player has enough tiles to swap
        if (player.Hand.Count < tilesToSwap)
            throw new InvalidOperationException(
                $"Player has only {player.Hand.Count} tiles but wants to swap {tilesToSwap}.");

        // Check if at least 7 tiles remain in the bag (Wordfeud rule)
        if (game.TileBag.Count < 7)
            throw new InvalidOperationException(
                $"Cannot swap tiles. At least 7 tiles must remain in the bag. Available: {game.TileBag.Count}");

        // Validate all tile IDs are in the player's hand
        var missingTiles = new List<string>();
        foreach (var tileId in request.TileIds!)
        {
            if (!player.Hand.Any(t => t.Id == tileId))
            {
                missingTiles.Add(tileId);
            }
        }

        if (missingTiles.Count > 0)
        {
            throw new ArgumentException(
                $"The following tiles are not in your hand: {string.Join(", ", missingTiles)}");
        }

        // Remove tiles from hand and return to bag
        foreach (var tileId in request.TileIds!)
        {
            var tile = player.Hand.First(t => t.Id == tileId);
            player.Hand.Remove(tile);
            game.TileBag.Add(tile);
        }

        // Shuffle the returned tiles back in
        ShuffleTileBag(game);

        // Draw replacement tiles
        var tilesDrawn = Math.Min(7 - player.Hand.Count, game.TileBag.Count);
        if (tilesDrawn > 0)
        {
            var newTiles = DrawTiles(game, tilesDrawn);
            player.Hand.AddRange(newTiles);
            player.TilesDrawn += tilesDrawn;
        }

        game.ConsecutivePasses = 0;
        game.MoveNumber++;
        game.CurrentPlayerId = GetNextPlayerId(game, playerId);
        game.UpdatedAt = DateTime.UtcNow;

        // Record move history
        game.MoveHistory.Add(new MoveHistory
        {
            MoveNumber = game.MoveNumber,
            PlayerId = playerId,
            PlayerName = player.Name,
            ActionType = "swap",
            Score = 0,
            Tiles = request.TileIds.Select(id => new MoveTileDto
            {
                TileId = id,
                Letter = "swap",
                Row = 0,
                Column = 0
            }).ToList(),
            Timestamp = DateTime.UtcNow
        });

        // Persist to database
        entity = GameEntity.FromGame(game);
        await _gameRepository.UpdateAsync(entity);

        return game;
    }

    #region Private Methods

    /// <summary>
    /// Draws tiles from the bag and returns them.
    /// </summary>
    private List<Tile> DrawTiles(Game game, int count)
    {
        var drawn = new List<Tile>();
        for (var i = 0; i < Math.Min(count, game.TileBag.Count); i++)
        {
            drawn.Add(game.TileBag[0]);
            game.TileBag.RemoveAt(0);
        }

        ShuffleTileBag(game);
        return drawn;
    }

    /// <summary>
    /// Shuffles the tile bag using Fisher-Yates algorithm.
    /// </summary>
    private void ShuffleTileBag(Game game)
    {
        for (var i = game.TileBag.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (game.TileBag[i], game.TileBag[j]) = (game.TileBag[j], game.TileBag[i]);
        }
    }

    /// <summary>
    /// Validates a tile placement on the board.
    /// </summary>
    private (bool IsValid, string ErrorMessage) ValidatePlacement(Game game, Player player, PlaceTilesRequest request)
    {
        // Check if it's the first move — must cross center (7,7)
        var hasAnyPlacedTile = false;
        for (var r = 0; r < BoardConfiguration.BoardSize; r++)
            for (var c = 0; c < BoardConfiguration.BoardSize; c++)
                if (game.Board[r, c] != null)
                    hasAnyPlacedTile = true;

        if (!hasAnyPlacedTile)
        {
            // First move: must include (7,7)
            var coversCenter = request.Tiles.Any(t => t.Row == 7 && t.Column == 7);
            if (!coversCenter)
            {
                return (false, "First move must cross the center square (7,7).");
            }
        }
        else
        {
            // Not first move: must connect to existing tiles
            if (!ConnectsToExistingTiles(game, request))
            {
                return (false, "At least one tile must connect to existing tiles.");
            }
        }

        // Derive direction from tile positions
        var direction = DeriveDirection(request.Tiles);

        // Check tiles are in a line (horizontal or vertical)
        if (!AreInLine(request.Tiles, direction))
        {
            return (false, "All placed tiles must be in a straight line (horizontal or vertical).");
        }

        // Check for gaps in placement
        if (HasGaps(game, request.Tiles, direction))
        {
            return (false, "Placed tiles must be contiguous (no gaps).");
        }

        // Validate formed words
        var formedWords = GetFormedWords(game, request, direction);
        foreach (var wordInfo in formedWords)
        {
            if (wordInfo.Word.Length < 2)
            {
                return (false, $"Formed word '{wordInfo.Word}' is too short.");
            }

            if (!_dictionaryService.Contains(wordInfo.Word))
            {
                return (false, $"Word '{wordInfo.Word}' is not a valid Dutch word.");
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Derives the placement direction from tile positions.
    /// If all tiles share the same row → horizontal (0).
    /// If all tiles share the same column → vertical (1).
    /// </summary>
    private static int DeriveDirection(List<TilePlacementDto> tiles)
    {
        if (tiles.Count <= 1)
            return 0; // Default to horizontal for single tile

        var allSameRow = tiles.All(t => t.Row == tiles[0].Row);
        var allSameCol = tiles.All(t => t.Column == tiles[0].Column);

        if (allSameRow)
            return 0; // Horizontal
        if (allSameCol)
            return 1; // Vertical

        return 0; // Default
    }

    /// <summary>
    /// Checks if a placement connects to existing tiles on the board.
    /// </summary>
    private bool ConnectsToExistingTiles(Game game, PlaceTilesRequest request)
    {
        foreach (var tile in request.Tiles)
        {
            // Check all 4 directions for adjacent existing tiles
            var directions = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };

            foreach (var (dr, dc) in directions)
            {
                var r = tile.Row + dr;
                var c = tile.Column + dc;
                while (r >= 0 && r < BoardConfiguration.BoardSize && c >= 0 && c < BoardConfiguration.BoardSize)
                {
                    if (game.Board[r, c] != null)
                        return true;
                    r += dr;
                    c += dc;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if all placed tiles are in a straight line.
    /// </summary>
    private static bool AreInLine(List<TilePlacementDto> tiles, int direction)
    {
        if (tiles.Count <= 1)
            return true;

        if (direction == 0)
        {
            // Horizontal: all rows must be the same
            var row = tiles[0].Row;
            return tiles.All(t => t.Row == row);
        }
        else
        {
            // Vertical: all columns must be the same
            var col = tiles[0].Column;
            return tiles.All(t => t.Column == col);
        }
    }

    /// <summary>
    /// Checks if placed tiles have gaps between them.
    /// </summary>
    private bool HasGaps(Game game, List<TilePlacementDto> tiles, int direction)
    {
        if (tiles.Count <= 1)
            return false;

        var sorted = direction == 0
            ? tiles.OrderBy(t => t.Column).ToList()
            : tiles.OrderBy(t => t.Row).ToList();

        for (var i = 1; i < sorted.Count; i++)
        {
            if (direction == 0)
            {
                if (sorted[i].Column != sorted[i - 1].Column + 1)
                {
                    // Gap exists — check if intermediate positions are filled by existing tiles
                    for (var col = sorted[i - 1].Column + 1; col < sorted[i].Column; col++)
                    {
                        if (game.Board[sorted[i - 1].Row, col] == null)
                            return true;
                    }
                }
            }
            else
            {
                if (sorted[i].Row != sorted[i - 1].Row + 1)
                {
                    for (var row = sorted[i - 1].Row + 1; row < sorted[i].Row; row++)
                    {
                        if (game.Board[row, sorted[i - 1].Column] == null)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all words formed by a placement, including cross words.
    /// The main word is built by scanning the full line (including existing tiles that fill gaps)
    /// so placements that connect through existing tiles produce the correct word.
    /// </summary>
    private List<(string Word, bool IsCrossWord)> GetFormedWords(Game game, PlaceTilesRequest request, int direction)
    {
        var words = new List<(string Word, bool IsCrossWord)>();

        // Build the main word by scanning the full line (including existing tiles)
        var mainWord = direction == 0
            ? BuildFullLineWord(game, request.Tiles, fixedCoord: request.Tiles[0].Row, vertical: false, request.BlankAssignments)
            : BuildFullLineWord(game, request.Tiles, fixedCoord: request.Tiles[0].Column, vertical: true, request.BlankAssignments);

        if (mainWord.Length >= 2)
            words.Add((mainWord, false));

        if (direction == 0)
        {
            // Horizontal placement - cross words are vertical
            var row = request.Tiles[0].Row;
            foreach (var tile in request.Tiles)
            {
                var col = tile.Column;
                var crossWord = new StringBuilder();

                // Find start
                var r = row - 1;
                while (r >= 0 && game.Board[r, col] != null)
                    r--;
                r++;

                // Build word
                while (r < BoardConfiguration.BoardSize && game.Board[r, col] != null)
                {
                    var t = game.Board[r, col]!;
                    crossWord.Append(t.BlankRepresentation ?? t.Letter);
                    r++;
                }

                if (crossWord.Length >= 2 && crossWord.ToString() != mainWord)
                    words.Add((crossWord.ToString(), true));
            }
        }
        else
        {
            // Vertical placement - cross words are horizontal
            var col = request.Tiles[0].Column;
            foreach (var tile in request.Tiles)
            {
                var row = tile.Row;
                var crossWord = new StringBuilder();

                // Find start
                var c = col - 1;
                while (c >= 0 && game.Board[row, c] != null)
                    c--;
                c++;

                // Build word
                while (c < BoardConfiguration.BoardSize && game.Board[row, c] != null)
                {
                    var t = game.Board[row, c]!;
                    crossWord.Append(t.BlankRepresentation ?? t.Letter);
                    c++;
                }

                if (crossWord.Length >= 2)
                    words.Add((crossWord.ToString(), true));
            }
        }

        return words;
    }

    /// <summary>
    /// Builds the full word for a placement line, including existing tiles that fill gaps
    /// between newly placed tiles.
    /// </summary>
    private static string BuildFullLineWord(
        Game game,
        List<TilePlacementDto> tiles,
        int row,
        bool vertical,
        IDictionary<string, string>? blankAssignments)
    {
        // Find the min and max positions covered by the placed tiles
        var minPos = tiles.Min(t => vertical ? t.Row : t.Column);
        var maxPos = tiles.Max(t => vertical ? t.Row : t.Column);

        // Extend left/up to find the true start
        var start = minPos;
        while (start > 0)
        {
            var r = vertical ? row : start - 1;
            var c = vertical ? start - 1 : row;
            if (vertical)
            {
                if (game.Board[start - 1, row] == null) break;
            }
            else
            {
                if (game.Board[row, start - 1] == null) break;
            }
            start--;
        }

        // Extend right/down to find the true end
        var end = maxPos;
        var boardSize = BoardConfiguration.BoardSize;
        while (end < boardSize - 1)
        {
            if (vertical)
            {
                if (game.Board[end + 1, row] == null) break;
            }
            else
            {
                if (game.Board[row, end + 1] == null) break;
            }
            end++;
        }

        // Build the word from start to end
        var sb = new StringBuilder();
        for (var pos = start; pos <= end; pos++)
        {
            var tile = vertical ? game.Board[pos, row] : game.Board[row, pos];
            if (tile != null)
            {
                sb.Append(tile.BlankRepresentation ?? tile.Letter);
            }
            else
            {
                // Gap position — find the corresponding placed tile and use its letter
                var placedTile = tiles.First(t => vertical ? t.Row == pos : t.Column == pos);
                var letter = placedTile.Letter ?? string.Empty;
                if (blankAssignments != null && blankAssignments.ContainsKey(placedTile.TileId))
                {
                    letter = blankAssignments[placedTile.TileId];
                }
                sb.Append(letter);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Calculates the score for a placement.
    /// </summary>
    private (int TotalScore, List<string> FormedWords) CalculateScore(Game game, PlaceTilesRequest request, int direction)
    {
        var totalScore = 0;
        var formedWordNames = new List<string>();
        var formedWords = GetFormedWords(game, request, direction);

        if (!formedWords.Any())
        {
            // No words formed - score tiles at base value (no word multipliers)
            foreach (var tileDto in request.Tiles)
            {
                var tile = game.Board[tileDto.Row, tileDto.Column];
                totalScore += tile?.Points ?? 0;
            }

            // 40-point bonus for playing all 7 tiles
            if (request.Tiles.Count == 7)
                totalScore += 40;

            return (totalScore, formedWordNames);
        }

        // Score each formed word
        foreach (var (word, isCrossWord) in formedWords)
        {
            var wordScore = isCrossWord
                ? ScoreCrossWord(game, word, request, direction)
                : ScoreMainWord(game, request);

            totalScore += wordScore;
            formedWordNames.Add(word);
        }

        // 40-point bonus for playing all 7 tiles
        if (request.Tiles.Count == 7)
            totalScore += 40;

        return (totalScore, formedWordNames);
    }

    /// <summary>
    /// Scores the main word by scoring all newly placed tiles with bonuses and word multipliers.
    /// </summary>
    private int ScoreMainWord(Game game, PlaceTilesRequest request)
    {
        var letterScore = 0;
        var wordMultiplier = 1;

        foreach (var tileDto in request.Tiles)
        {
            var tile = game.Board[tileDto.Row, tileDto.Column];
            var letterPoints = tile?.Points ?? 0;

            // Apply letter bonuses (only to newly placed tiles)
            var bonus = BoardConfiguration.GetBonusType(tileDto.Row, tileDto.Column);
            if (bonus == BonusType.DoubleLetter)
                letterPoints *= 2;
            else if (bonus == BonusType.TripleLetter)
                letterPoints *= 3;

            letterScore += letterPoints;
        }

        // Apply word multipliers (only to newly placed tiles)
        foreach (var tileDto in request.Tiles)
        {
            var bonus = BoardConfiguration.GetBonusType(tileDto.Row, tileDto.Column);
            if (bonus == BonusType.DoubleWord)
                wordMultiplier *= 2;
            else if (bonus == BonusType.TripleWord)
                wordMultiplier *= 3;
        }

        return letterScore * wordMultiplier;
    }

    /// <summary>
    /// Scores a cross word by identifying which newly placed tiles form this word and scoring them.
    /// Cross words score base letter values only — no letter or word multipliers apply.
    /// </summary>
    private int ScoreCrossWord(Game game, string word, PlaceTilesRequest request, int direction)
    {
        // Determine the perpendicular direction for the cross word
        var isHorizontalMain = direction == 0;

        foreach (var tileDto in request.Tiles)
        {
            var wordBuilder = isHorizontalMain
                ? BuildWordVertical(game, tileDto.Row, tileDto.Column)
                : BuildWordHorizontal(game, tileDto.Row, tileDto.Column);

            if (wordBuilder.ToString() == word)
            {
                var tile = game.Board[tileDto.Row, tileDto.Column];
                // Wordfeud rule: cross words score base letter value only, no multipliers
                return tile?.Points ?? 0;
            }
        }

        return 0;
    }

    /// <summary>
    /// Builds a word by walking vertically from the given position.
    /// </summary>
    private static StringBuilder BuildWordVertical(Game game, int startRow, int startCol)
    {
        var wordBuilder = new StringBuilder();

        // Build word upward from this tile
        var r = startRow - 1;
        while (r >= 0 && game.Board[r, startCol] != null)
            r--;
        r++;

        // Build word downward from this tile
        while (r < BoardConfiguration.BoardSize && game.Board[r, startCol] != null)
        {
            var tile = game.Board[r, startCol];
            wordBuilder.Append(tile?.BlankRepresentation ?? tile?.Letter ?? string.Empty);
            r++;
        }

        return wordBuilder;
    }

    /// <summary>
    /// Builds a word by walking horizontally from the given position.
    /// </summary>
    private static StringBuilder BuildWordHorizontal(Game game, int startRow, int startCol)
    {
        var wordBuilder = new StringBuilder();

        // Build word leftward from this tile
        var c = startCol - 1;
        while (c >= 0 && game.Board[startRow, c] != null)
            c--;
        c++;

        // Build word rightward from this tile
        while (c < BoardConfiguration.BoardSize && game.Board[startRow, c] != null)
        {
            var tile = game.Board[startRow, c];
            wordBuilder.Append(tile?.BlankRepresentation ?? tile?.Letter ?? string.Empty);
            c++;
        }

        return wordBuilder;
    }

    /// <summary>
    /// Gets the next player's ID.
    /// </summary>
    private string GetNextPlayerId(Game game, string currentPlayerId)
    {
        var playerIndex = game.Players.FindIndex(p => p.Id == currentPlayerId);
        var nextIndex = (playerIndex + 1) % game.Players.Count;
        return game.Players[nextIndex].Id;
    }

    /// <summary>
    /// Checks and handles game end conditions.
    /// </summary>
    private void CheckGameEnd(Game game)
    {
        // Check if any player has no tiles left and bag is empty
        foreach (var player in game.Players)
        {
            if (player.Hand.Count == 0 && game.TileBag.Count == 0)
            {
                game.Status = GameStatus.Finished;
                CalculateFinalScores(game);
                _logger.LogInformation("Game {GameId} ended: player '{PlayerName}' used all tiles",
                    game.Id, player.Name);
                return;
            }
        }
    }

    /// <summary>
    /// Calculates final scores after game ends.
    /// </summary>
    private void CalculateFinalScores(Game game)
    {
        // Count how many players have finished (no tiles left)
        var finishedPlayers = game.Players.Where(p => p.Hand.Count == 0).ToList();
        var unfinishedPlayers = game.Players.Where(p => p.Hand.Count > 0).ToList();

        foreach (var player in game.Players)
        {
            // Subtract remaining tile values
            var remainingTiles = player.Hand.Sum(t => t.Points);
            player.Score -= remainingTiles;
        }

        // If one player finished, add opponent's remaining tiles to finisher's score
        if (finishedPlayers.Count == 1 && unfinishedPlayers.Count == 1)
        {
            var finisher = finishedPlayers[0];
            var opponent = unfinishedPlayers[0];
            var opponentRemaining = opponent.Hand.Sum(t => t.Points);
            finisher.Score += opponentRemaining;
        }
    }

    /// <inheritdoc />
    public async Task<List<MoveHistory>> GetMoveHistoryAsync(string gameId)
    {
        _logger.LogInformation("Getting move history for game {GameId}", gameId);

        var entity = await _gameRepository.GetByIdAsync(gameId);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Game '{gameId}' not found.");
        }

        var game = entity.ToGame();
        var moveHistory = game.MoveHistory.ToList();

        _logger.LogInformation("Move history retrieved for game {GameId}: {MoveCount} moves", gameId, moveHistory.Count);
        return moveHistory;
    }

    #endregion
}
