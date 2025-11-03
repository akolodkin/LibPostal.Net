using LibPostal.Net.IO;

namespace LibPostal.Net.ML;

/// <summary>
/// Serializer for SparseMatrix in CSR format matching libpostal's binary format.
/// </summary>
/// <remarks>
/// Binary format (big-endian):
/// - m (uint32): number of rows
/// - n (uint32): number of columns
/// - indptr_len (uint64): length of indptr array (m + 1)
/// - indptr (uint32[]): row pointer array
/// - indices_len (uint64): length of indices array (number of non-zero values)
/// - indices (uint32[]): column indices
/// - data_len (uint64): length of data array (number of non-zero values)
/// - data (double[] or float[]): non-zero values
/// </remarks>
public static class SparseMatrixSerializer
{
    /// <summary>
    /// Writes a sparse matrix to a stream in CSR format.
    /// </summary>
    /// <typeparam name="T">The numeric type (typically double or float).</typeparam>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="matrix">The sparse matrix to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when stream or matrix is null.</exception>
    public static void WriteSparseMatrix<T>(Stream stream, SparseMatrix<T> matrix)
        where T : struct, IComparable<T>, IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(matrix);

        // Convert to CSR format
        var (rowPtr, colIndices, values) = matrix.ToCSR();

        using var writer = new BigEndianBinaryWriter(stream);

        // Write dimensions
        writer.WriteUInt32((uint)matrix.Rows);
        writer.WriteUInt32((uint)matrix.Columns);

        // Write indptr (row pointers)
        writer.WriteUInt64((ulong)rowPtr.Length);
        writer.WriteUInt32Array(Array.ConvertAll(rowPtr, x => (uint)x));

        // Write indices (column indices)
        writer.WriteUInt64((ulong)colIndices.Length);
        writer.WriteUInt32Array(Array.ConvertAll(colIndices, x => (uint)x));

        // Write data (values)
        writer.WriteUInt64((ulong)values.Length);
        WriteTypedArray(writer, values);
    }

    /// <summary>
    /// Reads a sparse matrix from a stream in CSR format.
    /// </summary>
    /// <typeparam name="T">The numeric type (typically double or float).</typeparam>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The sparse matrix.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static SparseMatrix<T> ReadSparseMatrix<T>(Stream stream)
        where T : struct, IComparable<T>, IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BigEndianBinaryReader(stream);

        // Read dimensions
        var rows = (int)reader.ReadUInt32();
        var cols = (int)reader.ReadUInt32();

        // Read indptr (row pointers)
        var indptrLen = (int)reader.ReadUInt64();
        var rowPtrUint = reader.ReadUInt32Array(indptrLen);
        var rowPtr = Array.ConvertAll(rowPtrUint, x => (int)x);

        // Read indices (column indices)
        var indicesLen = (int)reader.ReadUInt64();
        var indicesUint = reader.ReadUInt32Array(indicesLen);
        var colIndices = Array.ConvertAll(indicesUint, x => (int)x);

        // Read data (values)
        var dataLen = (int)reader.ReadUInt64();
        var values = ReadTypedArray<T>(reader, dataLen);

        // Create sparse matrix from CSR format
        return SparseMatrix<T>.FromCSR(rows, cols, rowPtr, colIndices, values);
    }

    /// <summary>
    /// Writes a typed array to the stream.
    /// Handles double and float types with proper serialization.
    /// </summary>
    private static void WriteTypedArray<T>(BigEndianBinaryWriter writer, T[] values)
        where T : struct
    {
        if (typeof(T) == typeof(double))
        {
            var doubleValues = values as double[] ?? throw new InvalidOperationException("Expected double array");
            writer.WriteDoubleArray(doubleValues);
        }
        else if (typeof(T) == typeof(float))
        {
            var floatValues = values as float[] ?? throw new InvalidOperationException("Expected float array");
            // Convert floats to doubles for serialization (libpostal uses doubles)
            var doubleValues = Array.ConvertAll(floatValues, x => (double)x);
            writer.WriteDoubleArray(doubleValues);
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported for sparse matrix serialization.");
        }
    }

    /// <summary>
    /// Reads a typed array from the stream.
    /// Handles double and float types with proper deserialization.
    /// </summary>
    private static T[] ReadTypedArray<T>(BigEndianBinaryReader reader, int count)
        where T : struct
    {
        if (typeof(T) == typeof(double))
        {
            var doubleValues = reader.ReadDoubleArray(count);
            return doubleValues as T[] ?? throw new InvalidOperationException("Failed to cast double array");
        }
        else if (typeof(T) == typeof(float))
        {
            // Read as doubles then convert to floats
            var doubleValues = reader.ReadDoubleArray(count);
            var floatValues = Array.ConvertAll(doubleValues, x => (float)x);
            return floatValues as T[] ?? throw new InvalidOperationException("Failed to cast float array");
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported for sparse matrix deserialization.");
        }
    }
}
