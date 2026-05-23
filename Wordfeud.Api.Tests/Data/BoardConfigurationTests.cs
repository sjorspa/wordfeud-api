using Wordfeud.Api.Data;
using Wordfeud.Api.Models;

namespace Wordfeud.Api.Tests.Data;

/// <summary>
/// Unit tests for <see cref="BoardConfiguration"/>.
/// </summary>
public class BoardConfigurationTests
{
    [Fact]
    public void BoardSize_ShouldBe15x15()
    {
        // Act & Assert
        BoardConfiguration.BoardSize.Should().Be(15);
    }

    [Fact]
    public void GetBonusType_ShouldReturnDoubleWordForCenter()
    {
        // Act
        var result = BoardConfiguration.GetBonusType(7, 7);

        // Assert
        result.Should().Be(BonusType.DoubleWord);
    }

    [Fact]
    public void GetBonusType_ShouldReturnTripleWordForCorner()
    {
        // Act
        var result = BoardConfiguration.GetBonusType(0, 0);

        // Assert
        result.Should().Be(BonusType.TripleWord);
    }

    [Fact]
    public void GetBonusType_ShouldReturnDoubleWordForBonusSquare()
    {
        // Act
        var result = BoardConfiguration.GetBonusType(1, 1);

        // Assert
        result.Should().Be(BonusType.DoubleWord);
    }

    [Fact]
    public void GetBonusType_ShouldReturnTripleLetterForBonusSquare()
    {
        // Act
        var result = BoardConfiguration.GetBonusType(1, 5);

        // Assert
        result.Should().Be(BonusType.TripleLetter);
    }

    [Fact]
    public void GetBonusType_ShouldReturnDoubleLetterForBonusSquare()
    {
        // Act
        var result = BoardConfiguration.GetBonusType(0, 3);

        // Assert
        result.Should().Be(BonusType.DoubleLetter);
    }

    [Fact]
    public void GetBonusType_ShouldReturnNoneForRegularSquare()
    {
        // Act
        var result = BoardConfiguration.GetBonusType(0, 1);

        // Assert
        result.Should().Be(BonusType.None);
    }

    [Fact]
    public void TripleWordSquares_ShouldContainAllEightSquares()
    {
        // Assert
        BoardConfiguration.TripleWordSquares.Should().HaveCount(8);
        BoardConfiguration.TripleWordSquares.Should().Contain((0, 0));
        BoardConfiguration.TripleWordSquares.Should().Contain((0, 7));
        BoardConfiguration.TripleWordSquares.Should().Contain((0, 14));
        BoardConfiguration.TripleWordSquares.Should().Contain((7, 0));
        BoardConfiguration.TripleWordSquares.Should().Contain((7, 14));
        BoardConfiguration.TripleWordSquares.Should().Contain((14, 0));
        BoardConfiguration.TripleWordSquares.Should().Contain((14, 7));
        BoardConfiguration.TripleWordSquares.Should().Contain((14, 14));
    }

    [Fact]
    public void DoubleWordSquares_ShouldContainAllSquares()
    {
        // Assert
        BoardConfiguration.DoubleWordSquares.Should().HaveCount(17);
        BoardConfiguration.DoubleWordSquares.Should().Contain((1, 1));
        BoardConfiguration.DoubleWordSquares.Should().Contain((2, 2));
        BoardConfiguration.DoubleWordSquares.Should().Contain((13, 13));
        BoardConfiguration.DoubleWordSquares.Should().Contain((7, 7));
    }

    [Fact]
    public void TripleLetterSquares_ShouldContainAllSquares()
    {
        // Assert
        BoardConfiguration.TripleLetterSquares.Should().HaveCount(12);
        BoardConfiguration.TripleLetterSquares.Should().Contain((1, 5));
        BoardConfiguration.TripleLetterSquares.Should().Contain((5, 1));
        BoardConfiguration.TripleLetterSquares.Should().Contain((5, 5));
        BoardConfiguration.TripleLetterSquares.Should().Contain((9, 9));
    }

