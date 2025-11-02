namespace LibPostal.Net.ML;

/// <summary>
/// Directed graph for postal code context relationships.
/// Based on libpostal's graph.c
/// </summary>
public class Graph
{
    private readonly Dictionary<int, HashSet<int>> _adjacencyList;
    private readonly int _numNodes;

    /// <summary>
    /// Gets the number of nodes.
    /// </summary>
    public int NumNodes => _numNodes;

    /// <summary>
    /// Gets the number of edges.
    /// </summary>
    public int NumEdges { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Graph"/> class.
    /// </summary>
    /// <param name="numNodes">The number of nodes.</param>
    public Graph(int numNodes)
    {
        _numNodes = numNodes;
        _adjacencyList = new Dictionary<int, HashSet<int>>();
        NumEdges = 0;
    }

    /// <summary>
    /// Adds a directed edge from source to destination.
    /// </summary>
    /// <param name="source">The source node.</param>
    /// <param name="destination">The destination node.</param>
    public void AddEdge(int source, int destination)
    {
        if (!_adjacencyList.ContainsKey(source))
        {
            _adjacencyList[source] = new HashSet<int>();
        }

        if (_adjacencyList[source].Add(destination))
        {
            NumEdges++;
        }
    }

    /// <summary>
    /// Determines whether an edge exists from source to destination.
    /// </summary>
    /// <param name="source">The source node.</param>
    /// <param name="destination">The destination node.</param>
    /// <returns>True if the edge exists; otherwise, false.</returns>
    public bool HasEdge(int source, int destination)
    {
        if (_adjacencyList.TryGetValue(source, out var neighbors))
        {
            return neighbors.Contains(destination);
        }

        return false;
    }

    /// <summary>
    /// Gets all neighbors of a node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>Collection of neighbor node IDs.</returns>
    public IEnumerable<int> GetNeighbors(int node)
    {
        if (_adjacencyList.TryGetValue(node, out var neighbors))
        {
            return neighbors;
        }

        return Enumerable.Empty<int>();
    }

    /// <summary>
    /// Clears all edges from the graph.
    /// </summary>
    public void Clear()
    {
        _adjacencyList.Clear();
        NumEdges = 0;
    }
}
