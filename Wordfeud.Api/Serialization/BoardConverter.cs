using System.Text.Json;
using System.Text.Json.Serialization;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Serialization;

/// <summary>
/// Serializes the Wordfeud board as a 2D array of tiles for JSON serialization.
/// </summary>
public sealed class BoardConverter : JsonConverter<Board>
{
    /// <inheritdoc />
    public override Board Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Skip the property name token ("board") that the JsonSerializer leaves us at
        if (reader.TokenType == JsonTokenType.PropertyName)
        {
            reader.Read();
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            reader.Read();
            return new Board();
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected StartArray but got {reader.TokenType}.");
        }

        // Manually read the 2D array to avoid issues with nested converters
        var rows = new List<List<Tile?>>(15);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var row = new List<Tile?>(15);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        row.Add(null);
                    }
                    else if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        var tile = JsonSerializer.Deserialize<Tile>(ref reader, options);
                        row.Add(tile);
                    }
                    else
                    {
                        row.Add(null);
                    }
                }
                rows.Add(row);
            }
        }

        if (rows == null || rows.Count == 0)
        {
            return new Board();
        }

        var board = new Board();
        for (int row = 0; row < 15; row++)
        {
            for (int col = 0; col < 15; col++)
            {
                board.SetTile(row, col, rows[row]?[col]);
            }
        }

        return board;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Board value, JsonSerializerOptions options)
    {
        // Serialize as List<List<Tile?>> (natively supported by JsonSerializer)
        var rows = new List<List<Tile?>>(15);
        for (int row = 0; row < 15; row++)
        {
            var rowList = new List<Tile?>(15);
            for (int col = 0; col < 15; col++)
            {
                rowList.Add(value.GetTile(row, col));
            }
            rows.Add(rowList);
        }

        JsonSerializer.Serialize(writer, rows, options);
    }
}
