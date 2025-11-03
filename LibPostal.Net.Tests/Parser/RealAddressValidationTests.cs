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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
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

    #region Additional US Address Variations

    [Fact]
    public void Parse_USAddress_WithBoulevard_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "456 Ocean Boulevard, Miami FL 33139";
        var result = _parser.Parse(address);

        result.GetComponent("house_number").Should().Be("456");
        result.GetComponent("road").Should().Contain("ocean");
        result.GetComponent("city").Should().Contain("miami");
        result.GetComponent("state").Should().Be("fl");
        result.GetComponent("postcode").Should().Be("33139");
    }

    [Fact]
    public void Parse_USAddress_WithDrive_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "789 Park Drive, Seattle WA 98101";
        var result = _parser.Parse(address);

        result.GetComponent("house_number").Should().Be("789");
        result.GetComponent("road").Should().Contain("park");
        result.GetComponent("city").Should().Contain("seattle");
        result.GetComponent("state").Should().Be("wa");
    }

    [Fact]
    public void Parse_USAddress_WithDirectional_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "100 North Main Street, Chicago IL 60601";
        var result = _parser.Parse(address);

        result.GetComponent("house_number").Should().Be("100");
        result.GetComponent("road").Should().MatchRegex("north.*main|main");
        result.GetComponent("city").Should().Contain("chicago");
        result.GetComponent("state").Should().Be("il");
    }

    [Fact]
    public void Parse_USAddress_Massachusetts_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "1 Main Street, Boston MA 02108";
        var result = _parser.Parse(address);

        result.GetComponent("house_number").Should().Be("1");
        result.GetComponent("city").Should().Contain("boston");
        result.GetComponent("state").Should().Be("ma");
        result.GetComponent("postcode").Should().Be("02108");
    }

    [Fact]
    public void Parse_USAddress_Texas_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "500 Congress Avenue, Austin TX 78701";
        var result = _parser.Parse(address);

        result.GetComponent("house_number").Should().Be("500");
        result.GetComponent("road").Should().Contain("congress");
        result.GetComponent("city").Should().Contain("austin");
        result.GetComponent("state").Should().Be("tx");
    }

    #endregion

    #region Additional International Addresses

    [Fact]
    public void Parse_SpanishAddress_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "Calle Mayor 10, 28013 Madrid, España";
        var result = _parser.Parse(address);

        result.GetComponent("road").Should().Contain("mayor");
        result.GetComponent("house_number").Should().Be("10");
        result.GetComponent("postcode").Should().Be("28013");
        result.GetComponent("city").Should().Contain("madrid");
    }

    [Fact]
    public void Parse_ItalianAddress_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "Via Roma 123, 00100 Roma, Italia";
        var result = _parser.Parse(address);

        result.GetComponent("road").Should().Contain("roma");
        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("postcode").Should().Be("00100");
        result.GetComponent("city").Should().Contain("roma");
    }

    [Fact]
    public void Parse_AustralianAddress_ShouldExtractCorrectly()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "123 George Street, Sydney NSW 2000, Australia";
        var result = _parser.Parse(address);

        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("george");
        result.GetComponent("city").Should().Contain("sydney");
        result.GetComponent("state").Should().Be("nsw");
        result.GetComponent("postcode").Should().Be("2000");
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void Parse_AddressWithFloor_ShouldExtractLevel()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "Floor 3, 123 Main Street, New York NY 10001";
        var result = _parser.Parse(address);

        var components = string.Join(" ", result.Components);
        components.Should().MatchRegex("(floor|3)");
        result.GetComponent("house_number").Should().Be("123");
    }

    [Fact]
    public void Parse_AddressWithHashUnit_ShouldExtractUnit()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "#5, 123 Main Street, Brooklyn NY 11216";
        var result = _parser.Parse(address);

        var components = string.Join(" ", result.Components);
        components.Should().Contain("5");
        result.GetComponent("house_number").Should().Be("123");
    }

    [Fact]
    public void Parse_LongComplexAddress_ShouldHandleGracefully()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "The Empire State Building, 350 Fifth Avenue, Suite 7000, New York NY 10118, United States";
        var result = _parser.Parse(address);

        result.Should().NotBeNull();
        result.GetComponent("house_number").Should().Be("350");
        result.GetComponent("road").Should().Contain("fifth");
        result.GetComponent("city").Should().Contain("new york");
    }

    [Fact]
    public void Parse_AddressWithAbbreviations_ShouldExpand()
    {
        if (!_modelsAvailable || _parser == null) return;

        var address = "123 Main St, NYC NY 10001";
        var result = _parser.Parse(address);

        result.GetComponent("house_number").Should().Be("123");
        result.GetComponent("road").Should().Contain("main");
        result.GetComponent("state").Should().Be("ny");
        result.GetComponent("postcode").Should().Be("10001");
    }

    #endregion
}
