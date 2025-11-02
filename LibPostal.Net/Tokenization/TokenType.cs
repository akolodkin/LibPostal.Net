namespace LibPostal.Net.Tokenization;

/// <summary>
/// Represents the type of a token in address text.
/// Based on libpostal's token_types.h
/// </summary>
/// <remarks>
/// libpostal defines 38+ distinct token types for precise address parsing.
/// This enum mirrors that classification system.
/// </remarks>
public enum TokenType
{
    /// <summary>
    /// Invalid or unrecognized character
    /// </summary>
    InvalidChar = 0,

    // Word types

    /// <summary>
    /// Standard word (sequence of letters)
    /// </summary>
    Word,

    /// <summary>
    /// Abbreviation (e.g., "St" for "Street")
    /// </summary>
    Abbreviation,

    /// <summary>
    /// Ideographic character (CJK characters)
    /// </summary>
    IdeographicChar,

    /// <summary>
    /// Hangul syllable (Korean characters)
    /// </summary>
    HangulSyllable,

    /// <summary>
    /// Acronym (e.g., "U.S.A." or "NATO")
    /// </summary>
    Acronym,

    /// <summary>
    /// Multi-word phrase
    /// </summary>
    Phrase,

    // Special patterns

    /// <summary>
    /// Email address
    /// </summary>
    Email,

    /// <summary>
    /// URL/website address
    /// </summary>
    Url,

    /// <summary>
    /// US phone number format
    /// </summary>
    UsPhone,

    /// <summary>
    /// International phone number format
    /// </summary>
    InternationalPhone,

    // Numeric types

    /// <summary>
    /// Numeric value (digits)
    /// </summary>
    Numeric,

    /// <summary>
    /// Ordinal number (1st, 2nd, 3rd, etc.)
    /// </summary>
    Ordinal,

    /// <summary>
    /// Roman numeral (I, II, III, IV, etc.)
    /// </summary>
    RomanNumeral,

    /// <summary>
    /// Ideographic number (e.g., Chinese numerals)
    /// </summary>
    IdeographicNumber,

    // Punctuation types (24 distinct types)

    /// <summary>
    /// Period (.)
    /// </summary>
    Period,

    /// <summary>
    /// Comma (,)
    /// </summary>
    Comma,

    /// <summary>
    /// Semicolon (;)
    /// </summary>
    Semicolon,

    /// <summary>
    /// Colon (:)
    /// </summary>
    Colon,

    /// <summary>
    /// Exclamation mark (!)
    /// </summary>
    Exclamation,

    /// <summary>
    /// Question mark (?)
    /// </summary>
    Question,

    /// <summary>
    /// Single quote (')
    /// </summary>
    Quote,

    /// <summary>
    /// Double quote (")
    /// </summary>
    DoubleQuote,

    /// <summary>
    /// Left parenthesis
    /// </summary>
    LeftParen,

    /// <summary>
    /// Right parenthesis
    /// </summary>
    RightParen,

    /// <summary>
    /// Left bracket
    /// </summary>
    LeftBracket,

    /// <summary>
    /// Right bracket
    /// </summary>
    RightBracket,

    /// <summary>
    /// Left brace
    /// </summary>
    LeftBrace,

    /// <summary>
    /// Right brace
    /// </summary>
    RightBrace,

    /// <summary>
    /// Dash (en-dash, em-dash)
    /// </summary>
    Dash,

    /// <summary>
    /// Hyphen
    /// </summary>
    Hyphen,

    /// <summary>
    /// Underscore
    /// </summary>
    Underscore,

    /// <summary>
    /// Plus sign
    /// </summary>
    Plus,

    /// <summary>
    /// Ampersand
    /// </summary>
    Ampersand,

    /// <summary>
    /// At symbol
    /// </summary>
    At,

    /// <summary>
    /// Pound/hash symbol
    /// </summary>
    Pound,

    /// <summary>
    /// Dollar sign
    /// </summary>
    Dollar,

    /// <summary>
    /// Percent sign
    /// </summary>
    Percent,

    /// <summary>
    /// Asterisk
    /// </summary>
    Asterisk,

    // Whitespace

    /// <summary>
    /// Whitespace (spaces, tabs)
    /// </summary>
    Whitespace,

    /// <summary>
    /// Newline character
    /// </summary>
    Newline,

    // Catchall

    /// <summary>
    /// Other/unknown token type
    /// </summary>
    Other
}
