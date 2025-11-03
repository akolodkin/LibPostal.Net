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

        // Add base features EXCEPT context features (we'll add phrase-aware context instead)
        foreach (var feature in baseFeatures)
        {
            // Filter out simple word context features - we'll replace with phrase-aware context
            if (!feature.StartsWith("prev_word") && !feature.StartsWith("next_word") &&
                !feature.Contains("+word=") && !feature.StartsWith("word+next_word"))
            {
                features.Add(feature);
            }
        }

        var token = tokenized[tokenIndex];

        // Skip whitespace
        if (token.Type == TokenType.Whitespace || token.Type == TokenType.Newline)
        {
            return baseFeatures;
        }

        // Add phrase-aware context features (prev_word, next_word with phrases)
        AddPhraseAwareContextFeatures(features, tokenIndex, tokenized, context);

        // Add dictionary phrase features
        var dictPhrase = context.GetDictionaryPhraseAt(tokenIndex);
        if (dictPhrase != null && context.Tokenized == tokenized)
        {
            AddDictionaryPhraseFeatures(features, dictPhrase, context);
        }

        // Add component phrase features (cities, states, countries)
        var componentPhrase = context.GetComponentPhraseAt(tokenIndex);
        if (componentPhrase != null && context.Tokenized == tokenized)
        {
            AddComponentPhraseFeatures(features, componentPhrase, context);
        }

        // Add postal code context features (graph-based validation)
        AddPostalCodeContextFeatures(features, tokenized, tokenIndex, context);

        // Add long context features for venue name detection (first unknown word only)
        AddLongContextFeatures(features, tokenIndex, tokenized, context);

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
        features.Add($"phrase:{phrase.PhraseText}");

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
                features.Add($"unambiguous phrase type:{componentName}");
                features.Add($"unambiguous phrase type+phrase:{componentName}:{phrase.PhraseText}");
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
    /// Adds component phrase features (cities, states, countries, etc.)
    /// Based on libpostal's address_parser.c lines 1200-1260
    /// </summary>
    private void AddComponentPhraseFeatures(FeatureVector features, PhraseMatch phrase, AddressParserContext context)
    {
        var model = context.Model;

        // Add generic phrase feature
        features.Add($"phrase:{phrase.PhraseText}");

        // Get component types if available
        if (model.ComponentPhraseTypes == null || phrase.PhraseId >= model.ComponentPhraseTypes.Length)
        {
            return;
        }

        var types = model.ComponentPhraseTypes[phrase.PhraseId];
        var components = (ComponentPhraseBoundary)types.Components;
        var mostCommonBoundary = GetBoundaryFromOrdinal(types.MostCommon);

        // Add component-specific features using helper
        AddComponentFeature(features, components, ComponentPhraseBoundary.Suburb, "suburb", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.City, "city", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.CityDistrict, "city_district", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.Island, "island", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.StateDistrict, "state_district", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.State, "state", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.CountryRegion, "country_region", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.Country, "country", phrase.PhraseText);
        AddComponentFeature(features, components, ComponentPhraseBoundary.WorldRegion, "world_region", phrase.PhraseText);

        // Add "commonly X" feature if most common differs from current components
        // This happens when phrase is ambiguous but has a statistically most common type
        var isAmbiguous = (components & (components - 1)) != 0; // More than one bit set
        var mostCommonIsInComponents = (components & mostCommonBoundary) != 0;

        if (isAmbiguous && mostCommonIsInComponents && mostCommonBoundary != ComponentPhraseBoundary.None)
        {
            var commonlyType = GetComponentBoundaryName(mostCommonBoundary);
            if (!string.IsNullOrEmpty(commonlyType))
            {
                features.Add($"commonly {commonlyType}:{phrase.PhraseText}");
            }
        }
    }

    /// <summary>
    /// Converts ordinal enum value to ComponentPhraseBoundary bitmask.
    /// </summary>
    private static ComponentPhraseBoundary GetBoundaryFromOrdinal(ushort ordinal)
    {
        // Map enum ordinals to bit positions
        // 0 = None, 1 = Suburb (1<<3), 2 = CityDistrict (1<<4), etc.
        return ordinal switch
        {
            0 => ComponentPhraseBoundary.None,
            1 => ComponentPhraseBoundary.Suburb,
            2 => ComponentPhraseBoundary.CityDistrict,
            3 => ComponentPhraseBoundary.City,
            4 => ComponentPhraseBoundary.StateDistrict,
            5 => ComponentPhraseBoundary.Island,
            6 => ComponentPhraseBoundary.State,
            7 => ComponentPhraseBoundary.CountryRegion,
            8 => ComponentPhraseBoundary.Country,
            9 => ComponentPhraseBoundary.WorldRegion,
            _ => ComponentPhraseBoundary.None
        };
    }

    /// <summary>
    /// Helper to add component phrase features (matches libpostal's add_phrase_features logic)
    /// </summary>
    private static void AddComponentFeature(FeatureVector features, ComponentPhraseBoundary phraseComponents,
        ComponentPhraseBoundary targetComponent, string componentName, string phraseText)
    {
        if (phraseComponents == targetComponent)
        {
            // Unambiguous: only this component type
            features.Add($"unambiguous phrase type:{componentName}");
            features.Add($"unambiguous phrase type+phrase:{componentName}:{phraseText}");
        }
        else if ((phraseComponents & targetComponent) != 0)
        {
            // Ambiguous: this component is one of multiple
            features.Add($"phrase:{componentName}");
            features.Add($"phrase type+phrase:{componentName}:{phraseText}");
        }
    }

    /// <summary>
    /// Gets component boundary name from enum value.
    /// </summary>
    private static string? GetComponentBoundaryName(ComponentPhraseBoundary boundary)
    {
        // Handle single-bit enum values
        return boundary switch
        {
            ComponentPhraseBoundary.Suburb => "suburb",
            ComponentPhraseBoundary.City => "city",
            ComponentPhraseBoundary.CityDistrict => "city_district",
            ComponentPhraseBoundary.Island => "island",
            ComponentPhraseBoundary.StateDistrict => "state_district",
            ComponentPhraseBoundary.State => "state",
            ComponentPhraseBoundary.CountryRegion => "country_region",
            ComponentPhraseBoundary.Country => "country",
            ComponentPhraseBoundary.WorldRegion => "world_region",
            _ => null
        };
    }

    /// <summary>
    /// Adds postal code context features based on graph validation.
    /// Validates that a postal code is geographically valid for an adjacent administrative region (city/state/country).
    /// Based on libpostal's address_parser.c lines 1262-1319.
    /// </summary>
    /// <param name="features">The feature vector to add features to.</param>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="tokenIndex">The current token index.</param>
    /// <param name="context">The address parser context containing phrase memberships and graph.</param>
    /// <remarks>
    /// Features generated:
    /// - "postcode have context" + "postcode have context:{code}" when validated
    /// - "postcode no context:{code}" when postal code found but not validated
    /// </remarks>
    private void AddPostalCodeContextFeatures(
        FeatureVector features,
        TokenizedString tokenized,
        int tokenIndex,
        AddressParserContext context)
    {
        // Get postal code phrase at current token
        var postalCodePhrase = context.GetPostalCodePhraseAt(tokenIndex);
        if (postalCodePhrase == null)
            return;

        // Skip if no graph available
        if (context.Model.PostalCodeGraph == null)
            return;

        uint postalCodeId = postalCodePhrase.PhraseId;
        bool haveContext = false;

        // Check PREVIOUS token for component phrase (before postal code phrase starts)
        // This handles cases like "Brooklyn 11216" or "NY 10001"
        int prevIndex = postalCodePhrase.StartIndex - 1;
        if (prevIndex >= 0)
        {
            haveContext = CheckPostalCodeAdminContext(context, postalCodeId, prevIndex);
        }

        // Check NEXT token for component phrase (after postal code phrase ends)
        // This handles cases like "11216 Brooklyn"
        // Only check if we haven't found context yet
        if (!haveContext)
        {
            int nextIndex = postalCodePhrase.StartIndex + postalCodePhrase.Length;
            if (nextIndex < tokenized.Count)
            {
                haveContext = CheckPostalCodeAdminContext(context, postalCodeId, nextIndex);
            }
        }

        // Generate features
        var word = tokenized[tokenIndex].Text.ToLowerInvariant();
        if (haveContext)
        {
            // Postal code has valid administrative context
            features.Add("postcode have context");
            features.Add($"postcode have context:{word}");
        }
        else
        {
            // Postal code found but no valid administrative context
            features.Add($"postcode no context:{word}");
        }
    }

    /// <summary>
    /// Checks if a postal code has valid context with an administrative region at the specified token index.
    /// </summary>
    /// <param name="context">The address parser context.</param>
    /// <param name="postalCodeId">The postal code phrase ID.</param>
    /// <param name="tokenIndex">The token index to check for a component phrase.</param>
    /// <returns>True if the postal code is valid for the administrative region; otherwise, false.</returns>
    private static bool CheckPostalCodeAdminContext(
        AddressParserContext context,
        uint postalCodeId,
        int tokenIndex)
    {
        var adminPhrase = context.GetComponentPhraseAt(tokenIndex);
        if (adminPhrase != null)
        {
            uint adminId = adminPhrase.PhraseId;
            return context.Model.PostalCodeGraph!.HasEdge((int)postalCodeId, (int)adminId);
        }
        return false;
    }

    /// <summary>
    /// Calculates phrase-aware context indices for prev/next word features.
    /// Based on libpostal's address_parser.c lines 1119-1310.
    /// </summary>
    /// <param name="tokenIndex">The current token index.</param>
    /// <param name="context">The parser context.</param>
    /// <returns>Tuple of (prevIndex, nextIndex) adjusted for phrase boundaries.</returns>
    private (int prevIndex, int nextIndex) GetPhraseAwareContextIndices(
        int tokenIndex,
        AddressParserContext context)
    {
        // Start with simple adjacent indices
        int prevIndex = tokenIndex - 1;
        int nextIndex = tokenIndex + 1;

        // Get phrases at current position
        var dictPhrase = context.GetDictionaryPhraseAt(tokenIndex);
        var componentPhrase = context.GetComponentPhraseAt(tokenIndex);
        var postalCodePhrase = context.GetPostalCodePhraseAt(tokenIndex);

        // Priority 1: Dictionary phrases (street types, etc.)
        // If dictionary phrase is longer or equal to component phrase, use it
        if (dictPhrase != null && dictPhrase.Length > 0 &&
            (componentPhrase == null || dictPhrase.Length >= componentPhrase.Length))
        {
            prevIndex = dictPhrase.StartIndex - 1;
            nextIndex = dictPhrase.EndIndex + 1; // Token AFTER phrase ends
        }

        // Priority 2: Component phrases (cities, states, etc.)
        // Only adjust if component phrase is dominant OR extends boundaries
        if (componentPhrase != null && componentPhrase.Length > 0)
        {
            // If no dict phrase or component is longer, use component boundaries
            if (dictPhrase == null || componentPhrase.Length > dictPhrase.Length)
            {
                prevIndex = componentPhrase.StartIndex - 1;
                nextIndex = componentPhrase.EndIndex + 1; // Token AFTER phrase ends
            }
            else
            {
                // Component phrase extends boundaries
                if (prevIndex >= componentPhrase.StartIndex - 1)
                {
                    prevIndex = componentPhrase.StartIndex - 1;
                }
                if (nextIndex <= componentPhrase.EndIndex)
                {
                    nextIndex = componentPhrase.EndIndex + 1;
                }
            }
        }

        // Priority 3: Postal code phrases (only extend boundaries)
        if (postalCodePhrase != null && postalCodePhrase.Length > 0)
        {
            if (prevIndex >= postalCodePhrase.StartIndex - 1)
            {
                prevIndex = postalCodePhrase.StartIndex - 1;
            }
            if (nextIndex <= postalCodePhrase.EndIndex)
            {
                nextIndex = postalCodePhrase.EndIndex + 1;
            }
        }

        return (prevIndex, nextIndex);
    }

    /// <summary>
    /// Gets the word or phrase text at the specified index.
    /// Based on libpostal's word_or_phrase_at_index() in address_parser.c lines 883-965.
    /// </summary>
    /// <param name="index">The token index.</param>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="context">The parser context.</param>
    /// <returns>The word or phrase text at the index, or null if out of bounds.</returns>
    private string? GetWordOrPhraseAtIndex(
        int index,
        TokenizedString tokenized,
        AddressParserContext context)
    {
        if (index < 0 || index >= tokenized.Count)
            return null;

        // Skip whitespace
        if (tokenized[index].Type == TokenType.Whitespace || tokenized[index].Type == TokenType.Newline)
            return null;

        // Priority 1: Dictionary phrase
        var dictPhrase = context.GetDictionaryPhraseAt(index);
        var componentPhrase = context.GetComponentPhraseAt(index);

        if (dictPhrase != null && dictPhrase.Length > 0 &&
            (componentPhrase == null || dictPhrase.Length >= componentPhrase.Length))
        {
            return GetPhraseText(tokenized, dictPhrase);
        }

        // Priority 2: Component phrase
        if (componentPhrase != null && componentPhrase.Length > 0)
        {
            return GetPhraseText(tokenized, componentPhrase);
        }

        // Priority 3: Postal code phrase
        var postalCodePhrase = context.GetPostalCodePhraseAt(index);
        if (postalCodePhrase != null && postalCodePhrase.Length > 0)
        {
            return GetPhraseText(tokenized, postalCodePhrase);
        }

        // Default: Plain word
        return tokenized[index].Text.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the concatenated text for a phrase.
    /// Based on libpostal's cstring_array_get_phrase() in trie_search.c lines 839-850.
    /// </summary>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="phrase">The phrase match.</param>
    /// <returns>Space-separated phrase text.</returns>
    private string GetPhraseText(TokenizedString tokenized, PhraseMatch phrase)
    {
        var parts = new List<string>();

        // Iterate from StartIndex to EndIndex (inclusive)
        for (int k = phrase.StartIndex; k <= phrase.EndIndex && k < tokenized.Count; k++)
        {
            // Skip whitespace tokens
            if (tokenized[k].Type != TokenType.Whitespace && tokenized[k].Type != TokenType.Newline)
            {
                parts.Add(tokenized[k].Text.ToLowerInvariant());
            }
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Finds the next non-whitespace token index starting from the given index.
    /// </summary>
    /// <param name="startIndex">The starting index.</param>
    /// <param name="tokenized">The tokenized string.</param>
    /// <returns>The next non-whitespace token index, or -1 if none found.</returns>
    private int FindNextNonWhitespace(int startIndex, TokenizedString tokenized)
    {
        for (int i = startIndex; i < tokenized.Count; i++)
        {
            if (tokenized[i].Type != TokenType.Whitespace && tokenized[i].Type != TokenType.Newline)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Finds the previous non-whitespace token index starting from the given index.
    /// </summary>
    /// <param name="startIndex">The starting index.</param>
    /// <param name="tokenized">The tokenized string.</param>
    /// <returns>The previous non-whitespace token index, or -1 if none found.</returns>
    private int FindPrevNonWhitespace(int startIndex, TokenizedString tokenized)
    {
        for (int i = startIndex; i >= 0; i--)
        {
            if (tokenized[i].Type != TokenType.Whitespace && tokenized[i].Type != TokenType.Newline)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Adds phrase-aware context window features (prev_word, next_word, bigrams).
    /// Based on libpostal's address_parser.c lines 1483-1527.
    /// </summary>
    /// <param name="features">The feature vector.</param>
    /// <param name="tokenIndex">The current token index.</param>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="context">The parser context.</param>
    private void AddPhraseAwareContextFeatures(
        FeatureVector features,
        int tokenIndex,
        TokenizedString tokenized,
        AddressParserContext context)
    {
        var currentWord = tokenized[tokenIndex].Text.ToLowerInvariant();

        // Calculate phrase-aware context indices
        var (prevIndex, nextIndex) = GetPhraseAwareContextIndices(tokenIndex, context);

        // Previous context - skip whitespace to find actual prev word/phrase
        prevIndex = FindPrevNonWhitespace(prevIndex, tokenized);
        if (prevIndex >= 0)
        {
            var prevWord = GetWordOrPhraseAtIndex(prevIndex, tokenized, context);
            if (prevWord != null)
            {
                features.Add($"prev_word={prevWord}");
                features.Add($"prev_word+word={prevWord} {currentWord}");
            }
        }

        // Next context - skip whitespace to find actual next word/phrase
        nextIndex = FindNextNonWhitespace(nextIndex, tokenized);
        if (nextIndex >= 0 && nextIndex < tokenized.Count)
        {
            var nextWord = GetWordOrPhraseAtIndex(nextIndex, tokenized, context);
            if (nextWord != null)
            {
                features.Add($"next_word={nextWord}");
                features.Add($"word+next_word={currentWord} {nextWord}");
            }
        }
    }

    /// <summary>
    /// Adds long context features for venue name detection.
    /// Based on libpostal's address_parser.c lines 1532-1638.
    /// </summary>
    /// <param name="features">The feature vector.</param>
    /// <param name="tokenIndex">The current token index.</param>
    /// <param name="tokenized">The tokenized string.</param>
    /// <param name="context">The parser context.</param>
    /// <remarks>
    /// Long context features help distinguish venue names from street names.
    /// Example: "Barboncino 781 Franklin Ave" - "Barboncino" is a venue name.
    /// Features look ahead to find numbers and street/venue phrases.
    /// </remarks>
    private void AddLongContextFeatures(
        FeatureVector features,
        int tokenIndex,
        TokenizedString tokenized,
        AddressParserContext context)
    {
        // Only for first token
        if (tokenIndex != 0)
            return;

        var token = tokenized[tokenIndex];

        // Must be a word
        if (token.Type != TokenType.Word)
            return;

        // Check if word is unknown (not in vocabulary)
        var word = token.Text.ToLowerInvariant();
        bool isUnknownWord = context.Model.Vocabulary != null &&
                             !context.Model.Vocabulary.ContainsKey(word);

        if (!isUnknownWord)
            return;

        // Check if token is part of any phrase (if so, skip long context)
        if (context.HasDictionaryPhrase(tokenIndex) ||
            context.HasComponentPhrase(tokenIndex) ||
            context.HasPostalCodePhrase(tokenIndex))
            return;

        // State tracking
        bool seenNumber = false;
        bool seenPhrase = false;

        // Scan ahead looking for numbers and phrases
        for (int rightIdx = tokenIndex + 1; rightIdx < tokenized.Count; rightIdx++)
        {
            var rightToken = tokenized[rightIdx];

            // Skip whitespace
            if (rightToken.Type == TokenType.Whitespace || rightToken.Type == TokenType.Newline)
                continue;

            // Check for dictionary phrase
            var dictPhrase = context.GetDictionaryPhraseAt(rightIdx);
            if (dictPhrase != null && context.Model.PhraseTypes != null)
            {
                uint phraseTypeFlags = context.Model.PhraseTypes[dictPhrase.PhraseId];
                var components = (AddressComponent)phraseTypeFlags;

                string relationToNumber = seenNumber ? "after number" : "before number";
                seenPhrase = true;

                var phraseWord = GetPhraseText(tokenized, dictPhrase);

                // Pure STREET (not NAME)
                if (components.HasFlag(AddressComponent.Road) &&
                    !components.HasFlag(AddressComponent.Name))
                {
                    features.Add($"first word unknown+street phrase right:{relationToNumber}");
                    features.Add($"first word unknown+street phrase right:{relationToNumber}:{phraseWord}");
                    break; // Stop searching
                }
                // Pure NAME (not STREET)
                else if (components.HasFlag(AddressComponent.Name) &&
                         !components.HasFlag(AddressComponent.Road))
                {
                    features.Add($"first word unknown+venue phrase right:{relationToNumber}");
                    features.Add($"first word unknown+venue phrase right:{relationToNumber}:{phraseWord}");
                    // Continue searching (might find STREET phrase later)
                }
                // Ambiguous (both NAME and STREET)
                else if (components.HasFlag(AddressComponent.Name) &&
                         components.HasFlag(AddressComponent.Road))
                {
                    if (seenNumber)
                    {
                        features.Add("first word unknown+number+ambiguous phrase right");
                        features.Add($"first word unknown+number+ambiguous phrase right:{phraseWord}");
                        break; // Stop searching
                    }
                    // If no number yet, continue searching
                }
            }

            // Check for numeric token
            if (rightToken.Type == TokenType.Numeric)
            {
                seenNumber = true;
                string relationToPhrase = seenPhrase ? "after phrase" : "before phrase";

                var numericWord = rightToken.Text.ToLowerInvariant();
                features.Add($"first word unknown+number right:{relationToPhrase}");
                features.Add($"first word unknown+number right:{relationToPhrase}:{numericWord}");

                // If we've seen both phrase and number, stop
                if (seenPhrase)
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the model from context (helper method).
    /// </summary>
    private static AddressParserModel? GetModelFromContext(AddressParserContext context)
    {
        return context.Model;
    }
}
