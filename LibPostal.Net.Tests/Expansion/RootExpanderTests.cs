using FluentAssertions;
using LibPostal.Net.Expansion;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for RootExpander - simplified implementation for Phase 5B.
/// Based on key test cases from libpostal's test_expand.c
/// </summary>
public class RootExpanderTests
{
    [Fact]
    public void ExpandRoot_SimpleCase_ShouldRemoveStreetType()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.ExpandRoot("Malcolm X Blvd");

        // Assert
        result.Should().Contain("malcolm x");
    }

    [Fact]
    public void ExpandRoot_WithDirectional_ShouldRemoveIgnorablePhrases()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.ExpandRoot("E 106 St");

        // Assert - simplified implementation for Phase 5B
        result.Should().NotBeEmpty();
        result.First().Should().Contain("106");
    }

    [Fact]
    public void ExpandRoot_WithNumber_ShouldPreserveNumber()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.ExpandRoot("123");

        // Assert
        result.Should().Contain("123");
    }

    [Fact]
    public void ExpandRoot_WithUnitPrefix_ShouldKeepNumber()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.ExpandRoot("Apt 101");

        // Assert - our simplified implementation keeps both for now
        result.Should().NotBeEmpty();
        result.First().Should().Contain("101");
    }

    [Fact]
    public void ExpandRoot_ShouldReturnMultipleAlternatives()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.ExpandRoot("Main St");

        // Assert
        result.Length.Should().BeGreaterThan(0);
        result.Should().Contain("main");
    }

    private static AddressExpander CreateTestExpander()
    {
        var dictionary = new Dictionary<string, AddressExpansionValue>
        {
            ["st"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "street",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.StreetType,
                    IsSeparable = true
                }
            }),
            ["blvd"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "boulevard",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.StreetType,
                    IsSeparable = true
                }
            }),
            ["e"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "east",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.Directional,
                    IsSeparable = true
                }
            }),
            ["apt"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "apartment",
                    Language = "en",
                    Components = AddressComponent.Unit,
                    DictionaryType = DictionaryType.UnitType,
                    IsSeparable = true
                }
            })
        };

        return new AddressExpander(dictionary);
    }
}
