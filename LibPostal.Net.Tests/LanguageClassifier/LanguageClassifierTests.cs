using FluentAssertions;
using LibPostal.Net.ML;
using Classifier = LibPostal.Net.LanguageClassifier.LanguageClassifier;

namespace LibPostal.Net.Tests.LanguageClassifier;

/// <summary>
/// Tests for LanguageClassifier.
/// </summary>
public class LanguageClassifierTests
{
    [Fact]
    public void ClassifyLanguage_WithTestModel_ShouldReturnResults()
    {
        // Arrange
        var classifier = CreateTestClassifier();

        // Act
        var results = classifier.ClassifyLanguage("hello world", topK: 2);

        // Assert
        results.Should().NotBeEmpty();
        results.Length.Should().BeLessThanOrEqualTo(2);
        results.Should().AllSatisfy(r => r.Confidence.Should().BeInRange(0.0, 1.0));
    }

    [Fact]
    public void GetMostLikelyLanguage_ShouldReturnTopResult()
    {
        // Arrange
        var classifier = CreateTestClassifier();

        // Act
        var result = classifier.GetMostLikelyLanguage("test");

        // Assert
        result.Should().NotBeNull();
        result!.LanguageCode.Should().NotBeNullOrEmpty();
        result.Confidence.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void ClassifyLanguage_WithEmptyText_ShouldReturnEmpty()
    {
        // Arrange
        var classifier = CreateTestClassifier();

        // Act
        var results = classifier.ClassifyLanguage("", topK: 3);

        // Assert
        results.Should().BeEmpty();
    }

    private static Classifier CreateTestClassifier()
    {
        // Create a simple test model (2 languages, 10 features)
        var weights = new SparseMatrix<double>(rows: 2, cols: 10);
        weights.SetValue(0, 0, 1.0); // Language 0 likes feature 0
        weights.SetValue(1, 1, 1.0); // Language 1 likes feature 1

        var labels = new[] { "en", "fr" };

        var featureMap = new Dictionary<string, int>
        {
            ["a"] = 0,
            ["b"] = 1,
            ["c"] = 2,
            ["ab"] = 3,
            ["bc"] = 4
        };

        return new Classifier(weights, labels, featureMap);
    }
}
