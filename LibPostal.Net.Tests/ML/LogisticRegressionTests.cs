using FluentAssertions;
using LibPostal.Net.ML;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for LogisticRegression classifier.
/// Based on libpostal's logistic_regression.c
/// </summary>
public class LogisticRegressionTests
{
    [Fact]
    public void Constructor_WithWeights_ShouldInitialize()
    {
        // Arrange
        var weights = new SparseMatrix<double>(rows: 3, cols: 5); // 3 classes, 5 features

        // Act
        var classifier = new LogisticRegression(weights, new[] { "en", "fr", "de" });

        // Assert
        classifier.NumClasses.Should().Be(3);
        classifier.NumFeatures.Should().Be(5);
    }

    [Fact]
    public void Predict_WithFeatureVector_ShouldReturnClassIndex()
    {
        // Arrange
        var weights = CreateSimpleWeights();
        var classifier = new LogisticRegression(weights, new[] { "en", "fr" });

        var features = new double[] { 1.0, 0.0, 1.0 };

        // Act
        var result = classifier.Predict(features);

        // Assert
        result.Should().BeInRange(0, 1);
    }

    [Fact]
    public void PredictProba_WithFeatureVector_ShouldReturnProbabilities()
    {
        // Arrange
        var weights = CreateSimpleWeights();
        var classifier = new LogisticRegression(weights, new[] { "en", "fr" });

        var features = new double[] { 1.0, 0.0, 1.0 };

        // Act
        var probabilities = classifier.PredictProba(features);

        // Assert
        probabilities.Should().HaveCount(2);
        probabilities.Sum().Should().BeApproximately(1.0, 0.0001); // Should sum to 1
        probabilities.Should().AllSatisfy(p => p.Should().BeGreaterThanOrEqualTo(0.0));
        probabilities.Should().AllSatisfy(p => p.Should().BeLessThanOrEqualTo(1.0));
    }

    [Fact]
    public void PredictWithLabel_ShouldReturnLanguageCode()
    {
        // Arrange
        var weights = CreateSimpleWeights();
        var classifier = new LogisticRegression(weights, new[] { "en", "fr" });

        var features = new double[] { 1.0, 0.0, 1.0 };

        // Act
        var (label, probability) = classifier.PredictWithLabel(features);

        // Assert
        label.Should().BeOneOf("en", "fr");
        probability.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void PredictTopK_ShouldReturnTopLanguages()
    {
        // Arrange
        var weights = CreateMultiClassWeights();
        var classifier = new LogisticRegression(weights, new[] { "en", "fr", "de", "es" });

        var features = new double[] { 1.0, 1.0, 0.0, 1.0 };

        // Act
        var top3 = classifier.PredictTopK(features, k: 3);

        // Assert
        top3.Should().HaveCount(3);
        top3.Select(r => r.probability).Should().BeInDescendingOrder();
        top3.Should().AllSatisfy(r => r.probability.Should().BeInRange(0.0, 1.0));
    }

    [Fact]
    public void Softmax_ShouldConvertScoresToProbabilities()
    {
        // Arrange
        var scores = new double[] { 2.0, 1.0, 0.1 };

        // Act
        var probabilities = LogisticRegression.Softmax(scores);

        // Assert
        probabilities.Should().HaveCount(3);
        probabilities.Sum().Should().BeApproximately(1.0, 0.0001);
        probabilities[0].Should().BeGreaterThan(probabilities[1]); // Higher score → higher probability
        probabilities[1].Should().BeGreaterThan(probabilities[2]);
    }

    private static SparseMatrix<double> CreateSimpleWeights()
    {
        var weights = new SparseMatrix<double>(rows: 2, cols: 3);
        weights.SetValue(0, 0, 1.0);  // Class 0 prefers feature 0
        weights.SetValue(0, 2, 0.5);
        weights.SetValue(1, 1, 1.0);  // Class 1 prefers feature 1
        weights.SetValue(1, 2, -0.5);
        return weights;
    }

    private static SparseMatrix<double> CreateMultiClassWeights()
    {
        var weights = new SparseMatrix<double>(rows: 4, cols: 4);
        // Simple weights for 4 classes
        for (int i = 0; i < 4; i++)
        {
            weights.SetValue(i, i, 1.0);
        }
        return weights;
    }
}
