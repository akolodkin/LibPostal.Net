namespace LibPostal.Net.LanguageClassifier;

/// <summary>
/// Represents a language classification result.
/// </summary>
public record LanguageResult : IComparable<LanguageResult>
{
    /// <summary>
    /// Gets the ISO 639-1 language code.
    /// </summary>
    public required string LanguageCode { get; init; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// Compares by confidence (descending order).
    /// </summary>
    public int CompareTo(LanguageResult? other)
    {
        if (other == null) return 1;
        return other.Confidence.CompareTo(Confidence); // Descending
    }

    /// <summary>
    /// Returns a string representation.
    /// </summary>
    public override string ToString() => $"{LanguageCode}: {Confidence:F4}";
}
