using FluentAssertions;
using LibPostal.Net.Expansion;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for AddressDictionaryReader.
/// Based on libpostal's address_dictionary.c binary format.
/// </summary>
public class AddressDictionaryReaderTests
{
    private const uint DictionarySignature = 0xBABABABA;

    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AddressDictionaryReader(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithInvalidSignature_ShouldThrowInvalidDataException()
    {
        // Arrange - wrong signature
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () => new AddressDictionaryReader(stream);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*address dictionary*");
    }

    [Fact]
    public void Constructor_WithValidSignature_ShouldNotThrow()
    {
        // Arrange
        var bytes = CreateMinimalDictionaryFile();
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () =>
        {
            using var reader = new AddressDictionaryReader(stream);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TryGetExpansions_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var bytes = CreateMinimalDictionaryFile();
        using var stream = new MemoryStream(bytes);
        using var reader = new AddressDictionaryReader(stream);

        // Act
        Action act = () => reader.TryGetExpansions(null!, "en", out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetExpansions_WithNullLanguage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var bytes = CreateMinimalDictionaryFile();
        using var stream = new MemoryStream(bytes);
        using var reader = new AddressDictionaryReader(stream);

        // Act
        Action act = () => reader.TryGetExpansions("test", null!, out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetExpansions_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var bytes = CreateMinimalDictionaryFile();
        using var stream = new MemoryStream(bytes);
        using var reader = new AddressDictionaryReader(stream);

        // Act
        var result = reader.TryGetExpansions("nonexistent", "en", out var expansions);

        // Assert
        result.Should().BeFalse();
        expansions.Should().BeNull();
    }

    [Fact]
    public void TryGetExpansions_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var entries = new Dictionary<string, AddressExpansionValue>
        {
            ["en|st"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "street",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.StreetType,
                    IsSeparable = true
                }
            })
        };

        var bytes = CreateDictionaryFileWithEntries(entries);
        using var stream = new MemoryStream(bytes);
        using var reader = new AddressDictionaryReader(stream);

        // Act
        var result = reader.TryGetExpansions("st", "en", out var expansions);

        // Assert
        result.Should().BeTrue();
        expansions.Should().NotBeNull();
        expansions!.Count.Should().Be(1);
        expansions.Expansions[0].Canonical.Should().Be("street");
    }

    [Fact]
    public void TryGetExpansions_WithMultipleLanguages_ShouldFindCorrectLanguage()
    {
        // Arrange
        var entries = new Dictionary<string, AddressExpansionValue>
        {
            ["en|st"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "street",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.StreetType,
                    IsSeparable = true
                }
            }),
            ["fr|st"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "saint",
                    Language = "fr",
                    Components = AddressComponent.Name,
                    DictionaryType = DictionaryType.PlaceName,
                    IsSeparable = true
                }
            })
        };

        var bytes = CreateDictionaryFileWithEntries(entries);
        using var stream = new MemoryStream(bytes);
        using var reader = new AddressDictionaryReader(stream);

        // Act
        var resultEn = reader.TryGetExpansions("st", "en", out var expansionsEn);
        var resultFr = reader.TryGetExpansions("st", "fr", out var expansionsFr);

        // Assert
        resultEn.Should().BeTrue();
        expansionsEn!.Expansions[0].Canonical.Should().Be("street");

        resultFr.Should().BeTrue();
        expansionsFr!.Expansions[0].Canonical.Should().Be("saint");
    }

    [Fact]
    public void TryGetExpansions_WithMultipleExpansions_ShouldReturnAll()
    {
        // Arrange - "st" can expand to both "street" and "saint"
        var entries = new Dictionary<string, AddressExpansionValue>
        {
            ["en|st"] = new AddressExpansionValue(new[]
            {
                new AddressExpansion
                {
                    Canonical = "street",
                    Language = "en",
                    Components = AddressComponent.Street,
                    DictionaryType = DictionaryType.StreetType,
                    IsSeparable = true
                },
                new AddressExpansion
                {
                    Canonical = "saint",
                    Language = "en",
                    Components = AddressComponent.Name,
                    DictionaryType = DictionaryType.PlaceName,
                    IsSeparable = true
                }
            })
        };

        var bytes = CreateDictionaryFileWithEntries(entries);
        using var stream = new MemoryStream(bytes);
        using var reader = new AddressDictionaryReader(stream);

        // Act
        var result = reader.TryGetExpansions("st", "en", out var expansions);

        // Assert
        result.Should().BeTrue();
        expansions!.Count.Should().Be(2);
        expansions.Expansions.Should().Contain(e => e.Canonical == "street");
        expansions.Expansions.Should().Contain(e => e.Canonical == "saint");
    }

    [Fact]
    public void Dispose_ShouldNotDisposeUnderlyingStream()
    {
        // Arrange
        var bytes = CreateMinimalDictionaryFile();
        var stream = new MemoryStream(bytes);
        var reader = new AddressDictionaryReader(stream);

        // Act
        reader.Dispose();

        // Assert - stream should still be usable
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public void TryGetExpansions_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var bytes = CreateMinimalDictionaryFile();
        using var stream = new MemoryStream(bytes);
        var reader = new AddressDictionaryReader(stream);
        reader.Dispose();

        // Act
        Action act = () => reader.TryGetExpansions("test", "en", out _);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    #region Helper Methods

    private static byte[] CreateMinimalDictionaryFile()
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms);

        // Write signature
        writer.WriteUInt32(DictionarySignature);

        // Write empty canonical strings
        writer.WriteUInt32(0); // Length

        // Write zero expansions
        writer.WriteUInt32(0); // Count

        // Write minimal trie (just signature)
        writer.WriteUInt32(FileSignature.TrieSignature);
        writer.WriteUInt32(0); // Entry count

        return ms.ToArray();
    }

    private static byte[] CreateDictionaryFileWithEntries(Dictionary<string, AddressExpansionValue> entries)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms);

        // Write signature
        writer.WriteUInt32(DictionarySignature);

        // Collect all canonical strings
        var canonicals = new List<string>();
        foreach (var entry in entries.Values)
        {
            foreach (var expansion in entry.Expansions)
            {
                if (expansion.Canonical != null && !canonicals.Contains(expansion.Canonical))
                {
                    canonicals.Add(expansion.Canonical);
                }
            }
        }

        // Write canonical strings array
        writer.WriteUInt32((uint)canonicals.Count);
        foreach (var canonical in canonicals)
        {
            writer.WriteLengthPrefixedString(canonical);
        }

        // Write expansion values
        writer.WriteUInt32((uint)entries.Count);
        foreach (var entry in entries.Values)
        {
            // Write components mask
            var componentsMask = entry.Expansions.First().Components;
            writer.WriteUInt32((uint)componentsMask);

            // Write number of expansions
            writer.WriteUInt32((uint)entry.Count);

            foreach (var expansion in entry.Expansions)
            {
                // Write canonical index
                var canonicalIndex = expansion.Canonical != null
                    ? canonicals.IndexOf(expansion.Canonical)
                    : -1;
                writer.WriteUInt32((uint)canonicalIndex);

                // Write language
                writer.WriteLengthPrefixedString(expansion.Language);

                // Write dictionary type (as single-item array)
                writer.WriteUInt32(1); // num_dictionaries
                writer.WriteUInt16((ushort)expansion.DictionaryType);

                // Write address components
                writer.WriteUInt32((uint)expansion.Components);

                // Write separable flag
                writer.WriteByte((byte)(expansion.IsSeparable ? 1 : 0));
            }
        }

        // Write simplified trie structure (using Phase 3 format)
        writer.WriteUInt32(FileSignature.TrieSignature);
        writer.WriteUInt32((uint)entries.Count);

        uint index = 0;
        foreach (var (key, _) in entries)
        {
            writer.WriteLengthPrefixedString(key);
            writer.WriteUInt32(index); // Value (index into expansions array)
            index++;
        }

        return ms.ToArray();
    }

    #endregion
}
