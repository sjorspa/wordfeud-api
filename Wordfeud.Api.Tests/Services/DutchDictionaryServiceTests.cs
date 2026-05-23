using Wordfeud.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Wordfeud.Api.Tests.Services;

/// <summary>
/// Unit tests for <see cref="DutchDictionaryService"/>.
/// </summary>
public class DutchDictionaryServiceTests
{
    private readonly ILogger<DutchDictionaryService> _logger = NullLogger<DutchDictionaryService>.Instance;

    [Fact]
    public void Contains_ShouldReturnTrueForKnownDutchWord()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("HUIS");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnTrueForAnotherDutchWord()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("LIEFDE");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnFalseForNonDutchWord()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("XYZ");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_ShouldReturnFalseForRandomString()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("ABCDEF");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_ShouldBeCaseInsensitive()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var resultUpper = service.Contains("HUIS");
        var resultLower = service.Contains("huis");

        // Assert
        resultUpper.Should().BeTrue();
        resultLower.Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnTrueForShortDutchWord()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("AAN");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnTrueForLongDutchWord()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("ONDERWIJS");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnFalseForEmptyString()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Contains_ShouldReturnFalseForSingleCharacter()
    {
        // Arrange
        var service = new DutchDictionaryService(_logger);

        // Act
        var result = service.Contains("A");

        // Assert
        result.Should().BeFalse();
    }
}
