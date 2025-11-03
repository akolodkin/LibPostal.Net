using LibPostal.Net.Core;
using LibPostal.Net.IO;

namespace LibPostal.Net.ML;

/// <summary>
/// Conditional Random Field (CRF) model for sequence labeling.
/// Based on libpostal's crf.c
/// </summary>
public class Crf : IDisposable
{
    private const uint CrfSignature = 0xCFCFCFCF;
    private uint _nextFeatureId;
    private uint _nextTransFeatureId;
    private bool _disposed;

    /// <summary>
    /// Gets the number of classes/labels.
    /// </summary>
    public int NumClasses { get; }

    /// <summary>
    /// Gets the class/label names.
    /// </summary>
    public string[] Classes { get; }

    /// <summary>
    /// Gets the state features trie (feature string → feature ID).
    /// </summary>
    public Trie<uint> StateFeatures { get; }

    /// <summary>
    /// Gets the state-transition features trie.
    /// </summary>
    public Trie<uint> StateTransFeatures { get; }

    /// <summary>
    /// Gets the feature weights (sparse matrix).
    /// </summary>
    public SparseMatrix<double> Weights { get; }

    /// <summary>
    /// Gets the state-transition weights (sparse matrix).
    /// </summary>
    public SparseMatrix<double> StateTransWeights { get; }

    /// <summary>
    /// Gets the transition weights (L×L dense matrix).
    /// </summary>
    public DenseMatrix TransWeights { get; }

