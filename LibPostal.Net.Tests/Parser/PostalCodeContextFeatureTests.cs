using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Core;
using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;
using TokenType = LibPostal.Net.Tokenization.TokenType;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for postal code context features (graph-based validation).
/// Based on libpostal's address_parser.c lines 1262-1319
/// </summary>
public class PostalCodeContextFeatureTests
{
    [Fact]
    public void ExtractFeatures_WithPostalCodeAndCity_ShouldAddHaveContext()
    {
        // Arrange - "Brooklyn 11216" where 11216 is valid for Brooklyn
        var tokenized = CreateTokenizedString("brooklyn 11216");
        // Tokens: 0=brooklyn, 1=space, 2=11216

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100); // Component phrase ID 100 (city)

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 200); // Postal code ID 200

        var graph = new Graph(numNodes: 1000); // Large enough for test node IDs
        graph.AddEdge(200, 100); // Postal code 200 is valid for component 100

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context); // Token "11216"

        // Assert
        features.Should().Contain("postcode have context");
        features.Should().Contain("postcode have context:11216");
    }

    [Fact]
    public void ExtractFeatures_WithPostalCodeAndState_ShouldAddHaveContext()
    {
        // Arrange - "NY 10001" where 10001 is valid for NY
        var tokenized = CreateTokenizedString("ny 10001");
        // Tokens: 0=ny, 1=space, 2=10001

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("ny", 50); // Component phrase ID 50 (state)

        var postalCodes = new Trie<uint>();
        postalCodes.Add("10001", 300); // Postal code ID 300

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(300, 50); // Postal code 300 is valid for component 50

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert
        features.Should().Contain("postcode have context");
        features.Should().Contain("postcode have context:10001");
    }

    [Fact]
    public void ExtractFeatures_WithPostalCodeNoAdminPhrase_ShouldAddNoContext()
    {
        // Arrange - "123 Main 11216" - postal code but no adjacent component phrase
        var tokenized = CreateTokenizedString("main 11216");
        // Tokens: 0=main, 1=space, 2=11216

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 200);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(200, 100); // Graph has edges, but no component phrase found

        var model = CreateModelWithPostalCodeGraph(null, null, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert
        features.Should().Contain("postcode no context:11216");
        features.Should().NotContain("postcode have context");
        features.Should().NotContain("postcode have context:11216");
    }

    [Fact]
    public void ExtractFeatures_WithPostalCodeInvalidContext_ShouldAddNoContext()
    {
        // Arrange - "Brooklyn 90210" - Brooklyn with LA postal code (no graph edge)
        var tokenized = CreateTokenizedString("brooklyn 90210");
        // Tokens: 0=brooklyn, 1=space, 2=90210

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100); // Brooklyn ID 100

        var postalCodes = new Trie<uint>();
        postalCodes.Add("90210", 500); // LA postal code ID 500

        var graph = new Graph(numNodes: 1000);
        // No edge from 500 to 100 (90210 not valid for Brooklyn)

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert
        features.Should().Contain("postcode no context:90210");
        features.Should().NotContain("postcode have context");
    }

    [Fact]
    public void ExtractFeatures_PostalCodeWithPreviousAdmin_ChecksGraph()
    {
        // Arrange - Component phrase BEFORE postal code: "NY 10001"
        var tokenized = CreateTokenizedString("ny 10001");

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("ny", 50);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("10001", 300);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(300, 50);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert - Should find previous admin (NY) and validate
        features.Should().Contain("postcode have context");
    }

    [Fact]
    public void ExtractFeatures_PostalCodeWithNextAdmin_ChecksGraph()
    {
        // Arrange - Component phrase AFTER postal code: "11216 Brooklyn"
        var tokenized = CreateTokenizedString("11216 brooklyn");
        // Tokens: 0=11216, 1=space, 2=brooklyn

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 200);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(200, 100);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // Token "11216"

        // Assert - Should find next admin (Brooklyn) and validate
        features.Should().Contain("postcode have context");
    }

    [Fact]
    public void ExtractFeatures_PostalCodeMultiToken_ChecksBoundaries()
    {
        // Arrange - Multi-token postal code "10001 2345" (numeric to avoid tokenization splitting)
        var tokenized = CreateTokenizedString("brooklyn 10001 2345");
        // Tokens: 0=brooklyn, 1=space, 2=10001, 3=space, 4=2345

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 400);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("10001 2345", 600); // Multi-token postal code

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(600, 400);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act - Check both tokens of the multi-token postal code
        var features2 = extractor.ExtractFeatures(tokenized, 2, context); // "10001"
        var features4 = extractor.ExtractFeatures(tokenized, 4, context); // "2345"

        // Assert - Both tokens should have context features (all tokens in a phrase get the features)
        features2.Should().Contain("postcode have context");
        features4.Should().Contain("postcode have context");
    }

    [Fact]
    public void ExtractFeatures_NoPostalCodeGraph_SkipsFeature()
    {
        // Arrange - Postal code exists but no graph in model
        var tokenized = CreateTokenizedString("brooklyn 11216");

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 200);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        // Create model WITHOUT postal code graph
        var crf = new Crf(new[] { "house_number", "road", "city", "postcode" });
        var vocab = new Trie<uint>();
        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            null, // dictionary phrases
            null, // dictionary phrase types
            postalCodes, // postal codes
            null, // NO GRAPH
            componentPhrases,
            componentTypes
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert - Should NOT add postal code context features (no graph)
        features.Should().NotContain(f => f.Contains("postcode"));
    }

    [Fact]
    public void ExtractFeatures_PostalCodeHaveContext_AddsWordFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("brooklyn 11216");

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 200);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(200, 100);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert - Should have both unigram and bigram features
        features.Should().Contain("postcode have context");
        features.Should().Contain("postcode have context:11216");
    }

    [Fact]
    public void ExtractFeatures_PostalCodeNoContext_AddsWordFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("main 11216");

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 200);

        var graph = new Graph(numNodes: 1000);

        var model = CreateModelWithPostalCodeGraph(null, null, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert - Should have bigram feature (no unigram for "no context")
        features.Should().Contain("postcode no context:11216");
        features.Should().NotContain("postcode no context"); // No unigram version
    }

    [Fact]
    public void ExtractFeatures_MultiplePostalCodes_HandlesEach()
    {
        // Arrange - "NY 10001 CA 90210" - two postal codes with contexts
        var tokenized = CreateTokenizedString("ny 10001 ca 90210");
        // Tokens: 0=ny, 1=space, 2=10001, 3=space, 4=ca, 5=space, 6=90210

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("ny", 50);
        componentPhrases.Add("ca", 60);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("10001", 300);
        postalCodes.Add("90210", 500);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(300, 50); // 10001 valid for NY
        graph.AddEdge(500, 60); // 90210 valid for CA

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            },
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features1 = extractor.ExtractFeatures(tokenized, 2, context); // 10001
        var features2 = extractor.ExtractFeatures(tokenized, 6, context); // 90210

        // Assert - Both postal codes should have context
        features1.Should().Contain("postcode have context:10001");
        features2.Should().Contain("postcode have context:90210");
    }

    [Fact]
    public void ExtractFeatures_PostalCodeAtBoundary_HandlesEdgeCase()
    {
        // Arrange - Postal code at start: "11216 Brooklyn"
        var tokenized = CreateTokenizedString("11216 brooklyn");
        // Tokens: 0=11216, 1=space, 2=brooklyn

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 200);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(200, 100);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithPostalCodeGraph(componentPhrases, componentTypes, postalCodes, graph);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // First token

        // Assert - Should handle boundary correctly and check next token
        features.Should().Contain("postcode have context");
    }

    // Helper methods

    private static TokenizedString CreateTokenizedString(string text)
    {
        var tokenizer = new Tokenizer();
        return tokenizer.Tokenize(text);
    }

    private static AddressParserModel CreateModelWithPostalCodeGraph(
        Trie<uint>? componentPhrases,
        ComponentPhraseTypes[]? componentTypes,
        Trie<uint>? postalCodes,
        Graph? postalCodeGraph)
    {
        var crf = new Crf(new[] { "house_number", "road", "city", "state", "postcode" });
        var vocab = new Trie<uint>();

        return new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            null, // dictionary phrases
            null, // dictionary phrase types
            postalCodes,
            postalCodeGraph, // Postal code graph
            componentPhrases,
            componentTypes
        );
    }
}
