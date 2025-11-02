using System.Text;
using FluentAssertions;
using LibPostal.Net.Core;

namespace LibPostal.Net.Tests.Core;

/// <summary>
/// Tests for String utility functions.
/// Ported from libpostal/test/test_string_utils.c
/// </summary>
public class StringUtilsTests
{
    [Fact]
    public void Reverse_WithAsciiString_ShouldReverseCorrectly()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = StringUtils.Reverse(input);

        // Assert
        result.Should().Be("olleh");
    }

    [Fact]
    public void Reverse_WithUnicodeString_ShouldReverseCorrectly()
    {
        // Arrange - German: "Bünderstraße"
        var input = "Bünderstraße";

        // Act
        var result = StringUtils.Reverse(input);

        // Assert
        result.Should().Be("eßartsrednüB");
    }

    [Theory]
    [InlineData("hello", "olleh")]
    [InlineData("café", "éfac")]
    [InlineData("北京", "京北")]
    [InlineData("Москва", "авксоМ")]
    [InlineData("العربية", "ةيبرعلا")]
    public void Reverse_VariousUnicodeStrings_ShouldReverseCorrectly(string input, string expected)
    {
        // Act
        var result = StringUtils.Reverse(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Reverse_EmptyString_ShouldReturnEmpty()
    {
        // Arrange
        var input = "";

        // Act
        var result = StringUtils.Reverse(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Reverse_NullString_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => StringUtils.Reverse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Trim_WithLeadingAndTrailingSpaces_ShouldTrim()
    {
        // Arrange
        var input = "  hello world  ";

        // Act
        var result = StringUtils.Trim(input);

        // Assert
        result.Should().Be("hello world");
    }

    [Fact]
    public void Trim_WithTabsAndNewlines_ShouldTrim()
    {
        // Arrange
        var input = "\t\nhello\t\n";

        // Act
        var result = StringUtils.Trim(input);

        // Assert
        result.Should().Be("hello");
    }

    [Theory]
    [InlineData("  test  ", "test")]
    [InlineData("\ttest\t", "test")]
    [InlineData("\ntest\n", "test")]
    [InlineData(" \t\ntest \t\n", "test")]
    [InlineData("test", "test")]
    [InlineData("", "")]
    public void Trim_VariousInputs_ShouldTrimCorrectly(string input, string expected)
    {
        // Act
        var result = StringUtils.Trim(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Split_WithDelimiter_ShouldSplitCorrectly()
    {
        // Arrange
        var input = "The|Low|End|Theory";

        // Act
        var result = StringUtils.Split(input, '|');

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().Be("The");
        result[1].Should().Be("Low");
        result[2].Should().Be("End");
        result[3].Should().Be("Theory");
    }

    [Fact]
    public void Split_WithNoDelimiter_ShouldReturnSingleElement()
    {
        // Arrange
        var input = "NoDelimiterHere";

        // Act
        var result = StringUtils.Split(input, '|');

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("NoDelimiterHere");
    }

    [Fact]
    public void Split_EmptyString_ShouldReturnEmptyArray()
    {
        // Arrange
        var input = "";

        // Act
        var result = StringUtils.Split(input, '|');

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Split_WithConsecutiveDelimiters_ShouldIncludeEmptyStrings()
    {
        // Arrange
        var input = "a||b";

        // Act
        var result = StringUtils.Split(input, '|');

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be("a");
        result[1].Should().BeEmpty();
        result[2].Should().Be("b");
    }

    [Fact]
    public void IsNullOrWhiteSpace_WithNull_ShouldReturnTrue()
    {
        // Act
        var result = StringUtils.IsNullOrWhiteSpace(null);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("\t", true)]
    [InlineData("\n", true)]
    [InlineData(" \t\n ", true)]
    [InlineData("a", false)]
    [InlineData(" a ", false)]
    [InlineData("  hello  ", false)]
    public void IsNullOrWhiteSpace_VariousInputs_ShouldReturnCorrectResult(string? input, bool expected)
    {
        // Act
        var result = StringUtils.IsNullOrWhiteSpace(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Normalize_WithNFC_ShouldNormalizeToComposed()
    {
        // Arrange - decomposed form é (e + combining acute)
        var input = "e\u0301"; // é decomposed

        // Act
        var result = StringUtils.Normalize(input, NormalizationForm.FormC);

        // Assert
        result.Should().Be("é"); // é composed
    }

    [Fact]
    public void Normalize_WithNFD_ShouldNormalizeToDecomposed()
    {
        // Arrange - composed form é
        var input = "é";

        // Act
        var result = StringUtils.Normalize(input, NormalizationForm.FormD);

        // Assert
        result.Should().Be("e\u0301"); // é decomposed (e + combining acute)
    }

    [Fact]
    public void ToLower_WithMixedCase_ShouldConvertToLowerCase()
    {
        // Arrange
        var input = "Hello WORLD";

        // Act
        var result = StringUtils.ToLower(input);

        // Assert
        result.Should().Be("hello world");
    }

    [Fact]
    public void ToLower_WithUnicode_ShouldConvertToLowerCase()
    {
        // Arrange
        var input = "CAFÉ";

        // Act
        var result = StringUtils.ToLower(input);

        // Assert
        result.Should().Be("café");
    }

    [Fact]
    public void ToUpper_WithMixedCase_ShouldConvertToUpperCase()
    {
        // Arrange
        var input = "Hello world";

        // Act
        var result = StringUtils.ToUpper(input);

        // Assert
        result.Should().Be("HELLO WORLD");
    }
}
