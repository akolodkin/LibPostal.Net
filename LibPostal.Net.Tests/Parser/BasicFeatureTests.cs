using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for basic feature extraction (word, numeric, position).
/// </summary>
public class BasicFeatureTests
{
    private readonly Tokenizer _tokenizer = new();

    [Fact]
    public void ExtractFeatures_WordToken_ShouldIncludeWordFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("main");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("word=main");
    }

    [Fact]
    public void ExtractFeatures_WordToken_ShouldIncludeWordLengthFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("street");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("word_length=6");
    }

    [Fact]
    public void ExtractFeatures_NumericToken_ShouldIncludeIsNumericFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("is_numeric");
    }

    [Fact]
    public void ExtractFeatures_FirstToken_ShouldIncludePositionFirst()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 main");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("position=first");
    }

    [Fact]
    public void ExtractFeatures_LastToken_ShouldIncludePositionLast()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 main");

        // Act - Index 2 is "main" (0="123", 1=" ", 2="main")
        var features = extractor.ExtractFeatures(tokenized, 2);

        // Assert
        features.Should().Contain("position=last");
    }

    [Fact]
    public void ExtractFeatures_MiddleToken_ShouldNotIncludePositionFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 main street");

        // Act - Index 2 is "main" (middle token)
        var features = extractor.ExtractFeatures(tokenized, 2);

        // Assert
        features.Should().NotContain("position=first");
        features.Should().NotContain("position=last");
    }

    [Fact]
    public void ExtractFeatures_WithPrevWord_ShouldIncludePrevWordFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 main");

        // Act - Index 2 is "main" which has prev word "123"
        var features = extractor.ExtractFeatures(tokenized, 2);

        // Assert
        features.Should().Contain("prev_word=123");
    }

    [Fact]
    public void ExtractFeatures_WithNextWord_ShouldIncludeNextWordFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("main street");

        // Act - Index 0 is "main" which has next word "street"
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("next_word=street");
    }

    [Fact]
    public void ExtractFeatures_WithPrevAndNext_ShouldIncludeBigramFeatures()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 main street");

        // Act - Index 2 is "main" (0="123", 1=" ", 2="main", 3=" ", 4="street")
        var features = extractor.ExtractFeatures(tokenized, 2);

        // Assert
        features.Should().Contain("prev_word+word=123 main");
        features.Should().Contain("word+next_word=main street");
    }

    [Fact]
    public void ExtractFeatures_CapitalizedWord_ShouldIncludeCapitalizedFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("Brooklyn");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("is_capitalized");
    }

    [Fact]
    public void ExtractFeatures_AllUppercase_ShouldIncludeAllCapsFeature()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("NYC");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("is_all_caps");
    }

    [Fact]
    public void ExtractFeatures_WithPunctuation_ShouldDetectPunctuation()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("St.");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().Contain("word=st");
        features.Should().Contain("has_period");
    }

    [Fact]
    public void ExtractFeatures_MultipleTokens_EachShouldHaveUniqueFeatures()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenized = _tokenizer.Tokenize("123 main street");

        // Act - Indices: 0="123", 1=" ", 2="main", 3=" ", 4="street"
        var features0 = extractor.ExtractFeatures(tokenized, 0);
        var features2 = extractor.ExtractFeatures(tokenized, 2);
        var features4 = extractor.ExtractFeatures(tokenized, 4);

        // Assert
        features0.Should().Contain("is_numeric");
        features2.Should().Contain("word=main");
        features4.Should().Contain("word=street");
    }
}
