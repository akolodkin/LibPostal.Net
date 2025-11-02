namespace LibPostal.Net.Tokenization;

/// <summary>
/// Options for string normalization.
/// Based on libpostal's NORMALIZE_STRING_* flags.
/// </summary>
[Flags]
public enum NormalizationOptions
{
    /// <summary>
    /// No normalization
    /// </summary>
    None = 0,

    /// <summary>
    /// Convert to lowercase
    /// </summary>
    Lowercase = 1 << 0,

    /// <summary>
    /// Trim leading/trailing whitespace
    /// </summary>
    Trim = 1 << 1,

    /// <summary>
    /// Strip diacritical marks/accents
    /// </summary>
    StripAccents = 1 << 2,

    /// <summary>
    /// Unicode NFD (decompose) normalization
    /// </summary>
    Decompose = 1 << 3,

    /// <summary>
    /// Unicode NFC (compose) normalization
    /// </summary>
    Compose = 1 << 4,

    /// <summary>
    /// Replace hyphens with spaces
    /// </summary>
    ReplaceHyphens = 1 << 5
}
