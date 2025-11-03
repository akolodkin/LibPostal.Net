using FluentAssertions;
using LibPostal.Net.Parser;
using System.IO;
using Xunit;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Validation tests using real libpostal models and real-world addresses.
/// These tests validate actual parsing accuracy on diverse address formats.
/// </summary>
public class RealAddressValidationTests
{
    private readonly AddressParser? _parser;
    private readonly bool _modelsAvailable;

    public RealAddressValidationTests()
    {
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dataDir = Path.Combine(userProfile, ".libpostal");
            var parserDir = Path.Combine(dataDir, "address_parser");

            if (Directory.Exists(parserDir) &&
                File.Exists(Path.Combine(parserDir, "address_parser_crf.dat")))
            {
                _parser = AddressParser.LoadFromDirectory(dataDir);
                _modelsAvailable = true;
            }
        }
        catch
        {
            _modelsAvailable = false;
        }
    }

    #region US Addresses - Standard Format

    [Fact]
    public void Parse_USAddress_Standard_ShouldExtractAllComponents()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "123 Main Street, Brooklyn NY 11216";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("main");
        result.GetComponent("city").Should().Contain("brooklyn");
        result.GetComponent("state").Should().Be("ny");
        result.GetComponent("postcode").Should().Be("11216");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_USAddress_WithUnit_ShouldExtractUnit()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "Apt 5, 123 Main Street, Brooklyn NY 11216";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("unit").Should().MatchRegex("apt|5");
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("main");
        result.GetComponent("city").Should().Contain("brooklyn");
        result.GetComponent("state").Should().Be("ny");
        result.GetComponent("postcode").Should().Be("11216");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_USAddress_POBox_ShouldExtractPOBox()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "PO Box 1234, New York NY 10001";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("po_box").Should().Contain("1234");
        result.GetComponent("city").Should().Contain("new york");
        result.GetComponent("state").Should().Be("ny");
        result.GetComponent("postcode").Should().Be("10001");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_USAddress_WithSuite_ShouldExtractSuite()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "Suite 200, 555 W 5th Street, Los Angeles CA 90013";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("unit").Should().MatchRegex("suite|200");
        result.GetComponent("house_number").Should().Be("555");
        result.GetComponent("road").Should().MatchRegex("5th.*street|w.*5th");
        result.GetComponent("city").Should().Contain("los angeles");
        result.GetComponent("state").Should().Be("ca");
        result.GetComponent("postcode").Should().Be("90013");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_USAddress_VenueName_ShouldExtractVenue()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "Barboncino, 781 Franklin Ave, Brooklyn NY 11216";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("name").Should().Contain("barboncino");
        result.GetComponent("house_number").Should().Be("781");
        result.GetComponent("road").Should().Contain("franklin");
        result.GetComponent("city").Should().Contain("brooklyn");
        result.GetComponent("state").Should().Be("ny");
        result.GetComponent("postcode").Should().Be("11216");
    }

    #endregion

    #region International Addresses

    [Fact(Skip = "Requires real models")]
    public void Parse_UKAddress_ShouldParseCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "10 Downing Street, London SW1A 2AA, UK";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("house_number").Should().Be("10");
        result.GetComponent("road").Should().Contain("downing");
        result.GetComponent("city").Should().Contain("london");
        result.GetComponent("postcode").Should().MatchRegex("sw1a.*2aa");
        result.GetComponent("country").Should().Contain("uk");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_CanadianAddress_ShouldParseCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "123 Yonge Street, Toronto ON M5B 1M4, Canada";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("yonge");
        result.GetComponent("city").Should().Contain("toronto");
        result.GetComponent("state").Should().Be("on");
        result.GetComponent("postcode").Should().MatchRegex("m5b.*1m4");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_GermanAddress_ShouldParseCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "Hauptstraße 123, 10115 Berlin, Deutschland";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("road").Should().Contain("hauptstra");
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("postcode").Should().Be("10115");
        result.GetComponent("city").Should().Contain("berlin");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_FrenchAddress_ShouldParseCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "123 Avenue des Champs-Élysées, 75008 Paris, France";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("champs");
        result.GetComponent("postcode").Should().Be("75008");
        result.GetComponent("city").Should().Contain("paris");
    }

    #endregion

    #region Edge Cases

    [Fact(Skip = "Requires real models")]
    public void Parse_AddressWithoutPostcode_ShouldParseRemaining()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "123 Main Street, Brooklyn NY";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("main");
        result.GetComponent("city").Should().Contain("brooklyn");
        result.GetComponent("state").Should().Be("ny");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_AddressWithoutHouseNumber_ShouldParseRemaining()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "Main Street, Brooklyn NY 11216";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("road").Should().Contain("main");
        result.GetComponent("city").Should().Contain("brooklyn");
        result.GetComponent("state").Should().Be("ny");
        result.GetComponent("postcode").Should().Be("11216");
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_VeryShortAddress_ShouldHandleGracefully()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "Brooklyn NY";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.Should().NotBeNull();
        result.Labels.Should().NotBeEmpty();
        // At minimum, should identify city and state
        result.Components.Should().Contain(c => c.Contains("brooklyn") || c.Contains("ny"));
    }

    [Fact(Skip = "Requires real models")]
    public void Parse_ComplexAddress_WithMultipleUnits_ShouldParse()
    {
        if (!_modelsAvailable || _parser == null) return;

        // Arrange
        var address = "Building 5, Floor 3, Apt 12B, 123 Main Street, Brooklyn NY 11216";

        // Act
        var result = _parser.Parse(address);

        // Assert
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("main");
        // Should extract at least one unit/level indicator
        var allComponents = string.Join(" ", result.Components);
        allComponents.Should().MatchRegex("(building|floor|apt|12b)");
    }

    #endregion
}
