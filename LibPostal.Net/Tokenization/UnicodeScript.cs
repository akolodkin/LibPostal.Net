namespace LibPostal.Net.Tokenization;

/// <summary>
/// Represents Unicode script categories.
/// Based on libpostal's unicode_scripts support.
/// </summary>
public enum UnicodeScript
{
    /// <summary>
    /// Unknown or unrecognized script
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Latin script (a-z, A-Z, Latin Extended)
    /// </summary>
    Latin,

    /// <summary>
    /// Cyrillic script (Russian, Ukrainian, Bulgarian, etc.)
    /// </summary>
    Cyrillic,

    /// <summary>
    /// Arabic script
    /// </summary>
    Arabic,

    /// <summary>
    /// Hebrew script
    /// </summary>
    Hebrew,

    /// <summary>
    /// Greek script
    /// </summary>
    Greek,

    /// <summary>
    /// Han (CJK Ideographs - Chinese, Japanese, Korean)
    /// </summary>
    Han,

    /// <summary>
    /// Hangul (Korean syllabic script)
    /// </summary>
    Hangul,

    /// <summary>
    /// Hiragana (Japanese)
    /// </summary>
    Hiragana,

    /// <summary>
    /// Katakana (Japanese)
    /// </summary>
    Katakana,

    /// <summary>
    /// Thai script
    /// </summary>
    Thai,

    /// <summary>
    /// Devanagari (Hindi, Sanskrit, etc.)
    /// </summary>
    Devanagari
}
