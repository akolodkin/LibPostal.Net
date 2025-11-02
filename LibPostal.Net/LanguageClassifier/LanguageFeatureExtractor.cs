namespace LibPostal.Net.LanguageClassifier;

/// <summary>
/// Extracts character n-gram features for language classification.
/// Based on libpostal's language_features.c
/// </summary>
public class LanguageFeatureExtractor
{
    private readonly int[] _ngramSizes;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageFeatureExtractor"/> class.
    /// </summary>
    /// <param name="ngramSizes">The n-gram sizes to extract (default: 1, 2).</param>
    public LanguageFeatureExtractor(params int[] ngramSizes)
    {
        _ngramSizes = ngramSizes.Length > 0 ? ngramSizes : new[] { 1, 2 };
    }

    /// <summary>
    /// Extracts character n-gram features from text.
    /// </summary>
    /// <param name="text">The text to extract features from.</param>
    /// <returns>Dictionary of n-gram features with their frequencies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    public Dictionary<string, int> ExtractFeatures(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text))
        {
            return new Dictionary<string, int>();
        }

        // Normalize to lowercase for consistency
        var normalized = text.ToLowerInvariant();

        var features = new Dictionary<string, int>();

        // Extract n-grams of each configured size
        foreach (var n in _ngramSizes)
        {
            ExtractNgrams(normalized, n, features);
        }

        return features;
    }

    private void ExtractNgrams(string text, int n, Dictionary<string, int> features)
    {
        for (int i = 0; i <= text.Length - n; i++)
        {
            var ngram = text.Substring(i, n);

            if (features.ContainsKey(ngram))
            {
                features[ngram]++;
            }
            else
            {
                features[ngram] = 1;
            }
        }
    }

    /// <summary>
    /// Converts feature dictionary to a sparse vector using a feature map.
    /// </summary>
    /// <param name="features">The feature dictionary.</param>
    /// <param name="featureMap">Mapping from feature names to indices.</param>
    /// <param name="vectorSize">The size of the output vector.</param>
    /// <returns>A sparse vector with feature counts.</returns>
    public double[] ToSparseVector(
        Dictionary<string, int> features,
        Dictionary<string, int> featureMap,
        int vectorSize)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(featureMap);

        var vector = new double[vectorSize];

        foreach (var (feature, count) in features)
        {
            if (featureMap.TryGetValue(feature, out var index))
            {
                if (index >= 0 && index < vectorSize)
                {
                    vector[index] = count;
                }
            }
        }

        return vector;
    }
}
