using System.Buffers.Binary;
using System.Text;

namespace LibPostal.Net.IO;

/// <summary>
/// Binary writer that writes data in big-endian byte order.
/// Companion class to <see cref="BigEndianBinaryReader"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="BinaryWriter"/>, this class does NOT take ownership
/// of the underlying stream and will not dispose it.
/// </remarks>
public sealed class BigEndianBinaryWriter : IDisposable
{
    private readonly Stream _stream;
    private bool _disposed;
    private readonly byte[] _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BigEndianBinaryWriter"/> class.
    /// </summary>
    /// <param name="stream">The output stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    public BigEndianBinaryWriter(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _buffer = new byte[8]; // Max size needed for uint64
    }

    /// <summary>
    /// Writes a 4-byte unsigned integer in big-endian byte order.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteUInt32(uint value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        BinaryPrimitives.WriteUInt32BigEndian(_buffer, value);
        _stream.Write(_buffer, 0, 4);
    }

    /// <summary>
    /// Writes an 8-byte unsigned integer in big-endian byte order.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteUInt64(ulong value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        BinaryPrimitives.WriteUInt64BigEndian(_buffer, value);
        _stream.Write(_buffer, 0, 8);
    }

    /// <summary>
    /// Writes a 2-byte unsigned integer in big-endian byte order.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteUInt16(ushort value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        BinaryPrimitives.WriteUInt16BigEndian(_buffer, value);
        _stream.Write(_buffer, 0, 2);
    }

    /// <summary>
    /// Writes a single byte to the stream.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteByte(byte value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _stream.WriteByte(value);
    }

    /// <summary>
    /// Writes a byte array to the stream.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes a null-terminated UTF-8 string to the stream.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteNullTerminatedString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytes = Encoding.UTF8.GetBytes(value);
        _stream.Write(bytes, 0, bytes.Length);
        _stream.WriteByte(0); // null terminator
    }

    /// <summary>
    /// Writes a length-prefixed UTF-8 string to the stream.
    /// The length is stored as a 4-byte big-endian unsigned integer.
    /// </summary>
    /// <param name="value">The string to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteLengthPrefixedString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytes = Encoding.UTF8.GetBytes(value);
        WriteUInt32((uint)bytes.Length);
        _stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Disposes the writer. Does NOT dispose the underlying stream.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }
}
