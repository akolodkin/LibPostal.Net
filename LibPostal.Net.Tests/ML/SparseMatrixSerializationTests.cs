using FluentAssertions;
using LibPostal.Net.ML;
using LibPostal.Net.IO;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for SparseMatrix CSR serialization matching libpostal binary format.
/// </summary>
public class SparseMatrixSerializationTests
{
    [Fact]
    public void WriteSparseMatrix_WithSimpleMatrix_ShouldWriteCorrectFormat()
    {
        // Arrange - 3x3 matrix with 4 non-zero values
        // [1.0  0    2.0]
        // [0    0    0  ]
        // [0    3.0  4.0]
        var matrix = new SparseMatrix<double>(3, 3);
        matrix.SetValue(0, 0, 1.0);
        matrix.SetValue(0, 2, 2.0);
        matrix.SetValue(2, 1, 3.0);
        matrix.SetValue(2, 2, 4.0);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        // m (rows)
        reader.ReadUInt32().Should().Be(3);

        // n (cols)
        reader.ReadUInt32().Should().Be(3);

        // indptr_len
        reader.ReadUInt64().Should().Be(4); // rows + 1

        // indptr [0, 2, 2, 4]
        reader.ReadUInt32Array(4).Should().Equal(0, 2, 2, 4);

        // indices_len
        reader.ReadUInt64().Should().Be(4); // 4 non-zero values

        // indices [0, 2, 1, 2]
        reader.ReadUInt32Array(4).Should().Equal(0, 2, 1, 2);

        // data_len
        reader.ReadUInt64().Should().Be(4);

        // data [1.0, 2.0, 3.0, 4.0]
        var data = reader.ReadDoubleArray(4);
        data[0].Should().Be(1.0);
        data[1].Should().Be(2.0);
        data[2].Should().Be(3.0);
        data[3].Should().Be(4.0);
    }

    [Fact]
    public void ReadSparseMatrix_WithValidFormat_ShouldReadCorrectly()
    {
        // Arrange - create binary data for 2x2 matrix
        // [5.0  0  ]
        // [0    6.0]
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(2); // m (rows)
            writer.WriteUInt32(2); // n (cols)

            writer.WriteUInt64(3); // indptr_len (rows + 1)
            writer.WriteUInt32Array(new uint[] { 0, 1, 2 }); // indptr

            writer.WriteUInt64(2); // indices_len
            writer.WriteUInt32Array(new uint[] { 0, 1 }); // indices

            writer.WriteUInt64(2); // data_len
            writer.WriteDoubleArray(new double[] { 5.0, 6.0 }); // data
        }

        stream.Position = 0;

