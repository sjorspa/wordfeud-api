namespace Wordfeud.Api.Models;

/// <summary>
/// Represents a bonus square type on the Wordfeud board.
/// </summary>
public enum BonusType
{
    /// <summary>
    /// No bonus.
    /// </summary>
    None = 0,

    /// <summary>
    /// Triple word score.
    /// </summary>
    TripleWord = 1,

    /// <summary>
    /// Double word score.
    /// </summary>
    DoubleWord = 2,

    /// <summary>
    /// Triple letter score.
    /// </summary>
    TripleLetter = 3,

    /// <summary>
    /// Double letter score.
    /// </summary>
    DoubleLetter = 4
}

/// <summary>
/// Represents a bonus square on the board.
/// </summary>
public class BonusSquare
{
    /// <summary>
    /// The row position (0-14).
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// The column position (0-14).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// The type of bonus applied.
    /// </summary>
    public BonusType Type { get; set; }

    /// <summary>
    /// Gets the bonus type string for a given board position.
    /// </summary>
    public static string GetBonusType(int row, int col)
    {
        var bonusSquares = new (int Row, int Col, string Type)[]
        {
            (0, 0, "TWS"), (0, 7, "TWS"), (0, 14, "TWS"),
            (7, 0, "TWS"), (7, 7, "TWS"), (7, 14, "TWS"),
            (14, 0, "TWS"), (14, 7, "TWS"), (14, 14, "TWS"),
            (1, 1, "DWS"), (2, 2, "DWS"), (3, 3, "DWS"), (4, 4, "DWS"),
            (1, 13, "DWS"), (2, 12, "DWS"), (3, 11, "DWS"), (4, 10, "DWS"),
            (13, 1, "DWS"), (12, 2, "DWS"), (11, 3, "DWS"), (10, 4, "DWS"),
            (13, 13, "DWS"), (12, 12, "DWS"), (11, 11, "DWS"), (10, 10, "DWS"),
            (1, 5, "DLS"), (1, 9, "DLS"),
            (5, 1, "DLS"), (5, 5, "DLS"), (5, 9, "DLS"), (5, 13, "DLS"),
            (9, 1, "DLS"), (9, 5, "DLS"), (9, 9, "DLS"), (9, 13, "DLS"),
            (13, 5, "DLS"), (13, 9, "DLS"),
            (2, 6, "TLS"), (6, 2, "TLS"), (8, 6, "TLS"), (6, 8, "TLS"),
        };

        var bonus = bonusSquares.FirstOrDefault(b => b.Row == row && b.Col == col);
        return bonus.Type;
    }
}
