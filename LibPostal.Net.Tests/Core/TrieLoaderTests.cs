using FluentAssertions;
using LibPostal.Net.Core;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.Core;

/// <summary>
/// Tests for loading libpostal double-array trie format.
/// Note: This is a simplified conversion layer that reads libpostal's
/// double-array format and converts to our dictionary-based Trie.
/// </summary>
public class TrieLoaderTests
{
    [Fact]
    public void LoadLibpostalTrie_WithSimpleFormat_ShouldLoadCorrectly()
    {
        // Arrange - create mock simplified trie data
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            // Simplified mock format (not full double-array yet)
            writer.WriteUInt32(FileSignature.TrieSignature); // 0xABABABAB
            writer.WriteUInt32(2); // num_keys

            // Key 1: "test" -> 1
            writer.WriteLengthPrefixedString("test");
            writer.WriteUInt32(1);

            // Key 2: "hello" -> 2
            writer.WriteLengthPrefixedString("hello");
            writer.WriteUInt32(2);
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        trie.Should().NotBeNull();
        trie.Count.Should().Be(2);
        trie.TryGetData("test", out var value1).Should().BeTrue();
        value1.Should().Be(1);
        trie.TryGetData("hello", out var value2).Should().BeTrue();
        value2.Should().Be(2);
    }

