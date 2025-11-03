using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Core;
using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;
using TokenType = LibPostal.Net.Tokenization.TokenType;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for phrase-aware context window features.
/// Based on libpostal's address_parser.c lines 1119-1527
/// </summary>
public class PhraseAwareContextTests
{
    [Fact]
    public void ExtractFeatures_SimplePrevNextWord_ShouldWork()
    {
        // Arrange - No phrases, baseline test
        var tokenized = CreateTokenizedString("123 main brooklyn");
        // Tokens: 0=123, 1=space, 2=main, 3=space, 4=brooklyn

        var model = CreateMockModel();
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context); // "main"

        // Assert - Simple word context
        features.Should().Contain("prev_word=123");
        features.Should().Contain("next_word=brooklyn");
    }

    [Fact]
    public void ExtractFeatures_DictionaryPhraseInPrevContext_ShouldUseFullPhrase()
    {
        // Arrange - "fifth avenue" is a phrase, "brooklyn" follows
        var tokenized = CreateTokenizedString("fifth avenue brooklyn");
        // Tokens: 0=fifth, 1=space, 2=avenue, 3=space, 4=brooklyn

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("fifth avenue", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var model = CreateModelWithDictionaryPhrases(dictPhrases, dictTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 4, context); // "brooklyn"

        // Assert - Previous context should be the full phrase "fifth avenue", not just "avenue"
        features.Should().Contain("prev_word=fifth avenue");
        features.Should().Contain("prev_word+word=fifth avenue brooklyn");
    }

    [Fact]
    public void ExtractFeatures_DictionaryPhraseInNextContext_ShouldUseFullPhrase()
    {
        // Arrange - "123" followed by "main street" phrase
        var tokenized = CreateTokenizedString("123 main street");
        // Tokens: 0=123, 1=space, 2=main, 3=space, 4=street

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("main street", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var model = CreateModelWithDictionaryPhrases(dictPhrases, dictTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "123"

        // Assert - Next context should be the full phrase "main street", not just "main"
        features.Should().Contain("next_word=main street");
        features.Should().Contain("word+next_word=123 main street");
    }

    [Fact]
    public void ExtractFeatures_ComponentPhraseInPrevContext_ShouldUsePhrase()
    {
        // Arrange - "brooklyn" (component phrase) followed by "11216"
        var tokenized = CreateTokenizedString("brooklyn 11216");
        // Tokens: 0=brooklyn, 1=space, 2=11216

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithComponentPhrases(componentPhrases, componentTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context); // "11216"

        // Assert - Previous context should be "brooklyn" (component phrase)
        features.Should().Contain("prev_word=brooklyn");
    }

    [Fact]
    public void ExtractFeatures_ComponentPhraseInNextContext_ShouldUsePhrase()
    {
        // Arrange - "11216" followed by "brooklyn" (component phrase)
        var tokenized = CreateTokenizedString("11216 brooklyn");
        // Tokens: 0=11216, 1=space, 2=brooklyn

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var model = CreateModelWithComponentPhrases(componentPhrases, componentTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "11216"

        // Assert - Next context should be "brooklyn" (component phrase)
        features.Should().Contain("next_word=brooklyn");
    }

    [Fact]
    public void ExtractFeatures_TokenWithinPhrase_ShouldHaveSharedContext()
    {
        // Arrange - All tokens within "fifth avenue" should share the same context boundaries
        var tokenized = CreateTokenizedString("123 fifth avenue brooklyn");
        // Tokens: 0=123, 1=space, 2=fifth, 3=space, 4=avenue, 5=space, 6=brooklyn

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("fifth avenue", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var model = CreateModelWithDictionaryPhrases(dictPhrases, dictTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features2 = extractor.ExtractFeatures(tokenized, 2, context); // "fifth"
        var features4 = extractor.ExtractFeatures(tokenized, 4, context); // "avenue"

        // Assert - Both tokens should have same context: prev="123", next="brooklyn"
        features2.Should().Contain("prev_word=123");
        features2.Should().Contain("next_word=brooklyn");

        features4.Should().Contain("prev_word=123");
        features4.Should().Contain("next_word=brooklyn");
    }

    [Fact]
    public void ExtractFeatures_PhrasePriority_DictionaryOverComponent()
    {
        // Arrange - Both dictionary and component phrase at same location, dictionary wins
        var tokenized = CreateTokenizedString("washington street brooklyn");
        // Tokens: 0=washington, 1=space, 2=street, 3=space, 4=brooklyn

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("washington street", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("washington", 100); // Also a state

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            }
        };

        var crf = new Crf(new[] { "house_number", "road", "city", "state" });
        var vocab = new Trie<uint>();

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            dictPhrases,
            dictTypes,
            null,
            null,
            componentPhrases,
            componentTypes
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 4, context); // "brooklyn"

        // Assert - Previous context should treat "washington street" as a single dictionary phrase
        features.Should().Contain("prev_word=washington street");
    }

    [Fact]
    public void ExtractFeatures_MultiplePhrasesAdjustBoundaries()
    {
        // Arrange - "fifth avenue" phrase followed by "brooklyn ny" component phrases
        var tokenized = CreateTokenizedString("fifth avenue brooklyn ny");
        // Tokens: 0=fifth, 1=space, 2=avenue, 3=space, 4=brooklyn, 5=space, 6=ny

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("fifth avenue", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);
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

        var crf = new Crf(new[] { "house_number", "road", "city", "state" });
        var vocab = new Trie<uint>();

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            dictPhrases,
            dictTypes,
            null,
            null,
            componentPhrases,
            componentTypes
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 4, context); // "brooklyn"

        // Assert - Previous context should be "fifth avenue", next should be "ny"
        features.Should().Contain("prev_word=fifth avenue");
        features.Should().Contain("next_word=ny");
    }

    [Fact]
    public void ExtractFeatures_BigramWithPhrase_ShouldConcatenateCorrectly()
    {
        // Arrange
        var tokenized = CreateTokenizedString("123 main street");

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("main street", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var model = CreateModelWithDictionaryPhrases(dictPhrases, dictTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "123"

        // Assert - Bigram should concatenate correctly
        features.Should().Contain("word+next_word=123 main street");
    }

    [Fact]
    public void ExtractFeatures_PhraseAtBoundary_HandlesCorrectly()
    {
        // Arrange - Phrase at the start
        var tokenized = CreateTokenizedString("main street brooklyn");

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("main street", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var model = CreateModelWithDictionaryPhrases(dictPhrases, dictTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "main"

        // Assert - No prev_word (at start), but has next_word
        features.Should().NotContain(f => f.StartsWith("prev_word"));
        features.Should().Contain("next_word=brooklyn");
    }

    [Fact]
    public void ExtractFeatures_PostalCodeAdjustsBoundaries()
    {
        // Arrange - Postal code should also adjust boundaries
        var tokenized = CreateTokenizedString("brooklyn 10001 2345");

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("brooklyn", 100);

        var postalCodes = new Trie<uint>();
        postalCodes.Add("10001 2345", 200);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.City,
                MostCommon = (ushort)ComponentPhraseBoundary.City
            }
        };

        var crf = new Crf(new[] { "house_number", "road", "city", "postcode" });
        var vocab = new Trie<uint>();

        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            null,
            null,
            postalCodes,
            null,
            componentPhrases,
            componentTypes
        );

        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act - Check token inside postal code phrase
        var features = extractor.ExtractFeatures(tokenized, 2, context); // "10001"

        // Assert - Context should jump over the postal code phrase
        features.Should().Contain("prev_word=brooklyn");
        features.Should().NotContain(f => f.Contains("next_word")); // At end
    }

    [Fact]
    public void ExtractFeatures_MultiTokenComponentPhrase_UsedInContext()
    {
        // Arrange - Multi-token component phrase like "New York"
        var tokenized = CreateTokenizedString("123 new york");
        // Tokens: 0=123, 1=space, 2=new, 3=space, 4=york

        var componentPhrases = new Trie<uint>();
        componentPhrases.Add("new york", 100);

        var componentTypes = new ComponentPhraseTypes[]
        {
            new ComponentPhraseTypes
            {
                Components = (ushort)ComponentPhraseBoundary.State,
                MostCommon = (ushort)ComponentPhraseBoundary.State
            }
        };

        var model = CreateModelWithComponentPhrases(componentPhrases, componentTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "123"

        // Assert - Next context should be full phrase "new york"
        features.Should().Contain("next_word=new york");
    }

    [Fact]
    public void ExtractFeatures_NoPhrasesAtAll_FallsBackToWordContext()
    {
        // Arrange - No phrases detected
        var tokenized = CreateTokenizedString("123 main brooklyn");

        var model = CreateMockModel();
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context); // "main"

        // Assert - Falls back to simple word context
        features.Should().Contain("prev_word=123");
        features.Should().Contain("next_word=brooklyn");
    }

    [Fact]
    public void ExtractFeatures_PhraseContextOverridesSimpleContext()
    {
        // Arrange - With phrases, should NOT have simple adjacent word context
        var tokenized = CreateTokenizedString("123 main street brooklyn");

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("main street", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var model = CreateModelWithDictionaryPhrases(dictPhrases, dictTypes);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context); // "main" (first token of phrase)

        // Assert - Should have phrase-aware context, not simple "main->street" context
        features.Should().Contain("prev_word=123");
        features.Should().Contain("next_word=brooklyn");
        // Should NOT have "next_word=street" (that would be simple word context)
        features.Should().NotContain("next_word=street");
    }

    // Helper methods

    private static TokenizedString CreateTokenizedString(string text)
    {
        var tokenizer = new Tokenizer();
        return tokenizer.Tokenize(text);
    }

    private static AddressParserModel CreateMockModel()
    {
        var crf = new Crf(new[] { "house_number", "road", "city", "state", "country" });
        var vocab = new Trie<uint>();

        return new AddressParserModel(ModelType.CRF, crf, vocab);
    }

    private static AddressParserModel CreateModelWithDictionaryPhrases(
        Trie<uint> dictPhrases,
        uint[] dictTypes)
    {
        var crf = new Crf(new[] { "house_number", "road", "city", "state" });
        var vocab = new Trie<uint>();

        return new AddressParserModel(
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
    }

    private static AddressParserModel CreateModelWithComponentPhrases(
        Trie<uint> componentPhrases,
        ComponentPhraseTypes[] componentTypes)
    {
        var crf = new Crf(new[] { "house_number", "road", "city", "state", "country" });
        var vocab = new Trie<uint>();

        return new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            null,
            null,
            null,
            null,
            componentPhrases,
            componentTypes
        );
    }
}
