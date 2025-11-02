namespace LibPostal.Net.IO;

/// <summary>
/// Reads trie data from libpostal binary format.
/// </summary>
/// <remarks>
/// <para>
/// This is a simplified implementation for Phase 3. It reads a basic key-value format
/// suitable for testing and development.
/// </para>
/// <para>
/// Future enhancement (Phase 6+): Implement full double-array trie reading for production use.
/// The full libpostal format includes:
/// - Alphabet (character mapping)
/// - Base array (double-array trie base)
/// - Check array (double-array trie check)
/// - Data nodes (values)
/// - Tail array (suffix storage)
/// </para>
/// </remarks>
public sealed class TrieReader : IDisposable
{
    private readonly Dictionary<string, uint> _data;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrieReader"/> class.
    /// </summary>
    /// <param name="stream">The stream containing trie data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file signature is invalid.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends unexpectedly.</exception>
    public TrieReader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _data = new Dictionary<string, uint>();

        // Validate signature
        FileSignature.ValidateTrieSignature(stream);

        // Read the trie data
        LoadFromStream(stream);
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">When this method returns, contains the value associated with the key, if found; otherwise, 0.</param>
    /// <returns>True if the key was found; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public bool TryGetValue(string key, out uint value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(key))
        {
            value = 0;
            return false;
        }

        return _data.TryGetValue(key, out value);
    }

    /// <summary>
    /// Loads trie data from the stream.
    /// </summary>
    private void LoadFromStream(Stream stream)
    {
        using var reader = new BigEndianBinaryReader(stream);

        // Skip past the signature (ValidateTrieSignature resets position to 0)
        reader.ReadUInt32(); // Skip signature

        // Read number of entries (simplified format for Phase 3)
        var count = reader.ReadUInt32();

        // Read each entry
        for (uint i = 0; i < count; i++)
        {
            var key = reader.ReadLengthPrefixedString();
            var value = reader.ReadUInt32();
            _data[key] = value;
        }

        // TODO: Phase 6 - Implement full double-array trie reading
        // This will include:
        // - Reading alphabet
        // - Reading base/check arrays
        // - Reading data nodes
        // - Reading tail array
        // - Implementing trie traversal instead of dictionary lookup
    }

    /// <summary>
    /// Disposes the reader.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _data.Clear();
            _disposed = true;
        }
    }
}
