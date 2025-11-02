using FluentAssertions;
using LibPostal.Net.ML;

namespace LibPostal.Net.Tests.ML;

/// <summary>
/// Tests for DenseMatrix class.
/// Needed for CRF transition weights and state scores.
/// </summary>
public class DenseMatrixTests
{
    [Fact]
    public void Constructor_WithDimensions_ShouldInitialize()
    {
        // Act
        var matrix = new DenseMatrix(rows: 5, cols: 3);

        // Assert
        matrix.Rows.Should().Be(5);
        matrix.Columns.Should().Be(3);
    }

    [Fact]
    public void Indexer_SetAndGet_ShouldWork()
    {
        // Arrange
        var matrix = new DenseMatrix(3, 3);

        // Act
        matrix[0, 0] = 1.5;
        matrix[1, 1] = 2.5;
        matrix[2, 2] = 3.5;

        // Assert
        matrix[0, 0].Should().Be(1.5);
        matrix[1, 1].Should().Be(2.5);
        matrix[2, 2].Should().Be(3.5);
    }

    [Fact]
    public void Indexer_OutOfBounds_ShouldThrow()
    {
        // Arrange
        var matrix = new DenseMatrix(3, 3);

        // Act
        Action act = () => matrix[5, 5] = 1.0;

        // Assert
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void GetRow_ShouldReturnCorrectValues()
    {
        // Arrange
        var matrix = new DenseMatrix(3, 4);
        matrix[1, 0] = 1.0;
        matrix[1, 1] = 2.0;
        matrix[1, 2] = 3.0;
        matrix[1, 3] = 4.0;

        // Act
        var row = matrix.GetRow(1);

        // Assert
        row.Should().HaveCount(4);
        row.Should().Equal(1.0, 2.0, 3.0, 4.0);
    }

    [Fact]
    public void SetRow_ShouldUpdateAllColumns()
    {
        // Arrange
        var matrix = new DenseMatrix(3, 3);
        var newRow = new double[] { 7.0, 8.0, 9.0 };

        // Act
        matrix.SetRow(1, newRow);

        // Assert
        matrix[1, 0].Should().Be(7.0);
        matrix[1, 1].Should().Be(8.0);
        matrix[1, 2].Should().Be(9.0);
    }

    [Fact]
    public void Zero_ShouldClearAllValues()
    {
        // Arrange
        var matrix = new DenseMatrix(3, 3);
        matrix[0, 0] = 1.0;
        matrix[1, 1] = 2.0;
        matrix[2, 2] = 3.0;

        // Act
        matrix.Zero();

        // Assert
        matrix[0, 0].Should().Be(0.0);
        matrix[1, 1].Should().Be(0.0);
        matrix[2, 2].Should().Be(0.0);
    }

    [Fact]
    public void Copy_ShouldDuplicateMatrix()
    {
        // Arrange
        var original = new DenseMatrix(2, 2);
        original[0, 0] = 1.0;
        original[0, 1] = 2.0;
        original[1, 0] = 3.0;
        original[1, 1] = 4.0;

        // Act
        var copy = original.Copy();

        // Assert
        copy[0, 0].Should().Be(1.0);
        copy[0, 1].Should().Be(2.0);
        copy[1, 0].Should().Be(3.0);
        copy[1, 1].Should().Be(4.0);

        // Modify copy shouldn't affect original
        copy[0, 0] = 99.0;
        original[0, 0].Should().Be(1.0);
    }

    [Fact]
    public void Resize_ShouldChangeMatrixSize()
    {
        // Arrange
        var matrix = new DenseMatrix(2, 2);
        matrix[0, 0] = 1.0;

        // Act
        matrix.Resize(5, 5);

        // Assert
        matrix.Rows.Should().Be(5);
        matrix.Columns.Should().Be(5);
        matrix[0, 0].Should().Be(1.0); // Existing values preserved
    }

    [Fact]
    public void MultiplyVector_ShouldComputeCorrectly()
    {
        // Arrange
        var matrix = new DenseMatrix(2, 3);
        matrix[0, 0] = 1.0;
        matrix[0, 1] = 2.0;
        matrix[0, 2] = 3.0;
        matrix[1, 0] = 4.0;
        matrix[1, 1] = 5.0;
        matrix[1, 2] = 6.0;

        var vector = new double[] { 1.0, 2.0, 3.0 };

        // Act
        var result = matrix.MultiplyVector(vector);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be(14.0); // 1*1 + 2*2 + 3*3
        result[1].Should().Be(32.0); // 4*1 + 5*2 + 6*3
    }

    [Fact]
    public void Exp_ShouldApplyElementWise()
    {
        // Arrange
        var matrix = new DenseMatrix(2, 2);
        matrix[0, 0] = 0.0;
        matrix[0, 1] = 1.0;
        matrix[1, 0] = 2.0;
        matrix[1, 1] = -1.0;

        // Act
        var result = matrix.Exp();

        // Assert
        result[0, 0].Should().BeApproximately(1.0, 0.001);     // e^0 = 1
        result[0, 1].Should().BeApproximately(2.718, 0.001);   // e^1 ≈ 2.718
        result[1, 0].Should().BeApproximately(7.389, 0.001);   // e^2 ≈ 7.389
        result[1, 1].Should().BeApproximately(0.368, 0.001);   // e^-1 ≈ 0.368
    }

    [Fact]
    public void AddInPlace_ShouldSumMatrices()
    {
        // Arrange
        var matrix1 = new DenseMatrix(2, 2);
        matrix1[0, 0] = 1.0;
        matrix1[0, 1] = 2.0;

        var matrix2 = new DenseMatrix(2, 2);
        matrix2[0, 0] = 3.0;
        matrix2[0, 1] = 4.0;

        // Act
        matrix1.AddInPlace(matrix2);

        // Assert
        matrix1[0, 0].Should().Be(4.0); // 1 + 3
        matrix1[0, 1].Should().Be(6.0); // 2 + 4
    }
}
