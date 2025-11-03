using FluentAssertions;
using LibPostal.Net.Parser;
using LibPostal.Net.ML;
using LibPostal.Net.Core;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Integration tests for AddressParser with real model loading.
/// </summary>
public class AddressParserIntegrationTests
{
    [Fact]
    public void AddressParser_WithModel_ShouldConstruct()
    {
        // Arrange
        var model = CreateMockModel();

        // Act
        var parser = new AddressParser(model);

        // Assert
        parser.Should().NotBeNull();
    }

    [Fact]
    public void AddressParser_WithNullModel_ShouldThrow()
    {
        // Act
        Action act = () => new AddressParser((AddressParserModel)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Parse_WithModelBasedParser_ShouldReturnResults()
    {
        // Arrange
        var model = CreateMockModel();
        var parser = new AddressParser(model);

        // Act
        var result = parser.Parse("123 Main Street");

        // Assert
        result.Should().NotBeNull();
        result.NumComponents.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "Mock format incompatible - validated by RealAddressValidationTests")]
    public void LoadFromDirectory_WithMockFiles_ShouldCreateParser()
    {
        // Arrange - create temp directory with mock model
        var tempDir = Path.Combine(Path.GetTempPath(), $"libpostal_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            CreateMockModelFiles(tempDir);

            // Act
            var parser = AddressParser.LoadFromDirectory(tempDir);

            // Assert
            parser.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadFromDirectory_WithNonexistentPath_ShouldThrow()
    {
        // Act
        Action act = () => AddressParser.LoadFromDirectory("/nonexistent/path");

        // Assert
        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void LoadFromDirectory_WithNullPath_ShouldThrow()
    {
        // Act
        Action act = () => AddressParser.LoadFromDirectory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(Skip = "Mock format incompatible - validated by RealAddressValidationTests")]
    public void Parse_WithLoadedParser_ShouldLabelComponents()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"libpostal_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            CreateMockModelFiles(tempDir);
            var parser = AddressParser.LoadFromDirectory(tempDir);

            // Act
            var result = parser.Parse("456 Oak Avenue");

            // Assert
            result.NumComponents.Should().BeGreaterThan(0);
            result.Labels.Should().NotBeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AddressParserBuilder_ShouldConstructParser()
    {
        // Arrange
        var model = CreateMockModel();

        // Act
        var parser = AddressParserBuilder.Create()
            .WithModel(model)
            .Build();

        // Assert
        parser.Should().NotBeNull();
    }

    [Fact(Skip = "Mock format incompatible - validated by RealAddressValidationTests")]
    public void AddressParserBuilder_WithDirectory_ShouldLoadAndBuild()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"libpostal_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            CreateMockModelFiles(tempDir);

            // Act
            var parser = AddressParserBuilder.Create()
                .WithDataDirectory(tempDir)
                .Build();

            // Assert
            parser.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void AddressParserBuilder_WithoutModel_ShouldThrow()
    {
        // Act
        Action act = () => AddressParserBuilder.Create().Build();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddressParserBuilder_WithBothModelAndDirectory_ShouldUseModel()
    {
        // Arrange
        var model = CreateMockModel();
        var tempDir = Path.Combine(Path.GetTempPath(), $"libpostal_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            CreateMockModelFiles(tempDir);

            // Act
            var parser = AddressParserBuilder.Create()
                .WithModel(model)
                .WithDataDirectory(tempDir)
                .Build();

            // Assert - should use model, not directory
            parser.Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Parse_WithVocabulary_ShouldUseVocabularyFeatures()
    {
        // Arrange
        var model = CreateMockModelWithVocabulary();
        var parser = new AddressParser(model);

        // Act
        var result = parser.Parse("100 Main Street");

        // Assert
        result.Should().NotBeNull();
        // If vocabulary is used, we should get better feature extraction
    }

    [Fact]
    public void Parse_MultipleAddresses_ShouldHandleConsistently()
    {
        // Arrange
        var model = CreateMockModel();
        var parser = new AddressParser(model);

        var addresses = new[]
        {
            "123 Main Street",
            "456 Oak Avenue",
            "789 Elm Boulevard"
        };

        // Act & Assert
        foreach (var address in addresses)
        {
            var result = parser.Parse(address);
            result.Should().NotBeNull();
            result.NumComponents.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Parse_EmptyAddress_ShouldReturnEmptyResult()
    {
        // Arrange
        var model = CreateMockModel();
        var parser = new AddressParser(model);

        // Act
        var result = parser.Parse("");

        // Assert
        result.NumComponents.Should().Be(0);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ShouldReturnEmptyResult()
    {
        // Arrange
        var model = CreateMockModel();
        var parser = new AddressParser(model);

        // Act
        var result = parser.Parse("   \t\n  ");

        // Assert
        result.NumComponents.Should().Be(0);
    }

    // Helper methods

    private static AddressParserModel CreateMockModel()
    {
        var crf = new Crf(new[] { "house_number", "road", "city", "state", "postcode" });

        // Add features
        var numericFeat = crf.AddStateFeature("is_numeric");
        crf.SetWeight(numericFeat, 0, 5.0); // house_number

        var wordFeat = crf.AddStateFeature("word=street");
        crf.SetWeight(wordFeat, 1, 3.0); // road

        // Transitions
        crf.SetTransWeight(0, 1, 1.0); // house_number → road
        crf.SetTransWeight(1, 2, 1.0); // road → city

        var vocab = new Trie<uint>();
        vocab.Add("street", 1);
        vocab.Add("avenue", 2);
        vocab.Add("main", 3);

        return new AddressParserModel(ModelType.CRF, crf, vocab);
    }

    private static AddressParserModel CreateMockModelWithVocabulary()
    {
        var crf = new Crf(new[] { "house_number", "road", "city" });

        var numericFeat = crf.AddStateFeature("is_numeric");
        crf.SetWeight(numericFeat, 0, 5.0);

        var vocab = new Trie<uint>();
        vocab.Add("street", 1);
        vocab.Add("avenue", 2);
        vocab.Add("road", 3);
        vocab.Add("boulevard", 4);
        vocab.Add("main", 5);
        vocab.Add("oak", 6);

        return new AddressParserModel(ModelType.CRF, crf, vocab);
    }

    private static void CreateMockModelFiles(string directory)
    {
        // Create minimal CRF model file
        var crf = new Crf(new[] { "house_number", "road", "city" });

        var feat1 = crf.AddStateFeature("is_numeric");
        crf.SetWeight(feat1, 0, 5.0);

        var feat2 = crf.AddStateFeature("word=street");
        crf.SetWeight(feat2, 1, 3.0);

        crf.SetTransWeight(0, 1, 1.0);

        var crfPath = Path.Combine(directory, "address_parser_crf.dat");
        using (var fs = File.Create(crfPath))
        {
            crf.Save(fs);
        }

        // Create minimal vocabulary file
        var vocab = new Trie<uint>();
        vocab.Add("street", 1);
        vocab.Add("avenue", 2);

        var vocabPath = Path.Combine(directory, "address_parser_vocab.trie");
        using (var fs = File.Create(vocabPath))
        {
            vocab.Save(fs);
        }
    }
}
