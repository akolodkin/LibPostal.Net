using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Parser;

/// <summary>
/// Parses addresses into labeled components using CRF.
/// Based on libpostal's address_parser.c
/// </summary>
public class AddressParser
{
    private readonly Crf _crf;
    private readonly Tokenizer _tokenizer;
    private readonly AddressFeatureExtractor _featureExtractor;
    private readonly AddressParserModel? _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressParser"/> class.
    /// </summary>
    /// <param name="crf">The trained CRF model.</param>
    public AddressParser(Crf crf)
    {
        ArgumentNullException.ThrowIfNull(crf);

        _crf = crf;
        _tokenizer = new Tokenizer();
        _featureExtractor = new AddressFeatureExtractor();
        _model = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressParser"/> class with a full model.
    /// </summary>
    /// <param name="model">The address parser model.</param>
    public AddressParser(AddressParserModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.Crf == null)
        {
            throw new ArgumentException("Model must contain a CRF.", nameof(model));
        }

        _crf = model.Crf;
        _tokenizer = new Tokenizer();
        _featureExtractor = new AddressFeatureExtractor();
        _model = model;
    }

    /// <summary>
    /// Loads an address parser from a data directory.
    /// </summary>
    /// <param name="dataDirectory">The directory containing model files.</param>
    /// <returns>A new AddressParser instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dataDirectory is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory doesn't exist.</exception>
    public static AddressParser LoadFromDirectory(string dataDirectory)
    {
        ArgumentNullException.ThrowIfNull(dataDirectory);

        var model = AddressParserModelLoader.LoadFromDirectory(dataDirectory);
        return new AddressParser(model);
    }

    /// <summary>
    /// Parses an address string into labeled components.
    /// </summary>
    /// <param name="address">The address string to parse.</param>
    /// <returns>The parsed address with component labels.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="address"/> is null.</exception>
    public AddressParserResponse Parse(string address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (string.IsNullOrWhiteSpace(address))
        {
            return new AddressParserResponse(Array.Empty<string>(), Array.Empty<string>());
        }

        // Step 1: Tokenize
        var tokenized = _tokenizer.Tokenize(address.ToLowerInvariant());

        // Get non-whitespace tokens (CRF operates on these)
        var nonWhitespaceTokens = tokenized.GetTokensWithoutWhitespace().ToList();

        if (nonWhitespaceTokens.Count == 0)
        {
            return new AddressParserResponse(Array.Empty<string>(), Array.Empty<string>());
        }

        // Step 2: Prepare CRF for inference
        _crf.PrepareForInference(nonWhitespaceTokens.Count);

        // Step 3: Extract features for each token and score
        for (int i = 0; i < tokenized.Count; i++)
        {
            var token = tokenized[i];

            // Skip whitespace tokens
            if (token.Type == TokenType.Whitespace || token.Type == TokenType.Newline)
            {
                continue;
            }

            // Extract features
            var features = _featureExtractor.ExtractFeatures(tokenized, i);

            // Find the index in non-whitespace list
            var nonWhitespaceIdx = nonWhitespaceTokens.FindIndex(t => t.Offset == token.Offset);

            if (nonWhitespaceIdx >= 0)
            {
                // Score token with features
                _crf.ScoreToken(nonWhitespaceIdx, features, prevTagFeatures: null);
            }
        }

        // Step 4: Run Viterbi to get optimal label sequence
        var labelIds = _crf.Predict();

        // Step 5: Build response
        var components = new List<string>();
        var labels = new List<string>();

        for (int i = 0; i < nonWhitespaceTokens.Count; i++)
        {
            components.Add(nonWhitespaceTokens[i].Text);
            labels.Add(_crf.Classes[labelIds[i]]);
        }

        return new AddressParserResponse(components.ToArray(), labels.ToArray());
    }
}
