using FluentAssertions;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.IO;

/// <summary>
/// Tests for FileSignature validation.
/// libpostal uses magic numbers (signatures) to validate file formats.
/// </summary>
public class FileSignatureTests
{
    [Fact]
    public void ValidateSignature_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => FileSignature.ValidateSignature(null!, 0x12345678, "test");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateSignature_WithNullFileType_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 0x12, 0x34, 0x56, 0x78 });

        // Act
        Action act = () => FileSignature.ValidateSignature(stream, 0x12345678, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateSignature_WithMatchingSignature_ShouldNotThrow()
    {
        // Arrange - 0x12345678 in big-endian format
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () => FileSignature.ValidateSignature(stream, 0x12345678, "test file");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateSignature_WithMismatchedSignature_ShouldThrowInvalidDataException()
    {
        // Arrange - 0xDEADBEEF but expecting 0x12345678
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () => FileSignature.ValidateSignature(stream, 0x12345678, "test file");

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*test file*")
            .WithMessage("*0x12345678*")
            .WithMessage("*0xDEADBEEF*");
    }

    [Fact]
    public void ValidateSignature_WithInsufficientData_ShouldThrowEndOfStreamException()
    {
        // Arrange - only 2 bytes
        var bytes = new byte[] { 0x12, 0x34 };
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () => FileSignature.ValidateSignature(stream, 0x12345678, "test file");

        // Assert
        act.Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void ValidateTrieSignature_WithValidSignature_ShouldNotThrow()
    {
        // Arrange - 0xABABABAB (libpostal trie signature)
        var bytes = new byte[] { 0xAB, 0xAB, 0xAB, 0xAB };
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () => FileSignature.ValidateTrieSignature(stream);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTrieSignature_WithInvalidSignature_ShouldThrowInvalidDataException()
    {
        // Arrange - wrong signature
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        using var stream = new MemoryStream(bytes);

        // Act
        Action act = () => FileSignature.ValidateTrieSignature(stream);

        // Assert
        act.Should().Throw<InvalidDataException>()
            .WithMessage("*trie*");
    }

    [Fact]
    public void ValidateSignature_ShouldResetStreamPosition()
    {
        // Arrange
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0xFF, 0xFF };
        using var stream = new MemoryStream(bytes);

        // Act
        FileSignature.ValidateSignature(stream, 0x12345678, "test file");

        // Assert - stream should be reset to position 0
        stream.Position.Should().Be(0);
    }

    [Fact]
    public void ValidateSignature_WithNonSeekableStream_ShouldNotResetPosition()
    {
        // Arrange - create a non-seekable stream wrapper
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var baseStream = new MemoryStream(bytes);
        using var nonSeekableStream = new NonSeekableStreamWrapper(baseStream);

        // Act
        FileSignature.ValidateSignature(nonSeekableStream, 0x12345678, "test file");

        // Assert - position should be after the signature
        nonSeekableStream.Position.Should().Be(4);
    }

    [Fact]
    public void TryValidateSignature_WithMatchingSignature_ShouldReturnTrue()
    {
        // Arrange
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var stream = new MemoryStream(bytes);

        // Act
        var result = FileSignature.TryValidateSignature(stream, 0x12345678);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TryValidateSignature_WithMismatchedSignature_ShouldReturnFalse()
    {
        // Arrange
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        using var stream = new MemoryStream(bytes);

        // Act
        var result = FileSignature.TryValidateSignature(stream, 0x12345678);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidateSignature_WithInsufficientData_ShouldReturnFalse()
    {
        // Arrange
        var bytes = new byte[] { 0x12, 0x34 };
        using var stream = new MemoryStream(bytes);

        // Act
        var result = FileSignature.TryValidateSignature(stream, 0x12345678);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryValidateSignature_ShouldResetStreamPosition()
    {
        // Arrange
        var bytes = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var stream = new MemoryStream(bytes);

        // Act
        FileSignature.TryValidateSignature(stream, 0x12345678);

        // Assert
        stream.Position.Should().Be(0);
    }

    /// <summary>
    /// Helper class to simulate a non-seekable stream.
    /// </summary>
    private class NonSeekableStreamWrapper : Stream
    {
        private readonly Stream _baseStream;

        public NonSeekableStreamWrapper(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => false; // Not seekable
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => _baseStream.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => _baseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
    }
}
