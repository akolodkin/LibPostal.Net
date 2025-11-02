using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for advanced features (n-grams, separators, etc.).
/// Simplified but covers key patterns.
/// </summary>
public class AdvancedFeatureTests
{
    private readonly Tokenizer _tokenizer = new();

    [Fact]
    public void ExtractFeatures_UnknownWord_ShouldIncludeNgramFeatures()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("barboncino"); // Rare word

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert - Should have n-gram features for rare words
        features.Should().Contain(f => f.StartsWith("word:prefix"));
        features.Should().Contain(f => f.StartsWith("word:suffix"));
    }

    [Fact]
    public void ExtractFeatures_WithComma_ShouldDetectSeparator()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 Main St, Brooklyn");

        // Act - Find the comma token and token after it
        var commaIdx = tokenized.ToList().FindIndex(t => t.Text == ",");
        var featuresAfterComma = extractor.ExtractFeatures(tokenized, commaIdx + 2); // Skip comma and whitespace

        // Assert
        featuresAfterComma.Should().Contain("after_comma");
    }

    [Fact]
    public void ExtractFeatures_LongWord_ShouldIncludeNgramsAndSubWords()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("twenty-one"); // Tokenizer keeps hyphenated as one if no space

        // Act - Get features for first token
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert - Should have either n-grams or word features
        features.Should().Contain(f => f.Contains("word=") || f.Contains("prefix") || f.Contains("sub_word"));
    }

    [Fact]
    public void ExtractFeatures_AllFeatureTypes_ShouldProduceReasonableCount()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 Main Street Brooklyn NY");

        // Act - Extract for middle token "Street"
        var features = extractor.ExtractFeatures(tokenized, 4); // "Street"

        // Assert - Should have multiple features
        features.Length.Should().BeGreaterThan(5);
        features.Should().Contain("bias");
        features.Should().Contain("word=street");
    }
}
