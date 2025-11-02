using FluentAssertions;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Tokenization;

/// <summary>
/// Tests for TokenType enum and Token struct.
/// Based on libpostal's token_types.h and tokens.h
/// </summary>
public class TokenTypeTests
{
    [Fact]
    public void TokenType_ShouldHaveWordType()
    {
        // Arrange & Act
        var type = TokenType.Word;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveAbbreviationType()
    {
        // Arrange & Act
        var type = TokenType.Abbreviation;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveEmailType()
    {
        // Arrange & Act
        var type = TokenType.Email;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveUrlType()
    {
        // Arrange & Act
        var type = TokenType.Url;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveUsPhoneType()
    {
        // Arrange & Act
        var type = TokenType.UsPhone;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveInternationalPhoneType()
    {
        // Arrange & Act
        var type = TokenType.InternationalPhone;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveNumericType()
    {
        // Arrange & Act
        var type = TokenType.Numeric;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveOrdinalType()
    {
        // Arrange & Act
        var type = TokenType.Ordinal;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveRomanNumeralType()
    {
        // Arrange & Act
        var type = TokenType.RomanNumeral;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveIdeographicNumberType()
    {
        // Arrange & Act
        var type = TokenType.IdeographicNumber;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveIdeographicCharType()
    {
        // Arrange & Act
        var type = TokenType.IdeographicChar;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveHangulSyllableType()
    {
        // Arrange & Act
        var type = TokenType.HangulSyllable;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveAcronymType()
    {
        // Arrange & Act
        var type = TokenType.Acronym;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHavePhraseType()
    {
        // Arrange & Act
        var type = TokenType.Phrase;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHavePunctuationTypes()
    {
        // Assert - verify all punctuation types exist
        TokenType.Period.Should().BeDefined();
        TokenType.Comma.Should().BeDefined();
        TokenType.Semicolon.Should().BeDefined();
        TokenType.Colon.Should().BeDefined();
        TokenType.Exclamation.Should().BeDefined();
        TokenType.Question.Should().BeDefined();
        TokenType.Quote.Should().BeDefined();
        TokenType.DoubleQuote.Should().BeDefined();
        TokenType.LeftParen.Should().BeDefined();
        TokenType.RightParen.Should().BeDefined();
        TokenType.LeftBracket.Should().BeDefined();
        TokenType.RightBracket.Should().BeDefined();
        TokenType.LeftBrace.Should().BeDefined();
        TokenType.RightBrace.Should().BeDefined();
        TokenType.Dash.Should().BeDefined();
        TokenType.Hyphen.Should().BeDefined();
        TokenType.Underscore.Should().BeDefined();
        TokenType.Plus.Should().BeDefined();
        TokenType.Ampersand.Should().BeDefined();
        TokenType.At.Should().BeDefined();
        TokenType.Pound.Should().BeDefined();
        TokenType.Dollar.Should().BeDefined();
        TokenType.Percent.Should().BeDefined();
        TokenType.Asterisk.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveWhitespaceType()
    {
        // Arrange & Act
        var type = TokenType.Whitespace;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveNewlineType()
    {
        // Arrange & Act
        var type = TokenType.Newline;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveOtherType()
    {
        // Arrange & Act
        var type = TokenType.Other;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void TokenType_ShouldHaveInvalidCharType()
    {
        // Arrange & Act
        var type = TokenType.InvalidChar;

        // Assert
        type.Should().BeDefined();
    }

    [Fact]
    public void Token_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var token = new Token("test", TokenType.Word, 0, 4);

        // Assert
        token.Text.Should().Be("test");
        token.Type.Should().Be(TokenType.Word);
        token.Offset.Should().Be(0);
        token.Length.Should().Be(4);
    }

    [Fact]
    public void Token_ShouldCalculateEndPositionCorrectly()
    {
        // Arrange
        var token = new Token("test", TokenType.Word, 5, 4);

        // Act
        var end = token.End;

        // Assert
        end.Should().Be(9); // 5 + 4
    }

    [Fact]
    public void Token_ShouldSupportValueEquality()
    {
        // Arrange
        var token1 = new Token("test", TokenType.Word, 0, 4);
        var token2 = new Token("test", TokenType.Word, 0, 4);

        // Act & Assert
        token1.Should().Be(token2);
        (token1 == token2).Should().BeTrue();
    }

    [Fact]
    public void Token_ShouldProvideReadableToString()
    {
        // Arrange
        var token = new Token("test", TokenType.Word, 5, 4);

        // Act
        var result = token.ToString();

        // Assert
        result.Should().Contain("Word");
        result.Should().Contain("test");
        result.Should().Contain("5");
        result.Should().Contain("9"); // end position
    }

    [Fact]
    public void Token_WithConstructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var token = new Token("hello", TokenType.Word, 10, 5);

        // Assert
        token.Text.Should().Be("hello");
        token.Type.Should().Be(TokenType.Word);
        token.Offset.Should().Be(10);
        token.Length.Should().Be(5);
        token.End.Should().Be(15);
    }

    [Theory]
    [InlineData("test", TokenType.Word, 0, 4)]
    [InlineData("user@example.com", TokenType.Email, 5, 16)]
    [InlineData("123", TokenType.Numeric, 10, 3)]
    [InlineData("1st", TokenType.Ordinal, 0, 3)]
    [InlineData(",", TokenType.Comma, 4, 1)]
    public void Token_WithVariousTypes_ShouldStoreCorrectly(string text, TokenType type, int offset, int length)
    {
        // Arrange & Act
        var token = new Token(text, type, offset, length);

        // Assert
        token.Text.Should().Be(text);
        token.Type.Should().Be(type);
        token.Offset.Should().Be(offset);
        token.Length.Should().Be(length);
    }
}
