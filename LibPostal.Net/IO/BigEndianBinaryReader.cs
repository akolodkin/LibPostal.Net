using System.Buffers.Binary;
using System.Text;

namespace LibPostal.Net.IO;

/// <summary>
/// Binary reader that reads data in big-endian byte order.
/// libpostal uses big-endian format for all binary data files.
/// </summary>
/// <remarks>
/// Unlike <see cref="BinaryReader"/>, this class does NOT take ownership
/// of the underlying stream and will not dispose it.
/// </remarks>
public sealed class BigEndianBinaryReader : IDisposable
{
    private readonly Stream _stream;
    private bool _disposed;
    private readonly byte[] _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="BigEndianBinaryReader"/> class.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    public BigEndianBinaryReader(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _buffer = new byte[8]; // Max size needed for uint64
    }

    /// <summary>
    /// Reads a 4-byte unsigned integer in big-endian byte order.
    /// </summary>
    /// <returns>The value read from the stream.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public uint ReadUInt32()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytesRead = _stream.Read(_buffer, 0, 4);
        if (bytesRead != 4)
            throw new EndOfStreamException("Unable to read 4 bytes from stream.");

        return BinaryPrimitives.ReadUInt32BigEndian(_buffer);
    }

    /// <summary>
    /// Reads an 8-byte unsigned integer in big-endian byte order.
    /// </summary>
    /// <returns>The value read from the stream.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public ulong ReadUInt64()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytesRead = _stream.Read(_buffer, 0, 8);
        if (bytesRead != 8)
            throw new EndOfStreamException("Unable to read 8 bytes from stream.");

        return BinaryPrimitives.ReadUInt64BigEndian(_buffer);
    }

    /// <summary>
    /// Reads a 2-byte unsigned integer in big-endian byte order.
    /// </summary>
    /// <returns>The value read from the stream.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public ushort ReadUInt16()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytesRead = _stream.Read(_buffer, 0, 2);
        if (bytesRead != 2)
            throw new EndOfStreamException("Unable to read 2 bytes from stream.");

        return BinaryPrimitives.ReadUInt16BigEndian(_buffer);
    }

    /// <summary>
    /// Reads an 8-byte double in big-endian byte order (IEEE 754 format).
    /// </summary>
    /// <returns>The value read from the stream.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public double ReadDouble()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytesRead = _stream.Read(_buffer, 0, 8);
        if (bytesRead != 8)
            throw new EndOfStreamException("Unable to read 8 bytes from stream.");

        return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(_buffer));
    }

    /// <summary>
    /// Reads a single byte from the stream.
    /// </summary>
    /// <returns>The byte read from the stream.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public byte ReadByte()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var value = _stream.ReadByte();
        if (value == -1)
            throw new EndOfStreamException("Unable to read byte from stream.");

        return (byte)value;
    }

    /// <summary>
    /// Reads the specified number of bytes from the stream.
    /// </summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>A byte array containing the data read from the stream.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached before reading all bytes.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public byte[] ReadBytes(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return Array.Empty<byte>();

        var buffer = new byte[count];
        var bytesRead = _stream.Read(buffer, 0, count);

        if (bytesRead != count)
            throw new EndOfStreamException($"Unable to read {count} bytes from stream. Only {bytesRead} bytes available.");

        return buffer;
    }

    /// <summary>
    /// Reads a null-terminated UTF-8 string from the stream.
    /// </summary>
    /// <returns>The string read from the stream, not including the null terminator.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached before finding null terminator.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public string ReadNullTerminatedString()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bytes = new List<byte>();

        while (true)
        {
            var b = _stream.ReadByte();
            if (b == -1)
                throw new EndOfStreamException("Reached end of stream before finding null terminator.");

            if (b == 0)
                break;

            bytes.Add((byte)b);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string from the stream.
    /// The length is stored as a 4-byte big-endian unsigned integer.
    /// </summary>
    /// <returns>The string read from the stream.</returns>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public string ReadLengthPrefixedString()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var length = ReadUInt32();

        if (length == 0)
            return string.Empty;

        var bytes = ReadBytes((int)length);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Reads an array of doubles in big-endian byte order.
    /// </summary>
    /// <param name="count">The number of doubles to read.</param>
    /// <returns>An array of doubles read from the stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached before reading all values.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public double[] ReadDoubleArray(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return Array.Empty<double>();

        var result = new double[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = ReadDouble();
        }

        return result;
    }

    /// <summary>
    /// Reads an array of 32-bit unsigned integers in big-endian byte order.
    /// </summary>
    /// <param name="count">The number of uint32 values to read.</param>
    /// <returns>An array of uint32 values read from the stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached before reading all values.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public uint[] ReadUInt32Array(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return Array.Empty<uint>();

        var result = new uint[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = ReadUInt32();
        }

        return result;
    }

    /// <summary>
    /// Reads an array of 64-bit unsigned integers in big-endian byte order.
    /// </summary>
    /// <param name="count">The number of uint64 values to read.</param>
    /// <returns>An array of uint64 values read from the stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the end of stream is reached before reading all values.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    public ulong[] ReadUInt64Array(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return Array.Empty<ulong>();

        var result = new ulong[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = ReadUInt64();
        }

        return result;
    }

    /// <summary>
    /// Disposes the reader. Does NOT dispose the underlying stream.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
    }
}
