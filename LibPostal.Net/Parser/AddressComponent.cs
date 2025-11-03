namespace LibPostal.Net.Parser;

/// <summary>
/// Address component flags matching libpostal's address_parser_types.h
/// Used for categorizing dictionary phrases and address components.
/// </summary>
[Flags]
public enum AddressComponent : uint
{
    /// <summary>
    /// No component.
    /// </summary>
    None = 0,

    /// <summary>
    /// House number (e.g., "123").
    /// </summary>
    HouseNumber = 1 << 0,

    /// <summary>
    /// House/building name (e.g., "Empire State Building").
    /// </summary>
    House = 1 << 1,

    /// <summary>
    /// Category/type (e.g., "restaurant", "hotel").
    /// </summary>
    Category = 1 << 2,

    /// <summary>
    /// Near/proximity (e.g., "near Central Park").
    /// </summary>
    Near = 1 << 3,

    /// <summary>
    /// Road/street name (e.g., "Main Street", "Fifth Avenue").
    /// </summary>
    Road = 1 << 4,

    /// <summary>
    /// Unit/apartment number (e.g., "Apt 5", "Unit 3B").
    /// </summary>
    Unit = 1 << 5,

    /// <summary>
    /// Floor/level (e.g., "Floor 3", "Level 2").
    /// </summary>
    Level = 1 << 6,

    /// <summary>
    /// Staircase (e.g., "Staircase B").
    /// </summary>
    Staircase = 1 << 7,

    /// <summary>
    /// Entrance (e.g., "Entrance A").
    /// </summary>
    Entrance = 1 << 8,

    /// <summary>
    /// PO Box (e.g., "PO Box 123").
    /// </summary>
    POBox = 1 << 9,

    /// <summary>
    /// Postal code (e.g., "10001", "SW1A 1AA").
    /// </summary>
    Postcode = 1 << 10,

    /// <summary>
    /// Suburb/neighborhood.
    /// </summary>
    Suburb = 1 << 11,

    /// <summary>
    /// City district.
    /// </summary>
    CityDistrict = 1 << 12,

    /// <summary>
    /// City name (e.g., "Brooklyn", "London").
    /// </summary>
    City = 1 << 13,

    /// <summary>
    /// Island (e.g., "Manhattan", "Sicily").
    /// </summary>
    Island = 1 << 14,

    /// <summary>
    /// State district/region within state.
    /// </summary>
    StateDistrict = 1 << 15,

    /// <summary>
    /// State/province (e.g., "NY", "California", "Ontario").
    /// </summary>
    State = 1 << 16,

    /// <summary>
    /// Country region (e.g., "New England", "Bavaria").
    /// </summary>
    CountryRegion = 1 << 17,

    /// <summary>
    /// Country name (e.g., "USA", "United Kingdom").
    /// </summary>
    Country = 1 << 18,

    /// <summary>
    /// World region (e.g., "Europe", "Asia").
    /// </summary>
    WorldRegion = 1 << 19,

    /// <summary>
    /// Generic name.
    /// </summary>
    Name = 1 << 20,

    /// <summary>
    /// Toponym/place name.
    /// </summary>
    Toponym = 1 << 21
}
