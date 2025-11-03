using FluentAssertions;
using LibPostal.Net.Core;
using LibPostal.Net.IO;
using System.IO;
using Xunit;

namespace LibPostal.Net.Tests.Core;

/// <summary>
/// Tests for loading libpostal double-array trie format.
/// Based on libpostal's trie.c implementation (lines 979-1122)
/// </summary>
public class DoubleArrayTrieLoaderTests
{
    [Fact]
    public void LoadDoubleArrayTrie_WithValidFile_ShouldLoadCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Write signature
        writer.WriteUInt32(0xABABABAB);

        // Write alphabet size and alphabet
        writer.WriteUInt32(3); // Simple alphabet: space, a, b
        stream.WriteByte(0x20); // space
        stream.WriteByte(0x61); // a
        stream.WriteByte(0x62); // b

        // Write num keys
        writer.WriteUInt32(2); // "a" and "ab"

        // Write num nodes (simplified: just a few nodes)
        writer.WriteUInt32(5);

        // Write nodes (base, check pairs)
        // Node 0 (NULL): base=0, check=0
        writer.WriteUInt32(0);
        writer.WriteUInt32(0);

        // Node 1 (FREE_LIST): base=0, check=0
        writer.WriteUInt32(0);
        writer.WriteUInt32(0);

        // Node 2 (ROOT): base=3, check=0
        writer.WriteUInt32(3);
        writer.WriteUInt32(0);

        // Node 3: base=-1 (terminal for "a"), check=2
        writer.WriteUInt32(unchecked((uint)-1)); // Negative indicates terminal
        writer.WriteUInt32(2);

        // Node 4: base=-2 (terminal for "ab"), check=3
        writer.WriteUInt32(unchecked((uint)-2));
        writer.WriteUInt32(3);

        // Write num data nodes
        writer.WriteUInt32(2);

        // Write data nodes (tail, data pairs)
        // Data 0 for "a": tail=0 (empty suffix), data=100
        writer.WriteUInt32(0);
        writer.WriteUInt32(100);

        // Data 1 for "ab": tail=0 (empty suffix), data=200
        writer.WriteUInt32(0);
        writer.WriteUInt32(200);

        // Write tail length and tail (empty for this test)
        writer.WriteUInt32(1);
        stream.WriteByte(0); // NUL terminator

        stream.Position = 0;

        // Act
        var trie = DoubleArrayTrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        trie.Should().NotBeNull();
        trie.Count.Should().Be(2);
    }

    [Fact]
    public void LoadDoubleArrayTrie_WithInvalidSignature_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        writer.WriteUInt32(0xDEADBEEF); // Invalid signature

        stream.Position = 0;

        // Act & Assert
        Action act = () => DoubleArrayTrieLoader.LoadLibpostalTrie<uint>(stream);
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*signature*");
    }

    [Fact]
    public void LoadDoubleArrayTrie_WithRealVocabFile_ShouldLoadKeys()
    {
        // Arrange
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var vocabPath = Path.Combine(userProfile, ".libpostal", "address_parser", "address_parser_vocab.trie");

        if (!File.Exists(vocabPath))
        {
            // Skip if real file doesn't exist
            return;
        }

        // Act
        using var stream = File.OpenRead(vocabPath);
        var trie = DoubleArrayTrieLoader.LoadLibpostalTrie<uint>(stream);

        // Assert
        trie.Should().NotBeNull();
        trie.Count.Should().BeGreaterThan(0);

        // Common words should be loadable
        trie.ContainsKey("street").Should().BeTrue();
        trie.ContainsKey("avenue").Should().BeTrue();
        trie.ContainsKey("city").Should().BeTrue();
    }
}
