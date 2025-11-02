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
