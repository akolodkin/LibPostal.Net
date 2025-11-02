using FluentAssertions;
using LibPostal.Net.Expansion;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for PhraseClassifier.
/// Based on libpostal's address_phrase_* helper functions.
/// </summary>
public class PhraseClassifierTests
{
    [Fact]
    public void IsIgnorableForComponents_WithNullPhrase_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => PhraseClassifier.IsIgnorableForComponents(null!, AddressComponent.Street);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsIgnorableForComponents_WithIgnorableDictionaryType_ShouldReturnTrue()
    {
        // Arrange - "st" is StreetType, ignorable for Street component
        var phrase = CreatePhrase("st", DictionaryType.StreetType, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsIgnorableForComponents(phrase, AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIgnorableForComponents_WithNonIgnorableDictionaryType_ShouldReturnFalse()
    {
        // Arrange - "main" is StreetName, not ignorable for Street
        var phrase = CreatePhrase("main", DictionaryType.StreetName, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsIgnorableForComponents(phrase, AddressComponent.Street);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEdgeIgnorableForComponents_WithEdgeIgnorable_ShouldReturnTrue()
    {
        // Arrange - directionals are edge-ignorable for streets
        var phrase = CreatePhrase("n", DictionaryType.Directional, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsEdgeIgnorableForComponents(phrase, AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEdgeIgnorableForComponents_WithNonEdgeIgnorable_ShouldReturnFalse()
    {
        // Arrange - street types are not edge-ignorable
        var phrase = CreatePhrase("st", DictionaryType.StreetType, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsEdgeIgnorableForComponents(phrase, AddressComponent.Street);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPossibleRootForComponents_WithPossibleRoot_ShouldReturnTrue()
    {
        // Arrange - directional "E" could be root in "Avenue E"
        var phrase = CreatePhrase("e", DictionaryType.Directional, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsPossibleRootForComponents(phrase, AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPossibleRootForComponents_WithNonRoot_ShouldReturnFalse()
    {
        // Arrange - street type "St" is not a root
        var phrase = CreatePhrase("st", DictionaryType.StreetType, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsPossibleRootForComponents(phrase, AddressComponent.Street);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasCanonicalInterpretation_WithCanonical_ShouldReturnTrue()
    {
        // Arrange - phrase has a canonical expansion
        var expansion = new AddressExpansion
        {
            Canonical = "street",
            Language = "en",
            Components = AddressComponent.Street,
            DictionaryType = DictionaryType.StreetType,
            IsSeparable = true
        };

        var phrase = new Phrase
        {
            StartIndex = 0,
            Length = 1,
            Value = "st",
            Expansions = new AddressExpansionValue(new[] { expansion })
        };

        // Act
        var result = PhraseClassifier.HasCanonicalInterpretation(phrase);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasCanonicalInterpretation_WithNoCanonical_ShouldReturnFalse()
    {
        // Arrange - phrase with no canonical expansion (canonical is null)
        var expansion = new AddressExpansion
        {
            Canonical = null,
            Language = "en",
            Components = AddressComponent.Street,
            DictionaryType = DictionaryType.StreetType,
            IsSeparable = true
        };

        var phrase = new Phrase
        {
            StartIndex = 0,
            Length = 1,
            Value = "street",
            Expansions = new AddressExpansionValue(new[] { expansion })
        };

        // Act
        var result = PhraseClassifier.HasCanonicalInterpretation(phrase);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasCanonicalInterpretation_WithNoExpansions_ShouldReturnFalse()
    {
        // Arrange
        var phrase = new Phrase
        {
            StartIndex = 0,
            Length = 1,
            Value = "test",
            Expansions = null
        };

        // Act
        var result = PhraseClassifier.HasCanonicalInterpretation(phrase);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void InDictionary_WithMatchingType_ShouldReturnTrue()
    {
        // Arrange
        var phrase = CreatePhrase("st", DictionaryType.StreetType, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.InDictionary(phrase, DictionaryType.StreetType);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void InDictionary_WithNonMatchingType_ShouldReturnFalse()
    {
        // Arrange
        var phrase = CreatePhrase("st", DictionaryType.StreetType, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.InDictionary(phrase, DictionaryType.Directional);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void InDictionary_WithNoExpansions_ShouldReturnFalse()
    {
        // Arrange
        var phrase = new Phrase
        {
            StartIndex = 0,
            Length = 1,
            Value = "test",
            Expansions = null
        };

        // Act
        var result = PhraseClassifier.InDictionary(phrase, DictionaryType.StreetType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidForComponents_WithValidComponents_ShouldReturnTrue()
    {
        // Arrange - StreetType is valid for Street component
        var phrase = CreatePhrase("st", DictionaryType.StreetType, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsValidForComponents(phrase, AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidForComponents_WithInvalidComponents_ShouldReturnFalse()
    {
        // Arrange - StreetType is not valid for Unit component
        var phrase = CreatePhrase("st", DictionaryType.StreetType, AddressComponent.Street);

        // Act
        var result = PhraseClassifier.IsValidForComponents(phrase, AddressComponent.Unit);

        // Assert
        result.Should().BeFalse();
    }

    private static Phrase CreatePhrase(string value, DictionaryType dictionaryType, AddressComponent components)
    {
        var expansion = new AddressExpansion
        {
            Canonical = value == "st" ? "street" : null,
            Language = "en",
            Components = components,
            DictionaryType = dictionaryType,
            IsSeparable = true
        };

        return new Phrase
        {
            StartIndex = 0,
            Length = 1,
            Value = value,
            Expansions = new AddressExpansionValue(new[] { expansion })
        };
    }
}
