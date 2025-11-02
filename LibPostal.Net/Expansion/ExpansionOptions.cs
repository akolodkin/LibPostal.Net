namespace LibPostal.Net.Expansion;

/// <summary>
/// Options for address expansion and normalization.
/// Based on libpostal's libpostal_normalize_options_t structure.
/// </summary>
public class ExpansionOptions
{
    /// <summary>
    /// Gets or sets the languages to use for expansion.
    /// Empty array means auto-detect language.
    /// </summary>
    public string[] Languages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the address components to include in expansion.
    /// </summary>
    public AddressComponent AddressComponents { get; init; } = AddressComponent.All;

    // String normalization options

    /// <summary>
    /// Gets or sets a value indicating whether to convert to Latin-ASCII.
    /// </summary>
    public bool LatinAscii { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to transliterate non-Latin scripts.
    /// </summary>
    public bool Transliterate { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to strip diacritical marks.
    /// </summary>
    public bool StripAccents { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to apply NFD (decompose) normalization.
    /// </summary>
    public bool Decompose { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to convert to lowercase.
    /// </summary>
    public bool Lowercase { get; init; } = true; // Default: true

    /// <summary>
    /// Gets or sets a value indicating whether to trim leading/trailing whitespace.
    /// </summary>
    public bool TrimString { get; init; } = true; // Default: true

    /// <summary>
    /// Gets or sets a value indicating whether to drop content in parentheses.
    /// </summary>
    public bool DropParentheticals { get; init; }

    // Token normalization options

    /// <summary>
    /// Gets or sets a value indicating whether to replace numeric hyphens with spaces.
    /// </summary>
    public bool ReplaceNumericHyphens { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete numeric hyphens.
    /// </summary>
    public bool DeleteNumericHyphens { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to split alphabetic from numeric characters.
    /// </summary>
    public bool SplitAlphaFromNumeric { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to replace word hyphens with spaces.
    /// </summary>
    public bool ReplaceWordHyphens { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete word hyphens.
    /// </summary>
    public bool DeleteWordHyphens { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete final periods from tokens.
    /// </summary>
    public bool DeleteFinalPeriods { get; init; } = true; // Default: true

    /// <summary>
    /// Gets or sets a value indicating whether to delete periods from acronyms.
    /// </summary>
    public bool DeleteAcronymPeriods { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to drop English possessives.
    /// </summary>
    public bool DropEnglishPossessives { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete apostrophes.
    /// </summary>
    public bool DeleteApostrophes { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to expand numeric expressions.
    /// </summary>
    public bool ExpandNumex { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to convert Roman numerals to digits.
    /// </summary>
    public bool RomanNumerals { get; init; }

    /// <summary>
    /// Gets the default expansion options.
    /// </summary>
    /// <returns>Default options matching libpostal behavior.</returns>
    public static ExpansionOptions GetDefault()
    {
        return new ExpansionOptions
        {
            Languages = Array.Empty<string>(),
            AddressComponents = AddressComponent.All,
            Lowercase = true,
            TrimString = true,
            DeleteFinalPeriods = true
        };
    }
}
