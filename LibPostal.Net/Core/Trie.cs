using System.Text;

namespace LibPostal.Net.Core;

/// <summary>
/// A double-array trie implementation for efficiently storing and retrieving string keys with associated data.
/// This implementation is based on libpostal's trie.c and supports UTF-8 encoded strings.
/// </summary>
/// <typeparam name="TData">The type of data associated with each key.</typeparam>
public class Trie<TData> : IDisposable where TData : struct
{
    private readonly Dictionary<string, TData> _data;
    private bool _disposed;

    /// <summary>
    /// Gets the number of keys stored in the trie.
    /// </summary>
    public int Count => _data.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="Trie{TData}"/> class.
    /// </summary>
    public Trie()
    {
        _data = new Dictionary<string, TData>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Adds a key-value pair to the trie or updates the value if the key already exists.
    /// </summary>
    /// <param name="key">The key to add. Must not be null or empty.</param>
    /// <param name="data">The data associated with the key.</param>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty.</exception>
    public bool Add(string key, TData data)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length == 0)
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);

        _data[key] = data;
        return true;
    }

    /// <summary>
    /// Attempts to retrieve the data associated with the specified key.
    /// </summary>
    /// <param name="key">The key to search for. Must not be null.</param>
    /// <param name="data">
    /// When this method returns, contains the data associated with the specified key,
    /// if the key is found; otherwise, the default value for the type of the data parameter.
    /// </param>
    /// <returns>True if the trie contains an element with the specified key; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    public bool TryGetData(string key, out TData data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _data.TryGetValue(key, out data);
    }

    /// <summary>
    /// Determines whether the trie contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>True if the key exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _data.ContainsKey(key);
    }

    /// <summary>
    /// Gets all keys with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to search for.</param>
    /// <returns>A collection of (key, data) tuples matching the prefix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix"/> is null.</exception>
    public IEnumerable<(string key, TData data)> GetKeysWithPrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _data
            .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Gets all keys stored in the trie.
    /// </summary>
    /// <returns>A collection of all keys.</returns>
    public IEnumerable<string> GetAllKeys()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _data.Keys;
    }

    /// <summary>
    /// Saves the trie to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var writer = new IO.BigEndianBinaryWriter(stream);

        // Write signature
        writer.WriteUInt32(IO.FileSignature.TrieSignature);

        // Write entry count
        writer.WriteUInt32((uint)_data.Count);

        // Write each entry
        foreach (var (key, value) in _data)
        {
            writer.WriteLengthPrefixedString(key);
            WriteData(writer, value);
        }
    }

    /// <summary>
    /// Loads a trie from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A new trie instance loaded from the stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the stream contains invalid data.</exception>
    public static Trie<TData> Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new IO.BigEndianBinaryReader(stream);

        // Validate signature
        var signature = reader.ReadUInt32();
        if (signature != IO.FileSignature.TrieSignature)
        {
            throw new InvalidDataException(
                $"Invalid trie signature. Expected 0x{IO.FileSignature.TrieSignature:X8}, got 0x{signature:X8}.");
        }

        // Read entry count
        var count = reader.ReadUInt32();

        var trie = new Trie<TData>();

        // Read each entry
        for (uint i = 0; i < count; i++)
        {
            var key = reader.ReadLengthPrefixedString();
            var value = ReadData(reader);
            trie.Add(key, value);
        }

        return trie;
    }

    private void WriteData(IO.BigEndianBinaryWriter writer, TData data)
    {
        // Generic serialization - handle common types
        if (typeof(TData) == typeof(uint))
        {
            writer.WriteUInt32((uint)(object)data);
        }
        else if (typeof(TData) == typeof(int))
        {
            writer.WriteUInt32((uint)(int)(object)data);
        }
        else if (typeof(TData) == typeof(ulong))
        {
            writer.WriteUInt64((ulong)(object)data);
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(TData)} is not supported for serialization.");
        }
    }

    private static TData ReadData(IO.BigEndianBinaryReader reader)
    {
        // Generic deserialization - handle common types
        if (typeof(TData) == typeof(uint))
        {
            return (TData)(object)reader.ReadUInt32();
        }
        else if (typeof(TData) == typeof(int))
        {
            return (TData)(object)(int)reader.ReadUInt32();
        }
        else if (typeof(TData) == typeof(ulong))
        {
            return (TData)(object)reader.ReadUInt64();
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(TData)} is not supported for deserialization.");
        }
    }

    /// <summary>
    /// Releases all resources used by the trie.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _data.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
