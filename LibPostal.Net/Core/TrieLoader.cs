using LibPostal.Net.IO;

namespace LibPostal.Net.Core;

/// <summary>
/// Utility for loading libpostal trie files in double-array format.
/// This is a conversion layer that reads libpostal's complex format
/// and converts to our simplified dictionary-based Trie.
/// </summary>
/// <remarks>
/// Libpostal uses a double-array trie (DA-Trie) for memory efficiency.
/// For Phase 8, we implement a simplified loader that can read the basic
/// trie format and convert it to our Trie implementation.
/// Full double-array trie support is a future enhancement.
/// </remarks>
public static class TrieLoader
{
    /// <summary>
    /// Loads a trie from a libpostal-format binary stream.
    /// </summary>
    /// <typeparam name="TData">The data type stored in the trie.</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A Trie loaded from the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid data.</exception>
    public static Trie<TData> LoadLibpostalTrie<TData>(Stream stream) where TData : struct
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BigEndianBinaryReader(stream);

        // Read and validate signature
        var signature = reader.ReadUInt32();
        if (signature != FileSignature.TrieSignature)
        {
            throw new InvalidDataException($"Invalid trie signature: 0x{signature:X8}. Expected 0x{FileSignature.TrieSignature:X8}");
        }

        // Read number of keys
        var numKeys = reader.ReadUInt32();

        // Create new trie
        var trie = new Trie<TData>();

        // Read each key-value pair
        // Note: For now, we're using the simplified format (same as our Trie.Save format)
        // Full double-array trie support would require reading:
        // - alphabet_size, alphabet
        // - nodes (base/check arrays)
        // - data (tail/data arrays)
        // - tail strings
        // This is deferred to future enhancement.
        for (uint i = 0; i < numKeys; i++)
        {
            var key = reader.ReadLengthPrefixedString();

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Trie contains empty key, which is not allowed.");
            }

            var data = ReadData<TData>(reader);

            // Add to trie (if duplicate, last value wins)
            trie.Add(key, data);
        }

        return trie;
    }

    /// <summary>
    /// Reads typed data from the stream based on the generic type parameter.
    /// </summary>
    private static TData ReadData<TData>(BigEndianBinaryReader reader) where TData : struct
    {
        if (typeof(TData) == typeof(uint))
        {
            var value = reader.ReadUInt32();
            return (TData)(object)value;
        }
        else if (typeof(TData) == typeof(ulong))
        {
            var value = reader.ReadUInt64();
            return (TData)(object)value;
        }
        else if (typeof(TData) == typeof(int))
        {
            // Read as uint32 but interpret as int
            var value = unchecked((int)reader.ReadUInt32());
            return (TData)(object)value;
        }
        else if (typeof(TData) == typeof(long))
        {
            // Read as uint64 but interpret as long
            var value = unchecked((long)reader.ReadUInt64());
            return (TData)(object)value;
        }
        else if (typeof(TData) == typeof(ushort))
        {
            var value = reader.ReadUInt16();
            return (TData)(object)value;
        }
        else if (typeof(TData) == typeof(short))
        {
            var value = unchecked((short)reader.ReadUInt16());
            return (TData)(object)value;
        }
        else if (typeof(TData) == typeof(byte))
        {
            var value = reader.ReadByte();
            return (TData)(object)value;
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(TData)} is not supported for trie data.");
        }
    }
}
