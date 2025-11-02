namespace LibPostal.Net.ML;

/// <summary>
/// Dense matrix implementation for CRF transition weights.
/// Based on libpostal's matrix.c
/// </summary>
public class DenseMatrix
{
    private double[,] _data;
    private int _rows;
    private int _cols;

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Rows => _rows;

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Columns => _cols;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMatrix"/> class.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="cols">The number of columns.</param>
    public DenseMatrix(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        _data = new double[rows, cols];
    }

    /// <summary>
    /// Gets or sets the value at the specified position.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    /// <returns>The value at the position.</returns>
    public double this[int row, int col]
    {
        get
        {
            if (row < 0 || row >= _rows || col < 0 || col >= _cols)
                throw new IndexOutOfRangeException($"Index ({row}, {col}) is out of range for matrix ({_rows}, {_cols}).");
            return _data[row, col];
        }
        set
        {
            if (row < 0 || row >= _rows || col < 0 || col >= _cols)
                throw new IndexOutOfRangeException($"Index ({row}, {col}) is out of range for matrix ({_rows}, {_cols}).");
            _data[row, col] = value;
        }
    }

    /// <summary>
    /// Gets all values in a row.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <returns>Array of row values.</returns>
    public double[] GetRow(int row)
    {
        if (row < 0 || row >= _rows)
            throw new IndexOutOfRangeException();

        var result = new double[_cols];
        for (int col = 0; col < _cols; col++)
        {
            result[col] = _data[row, col];
        }
        return result;
    }

    /// <summary>
    /// Sets all values in a row.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="values">The values to set.</param>
    public void SetRow(int row, double[] values)
    {
        if (row < 0 || row >= _rows)
            throw new IndexOutOfRangeException();
        if (values.Length != _cols)
            throw new ArgumentException($"Values length {values.Length} does not match columns {_cols}.");

        for (int col = 0; col < _cols; col++)
        {
            _data[row, col] = values[col];
        }
    }

    /// <summary>
    /// Sets all matrix elements to zero.
    /// </summary>
    public void Zero()
    {
        Array.Clear(_data, 0, _data.Length);
    }

    /// <summary>
    /// Creates a copy of the matrix.
    /// </summary>
    /// <returns>A new matrix with the same values.</returns>
    public DenseMatrix Copy()
    {
        var copy = new DenseMatrix(_rows, _cols);
        Array.Copy(_data, copy._data, _data.Length);
        return copy;
    }

    /// <summary>
    /// Resizes the matrix, preserving existing values where possible.
    /// </summary>
    /// <param name="newRows">The new number of rows.</param>
    /// <param name="newCols">The new number of columns.</param>
    public void Resize(int newRows, int newCols)
    {
        var newData = new double[newRows, newCols];

        int minRows = Math.Min(_rows, newRows);
        int minCols = Math.Min(_cols, newCols);

        for (int row = 0; row < minRows; row++)
        {
            for (int col = 0; col < minCols; col++)
            {
                newData[row, col] = _data[row, col];
            }
        }

        _data = newData;
        _rows = newRows;
        _cols = newCols;
    }

    /// <summary>
    /// Multiplies the matrix by a vector.
    /// </summary>
    /// <param name="vector">The vector to multiply.</param>
    /// <returns>The result vector.</returns>
    public double[] MultiplyVector(double[] vector)
    {
        if (vector.Length != _cols)
            throw new ArgumentException($"Vector length {vector.Length} does not match columns {_cols}.");

        var result = new double[_rows];

        for (int row = 0; row < _rows; row++)
        {
            double sum = 0.0;
            for (int col = 0; col < _cols; col++)
            {
                sum += _data[row, col] * vector[col];
            }
            result[row] = sum;
        }

        return result;
    }

    /// <summary>
    /// Applies exponential function element-wise.
    /// </summary>
    /// <returns>A new matrix with exp applied to each element.</returns>
    public DenseMatrix Exp()
    {
        var result = new DenseMatrix(_rows, _cols);

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                result._data[row, col] = Math.Exp(_data[row, col]);
            }
        }

        return result;
    }

    /// <summary>
    /// Adds another matrix to this matrix in-place.
    /// </summary>
    /// <param name="other">The matrix to add.</param>
    public void AddInPlace(DenseMatrix other)
    {
        if (other.Rows != _rows || other.Columns != _cols)
            throw new ArgumentException("Matrix dimensions must match.");

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                _data[row, col] += other._data[row, col];
            }
        }
    }
}
