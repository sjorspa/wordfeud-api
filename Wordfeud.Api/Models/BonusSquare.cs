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
}
