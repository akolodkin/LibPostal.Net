using FluentAssertions;
using LibPostal.Net.LanguageClassifier;

namespace LibPostal.Net.Tests.LanguageClassifier;

/// <summary>
/// Tests for LanguageFeatureExtractor.
/// Based on libpostal's language_features.c
/// </summary>
public class LanguageFeatureExtractorTests
{
    [Fact]
    public void ExtractFeatures_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        Action act = () => extractor.ExtractFeatures(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractFeatures_WithEmptyString_ShouldReturnEmptyFeatures()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("");

        // Assert
        features.Should().BeEmpty();
    }

    [Fact]
    public void ExtractFeatures_WithSimpleText_ShouldExtractNgrams()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("hello");

        // Assert
        features.Should().NotBeEmpty();
        features.Should().ContainKey("h");  // Unigram
        features.Should().ContainKey("he"); // Bigram
    }

    [Fact]
    public void ExtractFeatures_ShouldExtractUnigramsAndBigrams()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("ab");

        // Assert
        features.Should().ContainKey("a");  // Unigram
        features.Should().ContainKey("b");  // Unigram
        features.Should().ContainKey("ab"); // Bigram
    }

    [Fact]
    public void ExtractFeatures_ShouldCountFrequencies()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("aaa");

        // Assert
        features["a"].Should().Be(3); // "a" appears 3 times
    }

    [Fact]
    public void ExtractFeatures_WithUnicode_ShouldHandleCorrectly()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("café");

        // Assert
        features.Should().ContainKey("é");
        features.Should().ContainKey("fé");
    }

    [Fact]
    public void ExtractFeatures_ShouldNormalizeToLowercase()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("HELLO");

        // Assert
        features.Should().ContainKey("h"); // Should be lowercase
        features.Should().NotContainKey("H");
    }

    [Fact]
    public void ExtractFeatures_WithMixedContent_ShouldExtractAllNgrams()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("test");

        // Assert - should have unigrams and bigrams
        features.Should().ContainKey("t");
        features.Should().ContainKey("e");
        features.Should().ContainKey("s");
        features.Should().ContainKey("te");
        features.Should().ContainKey("es");
        features.Should().ContainKey("st");
    }

    [Fact]
    public void ToSparseVector_WithFeatureMap_ShouldCreateVector()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();
        var featureMap = new Dictionary<string, int>
        {
            ["a"] = 0,
            ["ab"] = 1,
            ["b"] = 2
        };

        var features = new Dictionary<string, int>
        {
            ["a"] = 2,
            ["ab"] = 1,
            ["b"] = 1
        };

        // Act
        var vector = extractor.ToSparseVector(features, featureMap, vectorSize: 10);

        // Assert
        vector.Should().HaveCount(10);
        vector[0].Should().Be(2);  // "a" maps to index 0
        vector[1].Should().Be(1);  // "ab" maps to index 1
        vector[2].Should().Be(1);  // "b" maps to index 2
        vector[3].Should().Be(0);  // Rest are 0
    }

    [Fact]
    public void ToSparseVector_WithUnknownFeatures_ShouldIgnoreThem()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();
        var featureMap = new Dictionary<string, int>
        {
            ["a"] = 0
        };

        var features = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["unknown"] = 5  // Not in feature map
        };

        // Act
        var vector = extractor.ToSparseVector(features, featureMap, vectorSize: 5);

        // Assert
        vector[0].Should().Be(1);  // "a" is mapped
        vector.Sum().Should().Be(1); // "unknown" ignored
    }

    [Fact]
    public void ExtractFeatures_WithWhitespace_ShouldHandleCorrectly()
    {
        // Arrange
        var extractor = new LanguageFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures("a b");

        // Assert
        features.Should().ContainKey("a");
        features.Should().ContainKey("b");
        features.Should().ContainKey(" "); // Space is a feature
    }
}
