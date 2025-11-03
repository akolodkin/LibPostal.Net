using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Core;
using LibPostal.Net.Tokenization;
using Token = LibPostal.Net.Tokenization.Token;
using TokenType = LibPostal.Net.Tokenization.TokenType;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for PhraseMatcher - trie-based phrase matching for tokens.
/// </summary>
public class PhraseMatcherTests
{
    [Fact]
    public void Constructor_WithNullTrie_ShouldThrow()
    {
        // Act
        Action act = () => new PhraseMatcher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidTrie_ShouldInitialize()
    {
        // Arrange
        var trie = new Trie<uint>();

        // Act
        var matcher = new PhraseMatcher(trie);

        // Assert
        matcher.Should().NotBeNull();
    }

    [Fact]
    public void SearchTokens_WithSingleTokenPhrase_ShouldFindMatch()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);
        var matcher = new PhraseMatcher(trie);

        var tokens = CreateTokens("main", "street");

        // Act
        var results = matcher.SearchTokens(tokens, 1).ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseText.Should().Be("street");
        results[0].StartIndex.Should().Be(1);
        results[0].EndIndex.Should().Be(1);
        results[0].PhraseId.Should().Be(1);
    }

    [Fact]
    public void SearchTokens_WithMultiTokenPhrase_ShouldFindMatch()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("main street", 10);
        var matcher = new PhraseMatcher(trie);

        var tokens = CreateTokens("123", "main", "street", "brooklyn");

        // Act
        var results = matcher.SearchTokens(tokens, 1).ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseText.Should().Be("main street");
        results[0].StartIndex.Should().Be(1);
        results[0].EndIndex.Should().Be(2); // inclusive
        results[0].Length.Should().Be(2);
    }

    [Fact]
    public void SearchTokens_WithOverlappingPhrases_ShouldReturnBoth()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("main", 1);
        trie.Add("main street", 2);
        var matcher = new PhraseMatcher(trie);

        var tokens = CreateTokens("main", "street");

        // Act
        var results = matcher.SearchTokens(tokens, 0).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.PhraseText == "main" && r.Length == 1);
        results.Should().Contain(r => r.PhraseText == "main street" && r.Length == 2);
    }

    [Fact]
    public void SearchTokens_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("avenue", 1);
        var matcher = new PhraseMatcher(trie);

        var tokens = CreateTokens("main", "street");

        // Act
        var results = matcher.SearchTokens(tokens, 0);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void SearchTokens_WithEmptyTokenArray_ShouldReturnEmpty()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("test", 1);
        var matcher = new PhraseMatcher(trie);

        var tokens = Array.Empty<Token>();

        // Act
        var results = matcher.SearchTokens(tokens, 0);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void SearchTokens_WithWhitespaceTokens_ShouldSkipWhitespace()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("oak street", 1);
        var matcher = new PhraseMatcher(trie);

        // Create tokens with whitespace
        var tokenList = new List<Token>
        {
            new Token("oak", TokenType.Word, 0, 3),
            new Token(" ", TokenType.Whitespace, 3, 1),
            new Token("street", TokenType.Word, 4, 6)
        };

        // Act
        var results = matcher.SearchTokens(tokenList.ToArray(), 0).ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseText.Should().Be("oak street");
        results[0].Length.Should().Be(2); // excludes whitespace from count
    }

    [Fact]
    public void SearchTokens_WithNormalization_ShouldMatchNormalizedForm()
    {
        // Arrange - trie has normalized form
        var trie = new Trie<uint>();
        trie.Add("cafe", 1); // normalized (no accent)
        var matcher = new PhraseMatcher(trie);

        var tokens = CreateTokens("café"); // with accent

        // Act - searching with normalized text
        var results = matcher.SearchTokens(tokens, 0, normalized: true).ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseText.Should().Be("cafe");
    }

    [Fact]
    public void SearchPrefixes_WithValidPrefix_ShouldFindMatch()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("|hinter", 1); // | indicates prefix
        var matcher = new PhraseMatcher(trie);

        // Act
        var results = matcher.SearchPrefixes("hinterhaus").ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseText.Should().Be("hinter");
        results[0].PhraseId.Should().Be(1);
    }

    [Fact]
    public void SearchPrefixes_WithNoMatch_ShouldReturnEmpty()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("|vor", 1);
        var matcher = new PhraseMatcher(trie);

        // Act
        var results = matcher.SearchPrefixes("hauptstrasse");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void SearchPrefixes_ShouldDistinguishFromFullWord()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("|str", 10); // prefix
        trie.Add("str", 20);  // full word
        var matcher = new PhraseMatcher(trie);

        // Act - search for prefix only
        var results = matcher.SearchPrefixes("street").ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseId.Should().Be(10); // should find prefix, not full word
        results[0].PhraseText.Should().Be("str");
    }

    [Fact]
    public void SearchSuffixes_WithValidSuffix_ShouldFindMatch()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("straße|", 1); // | indicates suffix
        var matcher = new PhraseMatcher(trie);

        // Act
        var results = matcher.SearchSuffixes("hauptstraße").ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseText.Should().Be("straße");
        results[0].PhraseId.Should().Be(1);
    }

    [Fact]
    public void SearchSuffixes_WithNoMatch_ShouldReturnEmpty()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("gasse|", 1);
        var matcher = new PhraseMatcher(trie);

        // Act
        var results = matcher.SearchSuffixes("hauptstrasse");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void SearchSuffixes_ShouldDistinguishFromFullWord()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("weg|", 10); // suffix
        trie.Add("weg", 20);  // full word
        var matcher = new PhraseMatcher(trie);

        // Act
        var results = matcher.SearchSuffixes("fußweg").ToList();

        // Assert
        results.Should().ContainSingle();
        results[0].PhraseId.Should().Be(10);
        results[0].PhraseText.Should().Be("weg");
    }

    // Helper methods

    private static Token[] CreateTokens(params string[] words)
    {
        var tokens = new List<Token>();
        int offset = 0;

        for (int i = 0; i < words.Length; i++)
        {
            tokens.Add(new Token(words[i], TokenType.Word, offset, words[i].Length));
            offset += words[i].Length + 1; // +1 for space
        }

        return tokens.ToArray();
    }
}
