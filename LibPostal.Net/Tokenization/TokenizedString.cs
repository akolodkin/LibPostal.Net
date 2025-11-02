using System.Collections;

namespace LibPostal.Net.Tokenization;

/// <summary>
/// Represents a string that has been tokenized.
/// Contains the original string and the extracted tokens.
/// </summary>
/// <remarks>
/// Based on libpostal's tokenized_string_t structure.
/// This class provides convenient access to tokens and their metadata.
/// </remarks>
public sealed class TokenizedString : IReadOnlyList<Token>
{
    private readonly List<Token> _tokens;

    /// <summary>
    /// Gets the original string that was tokenized.
    /// </summary>
    public string OriginalString { get; }

    /// <summary>
    /// Gets the collection of tokens extracted from the original string.
    /// </summary>
    public IReadOnlyList<Token> Tokens => _tokens;

    /// <summary>
    /// Gets the number of tokens.
    /// </summary>
    public int Count => _tokens.Count;

    /// <summary>
    /// Gets a value indicating whether this tokenized string is empty (has no tokens).
    /// </summary>
    public bool IsEmpty => _tokens.Count == 0;

    /// <summary>
    /// Gets the token at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the token to get.</param>
    /// <returns>The token at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is out of range.</exception>
    public Token this[int index]
    {
        get
        {
            if (index < 0 || index >= _tokens.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {_tokens.Count - 1}.");

            return _tokens[index];
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizedString"/> class.
    /// </summary>
    /// <param name="originalString">The original string.</param>
    /// <param name="tokens">The collection of tokens.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="originalString"/> or <paramref name="tokens"/> is null.</exception>
    public TokenizedString(string originalString, IEnumerable<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(originalString);
        ArgumentNullException.ThrowIfNull(tokens);

        OriginalString = originalString;
        _tokens = tokens.ToList();
    }

    /// <summary>
    /// Gets all token strings (text values).
    /// </summary>
    /// <returns>A collection of token text values.</returns>
    public IReadOnlyList<string> GetTokenStrings()
    {
        return _tokens.Select(t => t.Text).ToList();
    }

    /// <summary>
    /// Gets all tokens excluding whitespace and newline tokens.
    /// </summary>
    /// <returns>A collection of non-whitespace tokens.</returns>
    public IReadOnlyList<Token> GetTokensWithoutWhitespace()
    {
        return _tokens.Where(t => t.Type != TokenType.Whitespace && t.Type != TokenType.Newline).ToList();
    }

    /// <summary>
    /// Gets all tokens of a specific type.
    /// </summary>
    /// <param name="type">The token type to filter by.</param>
    /// <returns>A collection of tokens matching the specified type.</returns>
    public IReadOnlyList<Token> GetTokensByType(TokenType type)
    {
        return _tokens.Where(t => t.Type == type).ToList();
    }

    /// <summary>
    /// Returns the original string.
    /// </summary>
    /// <returns>The original string that was tokenized.</returns>
    public override string ToString() => OriginalString;

    /// <summary>
    /// Returns an enumerator that iterates through the tokens.
    /// </summary>
    /// <returns>An enumerator for the tokens.</returns>
    public IEnumerator<Token> GetEnumerator() => _tokens.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the tokens.
    /// </summary>
    /// <returns>An enumerator for the tokens.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