        // Act
        var matrix = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        matrix.Rows.Should().Be(2);
        matrix.Columns.Should().Be(2);
        matrix.GetValue(0, 0).Should().Be(5.0);
        matrix.GetValue(0, 1).Should().Be(0.0);
        matrix.GetValue(1, 0).Should().Be(0.0);
        matrix.GetValue(1, 1).Should().Be(6.0);
    }

    [Fact]
    public void RoundTrip_SparseMatrix_ShouldPreserveValues()
    {
        // Arrange - 5x4 matrix with various values
        var original = new SparseMatrix<double>(5, 4);
        original.SetValue(0, 1, 1.5);
        original.SetValue(1, 2, 2.7);
        original.SetValue(2, 0, -3.2);
        original.SetValue(4, 3, 9.9);

        using var stream = new MemoryStream();

        // Act - write then read
        SparseMatrixSerializer.WriteSparseMatrix(stream, original);
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        result.Rows.Should().Be(original.Rows);
        result.Columns.Should().Be(original.Columns);
        result.GetValue(0, 1).Should().Be(1.5);
        result.GetValue(1, 2).Should().Be(2.7);
        result.GetValue(2, 0).Should().Be(-3.2);
        result.GetValue(4, 3).Should().Be(9.9);
        result.GetValue(3, 0).Should().Be(0.0); // unset value
    }

    [Fact]
    public void WriteSparseMatrix_WithEmptyMatrix_ShouldWriteCorrectly()
    {
        // Arrange - 2x2 matrix with no values
        var matrix = new SparseMatrix<double>(2, 2);
        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        reader.ReadUInt32().Should().Be(2); // rows
        reader.ReadUInt32().Should().Be(2); // cols
        reader.ReadUInt64().Should().Be(3); // indptr_len (rows + 1)
        reader.ReadUInt32Array(3).Should().Equal(0, 0, 0); // all zeros
        reader.ReadUInt64().Should().Be(0); // indices_len
        reader.ReadUInt64().Should().Be(0); // data_len
    }

    [Fact]
    public void ReadSparseMatrix_WithEmptyMatrix_ShouldReturnEmptyMatrix()
    {
        // Arrange
        using var stream = new MemoryStream();
        using (var writer = new BigEndianBinaryWriter(stream))
        {
            writer.WriteUInt32(3); // rows
            writer.WriteUInt32(4); // cols
            writer.WriteUInt64(4); // indptr_len
            writer.WriteUInt32Array(new uint[] { 0, 0, 0, 0 }); // all zeros
            writer.WriteUInt64(0); // indices_len
            writer.WriteUInt64(0); // data_len
        }

        stream.Position = 0;

        // Act
        var matrix = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        matrix.Rows.Should().Be(3);
        matrix.Columns.Should().Be(4);
        matrix.NonZeroCount.Should().Be(0);
    }

    [Fact]
    public void WriteSparseMatrix_WithSingleRow_ShouldWriteCorrectly()
    {
        // Arrange - 1x5 matrix (row vector)
        var matrix = new SparseMatrix<double>(1, 5);
        matrix.SetValue(0, 1, 7.5);
        matrix.SetValue(0, 3, 8.5);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);

        // Assert
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        reader.ReadUInt32().Should().Be(1);
        reader.ReadUInt32().Should().Be(5);
        reader.ReadUInt64().Should().Be(2); // 1 + 1
        reader.ReadUInt32Array(2).Should().Equal(0, 2);
        reader.ReadUInt64().Should().Be(2);
        reader.ReadUInt32Array(2).Should().Equal(1, 3);
        reader.ReadUInt64().Should().Be(2);
        var data = reader.ReadDoubleArray(2);
        data.Should().Equal(7.5, 8.5);
    }

    [Fact]
    public void WriteSparseMatrix_WithSingleColumn_ShouldWriteCorrectly()
    {
        // Arrange - 5x1 matrix (column vector)
        var matrix = new SparseMatrix<double>(5, 1);
        matrix.SetValue(1, 0, 4.2);
        matrix.SetValue(3, 0, 5.3);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);

        // Assert
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        result.Rows.Should().Be(5);
        result.Columns.Should().Be(1);
        result.GetValue(1, 0).Should().Be(4.2);
        result.GetValue(3, 0).Should().Be(5.3);
    }

    [Fact]
    public void WriteSparseMatrix_WithLargeMatrix_ShouldHandleCorrectly()
    {
        // Arrange - 1000x100 matrix with 500 non-zero values
        var matrix = new SparseMatrix<double>(1000, 100);

        for (int i = 0; i < 500; i++)
        {
            matrix.SetValue(i * 2, (i * 3) % 100, i * 0.1);
        }

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        result.Rows.Should().Be(1000);
        result.Columns.Should().Be(100);
        result.NonZeroCount.Should().Be(500);

        // Spot check a few values
        result.GetValue(0, 0).Should().Be(0.0);
        result.GetValue(2, 3).Should().Be(0.1);
        result.GetValue(4, 6).Should().Be(0.2);
    }

    [Fact]
    public void ReadWriteSparseMatrix_WithFloatType_ShouldWork()
    {
        // Arrange - test with float instead of double
        var matrix = new SparseMatrix<float>(3, 3);
        matrix.SetValue(0, 0, 1.5f);
        matrix.SetValue(1, 1, 2.5f);
        matrix.SetValue(2, 2, 3.5f);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<float>(stream);

        // Assert
        result.Rows.Should().Be(3);
        result.Columns.Should().Be(3);
        result.GetValue(0, 0).Should().Be(1.5f);
        result.GetValue(1, 1).Should().Be(2.5f);
        result.GetValue(2, 2).Should().Be(3.5f);
    }

    [Fact]
    public void WriteSparseMatrix_WithNullMatrix_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        Action act = () => SparseMatrixSerializer.WriteSparseMatrix<double>(stream, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ReadSparseMatrix_WithNullStream_ShouldThrow()
    {
        // Act
        Action act = () => SparseMatrixSerializer.ReadSparseMatrix<double>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteSparseMatrix_WithNullStream_ShouldThrow()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(2, 2);

        // Act
        Action act = () => SparseMatrixSerializer.WriteSparseMatrix(null!, matrix);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WriteSparseMatrix_WithDensePattern_ShouldCompressCorrectly()
    {
        // Arrange - diagonal matrix (common in ML)
        var matrix = new SparseMatrix<double>(10, 10);
        for (int i = 0; i < 10; i++)
        {
            matrix.SetValue(i, i, i + 1.0);
        }

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);

        // Assert - verify CSR compression
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        reader.ReadUInt32().Should().Be(10); // rows
        reader.ReadUInt32().Should().Be(10); // cols
        reader.ReadUInt64().Should().Be(11); // indptr_len
        var indptr = reader.ReadUInt32Array(11);
        indptr.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10); // each row has 1 element

        reader.ReadUInt64().Should().Be(10); // indices_len
        var indices = reader.ReadUInt32Array(10);
        indices.Should().Equal(0, 1, 2, 3, 4, 5, 6, 7, 8, 9); // diagonal indices

        reader.ReadUInt64().Should().Be(10); // data_len
        var data = reader.ReadDoubleArray(10);
        for (int i = 0; i < 10; i++)
        {
            data[i].Should().Be(i + 1.0);
        }
    }

    [Fact]
    public void WriteSparseMatrix_WithRowsHavingDifferentSparsity_ShouldHandleCorrectly()
    {
        // Arrange - some rows dense, some sparse, some empty
        var matrix = new SparseMatrix<double>(5, 4);

        // Row 0: dense
        matrix.SetValue(0, 0, 1.0);
        matrix.SetValue(0, 1, 2.0);
        matrix.SetValue(0, 2, 3.0);
        matrix.SetValue(0, 3, 4.0);

        // Row 1: empty

        // Row 2: sparse
        matrix.SetValue(2, 1, 5.0);

        // Row 3: empty

        // Row 4: moderate
        matrix.SetValue(4, 0, 6.0);
        matrix.SetValue(4, 2, 7.0);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        result.GetValue(0, 0).Should().Be(1.0);
        result.GetValue(0, 1).Should().Be(2.0);
        result.GetValue(0, 2).Should().Be(3.0);
        result.GetValue(0, 3).Should().Be(4.0);
        result.GetValue(1, 0).Should().Be(0.0); // empty row
        result.GetValue(2, 1).Should().Be(5.0);
        result.GetValue(3, 0).Should().Be(0.0); // empty row
        result.GetValue(4, 0).Should().Be(6.0);
        result.GetValue(4, 2).Should().Be(7.0);
    }

    [Fact]
    public void WriteSparseMatrix_WithNegativeValues_ShouldHandleCorrectly()
    {
        // Arrange - test negative values (common in ML weights)
        var matrix = new SparseMatrix<double>(2, 2);
        matrix.SetValue(0, 0, -1.5);
        matrix.SetValue(1, 1, -2.5);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        result.GetValue(0, 0).Should().Be(-1.5);
        result.GetValue(1, 1).Should().Be(-2.5);
    }

    [Fact]
    public void WriteSparseMatrix_WithVerySmallValues_ShouldPreservePrecision()
    {
        // Arrange - test very small values
        var matrix = new SparseMatrix<double>(2, 2);
        matrix.SetValue(0, 0, 1e-10);
        matrix.SetValue(1, 1, 1e-100);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        result.GetValue(0, 0).Should().Be(1e-10);
        result.GetValue(1, 1).Should().Be(1e-100);
    }

    [Fact]
    public void WriteSparseMatrix_WithVeryLargeValues_ShouldHandleCorrectly()
    {
        // Arrange - test very large values
        var matrix = new SparseMatrix<double>(2, 2);
        matrix.SetValue(0, 0, 1e100);
        matrix.SetValue(1, 1, double.MaxValue / 2);

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);
        stream.Position = 0;
        var result = SparseMatrixSerializer.ReadSparseMatrix<double>(stream);

        // Assert
        result.GetValue(0, 0).Should().Be(1e100);
        result.GetValue(1, 1).Should().Be(double.MaxValue / 2);
    }

    [Fact]
    public void WriteSparseMatrix_MatchesLibpostalFormat_Integration()
    {
        // Arrange - create a realistic ML weight matrix
        var matrix = new SparseMatrix<double>(1000, 50);

        // Simulate typical sparse weights (10% density)
        var random = new Random(42);
        for (int i = 0; i < 5000; i++)
        {
            int row = random.Next(1000);
            int col = random.Next(50);
            double value = random.NextDouble() * 2 - 1; // [-1, 1]
            matrix.SetValue(row, col, value);
        }

        using var stream = new MemoryStream();

        // Act
        SparseMatrixSerializer.WriteSparseMatrix(stream, matrix);

        // Assert - verify binary format structure
        stream.Position = 0;
        using var reader = new BigEndianBinaryReader(stream);

        var rows = reader.ReadUInt32();
        var cols = reader.ReadUInt32();
        rows.Should().Be(1000);
        cols.Should().Be(50);

        var indptrLen = reader.ReadUInt64();
        indptrLen.Should().Be(1001); // rows + 1

        var indptr = reader.ReadUInt32Array((int)indptrLen);
        indptr[0].Should().Be(0);
        indptr[1000].Should().BeGreaterThan(0); // should have some data

        // Verify CSR property: indptr is non-decreasing
        for (int i = 1; i < indptr.Length; i++)
        {
            indptr[i].Should().BeGreaterThanOrEqualTo(indptr[i - 1]);
        }
    }
}
