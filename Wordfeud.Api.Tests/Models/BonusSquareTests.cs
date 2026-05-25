using Wordfeud.Api.Models;

namespace Wordfeud.Api.Tests.Models;

/// <summary>
/// Unit tests for <see cref="BonusType"/> enum and <see cref="BonusSquare"/> class.
/// </summary>
public class BonusSquareTests
{
    #region BonusType Enum

    [Fact]
    public void BonusType_None_ShouldHaveValueZero()
    {
        // Assert
        ((int)BonusType.None).Should().Be(0);
    }

    [Fact]
    public void BonusType_TripleWord_ShouldHaveValueOne()
    {
        // Assert
        ((int)BonusType.TripleWord).Should().Be(1);
    }

    [Fact]
    public void BonusType_DoubleWord_ShouldHaveValueTwo()
    {
        // Assert
        ((int)BonusType.DoubleWord).Should().Be(2);
    }

    [Fact]
    public void BonusType_TripleLetter_ShouldHaveValueThree()
    {
        // Assert
        ((int)BonusType.TripleLetter).Should().Be(3);
    }

    [Fact]
    public void BonusType_DoubleLetter_ShouldHaveValueFour()
    {
        // Assert
        ((int)BonusType.DoubleLetter).Should().Be(4);
    }

    [Fact]
    public void BonusType_ShouldHaveFiveValues()
    {
        // Act
        var values = Enum.GetValues<BonusType>();

        // Assert
        values.Should().HaveCount(5);
    }

    [Fact]
    public void BonusType_ShouldContainAllExpectedValues()
    {
        // Act
        var values = Enum.GetValues<BonusType>();

        // Assert
        values.Should().Contain(BonusType.None);
        values.Should().Contain(BonusType.TripleWord);
        values.Should().Contain(BonusType.DoubleWord);
        values.Should().Contain(BonusType.TripleLetter);
        values.Should().Contain(BonusType.DoubleLetter);
    }

    [Fact]
    public void BonusType_FromString_ShouldReturnCorrectValue()
    {
        // Act
        var tripleWord = (BonusType)Enum.Parse(typeof(BonusType), "TripleWord");
        var doubleWord = (BonusType)Enum.Parse(typeof(BonusType), "DoubleWord");
        var tripleLetter = (BonusType)Enum.Parse(typeof(BonusType), "TripleLetter");
        var doubleLetter = (BonusType)Enum.Parse(typeof(BonusType), "DoubleLetter");
        var none = (BonusType)Enum.Parse(typeof(BonusType), "None");

        // Assert
        tripleWord.Should().Be(BonusType.TripleWord);
        doubleWord.Should().Be(BonusType.DoubleWord);
        tripleLetter.Should().Be(BonusType.TripleLetter);
        doubleLetter.Should().Be(BonusType.DoubleLetter);
        none.Should().Be(BonusType.None);
    }

    [Fact]
    public void BonusType_ToString_ShouldReturnCorrectName()
    {
        // Act & Assert
        BonusType.None.ToString().Should().Be("None");
        BonusType.TripleWord.ToString().Should().Be("TripleWord");
        BonusType.DoubleWord.ToString().Should().Be("DoubleWord");
        BonusType.TripleLetter.ToString().Should().Be("TripleLetter");
        BonusType.DoubleLetter.ToString().Should().Be("DoubleLetter");
    }

    [Fact]
    public void BonusType_FromInt_ShouldReturnCorrectValue()
    {
        // Act
        var none = (BonusType)0;
        var tripleWord = (BonusType)1;
        var doubleWord = (BonusType)2;
        var tripleLetter = (BonusType)3;
        var doubleLetter = (BonusType)4;

        // Assert
        none.Should().Be(BonusType.None);
        tripleWord.Should().Be(BonusType.TripleWord);
        doubleWord.Should().Be(BonusType.DoubleWord);
        tripleLetter.Should().Be(BonusType.TripleLetter);
        doubleLetter.Should().Be(BonusType.DoubleLetter);
    }

    #endregion

    #region BonusSquare Class

