using System.Globalization;
using System.Text;

namespace LibPostal.Net.Core;

/// <summary>
/// Utility functions for string manipulation, particularly UTF-8 string operations.
/// Based on libpostal's string_utils.c
/// </summary>
public static class StringUtils
{
    /// <summary>
    /// Reverses a UTF-8 encoded string, properly handling multi-byte characters.
    /// </summary>
    /// <param name="input">The string to reverse.</param>
    /// <returns>The reversed string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public static string Reverse(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            return input;

        // Use TextElementEnumerator to properly handle grapheme clusters
        // This correctly handles combining characters, surrogate pairs, etc.
        var enumerator = StringInfo.GetTextElementEnumerator(input);
        var elements = new List<string>();

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.GetTextElement());
        }

        elements.Reverse();
        return string.Concat(elements);
    }

    /// <summary>
    /// Trims leading and trailing whitespace from a string.
    /// </summary>
    /// <param name="input">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    public static string Trim(string input)
    {
        return input?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Splits a string by a delimiter character.
    /// </summary>
    /// <param name="input">The string to split.</param>
    /// <param name="delimiter">The delimiter character.</param>
    /// <returns>An array of substrings.</returns>
    public static string[] Split(string input, char delimiter)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
            return Array.Empty<string>();

        return input.Split(delimiter);
    }

    /// <summary>
    /// Determines whether a string is null, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="input">The string to test.</param>
    /// <returns>True if the string is null, empty, or whitespace; otherwise, false.</returns>
    public static bool IsNullOrWhiteSpace(string? input)
    {
        return string.IsNullOrWhiteSpace(input);
    }

    /// <summary>
    /// Normalizes a string using the specified Unicode normalization form.
    /// </summary>
    /// <param name="input">The string to normalize.</param>
    /// <param name="normalizationForm">The normalization form to use.</param>
    /// <returns>The normalized string.</returns>
    public static string Normalize(string input, NormalizationForm normalizationForm)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.Normalize(normalizationForm);
    }

    /// <summary>
    /// Converts a string to lowercase using invariant culture.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The lowercase string.</returns>
    public static string ToLower(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to uppercase using invariant culture.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The uppercase string.</returns>
    public static string ToUpper(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.ToUpperInvariant();
    }
}
