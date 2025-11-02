using FluentAssertions;
using LibPostal.Net.Tokenization;
using System.Text;

namespace LibPostal.Net.Tests.Tokenization;

/// <summary>
/// Tests for StringNormalizer class.
/// Based on libpostal's normalize.c
/// </summary>
public class StringNormalizerTests
{
    [Fact]
    public void Normalize_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        Action act = () => normalizer.Normalize(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Normalize_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Normalize_ToLowercase_ShouldConvertToLower()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("HELLO WORLD", NormalizationOptions.Lowercase);

        // Assert
        result.Should().Be("hello world");
    }

    [Fact]
    public void Normalize_Trim_ShouldRemoveLeadingTrailingWhitespace()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("  hello world  ", NormalizationOptions.Trim);

        // Assert
        result.Should().Be("hello world");
    }

    [Fact]
    public void Normalize_ToNFC_ShouldComposeCharacters()
    {
        // Arrange
        var normalizer = new StringNormalizer();
        var decomposed = "e\u0301"; // é decomposed (e + combining acute)

        // Act
        var result = normalizer.Normalize(decomposed, NormalizationOptions.Compose);

        // Assert
        result.Should().Be("é"); // é composed
        result.Length.Should().Be(1);
    }

    [Fact]
    public void Normalize_ToNFD_ShouldDecomposeCharacters()
    {
        // Arrange
        var normalizer = new StringNormalizer();
        var composed = "é"; // é composed

        // Act
        var result = normalizer.Normalize(composed, NormalizationOptions.Decompose);

        // Assert
        result.Should().Be("e\u0301"); // é decomposed
        result.Length.Should().Be(2);
    }

    [Fact]
    public void Normalize_StripAccents_ShouldRemoveDiacritics()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("café", NormalizationOptions.StripAccents);

        // Assert
        result.Should().Be("cafe");
    }

    [Fact]
    public void Normalize_ReplaceHyphens_ShouldReplaceWithSpaces()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("twenty-one", NormalizationOptions.ReplaceHyphens);

        // Assert
        result.Should().Be("twenty one");
    }

    [Fact]
    public void Normalize_WithMultipleOptions_ShouldApplyAll()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize(
            "  CAFÉ  ",
            NormalizationOptions.Lowercase | NormalizationOptions.Trim | NormalizationOptions.StripAccents);

        // Assert
        result.Should().Be("cafe");
    }

    [Fact]
    public void Normalize_UnicodeGerman_ShouldHandleCorrectly()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("Bünderstraße", NormalizationOptions.Lowercase);

        // Assert
        result.Should().Be("bünderstraße");
    }

    [Fact]
    public void Normalize_UnicodeRussian_ShouldHandleCorrectly()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("МОСКВА", NormalizationOptions.Lowercase);

        // Assert
        result.Should().Be("москва");
    }

    [Fact]
    public void Normalize_UnicodeArabic_ShouldHandleCorrectly()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize("العربية", NormalizationOptions.None);

        // Assert
        result.Should().Be("العربية");
    }

    [Fact]
    public void Normalize_WithNoOptions_ShouldReturnUnchanged()
    {
        // Arrange
        var normalizer = new StringNormalizer();
        var input = "Hello World";

        // Act
        var result = normalizer.Normalize(input, NormalizationOptions.None);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Normalize_ComplexString_ShouldHandleAllTransformations()
    {
        // Arrange
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize(
            "  SAINT-ANDRÉ'S CAFÉ  ",
            NormalizationOptions.Lowercase |
            NormalizationOptions.Trim |
            NormalizationOptions.StripAccents |
            NormalizationOptions.ReplaceHyphens);

        // Assert
        result.Should().Be("saint andre's cafe");
    }

    [Fact]
    public void Normalize_WithComposeAndDecompose_DecomposeShouldWin()
    {
        // Arrange - both flags set, decompose should take precedence
        var normalizer = new StringNormalizer();

        // Act
        var result = normalizer.Normalize(
            "é",
            NormalizationOptions.Compose | NormalizationOptions.Decompose);

        // Assert - decompose wins
        result.Should().Be("e\u0301");
    }
}
