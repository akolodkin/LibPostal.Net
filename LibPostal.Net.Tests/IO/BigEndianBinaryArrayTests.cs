using FluentAssertions;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.IO;

/// <summary>
/// Tests for BigEndianBinaryReader and BigEndianBinaryWriter array methods.
/// These methods are needed for Phase 8 model loading (CSR matrices, etc.).
/// </summary>
public class BigEndianBinaryArrayTests
{
    #region Double Tests

    [Fact]
    public void ReadDouble_WithBigEndianBytes_ShouldReadCorrectly()
    {
        // Arrange - double 123.456 in big-endian IEEE 754 format
        var bytes = new byte[] { 0x40, 0x5E, 0xDD, 0x2F, 0x1A, 0x9F, 0xBE, 0x77 };
        using var stream = new MemoryStream(bytes);
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadDouble();

        // Assert
        result.Should().BeApproximately(123.456, 0.001);
    }

    [Fact]
    public void WriteDouble_ShouldWriteInBigEndianFormat()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Act
        writer.WriteDouble(123.456);

        // Assert
        var bytes = stream.ToArray();
        bytes.Should().HaveCount(8);
        // Verify big-endian format
        bytes.Should().Equal(0x40, 0x5E, 0xDD, 0x2F, 0x1A, 0x9F, 0xBE, 0x77);
    }

    [Fact]
    public void RoundTrip_Double_ShouldPreserveValue()
    {
        // Arrange
        var originalValue = 3.141592653589793;
        using var stream = new MemoryStream();

        // Act - write then read
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteDouble(originalValue);
        }

        stream.Position = 0;

        using (var reader = new BigEndianBinaryReader(stream))
        {
            var result = reader.ReadDouble();

            // Assert
            result.Should().Be(originalValue);
        }
    }

    #endregion

    #region Double Array Tests

    [Fact]
    public void ReadDoubleArray_WithValidCount_ShouldReadCorrectly()
    {
        // Arrange - array of doubles in big-endian format
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteDouble(1.0);
            writer.WriteDouble(2.0);
            writer.WriteDouble(3.0);
        }

        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadDoubleArray(3);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be(1.0);
        result[1].Should().Be(2.0);
        result[2].Should().Be(3.0);
    }

    [Fact]
    public void ReadDoubleArray_WithZeroCount_ShouldReturnEmptyArray()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadDoubleArray(0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReadDoubleArray_BeyondStreamEnd_ShouldThrow()
    {
        // Arrange - only 1 double available
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteDouble(1.0);
        }

        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        // Act - try to read 3 doubles
        Action act = () => reader.ReadDoubleArray(3);

        // Assert
        act.Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void WriteDoubleArray_ShouldWriteAllValues()
    {
        // Arrange
        var values = new[] { 1.1, 2.2, 3.3, 4.4 };
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Act
        writer.WriteDoubleArray(values);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);
        var result = reader.ReadDoubleArray(4);
        result.Should().Equal(values);
    }

    [Fact]
    public void WriteDoubleArray_WithEmptyArray_ShouldWriteNothing()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Act
        writer.WriteDoubleArray(Array.Empty<double>());

        // Assert
        stream.Length.Should().Be(0);
    }

    #endregion

    #region UInt32 Array Tests

    [Fact]
    public void ReadUInt32Array_WithValidCount_ShouldReadCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(0x12345678);
            writer.WriteUInt32(0xABCDEF00);
            writer.WriteUInt32(0xDEADBEEF);
        }

        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt32Array(3);

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be(0x12345678);
        result[1].Should().Be(0xABCDEF00);
        result[2].Should().Be(0xDEADBEEF);
    }

    [Fact]
    public void ReadUInt32Array_WithZeroCount_ShouldReturnEmptyArray()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt32Array(0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void WriteUInt32Array_ShouldWriteAllValues()
    {
        // Arrange
        var values = new uint[] { 100, 200, 300, 400 };
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Act
        writer.WriteUInt32Array(values);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);
        var result = reader.ReadUInt32Array(4);
        result.Should().Equal(values);
    }

    [Fact]
    public void WriteUInt32Array_WithEmptyArray_ShouldWriteNothing()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Act
        writer.WriteUInt32Array(Array.Empty<uint>());

        // Assert
        stream.Length.Should().Be(0);
    }

    #endregion

    #region UInt64 Array Tests

    [Fact]
    public void ReadUInt64Array_WithValidCount_ShouldReadCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt64(0x123456789ABCDEF0);
            writer.WriteUInt64(0xFEDCBA9876543210);
        }

        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt64Array(2);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be(0x123456789ABCDEF0);
        result[1].Should().Be(0xFEDCBA9876543210);
    }

    [Fact]
    public void ReadUInt64Array_WithZeroCount_ShouldReturnEmptyArray()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var reader = new BigEndianBinaryReader(stream);

        // Act
        var result = reader.ReadUInt64Array(0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void WriteUInt64Array_ShouldWriteAllValues()
    {
        // Arrange
        var values = new ulong[] { 1000UL, 2000UL, 3000UL };
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Act
        writer.WriteUInt64Array(values);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);
        var result = reader.ReadUInt64Array(3);
        result.Should().Equal(values);
    }

    [Fact]
    public void WriteUInt64Array_WithEmptyArray_ShouldWriteNothing()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new BigEndianBinaryWriter(stream);

        // Act
        writer.WriteUInt64Array(Array.Empty<ulong>());

        // Assert
        stream.Length.Should().Be(0);
    }

    #endregion

    #region Large Array Tests

    [Fact]
    public void ReadWriteDoubleArray_WithLargeArray_ShouldHandleCorrectly()
    {
        // Arrange - 10,000 element array
        var values = Enumerable.Range(0, 10_000).Select(i => (double)i * 0.1).ToArray();
        using var stream = new MemoryStream();

        // Act - write
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteDoubleArray(values);
        }

        // Act - read
        stream.Position = 0;
        using (var reader = new BigEndianBinaryReader(stream))
        {
            var result = reader.ReadDoubleArray(10_000);

            // Assert
            result.Should().HaveCount(10_000);
            result.Should().Equal(values);
        }
    }

    [Fact]
    public void ReadWriteUInt32Array_WithLargeArray_ShouldHandleCorrectly()
    {
        // Arrange - 10,000 element array
        var values = Enumerable.Range(0, 10_000).Select(i => (uint)i).ToArray();
        using var stream = new MemoryStream();

        // Act - write
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32Array(values);
        }

        // Act - read
        stream.Position = 0;
        using (var reader = new BigEndianBinaryReader(stream))
        {
            var result = reader.ReadUInt32Array(10_000);

            // Assert
            result.Should().HaveCount(10_000);
            result.Should().Equal(values);
        }
    }

    #endregion
}
