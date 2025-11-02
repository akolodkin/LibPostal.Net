using FluentAssertions;
using LibPostal.Net.Core;

namespace LibPostal.Net.Tests.Core;

/// <summary>
/// Tests for the Trie (double-array trie) implementation.
/// Ported from libpostal/test/test_trie.c
/// </summary>
public class TrieTests : IDisposable
{
    private Trie<uint>? _trie;

    [Fact]
    public void Trie_Constructor_ShouldCreateNonNullInstance()
    {
        // Act
        _trie = new Trie<uint>();

        // Assert
        _trie.Should().NotBeNull();
    }

    [Fact]
    public void Add_WithValidKey_ShouldReturnTrue()
    {
        // Arrange
        _trie = new Trie<uint>();
        var key = "st";
        uint data = 1;

        // Act
        bool added = _trie.Add(key, data);

        // Assert
        added.Should().BeTrue();
    }

    [Fact]
    public void TryGetData_AfterAdd_ShouldReturnTrueAndCorrectData()
    {
        // Arrange
        _trie = new Trie<uint>();
        var key = "st";
        uint expectedData = 1;
        _trie.Add(key, expectedData);

        // Act
        bool found = _trie.TryGetData(key, out uint actualData);

        // Assert
        found.Should().BeTrue();
        actualData.Should().Be(expectedData);
    }

    [Fact]
    public void TryGetData_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        _trie = new Trie<uint>();

        // Act
        bool found = _trie.TryGetData("nonexistent", out uint data);

        // Assert
        found.Should().BeFalse();
        data.Should().Be(default(uint));
    }

    [Fact]
    public void Add_MultipleKeys_ShouldAllBeRetrievable()
    {
        // Arrange
        _trie = new Trie<uint>();
        var testData = new Dictionary<string, uint>
        {
            { "st", 1 },
            { "street", 2 },
            { "st rt", 3 },
            { "st rd", 3 },
            { "state route", 4 },
            { "maine", 5 }
        };

        // Act & Assert - Add all keys
        foreach (var (key, data) in testData)
        {
            bool added = _trie.Add(key, data);
            added.Should().BeTrue($"key '{key}' should be added successfully");
        }

        // Assert - Retrieve all keys
        foreach (var (key, expectedData) in testData)
        {
            bool found = _trie.TryGetData(key, out uint actualData);
            found.Should().BeTrue($"key '{key}' should be found");
            actualData.Should().Be(expectedData, $"key '{key}' should have correct data");
        }
    }

    [Fact]
    public void Add_DuplicateKey_ShouldUpdateData()
    {
        // Arrange
        _trie = new Trie<uint>();
        var key = "test";
        uint initialData = 1;
        uint updatedData = 2;

        // Act
        _trie.Add(key, initialData);
        _trie.Add(key, updatedData);

        // Assert
        bool found = _trie.TryGetData(key, out uint actualData);
        found.Should().BeTrue();
        actualData.Should().Be(updatedData, "duplicate key should update the data");
    }

    [Fact]
    public void Add_EmptyString_ShouldThrowArgumentException()
    {
        // Arrange
        _trie = new Trie<uint>();

        // Act
        Action act = () => _trie.Add("", 1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*key*empty*");
    }

    [Fact]
    public void Add_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        _trie = new Trie<uint>();

        // Act
        Action act = () => _trie.Add(null!, 1);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Fact]
    public void TryGetData_NullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        _trie = new Trie<uint>();

        // Act
        Action act = () => _trie.TryGetData(null!, out _);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Theory]
    [InlineData("a", 1)]
    [InlineData("ab", 2)]
    [InlineData("abc", 3)]
    [InlineData("abcd", 4)]
    [InlineData("abcde", 5)]
    public void Add_VariousKeyLengths_ShouldWorkCorrectly(string key, uint data)
    {
        // Arrange
        _trie = new Trie<uint>();

        // Act
        bool added = _trie.Add(key, data);
        bool found = _trie.TryGetData(key, out uint actualData);

        // Assert
        added.Should().BeTrue();
        found.Should().BeTrue();
        actualData.Should().Be(data);
    }

    [Fact]
    public void Add_WithUnicodeCharacters_ShouldWorkCorrectly()
    {
        // Arrange
        _trie = new Trie<uint>();
        var testCases = new Dictionary<string, uint>
        {
            { "café", 1 },
            { "北京", 2 },
            { "Москва", 3 },
            { "العربية", 4 }
        };

        // Act & Assert
        foreach (var (key, data) in testCases)
        {
            bool added = _trie.Add(key, data);
            added.Should().BeTrue($"Unicode key '{key}' should be added");

            bool found = _trie.TryGetData(key, out uint actualData);
            found.Should().BeTrue($"Unicode key '{key}' should be found");
            actualData.Should().Be(data, $"Unicode key '{key}' should have correct data");
        }
    }

    [Fact]
    public void Add_KeysWithCommonPrefixes_ShouldAllBeDistinct()
    {
        // Arrange
        _trie = new Trie<uint>();
        var testData = new Dictionary<string, uint>
        {
            { "test", 1 },
            { "testing", 2 },
            { "tester", 3 },
            { "tested", 4 }
        };

        // Act
        foreach (var (key, data) in testData)
        {
            _trie.Add(key, data);
        }

        // Assert
        foreach (var (key, expectedData) in testData)
        {
            bool found = _trie.TryGetData(key, out uint actualData);
            found.Should().BeTrue($"key '{key}' should be found");
            actualData.Should().Be(expectedData, $"key '{key}' should have its own distinct data");
        }
    }

    [Fact]
    public void Count_AfterAddingKeys_ShouldReturnCorrectCount()
    {
        // Arrange
        _trie = new Trie<uint>();

        // Act & Assert - Initially empty
        _trie.Count.Should().Be(0);

        // Add keys
        _trie.Add("key1", 1);
        _trie.Count.Should().Be(1);

        _trie.Add("key2", 2);
        _trie.Count.Should().Be(2);

        _trie.Add("key3", 3);
        _trie.Count.Should().Be(3);

        // Adding duplicate shouldn't increase count
        _trie.Add("key1", 10);
        _trie.Count.Should().Be(3);
    }

    public void Dispose()
    {
        _trie?.Dispose();
    }
}
