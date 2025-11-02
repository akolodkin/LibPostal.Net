namespace LibPostal.Net.IO;

/// <summary>
/// Validates file signatures (magic numbers) for libpostal data files.
/// libpostal uses big-endian signatures to identify file types.
/// </summary>
public static class FileSignature
{
    /// <summary>
    /// The signature for trie data files (0xABABABAB).
    /// </summary>
    public const uint TrieSignature = 0xABABABAB;

    /// <summary>
    /// Validates that a stream starts with the expected signature.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <param name="expectedSignature">The expected signature value.</param>
    /// <param name="fileType">The file type description for error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="fileType"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the signature does not match.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream does not contain enough data.</exception>
    /// <remarks>
    /// If the stream is seekable, the position will be reset to the beginning after validation.
    /// If the stream is not seekable, the position will be after the signature.
    /// </remarks>
    public static void ValidateSignature(Stream stream, uint expectedSignature, string fileType)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(fileType);

        var startPosition = stream.CanSeek ? stream.Position : 0;

        using var reader = new BigEndianBinaryReader(stream);
        var actualSignature = reader.ReadUInt32();

        if (actualSignature != expectedSignature)
        {
            throw new InvalidDataException(
                $"Invalid {fileType} file signature. Expected 0x{expectedSignature:X8}, got 0x{actualSignature:X8}.");
        }

        // Reset position if stream is seekable
        if (stream.CanSeek)
        {
            stream.Position = startPosition;
        }
    }

    /// <summary>
    /// Tries to validate that a stream starts with the expected signature.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <param name="expectedSignature">The expected signature value.</param>
    /// <returns>True if the signature matches; otherwise, false.</returns>
    /// <remarks>
    /// The stream position will be reset to the beginning after validation if the stream is seekable.
    /// </remarks>
    public static bool TryValidateSignature(Stream stream, uint expectedSignature)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var startPosition = stream.CanSeek ? stream.Position : 0;

        try
        {
            using var reader = new BigEndianBinaryReader(stream);
            var actualSignature = reader.ReadUInt32();

            var isValid = actualSignature == expectedSignature;

            // Reset position if stream is seekable
            if (stream.CanSeek)
            {
                stream.Position = startPosition;
            }

            return isValid;
        }
        catch (EndOfStreamException)
        {
            // Not enough data in stream
            if (stream.CanSeek)
            {
                stream.Position = startPosition;
            }
            return false;
        }
    }

    /// <summary>
    /// Validates that a stream is a valid trie data file.
    /// </summary>
    /// <param name="stream">The stream to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the signature does not match.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream does not contain enough data.</exception>
    public static void ValidateTrieSignature(Stream stream)
    {
        ValidateSignature(stream, TrieSignature, "trie");
    }
}
