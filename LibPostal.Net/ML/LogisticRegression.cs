namespace LibPostal.Net.ML;

/// <summary>
/// Multi-class logistic regression classifier.
/// Based on libpostal's logistic_regression.c
/// </summary>
public class LogisticRegression
{
    private readonly SparseMatrix<double> _weights;
    private readonly string[] _labels;

    /// <summary>
    /// Gets the number of classes.
    /// </summary>
    public int NumClasses => _weights.Rows;

    /// <summary>
    /// Gets the number of features.
    /// </summary>
    public int NumFeatures => _weights.Columns;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogisticRegression"/> class.
    /// </summary>
    /// <param name="weights">The weight matrix (classes x features).</param>
    /// <param name="labels">The class labels.</param>
    public LogisticRegression(SparseMatrix<double> weights, string[] labels)
    {
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(labels);

        _weights = weights;
        _labels = labels;
    }

    /// <summary>
    /// Predicts the class for a feature vector.
    /// </summary>
    /// <param name="features">The feature vector.</param>
    /// <returns>The predicted class index.</returns>
    public int Predict(double[] features)
    {
        var probabilities = PredictProba(features);

        int maxIndex = 0;
        double maxProb = probabilities[0];

        for (int i = 1; i < probabilities.Length; i++)
        {
            if (probabilities[i] > maxProb)
            {
                maxProb = probabilities[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    /// <summary>
    /// Predicts probabilities for all classes.
    /// </summary>
    /// <param name="features">The feature vector.</param>
    /// <returns>Probability distribution over classes.</returns>
    public double[] PredictProba(double[] features)
    {
        // Compute scores: weights * features
        var scores = _weights.MultiplyVector(features);

        // Apply softmax to convert to probabilities
        return Softmax(scores);
    }

    /// <summary>
    /// Predicts the class and returns the label with probability.
    /// </summary>
    /// <param name="features">The feature vector.</param>
    /// <returns>Tuple of (label, probability).</returns>
    public (string label, double probability) PredictWithLabel(double[] features)
    {
        var probabilities = PredictProba(features);
        var classIndex = Predict(features);

        return (_labels[classIndex], probabilities[classIndex]);
    }

    /// <summary>
    /// Predicts the top-k most likely classes.
    /// </summary>
    /// <param name="features">The feature vector.</param>
    /// <param name="k">The number of top classes to return.</param>
    /// <returns>List of (label, probability) tuples ordered by probability.</returns>
    public List<(string label, double probability)> PredictTopK(double[] features, int k)
    {
        var probabilities = PredictProba(features);

        var results = new List<(int index, double prob)>();
        for (int i = 0; i < probabilities.Length; i++)
        {
            results.Add((i, probabilities[i]));
        }

        return results
            .OrderByDescending(r => r.prob)
            .Take(k)
            .Select(r => (_labels[r.index], r.prob))
            .ToList();
    }

    /// <summary>
    /// Applies softmax to convert scores to probabilities.
    /// </summary>
    /// <param name="scores">The class scores.</param>
    /// <returns>Probability distribution (sums to 1).</returns>
    public static double[] Softmax(double[] scores)
    {
        // Find max for numerical stability
        double max = scores.Max();

        // Compute exp(score - max)
        var exp = new double[scores.Length];
        double sum = 0.0;

        for (int i = 0; i < scores.Length; i++)
        {
            exp[i] = Math.Exp(scores[i] - max);
            sum += exp[i];
        }

        // Normalize
        var probabilities = new double[scores.Length];
        for (int i = 0; i < scores.Length; i++)
        {
            probabilities[i] = exp[i] / sum;
        }

        return probabilities;
    }
}