    /// <summary>
    /// Gets the CRF inference context.
    /// </summary>
    public CrfContext Context { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Crf"/> class.
    /// </summary>
    /// <param name="classes">The class/label names.</param>
    public Crf(string[] classes)
    {
        ArgumentNullException.ThrowIfNull(classes);
        if (classes.Length == 0)
            throw new ArgumentException("Classes array cannot be empty.", nameof(classes));

        NumClasses = classes.Length;
        Classes = classes;

        StateFeatures = new Trie<uint>();
        StateTransFeatures = new Trie<uint>();
        Weights = new SparseMatrix<double>(rows: 10000, cols: NumClasses); // Initial capacity
        StateTransWeights = new SparseMatrix<double>(rows: 10000, cols: NumClasses * NumClasses);
        TransWeights = new DenseMatrix(NumClasses, NumClasses);
        Context = new CrfContext(NumClasses, numItems: 0);

        _nextFeatureId = 0;
        _nextTransFeatureId = 0;
    }

    /// <summary>
    /// Private constructor for loading from stream.
    /// </summary>
    private Crf(string[] classes, Trie<uint> stateFeatures, Trie<uint> stateTransFeatures,
        SparseMatrix<double> weights, SparseMatrix<double> stateTransWeights, DenseMatrix transWeights)
    {
        NumClasses = classes.Length;
        Classes = classes;
        StateFeatures = stateFeatures;
        StateTransFeatures = stateTransFeatures;
        Weights = weights;
        StateTransWeights = stateTransWeights;
        TransWeights = transWeights;
        Context = new CrfContext(NumClasses, numItems: 0);

        // Set feature IDs based on loaded tries
        _nextFeatureId = (uint)stateFeatures.Count;
        _nextTransFeatureId = (uint)stateTransFeatures.Count;
    }

    /// <summary>
    /// Adds a state feature and returns its ID.
    /// </summary>
    /// <param name="feature">The feature string.</param>
    /// <returns>The feature ID.</returns>
    public uint AddStateFeature(string feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (StateFeatures.TryGetData(feature, out var existingId))
        {
            return existingId;
        }

        var id = _nextFeatureId++;
        StateFeatures.Add(feature, id);
        return id;
    }

    /// <summary>
    /// Tries to get the ID for a state feature.
    /// </summary>
    /// <param name="feature">The feature string.</param>
    /// <param name="id">The feature ID if found.</param>
    /// <returns>True if found; otherwise, false.</returns>
    public bool TryGetStateFeatureId(string feature, out uint id)
    {
        ArgumentNullException.ThrowIfNull(feature);
        return StateFeatures.TryGetData(feature, out id);
    }

    /// <summary>
    /// Adds a state-transition feature and returns its ID.
    /// </summary>
    /// <param name="feature">The feature string.</param>
    /// <returns>The feature ID.</returns>
    public uint AddStateTransFeature(string feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (StateTransFeatures.TryGetData(feature, out var existingId))
        {
            return existingId;
        }

        var id = _nextTransFeatureId++;
        StateTransFeatures.Add(feature, id);
        return id;
    }

    /// <summary>
    /// Tries to get the ID for a state-transition feature.
    /// </summary>
    /// <param name="feature">The feature string.</param>
    /// <param name="id">The feature ID if found.</param>
    /// <returns>True if found; otherwise, false.</returns>
    public bool TryGetStateTransFeatureId(string feature, out uint id)
    {
        ArgumentNullException.ThrowIfNull(feature);
        return StateTransFeatures.TryGetData(feature, out id);
    }

    /// <summary>
    /// Sets the weight for a feature-class pair.
    /// </summary>
    /// <param name="featureId">The feature ID.</param>
    /// <param name="classId">The class ID.</param>
    /// <param name="weight">The weight value.</param>
    public void SetWeight(uint featureId, int classId, double weight)
    {
        if (classId < 0 || classId >= NumClasses)
            throw new ArgumentException($"Class ID {classId} is out of range [0, {NumClasses}).");

        Weights.SetValue((int)featureId, classId, weight);
    }

    /// <summary>
    /// Gets the weight for a feature-class pair.
    /// </summary>
    /// <param name="featureId">The feature ID.</param>
    /// <param name="classId">The class ID.</param>
    /// <returns>The weight value, or 0.0 if not set.</returns>
    public double GetWeight(uint featureId, int classId)
    {
        if (classId < 0 || classId >= NumClasses)
            throw new ArgumentException($"Class ID {classId} is out of range [0, {NumClasses}).");

        return Weights.GetValue((int)featureId, classId);
    }

    /// <summary>
    /// Sets the transition weight between two classes.
    /// </summary>
    /// <param name="fromClass">The source class ID.</param>
    /// <param name="toClass">The destination class ID.</param>
    /// <param name="weight">The transition weight.</param>
    public void SetTransWeight(int fromClass, int toClass, double weight)
    {
        if (fromClass < 0 || fromClass >= NumClasses)
            throw new ArgumentException(nameof(fromClass));
        if (toClass < 0 || toClass >= NumClasses)
            throw new ArgumentException(nameof(toClass));

        TransWeights[fromClass, toClass] = weight;
    }

    /// <summary>
    /// Gets the transition weight between two classes.
    /// </summary>
    /// <param name="fromClass">The source class ID.</param>
    /// <param name="toClass">The destination class ID.</param>
    /// <returns>The transition weight.</returns>
    public double GetTransWeight(int fromClass, int toClass)
    {
        if (fromClass < 0 || fromClass >= NumClasses)
            throw new ArgumentException(nameof(fromClass));
        if (toClass < 0 || toClass >= NumClasses)
            throw new ArgumentException(nameof(toClass));

        return TransWeights[fromClass, toClass];
    }

    /// <summary>
    /// Prepares the model for inference with the specified number of tokens.
    /// </summary>
    /// <param name="numTokens">The number of tokens.</param>
    public void PrepareForInference(int numTokens)
    {
        Context.SetNumItems(numTokens);
        Context.Reset();
    }

    /// <summary>
    /// Scores a token with the given features.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <param name="features">The feature strings.</param>
    /// <param name="prevTagFeatures">Previous tag features (optional).</param>
    public void ScoreToken(int tokenIndex, string[] features, string[]? prevTagFeatures)
    {
        ArgumentNullException.ThrowIfNull(features);

        // Score state features
        foreach (var feature in features)
        {
            if (TryGetStateFeatureId(feature, out var featureId))
            {
                for (int classId = 0; classId < NumClasses; classId++)
                {
                    var weight = GetWeight(featureId, classId);
                    Context.State[tokenIndex, classId] += weight;
                }
            }
        }

        // TODO: Score state-transition features (if prevTagFeatures provided)
        // This would be used for features that depend on the previous label
    }

    /// <summary>
    /// Runs Viterbi algorithm to predict the label sequence.
    /// </summary>
    /// <returns>Array of predicted label IDs.</returns>
    public uint[] Predict()
    {
        var labels = new uint[Context.NumItems];
        Context.Viterbi(labels);
        return labels;
    }

    /// <summary>
    /// Saves the CRF model to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var writer = new BigEndianBinaryWriter(stream);

        // Write signature
        writer.WriteUInt32(CrfSignature);

        // Write number of classes
        writer.WriteUInt32((uint)NumClasses);

        // Write classes (concatenated null-terminated strings with total length)
        var classesBytes = System.Text.Encoding.UTF8.GetBytes(string.Join("\0", Classes) + "\0");
        writer.WriteUInt64((ulong)classesBytes.Length);
        writer.WriteBytes(classesBytes);

        // Write state features trie
        StateFeatures.Save(stream);

        // Write weights sparse matrix (simplified - just save as trie entries)
        SaveSparseWeights(stream, Weights);

        // Write state-transition features trie
        StateTransFeatures.Save(stream);

        // Write state-transition weights
        SaveSparseWeights(stream, StateTransWeights);

        // Write transition weights (dense matrix)
        SaveDenseMatrix(stream, TransWeights);
    }

