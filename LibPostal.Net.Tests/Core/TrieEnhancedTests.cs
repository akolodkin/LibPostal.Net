using FluentAssertions;
using LibPostal.Net.Core;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.Core;

/// <summary>
/// Tests for enhanced Trie functionality needed for Phase 7.
/// Tests persistence, prefix matching, and performance optimizations.
/// </summary>
public class TrieEnhancedTests
{
    [Fact]
    public void Save_WithEntries_ShouldSerializeToStream()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);
        trie.Add("avenue", 2);
        trie.Add("boulevard", 3);

        using var stream = new MemoryStream();

        // Act
        trie.Save(stream);

        // Assert
        stream.Length.Should().BeGreaterThan(0);
        stream.Position.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Load_AfterSave_ShouldRestoreEntries()
    {
        // Arrange
        var original = new Trie<uint>();
        original.Add("street", 1);
        original.Add("avenue", 2);
        original.Add("boulevard", 3);

        using var stream = new MemoryStream();
        original.Save(stream);
        stream.Position = 0;

        // Act
        var loaded = Trie<uint>.Load(stream);

        // Assert
        loaded.Count.Should().Be(3);
        loaded.TryGetData("street", out var value1).Should().BeTrue();
        value1.Should().Be(1);
        loaded.TryGetData("avenue", out var value2).Should().BeTrue();
        value2.Should().Be(2);
        loaded.TryGetData("boulevard", out var value3).Should().BeTrue();
        value3.Should().Be(3);
    }

    [Fact]
    public void GetKeysWithPrefix_WithMatchingPrefix_ShouldReturnKeys()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);
        trie.Add("st", 2);
        trie.Add("avenue", 3);

        // Act
        var results = trie.GetKeysWithPrefix("st");

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(kv => kv.key == "street" && kv.data == 1);
        results.Should().Contain(kv => kv.key == "st" && kv.data == 2);
    }

    [Fact]
    public void GetKeysWithPrefix_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);

        // Act
        var results = trie.GetKeysWithPrefix("avenue");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void GetKeysWithPrefix_WithEmptyPrefix_ShouldReturnAll()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);
        trie.Add("avenue", 2);

        // Act
        var results = trie.GetKeysWithPrefix("");

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public void ContainsKey_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);

        // Act
        var result = trie.ContainsKey("street");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_WithNonExistingKey_ShouldReturnFalse()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);

        // Act
        var result = trie.ContainsKey("avenue");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAllKeys_ShouldReturnAllStoredKeys()
    {
        // Arrange
        var trie = new Trie<uint>();
        trie.Add("street", 1);
        trie.Add("avenue", 2);
        trie.Add("boulevard", 3);

        // Act
        var keys = trie.GetAllKeys();

        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain("street");
        keys.Should().Contain("avenue");
        keys.Should().Contain("boulevard");
    }

    [Fact]
    public void Save_WithEmptyTrie_ShouldSerialize()
    {
        // Arrange
        var trie = new Trie<uint>();
        using var stream = new MemoryStream();

        // Act
        trie.Save(stream);

        // Assert
        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Load_WithCorruptedStream_ShouldThrowInvalidDataException()
    {
        // Arrange
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () => Trie<uint>.Load(stream);

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void Save_WithLargeTrie_ShouldHandleEfficiently()
    {
        // Arrange - simulate vocabulary trie with 10,000 entries
        var trie = new Trie<uint>();
        for (uint i = 0; i < 10000; i++)
        {
            trie.Add($"word{i}", i);
        }

        using var stream = new MemoryStream();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        trie.Save(stream);
        sw.Stop();

        // Assert
        trie.Count.Should().Be(10000);
        sw.ElapsedMilliseconds.Should().BeLessThan(1000); // < 1 second
    }

    [Fact]
    public void Load_WithLargeTrie_ShouldHandleEfficiently()
    {
        // Arrange
        var original = new Trie<uint>();
        for (uint i = 0; i < 10000; i++)
        {
            original.Add($"word{i}", i);
        }

        using var stream = new MemoryStream();
        original.Save(stream);
        stream.Position = 0;

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var loaded = Trie<uint>.Load(stream);
        sw.Stop();

        // Assert
        loaded.Count.Should().Be(10000);
        sw.ElapsedMilliseconds.Should().BeLessThan(500); // < 0.5 second
    }

    [Fact]
    public void GetKeysWithPrefix_WithCommonPrefix_ShouldBeEfficient()
    {
        // Arrange
        var trie = new Trie<uint>();
        for (uint i = 0; i < 1000; i++)
        {
            trie.Add($"street{i}", i);
        }
        for (uint i = 0; i < 1000; i++)
        {
            trie.Add($"avenue{i}", i + 1000);
        }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = trie.GetKeysWithPrefix("street");
        sw.Stop();

        // Assert
        results.Should().HaveCount(1000);
        sw.ElapsedMilliseconds.Should().BeLessThan(100); // Should be fast
    }

    [Fact]
    public void TryGetData_AfterLoadFromStream_ShouldWork()
    {
        // Arrange
        var original = new Trie<uint>();
        original.Add("test", 42);

        using var stream = new MemoryStream();
        original.Save(stream);
        stream.Position = 0;

        var loaded = Trie<uint>.Load(stream);

        // Act
        var found = loaded.TryGetData("test", out var value);

        // Assert
        found.Should().BeTrue();
        value.Should().Be(42);
    }
}
