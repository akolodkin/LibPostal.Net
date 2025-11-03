using LibPostal.Net.IO;

namespace LibPostal.Net.ML;

/// <summary>
/// Serializer for DenseMatrix matching libpostal's binary format.
/// </summary>
/// <remarks>
/// Binary format (big-endian):
/// - m (uint64): number of rows
/// - n (uint64): number of columns
/// - values (double[m*n]): row-major order values
/// </remarks>
public static class DenseMatrixSerializer
{
    /// <summary>
    /// Writes a dense matrix to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="matrix">The dense matrix to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream or matrix is null.</exception>
    public static void WriteDenseMatrix(Stream stream, DenseMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(matrix);

        using var writer = new BigEndianBinaryWriter(stream);

        // Write dimensions as uint64
        writer.WriteUInt64((ulong)matrix.Rows);
        writer.WriteUInt64((ulong)matrix.Columns);

        // Write values in row-major order
        for (int row = 0; row < matrix.Rows; row++)
        {
            for (int col = 0; col < matrix.Columns; col++)
            {
                writer.WriteDouble(matrix[row, col]);
            }
        }
    }

    /// <summary>
    /// Reads a dense matrix from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The dense matrix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static DenseMatrix ReadDenseMatrix(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BigEndianBinaryReader(stream);

        // Read dimensions
        var rows = (int)reader.ReadUInt64();
        var cols = (int)reader.ReadUInt64();

        var matrix = new DenseMatrix(rows, cols);

        // Read values in row-major order
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                matrix[row, col] = reader.ReadDouble();
            }
        }

        return matrix;
    }
}
