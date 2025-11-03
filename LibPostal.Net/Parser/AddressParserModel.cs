using LibPostal.Net.Core;
using LibPostal.Net.ML;

namespace LibPostal.Net.Parser;

/// <summary>
/// Represents a complete address parser model with all components.
/// Based on libpostal's address_parser_t structure.
/// </summary>
public class AddressParserModel
{
    /// <summary>
    /// Gets the model type.
    /// </summary>
    public ModelType Type { get; }

    /// <summary>
    /// Gets the CRF model (null if using Averaged Perceptron).
    /// </summary>
    public Crf? Crf { get; }

    /// <summary>
    /// Gets the vocabulary trie (word features).
    /// </summary>
    public Trie<uint> Vocabulary { get; }

    /// <summary>
    /// Gets the phrases trie (optional - for dictionary phrase features).
    /// </summary>
    public Trie<uint>? Phrases { get; }

    /// <summary>
    /// Gets the phrase types array (optional - corresponds to phrases).
    /// </summary>
    public uint[]? PhraseTypes { get; }

    /// <summary>
    /// Gets the postal codes trie (optional - for postal code features).
    /// </summary>
    public Trie<uint>? PostalCodes { get; }

    /// <summary>
    /// Gets the postal code context graph (optional - for geographic validation).
    /// </summary>
    public Graph? PostalCodeGraph { get; }

    /// <summary>
    /// Gets the component phrases trie (optional - for admin region phrases).
    /// </summary>
    public Trie<uint>? ComponentPhrases { get; }

    /// <summary>
    /// Gets the component phrase types array (optional - corresponds to component phrases).
    /// </summary>
    public ComponentPhraseTypes[]? ComponentPhraseTypes { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressParserModel"/> class.
    /// </summary>
    /// <param name="type">The model type.</param>
    /// <param name="crf">The CRF model.</param>
    /// <param name="vocabulary">The vocabulary trie.</param>
    /// <param name="phrases">The phrases trie (optional).</param>
    /// <param name="phraseTypes">The phrase types array (optional).</param>
    /// <param name="postalCodes">The postal codes trie (optional).</param>
    /// <param name="postalCodeGraph">The postal code context graph (optional).</param>
    /// <param name="componentPhrases">The component phrases trie (optional).</param>
    /// <param name="componentPhraseTypes">The component phrase types array (optional).</param>
    public AddressParserModel(
        ModelType type,
        Crf? crf,
        Trie<uint> vocabulary,
        Trie<uint>? phrases = null,
        uint[]? phraseTypes = null,
        Trie<uint>? postalCodes = null,
        Graph? postalCodeGraph = null,
        Trie<uint>? componentPhrases = null,
        ComponentPhraseTypes[]? componentPhraseTypes = null)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);

        if (type == ModelType.CRF && crf == null)
        {
            throw new ArgumentNullException(nameof(crf), "CRF model is required when type is ModelType.CRF");
        }

        Type = type;
        Crf = crf;
        Vocabulary = vocabulary;
        Phrases = phrases;
        PhraseTypes = phraseTypes;
        PostalCodes = postalCodes;
        PostalCodeGraph = postalCodeGraph;
        ComponentPhrases = componentPhrases;
        ComponentPhraseTypes = componentPhraseTypes;
    }
}

/// <summary>
/// Component phrase types matching libpostal's address_parser_types_t
/// </summary>
public struct ComponentPhraseTypes
{
    /// <summary>
    /// Bitset of possible component boundaries.
    /// </summary>
    public ushort Components { get; set; }

    /// <summary>
    /// Most common component boundary (enum value).
    /// </summary>
    public ushort MostCommon { get; set; }
}

/// <summary>
/// Component phrase boundary types matching libpostal's address_parser_boundary_type_t
/// </summary>
[Flags]
public enum ComponentPhraseBoundary : ushort
{
    None = 0,
    Suburb = 1 << 3,
    CityDistrict = 1 << 4,
    City = 1 << 5,
    Island = 1 << 7,
    StateDistrict = 1 << 8,
    State = 1 << 9,
    CountryRegion = 1 << 11,
    Country = 1 << 13,
    WorldRegion = 1 << 14
}
