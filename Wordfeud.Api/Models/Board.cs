using System.Text.Json;
using System.Text.Json.Serialization;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Models;

/// <summary>
/// Represents the 15x15 Wordfeud board with tile placement and retrieval capabilities.
/// </summary>
public sealed class Board
{
    private const int BoardSize = 15;
    private readonly Tile?[,] _tiles = new Tile?[BoardSize, BoardSize];

    /// <summary>
    /// Gets the size of the board (15).
    /// </summary>
    public int Size => BoardSize;

    /// <summary>
    /// Gets or sets a tile at the specified position.
    /// </summary>
    public Tile? this[int row, int col]
    {
        get => _tiles[row, col];
        set => _tiles[row, col] = value;
    }

    /// <summary>
    /// Gets a tile at the specified position.
    /// </summary>
    public Tile? GetTile(int row, int col)
    {
        return _tiles[row, col];
    }

    /// <summary>
    /// Sets a tile at the specified position.
    /// </summary>
    public void SetTile(int row, int col, Tile? tile)
    {
        _tiles[row, col] = tile;
    }

    /// <summary>
    /// Checks if the specified position is occupied.
    /// </summary>
    public bool IsOccupied(int row, int col)
    {
        return _tiles[row, col] != null;
    }

    /// <summary>
    /// Checks if the specified position is within the board boundaries.
    /// </summary>
    public bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;
    }
}
