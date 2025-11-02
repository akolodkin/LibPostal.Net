namespace LibPostal.Net.ML;

/// <summary>
/// CRF context for sequence labeling with Viterbi algorithm.
/// Based on libpostal's crf_context.c (lines 567-671).
/// </summary>
public class CrfContext
{
    /// <summary>
    /// Gets the number of labels (L).
    /// </summary>
    public int NumLabels { get; private set; }

    /// <summary>
    /// Gets the number of items/tokens (T).
    /// </summary>
    public int NumItems { get; private set; }

    /// <summary>
    /// State scores matrix (T×L): scores for each label at each position.
    /// </summary>
    public DenseMatrix State { get; private set; }

    /// <summary>
    /// Transition weights matrix (L×L): transition scores between labels.
    /// </summary>
    public DenseMatrix Trans { get; private set; }

    /// <summary>
    /// Alpha scores for Viterbi (T×L): cumulative max scores.
    /// </summary>
    public DenseMatrix AlphaScore { get; private set; }

    /// <summary>
    /// Backward edges for Viterbi (T×L): best previous label for backtracking.
    /// </summary>
    private uint[,] _backwardEdges;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrfContext"/> class.
    /// </summary>
    /// <param name="numLabels">The number of labels (L).</param>
    /// <param name="numItems">The number of items/tokens (T).</param>
    public CrfContext(int numLabels, int numItems)
    {
        NumLabels = numLabels;
        NumItems = numItems;

        State = new DenseMatrix(numItems, numLabels);
        Trans = new DenseMatrix(numLabels, numLabels);
        AlphaScore = new DenseMatrix(numItems, numLabels);
        _backwardEdges = new uint[numItems, numLabels];
    }

    /// <summary>
    /// Resizes the context to accommodate a different number of items.
    /// </summary>
    /// <param name="newNumItems">The new number of items.</param>
    public void SetNumItems(int newNumItems)
    {
        if (newNumItems == NumItems)
            return;

        State.Resize(newNumItems, NumLabels);
        AlphaScore.Resize(newNumItems, NumLabels);

        _backwardEdges = new uint[newNumItems, NumLabels];
        NumItems = newNumItems;
    }

    /// <summary>
    /// Resets the context by clearing all scores.
    /// </summary>
    public void Reset()
    {
        State.Zero();
        AlphaScore.Zero();
        Array.Clear(_backwardEdges, 0, _backwardEdges.Length);
    }

    /// <summary>
    /// Runs the Viterbi algorithm to find the optimal label sequence.
    /// </summary>
    /// <param name="labels">Output array for the optimal label sequence.</param>
    /// <returns>The score of the optimal path.</returns>
    public double Viterbi(uint[] labels)
    {
        if (labels.Length != NumItems)
            throw new ArgumentException($"Labels array length {labels.Length} must match NumItems {NumItems}.");

        if (NumItems == 0)
            return 0.0;

        // Initialize alpha scores for first position
        for (int label = 0; label < NumLabels; label++)
        {
            AlphaScore[0, label] = State[0, label];
        }

        // Forward pass: compute max scores for each position and label
        for (int t = 1; t < NumItems; t++)
        {
            for (int currLabel = 0; currLabel < NumLabels; currLabel++)
            {
                double maxScore = double.NegativeInfinity;
                uint bestPrevLabel = 0;

                // Find best previous label
                for (int prevLabel = 0; prevLabel < NumLabels; prevLabel++)
                {
                    double score = AlphaScore[t - 1, prevLabel] + Trans[prevLabel, currLabel];

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestPrevLabel = (uint)prevLabel;
                    }
                }

                // Store cumulative score and best predecessor
                AlphaScore[t, currLabel] = maxScore + State[t, currLabel];
                _backwardEdges[t, currLabel] = bestPrevLabel;
            }
        }

        // Find best final label
        double bestFinalScore = double.NegativeInfinity;
        uint bestFinalLabel = 0;

        for (int label = 0; label < NumLabels; label++)
        {
            if (AlphaScore[NumItems - 1, label] > bestFinalScore)
            {
                bestFinalScore = AlphaScore[NumItems - 1, label];
                bestFinalLabel = (uint)label;
            }
        }

        // Backtrack to recover the optimal path
        labels[NumItems - 1] = bestFinalLabel;

        for (int t = NumItems - 2; t >= 0; t--)
        {
            labels[t] = _backwardEdges[t + 1, labels[t + 1]];
        }

        return bestFinalScore;
    }
}
