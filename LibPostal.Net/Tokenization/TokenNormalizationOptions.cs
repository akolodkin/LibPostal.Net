namespace LibPostal.Net.Tokenization;

/// <summary>
/// Options for token-level normalization.
/// Based on libpostal's NORMALIZE_TOKEN_* flags.
/// </summary>
[Flags]
public enum TokenNormalizationOptions
{
    /// <summary>
    /// No normalization
    /// </summary>
    None = 0,

    /// <summary>
    /// Delete hyphens from tokens
    /// </summary>
    DeleteHyphens = 1 << 0,

    /// <summary>
    /// Delete final period (e.g., "St." → "St")
    /// </summary>
    DeleteFinalPeriod = 1 << 1,

    /// <summary>
    /// Delete periods from acronyms (e.g., "U.S.A." → "USA")
    /// </summary>
    DeleteAcronymPeriods = 1 << 2,

    /// <summary>
    /// Delete possessive forms (e.g., "John's" → "John", "James'" → "James")
    /// </summary>
    DeletePossessive = 1 << 3,

    /// <summary>
    /// Delete apostrophes (e.g., "O'Malley" → "OMalley")
    /// </summary>
    DeleteApostrophe = 1 << 4,

    /// <summary>
    /// Split alpha from numeric characters (e.g., "4B" → "4 B")
    /// </summary>
    SplitAlphaNumeric = 1 << 5,

    /// <summary>
    /// Replace digits with 'D' placeholder (e.g., "123" → "DDD")
    /// </summary>
    ReplaceDigits = 1 << 6
}
