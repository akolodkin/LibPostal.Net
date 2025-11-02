using FluentAssertions;
using LibPostal.Net.ML;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for CrfContext - the core CRF inference engine.
/// Based on libpostal's crf_context.c
/// </summary>
public class CrfContextTests
{
    [Fact]
    public void Constructor_WithDimensions_ShouldInitialize()
    {
        // Act
        var context = new CrfContext(numLabels: 5, numItems: 10);

        // Assert
        context.NumLabels.Should().Be(5);
        context.NumItems.Should().Be(10);
        context.State.Should().NotBeNull();
        context.State.Rows.Should().Be(10); // T×L matrix
        context.State.Columns.Should().Be(5);
    }

    [Fact]
    public void SetNumItems_ShouldResizeMatrices()
    {
        // Arrange
        var context = new CrfContext(5, 10);

        // Act
        context.SetNumItems(20);

        // Assert
        context.NumItems.Should().Be(20);
        context.State.Rows.Should().Be(20);
    }

    [Fact]
    public void Reset_ShouldClearMatrices()
    {
        // Arrange
        var context = new CrfContext(3, 5);
        context.State[0, 0] = 1.5;

        // Act
        context.Reset();

        // Assert
        context.State[0, 0].Should().Be(0.0);
    }

    [Fact]
    public void Viterbi_SimpleTwoLabelSequence_ShouldFindOptimalPath()
    {
        // Arrange: 2 labels (A=0, B=1), 3 tokens
        var context = new CrfContext(numLabels: 2, numItems: 3);

        // Token 0: A=1.0, B=0.0 (strongly prefer A)
        context.State[0, 0] = 1.0;
        context.State[0, 1] = 0.0;

        // Token 1: A=0.5, B=1.5 (strongly prefer B)
        context.State[1, 0] = 0.5;
        context.State[1, 1] = 1.5;

        // Token 2: A=2.0, B=0.5 (strongly prefer A)
        context.State[2, 0] = 2.0;
        context.State[2, 1] = 0.5;

        // Transitions: A→B=0.5, B→A=0.8, others=0
        context.Trans[0, 1] = 0.5;
        context.Trans[1, 0] = 0.8;

        // Act
        var labels = new uint[3];
        var score = context.Viterbi(labels);

        // Assert: Best path is A(1.0) → B(+0.5+1.5) → A(+0.8+2.0) = 5.8
        labels.Should().Equal(0u, 1u, 0u); // A, B, A
        score.Should().BeApproximately(5.8, 0.01);
    }

    [Fact]
    public void Viterbi_ThreeLabels_ShouldFindCorrectPath()
    {
        // Arrange: house_number(0), road(1), city(2)
        var context = new CrfContext(numLabels: 3, numItems: 3);

        // Token 0 "123": strongly house_number
        context.State[0, 0] = 5.0;  // house_number
        context.State[0, 1] = 0.1;  // road
        context.State[0, 2] = 0.1;  // city

        // Token 1 "main": strongly road
        context.State[1, 0] = 0.1;
        context.State[1, 1] = 5.0;  // road
        context.State[1, 2] = 0.2;

        // Token 2 "oakland": strongly city
        context.State[2, 0] = 0.1;
        context.State[2, 1] = 0.2;
        context.State[2, 2] = 5.0;  // city

        // Transitions prefer house_number → road → city
        context.Trans[0, 1] = 1.0;  // house_number → road
        context.Trans[1, 2] = 1.0;  // road → city

        // Act
        var labels = new uint[3];
        var score = context.Viterbi(labels);

        // Assert
        labels.Should().Equal(0u, 1u, 2u); // house_number, road, city
        score.Should().BeGreaterThan(10.0);
    }

    [Fact]
    public void Viterbi_WithAllSameScores_ShouldPickAnyValidPath()
    {
        // Arrange
        var context = new CrfContext(numLabels: 2, numItems: 2);
        // All scores equal
        context.State[0, 0] = 1.0;
        context.State[0, 1] = 1.0;
        context.State[1, 0] = 1.0;
        context.State[1, 1] = 1.0;

        // Act
        var labels = new uint[2];
        var score = context.Viterbi(labels);

        // Assert
        labels.Should().HaveCount(2);
        labels.Should().AllSatisfy(label => label.Should().BeLessThan(2));
        score.Should().BeApproximately(2.0, 0.01);
    }

    [Fact]
    public void Viterbi_SingleToken_ShouldReturnBestLabel()
    {
        // Arrange
        var context = new CrfContext(numLabels: 3, numItems: 1);
        context.State[0, 0] = 1.0;
        context.State[0, 1] = 5.0;  // Best
        context.State[0, 2] = 2.0;

        // Act
        var labels = new uint[1];
        var score = context.Viterbi(labels);

        // Assert
        labels[0].Should().Be(1); // Best score
        score.Should().BeApproximately(5.0, 0.01);
    }

    [Fact]
    public void Viterbi_WithNegativeScores_ShouldHandleCorrectly()
    {
        // Arrange
        var context = new CrfContext(numLabels: 2, numItems: 2);
        context.State[0, 0] = -1.0;
        context.State[0, 1] = -2.0;
        context.State[1, 0] = -0.5;
        context.State[1, 1] = -1.5;
        context.Trans[0, 0] = 0.1;
        context.Trans[1, 1] = 0.1;

        // Act
        var labels = new uint[2];
        var score = context.Viterbi(labels);

        // Assert
        labels.Should().Equal(0u, 0u); // Less negative path
        score.Should().BeLessThan(0.0); // Negative score is valid
    }
}
