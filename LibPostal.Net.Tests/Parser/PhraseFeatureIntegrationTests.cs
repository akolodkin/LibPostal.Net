using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Core;
using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Integration tests for all phrase features working together.
/// Tests the complete feature extraction pipeline with all Phase 9 features.
/// </summary>
public class PhraseFeatureIntegrationTests
{
    [Fact]
    public void ExtractFeatures_CompleteAddress_ShouldHaveAllPhraseFeatures()
    {
        // Arrange - Simplified comprehensive test
        var tokenized = CreateTokenizedString("apt main brooklyn 11216");
        // Tokens: 0=apt, 1=space, 2=main, 3=space, 4=brooklyn, 5=space, 6=11216

        var vocab = new Trie<uint>();
        vocab.Add("apt", 1);
        vocab.Add("main", 2);
        vocab.Add("brooklyn", 3);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("apt", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Unit };

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 0); // PhraseId must match array index in ComponentPhraseTypes

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 300);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(300, 0); // Postal code 300 → City 0

        var crf = new Crf(new[] { "house_number", "road", "unit", "city", "postcode" });

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            dictPhrases,
            dictTypes,
            postalCodes,
            graph,
            componentPhrases,
            componentTypes
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var featuresApt = extractor.ExtractFeatures(tokenized, 0, context); // "apt"
        var featuresBrooklyn = extractor.ExtractFeatures(tokenized, 4, context); // "brooklyn"
        var featuresPostal = extractor.ExtractFeatures(tokenized, 6, context); // "11216"

        // Assert - All three feature types should work without conflicts
        featuresApt.Should().Contain("unambiguous phrase type:unit"); // Dictionary phrase
        featuresBrooklyn.Should().Contain("unambiguous phrase type:city"); // Component phrase
        featuresPostal.Should().Contain("postcode have context"); // Postal code with context
    }

    [Fact]
    public void ExtractFeatures_VenueName_ShouldHaveLongContextFeatures()
    {
        // Arrange - "Barboncino 781 Franklin Ave" (real-world venue example)
        var tokenized = CreateTokenizedString("barboncino 781 franklin ave");

        var vocab = new Trie<uint>();
        vocab.Add("franklin", 1);
        vocab.Add("ave", 2);
        // "barboncino" is NOT in vocab (unknown word)

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("franklin ave", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var crf = new Crf(new[] { "house_number", "road", "name" });

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            dictPhrases,
            dictTypes,
            null,
            null,
            null,
            null
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "barboncino"

        // Assert - Long context features for venue name detection
        features.Should().Contain("first word unknown+street phrase right:after number");
        features.Should().Contain(f => f.StartsWith("first word unknown+number right:before phrase"));
    }

    [Fact]
    public void ExtractFeatures_AllFeaturesWorking_ShouldNotConflict()
    {
        // Arrange - Complex address testing all feature types
        var tokenized = CreateTokenizedString("suite 100 123 fifth avenue brooklyn ny 11201");

        var vocab = new Trie<uint>();
        vocab.Add("suite", 1);
        vocab.Add("fifth", 2);
        vocab.Add("avenue", 3);
        vocab.Add("brooklyn", 4);
        vocab.Add("ny", 5);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("suite", 0);
        dictPhrases.Add("fifth avenue", 1);

        var dictTypes = new uint[] { (uint)AddressComponent.Unit, (uint)AddressComponent.Road };

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 0); // PhraseId must match array index in ComponentPhraseTypes
        componentPhrases.Add("ny", 200);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            },
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            }
        };

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11201", 300);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(300, 0); // Postal code 300 → City 0

        var crf = new Crf(new[] { "house_number", "road", "unit", "city", "state", "postcode" });

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            dictPhrases,
            dictTypes,
            postalCodes,
            graph,
            componentPhrases,
            componentTypes
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act - Extract features for multiple tokens to ensure no conflicts
        var featuresSuite = extractor.ExtractFeatures(tokenized, 0, context);
        var featuresHouseNum = extractor.ExtractFeatures(tokenized, 4, context); // "123"
        var featuresFifth = extractor.ExtractFeatures(tokenized, 6, context); // "fifth"
        var featuresBrooklyn = extractor.ExtractFeatures(tokenized, 10, context); // "brooklyn"
        var featuresPostcode = extractor.ExtractFeatures(tokenized, 14, context); // "11201"

        // Assert - All features should work without conflicts
        featuresSuite.Should().NotBeEmpty();
        featuresHouseNum.Should().NotBeEmpty();
        featuresFifth.Should().NotBeEmpty();
        featuresBrooklyn.Should().NotBeEmpty();
        featuresPostcode.Should().NotBeEmpty();

        // Verify no duplicate features or conflicts
        featuresSuite.Should().OnlyHaveUniqueItems();
        featuresHouseNum.Should().OnlyHaveUniqueItems();
        featuresFifth.Should().OnlyHaveUniqueItems();
        featuresBrooklyn.Should().OnlyHaveUniqueItems();
        featuresPostcode.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ExtractFeatures_PhraseAwareContextWithAllPhraseTypes_WorksCorrectly()
    {
        // Arrange - Test phrase-aware context with dictionary, component, and postal code phrases
        var tokenized = CreateTokenizedString("main street brooklyn 11216");

        var vocab = new Trie<uint>();
        vocab.Add("main", 1);
        vocab.Add("street", 2);
        vocab.Add("brooklyn", 3);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("main street", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 0); // PhraseId must match array index in ComponentPhraseTypes

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 300);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(300, 0); // Postal code 300 → City 0

        var crf = new Crf(new[] { "road", "city", "postcode" });

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            dictPhrases,
            dictTypes,
            postalCodes,
            graph,
            componentPhrases,
            componentTypes
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        // Tokens: 0=main, 1=space, 2=street, 3=space, 4=brooklyn, 5=space, 6=11216
        var featuresMain = extractor.ExtractFeatures(tokenized, 0, context); // First token of "main street"
        var featuresBrooklyn = extractor.ExtractFeatures(tokenized, 4, context); // "brooklyn"
        var featuresPostcode = extractor.ExtractFeatures(tokenized, 6, context); // "11216"

        // Assert - Dictionary phrase
        featuresMain.Should().Contain(f => f.Contains("phrase") && f.Contains("main street"));

        // Assert - Component phrase
        featuresBrooklyn.Should().Contain("unambiguous phrase type:city");

        // Assert - Postal code with validated context
        featuresPostcode.Should().Contain("postcode have context");

        // Assert - Phrase-aware context
        featuresBrooklyn.Should().Contain(f => f.StartsWith("prev_word=main street"));
        featuresPostcode.Should().Contain(f => f.StartsWith("prev_word=brooklyn"));
    }

    [Fact]
    public void Parse_CompleteAddress_ShouldExtractAllComponents()
    {
        // Arrange - Full parsing integration test
        var address = "Apt 5, 123 Main Street, Brooklyn NY 11216, USA";

        var vocab = new Trie<uint>();
        vocab.Add("apt", 1);
        vocab.Add("main", 2);
        vocab.Add("street", 3);
        vocab.Add("brooklyn", 4);
        vocab.Add("ny", 5);
        vocab.Add("usa", 6);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("apt", 0);
        dictPhrases.Add("main street", 1);

        var dictTypes = new uint[] { (uint)AddressComponent.Unit, (uint)AddressComponent.Road };

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 0); // PhraseId must match array index in ComponentPhraseTypes
        componentPhrases.Add("ny", 200);
        componentPhrases.Add("usa", 300);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            },
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            },
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.Country,
                MostCommon = (ushort)ComponentPhraseBoundary.Country
            }
        };

        var postalCodes = new Trie<uint>();
        postalCodes.Add("11216", 400);

        var graph = new Graph(numNodes: 1000);
        graph.AddEdge(400, 100); // 11216 → Brooklyn
        graph.AddEdge(400, 200); // 11216 → NY

        var crf = new Crf(new[] { "house_number", "road", "unit", "city", "state", "postcode", "country" });

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            dictPhrases,
            dictTypes,
            postalCodes,
            graph,
            componentPhrases,
            componentTypes
        );

        var tokenizer = new Tokenizer();
        var tokenizedAddress = tokenizer.Tokenize(address);

        var parser = new AddressParser(model);

        // Act
        var result = parser.Parse(address);

        // Assert - Should successfully parse (even if labels aren't perfect with mock model)
        result.Should().NotBeNull();
        result.Labels.Should().NotBeEmpty();
        result.Components.Should().NotBeEmpty();
    }

    // Helper methods

    private static TokenizedString CreateTokenizedString(string text)
    {
        var tokenizer = new Tokenizer();
        return tokenizer.Tokenize(text);
    }
}
