namespace LibPostal.Net.Parser;

/// <summary>
/// Tracks which tokens belong to which phrases.
/// Based on libpostal's token_phrase_memberships in address_parser.c
/// </summary>
public class PhraseMembership
{
    private readonly PhraseMatch?[] _assignments;

    /// <summary>
    /// Gets the number of tokens.
    /// </summary>
    public int TokenCount => _assignments.Length;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhraseMembership"/> class.
    /// </summary>
    /// <param name="tokenCount">The number of tokens.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when tokenCount is negative.</exception>
    public PhraseMembership(int tokenCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tokenCount);

        _assignments = new PhraseMatch?[tokenCount];
    }

    /// <summary>
    /// Assigns a phrase to its token range.
    /// If tokens are already assigned, the existing assignment is preserved (first wins).
    /// </summary>
    /// <param name="phrase">The phrase to assign.</param>
    public void AssignPhrase(PhraseMatch phrase)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        // Assign this phrase to all tokens in its range
        // But only if the token is not already assigned (first wins for overlaps)
        for (int i = phrase.StartIndex; i <= phrase.EndIndex && i < _assignments.Length; i++)
        {
            if (_assignments[i] == null)
            {
                _assignments[i] = phrase;
            }
        }
    }

    /// <summary>
    /// Gets the phrase assigned to a token at the specified index.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>The phrase match, or null if no phrase is assigned.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when tokenIndex is out of range.</exception>
    public PhraseMatch? GetPhraseAt(int tokenIndex)
    {
        if (tokenIndex < 0 || tokenIndex >= _assignments.Length)
            throw new ArgumentOutOfRangeException(nameof(tokenIndex));

        return _assignments[tokenIndex];
    }

    /// <summary>
    /// Determines whether a token is assigned to a phrase.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>True if the token has a phrase assignment; otherwise, false.</returns>
    public bool HasPhrase(int tokenIndex)
    {
        if (tokenIndex < 0 || tokenIndex >= _assignments.Length)
            return false;

        return _assignments[tokenIndex] != null;
    }

    /// <summary>
    /// Determines whether a token is the start of its assigned phrase.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>True if the token is the start of a phrase; otherwise, false.</returns>
    public bool IsStartOfPhrase(int tokenIndex)
    {
        var phrase = GetPhraseAt(tokenIndex);
        return phrase != null && phrase.StartIndex == tokenIndex;
    }

    /// <summary>
    /// Determines whether a token is the end of its assigned phrase.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>True if the token is the end of a phrase; otherwise, false.</returns>
    public bool IsEndOfPhrase(int tokenIndex)
    {
        var phrase = GetPhraseAt(tokenIndex);
        return phrase != null && phrase.EndIndex == tokenIndex;
    }

    /// <summary>
    /// Determines whether a token is in the middle of its assigned phrase.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>True if the token is in the middle (not start or end) of a phrase; otherwise, false.</returns>
    public bool IsMiddleOfPhrase(int tokenIndex)
    {
        var phrase = GetPhraseAt(tokenIndex);
        if (phrase == null || phrase.Length <= 2)
            return false;

        return tokenIndex > phrase.StartIndex && tokenIndex < phrase.EndIndex;
    }
}
