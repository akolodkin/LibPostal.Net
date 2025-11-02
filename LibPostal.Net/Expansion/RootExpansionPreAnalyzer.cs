using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Expansion;

/// <summary>
/// Result of the pre-analysis pass for root expansion.
/// </summary>
public class PreAnalysisResult
{
    /// <summary>
    /// Gets a value indicating whether there are tokens not covered by any phrase.
    /// </summary>
    public required bool HaveNonPhraseTokens { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are word tokens not covered by any phrase.
    /// </summary>
    public required bool HaveNonPhraseWordTokens { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are phrases with canonical expansions.
    /// </summary>
    public required bool HaveCanonicalPhrases { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are ambiguous phrases.
    /// </summary>
    public required bool HaveAmbiguous { get; init; }

    /// <summary>
    /// Gets a value indicating whether there are phrases that could be roots.
    /// </summary>
    public required bool HavePossibleRoot { get; init; }
}

/// <summary>
/// Performs pre-analysis of phrases for root expansion.
/// Based on libpostal's pre-analysis pass (expand.c lines 820-890).
/// </summary>
public class RootExpansionPreAnalyzer
{
    /// <summary>
    /// Analyzes tokenized string and phrases to compute global flags.
    /// </summary>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="phrases">The matched phrases.</param>
    /// <param name="addressComponents">The address components filter.</param>
    /// <returns>The pre-analysis result with computed flags.</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
    public PreAnalysisResult Analyze(
        TokenizedString tokenized,
        List<Phrase> phrases,
        AddressComponent addressComponents)
    {
        ArgumentNullException.ThrowIfNull(tokenized);
        ArgumentNullException.ThrowIfNull(phrases);

        // Track which token positions are covered by phrases
        var coveredPositions = new HashSet<int>();

        bool haveCanonicalPhrases = false;
        bool haveAmbiguous = false;
        bool havePossibleRoot = false;

        // Scan all phrases
        foreach (var phrase in phrases)
        {
            // Mark positions as covered
            for (int i = phrase.StartIndex; i < phrase.EndIndex && i < tokenized.Count; i++)
            {
                coveredPositions.Add(i);
            }

            // Check for canonical phrases
            if (PhraseClassifier.HasCanonicalInterpretation(phrase))
            {
                haveCanonicalPhrases = true;
            }

            // Check for ambiguous phrases
            if (PhraseClassifier.InDictionary(phrase, DictionaryType.AmbiguousExpansion))
            {
                haveAmbiguous = true;
            }

            // Check for possible roots
            if (PhraseClassifier.IsPossibleRootForComponents(phrase, addressComponents))
            {
                havePossibleRoot = true;
            }
        }

        // Check for non-phrase tokens
        bool haveNonPhraseTokens = false;
        bool haveNonPhraseWordTokens = false;

        for (int i = 0; i < tokenized.Count; i++)
        {
            if (!coveredPositions.Contains(i))
            {
                var token = tokenized[i];

                // Skip whitespace and punctuation
                if (token.Type != TokenType.Whitespace && token.Type != TokenType.Newline)
                {
                    haveNonPhraseTokens = true;

                    // Check if it's a word token
                    if (token.Type == TokenType.Word ||
                        token.Type == TokenType.Abbreviation ||
                        token.Type == TokenType.Acronym ||
                        token.Type == TokenType.Phrase)
                    {
                        haveNonPhraseWordTokens = true;
                    }
                }
            }
        }

        return new PreAnalysisResult
        {
            HaveNonPhraseTokens = haveNonPhraseTokens,
            HaveNonPhraseWordTokens = haveNonPhraseWordTokens,
            HaveCanonicalPhrases = haveCanonicalPhrases,
            HaveAmbiguous = haveAmbiguous,
            HavePossibleRoot = havePossibleRoot
        };
    }
}
