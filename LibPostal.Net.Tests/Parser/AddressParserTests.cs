using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Integration tests for AddressParser.
/// </summary>
public class AddressParserTests
{
    [Fact]
    public void Parse_SimpleAddress_ShouldReturnComponents()
    {
        // Arrange
        var parser = CreateMockParser();

        // Act
        var response = parser.Parse("123 Main Street");

        // Assert
        response.Should().NotBeNull();
        response.NumComponents.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Parse_WithHouseNumberAndRoad_ShouldLabelCorrectly()
    {
        // Arrange
        var parser = CreateMockParser();

        // Act
        var response = parser.Parse("123 Main");

        // Assert
        response.Labels.Should().Contain("house_number");
        response.Labels.Should().Contain("road");
    }

    [Fact]
    public void Parse_NullInput_ShouldThrow()
    {
        // Arrange
        var parser = CreateMockParser();

        // Act
        Action act = () => parser.Parse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parse_EmptyString_ShouldReturnEmptyResponse()
    {
        // Arrange
        var parser = CreateMockParser();

        // Act
        var response = parser.Parse("");

        // Assert
        response.NumComponents.Should().Be(0);
    }

    private static AddressParser CreateMockParser()
    {
        // Create minimal mock CRF model for testing
        var crf = new Crf(new[] { "house_number", "road", "city" });

        // Add simple features
        var numericFeat = crf.AddStateFeature("is_numeric");
        crf.SetWeight(numericFeat, 0, 5.0); // is_numeric → house_number

        var wordFeat = crf.AddStateFeature("word=main");
        crf.SetWeight(wordFeat, 1, 3.0); // word=main → road

        // Transitions
        crf.SetTransWeight(0, 1, 1.0); // house_number → road
        crf.SetTransWeight(1, 2, 1.0); // road → city

        return new AddressParser(crf);
    }
}
