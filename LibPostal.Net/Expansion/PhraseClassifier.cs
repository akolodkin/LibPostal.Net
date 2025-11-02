namespace LibPostal.Net.Expansion;

/// <summary>
/// Classifies phrases for address expansion.
/// Wrapper around GazetteerClassifier that works at the phrase level.
/// Based on libpostal's address_phrase_* helper functions.
/// </summary>
public static class PhraseClassifier
{
    /// <summary>
    /// Determines if a phrase is ignorable for the specified components.
    /// </summary>
    /// <param name="phrase">The phrase to check.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if the phrase can be ignored for these components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="phrase"/> is null.</exception>
    public static bool IsIgnorableForComponents(Phrase phrase, AddressComponent components)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        if (phrase.Expansions == null)
            return false;

        // Check if ANY expansion is ignorable for the components
        foreach (var expansion in phrase.Expansions.Expansions)
        {
            if (GazetteerClassifier.IsIgnorableForComponents(expansion.DictionaryType, components))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a phrase is edge-ignorable for the specified components.
    /// </summary>
    /// <param name="phrase">The phrase to check.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if the phrase can be ignored at edges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="phrase"/> is null.</exception>
    public static bool IsEdgeIgnorableForComponents(Phrase phrase, AddressComponent components)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        if (phrase.Expansions == null)
            return false;

        // Check if ANY expansion is edge-ignorable
        foreach (var expansion in phrase.Expansions.Expansions)
        {
            if (GazetteerClassifier.IsEdgeIgnorableForComponents(expansion.DictionaryType, components))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a phrase could be a root for the specified components.
    /// </summary>
    /// <param name="phrase">The phrase to check.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if the phrase could be the root/core part.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="phrase"/> is null.</exception>
    public static bool IsPossibleRootForComponents(Phrase phrase, AddressComponent components)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        if (phrase.Expansions == null)
            return false;

        // Check if ANY expansion is a possible root
        foreach (var expansion in phrase.Expansions.Expansions)
        {
            if (GazetteerClassifier.IsPossibleRootForComponents(expansion.DictionaryType, components))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a phrase has a canonical interpretation (full form).
    /// </summary>
    /// <param name="phrase">The phrase to check.</param>
    /// <returns>True if the phrase has at least one canonical expansion.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="phrase"/> is null.</exception>
    public static bool HasCanonicalInterpretation(Phrase phrase)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        if (phrase.Expansions == null)
            return false;

        // Check if ANY expansion has a canonical form
        return phrase.Expansions.Expansions.Any(e => e.Canonical != null);
    }

    /// <summary>
    /// Determines if a phrase is in a specific dictionary type.
    /// </summary>
    /// <param name="phrase">The phrase to check.</param>
    /// <param name="dictionaryType">The dictionary type to check for.</param>
    /// <returns>True if the phrase is in this dictionary type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="phrase"/> is null.</exception>
    public static bool InDictionary(Phrase phrase, DictionaryType dictionaryType)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        if (phrase.Expansions == null)
            return false;

        // Check if ANY expansion matches the dictionary type
        return phrase.Expansions.Expansions.Any(e => e.DictionaryType == dictionaryType);
    }

    /// <summary>
    /// Determines if a phrase is valid for the specified components.
    /// </summary>
    /// <param name="phrase">The phrase to check.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if the phrase is valid for these components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="phrase"/> is null.</exception>
    public static bool IsValidForComponents(Phrase phrase, AddressComponent components)
    {
        ArgumentNullException.ThrowIfNull(phrase);

        if (phrase.Expansions == null)
            return false;

        // Check if ANY expansion is valid for the components
        foreach (var expansion in phrase.Expansions.Expansions)
        {
            var validComponents = GazetteerClassifier.GetValidComponents(expansion.DictionaryType);
            if ((validComponents & components) != 0)
            {
                return true;
            }
        }

        return false;
    }
}
