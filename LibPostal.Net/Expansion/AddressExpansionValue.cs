namespace LibPostal.Net.Expansion;

/// <summary>
/// Represents a collection of expansion alternatives for a phrase.
/// Based on libpostal's address_expansion_value_t structure.
/// </summary>
public class AddressExpansionValue
{
    /// <summary>
    /// Gets the expansion alternatives.
    /// </summary>
    public IReadOnlyList<AddressExpansion> Expansions { get; }

    /// <summary>
    /// Gets the number of expansion alternatives.
    /// </summary>
    public int Count => Expansions.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressExpansionValue"/> class.
    /// </summary>
    /// <param name="expansions">The expansion alternatives.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expansions"/> is null.</exception>
    public AddressExpansionValue(IEnumerable<AddressExpansion> expansions)
    {
        ArgumentNullException.ThrowIfNull(expansions);
        Expansions = expansions.ToList();
    }

    /// <summary>
    /// Gets the expansion at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The expansion at the specified index.</returns>
    public AddressExpansion this[int index] => Expansions[index];
}