    [Fact]
    public void LoadLibpostalTrie_WithEmptyTrie_ShouldReturnEmptyTrie()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(0); // num_keys
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        trie.Count.Should().Be(0);
    }

    [Fact]
    public void LoadLibpostalTrie_WithInvalidSignature_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(0xDEADBEEF); // wrong signature
        }

        stream.Position = 0;

        // Act
        Action act = () => TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void LoadLibpostalTrie_WithNullStream_ShouldThrow()
    {
        // Act
        Action act = () => TrieLoader.LoadLibpostalTrie<uint>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadLibpostalTrie_WithMultipleKeys_ShouldLoadAll()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(5); // num_keys

            var entries = new[]
            {
                ("street", 10u),
                ("avenue", 20u),
                ("road", 30u),
                ("boulevard", 40u),
                ("lane", 50u)
            };

            foreach (var (key, value) in entries)
            {
                writer.WriteLengthPrefixedString(key);
                writer.WriteUInt32(value);
            }
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        trie.Count.Should().Be(5);
        trie.TryGetData("street", out var v1).Should().BeTrue();
        v1.Should().Be(10);
        trie.TryGetData("boulevard", out var v2).Should().BeTrue();
        v2.Should().Be(40);
    }

    [Fact]
    public void LoadLibpostalTrie_WithUnicodeKeys_ShouldHandleCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(3);

            writer.WriteLengthPrefixedString("café");
            writer.WriteUInt32(1);

            writer.WriteLengthPrefixedString("naïve");
            writer.WriteUInt32(2);

            writer.WriteLengthPrefixedString("日本");
            writer.WriteUInt32(3);
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        trie.Count.Should().Be(3);
        trie.ContainsKey("café").Should().BeTrue();
        trie.ContainsKey("naïve").Should().BeTrue();
        trie.ContainsKey("日本").Should().BeTrue();
    }

    [Fact]
    public void LoadLibpostalTrie_WithLargeData_ShouldHandleCorrectly()
    {
        // Arrange - 1000 entries
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(1000);

            for (uint i = 0; i < 1000; i++)
            {
                writer.WriteLengthPrefixedString($"key_{i}");
                writer.WriteUInt32(i);
            }
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        trie.Count.Should().Be(1000);
        trie.TryGetData("key_0", out var v0).Should().BeTrue();
        v0.Should().Be(0);
        trie.TryGetData("key_999", out var v999).Should().BeTrue();
        v999.Should().Be(999);
    }

    [Fact]
    public void LoadLibpostalTrie_WithUlongData_ShouldWork()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(2);

            writer.WriteLengthPrefixedString("first");
            writer.WriteUInt64(0x123456789ABCDEF0);

            writer.WriteLengthPrefixedString("second");
            writer.WriteUInt64(0xFEDCBA9876543210);
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<ulong>(stream);

        // Assert
        trie.Count.Should().Be(2);
        trie.TryGetData("first", out var v1).Should().BeTrue();
        v1.Should().Be(0x123456789ABCDEF0);
        trie.TryGetData("second", out var v2).Should().BeTrue();
        v2.Should().Be(0xFEDCBA9876543210);
    }

    [Fact]
    public void LoadLibpostalTrie_RoundTrip_ShouldPreserveData()
    {
        // Arrange - create a trie, save it, then load it back
        var original = new Trie<uint>();
        original.Add("apple", 1);
        original.Add("banana", 2);
        original.Add("cherry", 3);

        using var stream = new MemoryStream();
        original.Save(stream);
        stream.Position = 0;

        // Act - load using TrieLoader (should handle our format too)
        var loaded = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        loaded.Count.Should().Be(original.Count);
        loaded.TryGetData("apple", out var v1).Should().BeTrue();
        v1.Should().Be(1);
        loaded.TryGetData("banana", out var v2).Should().BeTrue();
        v2.Should().Be(2);
        loaded.TryGetData("cherry", out var v3).Should().BeTrue();
        v3.Should().Be(3);
    }

    [Fact]
    public void LoadLibpostalTrie_WithDuplicateKeys_ShouldKeepLastValue()
    {
        // Arrange - simulate file with duplicate keys
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(3);

            writer.WriteLengthPrefixedString("test");
            writer.WriteUInt32(1);

            writer.WriteLengthPrefixedString("test"); // duplicate
            writer.WriteUInt32(2);

            writer.WriteLengthPrefixedString("other");
            writer.WriteUInt32(3);
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert - should have 2 unique keys, with last value for "test"
        trie.Count.Should().Be(2);
        trie.TryGetData("test", out var value).Should().BeTrue();
        value.Should().Be(2); // last value wins
    }

    [Fact]
    public void LoadLibpostalTrie_WithPrefixSearch_ShouldWork()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(4);

            writer.WriteLengthPrefixedString("street");
            writer.WriteUInt32(1);

            writer.WriteLengthPrefixedString("str");
            writer.WriteUInt32(2);

            writer.WriteLengthPrefixedString("strong");
            writer.WriteUInt32(3);

            writer.WriteLengthPrefixedString("road");
            writer.WriteUInt32(4);
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        var prefixMatches = trie.GetKeysWithPrefix("str").ToList();
        prefixMatches.Should().HaveCount(3);
        prefixMatches.Select(x => x.key).Should().Contain(new[] { "str", "street", "strong" });
    }

    [Fact]
    public void LoadLibpostalTrie_WithEmptyKey_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(1);

            writer.WriteLengthPrefixedString(""); // empty key
            writer.WriteUInt32(1);
        }

        stream.Position = 0;

        // Act
        Action act = () => TrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LoadLibpostalTrie_WithIntData_ShouldWork()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(2);

            writer.WriteLengthPrefixedString("positive");
            writer.WriteUInt32(42); // will be interpreted as int

            writer.WriteLengthPrefixedString("max");
            writer.WriteUInt32(int.MaxValue);
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<int>(stream);

        // Assert
        trie.Count.Should().Be(2);
        trie.TryGetData("positive", out var v1).Should().BeTrue();
        v1.Should().Be(42);
    }

    [Fact]
    public void LoadLibpostalTrie_CompareWithStandardLoad_ShouldBehaveIdentically()
    {
        // Arrange - same data in stream
        var data = new[]
        {
            ("alpha", 10u),
            ("beta", 20u),
            ("gamma", 30u)
        };

        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32((uint)data.Length);

            foreach (var (key, value) in data)
            {
                writer.WriteLengthPrefixedString(key);
                writer.WriteUInt32(value);
            }
        }

        // Act
        stream.Position = 0;
        var trieViaLoader = TrieLoader.LoadLibpostalTrie<uint>(stream);

        stream.Position = 0;
        var trieViaStandard = Trie<uint>.Load(stream);

        // Assert - both should produce identical results
        trieViaLoader.Count.Should().Be(trieViaStandard.Count);
        foreach (var (key, _) in data)
        {
            trieViaLoader.TryGetData(key, out var v1).Should().BeTrue();
            trieViaStandard.TryGetData(key, out var v2).Should().BeTrue();
            v1.Should().Be(v2);
        }
    }

    [Fact]
    public void LoadLibpostalTrie_WithGetAllKeys_ShouldReturnAllKeys()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(FileSignature.TrieSignature);
            writer.WriteUInt32(3);

            writer.WriteLengthPrefixedString("one");
            writer.WriteUInt32(1);

            writer.WriteLengthPrefixedString("two");
            writer.WriteUInt32(2);

            writer.WriteLengthPrefixedString("three");
            writer.WriteUInt32(3);
        }

        stream.Position = 0;

        // Act
        var trie = TrieLoader.LoadLibpostalTrie<uint>(stream);
        var allKeys = trie.GetAllKeys().ToList();

        // Assert
        allKeys.Should().HaveCount(3);
        allKeys.Should().Contain(new[] { "one", "two", "three" });
    }
}
