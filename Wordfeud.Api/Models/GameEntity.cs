using System.Text.Json;
using System.Text.Json.Serialization;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Models;

/// <summary>
/// EF Core entity representing a persisted Wordfeud game.
/// Complex types (Board, Players, TileBag, MoveHistory) are stored as JSON.
/// </summary>
public class GameEntity
{
    /// <summary>
    /// The unique identifier of the game.
    /// </summary>
    [JsonPropertyName("Id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The current status of the game.
    /// </summary>
    [JsonPropertyName("Status")]
    public string Status { get; set; } = "Waiting";

    /// <summary>
    /// The players in this game, serialized as JSON.
    /// </summary>
    [JsonPropertyName("PlayersJson")]
    public string PlayersJson { get; set; } = "[]";

    /// <summary>
    /// The current player whose turn it is.
    /// </summary>
    [JsonPropertyName("CurrentPlayerId")]
    public string? CurrentPlayerId { get; set; }

    /// <summary>
    /// The bag of remaining tiles, serialized as JSON.
    /// </summary>
    [JsonPropertyName("TileBagJson")]
    public string TileBagJson { get; set; } = "[]";

    /// <summary>
    /// The board state, serialized as JSON.
    /// </summary>
    [JsonPropertyName("BoardJson")]
    public string BoardJson { get; set; } = "[]";

    /// <summary>
    /// The list of words formed in the game.
    /// </summary>
    [JsonPropertyName("FormedWordsJson")]
    public string FormedWordsJson { get; set; } = "[]";

    /// <summary>
    /// The list of moves made in the game, serialized as JSON.
    /// </summary>
    [JsonPropertyName("MoveHistoryJson")]
    public string MoveHistoryJson { get; set; } = "[]";

    /// <summary>
    /// The number of consecutive passes in the current turn.
    /// </summary>
    [JsonPropertyName("ConsecutivePasses")]
    public int ConsecutivePasses { get; set; }

    /// <summary>
    /// The move number (incremented each turn).
    /// </summary>
    [JsonPropertyName("MoveNumber")]
    public int MoveNumber { get; set; }

    /// <summary>
    /// The timestamp when the game was created.
    /// </summary>
    [JsonPropertyName("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The timestamp when the game was last updated.
    /// </summary>
    [JsonPropertyName("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Deserializes this entity into a <see cref="Game"/> domain model.
    /// </summary>
    public Game ToGame()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Add the BoardConverter before any deserialization
        options.Converters.Add(new Wordfeud.Api.Serialization.BoardConverter());

        var game = new Game
        {
            Id = Id,
            Status = Enum.Parse<GameStatus>(Status, ignoreCase: true),
            CurrentPlayerId = CurrentPlayerId,
            ConsecutivePasses = ConsecutivePasses,
            MoveNumber = MoveNumber,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };

        game.Players = JsonSerializer.Deserialize<List<Player>>(PlayersJson, options) ?? new();
        game.TileBag = JsonSerializer.Deserialize<List<Tile>>(TileBagJson, options) ?? new();
        game.FormedWords = JsonSerializer.Deserialize<List<string>>(FormedWordsJson, options) ?? new();
        game.MoveHistory = JsonSerializer.Deserialize<List<MoveHistory>>(MoveHistoryJson, options) ?? new();
        game.Board = JsonSerializer.Deserialize<Board>(BoardJson, options) ?? new Board();

        return game;
    }

    /// <summary>
    /// Creates a <see cref="GameEntity"/> from a <see cref="Game"/> domain model.
    /// </summary>
    public static GameEntity FromGame(Game game)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Add the BoardConverter so Board is serialized as a 2D array
        options.Converters.Add(new Wordfeud.Api.Serialization.BoardConverter());

        return new GameEntity
        {
            Id = game.Id,
            Status = game.Status.ToString(),
            PlayersJson = JsonSerializer.Serialize(game.Players, options),
            CurrentPlayerId = game.CurrentPlayerId,
            TileBagJson = JsonSerializer.Serialize(game.TileBag, options),
            BoardJson = JsonSerializer.Serialize(game.Board, options),
            FormedWordsJson = JsonSerializer.Serialize(game.FormedWords, options),
            MoveHistoryJson = JsonSerializer.Serialize(game.MoveHistory, options),
            ConsecutivePasses = game.ConsecutivePasses,
            MoveNumber = game.MoveNumber,
            CreatedAt = game.CreatedAt,
            UpdatedAt = game.UpdatedAt,
        };
    }
}
