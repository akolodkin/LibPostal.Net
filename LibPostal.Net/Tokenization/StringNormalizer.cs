using System.Globalization;
using System.Text;

namespace LibPostal.Net.Tokenization;

/// <summary>
/// Normalizes strings for address parsing.
/// Based on libpostal's normalize.c
/// </summary>
public class StringNormalizer
{
    /// <summary>
    /// Normalizes a string with default options (none).
    /// </summary>
    /// <param name="input">The string to normalize.</param>
    /// <returns>The normalized string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public string Normalize(string input)
    {
        return Normalize(input, NormalizationOptions.None);
    }

    /// <summary>
    /// Normalizes a string with the specified options.
    /// </summary>
    /// <param name="input">The string to normalize.</param>
    /// <param name="options">The normalization options to apply.</param>
    /// <returns>The normalized string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public string Normalize(string input, NormalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input) || options == NormalizationOptions.None)
        {
            return input;
        }

        var result = input;

        // Apply normalizations in order

        // Trim first (before other operations)
        if (options.HasFlag(NormalizationOptions.Trim))
        {
            result = result.Trim();
        }

        // Unicode normalization (decompose takes precedence if both are set)
        if (options.HasFlag(NormalizationOptions.Decompose))
        {
            result = result.Normalize(NormalizationForm.FormD);
        }
        else if (options.HasFlag(NormalizationOptions.Compose))
        {
            result = result.Normalize(NormalizationForm.FormC);
        }

        // Strip accents (must come after decompose)
        if (options.HasFlag(NormalizationOptions.StripAccents))
        {
            result = StripAccents(result);
        }

        // Replace hyphens with spaces
        if (options.HasFlag(NormalizationOptions.ReplaceHyphens))
        {
            result = result.Replace('-', ' ');
            result = result.Replace('–', ' '); // en-dash
            result = result.Replace('—', ' '); // em-dash
        }

        // Lowercase (typically done last to preserve Unicode operations)
        if (options.HasFlag(NormalizationOptions.Lowercase))
        {
            result = result.ToLowerInvariant();
        }

        return result;
    }

    /// <summary>
    /// Strips diacritical marks (accents) from characters.
    /// </summary>
    /// <param name="input">The string to process.</param>
    /// <returns>The string with accents removed.</returns>
    private static string StripAccents(string input)
    {
        // Normalize to NFD (decomposed form) to separate base characters from combining marks
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            // Skip combining diacritical marks
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // Return to NFC (composed form)
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
