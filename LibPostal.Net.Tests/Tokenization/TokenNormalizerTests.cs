using FluentAssertions;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Tokenization;

/// <summary>
/// Tests for TokenNormalizer class.
/// Based on libpostal's NORMALIZE_TOKEN_* flags.
/// </summary>
public class TokenNormalizerTests
{
    [Fact]
    public void NormalizeToken_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        Action act = () => normalizer.NormalizeToken(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NormalizeToken_WithNoOptions_ShouldReturnUnchanged()
    {
        // Arrange
        var normalizer = new TokenNormalizer();
        var input = "test-word";

        // Act
        var result = normalizer.NormalizeToken(input, TokenNormalizationOptions.None);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void NormalizeToken_DeleteHyphens_ShouldRemoveHyphens()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken("twenty-one", TokenNormalizationOptions.DeleteHyphens);

        // Assert
        result.Should().Be("twentyone");
    }

    [Fact]
    public void NormalizeToken_DeleteFinalPeriod_ShouldRemoveTrailingPeriod()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken("St.", TokenNormalizationOptions.DeleteFinalPeriod);

        // Assert
        result.Should().Be("St");
    }

    [Fact]
    public void NormalizeToken_DeleteFinalPeriod_WithNoPeriod_ShouldReturnUnchanged()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken("Street", TokenNormalizationOptions.DeleteFinalPeriod);

        // Assert
        result.Should().Be("Street");
    }

    [Fact]
    public void NormalizeToken_DeleteAcronymPeriods_ShouldRemovePeriodsFromAcronym()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken("U.S.A.", TokenNormalizationOptions.DeleteAcronymPeriods);

        // Assert
        result.Should().Be("USA");
    }

    [Fact]
    public void NormalizeToken_DeletePossessive_ShouldRemovePossessiveS()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result1 = normalizer.NormalizeToken("John's", TokenNormalizationOptions.DeletePossessive);
        var result2 = normalizer.NormalizeToken("James'", TokenNormalizationOptions.DeletePossessive);

        // Assert
        result1.Should().Be("John");
        result2.Should().Be("James");
    }

    [Fact]
    public void NormalizeToken_DeleteApostrophe_ShouldRemoveApostrophes()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken("O'Malley", TokenNormalizationOptions.DeleteApostrophe);

        // Assert
        result.Should().Be("OMalley");
    }

    [Fact]
    public void NormalizeToken_SplitAlphaNumeric_ShouldSeparateLettersAndNumbers()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken("4B", TokenNormalizationOptions.SplitAlphaNumeric);

        // Assert
        result.Should().Contain("4");
        result.Should().Contain("B");
    }

    [Fact]
    public void NormalizeToken_ReplaceDigits_ShouldReplaceWithD()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken("123", TokenNormalizationOptions.ReplaceDigits);

        // Assert
        result.Should().Be("DDD");
    }

    [Fact]
    public void NormalizeToken_WithMultipleOptions_ShouldApplyAll()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken(
            "St.",
            TokenNormalizationOptions.DeleteFinalPeriod | TokenNormalizationOptions.DeleteHyphens);

        // Assert
        result.Should().Be("St");
    }

    [Fact]
    public void NormalizeToken_ComplexCase_ShouldHandleAll()
    {
        // Arrange
        var normalizer = new TokenNormalizer();

        // Act
        var result = normalizer.NormalizeToken(
            "O'Brien's",
            TokenNormalizationOptions.DeletePossessive | TokenNormalizationOptions.DeleteApostrophe);

        // Assert
        result.Should().Be("OBrien");
    }

    [Fact]
    public void NormalizeTokens_WithTokenizedString_ShouldNormalizeAllTokens()
    {
        // Arrange
        var normalizer = new TokenNormalizer();
        var tokens = new List<Token>
        {
            new Token("St.", TokenType.Abbreviation, 0, 3),
            new Token("John's", TokenType.Word, 4, 6)
        };
        var tokenizedString = new TokenizedString("St. John's", tokens);

        // Act
        var result = normalizer.NormalizeTokens(
            tokenizedString,
            TokenNormalizationOptions.DeleteFinalPeriod | TokenNormalizationOptions.DeletePossessive);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be("St");
        result[1].Should().Be("John");
    }

    [Fact]
    public void NormalizeTokens_SkippingWhitespace_ShouldOnlyNormalizeNonWhitespace()
    {
        // Arrange
        var normalizer = new TokenNormalizer();
        var tokens = new List<Token>
        {
            new Token("St.", TokenType.Abbreviation, 0, 3),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("Ave.", TokenType.Abbreviation, 4, 4)
        };
        var tokenizedString = new TokenizedString("St. Ave.", tokens);

        // Act
        var result = normalizer.NormalizeTokens(tokenizedString, TokenNormalizationOptions.DeleteFinalPeriod);

        // Assert
        result.Should().HaveCount(2); // Whitespace excluded
        result[0].Should().Be("St");
        result[1].Should().Be("Ave");
    }
}