    /// <summary>
    /// Loads a CRF model from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>The loaded CRF model.</returns>
    public static Crf Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BigEndianBinaryReader(stream);

        // Read and validate signature
        var signature = reader.ReadUInt32();
        if (signature != CrfSignature)
        {
            throw new InvalidDataException(
                $"Invalid CRF signature. Expected 0x{CrfSignature:X8}, got 0x{signature:X8}.");
        }

        // Read number of classes
        var numClasses = (int)reader.ReadUInt32();

        // Read class names
        var classesLen = reader.ReadUInt64();
        var classesBytes = reader.ReadBytes((int)classesLen);
        var classesStr = System.Text.Encoding.UTF8.GetString(classesBytes);
        var classes = classesStr.Split('\0', StringSplitOptions.RemoveEmptyEntries);

        // Load all components using libpostal double-array trie format
        var stateFeatures = DoubleArrayTrieLoader.LoadLibpostalTrie<uint>(stream);

        var weights = LoadSparseWeightsFromStream(stream, numClasses);

        var stateTransFeatures = DoubleArrayTrieLoader.LoadLibpostalTrie<uint>(stream);

        var stateTransWeights = LoadSparseWeightsFromStream(stream, numClasses * numClasses);

        var transWeights = new DenseMatrix(numClasses, numClasses);
        LoadDenseMatrix(stream, transWeights);

