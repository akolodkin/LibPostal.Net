using FluentAssertions;
using LibPostal.Net.Expansion;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for GazetteerClassifier.
/// Based on libpostal's gazetteer classification functions (expand.c lines 471-662).
/// </summary>
public class GazetteerClassifierTests
{
    #region IsIgnorableForComponents Tests

    [Fact]
    public void IsIgnorableForComponents_StreetType_ForStreet_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsIgnorableForComponents(
            DictionaryType.StreetType,
            AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIgnorableForComponents_StreetType_ForHouseNumber_ShouldReturnFalse()
    {
        // Act
        var result = GazetteerClassifier.IsIgnorableForComponents(
            DictionaryType.StreetType,
            AddressComponent.HouseNumber);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIgnorableForComponents_Directional_ForStreet_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsIgnorableForComponents(
            DictionaryType.Directional,
            AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIgnorableForComponents_UnitType_ForUnit_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsIgnorableForComponents(
            DictionaryType.UnitType,
            AddressComponent.Unit);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIgnorableForComponents_LevelType_ForLevel_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsIgnorableForComponents(
            DictionaryType.LevelType,
            AddressComponent.Level);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsEdgeIgnorableForComponents Tests

    [Fact]
    public void IsEdgeIgnorableForComponents_Directional_ForStreet_ShouldReturnTrue()
    {
        // Arrange - directionals like "N", "S" can be ignored at edges for streets
        // Act
        var result = GazetteerClassifier.IsEdgeIgnorableForComponents(
            DictionaryType.Directional,
            AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEdgeIgnorableForComponents_StreetType_ForStreet_ShouldReturnFalse()
    {
        // Arrange - street types like "St", "Ave" are not edge-ignorable
        // Act
        var result = GazetteerClassifier.IsEdgeIgnorableForComponents(
            DictionaryType.StreetType,
            AddressComponent.Street);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEdgeIgnorableForComponents_CompanyType_ForName_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsEdgeIgnorableForComponents(
            DictionaryType.CompanyType,
            AddressComponent.Name);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEdgeIgnorableForComponents_PlaceName_ForName_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsEdgeIgnorableForComponents(
            DictionaryType.PlaceName,
            AddressComponent.Name);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsPossibleRootForComponents Tests

    [Fact]
    public void IsPossibleRootForComponents_Directional_ForStreet_ShouldReturnTrue()
    {
        // Arrange - "E" in "E St" could be the root
        // Act
        var result = GazetteerClassifier.IsPossibleRootForComponents(
            DictionaryType.Directional,
            AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPossibleRootForComponents_StreetType_ForStreet_ShouldReturnFalse()
    {
        // Arrange - "St" in "Main St" is not the root
        // Act
        var result = GazetteerClassifier.IsPossibleRootForComponents(
            DictionaryType.StreetType,
            AddressComponent.Street);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPossibleRootForComponents_PlaceName_ForStreet_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsPossibleRootForComponents(
            DictionaryType.PlaceName,
            AddressComponent.Street);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPossibleRootForComponents_Number_ForName_ShouldReturnTrue()
    {
        // Act
        var result = GazetteerClassifier.IsPossibleRootForComponents(
            DictionaryType.Number,
            AddressComponent.Name);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsSpecifierForComponents Tests

    [Fact]
    public void IsSpecifierForComponents_LevelType_ForLevel_ShouldReturnTrue()
    {
        // Arrange - "Basement", "Penthouse" are specifiers
        // Act
        var result = GazetteerClassifier.IsSpecifierForComponents(
            DictionaryType.LevelType,
            AddressComponent.Level);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSpecifierForComponents_UnitType_ForUnit_ShouldReturnTrue()
    {
        // Arrange - "Penthouse", "Left" are unit specifiers
        // Act
        var result = GazetteerClassifier.IsSpecifierForComponents(
            DictionaryType.UnitType,
            AddressComponent.Unit);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSpecifierForComponents_StreetType_ForStreet_ShouldReturnFalse()
    {
        // Act
        var result = GazetteerClassifier.IsSpecifierForComponents(
            DictionaryType.StreetType,
            AddressComponent.Street);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetValidComponents Tests

    [Fact]
    public void GetValidComponents_StreetType_ShouldReturnNameAndStreet()
    {
        // Act
        var result = GazetteerClassifier.GetValidComponents(DictionaryType.StreetType);

        // Assert
        result.HasFlag(AddressComponent.Name).Should().BeTrue();
        result.HasFlag(AddressComponent.Street).Should().BeTrue();
        result.HasFlag(AddressComponent.Unit).Should().BeFalse();
    }

    [Fact]
    public void GetValidComponents_Directional_ShouldReturnMultipleComponents()
    {
        // Act
        var result = GazetteerClassifier.GetValidComponents(DictionaryType.Directional);

        // Assert
        result.HasFlag(AddressComponent.Street).Should().BeTrue();
        result.HasFlag(AddressComponent.Name).Should().BeTrue();
        result.HasFlag(AddressComponent.Unit).Should().BeTrue();
        result.HasFlag(AddressComponent.Level).Should().BeTrue();
    }

    [Fact]
    public void GetValidComponents_UnitType_ShouldReturnUnit()
    {
        // Act
        var result = GazetteerClassifier.GetValidComponents(DictionaryType.UnitType);

        // Assert
        result.HasFlag(AddressComponent.Unit).Should().BeTrue();
        result.HasFlag(AddressComponent.Street).Should().BeFalse();
    }

    [Fact]
    public void GetValidComponents_LevelType_ShouldReturnLevel()
    {
        // Act
        var result = GazetteerClassifier.GetValidComponents(DictionaryType.LevelType);

        // Assert
        result.HasFlag(AddressComponent.Level).Should().BeTrue();
        result.HasFlag(AddressComponent.Street).Should().BeFalse();
    }

    [Fact]
    public void GetValidComponents_Stopword_ShouldReturnMultiple()
    {
        // Act
        var result = GazetteerClassifier.GetValidComponents(DictionaryType.Stopword);

        // Assert
        result.HasFlag(AddressComponent.Street).Should().BeTrue();
        result.HasFlag(AddressComponent.Name).Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ClassificationFunctions_ShouldBeConsistent()
    {
        // Arrange - if something is edge-ignorable, it should also be ignorable
        var types = new[]
        {
            DictionaryType.Directional,
            DictionaryType.CompanyType,
            DictionaryType.PlaceName
        };

        foreach (var type in types)
        {
            foreach (AddressComponent component in Enum.GetValues(typeof(AddressComponent)))
            {
                if (component == AddressComponent.None || component == AddressComponent.Any || component == AddressComponent.All)
                    continue;

                // Act
                var edgeIgnorable = GazetteerClassifier.IsEdgeIgnorableForComponents(type, component);
                var ignorable = GazetteerClassifier.IsIgnorableForComponents(type, component);

                // Assert - if edge-ignorable, should also be ignorable
                if (edgeIgnorable)
                {
                    ignorable.Should().BeTrue($"{type} is edge-ignorable for {component}, so it should also be ignorable");
                }
            }
        }
    }

    #endregion
}
