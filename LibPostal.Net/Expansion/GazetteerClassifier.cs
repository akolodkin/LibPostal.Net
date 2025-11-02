namespace LibPostal.Net.Expansion;

/// <summary>
/// Classifies dictionary types for address component filtering.
/// Based on libpostal's gazetteer classification functions (expand.c lines 471-662).
/// </summary>
public static class GazetteerClassifier
{
    /// <summary>
    /// Determines if a dictionary type is ignorable for the specified address components.
    /// </summary>
    /// <param name="dictionaryType">The dictionary type.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if the dictionary type can be ignored for these components.</returns>
    public static bool IsIgnorableForComponents(DictionaryType dictionaryType, AddressComponent components)
    {
        return dictionaryType switch
        {
            // Street types can be ignored for NAME or STREET
            DictionaryType.StreetType =>
                (components & (AddressComponent.Name | AddressComponent.Street)) != 0,

            // Directionals can be ignored for STREET
            DictionaryType.Directional =>
                (components & AddressComponent.Street) != 0,

            // Building types can be ignored for NAME or STREET
            DictionaryType.BuildingType =>
                (components & (AddressComponent.Name | AddressComponent.Street)) != 0,

            // Unit types can be ignored for UNIT
            DictionaryType.UnitType =>
                (components & AddressComponent.Unit) != 0,

            // Level types can be ignored for LEVEL
            DictionaryType.LevelType =>
                (components & AddressComponent.Level) != 0,

            // House number markers can be ignored for HOUSE_NUMBER
            DictionaryType.HouseNumber =>
                (components & AddressComponent.HouseNumber) != 0,

            // Post office can be ignored for PO_BOX
            DictionaryType.PostOffice =>
                (components & AddressComponent.PoBox) != 0,

            // Qualifiers, synonyms, stopwords can be ignored for NAME or STREET
            DictionaryType.Qualifier or DictionaryType.Synonym or DictionaryType.Stopword =>
                (components & (AddressComponent.Name | AddressComponent.Street)) != 0,

            // Company types can be ignored for NAME
            DictionaryType.CompanyType =>
                (components & AddressComponent.Name) != 0,

            // Place names can be ignored for NAME or TOPONYM
            DictionaryType.PlaceName =>
                (components & (AddressComponent.Name | AddressComponent.Toponym)) != 0,

            _ => false
        };
    }

    /// <summary>
    /// Determines if a dictionary type is edge-ignorable (can be ignored at beginning/end).
    /// </summary>
    /// <param name="dictionaryType">The dictionary type.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if the dictionary type can be ignored at edges.</returns>
    public static bool IsEdgeIgnorableForComponents(DictionaryType dictionaryType, AddressComponent components)
    {
        return dictionaryType switch
        {
            // Directionals are edge-ignorable for STREET (e.g., "E" in "E Broadway" or "Park S")
            DictionaryType.Directional =>
                (components & AddressComponent.Street) != 0,

            // Company types are edge-ignorable for NAME
            DictionaryType.CompanyType =>
                (components & AddressComponent.Name) != 0,

            // Place names are edge-ignorable for NAME
            DictionaryType.PlaceName =>
                (components & AddressComponent.Name) != 0,

            _ => false
        };
    }

    /// <summary>
    /// Determines if a dictionary type could be part of the root for the specified components.
    /// </summary>
    /// <param name="dictionaryType">The dictionary type.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if this type could be the root/core part of the address.</returns>
    public static bool IsPossibleRootForComponents(DictionaryType dictionaryType, AddressComponent components)
    {
        return dictionaryType switch
        {
            // Directionals can be roots for STREET (e.g., "E" in "Avenue E")
            DictionaryType.Directional =>
                (components & AddressComponent.Street) != 0,

            // Place names can be roots for NAME or STREET
            DictionaryType.PlaceName =>
                (components & (AddressComponent.Name | AddressComponent.Street)) != 0,

            // Numbers can be roots for NAME or STREET
            DictionaryType.Number =>
                (components & (AddressComponent.Name | AddressComponent.Street)) != 0,

            // Street names are always possible roots for STREET
            DictionaryType.StreetName =>
                (components & AddressComponent.Street) != 0,

            // Personal titles can be roots for NAME
            DictionaryType.PersonalTitle =>
                (components & AddressComponent.Name) != 0,

            // Qualifiers can be roots for NAME or STREET
            DictionaryType.Qualifier =>
                (components & (AddressComponent.Name | AddressComponent.Street)) != 0,

            _ => false
        };
    }

    /// <summary>
    /// Determines if a dictionary type is a specifier (standalone descriptor).
    /// </summary>
    /// <param name="dictionaryType">The dictionary type.</param>
    /// <param name="components">The address components.</param>
    /// <returns>True if this is a specifier for these components.</returns>
    public static bool IsSpecifierForComponents(DictionaryType dictionaryType, AddressComponent components)
    {
        return dictionaryType switch
        {
            // Level types (Basement, Penthouse, etc.) are specifiers for LEVEL
            DictionaryType.LevelType =>
                (components & AddressComponent.Level) != 0,

            // Unit types (Left, Right, Penthouse) can be specifiers for UNIT
            DictionaryType.UnitType =>
                (components & AddressComponent.Unit) != 0,

            _ => false
        };
    }

    /// <summary>
    /// Gets the valid address components for a dictionary type.
    /// </summary>
    /// <param name="dictionaryType">The dictionary type.</param>
    /// <returns>The address components this dictionary type is valid for.</returns>
    public static AddressComponent GetValidComponents(DictionaryType dictionaryType)
    {
        return dictionaryType switch
        {
            DictionaryType.StreetType =>
                AddressComponent.Name | AddressComponent.Street,

            DictionaryType.Directional =>
                AddressComponent.Name | AddressComponent.Street | AddressComponent.Category |
                AddressComponent.Near | AddressComponent.Toponym | AddressComponent.Unit |
                AddressComponent.Level | AddressComponent.Staircase | AddressComponent.Entrance,

            DictionaryType.BuildingType =>
                AddressComponent.Name | AddressComponent.Street,

            DictionaryType.UnitType =>
                AddressComponent.Unit,

            DictionaryType.LevelType =>
                AddressComponent.Level,

            DictionaryType.HouseNumber =>
                AddressComponent.HouseNumber,

            DictionaryType.PostOffice =>
                AddressComponent.PoBox,

            DictionaryType.PostalCode =>
                AddressComponent.PostalCode,

            DictionaryType.CompanyType =>
                AddressComponent.Name,

            DictionaryType.PlaceName =>
                AddressComponent.Name | AddressComponent.Street | AddressComponent.Toponym,

            DictionaryType.PersonalTitle =>
                AddressComponent.Name,

            DictionaryType.AcademicDegree =>
                AddressComponent.Name,

            DictionaryType.Qualifier =>
                AddressComponent.Name | AddressComponent.Street | AddressComponent.Category |
                AddressComponent.Near | AddressComponent.Toponym,

            DictionaryType.Synonym =>
                AddressComponent.All,

            DictionaryType.Stopword =>
                AddressComponent.Name | AddressComponent.Street | AddressComponent.Category |
                AddressComponent.Near | AddressComponent.Toponym,

            DictionaryType.Elision =>
                AddressComponent.Name | AddressComponent.Street,

            DictionaryType.Number =>
                AddressComponent.Name | AddressComponent.Street | AddressComponent.HouseNumber |
                AddressComponent.PostalCode | AddressComponent.Unit | AddressComponent.Level,

            _ => AddressComponent.None
        };
    }
}
