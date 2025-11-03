using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Parser;

/// <summary>
/// Extracts features from tokenized addresses for CRF-based parsing.
/// Based on libpostal's address_parser_features() function (address_parser.c lines 1089-1850).
/// </summary>
public class AddressFeatureExtractor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressFeatureExtractor"/> class.
    /// </summary>
    public AddressFeatureExtractor()
    {
    }

    /// <summary>
    /// Extracts features for a token at the specified index.
    /// </summary>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="tokenIndex">The index of the token to extract features for.</param>
    /// <returns>Array of feature strings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenized"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tokenIndex"/> is out of range.</exception>
    public string[] ExtractFeatures(TokenizedString tokenized, int tokenIndex)
    {
        ArgumentNullException.ThrowIfNull(tokenized);

        if (tokenIndex < 0 || tokenIndex >= tokenized.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(tokenIndex),
                $"Token index {tokenIndex} is out of range for tokenized string with {tokenized.Count} tokens.");
        }

        var features = new FeatureVector();
        var token = tokenized[tokenIndex];

        // Skip whitespace and newline tokens
        if (token.Type == TokenType.Whitespace || token.Type == TokenType.Newline)
        {
            return Array.Empty<string>();
        }

        // Always add bias feature (intercept term)
        features.Add("bias");

        // Word features
        if (token.Type == TokenType.Word || token.Type == TokenType.Abbreviation || token.Type == TokenType.Acronym)
        {
            var word = token.Text.ToLowerInvariant();

            // Remove trailing period for feature (but detect it)
            var hasPeriod = word.EndsWith('.');
            var cleanWord = hasPeriod ? word.TrimEnd('.') : word;

            features.Add($"word={cleanWord}");
            features.Add($"word_length={cleanWord.Length}");

            // Capitalization features
            if (token.Text.Length > 0 && char.IsUpper(token.Text[0]))
            {
                features.Add("is_capitalized");
            }

            if (token.Text.Length > 0 && token.Text.All(c => char.IsUpper(c) || c == '.'))
            {
                features.Add("is_all_caps");
            }

            // Period feature
            if (hasPeriod || token.Text.Contains('.'))
            {
                features.Add("has_period");
            }

            // N-gram features for longer words (simulate unknown word handling)
            if (cleanWord.Length >= 6)
            {
                // Add prefix/suffix n-grams (3-6 characters)
                for (int n = 3; n <= Math.Min(6, cleanWord.Length); n++)
                {
                    features.Add($"word:prefix{n}={cleanWord.Substring(0, n)}");
                    if (cleanWord.Length >= n)
                    {
                        features.Add($"word:suffix{n}={cleanWord.Substring(cleanWord.Length - n)}");
                    }
                }
            }

            // Hyphenated words - extract sub-words
            if (cleanWord.Contains('-'))
            {
                var parts = cleanWord.Split('-');
                foreach (var part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                    {
                        features.Add($"sub_word={part}");
                    }
                }
            }
        }

        // Numeric features
        if (token.Type == TokenType.Numeric)
        {
            features.Add("is_numeric");
        }

        // Get non-whitespace tokens for context
        var nonWhitespace = tokenized.GetTokensWithoutWhitespace().ToList();
        var currentIdx = nonWhitespace.FindIndex(t => t.Offset == token.Offset);

        // Separator context - check if previous token was a separator
        if (currentIdx > 0)
        {
            var prevToken = nonWhitespace[currentIdx - 1];
            if (prevToken.Type == TokenType.Comma)
            {
                features.Add("after_comma");
            }
        }

        // Position features (based on non-whitespace position)
        if (currentIdx == 0)
        {
            features.Add("position=first");
        }
        else if (currentIdx == nonWhitespace.Count - 1)
        {
            features.Add("position=last");
        }

        // Context window features (prev/next words)
        if (currentIdx > 0)
        {
            var prevToken = nonWhitespace[currentIdx - 1];
            var prevWord = prevToken.Text.ToLowerInvariant();
            features.Add($"prev_word={prevWord}");

            // Bigram: prev+current
            var word = token.Text.ToLowerInvariant();
            features.Add($"prev_word+word={prevWord} {word}");
        }

        if (currentIdx >= 0 && currentIdx < nonWhitespace.Count - 1)
        {
            var nextToken = nonWhitespace[currentIdx + 1];
            var nextWord = nextToken.Text.ToLowerInvariant();
            features.Add($"next_word={nextWord}");

            // Bigram: current+next
            var word = token.Text.ToLowerInvariant();
            features.Add($"word+next_word={word} {nextWord}");
        }

        return features.ToArray();
    }

    /// <summary>
    /// Extracts features for a token at the specified index with phrase context.
    /// </summary>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="tokenIndex">The index of the token to extract features for.</param>
    /// <param name="context">The parser context with phrase information.</param>
    /// <returns>Array of feature strings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when parameters are null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when tokenIndex is out of range.</exception>
    public string[] ExtractFeatures(TokenizedString tokenized, int tokenIndex, AddressParserContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Start with base features
        var baseFeatures = ExtractFeatures(tokenized, tokenIndex);
        var features = new FeatureVector();

        // Add all base features
        foreach (var feature in baseFeatures)
        {
            features.Add(feature);
        }

        var token = tokenized[tokenIndex];

        // Skip whitespace
        if (token.Type == TokenType.Whitespace || token.Type == TokenType.Newline)
        {
            return baseFeatures;
        }

        // Add dictionary phrase features
        var dictPhrase = context.GetDictionaryPhraseAt(tokenIndex);
        if (dictPhrase != null && context.Tokenized == tokenized)
        {
            AddDictionaryPhraseFeatures(features, dictPhrase, context);
        }

        // Add prefix/suffix features if no phrase match
        // This handles per-token prefix/suffix detection (like German "hinter-", "-straße")
        if (dictPhrase == null && token.Type == TokenType.Word && context.Model.Phrases != null)
        {
            var matcher = new PhraseMatcher(context.Model.Phrases);
            var word = token.Text.ToLowerInvariant();

            // Check for prefixes
            var prefixes = matcher.SearchPrefixes(word);
            foreach (var prefix in prefixes)
            {
                features.Add($"prefix={prefix.PhraseText}");
            }

            // Check for suffixes
            var suffixes = matcher.SearchSuffixes(word);
            foreach (var suffix in suffixes)
            {
                features.Add($"suffix={suffix.PhraseText}");
            }
        }

        return features.ToArray();
    }

    /// <summary>
    /// Adds dictionary phrase features based on the phrase type.
    /// </summary>
    private void AddDictionaryPhraseFeatures(FeatureVector features, PhraseMatch phrase, AddressParserContext context)
    {
        // Add generic phrase feature
        features.Add($"phrase={phrase.PhraseText}");

        // Get component types from model if available
        AddressComponent components = AddressComponent.None;
        var model = GetModelFromContext(context);

        if (model?.PhraseTypes != null && phrase.PhraseId < model.PhraseTypes.Length)
        {
            components = (AddressComponent)model.PhraseTypes[phrase.PhraseId];
        }
        else
        {
            // Fallback to heuristics if no phrase types
            components = InferComponentsFromPhrase(phrase.PhraseText);
        }

        // Add features for each component type
        if ((components & AddressComponent.Road) != 0)
            features.Add("phrase:street");

        if ((components & AddressComponent.Unit) != 0)
            features.Add("phrase:unit");

        if ((components & AddressComponent.Level) != 0)
            features.Add("phrase:level");

        if ((components & AddressComponent.POBox) != 0)
            features.Add("phrase:po_box");

        if ((components & AddressComponent.Entrance) != 0)
            features.Add("phrase:entrance");

        if ((components & AddressComponent.Staircase) != 0)
            features.Add("phrase:staircase");

        if ((components & AddressComponent.House) != 0)
            features.Add("phrase:house");

        if ((components & AddressComponent.Name) != 0)
            features.Add("phrase:name");

        if ((components & AddressComponent.Category) != 0)
            features.Add("phrase:category");

        // Check if unambiguous (only one component type)
        if (IsUnambiguous(components))
        {
            var componentName = GetSingleComponentName(components);
            if (!string.IsNullOrEmpty(componentName))
            {
                features.Add($"unambiguous phrase type={componentName}");
            }
        }
    }

    /// <summary>
    /// Infers component types from phrase text using heuristics.
    /// </summary>
    private static AddressComponent InferComponentsFromPhrase(string phraseText)
    {
        var components = AddressComponent.None;

        if (phraseText.Contains("street") || phraseText.Contains("avenue") ||
            phraseText.Contains("road") || phraseText.Contains("boulevard") ||
            phraseText.Contains("north") || phraseText.Contains("south") ||
            phraseText.Contains("east") || phraseText.Contains("west"))
        {
            components |= AddressComponent.Road;
        }

        if (phraseText.Contains("apt") || phraseText.Contains("unit") ||
            phraseText.Contains("apartment") || phraseText.Contains("suite"))
        {
            components |= AddressComponent.Unit;
        }

        if (phraseText.Contains("floor") || phraseText.Contains("level"))
        {
            components |= AddressComponent.Level;
        }

        if (phraseText.Contains("po box") || phraseText.Contains("p.o. box"))
        {
            components |= AddressComponent.POBox;
        }

        if (phraseText.Contains("entrance"))
        {
            components |= AddressComponent.Entrance;
        }

        if (phraseText.Contains("staircase"))
        {
            components |= AddressComponent.Staircase;
        }

        if (phraseText.Contains("building") || phraseText.Contains("house"))
        {
            components |= AddressComponent.House | AddressComponent.Name;
        }

        return components;
    }

    /// <summary>
    /// Checks if component flags represent a single component type.
    /// </summary>
    private static bool IsUnambiguous(AddressComponent components)
    {
        // Check if exactly one bit is set
        return components != AddressComponent.None && (components & (components - 1)) == 0;
    }

    /// <summary>
    /// Gets the component name for a single-component type.
    /// </summary>
    private static string? GetSingleComponentName(AddressComponent component)
    {
        return component switch
        {
            AddressComponent.Road => "street",
            AddressComponent.Unit => "unit",
            AddressComponent.Level => "level",
            AddressComponent.POBox => "po_box",
            AddressComponent.Entrance => "entrance",
            AddressComponent.Staircase => "staircase",
            AddressComponent.House => "house",
            AddressComponent.Name => "name",
            AddressComponent.Category => "category",
            AddressComponent.HouseNumber => "house_number",
            AddressComponent.City => "city",
            AddressComponent.State => "state",
            AddressComponent.Postcode => "postcode",
            _ => null
        };
    }

    /// <summary>
    /// Gets the model from context (helper method).
    /// </summary>
    private static AddressParserModel? GetModelFromContext(AddressParserContext context)
    {
        return context.Model;
    }
}
