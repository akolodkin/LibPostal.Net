using FluentAssertions;
using LibPostal.Net.ML;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for SparseMatrix using CSR (Compressed Sparse Row) format.
/// Based on libpostal's sparse_matrix.c
/// </summary>
public class SparseMatrixTests
{
    [Fact]
    public void Constructor_WithValidDimensions_ShouldInitialize()
    {
        // Act
        var matrix = new SparseMatrix<double>(rows: 10, cols: 5);

        // Assert
        matrix.Rows.Should().Be(10);
        matrix.Columns.Should().Be(5);
        matrix.NonZeroCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNegativeRows_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new SparseMatrix<double>(rows: -1, cols: 5);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNegativeColumns_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new SparseMatrix<double>(rows: 10, cols: -1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetValue_ValidPosition_ShouldStoreValue()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 3, cols: 3);

        // Act
        matrix.SetValue(0, 0, 1.0);
        matrix.SetValue(1, 1, 2.0);
        matrix.SetValue(2, 2, 3.0);

        // Assert
        matrix.GetValue(0, 0).Should().Be(1.0);
        matrix.GetValue(1, 1).Should().Be(2.0);
        matrix.GetValue(2, 2).Should().Be(3.0);
        matrix.NonZeroCount.Should().Be(3);
    }

