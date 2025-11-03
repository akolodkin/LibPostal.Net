using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Core;
using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for dictionary phrase features in AddressFeatureExtractor.
/// These features use address dictionaries (street types, unit designators, etc.)
/// to improve parsing accuracy.
/// </summary>
public class DictionaryPhraseFeatureTests
{
    [Fact]
    public void ExtractFeatures_WithContext_ShouldIncludePhraseFeatures()
    {
        // Arrange
        var tokenized = CreateTokenizedString("main street");
        // Tokens: 0=main, 1=space, 2=street

        var phrases = new Trie<uint>();
        phrases.Add("street", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Road });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act - index 2 = "street"
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert
        features.Should().Contain(f => f.StartsWith("phrase:"));
    }

    [Fact]
    public void ExtractFeatures_WithStreetPhrase_ShouldAddStreetFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("oak avenue");

        // Create phrase trie with "avenue" as street type
        var phrases = new Trie<uint>();
        phrases.Add("avenue", 0); // ID 0 to match phraseTypes[0]

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Road });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act - index 2 = "avenue" (0=oak, 1=space, 2=avenue)
        var features = extractor.ExtractFeatures(tokenized, 2, context);

        // Assert
        features.Should().Contain("phrase:street");
    }

    [Fact]
    public void ExtractFeatures_WithUnitPhrase_ShouldAddUnitFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("apt 5");

        var phrases = new Trie<uint>();
        phrases.Add("apt", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Unit });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().Contain("phrase:unit");
    }

    [Fact]
    public void ExtractFeatures_WithMultiTokenPhrase_ShouldAddPhraseToAllTokens()
    {
        // Arrange
        var tokenized = CreateTokenizedString("central park west");
        // Tokens: 0=central, 1=space, 2=park, 3=space, 4=west

        var phrases = new Trie<uint>();
        phrases.Add("central park west", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Road });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act - extract for actual word tokens (not whitespace)
        var features0 = extractor.ExtractFeatures(tokenized, 0, context); // "central"
        var features2 = extractor.ExtractFeatures(tokenized, 2, context); // "park"
        var features4 = extractor.ExtractFeatures(tokenized, 4, context); // "west"

        // Assert - all tokens in phrase should have phrase feature
        features0.Should().Contain("phrase:street");
        features2.Should().Contain("phrase:street");
        features4.Should().Contain("phrase:street");
    }

    [Fact]
    public void ExtractFeatures_WithUnambiguousPhrase_ShouldAddUnambiguousFeature()
    {
        // Arrange - phrase with only ONE component type
        var tokenized = CreateTokenizedString("apartment");

        var phrases = new Trie<uint>();
        phrases.Add("apartment", 1);

        // Only Unit component (unambiguous)
        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Unit });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().Contain("phrase:unit");
        features.Should().Contain("unambiguous phrase type=unit");
    }

    [Fact]
    public void ExtractFeatures_WithAmbiguousPhrase_ShouldNotAddUnambiguousFeature()
    {
        // Arrange - phrase with multiple component types
        var tokenized = CreateTokenizedString("north");

        var phrases = new Trie<uint>();
        phrases.Add("north", 0); // ID 0 to match phraseTypes[0]

        // Both Road AND City components (ambiguous)
        var components = (uint)AddressComponent.Road | (uint)AddressComponent.City;
        var model = CreateModelWithPhrases(phrases, new uint[] { components });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().NotContain(f => f.StartsWith("unambiguous"));
    }

    [Fact]
    public void ExtractFeatures_WithPrefixPhrase_ShouldAddPrefixFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("hinterhaus");

        var phrases = new Trie<uint>();
        phrases.Add("|hinter", 1); // | prefix marker

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.House });
        var context = new AddressParserContext(tokenized, model);
        // For prefix, we'd need special handling in FillPhrases or use PhraseMatcher directly

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert - should have prefix feature
        features.Should().Contain(f => f.StartsWith("prefix="));
    }

    [Fact]
    public void ExtractFeatures_WithSuffixPhrase_ShouldAddSuffixFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("mainstreet");

        var phrases = new Trie<uint>();
        phrases.Add("street|", 0); // | suffix marker

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Road });
        var context = new AddressParserContext(tokenized, model);

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().Contain(f => f.StartsWith("suffix="));
        features.Should().Contain("suffix=street");
    }

    [Fact]
    public void ExtractFeatures_WithLevelPhrase_ShouldAddLevelFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("floor 3");

        var phrases = new Trie<uint>();
        phrases.Add("floor", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Level });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().Contain("phrase:level");
    }

    [Fact]
    public void ExtractFeatures_WithPOBoxPhrase_ShouldAddPOBoxFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("po box");
        // Tokens: 0=po, 1=space, 2=box

        var phrases = new Trie<uint>();
        phrases.Add("po box", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.POBox });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act - extract for word tokens
        var features0 = extractor.ExtractFeatures(tokenized, 0, context); // "po"
        var features2 = extractor.ExtractFeatures(tokenized, 2, context); // "box"

        // Assert
        features0.Should().Contain("phrase:po_box");
        features2.Should().Contain("phrase:po_box");
    }

    [Fact]
    public void ExtractFeatures_WithNoPhrase_ShouldNotAddPhraseFeatures()
    {
        // Arrange
        var tokenized = CreateTokenizedString("randomword");
        var model = CreateMockModel();
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().NotContain(f => f.StartsWith("phrase:"));
    }

    [Fact]
    public void ExtractFeatures_WithDirectionalPhrase_ShouldAddDirectionalFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("north");

        var phrases = new Trie<uint>();
        phrases.Add("north", 1);

        // Directional typically maps to Road component
        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Road });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().Contain("phrase:street"); // directionals are street-related
    }

    [Fact]
    public void ExtractFeatures_BackwardCompatibility_WithoutContext_ShouldStillWork()
    {
        // Arrange
        var tokenized = CreateTokenizedString("123 main");
        // Tokens: 0=123, 1=space, 2=main
        var extractor = new AddressFeatureExtractor();

        // Act - call without context (old 2-param API)
        var features = extractor.ExtractFeatures(tokenized, 2); // index 2 = "main"

        // Assert - should still work without phrase features
        features.Should().Contain("bias");
        features.Should().Contain("word=main");
        features.Should().NotContain(f => f.StartsWith("phrase:"));
    }

    [Fact]
    public void ExtractFeatures_WithEntrancePhrase_ShouldAddEntranceFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("entrance a");

        var phrases = new Trie<uint>();
        phrases.Add("entrance", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Entrance });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().Contain("phrase:entrance");
    }

    [Fact]
    public void ExtractFeatures_WithStaircasePhrase_ShouldAddStaircaseFeature()
    {
        // Arrange
        var tokenized = CreateTokenizedString("staircase b");

        var phrases = new Trie<uint>();
        phrases.Add("staircase", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Staircase });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert
        features.Should().Contain("phrase:staircase");
    }

    [Fact]
    public void ExtractFeatures_WithMultipleComponentTypes_ShouldAddAllRelevantFeatures()
    {
        // Arrange
        var tokenized = CreateTokenizedString("building");

        var phrases = new Trie<uint>();
        phrases.Add("building", 1);

        // Building can be both House and Name
        var components = (uint)AddressComponent.House | (uint)AddressComponent.Name;
        var model = CreateModelWithPhrases(phrases, new uint[] { components });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context);

        // Assert - should add features for both components
        features.Should().Contain("phrase:house");
        features.Should().Contain("phrase:name");
    }

    [Fact]
    public void ExtractFeatures_PhraseOverridesWordFeature_WhenPhraseLonger()
    {
        // Arrange - multi-token phrase should override individual word features
        var tokenized = CreateTokenizedString("saint james street");

        var phrases = new Trie<uint>();
        phrases.Add("saint james street", 1);

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Road });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        var extractor = new AddressFeatureExtractor();

        // Act
        var features = extractor.ExtractFeatures(tokenized, 0, context); // "saint"

        // Assert
        features.Should().Contain("phrase:street");
        // The word feature should still exist but phrase feature is more informative
        features.Should().Contain("word=saint");
    }

    // Helper methods

    private static TokenizedString CreateTokenizedString(string text)
    {
        var tokenizer = new Tokenizer();
        return tokenizer.Tokenize(text);
    }

    private static AddressParserModel CreateMockModel()
    {
        var crf = new Crf(new[] { "house_number", "road", "city" });
        var vocab = new Trie<uint>();

        return new AddressParserModel(ModelType.CRF, crf, vocab);
    }

    private static AddressParserModel CreateModelWithPhrases(Trie<uint> phrases, uint[] phraseTypes)
    {
        var crf = new Crf(new[] { "house_number", "road", "city", "state", "unit" });
        var vocab = new Trie<uint>();

        return new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            phrases,
            phraseTypes,
            null,
            null
        );
    }

    private static AddressParserContext CreateContextWithPhrase(string phraseText, uint phraseId)
    {
        var tokenized = CreateTokenizedString(phraseText);

        var phrases = new Trie<uint>();
        phrases.Add(phraseText, 0); // Always use 0 to match phraseTypes[0]

        var model = CreateModelWithPhrases(phrases, new uint[] { (uint)AddressComponent.Road });
        var context = new AddressParserContext(tokenized, model);
        context.FillPhrases();

        return context;
    }
}

/// <summary>
/// Address component flags matching libpostal's address_parser_types.h
/// </summary>
[Flags]
public enum AddressComponent : uint
{
    None = 0,
    HouseNumber = 1 << 0,
    House = 1 << 1,
    Category = 1 << 2,
    Near = 1 << 3,
    Road = 1 << 4,
    Unit = 1 << 5,
    Level = 1 << 6,
    Staircase = 1 << 7,
    Entrance = 1 << 8,
    POBox = 1 << 9,
    Postcode = 1 << 10,
    Suburb = 1 << 11,
    CityDistrict = 1 << 12,
    City = 1 << 13,
    Island = 1 << 14,
    StateDistrict = 1 << 15,
    State = 1 << 16,
    CountryRegion = 1 << 17,
    Country = 1 << 18,
    WorldRegion = 1 << 19,
    Name = 1 << 20,
    Toponym = 1 << 21
}
