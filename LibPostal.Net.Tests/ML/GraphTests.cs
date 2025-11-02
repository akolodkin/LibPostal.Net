using FluentAssertions;
using LibPostal.Net.ML;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for Graph class.
/// Used for postal code context relationships.
/// </summary>
public class GraphTests
{
    [Fact]
    public void Constructor_WithNumNodes_ShouldInitialize()
    {
        // Act
        var graph = new Graph(numNodes: 10);

        // Assert
        graph.NumNodes.Should().Be(10);
        graph.NumEdges.Should().Be(0);
    }

    [Fact]
    public void AddEdge_ShouldIncrementEdgeCount()
    {
        // Arrange
        var graph = new Graph(5);

        // Act
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 2);

        // Assert
        graph.NumEdges.Should().Be(2);
    }

    [Fact]
    public void HasEdge_WithExistingEdge_ShouldReturnTrue()
    {
        // Arrange
        var graph = new Graph(5);
        graph.AddEdge(0, 1);

        // Act
        var result = graph.HasEdge(0, 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasEdge_WithNonExistingEdge_ShouldReturnFalse()
    {
        // Arrange
        var graph = new Graph(5);
        graph.AddEdge(0, 1);

        // Act
        var result = graph.HasEdge(0, 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AddEdge_ShouldNotAddDuplicates()
    {
        // Arrange
        var graph = new Graph(5);

        // Act
        graph.AddEdge(0, 1);
        graph.AddEdge(0, 1); // Duplicate

        // Assert
        graph.NumEdges.Should().Be(1); // Should only count once
        graph.HasEdge(0, 1).Should().BeTrue();
    }

    [Fact]
    public void GetNeighbors_ShouldReturnConnectedNodes()
    {
        // Arrange
        var graph = new Graph(5);
        graph.AddEdge(0, 1);
        graph.AddEdge(0, 2);
        graph.AddEdge(0, 3);

        // Act
        var neighbors = graph.GetNeighbors(0);

        // Assert
        neighbors.Should().HaveCount(3);
        neighbors.Should().Contain(1);
        neighbors.Should().Contain(2);
        neighbors.Should().Contain(3);
    }

    [Fact]
    public void GetNeighbors_WithNoEdges_ShouldReturnEmpty()
    {
        // Arrange
        var graph = new Graph(5);

        // Act
        var neighbors = graph.GetNeighbors(0);

        // Assert
        neighbors.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllEdges()
    {
        // Arrange
        var graph = new Graph(5);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 2);

        // Act
        graph.Clear();

        // Assert
        graph.NumEdges.Should().Be(0);
        graph.HasEdge(0, 1).Should().BeFalse();
        graph.HasEdge(1, 2).Should().BeFalse();
    }

    [Fact]
    public void Graph_DirectedEdges_ShouldWorkCorrectly()
    {
        // Arrange
        var graph = new Graph(5);

        // Act
        graph.AddEdge(0, 1);

        // Assert
        graph.HasEdge(0, 1).Should().BeTrue();
        graph.HasEdge(1, 0).Should().BeFalse(); // Should be directed
    }
}
