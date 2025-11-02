namespace LibPostal.Net.Expansion;

/// <summary>
/// Represents address component types for filtering expansions.
/// Based on libpostal's LIBPOSTAL_ADDRESS_* constants.
/// </summary>
[Flags]
public enum AddressComponent
{
    /// <summary>
    /// No components
    /// </summary>
    None = 0,

    /// <summary>
    /// Any component (no filtering)
    /// </summary>
    Any = ~0,

    /// <summary>
    /// Person or business name
    /// </summary>
    Name = 1 << 0,

    /// <summary>
    /// House number
    /// </summary>
    HouseNumber = 1 << 1,

    /// <summary>
    /// Street name
    /// </summary>
    Street = 1 << 2,

    /// <summary>
    /// Unit (apartment, suite, etc.)
    /// </summary>
    Unit = 1 << 3,

    /// <summary>
    /// Level (floor)
    /// </summary>
    Level = 1 << 4,

    /// <summary>
    /// Staircase
    /// </summary>
    Staircase = 1 << 5,

    /// <summary>
    /// Entrance
    /// </summary>
    Entrance = 1 << 6,

    /// <summary>
    /// Category (restaurant, hotel, etc.)
    /// </summary>
    Category = 1 << 7,

    /// <summary>
    /// Near (proximity reference)
    /// </summary>
    Near = 1 << 8,

    /// <summary>
    /// Toponym (place name)
    /// </summary>
    Toponym = 1 << 9,

    /// <summary>
    /// Postal code
    /// </summary>
    PostalCode = 1 << 10,

    /// <summary>
    /// PO Box
    /// </summary>
    PoBox = 1 << 11,

    /// <summary>
    /// All components
    /// </summary>
    All = Name | HouseNumber | Street | Unit | Level | Staircase | Entrance | Category | Near | Toponym | PostalCode | PoBox
}
