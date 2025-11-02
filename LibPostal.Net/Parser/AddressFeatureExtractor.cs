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
}
