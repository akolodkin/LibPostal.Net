using LibPostal.Net.IO;

namespace LibPostal.Net.Expansion;

/// <summary>
/// Reads address dictionaries from binary format.
/// Based on libpostal's address_dictionary.c
/// </summary>
public sealed class AddressDictionaryReader : IDisposable
{
    private const uint DictionarySignature = 0xBABABABA;

    private readonly List<string> _canonicalStrings;
    private readonly List<AddressExpansionValue> _expansionValues;
    private readonly Dictionary<string, uint> _trie;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressDictionaryReader"/> class.
    /// </summary>
    /// <param name="stream">The stream containing dictionary data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the file signature is invalid.</exception>
    public AddressDictionaryReader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _canonicalStrings = new List<string>();
        _expansionValues = new List<AddressExpansionValue>();
        _trie = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        LoadFromStream(stream);
    }

    /// <summary>
    /// Tries to get expansions for a phrase in a specific language.
    /// </summary>
    /// <param name="phrase">The phrase to look up.</param>
    /// <param name="language">The ISO language code.</param>
    /// <param name="expansions">The expansion value if found.</param>
    /// <returns>True if expansions were found; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="phrase"/> or <paramref name="language"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public bool TryGetExpansions(string phrase, string language, out AddressExpansionValue? expansions)
    {
        ArgumentNullException.ThrowIfNull(phrase);
        ArgumentNullException.ThrowIfNull(language);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Build language-prefixed key (e.g., "en|street")
        var key = $"{language}|{phrase}";

        if (_trie.TryGetValue(key, out var index) && index < _expansionValues.Count)
        {
            expansions = _expansionValues[(int)index];
            return true;
        }

        expansions = null;
        return false;
    }

    private void LoadFromStream(Stream stream)
    {
        using var reader = new BigEndianBinaryReader(stream);

        // Validate signature
        var signature = reader.ReadUInt32();
        if (signature != DictionarySignature)
        {
            throw new InvalidDataException(
                $"Invalid address dictionary file signature. Expected 0x{DictionarySignature:X8}, got 0x{signature:X8}.");
        }

        // Read canonical strings
        var canonicalCount = reader.ReadUInt32();
        for (uint i = 0; i < canonicalCount; i++)
        {
            _canonicalStrings.Add(reader.ReadLengthPrefixedString());
        }

        // Read expansion values
        var valueCount = reader.ReadUInt32();
        for (uint i = 0; i < valueCount; i++)
        {
            var value = ReadExpansionValue(reader);
            _expansionValues.Add(value);
        }

        // Read trie
        ReadTrie(reader);
    }

    private AddressExpansionValue ReadExpansionValue(BigEndianBinaryReader reader)
    {
        // Read components mask
        var componentsMask = (AddressComponent)reader.ReadUInt32();

        // Read number of expansions
        var expansionCount = reader.ReadUInt32();
        var expansions = new List<AddressExpansion>();

        for (uint i = 0; i < expansionCount; i++)
        {
            // Read canonical index
            var canonicalIndex = (int)reader.ReadUInt32();
            var canonical = canonicalIndex >= 0 && canonicalIndex < _canonicalStrings.Count
                ? _canonicalStrings[canonicalIndex]
                : null;

            // Read language
            var language = reader.ReadLengthPrefixedString();

            // Read dictionary types
            var numDictionaries = reader.ReadUInt32();
            var dictionaryType = DictionaryType.Unknown;
            if (numDictionaries > 0)
            {
                dictionaryType = (DictionaryType)reader.ReadUInt16();

                // Skip remaining dictionary types (we only use the first one for now)
                for (uint j = 1; j < numDictionaries; j++)
                {
                    reader.ReadUInt16();
                }
            }

            // Read address components
            var components = (AddressComponent)reader.ReadUInt32();

            // Read separable flag
            var separable = reader.ReadByte() != 0;

            expansions.Add(new AddressExpansion
            {
                Canonical = canonical,
                Language = language,
                Components = components,
                DictionaryType = dictionaryType,
                IsSeparable = separable
            });
        }

        return new AddressExpansionValue(expansions);
    }

    private void ReadTrie(BigEndianBinaryReader reader)
    {
        // Read trie signature
        var trieSignature = reader.ReadUInt32();
        if (trieSignature != FileSignature.TrieSignature)
        {
            throw new InvalidDataException("Invalid trie signature in dictionary file.");
        }

        // Read trie entries (simplified format from Phase 3)
        var entryCount = reader.ReadUInt32();
        for (uint i = 0; i < entryCount; i++)
        {
            var key = reader.ReadLengthPrefixedString();
            var value = reader.ReadUInt32();
            _trie[key] = value;
        }
    }

    /// <summary>
    /// Disposes the reader.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _canonicalStrings.Clear();
            _expansionValues.Clear();
            _trie.Clear();
            _disposed = true;
        }
    }
}
