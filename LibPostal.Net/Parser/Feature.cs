namespace LibPostal.Net.Parser;

/// <summary>
/// Represents a feature extracted from an address token.
/// Based on libpostal's feature representation.
/// </summary>
public record Feature
{
    /// <summary>
    /// Gets the feature name (e.g., "word=main", "is_numeric").
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the feature value (typically 1.0 for binary features).
    /// </summary>
    public double Value { get; init; } = 1.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="Feature"/> record.
    /// </summary>
    /// <param name="name">The feature name.</param>
    /// <param name="value">The feature value (default: 1.0).</param>
    public Feature(string name, double value = 1.0)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Returns the feature name.
    /// </summary>
    public override string ToString() => Name;
}
