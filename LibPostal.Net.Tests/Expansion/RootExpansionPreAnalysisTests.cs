using FluentAssertions;
using LibPostal.Net.Expansion;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for RootExpansionPreAnalyzer.
/// Based on libpostal's pre-analysis pass (expand.c lines 820-890).
/// </summary>
public class RootExpansionPreAnalysisTests
{
    [Fact]
    public void Analyze_WithNullTokenizedString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var phrases = new List<Phrase>();

        // Act
        Action act = () => analyzer.Analyze(null!, phrases, AddressComponent.Street);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_WithNullPhrases_ShouldThrowArgumentNullException()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var tokenized = CreateTokenizedString("test");

        // Act
        Action act = () => analyzer.Analyze(tokenized, null!, AddressComponent.Street);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_WithNoPhrases_ShouldSetHaveNonPhraseTokensTrue()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var tokenized = CreateTokenizedString("test word");
        var phrases = new List<Phrase>();

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert
        result.HaveNonPhraseTokens.Should().BeTrue();
        result.HaveNonPhraseWordTokens.Should().BeTrue();
    }

    [Fact]
    public void Analyze_WithAllTokensCovered_ShouldSetHaveNonPhraseTokensFalse()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var tokenized = CreateTokenizedString("st");
        var phrases = new List<Phrase>
        {
            CreatePhrase(0, 1, "st", hasCanonical: true)
        };

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert
        result.HaveNonPhraseTokens.Should().BeFalse();
    }

    [Fact]
    public void Analyze_WithCanonicalPhrase_ShouldSetHaveCanonicalPhrasesTrue()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var tokenized = CreateTokenizedString("st");
        var phrases = new List<Phrase>
        {
            CreatePhrase(0, 1, "st", hasCanonical: true)
        };

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert
        result.HaveCanonicalPhrases.Should().BeTrue();
    }

    [Fact]
    public void Analyze_WithAmbiguousPhrase_ShouldSetHaveAmbiguousTrue()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var tokenized = CreateTokenizedString("st");
        var phrases = new List<Phrase>
        {
            CreatePhrase(0, 1, "st", dictionaryType: DictionaryType.AmbiguousExpansion)
        };

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert
        result.HaveAmbiguous.Should().BeTrue();
    }

    [Fact]
    public void Analyze_WithPossibleRoot_ShouldSetHavePossibleRootTrue()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var tokenized = CreateTokenizedString("e");
        var phrases = new List<Phrase>
        {
            CreatePhrase(0, 1, "e", dictionaryType: DictionaryType.Directional) // Directionals are possible roots
        };

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert
        result.HavePossibleRoot.Should().BeTrue();
    }

    [Fact]
    public void Analyze_WithMixedScenario_ShouldSetAllFlagsCorrectly()
    {
        // Arrange - "Main St" where Main is not in dictionary, St is
        var analyzer = new RootExpansionPreAnalyzer();
        var tokenized = CreateTokenizedString("main st");
        var phrases = new List<Phrase>
        {
            CreatePhrase(2, 1, "st", hasCanonical: true, dictionaryType: DictionaryType.StreetType)
        };

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert
        result.HaveNonPhraseTokens.Should().BeTrue(); // "main" is not in a phrase
        result.HaveNonPhraseWordTokens.Should().BeTrue();
        result.HaveCanonicalPhrases.Should().BeTrue(); // "st" has canonical
    }

    [Fact]
    public void Analyze_WithOnlyWhitespace_ShouldNotCountAsNonPhraseWord()
    {
        // Arrange
        var analyzer = new RootExpansionPreAnalyzer();
        var tokens = new List<Token>
        {
            new Token("st", TokenType.Abbreviation, 0, 2),
            new Token(" ", TokenType.Whitespace, 2, 1)
        };
        var tokenized = new TokenizedString("st ", tokens);
        var phrases = new List<Phrase>
        {
            CreatePhrase(0, 1, "st", hasCanonical: true)
        };

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert
        result.HaveNonPhraseWordTokens.Should().BeFalse(); // Whitespace doesn't count
    }

    [Fact]
    public void Analyze_ShouldCorrectlyCalculateAllFlags()
    {
        // Arrange - complex scenario
        var analyzer = new RootExpansionPreAnalyzer();
        var tokens = new List<Token>
        {
            new Token("main", TokenType.Word, 0, 4),
            new Token(" ", TokenType.Whitespace, 4, 1),
            new Token("st", TokenType.Abbreviation, 5, 2)
        };
        var tokenized = new TokenizedString("main st", tokens);
        var phrases = new List<Phrase>
        {
            CreatePhrase(2, 1, "st", hasCanonical: true, dictionaryType: DictionaryType.StreetType)
        };

        // Act
        var result = analyzer.Analyze(tokenized, phrases, AddressComponent.Street);

        // Assert - verify all computed flags
        result.Should().NotBeNull();
        result.HaveNonPhraseTokens.Should().BeTrue();
        result.HaveNonPhraseWordTokens.Should().BeTrue();
        result.HaveCanonicalPhrases.Should().BeTrue();
        result.HavePossibleRoot.Should().BeFalse(); // StreetType is not a possible root
        result.HaveAmbiguous.Should().BeFalse();
    }

    #region Helper Methods

    private static TokenizedString CreateTokenizedString(string input)
    {
        var tokenizer = new Tokenizer();
        return tokenizer.Tokenize(input);
    }

    private static Phrase CreatePhrase(
        int startIndex,
        int length,
        string value,
        bool hasCanonical = false,
        DictionaryType dictionaryType = DictionaryType.StreetType)
    {
        var expansion = new AddressExpansion
        {
            Canonical = hasCanonical ? "canonical_form" : null,
            Language = "en",
            Components = AddressComponent.Street,
            DictionaryType = dictionaryType,
            IsSeparable = true
        };

        return new Phrase
        {
            StartIndex = startIndex,
            Length = length,
            Value = value,
            Expansions = new AddressExpansionValue(new[] { expansion })
        };
    }

    #endregion
}
