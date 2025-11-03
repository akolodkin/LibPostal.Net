using LibPostal.Net.IO;
using System.Text;

namespace LibPostal.Net.Core;

/// <summary>
/// Loads libpostal double-array trie format and converts to Trie&lt;T&gt;.
/// Based on libpostal's trie.c (lines 979-1122)
/// </summary>
public static class DoubleArrayTrieLoader
{
    private const uint TrieSignature = 0xABABABAB;
    private const int RootNodeId = 2;

    /// <summary>
    /// Node in double-array trie.
    /// </summary>
    private struct TrieNode
    {
        public int Base;
        public int Check;
    }

    /// <summary>
    /// Data node in double-array trie.
    /// </summary>
    private struct TrieDataNode
    {
        public uint Tail;
        public uint Data;
    }

    /// <summary>
    /// Loads a libpostal double-array trie file and converts to Trie&lt;T&gt;.
    /// </summary>
    /// <typeparam name="TData">The data type stored in trie values.</typeparam>
    /// <param name="stream">The stream containing the trie data.</param>
    /// <returns>A Trie&lt;TData&gt; with all keys and values.</returns>
    /// <exception cref="InvalidDataException">Thrown when file signature is invalid.</exception>
    public static Trie<TData> LoadLibpostalTrie<TData>(Stream stream) where TData : struct
    {
        using var reader = new BigEndianBinaryReader(stream);

        // Read and validate signature
        var signature = reader.ReadUInt32();
        if (signature != TrieSignature)
        {
            throw new InvalidDataException(
                $"Invalid trie signature. Expected 0x{TrieSignature:X8}, got 0x{signature:X8}.");
        }

        // Read alphabet
        var alphabetSize = reader.ReadUInt32();
        var alphabet = new byte[alphabetSize];
        for (int i = 0; i < alphabetSize; i++)
        {
            alphabet[i] = reader.ReadByte();
        }

        // Build reverse alphabet map (byte → index)
        var alphaMap = new byte[256];
        for (int i = 0; i < alphabetSize; i++)
        {
            alphaMap[alphabet[i]] = (byte)(i + 1); // +1 offset
        }

        // Read number of keys
        var numKeys = reader.ReadUInt32();

        // Read nodes
        var numNodes = reader.ReadUInt32();
        var nodes = new TrieNode[numNodes];

        for (int i = 0; i < numNodes; i++)
        {
            nodes[i] = new TrieNode
            {
                Base = unchecked((int)reader.ReadUInt32()),
                Check = unchecked((int)reader.ReadUInt32())
            };
        }

        // Read data nodes
        var numDataNodes = reader.ReadUInt32();
        var dataNodes = new TrieDataNode[numDataNodes];

        for (int i = 0; i < numDataNodes; i++)
        {
            dataNodes[i] = new TrieDataNode
            {
                Tail = reader.ReadUInt32(),
                Data = reader.ReadUInt32()
            };
        }

        // Read tail
        var tailLen = reader.ReadUInt32();
        var tail = new byte[tailLen];
        for (int i = 0; i < tailLen; i++)
        {
            tail[i] = reader.ReadByte();
        }

        // Extract all keys by traversing the trie
        var result = new Trie<TData>();
        ExtractKeys(nodes, dataNodes, tail, alphabet, alphaMap, result);

        return result;
    }

    /// <summary>
    /// Extracts all keys from double-array trie by traversal.
    /// </summary>
    private static void ExtractKeys<TData>(
        TrieNode[] nodes,
        TrieDataNode[] dataNodes,
        byte[] tail,
        byte[] alphabet,
        byte[] alphaMap,
        Trie<TData> result) where TData : struct
    {
        var currentKey = new StringBuilder();
        Traverse(RootNodeId, nodes, dataNodes, tail, alphabet, currentKey, result);
    }

    /// <summary>
    /// Recursively traverses the double-array trie to extract keys.
    /// </summary>
    private static void Traverse<TData>(
        int nodeId,
        TrieNode[] nodes,
        TrieDataNode[] dataNodes,
        byte[] tail,
        byte[] alphabet,
        StringBuilder currentKey,
        Trie<TData> result) where TData : struct
    {
        if (nodeId < 0 || nodeId >= nodes.Length)
            return;

        var node = nodes[nodeId];

        // Check if this is a terminal node (negative base)
        if (node.Base < 0)
        {
            var dataIndex = -node.Base - 1;
            if (dataIndex >= 0 && dataIndex < dataNodes.Length)
            {
                var dataNode = dataNodes[dataIndex];
                var key = currentKey.ToString();

                // Append tail suffix if present
                if (dataNode.Tail > 0 && dataNode.Tail < tail.Length)
                {
                    var tailStr = ExtractTailString(tail, (int)dataNode.Tail);
                    key += tailStr;
                }

                // Add to result trie
                var data = ConvertData<TData>(dataNode.Data);
                result.Add(key, data);
            }
            return; // Terminal node - don't continue traversal
        }

        // Non-terminal node - try all alphabet characters
        for (int i = 0; i < alphabet.Length; i++)
        {
            var ch = (char)alphabet[i];
            var nextNodeId = node.Base + i + 1; // +1 for alpha_map offset

            if (nextNodeId >= 0 && nextNodeId < nodes.Length)
            {
                var nextNode = nodes[nextNodeId];
                if (nextNode.Check == nodeId) // Valid transition
                {
                    currentKey.Append(ch);
                    Traverse(nextNodeId, nodes, dataNodes, tail, alphabet, currentKey, result);
                    currentKey.Length--; // Backtrack
                }
            }
        }
    }

    /// <summary>
    /// Extracts a NUL-terminated string from the tail array.
    /// </summary>
    private static string ExtractTailString(byte[] tail, int startIndex)
    {
        var endIndex = startIndex;
        while (endIndex < tail.Length && tail[endIndex] != 0)
        {
            endIndex++;
        }

        if (endIndex == startIndex)
            return string.Empty;

        return Encoding.UTF8.GetString(tail, startIndex, endIndex - startIndex);
    }

    /// <summary>
    /// Converts uint32 data to the target type.
    /// </summary>
    private static TData ConvertData<TData>(uint data) where TData : struct
    {
        if (typeof(TData) == typeof(uint))
        {
            return (TData)(object)data;
        }
        else if (typeof(TData) == typeof(int))
        {
            return (TData)(object)unchecked((int)data);
        }
        else if (typeof(TData) == typeof(ulong))
        {
            return (TData)(object)(ulong)data;
        }
        else if (typeof(TData) == typeof(short))
        {
            return (TData)(object)(short)data;
        }
        else if (typeof(TData) == typeof(byte))
        {
            return (TData)(object)(byte)data;
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(TData)} is not supported for trie data.");
        }
    }
}
