namespace LibPostal.Net.Expansion;

/// <summary>
/// Represents a phrase found in tokenized text.
/// Based on libpostal's phrase_t structure.
/// </summary>
public record Phrase
{
    /// <summary>
    /// Gets the start index in the token array.
    /// </summary>
    public required int StartIndex { get; init; }

    /// <summary>
    /// Gets the number of tokens in the phrase.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Gets the phrase text (concatenated tokens).
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the end index in the token array (exclusive).
    /// </summary>
    public int EndIndex => StartIndex + Length;

    /// <summary>
    /// Gets the expansion alternatives for this phrase.
    /// </summary>
    public AddressExpansionValue? Expansions { get; init; }
}
