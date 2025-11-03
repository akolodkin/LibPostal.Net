using FluentAssertions;
using LibPostal.Net.Parser;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for AddressParserResponse.
/// Based on libpostal's libpostal_address_parser_response_t
/// </summary>
public class AddressParserResponseTests
{
    [Fact]
    public void Constructor_WithComponentsAndLabels_ShouldInitialize()
    {
        // Arrange
        var components = new[] { "123", "main street", "brooklyn" };
        var labels = new[] { "house_number", "road", "city" };

        // Act
        var response = new AddressParserResponse(components, labels);

        // Assert
        response.NumComponents.Should().Be(3);
        response.Components.Should().Equal(components);
        response.Labels.Should().Equal(labels);
    }

    [Fact]
    public void GetComponent_ByLabel_ShouldReturnValue()
    {
        // Arrange
        var components = new[] { "123", "main street" };
        var labels = new[] { "house_number", "road" };
        var response = new AddressParserResponse(components, labels);

        // Act
        var houseNumber = response.GetComponent("house_number");
        var road = response.GetComponent("road");

        // Assert
        houseNumber.Should().Be("123");
        road.Should().Be("main street");
    }

    [Fact]
    public void GetComponent_NonExistentLabel_ShouldReturnNull()
    {
        // Arrange
        var response = new AddressParserResponse(new[] { "123" }, new[] { "house_number" });

        // Act
        var result = response.GetComponent("city");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void HasComponent_ExistingLabel_ShouldReturnTrue()
    {
        // Arrange
        var response = new AddressParserResponse(new[] { "123" }, new[] { "house_number" });

        // Act
        var result = response.HasComponent("house_number");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasComponent_NonExistentLabel_ShouldReturnFalse()
    {
        // Arrange
        var response = new AddressParserResponse(new[] { "123" }, new[] { "house_number" });

        // Act
        var result = response.HasComponent("city");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldFormatReadably()
    {
        // Arrange
        var response = new AddressParserResponse(
            new[] { "123", "main st" },
            new[] { "house_number", "road" });

        // Act
        var result = response.ToString();

        // Assert
        result.Should().Contain("123");
        result.Should().Contain("house_number");
        result.Should().Contain("main st");
        result.Should().Contain("road");
    }
}
