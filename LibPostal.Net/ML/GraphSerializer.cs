using LibPostal.Net.IO;

namespace LibPostal.Net.ML;

/// <summary>
/// Serializer for Graph in CSR format matching libpostal's binary format.
/// </summary>
/// <remarks>
/// Binary format (big-endian):
/// - type (uint32): graph type (0=directed, 1=undirected, 2=bipartite)
/// - m (uint32): number of source vertices
/// - n (uint32): number of destination vertices
/// - indptr_len (uint64): length of indptr array (m + 1)
/// - indptr (uint32[]): row pointer array (CSR format)
/// - indices_len (uint64): length of indices array (number of edges)
/// - indices (uint32[]): column indices (CSR format)
/// </remarks>
public static class GraphSerializer
{
    /// <summary>
    /// Graph types matching libpostal's graph.h
    /// </summary>
    private const uint GraphTypeDirected = 0;
    private const uint GraphTypeUndirected = 1;
    private const uint GraphTypeBipartite = 2;

    /// <summary>
    /// Writes a graph to a stream in CSR format.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="graph">The graph to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream or graph is null.</exception>
    public static void WriteGraph(Stream stream, Graph graph)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(graph);

        using var writer = new BigEndianBinaryWriter(stream);

        // Convert graph to CSR format
        var (indptr, indices) = ConvertToCSR(graph);

        // Write type (always directed for now)
        writer.WriteUInt32(GraphTypeDirected);

        // Write dimensions (m = n for non-bipartite graphs)
        writer.WriteUInt32((uint)graph.NumNodes);
        writer.WriteUInt32((uint)graph.NumNodes);

        // Write indptr (row pointers)
        writer.WriteUInt64((ulong)indptr.Length);
        writer.WriteUInt32Array(Array.ConvertAll(indptr, x => (uint)x));

        // Write indices (column indices)
        writer.WriteUInt64((ulong)indices.Length);
        writer.WriteUInt32Array(Array.ConvertAll(indices, x => (uint)x));
    }

    /// <summary>
    /// Reads a graph from a stream in CSR format.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The graph.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static Graph ReadGraph(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BigEndianBinaryReader(stream);

        // Read type (ignored for now - always treat as directed)
        var type = reader.ReadUInt32();

        // Read dimensions
        var m = (int)reader.ReadUInt32();
        var n = (int)reader.ReadUInt32();

        // Read indptr (row pointers)
        var indptrLen = (int)reader.ReadUInt64();
        var indptrUint = reader.ReadUInt32Array(indptrLen);
        var indptr = Array.ConvertAll(indptrUint, x => (int)x);

        // Read indices (column indices)
        var indicesLen = (int)reader.ReadUInt64();
        var indicesUint = reader.ReadUInt32Array(indicesLen);
        var indices = Array.ConvertAll(indicesUint, x => (int)x);

        // Create graph from CSR format
        return CreateFromCSR(m, indptr, indices);
    }

    /// <summary>
    /// Converts a graph to CSR (Compressed Sparse Row) format.
    /// </summary>
    /// <param name="graph">The graph to convert.</param>
    /// <returns>Tuple of (indptr, indices).</returns>
    private static (int[] indptr, int[] indices) ConvertToCSR(Graph graph)
    {
        var indptr = new int[graph.NumNodes + 1];
        var indicesList = new List<int>();

        int edgeIndex = 0;
        for (int node = 0; node < graph.NumNodes; node++)
        {
            indptr[node] = edgeIndex;

            // Get neighbors and sort them for consistent output
            var neighbors = graph.GetNeighbors(node).OrderBy(x => x).ToList();

            foreach (var neighbor in neighbors)
            {
                indicesList.Add(neighbor);
                edgeIndex++;
            }
        }

        indptr[graph.NumNodes] = edgeIndex;

        return (indptr, indicesList.ToArray());
    }

    /// <summary>
    /// Creates a graph from CSR (Compressed Sparse Row) format.
    /// </summary>
    /// <param name="numNodes">The number of nodes.</param>
    /// <param name="indptr">The row pointer array.</param>
    /// <param name="indices">The column indices array.</param>
    /// <returns>The graph.</returns>
    private static Graph CreateFromCSR(int numNodes, int[] indptr, int[] indices)
    {
        var graph = new Graph(numNodes);

        for (int node = 0; node < numNodes; node++)
        {
            int start = indptr[node];
            int end = indptr[node + 1];

            for (int idx = start; idx < end; idx++)
            {
                int neighbor = indices[idx];
                graph.AddEdge(node, neighbor);
            }
        }

        return graph;
    }
}
