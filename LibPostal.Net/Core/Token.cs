namespace LibPostal.Net.Core;

/// <summary>
/// Represents the type of a token in address text.
/// Based on libpostal's token types.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Whitespace token (spaces, tabs, etc.)
    /// </summary>
    Whitespace,

    /// <summary>
    /// Punctuation token (commas, periods, etc.)
    /// </summary>
    Punctuation,

    /// <summary>
    /// Numeric token (digits)
    /// </summary>
    Numeric,

    /// <summary>
    /// Alphabetic token (letters)
    /// </summary>
    Alphabetic,

    /// <summary>
    /// Alphanumeric token (letters and digits)
    /// </summary>
    Alphanumeric,

    /// <summary>
    /// Ideographic token (CJK characters, etc.)
    /// </summary>
    Ideographic,

    /// <summary>
    /// Hangul token (Korean characters)
    /// </summary>
    Hangul,

    /// <summary>
    /// Arabic token
    /// </summary>
    Arabic,

    /// <summary>
    /// Hebrew token
    /// </summary>
    Hebrew,

    /// <summary>
    /// Greek token
    /// </summary>
    Greek,

    /// <summary>
    /// Cyrillic token
    /// </summary>
    Cyrillic,

    /// <summary>
    /// Latin token
    /// </summary>
    Latin,

    /// <summary>
    /// Other/unknown token type
    /// </summary>
    Other
}

/// <summary>
/// Represents a token extracted from address text.
/// Tokens are the basic units of text used in address parsing.
/// </summary>
public readonly record struct Token
{
    /// <summary>
    /// Gets the token text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the token type.
    /// </summary>
    public required TokenType Type { get; init; }

    /// <summary>
    /// Gets the start position of the token in the original text.
    /// </summary>
    public required int Start { get; init; }

    /// <summary>
    /// Gets the length of the token.
    /// </summary>
    public required int Length { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> struct.
    /// </summary>
    /// <param name="text">The token text.</param>
    /// <param name="type">The token type.</param>
    /// <param name="start">The start position.</param>
    /// <param name="length">The length.</param>
    public Token(string text, TokenType type, int start, int length)
    {
        Text = text;
        Type = type;
        Start = start;
        Length = length;
    }

    /// <summary>
    /// Returns a string representation of the token.
    /// </summary>
    public override string ToString() => $"{Type}: \"{Text}\" [{Start}:{Start + Length}]";
}
