using FluentAssertions;
using LibPostal.Net.Parser;
using System.IO;
using Xunit;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for loading real libpostal models from disk.
/// These tests require real model files to be downloaded.
/// </summary>
public class RealModelLoadingTests
{
    private readonly string _dataDirectory;

    public RealModelLoadingTests()
    {
        // Check for models in standard location
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _dataDirectory = Path.Combine(userProfile, ".libpostal");
    }

    [Fact]
    public void LoadDefault_WithRealModels_ShouldSucceed()
    {
        // Arrange
        var parserDir = Path.Combine(_dataDirectory, "address_parser");

        // Skip if models not downloaded
        if (!Directory.Exists(parserDir))
        {
            // This is expected if models haven't been downloaded yet
            return;
        }

        // Act
        var parser = AddressParser.LoadDefault();

        // Assert
        parser.Should().NotBeNull();
    }

    [Fact(Skip = "Requires real models to be downloaded - run manually")]
    public void LoadFromDirectory_WithRealModels_ShouldLoadCrfModel()
    {
        // Arrange
        var parserDir = Path.Combine(_dataDirectory, "address_parser");

        if (!Directory.Exists(parserDir))
        {
            return;
        }

        // Act
        var parser = AddressParser.LoadFromDirectory(_dataDirectory);

        // Assert
        parser.Should().NotBeNull();

        // Verify can parse a simple address
        var result = parser.Parse("123 Main Street");
        result.Should().NotBeNull();
        result.Labels.Should().NotBeEmpty();
    }

    [Fact(Skip = "Requires real models to be downloaded - run manually")]
    public void RealModel_ShouldHaveExpectedLabels()
    {
        // Arrange
        var parserDir = Path.Combine(_dataDirectory, "address_parser");

        if (!Directory.Exists(parserDir))
        {
            return;
        }

        var parser = AddressParser.LoadFromDirectory(_dataDirectory);

        // Act
        var result = parser.Parse("123 Main Street Brooklyn NY 11216");

        // Assert - Real model should have standard address component labels
        var labels = result.Labels;
        labels.Should().Contain(l => l == "house_number" || l == "road" || l == "city" ||
                                      l == "state" || l == "postcode");
    }

    [Fact(Skip = "Requires real models to be downloaded - run manually")]
    public void RealModel_VocabularyShouldBeLoaded()
    {
        // Arrange
        var parserDir = Path.Combine(_dataDirectory, "address_parser");

        if (!Directory.Exists(parserDir))
        {
            return;
        }

        // Act - Load the model directly to check vocabulary
        var model = AddressParserModelLoader.LoadFromDirectory(_dataDirectory);

        // Assert
        model.Should().NotBeNull();
        model.Vocabulary.Should().NotBeNull();
        model.Vocabulary.Count.Should().BeGreaterThan(0);

        // Common words should be in vocabulary
        model.Vocabulary.ContainsKey("street").Should().BeTrue();
        model.Vocabulary.ContainsKey("avenue").Should().BeTrue();
    }

    [Fact(Skip = "Requires real models to be downloaded - run manually")]
    public void RealModel_PhrasesShouldBeLoaded()
    {
        // Arrange
        var parserDir = Path.Combine(_dataDirectory, "address_parser");

        if (!Directory.Exists(parserDir))
        {
            return;
        }

        // Act
        var model = AddressParserModelLoader.LoadFromDirectory(_dataDirectory);

        // Assert - Phrases should be loaded if file exists
        if (File.Exists(Path.Combine(_dataDirectory, "address_parser", "address_parser_phrases.dat")))
        {
            model.Phrases.Should().NotBeNull();
            model.PhraseTypes.Should().NotBeNull();
            model.Phrases!.Count.Should().BeGreaterThan(0);
        }
    }
}
