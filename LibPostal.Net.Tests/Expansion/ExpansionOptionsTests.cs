using FluentAssertions;
using LibPostal.Net.Expansion;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for ExpansionOptions class.
/// Based on libpostal's libpostal_normalize_options_t structure.
/// </summary>
public class ExpansionOptionsTests
{
    [Fact]
    public void GetDefault_ShouldReturnDefaultOptions()
    {
        // Act
        var options = ExpansionOptions.GetDefault();

        // Assert
        options.Should().NotBeNull();
        options.Languages.Should().BeEmpty(); // Auto-detect
        options.AddressComponents.Should().Be(AddressComponent.All);
    }

    [Fact]
    public void Constructor_WithLanguages_ShouldStoreLanguages()
    {
        // Arrange
        var languages = new[] { "en", "fr", "de" };

        // Act
        var options = new ExpansionOptions
        {
            Languages = languages
        };

        // Assert
        options.Languages.Should().BeEquivalentTo(languages);
    }

    [Fact]
    public void Constructor_WithAddressComponents_ShouldStoreComponents()
    {
        // Arrange
        var components = AddressComponent.Street | AddressComponent.HouseNumber;

        // Act
        var options = new ExpansionOptions
        {
            AddressComponents = components
        };

        // Assert
        options.AddressComponents.Should().Be(components);
    }

    [Fact]
    public void LatinAscii_DefaultShouldBeFalse()
    {
        // Act
        var options = ExpansionOptions.GetDefault();

        // Assert
        options.LatinAscii.Should().BeFalse();
    }

    [Fact]
    public void StripAccents_DefaultShouldBeFalse()
    {
        // Act
        var options = ExpansionOptions.GetDefault();

        // Assert
        options.StripAccents.Should().BeFalse();
    }

    [Fact]
    public void Lowercase_DefaultShouldBeTrue()
    {
        // Act
        var options = ExpansionOptions.GetDefault();

        // Assert
        options.Lowercase.Should().BeTrue();
    }

    [Fact]
    public void TrimString_DefaultShouldBeTrue()
    {
        // Act
        var options = ExpansionOptions.GetDefault();

        // Assert
        options.TrimString.Should().BeTrue();
    }

    [Fact]
    public void DeleteFinalPeriods_DefaultShouldBeTrue()
    {
        // Act
        var options = ExpansionOptions.GetDefault();

        // Assert
        options.DeleteFinalPeriods.Should().BeTrue();
    }

    [Fact]
    public void Options_ShouldSupportAllProperties()
    {
        // Arrange & Act
        var options = new ExpansionOptions
        {
            Languages = new[] { "en" },
            AddressComponents = AddressComponent.Street,
            LatinAscii = true,
            Transliterate = true,
            StripAccents = true,
            Decompose = true,
            Lowercase = true,
            TrimString = true,
            DropParentheticals = true,
            ReplaceNumericHyphens = true,
            DeleteNumericHyphens = false,
            SplitAlphaFromNumeric = true,
            ReplaceWordHyphens = false,
            DeleteWordHyphens = true,
            DeleteFinalPeriods = true,
            DeleteAcronymPeriods = true,
            DropEnglishPossessives = true,
            DeleteApostrophes = true,
            ExpandNumex = false,
            RomanNumerals = false
        };

        // Assert
        options.Languages.Should().ContainSingle("en");
        options.AddressComponents.Should().Be(AddressComponent.Street);
        options.LatinAscii.Should().BeTrue();
        options.Transliterate.Should().BeTrue();
        options.StripAccents.Should().BeTrue();
        options.Decompose.Should().BeTrue();
        options.Lowercase.Should().BeTrue();
        options.TrimString.Should().BeTrue();
        options.DropParentheticals.Should().BeTrue();
        options.ReplaceNumericHyphens.Should().BeTrue();
        options.DeleteNumericHyphens.Should().BeFalse();
        options.SplitAlphaFromNumeric.Should().BeTrue();
        options.ReplaceWordHyphens.Should().BeFalse();
        options.DeleteWordHyphens.Should().BeTrue();
        options.DeleteFinalPeriods.Should().BeTrue();
        options.DeleteAcronymPeriods.Should().BeTrue();
        options.DropEnglishPossessives.Should().BeTrue();
        options.DeleteApostrophes.Should().BeTrue();
        options.ExpandNumex.Should().BeFalse();
        options.RomanNumerals.Should().BeFalse();
    }

    [Fact]
    public void GetDefault_ShouldHaveConsistentDefaults()
    {
        // Act
        var options1 = ExpansionOptions.GetDefault();
        var options2 = ExpansionOptions.GetDefault();

        // Assert
        options1.Lowercase.Should().Be(options2.Lowercase);
        options1.TrimString.Should().Be(options2.TrimString);
        options1.DeleteFinalPeriods.Should().Be(options2.DeleteFinalPeriods);
    }
}
