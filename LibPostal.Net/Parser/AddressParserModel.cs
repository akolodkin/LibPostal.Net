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
    /// Initializes a new instance of the <see cref="AddressParserModel"/> class.
    /// </summary>
    /// <param name="type">The model type.</param>
    /// <param name="crf">The CRF model.</param>
    /// <param name="vocabulary">The vocabulary trie.</param>
    /// <param name="phrases">The phrases trie (optional).</param>
    /// <param name="phraseTypes">The phrase types array (optional).</param>
    /// <param name="postalCodes">The postal codes trie (optional).</param>
    /// <param name="postalCodeGraph">The postal code context graph (optional).</param>
    public AddressParserModel(
        ModelType type,
        Crf? crf,
        Trie<uint> vocabulary,
        Trie<uint>? phrases = null,
        uint[]? phraseTypes = null,
        Trie<uint>? postalCodes = null,
        Graph? postalCodeGraph = null)
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
    }
}
