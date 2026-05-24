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
        if (reader.TokenType == JsonTokenType.Null)
        {
            reader.Read();
            return new Board();
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected StartArray but got {reader.TokenType}");
        }

        // Deserialize the 2D array into List<List<Tile?>> using the built-in
        // collection support, then populate the Board from the resulting data.
        // JsonSerializer.Deserialize properly advances the reader to the token
        // after the consumed value (EndArray), so we must NOT call reader.Read()
        // again after it returns.
        var rows = JsonSerializer.Deserialize<List<List<Tile?>>>(ref reader, options);
        if (rows == null)
        {
            return new Board();
        }

        var board = new Board();
        for (int row = 0; row < rows.Count && row < 15; row++)
        {
            var rowTiles = rows[row];
            for (int col = 0; col < rowTiles.Count && col < 15; col++)
            {
                if (rowTiles[col] != null && board.IsValidPosition(row, col))
                {
                    board.SetTile(row, col, rowTiles[col]);
                }
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
