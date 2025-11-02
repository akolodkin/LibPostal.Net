namespace LibPostal.Net.Tokenization;

/// <summary>
/// Detects the Unicode script of text.
/// Based on libpostal's unicode_scripts support.
/// </summary>
public static class UnicodeScriptDetector
{
    /// <summary>
    /// Detects the dominant Unicode script in the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>The detected Unicode script.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    public static UnicodeScript DetectScript(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (string.IsNullOrEmpty(text))
        {
            return UnicodeScript.Unknown;
        }

        // Count characters per script
        var scriptCounts = new Dictionary<UnicodeScript, int>();

        foreach (var ch in text)
        {
            // Skip whitespace and punctuation
            if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsDigit(ch))
                continue;

            var script = GetCharacterScript(ch);
            if (script != UnicodeScript.Unknown)
            {
                scriptCounts.TryGetValue(script, out var count);
                scriptCounts[script] = count + 1;
            }
        }

        // Return the dominant script
        if (scriptCounts.Count == 0)
        {
            return UnicodeScript.Unknown;
        }

        return scriptCounts.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    private static UnicodeScript GetCharacterScript(char ch)
    {
        // Latin (including Latin Extended)
        if ((ch >= 0x0041 && ch <= 0x005A) ||  // A-Z
            (ch >= 0x0061 && ch <= 0x007A) ||  // a-z
            (ch >= 0x00C0 && ch <= 0x00FF) ||  // Latin-1 Supplement
            (ch >= 0x0100 && ch <= 0x017F) ||  // Latin Extended-A
            (ch >= 0x0180 && ch <= 0x024F))    // Latin Extended-B
        {
            return UnicodeScript.Latin;
        }

        // Cyrillic
        if ((ch >= 0x0400 && ch <= 0x04FF) ||  // Cyrillic
            (ch >= 0x0500 && ch <= 0x052F))    // Cyrillic Supplement
        {
            return UnicodeScript.Cyrillic;
        }

        // Arabic
        if ((ch >= 0x0600 && ch <= 0x06FF) ||  // Arabic
            (ch >= 0x0750 && ch <= 0x077F))    // Arabic Supplement
        {
            return UnicodeScript.Arabic;
        }

        // Hebrew
        if (ch >= 0x0590 && ch <= 0x05FF)
        {
            return UnicodeScript.Hebrew;
        }

        // Greek
        if ((ch >= 0x0370 && ch <= 0x03FF) ||  // Greek and Coptic
            (ch >= 0x1F00 && ch <= 0x1FFF))    // Greek Extended
        {
            return UnicodeScript.Greek;
        }

        // Hangul (Korean)
        if ((ch >= 0xAC00 && ch <= 0xD7AF) ||  // Hangul Syllables
            (ch >= 0x1100 && ch <= 0x11FF) ||  // Hangul Jamo
            (ch >= 0x3130 && ch <= 0x318F))    // Hangul Compatibility Jamo
        {
            return UnicodeScript.Hangul;
        }

        // CJK Ideographs (Han)
        if ((ch >= 0x4E00 && ch <= 0x9FFF) ||   // CJK Unified Ideographs
            (ch >= 0x3400 && ch <= 0x4DBF) ||   // CJK Extension A
            (ch >= 0x20000 && ch <= 0x2A6DF) || // CJK Extension B
            (ch >= 0xF900 && ch <= 0xFAFF))     // CJK Compatibility Ideographs
        {
            return UnicodeScript.Han;
        }

        // Hiragana (Japanese)
        if (ch >= 0x3040 && ch <= 0x309F)
        {
            return UnicodeScript.Hiragana;
        }

        // Katakana (Japanese)
        if ((ch >= 0x30A0 && ch <= 0x30FF) ||  // Katakana
            (ch >= 0x31F0 && ch <= 0x31FF))    // Katakana Phonetic Extensions
        {
            return UnicodeScript.Katakana;
        }

        // Thai
        if (ch >= 0x0E00 && ch <= 0x0E7F)
        {
            return UnicodeScript.Thai;
        }

        // Devanagari
        if (ch >= 0x0900 && ch <= 0x097F)
        {
            return UnicodeScript.Devanagari;
        }

        return UnicodeScript.Unknown;
    }
}
