using FluentAssertions;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.IO;

/// <summary>
/// Tests for BigEndianBinaryReader.
/// libpostal uses big-endian binary format for data files.
/// </summary>
public class BigEndianBinaryReaderTests
{
    [Fact]
    public void Constructor_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new BigEndianBinaryReader(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadUInt32_WithBigEndianBytes_ShouldReadCorrectly()
    {
        // Arrange - 0x12345678 in big-endian format
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt32();

        // Assert
        result.Should().Be(0x12345678);
    }

    [Fact]
    public void ReadUInt32_WithLittleEndianBytes_ShouldNotMatchNativeRead()
    {
        // Arrange - 0x12345678 in little-endian format (native .NET BinaryReader)
        var bytes = new byte[] { 0x78, 0x56, 0x34, 0x12 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt32();

        // Assert - should be different from little-endian interpretation
        result.Should().NotBe(0x12345678);
        result.Should().Be(0x78563412); // big-endian interpretation
    }

    [Fact]
    public void ReadUInt64_WithBigEndianBytes_ShouldReadCorrectly()
    {
        // Arrange - 0x123456789ABCDEF0 in big-endian format
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt64();

        // Assert
        result.Should().Be(0x123456789ABCDEF0);
    }

    [Fact]
    public void ReadUInt16_WithBigEndianBytes_ShouldReadCorrectly()
    {
        // Arrange - 0x1234 in big-endian format
        var bytes = new byte[] { 0x12, 0x34 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt16();

        // Assert
        result.Should().Be(0x1234);
    }

    [Fact]
    public void ReadByte_ShouldReadSingleByte()
    {
        // Arrange
        var bytes = new byte[] { 0xAB };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadByte();

        // Assert
        result.Should().Be(0xAB);
    }

    [Fact]
    public void ReadBytes_WithCount_ShouldReadCorrectNumberOfBytes()
    {
        // Arrange
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadBytes(3);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Equal(0x01, 0x02, 0x03);
    }

    [Fact]
    public void ReadBytes_BeyondStreamEnd_ShouldThrowEndOfStreamException()
    {
        // Arrange
        var bytes = new byte[] { 0x01, 0x02 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        Action act = () => reader.ReadBytes(5);

        // Assert
        act.Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void ReadNullTerminatedString_WithAsciiString_ShouldReadCorrectly()
    {
        // Arrange - "hello\0" in UTF-8
        var bytes = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F, 0x00 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadNullTerminatedString();

        // Assert
        result.Should().Be("hello");
    }

    [Fact]
    public void ReadNullTerminatedString_WithUnicodeString_ShouldReadCorrectly()
    {
        // Arrange - "café\0" in UTF-8 (é = 0xC3 0xA9)
        var bytes = new byte[] { 0x63, 0x61, 0x66, 0xC3, 0xA9, 0x00 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadNullTerminatedString();

        // Assert
        result.Should().Be("café");
    }

    [Fact]
    public void ReadNullTerminatedString_WithEmptyString_ShouldReturnEmpty()
    {
        // Arrange - just null terminator
        var bytes = new byte[] { 0x00 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadNullTerminatedString();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReadNullTerminatedString_WithoutNullTerminator_ShouldThrowEndOfStreamException()
    {
        // Arrange - no null terminator
        var bytes = new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        Action act = () => reader.ReadNullTerminatedString();

        // Assert
        act.Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void ReadLengthPrefixedString_WithValidString_ShouldReadCorrectly()
    {
        // Arrange - length (5) + "hello" in UTF-8
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x05, 0x68, 0x65, 0x6C, 0x6C, 0x6F };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadLengthPrefixedString();

        // Assert
        result.Should().Be("hello");
    }

    [Fact]
    public void ReadLengthPrefixedString_WithUnicodeString_ShouldReadCorrectly()
    {
        // Arrange - length (5) + "café" in UTF-8 (5 bytes: c,a,f,é)
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x05, 0x63, 0x61, 0x66, 0xC3, 0xA9 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadLengthPrefixedString();

        // Assert
        result.Should().Be("café");
    }

    [Fact]
    public void ReadLengthPrefixedString_WithZeroLength_ShouldReturnEmpty()
    {
        // Arrange - length (0)
        var bytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadLengthPrefixedString();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReadMultipleValues_InSequence_ShouldReadCorrectly()
    {
        // Arrange - uint32, uint16, byte, uint64
        var bytes = new byte[]
        {
            0x12, 0x34, 0x56, 0x78,           // uint32
            0xAB, 0xCD,                        // uint16
            0xEF,                              // byte
            0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88  // uint64
        };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var val32 = reader.ReadUInt32();
        var val16 = reader.ReadUInt16();
        var val8 = reader.ReadByte();
        var val64 = reader.ReadUInt64();

        // Assert
        val32.Should().Be(0x12345678);
        val16.Should().Be(0xABCD);
        val8.Should().Be(0xEF);
        val64.Should().Be(0x1122334455667788);
    }

    [Fact]
    public void Dispose_ShouldNotDisposeUnderlyingStream()
    {
        // Arrange - BigEndianBinaryReader should NOT own the stream
        var stream = new MemoryStream(new byte[] { 0x01, 0x02 });
        var reader = new BigEndianBinaryReader(stream);

        // Act
        reader.Dispose();

        // Assert - stream should still be usable
        stream.CanRead.Should().BeTrue();
        stream.Position = 0;
        stream.ReadByte().Should().Be(0x01);
    }

    [Fact]
    public void ReadUInt32_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04 });
        var reader = new BigEndianBinaryReader(stream);
        reader.Dispose();

        // Act
        Action act = () => reader.ReadUInt32();

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ReadFileSignature_WithValidSignature_ShouldReadCorrectly()
    {
        // Arrange - libpostal trie signature 0xABABABAB
        var bytes = new byte[] { 0xAB, 0xAB, 0xAB, 0xAB };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var signature = reader.ReadUInt32();

        // Assert
        signature.Should().Be(0xABABABAB);
    }
}
