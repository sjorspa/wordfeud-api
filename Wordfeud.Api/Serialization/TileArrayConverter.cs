using System.Text.Json;
using System.Text.Json.Serialization;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Serialization;

/// <summary>
/// Custom JSON converter for 2D nullable tile arrays.
/// Serializes Tile?[15,15] as a 2D grid of tile objects (null for empty squares).
/// </summary>
public sealed class TileArrayConverter : JsonConverter<Tile?[,]>
{
    public override Tile?[,]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException("Deserialization of Tile[,] is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, Tile?[,]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        int rows = value.GetLength(0);
        int cols = value.GetLength(1);

        writer.WriteStartArray();
        for (int row = 0; row < rows; row++)
        {
            writer.WriteStartArray();
            for (int col = 0; col < cols; col++)
            {
                var tile = value[row, col];
                if (tile == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStartObject();
                    writer.WriteString("id", tile.Id);
                    writer.WriteString("letter", tile.Letter);
                    writer.WriteNumber("points", tile.Points);
                    writer.WriteBoolean("isBlank", tile.IsBlank);
                    writer.WriteString("blankRepresentation", tile.BlankRepresentation ?? "");
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}
