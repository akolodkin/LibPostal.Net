using LibPostal.Net.Core;
using LibPostal.Net.Tokenization;
using Token = LibPostal.Net.Tokenization.Token;
using TokenType = LibPostal.Net.Tokenization.TokenType;
using System.Text;

namespace LibPostal.Net.Parser;

/// <summary>
/// Matches phrases from a trie against token sequences.
/// Based on libpostal's phrase matching logic in address_parser.c
/// </summary>
public class PhraseMatcher
{
    private readonly Trie<uint> _phraseTrie;
    private const char PrefixMarker = '|'; // Prefix phrases start with |
    private const char SuffixMarker = '|'; // Suffix phrases end with |

    /// <summary>
    /// Initializes a new instance of the <see cref="PhraseMatcher"/> class.
    /// </summary>
    /// <param name="phraseTrie">The trie containing phrases.</param>
    /// <exception cref="ArgumentNullException">Thrown when phraseTrie is null.</exception>
    public PhraseMatcher(Trie<uint> phraseTrie)
    {
        ArgumentNullException.ThrowIfNull(phraseTrie);
        _phraseTrie = phraseTrie;
    }

    /// <summary>
    /// Searches for phrases starting at the specified token index.
    /// </summary>
    /// <param name="tokens">The token array.</param>
    /// <param name="startIndex">The starting token index.</param>
    /// <param name="normalized">Whether to use normalized token text.</param>
    /// <returns>Collection of phrase matches.</returns>
    public IEnumerable<PhraseMatch> SearchTokens(Token[] tokens, int startIndex, bool normalized = false)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        if (startIndex < 0 || startIndex >= tokens.Length)
            yield break;

        if (tokens.Length == 0)
            yield break;

        // Try to build phrases of increasing length starting from startIndex
        var phraseBuilder = new List<string>();

        for (int endIndex = startIndex; endIndex < tokens.Length; endIndex++)
        {
            var token = tokens[endIndex];

            // Skip whitespace tokens in phrase building
            if (token.Type == TokenType.Whitespace || token.Type == TokenType.Newline)
                continue;

            // Add token text to phrase
            var tokenText = normalized ? NormalizeText(token.Text) : token.Text;
            phraseBuilder.Add(tokenText);

            // Build phrase string (space-separated)
            var phrase = string.Join(" ", phraseBuilder);

            // Check if this phrase exists in trie
            if (_phraseTrie.TryGetData(phrase, out var phraseId))
            {
                yield return new PhraseMatch
                {
                    PhraseText = phrase,
                    PhraseId = phraseId,
                    StartIndex = startIndex,
                    EndIndex = endIndex,
                    Length = phraseBuilder.Count
                };
            }
        }
    }

    /// <summary>
    /// Searches for prefix phrases in a word.
    /// </summary>
    /// <param name="word">The word to search for prefixes.</param>
    /// <returns>Collection of prefix phrase matches.</returns>
    public IEnumerable<PhraseMatch> SearchPrefixes(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        if (string.IsNullOrEmpty(word))
            yield break;

        // Search for keys that start with | (prefix marker)
        var prefixKeys = _phraseTrie.GetKeysWithPrefix(PrefixMarker.ToString());

        foreach (var (key, phraseId) in prefixKeys)
        {
            // Remove the | marker to get actual prefix
            var prefix = key.Substring(1);

            // Check if word starts with this prefix
            if (word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return new PhraseMatch
                {
                    PhraseText = prefix,
                    PhraseId = phraseId,
                    StartIndex = 0,
                    EndIndex = 0,
                    Length = 1
                };
            }
        }
    }

    /// <summary>
    /// Searches for suffix phrases in a word.
    /// </summary>
    /// <param name="word">The word to search for suffixes.</param>
    /// <returns>Collection of suffix phrase matches.</returns>
    public IEnumerable<PhraseMatch> SearchSuffixes(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        if (string.IsNullOrEmpty(word))
            yield break;

        // Get all keys and filter for suffixes
        var allKeys = _phraseTrie.GetAllKeys();

        foreach (var key in allKeys)
        {
            // Check if key ends with | (suffix marker)
            if (key.EndsWith(SuffixMarker))
            {
                // Remove the | marker to get actual suffix
                var suffix = key.Substring(0, key.Length - 1);

                // Check if word ends with this suffix
                if (word.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    if (_phraseTrie.TryGetData(key, out var phraseId))
                    {
                        yield return new PhraseMatch
                        {
                            PhraseText = suffix,
                            PhraseId = phraseId,
                            StartIndex = 0,
                            EndIndex = 0,
                            Length = 1
                        };
                    }
                }
            }
        }
    }

    /// <summary>
    /// Normalizes text (lowercase and remove accents).
    /// </summary>
    private static string NormalizeText(string text)
    {
        // Decompose to NFD, remove combining marks, then lowercase
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = char.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().ToLowerInvariant();
    }
}

/// <summary>
/// Represents a matched phrase from the trie.
/// </summary>
public record PhraseMatch
{
    /// <summary>
    /// Gets or sets the matched phrase text.
    /// </summary>
    public required string PhraseText { get; init; }

    /// <summary>
    /// Gets or sets the phrase ID from the trie.
    /// </summary>
    public required uint PhraseId { get; init; }

    /// <summary>
    /// Gets or sets the starting token index.
    /// </summary>
    public required int StartIndex { get; init; }

    /// <summary>
    /// Gets or sets the ending token index (inclusive).
    /// </summary>
    public required int EndIndex { get; init; }

    /// <summary>
    /// Gets or sets the number of non-whitespace tokens in the phrase.
    /// </summary>
    public required int Length { get; init; }
}
