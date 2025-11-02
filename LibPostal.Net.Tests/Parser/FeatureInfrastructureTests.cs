using FluentAssertions;
using LibPostal.Net.Parser;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for feature extraction infrastructure.
/// Based on libpostal's address_parser_features function.
/// </summary>
public class FeatureInfrastructureTests
{
    [Fact]
    public void Feature_ShouldInitializeWithNameAndValue()
    {
        // Arrange & Act
        var feature = new Feature("word=main", 1.0);

        // Assert
        feature.Name.Should().Be("word=main");
        feature.Value.Should().Be(1.0);
    }

    [Fact]
    public void Feature_DefaultValue_ShouldBeOne()
    {
        // Arrange & Act
        var feature = new Feature("bias");

        // Assert
        feature.Name.Should().Be("bias");
        feature.Value.Should().Be(1.0); // Default weight
    }

    [Fact]
    public void FeatureVector_Constructor_ShouldInitialize()
    {
        // Arrange & Act
        var vector = new FeatureVector();

        // Assert
        vector.Should().NotBeNull();
        vector.Count.Should().Be(0);
    }

    [Fact]
    public void FeatureVector_Add_ShouldStoreFeature()
    {
        // Arrange
        var vector = new FeatureVector();

        // Act
        vector.Add("word=main");
        vector.Add("is_numeric", value: 2.0);

        // Assert
        vector.Count.Should().Be(2);
        vector.Contains("word=main").Should().BeTrue();
        vector.Contains("is_numeric").Should().BeTrue();
    }

    [Fact]
    public void FeatureVector_ToArray_ShouldReturnAllFeatures()
    {
        // Arrange
        var vector = new FeatureVector();
        vector.Add("f1");
        vector.Add("f2");
        vector.Add("f3");

        // Act
        var features = vector.ToArray();

        // Assert
        features.Should().HaveCount(3);
        features.Should().Contain("f1");
        features.Should().Contain("f2");
        features.Should().Contain("f3");
    }

    [Fact]
    public void FeatureVector_Clear_ShouldRemoveAllFeatures()
    {
        // Arrange
        var vector = new FeatureVector();
        vector.Add("f1");
        vector.Add("f2");

        // Act
        vector.Clear();

        // Assert
        vector.Count.Should().Be(0);
    }

    [Fact]
    public void AddressFeatureExtractor_Constructor_ShouldInitialize()
    {
        // Act
        var extractor = new AddressFeatureExtractor();

        // Assert
        extractor.Should().NotBeNull();
    }

    [Fact]
    public void ExtractFeatures_WithNullTokenized_ShouldThrowArgumentNullException()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();

        // Act
        Action act = () => extractor.ExtractFeatures(null!, 0);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExtractFeatures_WithInvalidIndex_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenizer = new LibPostal.Net.Tokenization.Tokenizer();
        var tokenized = tokenizer.Tokenize("test");

        // Act
        Action act = () => extractor.ExtractFeatures(tokenized, 999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ExtractFeatures_WithValidToken_ShouldReturnNonEmpty()
    {
        // Arrange
        var extractor = new AddressFeatureExtractor();
        var tokenizer = new LibPostal.Net.Tokenization.Tokenizer();
        var tokenized = tokenizer.Tokenize("main");

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0);

        // Assert
        features.Should().NotBeEmpty();
        features.Should().Contain("bias"); // Should always have bias feature
    }
}
