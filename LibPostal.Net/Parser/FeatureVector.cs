namespace LibPostal.Net.Parser;

/// <summary>
/// Represents a collection of features extracted from a token.
/// </summary>
public class FeatureVector
{
    private readonly List<string> _features;

    /// <summary>
    /// Gets the number of features.
    /// </summary>
    public int Count => _features.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureVector"/> class.
    /// </summary>
    public FeatureVector()
    {
        _features = new List<string>();
    }

    /// <summary>
    /// Adds a feature to the vector.
    /// </summary>
    /// <param name="name">The feature name.</param>
    /// <param name="value">The feature value (default: 1.0).</param>
    public void Add(string name, double value = 1.0)
    {
        ArgumentNullException.ThrowIfNull(name);
        _features.Add(name);
    }

    /// <summary>
    /// Determines whether the vector contains the specified feature.
    /// </summary>
    /// <param name="name">The feature name.</param>
    /// <returns>True if the feature is present; otherwise, false.</returns>
    public bool Contains(string name)
    {
        return _features.Contains(name);
    }

    /// <summary>
    /// Converts the feature vector to an array of feature names.
    /// </summary>
    /// <returns>Array of feature names.</returns>
    public string[] ToArray()
    {
        return _features.ToArray();
    }

    /// <summary>
    /// Clears all features from the vector.
    /// </summary>
    public void Clear()
    {
        _features.Clear();
    }
}
