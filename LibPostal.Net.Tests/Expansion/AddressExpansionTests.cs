using FluentAssertions;
using LibPostal.Net.Expansion;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for Address Expansion data structures.
/// Based on libpostal's address_expansion structures.
/// </summary>
public class AddressExpansionTests
{
    [Fact]
    public void AddressComponent_ShouldHaveAnyFlag()
    {
        // Arrange & Act
        var component = AddressComponent.Any;

        // Assert
        component.Should().BeDefined();
    }

    [Fact]
    public void AddressComponent_ShouldHaveStreetFlag()
    {
        // Arrange & Act
        var component = AddressComponent.Street;

        // Assert
        component.Should().BeDefined();
    }

    [Fact]
    public void AddressComponent_ShouldHaveHouseNumberFlag()
    {
        // Arrange & Act
        var component = AddressComponent.HouseNumber;

        // Assert
        component.Should().BeDefined();
    }

    [Fact]
    public void AddressComponent_ShouldHaveUnitFlag()
    {
        // Arrange & Act
        var component = AddressComponent.Unit;

        // Assert
        component.Should().BeDefined();
    }

    [Fact]
    public void AddressComponent_ShouldHaveLevelFlag()
    {
        // Arrange & Act
        var component = AddressComponent.Level;

        // Assert
        component.Should().BeDefined();
    }

    [Fact]
    public void AddressComponent_ShouldSupportFlagCombinations()
    {
        // Arrange & Act
        var combined = AddressComponent.Street | AddressComponent.HouseNumber;

        // Assert
        combined.HasFlag(AddressComponent.Street).Should().BeTrue();
        combined.HasFlag(AddressComponent.HouseNumber).Should().BeTrue();
        combined.HasFlag(AddressComponent.Unit).Should().BeFalse();
    }

    [Fact]
    public void AddressExpansion_ShouldInitializeWithProperties()
    {
        // Arrange & Act
        var expansion = new AddressExpansion
        {
            Canonical = "street",
            Language = "en",
            Components = AddressComponent.Street,
            DictionaryType = DictionaryType.StreetType,
            IsSeparable = true
        };

        // Assert
        expansion.Canonical.Should().Be("street");
        expansion.Language.Should().Be("en");
        expansion.Components.Should().Be(AddressComponent.Street);
        expansion.DictionaryType.Should().Be(DictionaryType.StreetType);
        expansion.IsSeparable.Should().BeTrue();
    }

    [Fact]
    public void AddressExpansion_WithNoCanonical_ShouldAllowNull()
    {
        // Arrange & Act
        var expansion = new AddressExpansion
        {
            Canonical = null,
            Language = "en",
            Components = AddressComponent.Street,
            DictionaryType = DictionaryType.StreetType,
            IsSeparable = false
        };

        // Assert
        expansion.Canonical.Should().BeNull();
    }

    [Fact]
    public void AddressExpansionValue_ShouldStoreMultipleExpansions()
    {
        // Arrange
        var expansions = new List<AddressExpansion>
        {
            new AddressExpansion
            {
                Canonical = "street",
                Language = "en",
                Components = AddressComponent.Street,
                DictionaryType = DictionaryType.StreetType,
                IsSeparable = true
            },
            new AddressExpansion
            {
                Canonical = "st",
                Language = "en",
                Components = AddressComponent.Street,
                DictionaryType = DictionaryType.StreetType,
                IsSeparable = true
            }
        };

        // Act
        var value = new AddressExpansionValue(expansions);

        // Assert
        value.Expansions.Should().HaveCount(2);
        value.Expansions[0].Canonical.Should().Be("street");
        value.Expansions[1].Canonical.Should().Be("st");
    }

    [Fact]
    public void AddressExpansionValue_WithNullExpansions_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AddressExpansionValue(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DictionaryType_ShouldHaveCommonTypes()
    {
        // Assert - verify key dictionary types exist
        DictionaryType.StreetType.Should().BeDefined();
        DictionaryType.Directional.Should().BeDefined();
        DictionaryType.BuildingType.Should().BeDefined();
        DictionaryType.UnitType.Should().BeDefined();
        DictionaryType.LevelType.Should().BeDefined();
        DictionaryType.PostOffice.Should().BeDefined();
        DictionaryType.Qualifier.Should().BeDefined();
        DictionaryType.Synonym.Should().BeDefined();
    }

    [Fact]
    public void AddressExpansion_ShouldSupportRecordEquality()
    {
        // Arrange
        var expansion1 = new AddressExpansion
        {
            Canonical = "street",
            Language = "en",
            Components = AddressComponent.Street,
            DictionaryType = DictionaryType.StreetType,
            IsSeparable = true
        };

        var expansion2 = new AddressExpansion
        {
            Canonical = "street",
            Language = "en",
            Components = AddressComponent.Street,
            DictionaryType = DictionaryType.StreetType,
            IsSeparable = true
        };

        // Act & Assert
        expansion1.Should().Be(expansion2);
    }
}
