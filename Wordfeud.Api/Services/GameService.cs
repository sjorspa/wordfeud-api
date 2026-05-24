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
    private readonly Dictionary<string, Game> _games = new();
    private readonly IDutchDictionaryService _dictionaryService;
    private readonly ILogger<GameService> _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new GameService.
    /// </summary>
    public GameService(IDutchDictionaryService dictionaryService, ILogger<GameService> logger)
    {
        _dictionaryService = dictionaryService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Game> CreateGameAsync(string playerName)
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

        lock (_lock)
        {
            _games[game.Id] = game;
        }

        _logger.LogInformation("Game {GameId} created", game.Id);
        return Task.FromResult(game);
    }

    /// <inheritdoc />
    public async Task<Game> JoinGameAsync(string gameId, string playerName)
    {
        _logger.LogInformation("Player '{PlayerName}' joining game {GameId}", playerName, gameId);

        Game game;
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out game))
            {
                throw new KeyNotFoundException($"Game '{gameId}' not found.");
            }

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
        }

        _logger.LogInformation("Player '{PlayerName}' joined game {GameId}", playerName, gameId);
        return await Task.FromResult(game);
    }

    /// <inheritdoc />
    public Task<Game?> GetGameAsync(string gameId)
    {
        Game? game;
        lock (_lock)
        {
            _games.TryGetValue(gameId, out game);
        }

        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found", gameId);
        }

        return Task.FromResult(game);
    }

    /// <inheritdoc />
    public async Task<Game> PlaceTilesAsync(string gameId, string playerId, PlaceTilesRequest request)
    {
        _logger.LogInformation("Player {PlayerId} placing {TileCount} tiles in game {GameId}",
            playerId, request.Tiles?.Count ?? 0, gameId);

        Game game;
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out game))
                throw new KeyNotFoundException($"Game '{gameId}' not found.");

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
                if (string.IsNullOrEmpty(tileDto.Letter))
                    throw new ArgumentException("Blank tile must have a letter assigned.");
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
            var scoreResult = CalculateScore(game, request);
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

            _logger.LogInformation("Player {PlayerId} scored {Score} points in game {GameId}",
                playerId, scoreResult.TotalScore, gameId);
        }

        return game;
    }

    /// <inheritdoc />
    public Task<Game> GetScoresAsync(string gameId)
    {
        Game? game;
        lock (_lock)
        {
            _games.TryGetValue(gameId, out game);
        }

        if (game == null)
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        // Calculate final scores if game is finished
        if (game.Status == GameStatus.Finished)
        {
            CalculateFinalScores(game);
        }

        return Task.FromResult(game);
    }

    /// <inheritdoc />
    public Task<Game> GetBoardAsync(string gameId)
    {
        Game? game;
        lock (_lock)
        {
            _games.TryGetValue(gameId, out game);
        }

        if (game == null)
            throw new KeyNotFoundException($"Game '{gameId}' not found.");

        return Task.FromResult(game);
    }

    /// <inheritdoc />
    public async Task<Game> PassTurnAsync(string gameId, string playerId)
    {
        _logger.LogInformation("Player {PlayerId} passing turn in game {GameId}", playerId, gameId);

        Game game;
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out game))
                throw new KeyNotFoundException($"Game '{gameId}' not found.");

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
        }

        return game;
    }

    /// <inheritdoc />
    public async Task<Game> SwapTilesAsync(string gameId, string playerId, SwapTilesRequest request)
    {
        _logger.LogInformation("Player {PlayerId} swapping {TileCount} tiles in game {GameId}",
            playerId, request.TileIds?.Count ?? 0, gameId);

        Game game;
        lock (_lock)
        {
            if (!_games.TryGetValue(gameId, out game))
                throw new KeyNotFoundException($"Game '{gameId}' not found.");

            if (game.Status == GameStatus.Finished)
                throw new InvalidOperationException("Game is already finished.");

            if (game.CurrentPlayerId != playerId)
                throw new UnauthorizedAccessException("It is not your turn.");

            var player = game.Players.First(p => p.Id == playerId);

            // Check if player has enough tiles to swap
            if (player.Hand.Count < tilesToSwap)
                throw new InvalidOperationException(
                    $"Player has only {player.Hand.Count} tiles but wants to swap {tilesToSwap}.");

            // Check if enough tiles remain in the bag
            if (game.TileBag.Count < tilesToSwap)
                throw new InvalidOperationException(
                    $"Not enough tiles in the bag to swap. Available: {game.TileBag.Count}, Requested: {tilesToSwap}");

            // Remove tiles from hand and return to bag
            foreach (var tileId in request.TileIds!)
            {
                var tile = player.Hand.FirstOrDefault(t => t.Id == tileId);
                if (tile != null)
                {
                    player.Hand.Remove(tile);
                    game.TileBag.Add(tile);
                }
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
        }

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
        var random = new Random();
        for (var i = game.TileBag.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
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
        for (var r = 0; r < 15; r++)
            for (var c = 0; c < 15; c++)
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

        // Check tiles are in a line (horizontal or vertical)
        if (!AreInLine(request.Tiles, request.Direction))
        {
            return (false, "All placed tiles must be in a straight line (horizontal or vertical).");
        }

        // Check for gaps in placement
        if (HasGaps(request.Tiles, request.Direction))
        {
            return (false, "Placed tiles must be contiguous (no gaps).");
        }

        // Validate formed words
        var formedWords = GetFormedWords(game, request);
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
                while (r >= 0 && r < 15 && c >= 0 && c < 15)
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
    private static bool HasGaps(List<TilePlacementDto> tiles, int direction)
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
                    return true;
            }
            else
            {
                if (sorted[i].Row != sorted[i - 1].Row + 1)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all words formed by a placement, including cross words.
    /// The main word is built from the request tile letters (not the board) so it works
    /// both during validation (before placement) and scoring (after placement).
    /// </summary>
    private List<(string Word, bool IsCrossWord)> GetFormedWords(Game game, PlaceTilesRequest request)
    {
        var words = new List<(string Word, bool IsCrossWord)>();

        // Build the main word from request tile letters directly
        var mainWord = request.Direction == 0
            ? BuildStringFromTiles(request.Tiles.OrderBy(t => t.Column).ToList(), request.BlankAssignments)
            : BuildStringFromTiles(request.Tiles.OrderBy(t => t.Row).ToList(), request.BlankAssignments);

        if (mainWord.Length >= 2)
            words.Add((mainWord, false));

        if (request.Direction == 0)
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
                while (r < 15 && game.Board[r, col] != null)
                {
                    var t = game.Board[r, col];
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
                while (c < 15 && game.Board[row, c] != null)
                {
                    var t = game.Board[row, c];
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
    /// Builds a word string from a list of tiles, using their letters or blank representations
    /// from the request's BlankAssignments dictionary.
    /// </summary>
    private static string BuildStringFromTiles(List<TilePlacementDto> tiles, IDictionary<string, string>? blankAssignments)
    {
        var sb = new StringBuilder();
        foreach (var tile in tiles)
        {
            var letter = tile.Letter ?? string.Empty;
            if (blankAssignments != null && blankAssignments.ContainsKey(tile.TileId))
            {
                letter = blankAssignments[tile.TileId];
            }
            sb.Append(letter);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Calculates the score for a placement.
    /// </summary>
    private (int TotalScore, List<string> FormedWords) CalculateScore(Game game, PlaceTilesRequest request)
    {
        var totalScore = 0;
        var formedWordNames = new List<string>();
        var formedWords = GetFormedWords(game, request);

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
                ? ScoreCrossWord(game, word, request)
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
    /// </summary>
    private int ScoreCrossWord(Game game, string word, PlaceTilesRequest request)
    {
        var letterScore = 0;
        var wordMultiplier = 1;

        if (request.Direction == 0)
        {
            // Horizontal main placement - cross words are vertical
            foreach (var tileDto in request.Tiles)
            {
                var col = tileDto.Column;
                var row = tileDto.Row;

                // Check if this tile is part of the cross word by walking vertically
                var wordBuilder = new StringBuilder();

                // Build word upward from this tile
                var r = row - 1;
                while (r >= 0 && game.Board[r, col] != null)
                    r--;
                r++;

                // Build word downward from this tile
                while (r < 15 && game.Board[r, col] != null)
                {
                    var t = game.Board[r, col];
                    wordBuilder.Append(t?.BlankRepresentation ?? t?.Letter ?? string.Empty);
                    r++;
                }

                // Check if this tile's vertical word matches the cross word
                if (wordBuilder.ToString() == word)
                {
                    var tile = game.Board[row, col];
                    var letterPoints = tile?.Points ?? 0;

                    // Apply letter bonuses (only to newly placed tiles)
                    var bonus = BoardConfiguration.GetBonusType(row, col);
                    if (bonus == BonusType.DoubleLetter)
                        letterPoints *= 2;
                    else if (bonus == BonusType.TripleLetter)
                        letterPoints *= 3;

                    letterScore += letterPoints;

                    // Apply word multipliers (only to newly placed tiles)
                    if (bonus == BonusType.DoubleWord)
                        wordMultiplier *= 2;
                    else if (bonus == BonusType.TripleWord)
                        wordMultiplier *= 3;

                    break;
                }
            }
        }
        else
        {
            // Vertical main placement - cross words are horizontal
            foreach (var tileDto in request.Tiles)
            {
                var row = tileDto.Row;
                var col = tileDto.Column;

                // Check if this tile is part of the cross word by walking horizontally
                var wordBuilder = new StringBuilder();

                // Build word leftward from this tile
                var c = col - 1;
                while (c >= 0 && game.Board[row, c] != null)
                    c--;
                c++;

                // Build word rightward from this tile
                while (c < 15 && game.Board[row, c] != null)
                {
                    var t = game.Board[row, c];
                    wordBuilder.Append(t?.BlankRepresentation ?? t?.Letter ?? string.Empty);
                    c++;
                }

                // Check if this tile's horizontal word matches the cross word
                if (wordBuilder.ToString() == word)
                {
                    var tile = game.Board[row, col];
                    var letterPoints = tile?.Points ?? 0;

                    // Apply letter bonuses (only to newly placed tiles)
                    var bonus = BoardConfiguration.GetBonusType(row, col);
                    if (bonus == BonusType.DoubleLetter)
                        letterPoints *= 2;
                    else if (bonus == BonusType.TripleLetter)
                        letterPoints *= 3;

                    letterScore += letterPoints;

                    // Apply word multipliers (only to newly placed tiles)
                    if (bonus == BonusType.DoubleWord)
                        wordMultiplier *= 2;
                    else if (bonus == BonusType.TripleWord)
                        wordMultiplier *= 3;

                    break;
                }
            }
        }

        return letterScore * wordMultiplier;
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

    #endregion
}
