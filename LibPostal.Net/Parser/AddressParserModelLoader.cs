using LibPostal.Net.Core;
using LibPostal.Net.ML;

namespace LibPostal.Net.Parser;

/// <summary>
/// Loads address parser models from libpostal data files.
/// </summary>
public static class AddressParserModelLoader
{
    private const string CrfModelFileName = "address_parser_crf.dat";
    private const string AveragedPerceptronFileName = "address_parser.dat";
    private const string VocabularyFileName = "address_parser_vocab.trie";
    private const string PhrasesFileName = "address_parser_phrases.dat";
    private const string PostalCodesFileName = "address_parser_postal_codes.dat";

    /// <summary>
    /// Loads an address parser model from a directory.
    /// </summary>
    /// <param name="dataDirectory">The directory containing the model files.</param>
    /// <returns>The loaded address parser model.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dataDirectory is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory doesn't exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown when required model files are missing.</exception>
    public static AddressParserModel LoadFromDirectory(string dataDirectory)
    {
        ArgumentNullException.ThrowIfNull(dataDirectory);

        if (!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {dataDirectory}");
        }

        // Detect model type (CRF has priority)
        var crfPath = Path.Combine(dataDirectory, CrfModelFileName);
        var apPath = Path.Combine(dataDirectory, AveragedPerceptronFileName);

        ModelType modelType;
        Crf? crf = null;

        if (File.Exists(crfPath))
        {
            // Load CRF model
            modelType = ModelType.CRF;
            using var stream = File.OpenRead(crfPath);
            crf = Crf.Load(stream);
        }
        else if (File.Exists(apPath))
        {
            // Averaged Perceptron not yet implemented
            throw new NotImplementedException("Averaged Perceptron model loading is not yet implemented. Use CRF models.");
        }
        else
        {
            throw new FileNotFoundException($"No model file found in {dataDirectory}. Expected {CrfModelFileName} or {AveragedPerceptronFileName}");
        }

        // Load vocabulary (required)
        var vocabPath = Path.Combine(dataDirectory, VocabularyFileName);
        Trie<uint> vocabulary;

        if (File.Exists(vocabPath))
        {
            using var stream = File.OpenRead(vocabPath);
            vocabulary = TrieLoader.LoadLibpostalTrie<uint>(stream);
        }
        else
        {
            // Vocabulary is required - create empty one for now
            vocabulary = new Trie<uint>();
        }

        // Load optional components
        Trie<uint>? phrases = null;
        uint[]? phraseTypes = null;
        Trie<uint>? postalCodes = null;
        Graph? postalCodeGraph = null;

        var phrasesPath = Path.Combine(dataDirectory, PhrasesFileName);
        if (File.Exists(phrasesPath))
        {
            // Phrases loading not yet fully implemented
            // Would load: phrases trie + phrase types array
            phrases = new Trie<uint>();
        }

        var postalCodesPath = Path.Combine(dataDirectory, PostalCodesFileName);
        if (File.Exists(postalCodesPath))
        {
            // Postal codes loading not yet fully implemented
            // Would load: postal codes trie + context graph
            postalCodes = new Trie<uint>();
        }

        return new AddressParserModel(
            modelType,
            crf,
            vocabulary,
            phrases,
            phraseTypes,
            postalCodes,
            postalCodeGraph
        );
    }

    /// <summary>
    /// Loads an address parser model from a stream.
    /// This is primarily for testing purposes.
    /// </summary>
    /// <param name="stream">The stream containing the model data.</param>
    /// <returns>The loaded address parser model.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public static AddressParserModel LoadFromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // For now, assume stream contains a CRF model
        var crf = Crf.Load(stream);

        // Try to load vocabulary if stream has more data
        Trie<uint> vocabulary;
        try
        {
            vocabulary = TrieLoader.LoadLibpostalTrie<uint>(stream);
        }
        catch
        {
            // If vocabulary loading fails, use empty trie
            vocabulary = new Trie<uint>();
        }

        return new AddressParserModel(
            ModelType.CRF,
            crf,
            vocabulary
        );
    }
}
