using LibPostal.Net.ML;

namespace LibPostal.Net.LanguageClassifier;

/// <summary>
/// Classifies the language of address text.
/// Based on libpostal's language_classifier.c
/// </summary>
public class LanguageClassifier
{
    private readonly LogisticRegression _classifier;
    private readonly LanguageFeatureExtractor _featureExtractor;
    private readonly Dictionary<string, int> _featureMap;
    private readonly int _vectorSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageClassifier"/> class.
    /// </summary>
    /// <param name="weights">The trained weight matrix.</param>
    /// <param name="labels">The language code labels.</param>
    /// <param name="featureMap">The feature name to index mapping.</param>
    public LanguageClassifier(
        SparseMatrix<double> weights,
        string[] labels,
        Dictionary<string, int> featureMap)
    {
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(featureMap);

        _classifier = new LogisticRegression(weights, labels);
        _featureExtractor = new LanguageFeatureExtractor(1, 2); // Unigrams and bigrams
        _featureMap = featureMap;
        _vectorSize = weights.Columns;
    }

    /// <summary>
    /// Classifies the language of the input text.
    /// </summary>
    /// <param name="text">The text to classify.</param>
    /// <param name="topK">The number of top languages to return (default: 3).</param>
    /// <returns>Array of language results ordered by confidence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    public LanguageResult[] ClassifyLanguage(string text, int topK = 3)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<LanguageResult>();
        }

        // Extract features
        var features = _featureExtractor.ExtractFeatures(text);

        // Convert to vector
        var featureVector = _featureExtractor.ToSparseVector(features, _featureMap, _vectorSize);

        // Classify
        var topLanguages = _classifier.PredictTopK(featureVector, topK);

        // Convert to results
        return topLanguages
            .Select(r => new LanguageResult
            {
                LanguageCode = r.label,
                Confidence = r.probability
            })
            .ToArray();
    }

    /// <summary>
    /// Gets the most likely language for the input text.
    /// </summary>
    /// <param name="text">The text to classify.</param>
    /// <returns>The most likely language result.</returns>
    public LanguageResult? GetMostLikelyLanguage(string text)
    {
        var results = ClassifyLanguage(text, topK: 1);
        return results.Length > 0 ? results[0] : null;
    }
}
