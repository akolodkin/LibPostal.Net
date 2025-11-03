namespace LibPostal.Net.Parser;

/// <summary>
/// Represents the result of parsing an address.
/// Based on libpostal's libpostal_address_parser_response_t
/// </summary>
public class AddressParserResponse
{
    /// <summary>
    /// Gets the number of components.
    /// </summary>
    public int NumComponents => Components.Length;

    /// <summary>
    /// Gets the component values (text segments).
    /// </summary>
    public string[] Components { get; }

    /// <summary>
    /// Gets the component labels (house_number, road, city, etc.).
    /// </summary>
    public string[] Labels { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressParserResponse"/> class.
    /// </summary>
    /// <param name="components">The component values.</param>
    /// <param name="labels">The component labels.</param>
    public AddressParserResponse(string[] components, string[] labels)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(labels);

        if (components.Length != labels.Length)
        {
            throw new ArgumentException("Components and labels must have the same length.");
        }

        Components = components;
        Labels = labels;
    }

    /// <summary>
    /// Gets the value of a component by its label.
    /// </summary>
    /// <param name="label">The label to search for.</param>
    /// <returns>The component value, or null if not found.</returns>
    public string? GetComponent(string label)
    {
        ArgumentNullException.ThrowIfNull(label);

        for (int i = 0; i < Labels.Length; i++)
        {
            if (Labels[i] == label)
            {
                return Components[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the response contains a component with the specified label.
    /// </summary>
    /// <param name="label">The label to check for.</param>
    /// <returns>True if the component exists; otherwise, false.</returns>
    public bool HasComponent(string label)
    {
        ArgumentNullException.ThrowIfNull(label);
        return Labels.Contains(label);
    }

    /// <summary>
    /// Returns a string representation of the parsed address.
    /// </summary>
    public override string ToString()
    {
        var parts = new List<string>();
        for (int i = 0; i < NumComponents; i++)
        {
            parts.Add($"{Labels[i]}: {Components[i]}");
        }
        return string.Join(", ", parts);
    }
}
