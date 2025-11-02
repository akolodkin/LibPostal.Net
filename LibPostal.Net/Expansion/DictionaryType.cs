namespace LibPostal.Net.Expansion;

/// <summary>
/// Represents types of dictionaries used for address expansion.
/// Based on libpostal's gazetteer types.
/// </summary>
public enum DictionaryType
{
    /// <summary>
    /// Street name dictionary
    /// </summary>
    StreetName = 0,

    /// <summary>
    /// Street type (avenue, boulevard, street, etc.)
    /// </summary>
    StreetType,

    /// <summary>
    /// Directional (north, south, east, west, etc.)
    /// </summary>
    Directional,

    /// <summary>
    /// Building type (building, tower, complex, etc.)
    /// </summary>
    BuildingType,

    /// <summary>
    /// Unit type (apartment, suite, unit, etc.)
    /// </summary>
    UnitType,

    /// <summary>
    /// Level type (floor, level, etc.)
    /// </summary>
    LevelType,

    /// <summary>
    /// House number
    /// </summary>
    HouseNumber,

    /// <summary>
    /// Number word
    /// </summary>
    Number,

    /// <summary>
    /// Post office
    /// </summary>
    PostOffice,

    /// <summary>
    /// Postal code
    /// </summary>
    PostalCode,

    /// <summary>
    /// Company type (corporation, limited, etc.)
    /// </summary>
    CompanyType,

    /// <summary>
    /// Place name
    /// </summary>
    PlaceName,

    /// <summary>
    /// Personal title (mr, mrs, dr, etc.)
    /// </summary>
    PersonalTitle,

    /// <summary>
    /// Academic degree
    /// </summary>
    AcademicDegree,

    /// <summary>
    /// Qualifier (old, new, upper, lower, etc.)
    /// </summary>
    Qualifier,

    /// <summary>
    /// Synonym (alternative form)
    /// </summary>
    Synonym,

    /// <summary>
    /// Stopword (the, of, etc.)
    /// </summary>
    Stopword,

    /// <summary>
    /// Elision (d', l', etc. in French)
    /// </summary>
    Elision,

    /// <summary>
    /// Ambiguous expansion
    /// </summary>
    AmbiguousExpansion,

    /// <summary>
    /// Unknown dictionary type
    /// </summary>
    Unknown
}
