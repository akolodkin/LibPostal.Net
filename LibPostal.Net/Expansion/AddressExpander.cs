using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Expansion;

/// <summary>
/// Expands addresses into normalized alternatives.
/// Based on libpostal's expand_address functionality.
/// </summary>
public class AddressExpander
{
    private readonly Dictionary<string, AddressExpansionValue> _dictionary;
    private readonly Tokenizer _tokenizer;
    private readonly StringNormalizer _stringNormalizer;
    private readonly TokenNormalizer _tokenNormalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressExpander"/> class.
    /// </summary>
    /// <param name="dictionary">The expansion dictionary.</param>
    public AddressExpander(Dictionary<string, AddressExpansionValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        _dictionary = dictionary;
        _tokenizer = new Tokenizer();
        _stringNormalizer = new StringNormalizer();
        _tokenNormalizer = new TokenNormalizer();
    }

    /// <summary>
    /// Expands an address with default options.
    /// </summary>
    /// <param name="input">The address string to expand.</param>
    /// <returns>An array of expanded address alternatives.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public string[] Expand(string input)
    {
        return Expand(input, ExpansionOptions.GetDefault());
    }

    /// <summary>
    /// Expands an address with the specified options.
    /// </summary>
    /// <param name="input">The address string to expand.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>An array of expanded address alternatives.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> or <paramref name="options"/> is null.</exception>
    public string[] Expand(string input, ExpansionOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(input))
        {
            return Array.Empty<string>();
        }

        // Step 1: Normalize the input string
        var normalized = NormalizeInputString(input, options);

        // Step 2: Tokenize
        var tokenized = _tokenizer.Tokenize(normalized);

        // Step 3: Search for phrases in dictionary
        var phraseSearcher = new PhraseSearcher(_dictionary);
        var phrases = phraseSearcher.SearchTokens(tokenized);

        // Step 4: Filter phrases by component and language
        var filteredPhrases = FilterPhrases(phrases, options);

        // Step 5: Generate expansions using StringTree
        var alternatives = GenerateAlternatives(tokenized, filteredPhrases, options);

        // Step 6: Apply token-level normalization
        var normalized_results = ApplyTokenNormalization(alternatives, options);

        // Step 7: Return unique results
        return normalized_results.Distinct().ToArray();
    }

    private string NormalizeInputString(string input, ExpansionOptions options)
    {
        var normalizationOptions = NormalizationOptions.None;

        if (options.TrimString)
            normalizationOptions |= NormalizationOptions.Trim;

        if (options.Lowercase)
            normalizationOptions |= NormalizationOptions.Lowercase;

        if (options.StripAccents)
            normalizationOptions |= NormalizationOptions.StripAccents;

        if (options.Decompose)
            normalizationOptions |= NormalizationOptions.Decompose;

        return _stringNormalizer.Normalize(input, normalizationOptions);
    }

    private List<Phrase> FilterPhrases(List<Phrase> phrases, ExpansionOptions options)
    {
        var filtered = new List<Phrase>();

        foreach (var phrase in phrases)
        {
            if (phrase.Expansions == null)
                continue;

            // Filter by language if specified
            var validExpansions = phrase.Expansions.Expansions
                .Where(e => options.Languages.Length == 0 || options.Languages.Contains(e.Language))
                .Where(e => (e.Components & options.AddressComponents) != 0)
                .ToList();

            if (validExpansions.Count > 0)
            {
                var filteredPhrase = phrase with
                {
                    Expansions = new AddressExpansionValue(validExpansions)
                };
                filtered.Add(filteredPhrase);
            }
        }

        return filtered;
    }

    private List<string> GenerateAlternatives(TokenizedString tokenized, List<Phrase> phrases, ExpansionOptions options)
    {
        // Build a StringTree with alternatives at each position
        var tree = new StringTree(maxPermutations: 100);

        // Track which token positions are covered by phrases
        var coveredPositions = new HashSet<int>();
        var phrasesByPosition = new Dictionary<int, List<Phrase>>();

        foreach (var phrase in phrases)
        {
            if (!phrasesByPosition.ContainsKey(phrase.StartIndex))
            {
                phrasesByPosition[phrase.StartIndex] = new List<Phrase>();
            }
            phrasesByPosition[phrase.StartIndex].Add(phrase);
        }

        int position = 0;
        while (position < tokenized.Count)
        {
            // Check if there's a phrase starting at this position
            if (phrasesByPosition.TryGetValue(position, out var phrasesAtPosition))
            {
                // Use the longest phrase
                var longestPhrase = phrasesAtPosition.OrderByDescending(p => p.Length).First();

                // Add alternatives: original + expansions
                var alternatives = new List<string> { longestPhrase.Value };

                if (longestPhrase.Expansions != null)
                {
                    foreach (var expansion in longestPhrase.Expansions.Expansions)
                    {
                        if (expansion.Canonical != null && expansion.Canonical != longestPhrase.Value.ToLowerInvariant())
                        {
                            alternatives.Add(expansion.Canonical);
                        }
                    }
                }

                tree.AddAlternatives(alternatives.Distinct());

                // Mark positions as covered
                for (int i = position; i < position + longestPhrase.Length; i++)
                {
                    coveredPositions.Add(i);
                }

                position += longestPhrase.Length;
            }
            else
            {
                // No phrase match, just add the token
                if (!coveredPositions.Contains(position))
                {
                    tree.AddString(tokenized[position].Text);
                }
                position++;
            }
        }

        return tree.GetAllCombinations().ToList();
    }

    private List<string> ApplyTokenNormalization(List<string> inputs, ExpansionOptions options)
    {
        var tokenOptions = GetTokenNormalizationOptions(options);

        if (tokenOptions == TokenNormalizationOptions.None)
        {
            return inputs;
        }

        var results = new List<string>();

        foreach (var input in inputs)
        {
            // Re-tokenize and normalize each alternative
            var tokenized = _tokenizer.Tokenize(input);
            var normalized = _tokenNormalizer.NormalizeTokens(tokenized, tokenOptions);

            results.Add(string.Join(" ", normalized));
        }

        return results;
    }

    private TokenNormalizationOptions GetTokenNormalizationOptions(ExpansionOptions options)
    {
        var tokenOptions = TokenNormalizationOptions.None;

        if (options.DeleteFinalPeriods)
            tokenOptions |= TokenNormalizationOptions.DeleteFinalPeriod;

        if (options.DeleteAcronymPeriods)
            tokenOptions |= TokenNormalizationOptions.DeleteAcronymPeriods;

        if (options.DropEnglishPossessives)
            tokenOptions |= TokenNormalizationOptions.DeletePossessive;

        if (options.DeleteApostrophes)
            tokenOptions |= TokenNormalizationOptions.DeleteApostrophe;

        if (options.DeleteWordHyphens)
            tokenOptions |= TokenNormalizationOptions.DeleteHyphens;

        if (options.SplitAlphaFromNumeric)
            tokenOptions |= TokenNormalizationOptions.SplitAlphaNumeric;

        return tokenOptions;
    }
}
