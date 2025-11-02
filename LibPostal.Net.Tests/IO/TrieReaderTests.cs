using FluentAssertions;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.IO;

/// <summary>
/// Tests for TrieReader.
/// libpostal uses a double-array trie binary format.
/// </summary>
public class TrieReaderTests
{
    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new TrieReader(null!);

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
        Action act = () => new TrieReader(stream);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*trie*");
    }

    [Fact]
    public void Constructor_WithValidSignature_ShouldNotThrow()
    {
        // Arrange - create a minimal valid trie file
        var bytes = CreateMinimalTrieFile();
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () =>
        {
            using var reader = new TrieReader(stream);
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TryGetValue_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var bytes = CreateMinimalTrieFile();
        using var stream = new MemoryStream(bytes);
        using var reader = new TrieReader(stream);

        // Act
        Action act = () => reader.TryGetValue(null!, out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryGetValue_WithEmptyKey_ShouldReturnFalse()
    {
        // Arrange
        var bytes = CreateMinimalTrieFile();
        using var stream = new MemoryStream(bytes);
        using var reader = new TrieReader(stream);

        // Act
        var result = reader.TryGetValue("", out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var bytes = CreateMinimalTrieFile();
        using var stream = new MemoryStream(bytes);
        using var reader = new TrieReader(stream);

        // Act
        var result = reader.TryGetValue("nonexistent", out var value);

        // Assert
        result.Should().BeFalse();
        value.Should().Be(0);
    }

    [Fact]
    public void TryGetValue_WithExistingKey_ShouldReturnTrue()
    {
        // Arrange - create a trie with a known key-value pair
        var bytes = CreateTrieFileWithSingleEntry("test", 42);
        using var stream = new MemoryStream(bytes);
        using var reader = new TrieReader(stream);

        // Act
        var result = reader.TryGetValue("test", out var value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_WithMultipleKeys_ShouldReturnCorrectValues()
    {
        // Arrange
        var entries = new Dictionary<string, uint>
        {
            ["apple"] = 1,
            ["banana"] = 2,
            ["cherry"] = 3
        };
        var bytes = CreateTrieFileWithEntries(entries);
        using var stream = new MemoryStream(bytes);
        using var reader = new TrieReader(stream);

        // Act & Assert
        reader.TryGetValue("apple", out var value1).Should().BeTrue();
        value1.Should().Be(1);

        reader.TryGetValue("banana", out var value2).Should().BeTrue();
        value2.Should().Be(2);

        reader.TryGetValue("cherry", out var value3).Should().BeTrue();
        value3.Should().Be(3);
    }

    [Fact]
    public void TryGetValue_WithPrefixKeys_ShouldDistinguishBetweenPrefixes()
    {
        // Arrange - keys that are prefixes of each other
        var entries = new Dictionary<string, uint>
        {
            ["test"] = 1,
            ["testing"] = 2,
            ["tester"] = 3
        };
        var bytes = CreateTrieFileWithEntries(entries);
        using var stream = new MemoryStream(bytes);
        using var reader = new TrieReader(stream);

        // Act & Assert
        reader.TryGetValue("test", out var value1).Should().BeTrue();
        value1.Should().Be(1);

        reader.TryGetValue("testing", out var value2).Should().BeTrue();
        value2.Should().Be(2);

        reader.TryGetValue("tester", out var value3).Should().BeTrue();
        value3.Should().Be(3);

        reader.TryGetValue("tes", out _).Should().BeFalse();
        reader.TryGetValue("tests", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetValue_WithUnicodeKey_ShouldWork()
    {
        // Arrange
        var entries = new Dictionary<string, uint>
        {
            ["café"] = 1,
            ["北京"] = 2,
            ["Москва"] = 3
        };
        var bytes = CreateTrieFileWithEntries(entries);
        using var stream = new MemoryStream(bytes);
        using var reader = new TrieReader(stream);

        // Act & Assert
        reader.TryGetValue("café", out var value1).Should().BeTrue();
        value1.Should().Be(1);

        reader.TryGetValue("北京", out var value2).Should().BeTrue();
        value2.Should().Be(2);

        reader.TryGetValue("Москва", out var value3).Should().BeTrue();
        value3.Should().Be(3);
    }

    [Fact]
    public void Dispose_ShouldNotDisposeUnderlyingStream()
    {
        // Arrange
        var bytes = CreateMinimalTrieFile();
        var stream = new MemoryStream(bytes);
        var reader = new TrieReader(stream);

        // Act
        reader.Dispose();

        // Assert - stream should still be usable
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public void TryGetValue_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var bytes = CreateMinimalTrieFile();
        using var stream = new MemoryStream(bytes);
        var reader = new TrieReader(stream);
        reader.Dispose();

        // Act
        Action act = () => reader.TryGetValue("test", out _);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid trie file with just the signature and empty data.
    /// </summary>
    private static byte[] CreateMinimalTrieFile()
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms);

        // Write signature
        writer.WriteUInt32(FileSignature.TrieSignature);

        // Write minimal structure
        writer.WriteUInt32(0); // alphabet size
        writer.WriteUInt32(0); // base array size
        writer.WriteUInt32(0); // check array size
        writer.WriteUInt32(0); // data array size
        writer.WriteUInt32(0); // tail array size

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a trie file with a single entry (for simple testing).
    /// </summary>
    private static byte[] CreateTrieFileWithSingleEntry(string key, uint value)
    {
        return CreateTrieFileWithEntries(new Dictionary<string, uint> { [key] = value });
    }

    /// <summary>
    /// Creates a trie file with multiple entries.
    /// This is a simplified format for testing - real libpostal format is more complex.
    /// </summary>
    private static byte[] CreateTrieFileWithEntries(Dictionary<string, uint> entries)
    {
        using var ms = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(ms);

        // Write signature
        writer.WriteUInt32(FileSignature.TrieSignature);

        // For simplicity, we'll use a simple key-value format for testing
        // Real libpostal uses double-array trie, but for testing we can use a simpler format

        // Write number of entries
        writer.WriteUInt32((uint)entries.Count);

        // Write each entry
        foreach (var (key, value) in entries)
        {
            writer.WriteLengthPrefixedString(key);
            writer.WriteUInt32(value);
        }

        return ms.ToArray();
    }

    #endregion
}
