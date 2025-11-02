using FluentAssertions;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.IO;

/// <summary>
/// Tests for DictionaryLoader.
/// libpostal dictionary files are pipe-delimited UTF-8 text files.
/// Format: "original|abbrev1|abbrev2|..."
/// Example: "street|st|str"
/// </summary>
public class DictionaryLoaderTests
{
    [Fact]
    public void LoadFromStream_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => DictionaryLoader.LoadFromStream(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadFromStream_WithSingleEntry_ShouldLoadCorrectly()
    {
        // Arrange
        var content = "street|st|str";
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().HaveCount(3);
        result[0][0].Should().Be("street");
        result[0][1].Should().Be("st");
        result[0][2].Should().Be("str");
    }

    [Fact]
    public void LoadFromStream_WithMultipleEntries_ShouldLoadCorrectly()
    {
        // Arrange
        var content = """
            street|st|str
            avenue|ave|av
            boulevard|blvd|bld
            """;
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(3);

        result[0].Should().Equal("street", "st", "str");
        result[1].Should().Equal("avenue", "ave", "av");
        result[2].Should().Equal("boulevard", "blvd", "bld");
    }

    [Fact]
    public void LoadFromStream_WithEmptyLines_ShouldSkipEmptyLines()
    {
        // Arrange
        var content = """
            street|st|str

            avenue|ave|av

            boulevard|blvd|bld
            """;
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Equal("street", "st", "str");
        result[1].Should().Equal("avenue", "ave", "av");
        result[2].Should().Equal("boulevard", "blvd", "bld");
    }

    [Fact]
    public void LoadFromStream_WithCommentLines_ShouldSkipComments()
    {
        // Arrange
        var content = """
            # This is a comment
            street|st|str
            # Another comment
            avenue|ave|av
            """;
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Equal("street", "st", "str");
        result[1].Should().Equal("avenue", "ave", "av");
    }

    [Fact]
    public void LoadFromStream_WithWhitespaceLines_ShouldSkipWhitespace()
    {
        // Arrange
        var content = """
            street|st|str

            avenue|ave|av

            boulevard|blvd|bld
            """;
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void LoadFromStream_WithSingleValue_ShouldLoadAsSingleElement()
    {
        // Arrange - no pipes, just a single value
        var content = "street";
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().HaveCount(1);
        result[0][0].Should().Be("street");
    }

    [Fact]
    public void LoadFromStream_WithUnicodeContent_ShouldLoadCorrectly()
    {
        // Arrange - German, Chinese, Arabic
        var content = """
            straße|str
            大街|街
            شارع|ش
            """;
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Equal("straße", "str");
        result[1].Should().Equal("大街", "街");
        result[2].Should().Equal("شارع", "ش");
    }

    [Fact]
    public void LoadFromStream_WithEmptyFile_ShouldReturnEmptyList()
    {
        // Arrange
        var content = "";
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromStream_WithOnlyComments_ShouldReturnEmptyList()
    {
        // Arrange
        var content = """
            # Comment 1
            # Comment 2
            # Comment 3
            """;
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void LoadFromStream_WithTrailingPipe_ShouldNotIncludeEmptyString()
    {
        // Arrange - "street|st|" should only give ["street", "st"]
        var content = "street|st|";
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Equal("street", "st");
    }

    [Fact]
    public void LoadFromStream_WithConsecutivePipes_ShouldSkipEmptyValues()
    {
        // Arrange - "street||st" should only give ["street", "st"]
        var content = "street||st";
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Equal("street", "st");
    }

    [Fact]
    public void LoadFromStream_WithMixedLineEndings_ShouldHandleCorrectly()
    {
        // Arrange - mix of \n and \r\n
        var content = "street|st|str\navenue|ave|av\r\nboulevard|blvd|bld";
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Equal("street", "st", "str");
        result[1].Should().Equal("avenue", "ave", "av");
        result[2].Should().Equal("boulevard", "blvd", "bld");
    }

    [Fact]
    public void LoadFromStream_WithWhitespaceAroundValues_ShouldTrimValues()
    {
        // Arrange
        var content = " street | st | str ";
        using var stream = CreateStreamFromString(content);

        // Act
        var result = DictionaryLoader.LoadFromStream(stream);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Equal("street", "st", "str");
    }

    [Fact]
    public void LoadFromFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var path = "Q:\\NonExistent\\Path\\file.txt";

        // Act
        Action act = () => DictionaryLoader.LoadFromFile(path);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void LoadFromFile_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => DictionaryLoader.LoadFromFile(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static Stream CreateStreamFromString(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}
