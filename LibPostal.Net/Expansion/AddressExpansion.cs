namespace LibPostal.Net.Expansion;

/// <summary>
/// Represents an address expansion alternative.
/// Based on libpostal's address_expansion_t structure.
/// </summary>
public record AddressExpansion
{
    /// <summary>
    /// Gets the canonical (full) form of the phrase.
    /// Null if this is the canonical form itself.
    /// </summary>
    public string? Canonical { get; init; }

    /// <summary>
    /// Gets the ISO language code (e.g., "en", "fr", "de").
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Gets the valid address components for this expansion.
    /// </summary>
    public required AddressComponent Components { get; init; }

    /// <summary>
    /// Gets the dictionary type that contains this expansion.
    /// </summary>
    public required DictionaryType DictionaryType { get; init; }

    /// <summary>
    /// Gets a value indicating whether this expansion can be separated with spaces.
    /// </summary>
    public required bool IsSeparable { get; init; }
}
