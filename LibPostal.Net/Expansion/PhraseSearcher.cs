using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Expansion;

/// <summary>
/// Searches for phrases in tokenized text using dictionary lookups.
/// Based on libpostal's phrase search logic.
/// </summary>
public class PhraseSearcher
{
    private readonly Dictionary<string, AddressExpansionValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhraseSearcher"/> class.
    /// </summary>
    /// <param name="dictionary">The expansion dictionary.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is null.</exception>
    public PhraseSearcher(Dictionary<string, AddressExpansionValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        _dictionary = dictionary;
    }

    /// <summary>
    /// Searches for phrases in the tokenized string.
    /// </summary>
    /// <param name="tokenizedString">The tokenized string to search.</param>
    /// <returns>A list of phrases found in the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenizedString"/> is null.</exception>
    public List<Phrase> SearchTokens(TokenizedString tokenizedString)
    {
        ArgumentNullException.ThrowIfNull(tokenizedString);

        var phrases = new List<Phrase>();

        // Try all possible phrase lengths starting from each position
        for (int start = 0; start < tokenizedString.Count; start++)
        {
            // Try increasingly longer phrases
            for (int length = 1; length <= tokenizedString.Count - start; length++)
            {
                var phrase = ExtractPhrase(tokenizedString, start, length);
                if (phrase != null)
                {
                    phrases.Add(phrase);
                }
            }
        }

        return phrases;
    }

    private Phrase? ExtractPhrase(TokenizedString tokenizedString, int start, int length)
    {
        // Build the phrase key (concatenate token texts)
        var phraseTokens = new List<string>();

        for (int i = start; i < start + length && i < tokenizedString.Count; i++)
        {
            phraseTokens.Add(tokenizedString[i].Text);
        }

        var phraseValue = string.Concat(phraseTokens);
        var normalizedKey = phraseValue.ToLowerInvariant(); // Normalize for lookup

        // Look up in dictionary
        if (_dictionary.TryGetValue(normalizedKey, out var expansions))
        {
            return new Phrase
            {
                StartIndex = start,
                Length = length,
                Value = phraseValue,
                Expansions = expansions
            };
        }

        return null;
    }
}
