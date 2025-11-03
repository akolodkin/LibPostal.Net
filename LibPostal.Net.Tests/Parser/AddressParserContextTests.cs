using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.Core;
using LibPostal.Net.ML;
using LibPostal.Net.Tokenization;
using Token = LibPostal.Net.Tokenization.Token;
using TokenType = LibPostal.Net.Tokenization.TokenType;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for AddressParserContext - manages state during feature extraction.
/// </summary>
public class AddressParserContextTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange
        var tokenized = CreateTokenizedString("123 main street");
        var model = CreateMockModel();

        // Act
        var context = new AddressParserContext(tokenized, model);

        // Assert
        context.Should().NotBeNull();
        context.TokenCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Constructor_WithNullTokenized_ShouldThrow()
    {
        // Arrange
        var model = CreateMockModel();

        // Act
        Action act = () => new AddressParserContext(null!, model);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullModel_ShouldThrow()
    {
        // Arrange
        var tokenized = CreateTokenizedString("test");

        // Act
        Action act = () => new AddressParserContext(tokenized, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FillPhrases_WithDictionaryPhrases_ShouldFindMatches()
    {
        // Arrange
        var tokenized = CreateTokenizedString("main street");

        var trie = new Trie<uint>();
        trie.Add("street", 1);
        trie.Add("main street", 2);

        var model = CreateMockModelWithPhrases(trie);
        var context = new AddressParserContext(tokenized, model);

        // Act
        context.FillPhrases();

        // Assert
        var phrase = context.GetDictionaryPhraseAt(1); // "street" token
        phrase.Should().NotBeNull();
    }

    [Fact]
    public void FillPhrases_WithMultipleMatches_ShouldTrackAll()
    {
        // Arrange
        var tokenized = CreateTokenizedString("main street avenue");

        var trie = new Trie<uint>();
        trie.Add("main", 1);
        trie.Add("street", 2);
        trie.Add("avenue", 3);

        var model = CreateMockModelWithPhrases(trie);
        var context = new AddressParserContext(tokenized, model);

        // Act
        context.FillPhrases();

        // Assert
        context.GetDictionaryPhraseAt(0).Should().NotBeNull(); // "main"
        context.GetDictionaryPhraseAt(1).Should().NotBeNull(); // "street"
        context.GetDictionaryPhraseAt(2).Should().NotBeNull(); // "avenue"
    }

    [Fact]
    public void GetDictionaryPhraseAt_WithNoPhrase_ShouldReturnNull()
    {
        // Arrange
        var tokenized = CreateTokenizedString("random text");
        var model = CreateMockModel();
        var context = new AddressParserContext(tokenized, model);

        context.FillPhrases();

        // Act
        var phrase = context.GetDictionaryPhraseAt(0);

        // Assert
        phrase.Should().BeNull();
    }

    [Fact]
    public void GetDictionaryPhraseAt_WithInvalidIndex_ShouldThrow()
    {
        // Arrange
        var tokenized = CreateTokenizedString("test");
        var model = CreateMockModel();
        var context = new AddressParserContext(tokenized, model);

        // Act
        Action act1 = () => context.GetDictionaryPhraseAt(-1);
        Action act2 = () => context.GetDictionaryPhraseAt(100);

        // Assert
        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FillPhrases_WithEmptyModel_ShouldNotCrash()
    {
        // Arrange
        var tokenized = CreateTokenizedString("test");
        var model = CreateMockModel(); // no phrases
        var context = new AddressParserContext(tokenized, model);

        // Act
        Action act = () => context.FillPhrases();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void HasDictionaryPhrase_WithAssignedToken_ShouldReturnTrue()
    {
        // Arrange
        var tokenized = CreateTokenizedString("street");

        var trie = new Trie<uint>();
        trie.Add("street", 1);

        var model = CreateMockModelWithPhrases(trie);
        var context = new AddressParserContext(tokenized, model);

        context.FillPhrases();

        // Act & Assert
        context.HasDictionaryPhrase(0).Should().BeTrue();
    }

    [Fact]
    public void HasDictionaryPhrase_WithUnassignedToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenized = CreateTokenizedString("random");
        var model = CreateMockModel();
        var context = new AddressParserContext(tokenized, model);

        context.FillPhrases();

        // Act & Assert
        context.HasDictionaryPhrase(0).Should().BeFalse();
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

    private static AddressParserModel CreateMockModelWithPhrases(Trie<uint> phrases)
    {
        var crf = new Crf(new[] { "house_number", "road", "city" });
        var vocab = new Trie<uint>();

        return new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            phrases,  // phrases trie
            null,     // phraseTypes
            null,     // postalCodes
            null      // postalCodeGraph
        );
    }
}
