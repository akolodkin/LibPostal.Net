using FluentAssertions;
using LibPostal.Net.ML;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for Graph serialization matching libpostal binary format.
/// </summary>
public class GraphSerializationTests
{
    [Fact]
    public void WriteGraph_WithSimpleGraph_ShouldWriteCorrectFormat()
    {
        // Arrange - 4 node graph with 3 edges
        // 0 -> 1, 2
        // 1 -> 3
        // 2 -> (none)
        // 3 -> (none)
        var graph = new Graph(4);
        graph.AddEdge(0, 1);
        graph.AddEdge(0, 2);
        graph.AddEdge(1, 3);

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        // type (0 = directed)
        reader.ReadUInt32().Should().Be(0);

        // m (source vertices)
        reader.ReadUInt32().Should().Be(4);

        // n (destination vertices)
        reader.ReadUInt32().Should().Be(4);

        // indptr_len (m + 1)
        reader.ReadUInt64().Should().Be(5);

        // indptr [0, 2, 3, 3, 3]
        reader.ReadUInt32Array(5).Should().Equal(0, 2, 3, 3, 3);

        // indices_len
        reader.ReadUInt64().Should().Be(3);

        // indices [1, 2, 3]
        reader.ReadUInt32Array(3).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ReadGraph_WithValidFormat_ShouldReadCorrectly()
    {
        // Arrange - create binary data for simple graph
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(0); // type (directed)
            writer.WriteUInt32(3); // m
            writer.WriteUInt32(3); // n

            writer.WriteUInt64(4); // indptr_len
            writer.WriteUInt32Array(new uint[] { 0, 1, 2, 2 }); // indptr

            writer.WriteUInt64(2); // indices_len
            writer.WriteUInt32Array(new uint[] { 1, 2 }); // indices
        }

        stream.Position = 0;

        // Act
        var graph = GraphSerializer.ReadGraph(stream);

        // Assert
        graph.NumNodes.Should().Be(3);
        graph.NumEdges.Should().Be(2);
        graph.HasEdge(0, 1).Should().BeTrue();
        graph.HasEdge(1, 2).Should().BeTrue();
        graph.HasEdge(0, 2).Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_Graph_ShouldPreserveStructure()
    {
        // Arrange - postal code context graph
        // 11216 (Brooklyn) -> NY, Kings County
        var original = new Graph(10);
        original.AddEdge(0, 1); // postal -> state
        original.AddEdge(0, 2); // postal -> county
        original.AddEdge(1, 3); // state -> country
        original.AddEdge(5, 6);
        original.AddEdge(5, 7);

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, original);
        stream.Position = 0;
        var result = GraphSerializer.ReadGraph(stream);

        // Assert
        result.NumNodes.Should().Be(original.NumNodes);
        result.NumEdges.Should().Be(original.NumEdges);
        result.HasEdge(0, 1).Should().BeTrue();
        result.HasEdge(0, 2).Should().BeTrue();
        result.HasEdge(1, 3).Should().BeTrue();
        result.HasEdge(5, 6).Should().BeTrue();
        result.HasEdge(5, 7).Should().BeTrue();
        result.HasEdge(0, 3).Should().BeFalse();
    }

    [Fact]
    public void WriteGraph_WithEmptyGraph_ShouldWriteCorrectly()
    {
        // Arrange - graph with nodes but no edges
        var graph = new Graph(5);

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        reader.ReadUInt32().Should().Be(0); // type
        reader.ReadUInt32().Should().Be(5); // m
        reader.ReadUInt32().Should().Be(5); // n
        reader.ReadUInt64().Should().Be(6); // indptr_len (5 + 1)
        reader.ReadUInt32Array(6).Should().Equal(0, 0, 0, 0, 0, 0); // all zeros
        reader.ReadUInt64().Should().Be(0); // indices_len
    }