        // Create model using private constructor
        return new Crf(classes, stateFeatures, stateTransFeatures, weights, stateTransWeights, transWeights);
    }

    private void SaveSparseWeights(Stream stream, SparseMatrix<double> matrix)
    {
        var (rowPtr, colIndices, values) = matrix.ToCSR();

        using var writer = new BigEndianBinaryWriter(stream);
        writer.WriteUInt32((uint)rowPtr.Length);
        foreach (var val in rowPtr)
            writer.WriteUInt32((uint)val);

        writer.WriteUInt32((uint)colIndices.Length);
        foreach (var val in colIndices)
            writer.WriteUInt32((uint)val);

        writer.WriteUInt32((uint)values.Length);
        foreach (var val in values)
            writer.WriteUInt64(BitConverter.DoubleToUInt64Bits(val));
    }

    /// <summary>
    /// Loads sparse weights from stream and creates matrix with correct size.
    /// Based on libpostal's sparse_matrix_read() in sparse_matrix.c lines 318-393.
    /// </summary>
    private static SparseMatrix<double> LoadSparseWeightsFromStream(Stream stream, int expectedCols)
    {
        using var reader = new BigEndianBinaryReader(stream);

        // Read dimensions (these are in the file!)
        var m = (int)reader.ReadUInt32();  // Number of rows
        var n = (int)reader.ReadUInt32();  // Number of columns

        // Read indptr array
        var indptrLen = reader.ReadUInt64();
        var rowPtr = new int[indptrLen];
        for (int i = 0; i < (int)indptrLen; i++)
            rowPtr[i] = (int)reader.ReadUInt32();

        // Read indices array
        var indicesLen = reader.ReadUInt64();
        var colIndices = new int[indicesLen];
        for (int i = 0; i < (int)indicesLen; i++)
            colIndices[i] = (int)reader.ReadUInt32();

        // Read data array
        var dataLen = reader.ReadUInt64();
        var values = new double[dataLen];
        for (int i = 0; i < (int)dataLen; i++)
            values[i] = BitConverter.UInt64BitsToDouble(reader.ReadUInt64());

        // Create matrix from CSR format
        var matrix = SparseMatrix<double>.FromCSR(m, n, rowPtr, colIndices, values);

        return matrix;
    }

    private static void LoadSparseWeights(Stream stream, SparseMatrix<double> matrix)
    {
        using var reader = new BigEndianBinaryReader(stream);

        var rowPtrLen = reader.ReadUInt32();
        var rowPtr = new int[rowPtrLen];
        for (int i = 0; i < rowPtrLen; i++)
            rowPtr[i] = (int)reader.ReadUInt32();

        var colIndicesLen = reader.ReadUInt32();
        var colIndices = new int[colIndicesLen];
        for (int i = 0; i < colIndicesLen; i++)
            colIndices[i] = (int)reader.ReadUInt32();

        var valuesLen = reader.ReadUInt32();
        var values = new double[valuesLen];
        for (int i = 0; i < valuesLen; i++)
            values[i] = BitConverter.UInt64BitsToDouble(reader.ReadUInt64());

        // Populate matrix from CSR format
        for (int row = 0; row < rowPtr.Length - 1; row++)
        {
            int start = rowPtr[row];
            int end = rowPtr[row + 1];
            for (int idx = start; idx < end; idx++)
            {
                int col = colIndices[idx];
                double value = values[idx];
                matrix.SetValue(row, col, value);
            }
        }
    }

    private void SaveDenseMatrix(Stream stream, DenseMatrix matrix)
    {
        using var writer = new BigEndianBinaryWriter(stream);

        writer.WriteUInt32((uint)matrix.Rows);
        writer.WriteUInt32((uint)matrix.Columns);

        for (int row = 0; row < matrix.Rows; row++)
        {
            for (int col = 0; col < matrix.Columns; col++)
            {
                writer.WriteUInt64(BitConverter.DoubleToUInt64Bits(matrix[row, col]));
            }
        }
    }

    private static void LoadDenseMatrix(Stream stream, DenseMatrix matrix)
    {
        using var reader = new BigEndianBinaryReader(stream);

        var rows = reader.ReadUInt32();
        var cols = reader.ReadUInt32();

        if (rows != matrix.Rows || cols != matrix.Columns)
        {
            matrix.Resize((int)rows, (int)cols);
        }

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                matrix[row, col] = BitConverter.UInt64BitsToDouble(reader.ReadUInt64());
            }
        }
    }

    /// <summary>
    /// Gets the CRF context for inference.
    /// </summary>
    /// <returns>The CRF context.</returns>
    public CrfContext GetContext()
    {
        return Context;
    }

    /// <summary>
    /// Disposes the CRF model.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            StateFeatures?.Dispose();
            StateTransFeatures?.Dispose();
            _disposed = true;
        }
    }
}
