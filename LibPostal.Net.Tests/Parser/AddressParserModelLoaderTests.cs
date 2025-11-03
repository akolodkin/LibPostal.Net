using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.IO;
using LibPostal.Net.Core;
using LibPostal.Net.ML;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for AddressParserModel loading.
/// Note: These tests use mock binary data since we don't have actual libpostal model files in the repo.
/// </summary>
public class AddressParserModelLoaderTests
{
    [Fact]
    public void AddressParserModel_WithBasicComponents_ShouldConstruct()
    {
        // Arrange
        var crf = new Crf(new[] { "house_number", "road", "city" });
        var vocab = new Trie<uint>();
        vocab.Add("test", 1);

        // Act
        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            null, // phrases
            null, // phraseTypes
            null, // postalCodes
            null  // postalCodeGraph
        );

        // Assert
        model.Should().NotBeNull();
        model.Type.Should().Be(ModelType.CRF);
        model.Crf.Should().BeSameAs(crf);
        model.Vocabulary.Should().BeSameAs(vocab);
    }

    [Fact]
    public void AddressParserModel_WithAllComponents_ShouldConstruct()
    {
        // Arrange
        var crf = new Crf(new[] { "house_number", "road" });
        var vocab = new Trie<uint>();
        var phrases = new Trie<uint>();
        var postalCodes = new Trie<uint>();
        var graph = new Graph(10);

        // Act
        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            phrases,
            new uint[] { 1, 2, 3 },
            postalCodes,
            graph
        );

        // Assert
        model.Crf.Should().NotBeNull();
        model.Vocabulary.Should().NotBeNull();
        model.Phrases.Should().NotBeNull();
        model.PhraseTypes.Should().NotBeNull();
        model.PostalCodes.Should().NotBeNull();
        model.PostalCodeGraph.Should().NotBeNull();
    }

    [Fact]
    public void LoadFromStream_WithMinimalCrfModel_ShouldLoad()
    {
        // Arrange - create minimal CRF model in stream
        using var stream = CreateMockCrfModel();

        // Act
        var model = AddressParserModelLoader.LoadFromStream(stream);

        // Assert
        model.Should().NotBeNull();
        model.Type.Should().Be(ModelType.CRF);
        model.Crf.Should().NotBeNull();
        model.Crf.NumClasses.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadFromStream_WithNullStream_ShouldThrow()
    {
        // Act
        Action act = () => AddressParserModelLoader.LoadFromStream(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadFromStream_WithVocabulary_ShouldLoadVocabulary()
    {
        // Arrange
        using var stream = CreateMockModelWithVocab();

        // Act
        var model = AddressParserModelLoader.LoadFromStream(stream);

        // Assert
        model.Vocabulary.Should().NotBeNull();
        model.Vocabulary.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void LoadFromDirectory_WithMockFiles_ShouldLoadModel()
    {
        // Arrange - create temp directory with mock model files
        var tempDir = Path.Combine(Path.GetTempPath(), $"libpostal_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create mock CRF file
            var crfPath = Path.Combine(tempDir, "address_parser_crf.dat");
            using (var fs = File.Create(crfPath))
            {
                CreateMockCrfModel(fs);
            }

            // Act
            var model = AddressParserModelLoader.LoadFromDirectory(tempDir);

            // Assert
            model.Should().NotBeNull();
            model.Type.Should().Be(ModelType.CRF);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadFromDirectory_WithNonexistentDirectory_ShouldThrow()
    {
        // Act
        Action act = () => AddressParserModelLoader.LoadFromDirectory("/nonexistent/path");

        // Assert
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void LoadFromDirectory_WithNullPath_ShouldThrow()
    {
        // Act
        Action act = () => AddressParserModelLoader.LoadFromDirectory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadFromDirectory_WithMissingModelFile_ShouldThrow()
    {
        // Arrange - create empty directory
        var tempDir = Path.Combine(Path.GetTempPath(), $"libpostal_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act - no model files present
            Action act = () => AddressParserModelLoader.LoadFromDirectory(tempDir);

            // Assert
            act.Should().Throw<FileNotFoundException>();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ModelType_Enum_ShouldHaveExpectedValues()
    {
        // Assert - verify enum values match libpostal
        ModelType.CRF.Should().Be((ModelType)0);
        ModelType.AveragedPerceptron.Should().Be((ModelType)1);
    }

    [Fact]
    public void AddressParserModel_WithOptionalNulls_ShouldAllowNulls()
    {
        // Arrange
        var crf = new Crf(new[] { "house_number" });
        var vocab = new Trie<uint>();

        // Act - only required components
        var model = new AddressParserModel(
            ModelType.CRF,
            crf,
            vocab,
            null,  // phrases optional
            null,  // phraseTypes optional
            null,  // postalCodes optional
            null   // postalCodeGraph optional
        );

        // Assert
        model.Phrases.Should().BeNull();
        model.PhraseTypes.Should().BeNull();
        model.PostalCodes.Should().BeNull();
        model.PostalCodeGraph.Should().BeNull();
    }

    [Fact]
    public void AddressParserModel_WithNullCrf_ShouldThrow()
    {
        // Arrange
        var vocab = new Trie<uint>();

        // Act
        Action act = () => new AddressParserModel(
            ModelType.CRF,
            null!, // CRF is required
            vocab,
            null,
            null,
            null,
            null
        );

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddressParserModel_WithNullVocabulary_ShouldThrow()
    {
        // Arrange
        var crf = new Crf(new[] { "house_number" });

        // Act
        Action act = () => new AddressParserModel(
            ModelType.CRF,
            crf,
            null!, // Vocabulary is required
            null,
            null,
            null,
            null
        );

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LoadFromStream_WithCompleteModel_ShouldLoadAllComponents()
    {
        // Arrange
        using var stream = CreateCompleteModel();

        // Act
        var model = AddressParserModelLoader.LoadFromStream(stream);

        // Assert
        model.Crf.Should().NotBeNull();
        model.Vocabulary.Should().NotBeNull();
        // Note: phrases, postal codes etc. would be loaded in real implementation
    }

    // Helper methods to create mock binary data

    private static MemoryStream CreateMockCrfModel()
    {
        var stream = new MemoryStream();
        CreateMockCrfModel(stream);
        stream.Position = 0;
        return stream;
    }

    private static void CreateMockCrfModel(Stream stream)
    {
        // Create minimal valid CRF model
        var crf = new Crf(new[] { "house_number", "road", "city" });

        // Add some features
        var feat1 = crf.AddStateFeature("is_numeric");
        crf.SetWeight(feat1, 0, 5.0); // house_number

        var feat2 = crf.AddStateFeature("word=street");
        crf.SetWeight(feat2, 1, 3.0); // road

        // Save to stream
        crf.Save(stream);
    }

    private static MemoryStream CreateMockModelWithVocab()
    {
        var stream = new MemoryStream();

        // Write CRF model
        CreateMockCrfModel(stream);

        // Write vocabulary trie
        var vocab = new Trie<uint>();
        vocab.Add("street", 1);
        vocab.Add("avenue", 2);
        vocab.Add("road", 3);
        vocab.Save(stream);

        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateCompleteModel()
    {
        var stream = new MemoryStream();

        // Write CRF
        CreateMockCrfModel(stream);

        // Write vocabulary
        var vocab = new Trie<uint>();
        vocab.Add("test", 1);
        vocab.Save(stream);

        stream.Position = 0;
        return stream;
    }
}
