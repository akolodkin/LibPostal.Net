using System.Text;

namespace LibPostal.Net.IO;

/// <summary>
/// Loads dictionary files used by libpostal.
/// Dictionary files are pipe-delimited UTF-8 text files.
/// Format: "original|abbrev1|abbrev2|..."
/// Example: "street|st|str"
/// </summary>
public static class DictionaryLoader
{
    /// <summary>
    /// Loads dictionary entries from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <returns>A list of dictionary entries, where each entry is a list of alternative forms.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    public static List<List<string>> LoadFromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var entries = new List<List<string>>();

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            // Skip null, empty, or whitespace lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip comment lines
            if (line.TrimStart().StartsWith('#'))
                continue;

            // Split by pipe and trim each value
            var values = line.Split('|')
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            // Only add if we have at least one value
            if (values.Count > 0)
                entries.Add(values);
        }

        return entries;
    }

    /// <summary>
    /// Loads dictionary entries from a file.
    /// </summary>
    /// <param name="path">The path to the dictionary file.</param>
    /// <returns>A list of dictionary entries, where each entry is a list of alternative forms.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static List<List<string>> LoadFromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Dictionary file not found: {path}", path);

        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }
}
