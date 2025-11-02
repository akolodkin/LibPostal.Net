using FluentAssertions;
using LibPostal.Net.Expansion;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for Phrase and PhraseSearcher classes.
/// Based on libpostal's phrase search logic.
/// </summary>
public class PhraseTests
{
    [Fact]
    public void Phrase_ShouldInitializeWithProperties()
    {
        // Arrange & Act
        var phrase = new Phrase
        {
            StartIndex = 0,
            Length = 2,
            Value = "main street"
        };

        // Assert
        phrase.StartIndex.Should().Be(0);
        phrase.Length.Should().Be(2);
        phrase.Value.Should().Be("main street");
    }

    [Fact]
    public void Phrase_ShouldCalculateEndIndex()
    {
        // Arrange
        var phrase = new Phrase
        {
            StartIndex = 5,
            Length = 3,
            Value = "test phrase"
        };

        // Act
        var end = phrase.EndIndex;

        // Assert
        end.Should().Be(8); // 5 + 3
    }

    [Fact]
    public void PhraseSearcher_WithNullDictionary_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new PhraseSearcher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PhraseSearcher_SearchTokens_WithNullTokenizedString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dictionary = new Dictionary<string, AddressExpansionValue>();
        var searcher = new PhraseSearcher(dictionary);

        // Act
        Action act = () => searcher.SearchTokens(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PhraseSearcher_SearchTokens_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var dictionary = new Dictionary<string, AddressExpansionValue>();
        var searcher = new PhraseSearcher(dictionary);

        var tokens = new List<Token>
        {
            new Token("test", TokenType.Word, 0, 4)
        };
        var tokenizedString = new TokenizedString("test", tokens);

        // Act
        var result = searcher.SearchTokens(tokenizedString);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void PhraseSearcher_SearchTokens_WithSingleTokenMatch_ShouldFindPhrase()
    {
        // Arrange
        var expansions = new AddressExpansionValue(new[]
        {
            new AddressExpansion
            {
                Canonical = "street",
                Language = "en",
                Components = AddressComponent.Street,
                DictionaryType = DictionaryType.StreetType,
                IsSeparable = true
            }
        });

        var dictionary = new Dictionary<string, AddressExpansionValue>
        {
            ["st"] = expansions
        };
        var searcher = new PhraseSearcher(dictionary);

        var tokens = new List<Token>
        {
            new Token("st", TokenType.Abbreviation, 0, 2)
        };
        var tokenizedString = new TokenizedString("st", tokens);

        // Act
        var result = searcher.SearchTokens(tokenizedString);

        // Assert
        result.Should().HaveCount(1);
        result[0].StartIndex.Should().Be(0);
        result[0].Length.Should().Be(1); // 1 token
        result[0].Value.Should().Be("st");
    }

    [Fact]
    public void PhraseSearcher_SearchTokens_WithMultiTokenMatch_ShouldFindPhrase()
    {
        // Arrange
        var expansions = new AddressExpansionValue(new[]
        {
            new AddressExpansion
            {
                Canonical = "boulevard",
                Language = "en",
                Components = AddressComponent.Street,
                DictionaryType = DictionaryType.StreetType,
                IsSeparable = true
            }
        });

        var dictionary = new Dictionary<string, AddressExpansionValue>
        {
            ["boul vard"] = expansions
        };
        var searcher = new PhraseSearcher(dictionary);

        var tokens = new List<Token>
        {
            new Token("boul", TokenType.Word, 0, 4),
            new Token(" ", TokenType.Whitespace, 4, 1),
            new Token("vard", TokenType.Word, 5, 4)
        };
        var tokenizedString = new TokenizedString("boul vard", tokens);

        // Act
        var result = searcher.SearchTokens(tokenizedString);

        // Assert
        result.Should().HaveCount(1);
        result[0].StartIndex.Should().Be(0);
        result[0].Length.Should().Be(3); // 3 tokens (including whitespace)
        result[0].Value.Should().Be("boul vard");
    }

    [Fact]
    public void PhraseSearcher_SearchTokens_WithOverlappingMatches_ShouldFindLongest()
    {
        // Arrange - both "main" and "main st" are in dictionary
        var mainExpansion = new AddressExpansionValue(new[]
        {
            new AddressExpansion
            {
                Canonical = "principal",
                Language = "en",
                Components = AddressComponent.Street,
                DictionaryType = DictionaryType.StreetName,
                IsSeparable = true
            }
        });

        var mainStExpansion = new AddressExpansionValue(new[]
        {
            new AddressExpansion
            {
                Canonical = "main street",
                Language = "en",
                Components = AddressComponent.Street,
                DictionaryType = DictionaryType.StreetName,
                IsSeparable = true
            }
        });

        var dictionary = new Dictionary<string, AddressExpansionValue>
        {
            ["main"] = mainExpansion,
            ["main st"] = mainStExpansion
        };
        var searcher = new PhraseSearcher(dictionary);

        var tokens = new List<Token>
        {
            new Token("main", TokenType.Word, 0, 4),
            new Token(" ", TokenType.Whitespace, 4, 1),
            new Token("st", TokenType.Abbreviation, 5, 2)
        };
        var tokenizedString = new TokenizedString("main st", tokens);

        // Act
        var result = searcher.SearchTokens(tokenizedString);

        // Assert - should find both, but longest match is preferred
        result.Should().NotBeEmpty();
        result.Should().Contain(p => p.Length == 3 && p.Value == "main st"); // Longest match
    }

    [Fact]
    public void PhraseSearcher_SearchTokens_ShouldNormalizeKeysForLookup()
    {
        // Arrange - dictionary has lowercase, input has uppercase
        var expansions = new AddressExpansionValue(new[]
        {
            new AddressExpansion
            {
                Canonical = "street",
                Language = "en",
                Components = AddressComponent.Street,
                DictionaryType = DictionaryType.StreetType,
                IsSeparable = true
            }
        });

        var dictionary = new Dictionary<string, AddressExpansionValue>
        {
            ["st"] = expansions
        };
        var searcher = new PhraseSearcher(dictionary);

        var tokens = new List<Token>
        {
            new Token("ST", TokenType.Abbreviation, 0, 2)
        };
        var tokenizedString = new TokenizedString("ST", tokens);

        // Act
        var result = searcher.SearchTokens(tokenizedString);

        // Assert - should match despite case difference
        result.Should().HaveCount(1);
    }

    [Fact]
    public void PhraseSearcher_SearchTokens_ShouldSkipWhitespaceInKey()
    {
        // Arrange
        var expansions = new AddressExpansionValue(new[]
        {
            new AddressExpansion
            {
                Canonical = "post office",
                Language = "en",
                Components = AddressComponent.PoBox,
                DictionaryType = DictionaryType.PostOffice,
                IsSeparable = true
            }
        });

        var dictionary = new Dictionary<string, AddressExpansionValue>
        {
            ["po box"] = expansions
        };
        var searcher = new PhraseSearcher(dictionary);

        var tokens = new List<Token>
        {
            new Token("po", TokenType.Word, 0, 2),
            new Token(" ", TokenType.Whitespace, 2, 1),
            new Token("box", TokenType.Word, 3, 3)
        };
        var tokenizedString = new TokenizedString("po box", tokens);

        // Act
        var result = searcher.SearchTokens(tokenizedString);

        // Assert
        result.Should().HaveCount(1);
        result[0].Value.Should().Be("po box");
    }
}