    [Fact]
    public void GetValue_ForZeroEntry_ShouldReturnZero()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 5, cols: 5);
        matrix.SetValue(0, 0, 1.0);

        // Act
        var value = matrix.GetValue(2, 2);

        // Assert
        value.Should().Be(0.0);
    }

    [Fact]
    public void SetValue_OutOfBounds_ShouldThrowIndexOutOfRangeException()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 3, cols: 3);

        // Act
        Action act = () => matrix.SetValue(5, 5, 1.0);

        // Assert
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void MultiplyVector_WithValidVector_ShouldComputeCorrectly()
    {
        // Arrange - 2x3 matrix
        var matrix = new SparseMatrix<double>(rows: 2, cols: 3);
        matrix.SetValue(0, 0, 1.0);
        matrix.SetValue(0, 1, 2.0);
        matrix.SetValue(0, 2, 3.0);
        matrix.SetValue(1, 0, 4.0);
        matrix.SetValue(1, 1, 5.0);
        matrix.SetValue(1, 2, 6.0);

        var vector = new double[] { 1.0, 2.0, 3.0 };

        // Act
        var result = matrix.MultiplyVector(vector);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be(14.0); // 1*1 + 2*2 + 3*3 = 14
        result[1].Should().Be(32.0); // 4*1 + 5*2 + 6*3 = 32
    }

    [Fact]
    public void MultiplyVector_WithSparseMatrix_ShouldComputeCorrectly()
    {
        // Arrange - mostly zeros
        var matrix = new SparseMatrix<double>(rows: 3, cols: 100);
        matrix.SetValue(0, 5, 2.0);
        matrix.SetValue(1, 10, 3.0);
        matrix.SetValue(2, 50, 4.0);

        var vector = new double[100];
        vector[5] = 1.0;
        vector[10] = 2.0;
        vector[50] = 3.0;

        // Act
        var result = matrix.MultiplyVector(vector);

        // Assert
        result[0].Should().Be(2.0);  // 2.0 * 1.0
        result[1].Should().Be(6.0);  // 3.0 * 2.0
        result[2].Should().Be(12.0); // 4.0 * 3.0
    }

    [Fact]
    public void MultiplyVector_WithWrongVectorSize_ShouldThrowArgumentException()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 2, cols: 3);
        var vector = new double[] { 1.0, 2.0 }; // Wrong size

        // Act
        Action act = () => matrix.MultiplyVector(vector);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateFromTuples_ShouldBuildCorrectMatrix()
    {
        // Arrange
        var tuples = new List<(int row, int col, double value)>
        {
            (0, 0, 1.0),
            (0, 2, 2.0),
            (1, 1, 3.0),
            (2, 0, 4.0),
            (2, 2, 5.0)
        };

        // Act
        var matrix = SparseMatrix<double>.FromTuples(3, 3, tuples);

        // Assert
        matrix.Rows.Should().Be(3);
        matrix.Columns.Should().Be(3);
        matrix.NonZeroCount.Should().Be(5);
        matrix.GetValue(0, 0).Should().Be(1.0);
        matrix.GetValue(0, 2).Should().Be(2.0);
        matrix.GetValue(1, 1).Should().Be(3.0);
    }

    [Fact]
    public void GetRow_ShouldReturnCorrectValues()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 3, cols: 4);
        matrix.SetValue(1, 0, 1.0);
        matrix.SetValue(1, 2, 2.0);
        matrix.SetValue(1, 3, 3.0);

        // Act
        var row = matrix.GetRow(1);

        // Assert
        row.Should().HaveCount(4);
        row[0].Should().Be(1.0);
        row[1].Should().Be(0.0);
        row[2].Should().Be(2.0);
        row[3].Should().Be(3.0);
    }

    [Fact]
    public void ToCSR_ShouldConvertToCompressedFormat()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 3, cols: 3);
        matrix.SetValue(0, 0, 1.0);
        matrix.SetValue(0, 2, 2.0);
        matrix.SetValue(1, 1, 3.0);

        // Act
        var (rowPtr, colIndices, values) = matrix.ToCSR();

        // Assert
        rowPtr.Should().HaveCount(4); // rows + 1
        colIndices.Should().HaveCount(3); // non-zero count
        values.Should().HaveCount(3);
    }

    [Fact]
    public void Transpose_ShouldSwapRowsAndColumns()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 2, cols: 3);
        matrix.SetValue(0, 1, 5.0);
        matrix.SetValue(1, 2, 7.0);

        // Act
        var transposed = matrix.Transpose();

        // Assert
        transposed.Rows.Should().Be(3);
        transposed.Columns.Should().Be(2);
        transposed.GetValue(1, 0).Should().Be(5.0);
        transposed.GetValue(2, 1).Should().Be(7.0);
    }

    [Fact]
    public void Clear_ShouldRemoveAllValues()
    {
        // Arrange
        var matrix = new SparseMatrix<double>(rows: 5, cols: 5);
        matrix.SetValue(0, 0, 1.0);
        matrix.SetValue(1, 1, 2.0);
        matrix.SetValue(2, 2, 3.0);

        // Act
        matrix.Clear();

        // Assert
        matrix.NonZeroCount.Should().Be(0);
        matrix.GetValue(0, 0).Should().Be(0.0);
        matrix.GetValue(1, 1).Should().Be(0.0);
    }

    [Fact]
    public void SparseMatrix_WithFloatType_ShouldWork()
    {
        // Arrange
        var matrix = new SparseMatrix<float>(rows: 2, cols: 2);

        // Act
        matrix.SetValue(0, 0, 1.5f);
        matrix.SetValue(1, 1, 2.5f);

        // Assert
        matrix.GetValue(0, 0).Should().Be(1.5f);
        matrix.GetValue(1, 1).Should().Be(2.5f);
    }

    [Fact]
    public void MultiplyVector_LargeMatrix_ShouldBeEfficient()
    {
        // Arrange - simulate a language classifier weight matrix
        var matrix = new SparseMatrix<double>(rows: 100, cols: 10000);

        // Set sparse values (only 0.1% density)
        for (int i = 0; i < 100; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                matrix.SetValue(i, i * 100 + j, (i + 1) * 0.1);
            }
        }

        var vector = new double[10000];
        for (int i = 0; i < 100; i++)
        {
            vector[i * 100] = 1.0;
        }

        // Act
        var result = matrix.MultiplyVector(vector);

        // Assert
        result.Should().HaveCount(100);
        matrix.NonZeroCount.Should().Be(1000); // 100 rows * 10 values each
    }

    [Fact]
    public void FromCSR_ShouldCreateMatrixFromCompressedFormat()
    {
        // Arrange - CSR format for a simple matrix
        var rowPtr = new int[] { 0, 2, 3, 5 }; // 3 rows
        var colIndices = new int[] { 0, 2, 1, 0, 2 };
        var values = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var matrix = SparseMatrix<double>.FromCSR(3, 3, rowPtr, colIndices, values);

        // Assert
        matrix.Rows.Should().Be(3);
        matrix.Columns.Should().Be(3);
        matrix.NonZeroCount.Should().Be(5);
        matrix.GetValue(0, 0).Should().Be(1.0);
        matrix.GetValue(0, 2).Should().Be(2.0);
        matrix.GetValue(1, 1).Should().Be(3.0);
        matrix.GetValue(2, 0).Should().Be(4.0);
        matrix.GetValue(2, 2).Should().Be(5.0);
    }
}
