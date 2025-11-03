namespace LibPostal.Net.Parser;

/// <summary>
/// Builder for creating AddressParser instances with fluent API.
/// </summary>
public class AddressParserBuilder
{
    private AddressParserModel? _model;
    private string? _dataDirectory;

    private AddressParserBuilder()
    {
    }

    /// <summary>
    /// Creates a new instance of the builder.
    /// </summary>
    /// <returns>A new AddressParserBuilder.</returns>
    public static AddressParserBuilder Create()
    {
        return new AddressParserBuilder();
    }

    /// <summary>
    /// Sets the model to use.
    /// </summary>
    /// <param name="model">The address parser model.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AddressParserBuilder WithModel(AddressParserModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = model;
        return this;
    }

    /// <summary>
    /// Sets the data directory to load the model from.
    /// </summary>
    /// <param name="dataDirectory">The directory containing model files.</param>
    /// <returns>This builder instance for chaining.</returns>
    public AddressParserBuilder WithDataDirectory(string dataDirectory)
    {
        ArgumentNullException.ThrowIfNull(dataDirectory);
        _dataDirectory = dataDirectory;
        return this;
    }

    /// <summary>
    /// Builds the AddressParser instance.
    /// </summary>
    /// <returns>A configured AddressParser.</returns>
    /// <exception cref="InvalidOperationException">Thrown when neither model nor data directory is set.</exception>
    public AddressParser Build()
    {
        // Model takes precedence over directory
        if (_model != null)
        {
            return new AddressParser(_model);
        }

        if (_dataDirectory != null)
        {
            return AddressParser.LoadFromDirectory(_dataDirectory);
        }

        throw new InvalidOperationException(
            "Either a model or data directory must be provided. " +
            "Use WithModel() or WithDataDirectory() before calling Build().");
    }
}
