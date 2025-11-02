namespace LibPostal.Net.Expansion;

/// <summary>
/// Represents a tree structure for generating string alternatives.
/// Based on libpostal's string_tree_t structure.
/// </summary>
/// <remarks>
/// The string tree is used to generate all possible combinations of address expansions.
/// Each position can have multiple alternatives, and the tree generates the Cartesian product.
/// </remarks>
public class StringTree
{
    private readonly List<string[]> _positions;
    private readonly int _maxPermutations;

    /// <summary>
    /// Gets a value indicating whether the tree is empty.
    /// </summary>
    public bool IsEmpty => _positions.Count == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringTree"/> class.
    /// </summary>
    /// <param name="maxPermutations">The maximum number of permutations to generate (default: 100).</param>
    public StringTree(int maxPermutations = 100)
    {
        _positions = new List<string[]>();
        _maxPermutations = maxPermutations;
    }

    /// <summary>
    /// Adds a single string at the current position.
    /// </summary>
    /// <param name="value">The string to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public void AddString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _positions.Add(new[] { value });
    }

    /// <summary>
    /// Adds multiple alternative strings at the current position.
    /// </summary>
    /// <param name="alternatives">The alternative strings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="alternatives"/> is null.</exception>
    public void AddAlternatives(IEnumerable<string> alternatives)
    {
        ArgumentNullException.ThrowIfNull(alternatives);

        var alternativesArray = alternatives.ToArray();
        if (alternativesArray.Length > 0)
        {
            _positions.Add(alternativesArray);
        }
    }

    /// <summary>
    /// Generates all possible combinations of alternatives.
    /// </summary>
    /// <returns>An enumerable of all possible string combinations.</returns>
    public IEnumerable<string> GetAllCombinations()
    {
        if (IsEmpty)
        {
            yield break;
        }

        var count = 0;
        foreach (var combination in GenerateCombinations(0, new List<string>()))
        {
            yield return string.Concat(combination);

            count++;
            if (count >= _maxPermutations)
            {
                yield break; // Stop if we hit the limit
            }
        }
    }

    private IEnumerable<List<string>> GenerateCombinations(int position, List<string> current)
    {
        if (position >= _positions.Count)
        {
            yield return new List<string>(current);
            yield break;
        }

        foreach (var alternative in _positions[position])
        {
            current.Add(alternative);

            foreach (var combination in GenerateCombinations(position + 1, current))
            {
                yield return combination;
            }

            current.RemoveAt(current.Count - 1);
        }
    }
}
