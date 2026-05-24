using Wordfeud.Api.Models;
using System.Collections.Generic;

namespace Wordfeud.Api.Data;

/// <summary>
/// Provides board configuration including bonus squares for Wordfeud.
/// </summary>
public static class BoardConfiguration
{
    /// <summary>
    /// The board size (15x15).
    /// </summary>
    public const int BoardSize = 15;

    /// <summary>
    /// Triple Word squares.
    /// </summary>
    public static readonly HashSet<(int Row, int Column)> TripleWordSquares = new()
    {
        (0, 0), (0, 7), (0, 14),
        (7, 0), (7, 14),
        (14, 0), (14, 7), (14, 14)
    };

    /// <summary>
    /// Double Word squares.
    /// </summary>
    public static readonly HashSet<(int Row, int Column)> DoubleWordSquares = new()
    {
        (1, 1), (2, 2), (3, 3), (4, 4),
        (1, 13), (2, 12), (3, 11), (4, 10),
        (10, 4), (11, 3), (12, 2), (13, 1),
        (10, 10), (11, 11), (12, 12), (13, 13),
    };

    /// <summary>
    /// Triple Letter squares.
    /// </summary>
    public static readonly HashSet<(int Row, int Column)> TripleLetterSquares = new()
    {
        (1, 5), (1, 9),
        (5, 1), (5, 5), (5, 9), (5, 13),
        (9, 1), (9, 5), (9, 9), (9, 13),
        (13, 5), (13, 9)
    };

    /// <summary>
    /// Double Letter squares.
    /// </summary>
    public static readonly HashSet<(int Row, int Column)> DoubleLetterSquares = new()
    {
        (0, 3), (0, 11),
        (2, 6), (2, 8),
        (3, 0), (3, 7), (3, 14),
        (6, 2), (6, 6), (6, 8), (6, 12),
        (7, 3), (7, 11),
        (8, 2), (8, 6), (8, 8), (8, 12),
        (11, 0), (11, 7), (11, 14),
        (12, 6), (12, 8),
        (14, 3), (14, 11)
    };

    /// <summary>
    /// Gets the bonus type for a given board position.
    /// </summary>
    public static BonusType GetBonusType(int row, int column)
    {
        if (TripleWordSquares.Contains((row, column)))
            return BonusType.TripleWord;
        if (DoubleWordSquares.Contains((row, column)))
            return BonusType.DoubleWord;
        if (TripleLetterSquares.Contains((row, column)))
            return BonusType.TripleLetter;
        if (DoubleLetterSquares.Contains((row, column)))
            return BonusType.DoubleLetter;
        return BonusType.None;
    }

    /// <summary>
    /// Creates the official Dutch tile distribution and returns a shuffled bag.
    /// </summary>
    public static List<Tile> CreateTileBag()
    {
        var distribution = new (string Letter, int Points, int Count, bool IsBlank)[]
        {
            ("A", 1, 7, false),
            ("B", 4, 2, false),
            ("C", 5, 2, false),
            ("D", 2, 5, false),
            ("E", 1, 18, false),
            ("F", 4, 2, false),
            ("G", 3, 3, false),
            ("H", 4, 2, false),
            ("I", 2, 4, false),
            ("J", 4, 2, false),
            ("K", 3, 3, false),
            ("L", 3, 3, false),
            ("M", 3, 3, false),
            ("N", 1, 11, false),
            ("O", 1, 6, false),
            ("P", 4, 2, false),
            ("Q", 10, 1, false),
            ("R", 2, 5, false),
            ("S", 2, 5, false),
            ("T", 2, 5, false),
            ("U", 2, 3, false),
            ("V", 4, 2, false),
            ("W", 5, 2, false),
            ("X", 8, 1, false),
            ("Y", 8, 1, false),
            ("Z", 5, 2, false),
            ("", 0, 2, true)
        };

        var tiles = new List<Tile>();

        foreach (var (letter, points, count, isBlank) in distribution)
        {
            for (var i = 0; i < count; i++)
            {
                tiles.Add(new Tile
                {
                    Letter = letter,
                    Points = points,
                    IsBlank = isBlank,
                    BlankRepresentation = isBlank ? null : letter,
                    Id = Guid.NewGuid().ToString()
                });
            }
        }

        // Fisher-Yates shuffle
        var random = new Random();
        for (var i = tiles.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }

        return tiles;
    }
}
