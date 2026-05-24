using System.Text.Json;
using Wordfeud.Api.Models;
using Wordfeud.Api.Serialization;

namespace Wordfeud.Api.Tests.Serialization;

/// <summary>
/// Tests for the BoardConverter JSON serialization/deserialization.
/// </summary>
public class BoardConverterTests
{
    private readonly JsonSerializerOptions _options;

    public BoardConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new BoardConverter());
    }

    [Fact]
    public void Serialize_BoardWithTiles_Returns2DArray()
    {
        // Arrange
        var board = new Board();
        board.SetTile(7, 7, new Tile { Letter = "A", Points = 1, IsBlank = false });
        board.SetTile(7, 8, new Tile { Letter = "B", Points = 4, IsBlank = false });
        board.SetTile(8, 7, new Tile { Letter = "C", Points = 5, IsBlank = false });

        // Act
        var json = JsonSerializer.Serialize(board, _options);
        var deserialized = JsonSerializer.Deserialize<Board>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.GetTile(7, 7)!.Letter.Should().Be("A");
        deserialized.GetTile(7, 7)!.Points.Should().Be(1);
        deserialized.GetTile(7, 8)!.Letter.Should().Be("B");
        deserialized.GetTile(7, 8)!.Points.Should().Be(4);
        deserialized.GetTile(8, 7)!.Letter.Should().Be("C");
        deserialized.GetTile(8, 7)!.Points.Should().Be(5);
    }

    [Fact]
    public void Serialize_BoardWithBlankTiles_PersistsBlankRepresentation()
    {
        // Arrange
        var board = new Board();
        board.SetTile(0, 0, new Tile { Letter = "", Points = 0, IsBlank = true, BlankRepresentation = "Z" });

        // Act
        var json = JsonSerializer.Serialize(board, _options);
        var deserialized = JsonSerializer.Deserialize<Board>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.GetTile(0, 0)!.IsBlank.Should().BeTrue();
        deserialized.GetTile(0, 0)!.BlankRepresentation.Should().Be("Z");
    }

    [Fact]
    public void Serialize_EmptyBoard_ReturnsEmpty2DArray()
    {
        // Arrange
        var board = new Board();

        // Act
        var json = JsonSerializer.Serialize(board, _options);
        var deserialized = JsonSerializer.Deserialize<Board>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Size.Should().Be(15);
        deserialized.GetTile(0, 0).Should().BeNull();
        deserialized.GetTile(7, 7).Should().BeNull();
    }

    [Fact]
    public void Serialize_BoardWithNullToken_ReturnsNullBoard()
    {
        // Arrange
        var json = "null";

        // Act
        var deserialized = JsonSerializer.Deserialize<Board>(json, _options);

        // Assert - top-level null JSON returns null without invoking the converter
        deserialized.Should().BeNull();
    }

    [Fact]
    public void Serialize_BoardWithTilesOnAllEdges_PersistsAllTiles()
    {
        // Arrange
        var board = new Board();
        // Place tiles on all four edges (excluding corners to avoid overlap between loops)
        for (int i = 1; i < 14; i++)
        {
            board.SetTile(0, i, new Tile { Letter = $"E{i}", Points = i, IsBlank = false });
            board.SetTile(14, i, new Tile { Letter = $"F{i}", Points = i, IsBlank = false });
            board.SetTile(i, 0, new Tile { Letter = $"G{i}", Points = i, IsBlank = false });
            board.SetTile(i, 14, new Tile { Letter = $"H{i}", Points = i, IsBlank = false });
        }
        // Set corners explicitly
        board.SetTile(0, 0, new Tile { Letter = "C0", Points = 0, IsBlank = false });
        board.SetTile(0, 14, new Tile { Letter = "C1", Points = 14, IsBlank = false });
        board.SetTile(14, 0, new Tile { Letter = "C2", Points = 14, IsBlank = false });
        board.SetTile(14, 14, new Tile { Letter = "C3", Points = 28, IsBlank = false });

        // Act
        var json = JsonSerializer.Serialize(board, _options);
        var deserialized = JsonSerializer.Deserialize<Board>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.GetTile(0, 0)!.Letter.Should().Be("C0");
        deserialized.GetTile(0, 14)!.Letter.Should().Be("C1");
        deserialized.GetTile(14, 0)!.Letter.Should().Be("C2");
        deserialized.GetTile(14, 14)!.Letter.Should().Be("C3");
        deserialized.GetTile(0, 7)!.Letter.Should().Be("E7");
        deserialized.GetTile(14, 7)!.Letter.Should().Be("F7");
        deserialized.GetTile(7, 0)!.Letter.Should().Be("G7");
        deserialized.GetTile(7, 14)!.Letter.Should().Be("H7");
    }

    [Fact]
    public void Serialize_BoardWithMixedBlankAndNormalTiles_PersistsCorrectly()
    {
        // Arrange
        var board = new Board();
        board.SetTile(7, 7, new Tile { Letter = "A", Points = 1, IsBlank = false });
        board.SetTile(7, 8, new Tile { Letter = "", Points = 0, IsBlank = true, BlankRepresentation = "Q" });
        board.SetTile(7, 9, new Tile { Letter = "E", Points = 1, IsBlank = false });

        // Act
        var json = JsonSerializer.Serialize(board, _options);
        var deserialized = JsonSerializer.Deserialize<Board>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.GetTile(7, 7)!.Letter.Should().Be("A");
        deserialized.GetTile(7, 7)!.IsBlank.Should().BeFalse();
        deserialized.GetTile(7, 8)!.Letter.Should().Be("");
        deserialized.GetTile(7, 8)!.IsBlank.Should().BeTrue();
        deserialized.GetTile(7, 8)!.BlankRepresentation.Should().Be("Q");
        deserialized.GetTile(7, 9)!.Letter.Should().Be("E");
    }
}
