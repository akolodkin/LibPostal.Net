namespace LibPostal.Net.Tokenization;

/// <summary>
/// Represents a token extracted from text.
/// Tokens are the basic units of text used in address parsing.
/// </summary>
/// <remarks>
/// This struct uses UTF-16 offsets (native .NET string indexing).
/// For UTF-8 byte offsets, use helper methods on the tokenizer.
/// </remarks>
public readonly record struct Token
{
    /// <summary>
    /// Gets the token text.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// Gets the token type.
    /// </summary>
    public TokenType Type { get; init; }

    /// <summary>
    /// Gets the offset (position) of the token in the original text (UTF-16 code units).
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Gets the length of the token in the original text (UTF-16 code units).
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// Gets the end position of the token (Offset + Length).
    /// </summary>
    public int End => Offset + Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="Token"/> struct.
    /// </summary>
    /// <param name="text">The token text.</param>
    /// <param name="type">The token type.</param>
    /// <param name="offset">The offset in the original text.</param>
    /// <param name="length">The length of the token.</param>
    public Token(string text, TokenType type, int offset, int length)
    {
        Text = text;
        Type = type;
        Offset = offset;
        Length = length;
    }

    /// <summary>
    /// Returns a string representation of the token.
    /// </summary>
    /// <returns>A string in the format: "Type: 'Text' [Offset:End]"</returns>
    public override string ToString() => $"{Type}: '{Text}' [{Offset}:{End}]";
}
