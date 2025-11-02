using System.Globalization;
using System.Text.RegularExpressions;

namespace LibPostal.Net.Tokenization;

/// <summary>
/// Tokenizes text into a sequence of tokens.
/// Based on libpostal's tokenization logic.
/// </summary>
/// <remarks>
/// This tokenizer uses .NET regular expressions instead of libpostal's re2c-generated scanner.
/// It detects various token types including words, numbers, punctuation, emails, URLs, and Unicode characters.
/// </remarks>
public partial class Tokenizer
{
    // Regex patterns for token detection (ordered by priority)

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"https?://[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
    private static partial Regex NumericPattern();

    [GeneratedRegex(@"[a-zA-Z]+\.(?:[a-zA-Z]+\.)*", RegexOptions.Compiled)]
    private static partial Regex AcronymPattern();

    [GeneratedRegex(@"[a-zA-Z]+", RegexOptions.Compiled)]
    private static partial Regex WordPattern();

    [GeneratedRegex(@"\n", RegexOptions.Compiled)]
    private static partial Regex NewlinePattern();

    [GeneratedRegex(@"[ \t\r]+", RegexOptions.Compiled)]
    private static partial Regex WhitespacePattern();

    /// <summary>
    /// Tokenizes the input string.
    /// </summary>
    /// <param name="input">The string to tokenize.</param>
    /// <returns>A <see cref="TokenizedString"/> containing the original string and extracted tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is null.</exception>
    public TokenizedString Tokenize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
        {
            return new TokenizedString(input, []);
        }

        var tokens = new List<Token>();
        var position = 0;

        while (position < input.Length)
        {
            var token = GetNextToken(input, position);
            if (token.HasValue)
            {
                tokens.Add(token.Value);
                position = token.Value.End;
            }
            else
            {
                // Fallback: treat as single character
                tokens.Add(new Token(
                    input.Substring(position, 1),
                    TokenType.Other,
                    position,
                    1));
                position++;
            }
        }

        return new TokenizedString(input, tokens);
    }

    private Token? GetNextToken(string input, int position)
    {
        // Try patterns in priority order

        // Email (must come before word/punctuation patterns)
        var emailMatch = EmailPattern().Match(input, position);
        if (emailMatch.Success && emailMatch.Index == position)
        {
            return new Token(
                emailMatch.Value,
                TokenType.Email,
                position,
                emailMatch.Length);
        }

        // URL
        var urlMatch = UrlPattern().Match(input, position);
        if (urlMatch.Success && urlMatch.Index == position)
        {
            return new Token(
                urlMatch.Value,
                TokenType.Url,
                position,
                urlMatch.Length);
        }

        // Newline
        if (position < input.Length && input[position] == '\n')
        {
            return new Token("\n", TokenType.Newline, position, 1);
        }

        // Whitespace (spaces, tabs)
        var whitespaceMatch = WhitespacePattern().Match(input, position);
        if (whitespaceMatch.Success && whitespaceMatch.Index == position)
        {
            return new Token(
                whitespaceMatch.Value,
                TokenType.Whitespace,
                position,
                whitespaceMatch.Length);
        }

        // Check for ideographic/Hangul characters
        if (position < input.Length)
        {
            var charInfo = CharUnicodeInfo.GetUnicodeCategory(input[position]);

            // Ideographic characters (CJK)
            if (IsIdeographic(input[position]))
            {
                return new Token(
                    input.Substring(position, 1),
                    TokenType.IdeographicChar,
                    position,
                    1);
            }

            // Hangul syllables (Korean)
            if (IsHangul(input[position]))
            {
                return new Token(
                    input.Substring(position, 1),
                    TokenType.HangulSyllable,
                    position,
                    1);
            }
        }

        // Acronym (e.g., "U.S.A.")
        var acronymMatch = AcronymPattern().Match(input, position);
        if (acronymMatch.Success && acronymMatch.Index == position)
        {
            return new Token(
                acronymMatch.Value,
                TokenType.Acronym,
                position,
                acronymMatch.Length);
        }

        // Numeric
        var numericMatch = NumericPattern().Match(input, position);
        if (numericMatch.Success && numericMatch.Index == position)
        {
            return new Token(
                numericMatch.Value,
                TokenType.Numeric,
                position,
                numericMatch.Length);
        }

        // Word
        var wordMatch = WordPattern().Match(input, position);
        if (wordMatch.Success && wordMatch.Index == position)
        {
            return new Token(
                wordMatch.Value,
                TokenType.Word,
                position,
                wordMatch.Length);
        }

        // Single character punctuation
        if (position < input.Length)
        {
            var ch = input[position];
            var punctuationType = GetPunctuationType(ch);
            if (punctuationType != null)
            {
                return new Token(
                    ch.ToString(),
                    punctuationType.Value,
                    position,
                    1);
            }
        }

        return null;
    }

    private static bool IsIdeographic(char c)
    {
        // CJK Unified Ideographs and Extensions
        return (c >= 0x4E00 && c <= 0x9FFF) ||   // CJK Unified Ideographs
               (c >= 0x3400 && c <= 0x4DBF) ||   // CJK Extension A
               (c >= 0x20000 && c <= 0x2A6DF) || // CJK Extension B
               (c >= 0x2A700 && c <= 0x2B73F) || // CJK Extension C
               (c >= 0x2B740 && c <= 0x2B81F) || // CJK Extension D
               (c >= 0x2B820 && c <= 0x2CEAF) || // CJK Extension E
               (c >= 0xF900 && c <= 0xFAFF) ||   // CJK Compatibility Ideographs
               (c >= 0x2F800 && c <= 0x2FA1F);   // CJK Compatibility Ideographs Supplement
    }

    private static bool IsHangul(char c)
    {
        // Hangul Syllables
        return c >= 0xAC00 && c <= 0xD7AF;
    }

    private static TokenType? GetPunctuationType(char c)
    {
        return c switch
        {
            '.' => TokenType.Period,
            ',' => TokenType.Comma,
            ';' => TokenType.Semicolon,
            ':' => TokenType.Colon,
            '!' => TokenType.Exclamation,
            '?' => TokenType.Question,
            '\'' => TokenType.Quote,
            '"' => TokenType.DoubleQuote,
            '(' => TokenType.LeftParen,
            ')' => TokenType.RightParen,
            '[' => TokenType.LeftBracket,
            ']' => TokenType.RightBracket,
            '{' => TokenType.LeftBrace,
            '}' => TokenType.RightBrace,
            '-' => TokenType.Hyphen,
            '–' or '—' => TokenType.Dash, // en-dash, em-dash
            '_' => TokenType.Underscore,
            '+' => TokenType.Plus,
            '&' => TokenType.Ampersand,
            '@' => TokenType.At,
            '#' => TokenType.Pound,
            '$' => TokenType.Dollar,
            '%' => TokenType.Percent,
            '*' => TokenType.Asterisk,
            _ => null
        };
    }
}
