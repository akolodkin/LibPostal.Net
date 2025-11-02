using FluentAssertions;
using LibPostal.Net.Expansion;

namespace LibPostal.Net.Tests.Expansion;

/// <summary>
/// Tests for StringTree class.
/// Based on libpostal's string_tree_t structure.
/// </summary>
public class StringTreeTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmpty()
    {
        // Act
        var tree = new StringTree();

        // Assert
        tree.Should().NotBeNull();
        tree.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void AddString_WithSingleString_ShouldAddNode()
    {
        // Arrange
        var tree = new StringTree();

        // Act
        tree.AddString("hello");

        // Assert
        tree.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void AddAlternatives_WithMultipleAlternatives_ShouldCreateBranches()
    {
        // Arrange
        var tree = new StringTree();

        // Act
        tree.AddAlternatives(new[] { "st", "street", "str" });

        // Assert
        tree.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void GetAllCombinations_WithSingleString_ShouldReturnSingleResult()
    {
        // Arrange
        var tree = new StringTree();
        tree.AddString("hello");

        // Act
        var combinations = tree.GetAllCombinations().ToList();

        // Assert
        combinations.Should().HaveCount(1);
        combinations[0].Should().Be("hello");
    }

    [Fact]
    public void GetAllCombinations_WithTwoAlternatives_ShouldReturnBothResults()
    {
        // Arrange
        var tree = new StringTree();
        tree.AddAlternatives(new[] { "st", "street" });

        // Act
        var combinations = tree.GetAllCombinations().ToList();

        // Assert
        combinations.Should().HaveCount(2);
        combinations.Should().Contain("st");
        combinations.Should().Contain("street");
    }

    [Fact]
    public void GetAllCombinations_WithMultiplePositions_ShouldReturnAllPermutations()
    {
        // Arrange
        var tree = new StringTree();
        tree.AddAlternatives(new[] { "main", "principal" }); // Position 0
        tree.AddString(" "); // Position 1
        tree.AddAlternatives(new[] { "st", "street" }); // Position 2

        // Act
        var combinations = tree.GetAllCombinations().ToList();

        // Assert
        combinations.Should().HaveCount(4);
        combinations.Should().Contain("main st");
        combinations.Should().Contain("main street");
        combinations.Should().Contain("principal st");
        combinations.Should().Contain("principal street");
    }

    [Fact]
    public void GetAllCombinations_WithPermutationLimit_ShouldLimitResults()
    {
        // Arrange
        var tree = new StringTree(maxPermutations: 5);

        // Create 3x3 = 9 combinations (exceeds limit of 5)
        tree.AddAlternatives(new[] { "a", "b", "c" });
        tree.AddString(" ");
        tree.AddAlternatives(new[] { "x", "y", "z" });

        // Act
        var combinations = tree.GetAllCombinations().ToList();

        // Assert
        combinations.Should().HaveCount(5); // Limited to 5
    }

    [Fact]
    public void GetAllCombinations_WithEmptyTree_ShouldReturnEmpty()
    {
        // Arrange
        var tree = new StringTree();

        // Act
        var combinations = tree.GetAllCombinations().ToList();

        // Assert
        combinations.Should().BeEmpty();
    }

    [Fact]
    public void AddString_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tree = new StringTree();

        // Act
        Action act = () => tree.AddString(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddAlternatives_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tree = new StringTree();

        // Act
        Action act = () => tree.AddAlternatives(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetAllCombinations_WithComplexTree_ShouldGenerateCorrectly()
    {
        // Arrange - simulate "123 Main St" with alternatives
        var tree = new StringTree();
        tree.AddString("123");
        tree.AddString(" ");
        tree.AddAlternatives(new[] { "main", "principal" });
        tree.AddString(" ");
        tree.AddAlternatives(new[] { "st", "street", "str" });

        // Act
        var combinations = tree.GetAllCombinations().ToList();

        // Assert
        combinations.Should().HaveCount(6); // 1 x 2 x 3
        combinations.Should().Contain("123 main st");
        combinations.Should().Contain("123 main street");
        combinations.Should().Contain("123 main str");
        combinations.Should().Contain("123 principal st");
        combinations.Should().Contain("123 principal street");
        combinations.Should().Contain("123 principal str");
    }
}
