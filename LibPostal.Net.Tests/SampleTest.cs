using FluentAssertions;

namespace LibPostal.Net.Tests;

/// <summary>
/// Sample test class to verify test infrastructure is working.
/// This will be replaced with actual tests during implementation.
/// </summary>
public class SampleTest
{
    [Fact]
    public void TestInfrastructure_ShouldWork()
    {
        // Arrange
        var expected = "LibPostal.Net";

        // Act
        var actual = "LibPostal.Net";

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("hello", "hello")]
    public void TheoryTest_ShouldWork(string input, string expected)
    {
        // Assert
        input.Should().Be(expected);
    }
}