    [Fact]
    public void BonusSquare_DefaultConstructor_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var bonusSquare = new BonusSquare();

        // Assert
        bonusSquare.Row.Should().Be(0);
        bonusSquare.Column.Should().Be(0);
        bonusSquare.Type.Should().Be(BonusType.None);
    }

    [Fact]
    public void BonusSquare_SetProperties_ShouldStoreValues()
    {
        // Arrange
        var bonusSquare = new BonusSquare();

        // Act
        bonusSquare.Row = 7;
        bonusSquare.Column = 7;
        bonusSquare.Type = BonusType.TripleWord;

        // Assert
        bonusSquare.Row.Should().Be(7);
        bonusSquare.Column.Should().Be(7);
        bonusSquare.Type.Should().Be(BonusType.TripleWord);
    }

    [Fact]
    public void BonusSquare_CtorWithProperties_ShouldStoreValues()
    {
        // Arrange & Act
        var bonusSquare = new BonusSquare
        {
            Row = 0,
            Column = 0,
            Type = BonusType.DoubleWord
        };

        // Assert
        bonusSquare.Row.Should().Be(0);
        bonusSquare.Column.Should().Be(0);
        bonusSquare.Type.Should().Be(BonusType.DoubleWord);
    }

    [Fact]
    public void BonusSquare_Row_ShouldAcceptBoardBoundaries()
    {
        // Arrange
        var bonusSquare = new BonusSquare();

        // Act - minimum board position
        bonusSquare.Row = 0;
        bonusSquare.Column = 0;

        // Assert
        bonusSquare.Row.Should().Be(0);
        bonusSquare.Column.Should().Be(0);
    }

    [Fact]
    public void BonusSquare_Row_ShouldAcceptMaxBoardPosition()
    {
        // Arrange
        var bonusSquare = new BonusSquare();

        // Act - maximum board position
        bonusSquare.Row = 14;
        bonusSquare.Column = 14;

        // Assert
        bonusSquare.Row.Should().Be(14);
        bonusSquare.Column.Should().Be(14);
    }

    [Fact]
    public void BonusSquare_AllBonusTypes_ShouldBeAssignable()
    {
        // Arrange
        var bonusSquare = new BonusSquare();

        // Act & Assert - test each bonus type
        bonusSquare.Type = BonusType.None;
        bonusSquare.Type.Should().Be(BonusType.None);

        bonusSquare.Type = BonusType.TripleWord;
        bonusSquare.Type.Should().Be(BonusType.TripleWord);

        bonusSquare.Type = BonusType.DoubleWord;
        bonusSquare.Type.Should().Be(BonusType.DoubleWord);

        bonusSquare.Type = BonusType.TripleLetter;
        bonusSquare.Type.Should().Be(BonusType.TripleLetter);

        bonusSquare.Type = BonusType.DoubleLetter;
        bonusSquare.Type.Should().Be(BonusType.DoubleLetter);
    }

    [Fact]
    public void BonusSquare_Equality_ShouldCompareByValue()
    {
        // Arrange
        var bonusSquare1 = new BonusSquare { Row = 7, Column = 7, Type = BonusType.TripleWord };
        var bonusSquare2 = new BonusSquare { Row = 7, Column = 7, Type = BonusType.TripleWord };
        var bonusSquare3 = new BonusSquare { Row = 0, Column = 0, Type = BonusType.DoubleWord };

        // Act & Assert
        (bonusSquare1.Row == bonusSquare2.Row && bonusSquare1.Column == bonusSquare2.Column
            && bonusSquare1.Type == bonusSquare2.Type).Should().BeTrue();
        (bonusSquare1.Row == bonusSquare3.Row && bonusSquare1.Column == bonusSquare3.Column
            && bonusSquare1.Type == bonusSquare3.Type).Should().BeFalse();
    }

    [Fact]
    public void BonusSquare_ToString_ShouldReturnTypeName()
    {
        // Arrange
        var bonusSquare = new BonusSquare { Type = BonusType.TripleWord };

        // Act
        var result = bonusSquare.ToString();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    #endregion
}
