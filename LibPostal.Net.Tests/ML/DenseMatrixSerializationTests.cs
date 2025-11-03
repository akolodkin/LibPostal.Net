using FluentAssertions;
using LibPostal.Net.ML;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for DenseMatrix serialization matching libpostal binary format.
/// </summary>
public class DenseMatrixSerializationTests
{
    [Fact]
    public void WriteDenseMatrix_WithSimpleMatrix_ShouldWriteCorrectFormat()
    {
        // Arrange - 2x3 matrix
        // [1.0  2.0  3.0]
        // [4.0  5.0  6.0]
        var matrix = new DenseMatrix(2, 3);
        matrix[0, 0] = 1.0;
        matrix[0, 1] = 2.0;
        matrix[0, 2] = 3.0;
        matrix[1, 0] = 4.0;
        matrix[1, 1] = 5.0;
        matrix[1, 2] = 6.0;

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, matrix);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        // m (rows) - uint64
        reader.ReadUInt64().Should().Be(2);

        // n (cols) - uint64
        reader.ReadUInt64().Should().Be(3);

        // values (row-major order)
        var values = reader.ReadDoubleArray(6);
        values.Should().Equal(1.0, 2.0, 3.0, 4.0, 5.0, 6.0);
    }

    [Fact]
    public void ReadDenseMatrix_WithValidFormat_ShouldReadCorrectly()
    {
        // Arrange - create binary data for 2x2 matrix
        // [1.5  2.5]
        // [3.5  4.5]
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt64(2); // m (rows)
            writer.WriteUInt64(2); // n (cols)
            writer.WriteDoubleArray(new double[] { 1.5, 2.5, 3.5, 4.5 }); // row-major
        }

        stream.Position = 0;

        // Act
        var matrix = DenseMatrixSerializer.ReadDenseMatrix(stream);

        // Assert
        matrix.Rows.Should().Be(2);
        matrix.Columns.Should().Be(2);
        matrix[0, 0].Should().Be(1.5);
        matrix[0, 1].Should().Be(2.5);
        matrix[1, 0].Should().Be(3.5);
        matrix[1, 1].Should().Be(4.5);
    }

    [Fact]
    public void RoundTrip_DenseMatrix_ShouldPreserveValues()
    {
        // Arrange - 3x4 matrix
        var original = new DenseMatrix(3, 4);
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                original[row, col] = row * 4 + col + 0.1;
            }
        }

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, original);
        stream.Position = 0;
        var result = DenseMatrixSerializer.ReadDenseMatrix(stream);

        // Assert
        result.Rows.Should().Be(original.Rows);
        result.Columns.Should().Be(original.Columns);
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                result[row, col].Should().Be(original[row, col]);
            }
        }
    }

    [Fact]
    public void WriteDenseMatrix_WithSquareMatrix_ShouldWriteCorrectly()
    {
        // Arrange - 5x5 square matrix (typical for CRF transitions)
        var matrix = new DenseMatrix(5, 5);
        for (int i = 0; i < 5; i++)
        {
            matrix[i, i] = i + 1.0; // diagonal
        }

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, matrix);

        // Assert
        stream.Position = 0;
        var result = DenseMatrixSerializer.ReadDenseMatrix(stream);

        result.Rows.Should().Be(5);
        result.Columns.Should().Be(5);
        for (int i = 0; i < 5; i++)
        {
            result[i, i].Should().Be(i + 1.0);
        }
    }

    [Fact]
    public void WriteDenseMatrix_WithSingleElement_ShouldWriteCorrectly()
    {
        // Arrange - 1x1 matrix
        var matrix = new DenseMatrix(1, 1);
        matrix[0, 0] = 42.0;

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, matrix);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        reader.ReadUInt64().Should().Be(1);
        reader.ReadUInt64().Should().Be(1);
        reader.ReadDouble().Should().Be(42.0);
    }

    [Fact]
    public void WriteDenseMatrix_WithRowVector_ShouldWriteCorrectly()
    {
        // Arrange - 1x5 matrix (row vector)
        var matrix = new DenseMatrix(1, 5);
        for (int i = 0; i < 5; i++)
        {
            matrix[0, i] = i * 0.5;
        }

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, matrix);
        stream.Position = 0;
        var result = DenseMatrixSerializer.ReadDenseMatrix(stream);

        // Assert
        result.Rows.Should().Be(1);
        result.Columns.Should().Be(5);
        for (int i = 0; i < 5; i++)
        {
            result[0, i].Should().Be(i * 0.5);
        }
    }

    [Fact]
    public void WriteDenseMatrix_WithColumnVector_ShouldWriteCorrectly()
    {
        // Arrange - 5x1 matrix (column vector)
        var matrix = new DenseMatrix(5, 1);
        for (int i = 0; i < 5; i++)
        {
            matrix[i, 0] = i * 1.5;
        }

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, matrix);
        stream.Position = 0;
        var result = DenseMatrixSerializer.ReadDenseMatrix(stream);

        // Assert
        result.Rows.Should().Be(5);
        result.Columns.Should().Be(1);
        for (int i = 0; i < 5; i++)
        {
            result[i, 0].Should().Be(i * 1.5);
        }
    }

    [Fact]
    public void WriteDenseMatrix_WithNegativeValues_ShouldHandleCorrectly()
    {
        // Arrange - matrix with negative values (common in ML)
        var matrix = new DenseMatrix(2, 2);
        matrix[0, 0] = -1.5;
        matrix[0, 1] = 2.5;
        matrix[1, 0] = -3.5;
        matrix[1, 1] = 4.5;

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, matrix);
        stream.Position = 0;
        var result = DenseMatrixSerializer.ReadDenseMatrix(stream);

        // Assert
        result[0, 0].Should().Be(-1.5);
        result[0, 1].Should().Be(2.5);
        result[1, 0].Should().Be(-3.5);
        result[1, 1].Should().Be(4.5);
    }

    [Fact]
    public void WriteDenseMatrix_WithZeroMatrix_ShouldWriteCorrectly()
    {
        // Arrange - all zeros
        var matrix = new DenseMatrix(3, 3);
        matrix.Zero();

        using var stream = new MemoryStream();

        // Act
        DenseMatrixSerializer.WriteDenseMatrix(stream, matrix);
        stream.Position = 0;
        var result = DenseMatrixSerializer.ReadDenseMatrix(stream);

        // Assert
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                result[row, col].Should().Be(0.0);
            }
        }
    }

    [Fact]
    public void WriteDenseMatrix_WithNullMatrix_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        Action act = () => DenseMatrixSerializer.WriteDenseMatrix(stream, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadDenseMatrix_WithNullStream_ShouldThrow()
    {
        // Act
        Action act = () => DenseMatrixSerializer.ReadDenseMatrix(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteDenseMatrix_WithNullStream_ShouldThrow()
    {
        // Arrange
        var matrix = new DenseMatrix(2, 2);

        // Act
        Action act = () => DenseMatrixSerializer.WriteDenseMatrix(null!, matrix);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
