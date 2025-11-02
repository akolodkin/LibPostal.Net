using FluentAssertions;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Tokenization;

/// <summary>
/// Tests for Tokenizer class.
/// Based on libpostal's tokenization logic.
/// </summary>
public class TokenizerTests
{
    [Fact]
    public void Tokenize_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        Action act = () => tokenizer.Tokenize(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Tokenize_WithEmptyString_ShouldReturnEmptyTokenizedString()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("");

        // Assert
        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();
        result.Count.Should().Be(0);
    }

    [Fact]
    public void Tokenize_WithSingleWord_ShouldReturnWordToken()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("hello");

        // Assert
        result.Count.Should().Be(1);
        result[0].Text.Should().Be("hello");
        result[0].Type.Should().Be(TokenType.Word);
        result[0].Offset.Should().Be(0);
        result[0].Length.Should().Be(5);
    }

    [Fact]
    public void Tokenize_WithMultipleWords_ShouldReturnMultipleTokens()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("hello world");

        // Assert
        result.Count.Should().Be(3);
        result[0].Text.Should().Be("hello");
        result[0].Type.Should().Be(TokenType.Word);
        result[1].Text.Should().Be(" ");
        result[1].Type.Should().Be(TokenType.Whitespace);
        result[2].Text.Should().Be("world");
        result[2].Type.Should().Be(TokenType.Word);
    }

    [Fact]
    public void Tokenize_WithNumbers_ShouldReturnNumericTokens()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("123 Main St");

        // Assert
        result.GetTokensByType(TokenType.Numeric).Should().HaveCount(1);
        result.GetTokensByType(TokenType.Numeric)[0].Text.Should().Be("123");
    }

    [Fact]
    public void Tokenize_WithPunctuation_ShouldReturnPunctuationTokens()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("Hello, world!");

        // Assert
        result.GetTokensByType(TokenType.Comma).Should().HaveCount(1);
        result.GetTokensByType(TokenType.Exclamation).Should().HaveCount(1);
    }

    [Fact]
    public void Tokenize_WithEmail_ShouldDetectEmailToken()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("Contact: user@example.com");

        // Assert
        var emailTokens = result.GetTokensByType(TokenType.Email);
        emailTokens.Should().HaveCount(1);
        emailTokens[0].Text.Should().Be("user@example.com");
    }

    [Fact]
    public void Tokenize_WithUrl_ShouldDetectUrlToken()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("Visit https://example.com");

        // Assert
        var urlTokens = result.GetTokensByType(TokenType.Url);
        urlTokens.Should().HaveCount(1);
        urlTokens[0].Text.Should().Be("https://example.com");
    }

    [Fact]
    public void Tokenize_WithAbbreviation_ShouldDetectAbbreviationToken()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("123 Main St.");

        // Assert
        // "St." should be detected as an abbreviation
        var tokens = result.GetTokensWithoutWhitespace();
        tokens.Should().Contain(t => t.Text == "St.");
    }

    [Fact]
    public void Tokenize_WithUnicodeChinese_ShouldDetectIdeographicTokens()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("北京市");

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(0);
        // Each Chinese character should be a separate ideographic token
        result.Tokens.Should().Contain(t => t.Type == TokenType.IdeographicChar);
    }

    [Fact]
    public void Tokenize_WithUnicodeKorean_ShouldDetectHangulTokens()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("서울시");

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(0);
        // Korean syllables should be detected
        result.Tokens.Should().Contain(t => t.Type == TokenType.HangulSyllable);
    }

    [Fact]
    public void Tokenize_WithUnicodeArabic_ShouldHandleCorrectly()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("شارع الملك");

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(0);
        result.GetTokensWithoutWhitespace().Should().NotBeEmpty();
    }

    [Fact]
    public void Tokenize_WithMixedContent_ShouldHandleAllTypes()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("123 Main St, Apt 4B");

        // Assert
        result.GetTokensByType(TokenType.Numeric).Should().HaveCount(2); // "123" and "4"
        result.GetTokensByType(TokenType.Word).Should().NotBeEmpty(); // "Main", "Apt", "B"
        result.GetTokensByType(TokenType.Comma).Should().HaveCount(1);
    }

    [Fact]
    public void Tokenize_WithWhitespaceOnly_ShouldReturnWhitespaceToken()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("   ");

        // Assert
        result.Count.Should().Be(1);
        result[0].Type.Should().Be(TokenType.Whitespace);
        result[0].Text.Should().Be("   ");
    }

    [Fact]
    public void Tokenize_WithNewlines_ShouldDetectNewlineTokens()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("line1\nline2");

        // Assert
        result.GetTokensByType(TokenType.Newline).Should().HaveCount(1);
    }

    [Fact]
    public void Tokenize_WithAcronym_ShouldDetectAcronymToken()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("U.S.A. or USA");

        // Assert
        // Should detect at least one acronym pattern
        var tokens = result.GetTokensWithoutWhitespace();
        tokens.Should().Contain(t => t.Text.Contains("U.S.A") || t.Text.Contains("USA"));
    }

    [Fact]
    public void Tokenize_WithHyphenatedWord_ShouldHandleCorrectly()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("twenty-one");

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(0);
        // Should have tokens for "twenty", "-", "one"
        result.GetTokensByType(TokenType.Hyphen).Should().HaveCount(1);
    }

    [Fact]
    public void Tokenize_ShouldMaintainCorrectOffsets()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var input = "123 Main Street";

        // Act
        var result = tokenizer.Tokenize(input);

        // Assert
        foreach (var token in result)
        {
            var extractedText = input.Substring(token.Offset, token.Length);
            extractedText.Should().Be(token.Text, $"Token at offset {token.Offset} should match");
        }
    }

    [Fact]
    public void Tokenize_WithComplexAddress_ShouldTokenizeCorrectly()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var result = tokenizer.Tokenize("1600 Pennsylvania Ave NW, Washington, DC 20500");

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().BeGreaterThan(10); // Multiple tokens
        result.GetTokensByType(TokenType.Numeric).Should().HaveCount(2); // "1600" and "20500"
        result.GetTokensByType(TokenType.Word).Should().NotBeEmpty();
        result.GetTokensByType(TokenType.Comma).Should().HaveCount(2);
    }
}