    [Fact]
    public void ReadGraph_WithEmptyGraph_ShouldReturnEmptyGraph()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(0); // type
            writer.WriteUInt32(4); // m
            writer.WriteUInt32(4); // n
            writer.WriteUInt64(5); // indptr_len
            writer.WriteUInt32Array(new uint[] { 0, 0, 0, 0, 0 });
            writer.WriteUInt64(0); // indices_len
        }

        stream.Position = 0;

        // Act
        var graph = GraphSerializer.ReadGraph(stream);

        // Assert
        graph.NumNodes.Should().Be(4);
        graph.NumEdges.Should().Be(0);
    }

    [Fact]
    public void WriteGraph_WithFullyConnectedGraph_ShouldWriteCorrectly()
    {
        // Arrange - every node connects to every other node
        var graph = new Graph(3);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (i != j) // no self-loops
                {
                    graph.AddEdge(i, j);
                }
            }
        }

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);
        stream.Position = 0;
        var result = GraphSerializer.ReadGraph(stream);

        // Assert
        result.NumNodes.Should().Be(3);
        result.NumEdges.Should().Be(6); // 3 * 2 edges

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (i != j)
                {
                    result.HasEdge(i, j).Should().BeTrue($"edge {i} -> {j} should exist");
                }
            }
        }
    }

    [Fact]
    public void WriteGraph_WithChainGraph_ShouldWriteCorrectly()
    {
        // Arrange - linear chain: 0 -> 1 -> 2 -> 3
        var graph = new Graph(4);
        graph.AddEdge(0, 1);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);
        stream.Position = 0;
        var result = GraphSerializer.ReadGraph(stream);

        // Assert
        result.NumEdges.Should().Be(3);
        result.HasEdge(0, 1).Should().BeTrue();
        result.HasEdge(1, 2).Should().BeTrue();
        result.HasEdge(2, 3).Should().BeTrue();
        result.HasEdge(0, 2).Should().BeFalse();
        result.HasEdge(1, 3).Should().BeFalse();
    }

    [Fact]
    public void WriteGraph_WithSingleNode_ShouldWriteCorrectly()
    {
        // Arrange - graph with single node
        var graph = new Graph(1);

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        reader.ReadUInt32().Should().Be(0);
        reader.ReadUInt32().Should().Be(1);
        reader.ReadUInt32().Should().Be(1);
        reader.ReadUInt64().Should().Be(2);
        reader.ReadUInt32Array(2).Should().Equal(0, 0);
        reader.ReadUInt64().Should().Be(0);
    }

    [Fact]
    public void WriteGraph_WithStarGraph_ShouldWriteCorrectly()
    {
        // Arrange - star: center node 0 connects to all others
        var graph = new Graph(5);
        graph.AddEdge(0, 1);
        graph.AddEdge(0, 2);
        graph.AddEdge(0, 3);
        graph.AddEdge(0, 4);

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);
        stream.Position = 0;
        var result = GraphSerializer.ReadGraph(stream);

        // Assert
        result.NumEdges.Should().Be(4);
        result.GetNeighbors(0).Should().HaveCount(4);
        result.HasEdge(0, 1).Should().BeTrue();
        result.HasEdge(0, 2).Should().BeTrue();
        result.HasEdge(0, 3).Should().BeTrue();
        result.HasEdge(0, 4).Should().BeTrue();
    }

    [Fact]
    public void WriteGraph_WithLargeGraph_ShouldHandleCorrectly()
    {
        // Arrange - 1000 node graph with various edges
        var graph = new Graph(1000);
        for (int i = 0; i < 500; i++)
        {
            graph.AddEdge(i, (i + 1) % 1000);
            graph.AddEdge(i, (i + 10) % 1000);
        }

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);
        stream.Position = 0;
        var result = GraphSerializer.ReadGraph(stream);

        // Assert
        result.NumNodes.Should().Be(1000);
        result.NumEdges.Should().Be(1000);

        // Spot check
        result.HasEdge(0, 1).Should().BeTrue();
        result.HasEdge(0, 10).Should().BeTrue();
        result.HasEdge(100, 101).Should().BeTrue();
        result.HasEdge(100, 110).Should().BeTrue();
    }

    [Fact]
    public void WriteGraph_WithNullGraph_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        Action act = () => GraphSerializer.WriteGraph(stream, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadGraph_WithNullStream_ShouldThrow()
    {
        // Act
        Action act = () => GraphSerializer.ReadGraph(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteGraph_WithNullStream_ShouldThrow()
    {
        // Arrange
        var graph = new Graph(2);

        // Act
        Action act = () => GraphSerializer.WriteGraph(null!, graph);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteGraph_WithMultipleEdgesToSameNode_ShouldDeduplicateCorrectly()
    {
        // Arrange - test that duplicate edges are handled
        var graph = new Graph(3);
        graph.AddEdge(0, 1);
        graph.AddEdge(0, 1); // duplicate - should be ignored
        graph.AddEdge(0, 2);

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);
        stream.Position = 0;
        var result = GraphSerializer.ReadGraph(stream);

        // Assert
        result.NumEdges.Should().Be(2); // duplicates removed
        result.GetNeighbors(0).Should().HaveCount(2);
    }

    [Fact]
    public void WriteGraph_MatchesLibpostalFormat_Integration()
    {
        // Arrange - realistic postal code context graph
        var graph = new Graph(100);

        // Simulate postal code -> admin region mappings
        var random = new Random(42);
        for (int i = 0; i < 50; i++)
        {
            int postal = i;
            int state = 50 + (i % 10);
            int county = 60 + (i % 20);

            graph.AddEdge(postal, state);
            graph.AddEdge(postal, county);
        }

        using var stream = new MemoryStream();

        // Act
        GraphSerializer.WriteGraph(stream, graph);

        // Assert - verify binary format structure
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        var type = reader.ReadUInt32();
        var m = reader.ReadUInt32();
        var n = reader.ReadUInt32();

        type.Should().Be(0); // directed
        m.Should().Be(100);
        n.Should().Be(100);

        var indptrLen = reader.ReadUInt64();
        indptrLen.Should().Be(101); // m + 1

        var indptr = reader.ReadUInt32Array((int)indptrLen);
        indptr[0].Should().Be(0);
        indptr[100].Should().BeGreaterThan(0);

        // Verify CSR property: indptr is non-decreasing
        for (int i = 1; i < indptr.Length; i++)
        {
            indptr[i].Should().BeGreaterThanOrEqualTo(indptr[i - 1]);
        }
    }
}
