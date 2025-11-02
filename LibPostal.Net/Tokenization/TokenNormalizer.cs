using System.Text;
using System.Text.RegularExpressions;

namespace LibPostal.Net.Tokenization;

/// <summary>
/// Normalizes individual tokens.
/// Based on libpostal's token-level normalization.
/// </summary>
public partial class TokenNormalizer
{
    [GeneratedRegex(@"\d")]
    private static partial Regex DigitPattern();

    /// <summary>
    /// Normalizes a single token with default options (none).
    /// </summary>
    /// <param name="token">The token text to normalize.</param>
    /// <returns>The normalized token text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null.</exception>
    public string NormalizeToken(string token)
    {
        return NormalizeToken(token, TokenNormalizationOptions.None);
    }

    /// <summary>
    /// Normalizes a single token with the specified options.
    /// </summary>
    /// <param name="token">The token text to normalize.</param>
    /// <param name="options">The normalization options to apply.</param>
    /// <returns>The normalized token text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is null.</exception>
    public string NormalizeToken(string token, TokenNormalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (string.IsNullOrEmpty(token) || options == TokenNormalizationOptions.None)
        {
            return token;
        }

        var result = token;

        // Delete hyphens
        if (options.HasFlag(TokenNormalizationOptions.DeleteHyphens))
        {
            result = result.Replace("-", "");
            result = result.Replace("–", ""); // en-dash
            result = result.Replace("—", ""); // em-dash
        }

        // Delete possessive forms ('s or s') - must be before DeleteApostrophe
        if (options.HasFlag(TokenNormalizationOptions.DeletePossessive))
        {
            if (result.EndsWith("'s", StringComparison.Ordinal))
            {
                result = result.Substring(0, result.Length - 2);
            }
            else if (result.EndsWith("s'", StringComparison.Ordinal))
            {
                result = result.Substring(0, result.Length - 1);
            }
        }

        // Delete apostrophes (should be after possessive handling)
        if (options.HasFlag(TokenNormalizationOptions.DeleteApostrophe))
        {
            result = result.Replace("'", "");
            result = result.Replace("'", ""); // right single quotation mark
        }

        // Delete acronym periods (e.g., U.S.A. → USA)
        if (options.HasFlag(TokenNormalizationOptions.DeleteAcronymPeriods))
        {
            result = result.Replace(".", "");
        }

        // Delete final period
        if (options.HasFlag(TokenNormalizationOptions.DeleteFinalPeriod))
        {
            if (result.EndsWith('.'))
            {
                result = result.Substring(0, result.Length - 1);
            }
        }

        // Replace digits with 'D'
        if (options.HasFlag(TokenNormalizationOptions.ReplaceDigits))
        {
            result = DigitPattern().Replace(result, "D");
        }

        // Split alpha/numeric (returns first non-digit part for now, full split would need multiple tokens)
        if (options.HasFlag(TokenNormalizationOptions.SplitAlphaNumeric))
        {
            // For simplicity, just add space between digits and letters
            result = SplitAlphaNumeric(result);
        }

        return result;
    }

    /// <summary>
    /// Normalizes all tokens in a tokenized string.
    /// </summary>
    /// <param name="tokenizedString">The tokenized string.</param>
    /// <param name="options">The normalization options to apply.</param>
    /// <returns>A list of normalized token strings (excluding whitespace).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenizedString"/> is null.</exception>
    public IReadOnlyList<string> NormalizeTokens(TokenizedString tokenizedString, TokenNormalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(tokenizedString);

        var result = new List<string>();

        foreach (var token in tokenizedString.GetTokensWithoutWhitespace())
        {
            var normalized = NormalizeToken(token.Text, options);
            result.Add(normalized);
        }

        return result;
    }

    private static string SplitAlphaNumeric(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder();
        var lastWasDigit = char.IsDigit(input[0]);

        foreach (var ch in input)
        {
            var isDigit = char.IsDigit(ch);

            // Add space when transitioning between digit and non-digit
            if (sb.Length > 0 && lastWasDigit != isDigit)
            {
                sb.Append(' ');
            }

            sb.Append(ch);
            lastWasDigit = isDigit;
        }

        return sb.ToString();
    }
}
