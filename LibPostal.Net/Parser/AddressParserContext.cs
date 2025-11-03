using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Parser;

/// <summary>
/// Manages state and phrase memberships during address parsing.
/// Based on libpostal's address_parser_context_t in address_parser.c
/// </summary>
public class AddressParserContext
{
    private readonly TokenizedString _tokenized;
    private readonly AddressParserModel _model;
    private PhraseMembership? _dictionaryPhraseMembership;
    private PhraseMembership? _componentPhraseMembership;
    private PhraseMembership? _postalCodePhraseMembership;

    /// <summary>
    /// Gets the number of tokens.
    /// </summary>
    public int TokenCount => _tokenized.Count;

    /// <summary>
    /// Gets the tokenized string.
    /// </summary>
    public TokenizedString Tokenized => _tokenized;

    /// <summary>
    /// Gets the address parser model.
    /// </summary>
    public AddressParserModel Model => _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressParserContext"/> class.
    /// </summary>
    /// <param name="tokenized">The tokenized address string.</param>
    /// <param name="model">The address parser model.</param>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
    public AddressParserContext(TokenizedString tokenized, AddressParserModel model)
    {
        ArgumentNullException.ThrowIfNull(tokenized);
        ArgumentNullException.ThrowIfNull(model);

        _tokenized = tokenized;
        _model = model;
    }

    /// <summary>
    /// Fills phrase memberships by searching for dictionary, component, and postal code phrases.
    /// </summary>
    public void FillPhrases()
    {
        var tokens = _tokenized.ToArray();

        // Initialize memberships
        _dictionaryPhraseMembership = new PhraseMembership(tokens.Length);
        _componentPhraseMembership = new PhraseMembership(tokens.Length);
        _postalCodePhraseMembership = new PhraseMembership(tokens.Length);

        // Search for dictionary phrases
        if (_model.Phrases != null)
        {
            var dictionaryMatcher = new PhraseMatcher(_model.Phrases);

            for (int i = 0; i < tokens.Length; i++)
            {
                var matches = dictionaryMatcher.SearchTokens(tokens, i, normalized: true);

                foreach (var match in matches)
                {
                    _dictionaryPhraseMembership.AssignPhrase(match);
                }
            }
        }

        // Search for component phrases (cities, states, etc.)
        if (_model.ComponentPhrases != null)
        {
            var componentMatcher = new PhraseMatcher(_model.ComponentPhrases);

            for (int i = 0; i < tokens.Length; i++)
            {
                var matches = componentMatcher.SearchTokens(tokens, i, normalized: true);

                foreach (var match in matches)
                {
                    _componentPhraseMembership.AssignPhrase(match);
                }
            }
        }

        // Search for postal code phrases
        if (_model.PostalCodes != null)
        {
            var postalCodeMatcher = new PhraseMatcher(_model.PostalCodes);

            for (int i = 0; i < tokens.Length; i++)
            {
                var matches = postalCodeMatcher.SearchTokens(tokens, i, normalized: false);

                foreach (var match in matches)
                {
                    _postalCodePhraseMembership.AssignPhrase(match);
                }
            }
        }
    }

    /// <summary>
    /// Gets the dictionary phrase at the specified token index.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>The phrase match, or null if no phrase at this position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when tokenIndex is out of range.</exception>
    public PhraseMatch? GetDictionaryPhraseAt(int tokenIndex)
    {
        if (tokenIndex < 0 || tokenIndex >= TokenCount)
            throw new ArgumentOutOfRangeException(nameof(tokenIndex));

        if (_dictionaryPhraseMembership == null)
            return null;

        return _dictionaryPhraseMembership.GetPhraseAt(tokenIndex);
    }

    /// <summary>
    /// Gets the component phrase at the specified token index.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>The phrase match, or null if no phrase at this position.</returns>
    public PhraseMatch? GetComponentPhraseAt(int tokenIndex)
    {
        if (_componentPhraseMembership == null)
            return null;

        return _componentPhraseMembership.GetPhraseAt(tokenIndex);
    }

    /// <summary>
    /// Gets the postal code phrase at the specified token index.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>The phrase match, or null if no phrase at this position.</returns>
    public PhraseMatch? GetPostalCodePhraseAt(int tokenIndex)
    {
        if (_postalCodePhraseMembership == null)
            return null;

        return _postalCodePhraseMembership.GetPhraseAt(tokenIndex);
    }

    /// <summary>
    /// Determines whether a token has a dictionary phrase.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>True if the token has a dictionary phrase; otherwise, false.</returns>
    public bool HasDictionaryPhrase(int tokenIndex)
    {
        if (_dictionaryPhraseMembership == null)
            return false;

        return _dictionaryPhraseMembership.HasPhrase(tokenIndex);
    }

    /// <summary>
    /// Determines whether a token has a component phrase.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>True if the token has a component phrase; otherwise, false.</returns>
    public bool HasComponentPhrase(int tokenIndex)
    {
        if (_componentPhraseMembership == null)
            return false;

        return _componentPhraseMembership.HasPhrase(tokenIndex);
    }

    /// <summary>
    /// Determines whether a token has a postal code phrase.
    /// </summary>
    /// <param name="tokenIndex">The token index.</param>
    /// <returns>True if the token has a postal code phrase; otherwise, false.</returns>
    public bool HasPostalCodePhrase(int tokenIndex)
    {
        if (_postalCodePhraseMembership == null)
            return false;

        return _postalCodePhraseMembership.HasPhrase(tokenIndex);
    }
}
