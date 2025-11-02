using FluentAssertions;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Tokenization;

/// <summary>
/// Tests for TokenizedString class.
/// Based on libpostal's tokenized_string_t structure.
/// </summary>
public class TokenizedStringTests
{
    [Fact]
    public void Constructor_WithNullOriginalString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tokens = new List<Token>();

        // Act
        Action act = () => new TokenizedString(null!, tokens);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullTokens_ShouldThrowArgumentNullException()
    {
        // Arrange
        var originalString = "test";

        // Act
        Action act = () => new TokenizedString(originalString, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidInputs_ShouldInitializeCorrectly()
    {
        // Arrange
        var originalString = "123 Main St";
        var tokens = new List<Token>
        {
            new Token("123", TokenType.Numeric, 0, 3),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("Main", TokenType.Word, 4, 4),
            new Token(" ", TokenType.Whitespace, 8, 1),
            new Token("St", TokenType.Abbreviation, 9, 2)
        };

        // Act
        var result = new TokenizedString(originalString, tokens);

        // Assert
        result.OriginalString.Should().Be(originalString);
        result.Tokens.Should().HaveCount(5);
        result.Tokens.Should().BeEquivalentTo(tokens);
    }

    [Fact]
    public void Tokens_ShouldBeReadOnly()
    {
        // Arrange
        var originalString = "test";
        var tokens = new List<Token> { new Token("test", TokenType.Word, 0, 4) };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act & Assert
        tokenizedString.Tokens.Should().BeAssignableTo<IReadOnlyList<Token>>();
    }

    [Fact]
    public void Count_ShouldReturnCorrectTokenCount()
    {
        // Arrange
        var originalString = "one two three";
        var tokens = new List<Token>
        {
            new Token("one", TokenType.Word, 0, 3),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("two", TokenType.Word, 4, 3),
            new Token(" ", TokenType.Whitespace, 7, 1),
            new Token("three", TokenType.Word, 8, 5)
        };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var count = tokenizedString.Count;

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public void Indexer_WithValidIndex_ShouldReturnToken()
    {
        // Arrange
        var originalString = "test string";
        var tokens = new List<Token>
        {
            new Token("test", TokenType.Word, 0, 4),
            new Token(" ", TokenType.Whitespace, 4, 1),
            new Token("string", TokenType.Word, 5, 6)
        };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var firstToken = tokenizedString[0];
        var secondToken = tokenizedString[1];
        var thirdToken = tokenizedString[2];

        // Assert
        firstToken.Text.Should().Be("test");
        secondToken.Text.Should().Be(" ");
        thirdToken.Text.Should().Be("string");
    }

    [Fact]
    public void Indexer_WithInvalidIndex_ShouldThrowIndexOutOfRangeException()
    {
        // Arrange
        var originalString = "test";
        var tokens = new List<Token> { new Token("test", TokenType.Word, 0, 4) };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        Action act = () => _ = tokenizedString[5];

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetTokenStrings_ShouldReturnAllTokenTexts()
    {
        // Arrange
        var originalString = "one two three";
        var tokens = new List<Token>
        {
            new Token("one", TokenType.Word, 0, 3),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("two", TokenType.Word, 4, 3),
            new Token(" ", TokenType.Whitespace, 7, 1),
            new Token("three", TokenType.Word, 8, 5)
        };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var strings = tokenizedString.GetTokenStrings();

        // Assert
        strings.Should().HaveCount(5);
        strings.Should().ContainInOrder("one", " ", "two", " ", "three");
    }

    [Fact]
    public void GetTokensWithoutWhitespace_ShouldExcludeWhitespaceTokens()
    {
        // Arrange
        var originalString = "one two three";
        var tokens = new List<Token>
        {
            new Token("one", TokenType.Word, 0, 3),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("two", TokenType.Word, 4, 3),
            new Token(" ", TokenType.Whitespace, 7, 1),
            new Token("three", TokenType.Word, 8, 5)
        };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var nonWhitespaceTokens = tokenizedString.GetTokensWithoutWhitespace();

        // Assert
        nonWhitespaceTokens.Should().HaveCount(3);
        nonWhitespaceTokens.Select(t => t.Text).Should().ContainInOrder("one", "two", "three");
    }

    [Fact]
    public void GetTokensByType_ShouldReturnOnlyMatchingTokens()
    {
        // Arrange
        var originalString = "123 Main St, Apt 4";
        var tokens = new List<Token>
        {
            new Token("123", TokenType.Numeric, 0, 3),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("Main", TokenType.Word, 4, 4),
            new Token(" ", TokenType.Whitespace, 8, 1),
            new Token("St", TokenType.Abbreviation, 9, 2),
            new Token(",", TokenType.Comma, 11, 1),
            new Token(" ", TokenType.Whitespace, 12, 1),
            new Token("Apt", TokenType.Abbreviation, 13, 3),
            new Token(" ", TokenType.Whitespace, 16, 1),
            new Token("4", TokenType.Numeric, 17, 1)
        };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var numericTokens = tokenizedString.GetTokensByType(TokenType.Numeric);
        var abbreviationTokens = tokenizedString.GetTokensByType(TokenType.Abbreviation);
        var wordTokens = tokenizedString.GetTokensByType(TokenType.Word);

        // Assert
        numericTokens.Should().HaveCount(2);
        numericTokens.Select(t => t.Text).Should().ContainInOrder("123", "4");

        abbreviationTokens.Should().HaveCount(2);
        abbreviationTokens.Select(t => t.Text).Should().ContainInOrder("St", "Apt");

        wordTokens.Should().HaveCount(1);
        wordTokens.Select(t => t.Text).Should().Contain("Main");
    }

    [Fact]
    public void IsEmpty_WithNoTokens_ShouldReturnTrue()
    {
        // Arrange
        var originalString = "";
        var tokens = new List<Token>();
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var isEmpty = tokenizedString.IsEmpty;

        // Assert
        isEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WithTokens_ShouldReturnFalse()
    {
        // Arrange
        var originalString = "test";
        var tokens = new List<Token> { new Token("test", TokenType.Word, 0, 4) };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var isEmpty = tokenizedString.IsEmpty;

        // Assert
        isEmpty.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldReturnOriginalString()
    {
        // Arrange
        var originalString = "123 Main Street";
        var tokens = new List<Token> { new Token("123", TokenType.Numeric, 0, 3) };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var result = tokenizedString.ToString();

        // Assert
        result.Should().Be(originalString);
    }

    [Fact]
    public void GetEnumerator_ShouldIterateOverTokens()
    {
        // Arrange
        var originalString = "a b c";
        var tokens = new List<Token>
        {
            new Token("a", TokenType.Word, 0, 1),
            new Token(" ", TokenType.Whitespace, 1, 1),
            new Token("b", TokenType.Word, 2, 1),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("c", TokenType.Word, 4, 1)
        };
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Act
        var tokenTexts = new List<string>();
        foreach (var token in tokenizedString)
        {
            tokenTexts.Add(token.Text);
        }

        // Assert
        tokenTexts.Should().ContainInOrder("a", " ", "b", " ", "c");
    }

    [Fact]
    public void Constructor_WithEmptyTokenList_ShouldInitializeCorrectly()
    {
        // Arrange
        var originalString = "";
        var tokens = new List<Token>();

        // Act
        var tokenizedString = new TokenizedString(originalString, tokens);

        // Assert
        tokenizedString.OriginalString.Should().Be(originalString);
        tokenizedString.Count.Should().Be(0);
        tokenizedString.IsEmpty.Should().BeTrue();
    }
}
