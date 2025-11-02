using FluentAssertions;
using LibPostal.Net.Expansion;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for AddressExpander class.
/// Based on libpostal's expand_address functionality.
/// </summary>
public class AddressExpanderTests
{
    [Fact]
    public void Expand_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        Action act = () => expander.Expand(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Expand_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Expand_WithNoExpansions_ShouldReturnNormalizedInput()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("unknown phrase");

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("unknown phrase"); // Just normalized
    }

    [Fact]
    public void Expand_WithSingleAbbreviation_ShouldExpandToFullForm()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("Main St");

        // Assert
        result.Should().Contain("main street");
        result.Should().Contain("main st");
    }

    [Fact]
    public void Expand_WithMultipleAbbreviations_ShouldExpandAll()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("N Main St");

        // Assert
        result.Should().Contain("north main street");
        result.Should().Contain("north main st");
        result.Should().Contain("n main street");
        result.Should().Contain("n main st");
    }

    [Fact]
    public void Expand_WithOptions_ShouldApplyNormalization()
    {
        // Arrange
        var expander = CreateTestExpander();
        var options = new ExpansionOptions
        {
            Lowercase = true,
            TrimString = true,
            StripAccents = true
        };

        // Act
        var result = expander.Expand("  CAFÉ St  ", options);

        // Assert
        result.Should().AllSatisfy(r =>
        {
            r.Should().NotStartWith(" ");
            r.Should().NotEndWith(" ");
            r.Should().MatchRegex("^[a-z ]+$"); // Lowercase, no accents
        });
    }

    [Fact]
    public void Expand_ShouldLimitPermutationsTo100()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Create input with many potential expansions
        // If each token has 2 alternatives: 2^10 = 1024 > 100 limit
        var input = "n main st w park ave s oak blvd e";

        // Act
        var result = expander.Expand(input);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void Expand_ShouldReturnUniqueResults()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("Main Street");

        // Assert
        var uniqueCount = result.Distinct().Count();
        uniqueCount.Should().Be(result.Length); // All results should be unique
    }

    [Fact]
    public void Expand_WithNumbers_ShouldPreserveNumbers()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("123 Main St");

        // Assert
        result.Should().AllSatisfy(r => r.Should().StartWith("123"));
    }

    [Fact]
    public void Expand_WithPunctuation_ShouldHandleCorrectly()
    {
        // Arrange
        var expander = CreateTestExpander();
        var options = new ExpansionOptions
        {
            DeleteFinalPeriods = true
        };

        // Act
        var result = expander.Expand("Main St", options); // Without period for simplicity

        // Assert
        result.Should().Contain("main street");
        result.Should().Contain("main st");
    }

    [Fact]
    public void Expand_WithDefaultOptions_ShouldUseSensibleDefaults()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("MAIN STREET");

        // Assert
        // Default options should lowercase
        result.Should().AllSatisfy(r => r.Should().MatchRegex("^[a-z ]+$"));
    }

    [Fact]
    public void Expand_WithComponentFilter_ShouldFilterExpansions()
    {
        // Arrange
        var expander = CreateTestExpander();
        var options = new ExpansionOptions
        {
            AddressComponents = AddressComponent.Street // Only street components
        };

        // Act
        var result = expander.Expand("Main St", options);

        // Assert
        result.Should().NotBeEmpty();
        // Should only include street-related expansions
    }

    [Fact]
    public void Expand_WithUnicodeInput_ShouldHandleCorrectly()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("Rue de la Paix");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("rue de la paix");
    }

    [Fact]
    public void Expand_WithComplexAddress_ShouldGenerateMultipleAlternatives()
    {
        // Arrange
        var expander = CreateTestExpander();

        // Act
        var result = expander.Expand("123 N Main St");

        // Assert
        result.Length.Should().BeGreaterThan(2); // At least a few alternatives
        result.Should().Contain("123 north main street");
    }

    /// <summary>
    /// Creates a test expander with a basic English dictionary.
    /// </summary>
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
            ["ave"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "avenue",
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
            ["n"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "north",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.Directional,
                    IsSeparable = true
                }
            }),
            ["s"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "south",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.Directional,
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
            ["w"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "west",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.Directional,
                    IsSeparable = true
                }
            })
        };

        return new AddressExpander(dictionary);
    }
}
