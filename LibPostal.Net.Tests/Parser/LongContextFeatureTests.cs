using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Core;
using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;
using TokenType = LibPostal.Net.Tokenization.TokenType;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for long context features (venue name detection for first unknown words).
/// Based on libpostal's address_parser.c lines 1532-1638
/// </summary>
public class LongContextFeatureTests
{
    [Fact]
    public void ExtractFeatures_FirstWordKnown_ShouldNotAddLongContext()
    {
        // Arrange - "main street brooklyn" - "main" is a known word
        var tokenized = CreateTokenizedString("main street brooklyn");

        var vocab = new Trie<uint>();
        vocab.Add("main", 1); // Known word (in vocabulary)
        vocab.Add("street", 2);
        vocab.Add("brooklyn", 3);

        var model = CreateModelWithVocab(vocab);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "main"

        // Assert - No long context features (word is known)
        features.Should().NotContain(f => f.Contains("first word unknown"));
    }

    [Fact]
    public void ExtractFeatures_FirstWordUnknownWithStreetAfterNumber_ShouldAddLongContext()
    {
        // Arrange - "Barboncino 781 Franklin Ave" (venue then number then street)
        var tokenized = CreateTokenizedString("barboncino 781 ave");

        var vocab = new Trie<uint>();
        vocab.Add("ave", 1); // "ave" is known

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("ave", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road }; // Pure STREET

        var crf = new Crf(new[] { "house_number", "road", "city" });

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
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "barboncino" (unknown)

        // Assert - Should detect: unknown word → number → street phrase
        features.Should().Contain("first word unknown+street phrase right:after number");
        features.Should().Contain("first word unknown+street phrase right:after number:ave");
    }

    [Fact]
    public void ExtractFeatures_FirstWordUnknownWithStreetBeforeNumber_ShouldAddLongContext()
    {
        // Arrange - "Pizzeria Calle 781" (venue then street then number) - Spanish pattern
        var tokenized = CreateTokenizedString("pizzeria calle 781");

        var vocab = new Trie<uint>();
        vocab.Add("calle", 1); // "calle" is known

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("calle", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road }; // Pure STREET

        var crf = new Crf(new[] { "house_number", "road" });

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
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "pizzeria" (unknown)

        // Assert - Should detect: unknown word → street phrase → number
        features.Should().Contain("first word unknown+street phrase right:before number");
        features.Should().Contain("first word unknown+street phrase right:before number:calle");
    }

    [Fact]
    public void ExtractFeatures_FirstWordUnknownWithVenuePhrase_ShouldAddLongContext()
    {
        // Arrange - "Barboncino 781 Pizzeria" (venue then number then venue type)
        var tokenized = CreateTokenizedString("barboncino 781 pizzeria");

        var vocab = new Trie<uint>();
        vocab.Add("pizzeria", 1);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("pizzeria", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Name }; // Pure NAME (venue)

        var crf = new Crf(new[] { "house_number", "road", "city" });

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
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "barboncino" (unknown)

        // Assert - Should detect: unknown word → number → venue phrase
        features.Should().Contain("first word unknown+venue phrase right:after number");
        features.Should().Contain("first word unknown+venue phrase right:after number:pizzeria");
    }

    [Fact]
    public void ExtractFeatures_FirstWordUnknownWithAmbiguousAfterNumber_ShouldAddAmbiguous()
    {
        // Arrange - "Barboncino 781 Plaza" where "Plaza" can be NAME or STREET
        var tokenized = CreateTokenizedString("barboncino 781 plaza");

        var vocab = new Trie<uint>();
        vocab.Add("plaza", 1);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("plaza", 0);

        var dictTypes = new uint[] { (uint)(AddressComponent.Road | AddressComponent.Name) }; // Ambiguous

        var crf = new Crf(new[] { "house_number", "road", "city" });

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

        // Assert - Ambiguous phrase after number
        features.Should().Contain("first word unknown+number+ambiguous phrase right");
        features.Should().Contain("first word unknown+number+ambiguous phrase right:plaza");
    }

    [Fact]
    public void ExtractFeatures_FirstWordUnknownWithNumberBeforePhrase_ShouldAddNumberFeature()
    {
        // Arrange - "Barboncino 781 Main Street" (unknown → number → phrase)
        var tokenized = CreateTokenizedString("barboncino 781 ave");

        var vocab = new Trie<uint>();
        vocab.Add("ave", 1);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("ave", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var crf = new Crf(new[] { "house_number", "road" });

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
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert - Number feature with relation to phrase
        features.Should().Contain("first word unknown+number right:before phrase");
        features.Should().Contain("first word unknown+number right:before phrase:781");
    }

    [Fact]
    public void ExtractFeatures_FirstWordUnknownWithNumberAfterVenuePhrase_ShouldAddNumberFeature()
    {
        // Arrange - "Barboncino Pizzeria 781" (unknown → venue phrase → number)
        // Venue phrases don't break, so we continue and find the number
        var tokenized = CreateTokenizedString("barboncino pizzeria 781");

        var vocab = new Trie<uint>();
        vocab.Add("pizzeria", 1);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("pizzeria", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Name }; // Pure NAME (venue)

        var crf = new Crf(new[] { "house_number", "road" });

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
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert - Should find venue phrase first, then continue to find number
        features.Should().Contain("first word unknown+venue phrase right:before number");
        features.Should().Contain("first word unknown+number right:after phrase");
    }

    [Fact]
    public void ExtractFeatures_SecondWordUnknown_ShouldNotAddLongContext()
    {
        // Arrange - Unknown word at index 2, not index 0
        var tokenized = CreateTokenizedString("main barboncino 781");

        var vocab = new Trie<uint>();
        vocab.Add("main", 1); // "main" is known

        var model = CreateModelWithVocab(vocab);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 2, context); // "barboncino" (unknown but not first)

        // Assert - No long context (not at index 0)
        features.Should().NotContain(f => f.Contains("first word unknown"));
    }

    [Fact]
    public void ExtractFeatures_FirstWordPartOfPhrase_ShouldNotAddLongContext()
    {
        // Arrange - First word is part of a known phrase (e.g., "Main Street")
        var tokenized = CreateTokenizedString("main street brooklyn");

        var vocab = new Trie<uint>();
        vocab.Add("main", 1);
        vocab.Add("street", 2);

        var dictPhrases = new Trie<uint>();
        dictPhrases.Add("main street", 0);

        var dictTypes = new uint[] { (uint)AddressComponent.Road };

        var crf = new Crf(new[] { "house_number", "road", "city" });

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
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "main" (part of phrase)

        // Assert - No long context (token is part of a phrase)
        features.Should().NotContain(f => f.Contains("first word unknown"));
    }

    [Fact]
    public void ExtractFeatures_NoNumberOrPhraseToRight_ShouldNotAddLongContext()
    {
        // Arrange - "barboncino" with nothing relevant to the right
        var tokenized = CreateTokenizedString("barboncino");

        var vocab = new Trie<uint>();
        // Empty vocab - "barboncino" is unknown

        var model = CreateModelWithVocab(vocab);
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert - No long context features (nothing found to the right)
        features.Should().NotContain(f => f.Contains("first word unknown"));
    }

    // Helper methods

    private static TokenizedString CreateTokenizedString(string text)
    {
        var tokenizer = new Tokenizer();
        return tokenizer.Tokenize(text);
    }

    private static AddressParserModel CreateModelWithVocab(Trie<uint> vocab)
    {
        var crf = new Crf(new[] { "house_number", "road", "city", "state", "country" });

        return new AddressParserModel(ModelType.CRF, crf, vocab);
    }
}
