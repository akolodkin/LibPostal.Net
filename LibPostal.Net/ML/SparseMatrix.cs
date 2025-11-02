namespace LibPostal.Net.ML;

/// <summary>
/// Sparse matrix implementation using CSR (Compressed Sparse Row) format.
/// Based on libpostal's sparse_matrix.c
/// </summary>
/// <typeparam name="T">The numeric type (typically double or float).</typeparam>
public class SparseMatrix<T> where T : struct, IComparable<T>, IEquatable<T>
{
    private readonly Dictionary<(int row, int col), T> _data;
    private readonly int _rows;
    private readonly int _cols;

    /// <summary>
    /// Gets the number of rows.
    /// </summary>
    public int Rows => _rows;

    /// <summary>
    /// Gets the number of columns.
    /// </summary>
    public int Columns => _cols;

    /// <summary>
    /// Gets the number of non-zero entries.
    /// </summary>
    public int NonZeroCount => _data.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="SparseMatrix{T}"/> class.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="cols">The number of columns.</param>
    public SparseMatrix(int rows, int cols)
    {
        if (rows < 0)
            throw new ArgumentException("Rows must be non-negative.", nameof(rows));
        if (cols < 0)
            throw new ArgumentException("Columns must be non-negative.", nameof(cols));

        _rows = rows;
        _cols = cols;
        _data = new Dictionary<(int, int), T>();
    }

    /// <summary>
    /// Sets a value at the specified position.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(int row, int col, T value)
    {
        if (row < 0 || row >= _rows || col < 0 || col >= _cols)
            throw new IndexOutOfRangeException($"Index ({row}, {col}) is out of range for matrix of size ({_rows}, {_cols}).");

        _data[(row, col)] = value;
    }

    /// <summary>
    /// Gets a value at the specified position.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <param name="col">The column index.</param>
    /// <returns>The value at the position, or zero if not set.</returns>
    public T GetValue(int row, int col)
    {
        if (row < 0 || row >= _rows || col < 0 || col >= _cols)
            throw new IndexOutOfRangeException($"Index ({row}, {col}) is out of range for matrix of size ({_rows}, {_cols}).");

        return _data.TryGetValue((row, col), out var value) ? value : default(T);
    }

    /// <summary>
    /// Multiplies the matrix by a vector.
    /// </summary>
    /// <param name="vector">The vector to multiply.</param>
    /// <returns>The result vector.</returns>
    public T[] MultiplyVector(T[] vector)
    {
        if (vector.Length != _cols)
            throw new ArgumentException($"Vector length {vector.Length} does not match matrix columns {_cols}.");

        var result = new T[_rows];

        foreach (var ((row, col), value) in _data)
        {
            dynamic sum = result[row];
            dynamic matVal = value;
            dynamic vecVal = vector[col];

            result[row] = sum + (matVal * vecVal);
        }

        return result;
    }

    /// <summary>
    /// Gets all values in a row as a dense array.
    /// </summary>
    /// <param name="row">The row index.</param>
    /// <returns>The row values.</returns>
    public T[] GetRow(int row)
    {
        if (row < 0 || row >= _rows)
            throw new IndexOutOfRangeException($"Row index {row} is out of range.");

        var result = new T[_cols];

        for (int col = 0; col < _cols; col++)
        {
            if (_data.TryGetValue((row, col), out var value))
            {
                result[col] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts to CSR (Compressed Sparse Row) format.
    /// </summary>
    /// <returns>Tuple of (rowPtr, colIndices, values).</returns>
    public (int[] rowPtr, int[] colIndices, T[] values) ToCSR()
    {
        var rowPtr = new int[_rows + 1];
        var colIndicesList = new List<int>();
        var valuesList = new List<T>();

        int index = 0;
        for (int row = 0; row < _rows; row++)
        {
            rowPtr[row] = index;

            // Collect all non-zero values in this row, sorted by column
            var rowData = _data.Where(kv => kv.Key.row == row)
                .OrderBy(kv => kv.Key.col)
                .ToList();

            foreach (var (key, value) in rowData)
            {
                colIndicesList.Add(key.col);
                valuesList.Add(value);
                index++;
            }
        }

        rowPtr[_rows] = index;

        return (rowPtr, colIndicesList.ToArray(), valuesList.ToArray());
    }

    /// <summary>
    /// Creates a sparse matrix from CSR (Compressed Sparse Row) format.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="cols">The number of columns.</param>
    /// <param name="rowPtr">The row pointer array.</param>
    /// <param name="colIndices">The column indices array.</param>
    /// <param name="values">The values array.</param>
    /// <returns>A sparse matrix.</returns>
    public static SparseMatrix<T> FromCSR(int rows, int cols, int[] rowPtr, int[] colIndices, T[] values)
    {
        var matrix = new SparseMatrix<T>(rows, cols);

        for (int row = 0; row < rows; row++)
        {
            int start = rowPtr[row];
            int end = rowPtr[row + 1];

            for (int idx = start; idx < end; idx++)
            {
                int col = colIndices[idx];
                T value = values[idx];
                matrix.SetValue(row, col, value);
            }
        }

        return matrix;
    }

    /// <summary>
    /// Creates a sparse matrix from a list of (row, col, value) tuples.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="cols">The number of columns.</param>
    /// <param name="tuples">The list of tuples.</param>
    /// <returns>A sparse matrix.</returns>
    public static SparseMatrix<T> FromTuples(int rows, int cols, IEnumerable<(int row, int col, T value)> tuples)
    {
        var matrix = new SparseMatrix<T>(rows, cols);

        foreach (var (row, col, value) in tuples)
        {
            matrix.SetValue(row, col, value);
        }

        return matrix;
    }

    /// <summary>
    /// Transposes the matrix (swaps rows and columns).
    /// </summary>
    /// <returns>The transposed matrix.</returns>
    public SparseMatrix<T> Transpose()
    {
        var transposed = new SparseMatrix<T>(_cols, _rows);

        foreach (var ((row, col), value) in _data)
        {
            transposed.SetValue(col, row, value);
        }

        return transposed;
    }

    /// <summary>
    /// Clears all values from the matrix.
    /// </summary>
    public void Clear()
    {
        _data.Clear();
    }
}