    [Fact]
    public void DoubleLetterSquares_ShouldContainAllSquares()
    {
        // Assert
        BoardConfiguration.DoubleLetterSquares.Should().HaveCount(24);
        BoardConfiguration.DoubleLetterSquares.Should().Contain((0, 3));
        BoardConfiguration.DoubleLetterSquares.Should().Contain((0, 11));
        BoardConfiguration.DoubleLetterSquares.Should().Contain((3, 0));
    }

    [Fact]
    public void CreateTileBag_ShouldReturn104Tiles()
    {
        // Act
        var bag = BoardConfiguration.CreateTileBag();

        // Assert
        bag.Should().HaveCount(104);
    }

    [Fact]
    public void CreateTileBag_ShouldHaveCorrectDistribution()
    {
        // Act
        var bag = BoardConfiguration.CreateTileBag();

        // Assert
        var byLetter = bag.GroupBy(t => t.Letter).ToDictionary(g => g.Key, g => g.Count());

        byLetter["A"].Should().Be(7);
        byLetter["B"].Should().Be(2);
        byLetter["C"].Should().Be(2);
        byLetter["D"].Should().Be(5);
        byLetter["E"].Should().Be(18);
        byLetter["F"].Should().Be(2);
        byLetter["G"].Should().Be(3);
        byLetter["H"].Should().Be(2);
        byLetter["I"].Should().Be(4);
        byLetter["J"].Should().Be(2);
        byLetter["K"].Should().Be(3);
        byLetter["L"].Should().Be(3);
        byLetter["M"].Should().Be(3);
        byLetter["N"].Should().Be(11);
        byLetter["O"].Should().Be(6);
        byLetter["P"].Should().Be(2);
        byLetter["Q"].Should().Be(1);
        byLetter["R"].Should().Be(5);
        byLetter["S"].Should().Be(5);
        byLetter["T"].Should().Be(5);
        byLetter["U"].Should().Be(3);
        byLetter["V"].Should().Be(2);
        byLetter["W"].Should().Be(2);
        byLetter["X"].Should().Be(1);
        byLetter["Y"].Should().Be(1);
        byLetter["Z"].Should().Be(2);
        byLetter[""].Should().Be(2); // Blanks
    }

    [Fact]
    public void CreateTileBag_ShouldHaveCorrectPoints()
    {
        // Act
        var bag = BoardConfiguration.CreateTileBag();

        // Assert
        var blankTiles = bag.Where(t => t.IsBlank).ToList();
        blankTiles.Should().AllSatisfy(t => t.Points.Should().Be(0));

        var qTiles = bag.Where(t => t.Letter == "Q").ToList();
        qTiles.Should().AllSatisfy(t => t.Points.Should().Be(10));

        var eTiles = bag.Where(t => t.Letter == "E").ToList();
        eTiles.Should().AllSatisfy(t => t.Points.Should().Be(1));

        var xTiles = bag.Where(t => t.Letter == "X").ToList();
        xTiles.Should().AllSatisfy(t => t.Points.Should().Be(8));
    }

    [Fact]
    public void CreateTileBag_ShouldBeShuffled()
    {
        // Act
        var bag1 = BoardConfiguration.CreateTileBag();
        var bag2 = BoardConfiguration.CreateTileBag();

        // Assert - at least one position should differ (statistically likely)
        var differentPositions = bag1
            .Zip(bag2)
            .Count(pair => pair.First.Id != pair.Second.Id);

        // With 102 tiles, the probability they're identical is astronomically low
        differentPositions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateTileBag_ShouldHaveAllTilesWithUniqueIds()
    {
        // Act
        var bag = BoardConfiguration.CreateTileBag();

        // Assert
        var ids = bag.Select(t => t.Id).ToList();
        ids.Should().HaveCount(bag.Count); // All unique
    }
}
