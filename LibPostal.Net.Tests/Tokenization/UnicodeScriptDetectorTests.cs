using FluentAssertions;
using LibPostal.Net.Tokenization;

namespace LibPostal.Net.Tests.Tokenization;

/// <summary>
/// Tests for UnicodeScriptDetector.
/// Based on libpostal's unicode_scripts support.
/// </summary>
public class UnicodeScriptDetectorTests
{
    [Fact]
    public void DetectScript_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => UnicodeScriptDetector.DetectScript(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DetectScript_WithEmptyString_ShouldReturnUnknown()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("");

        // Assert
        result.Should().Be(UnicodeScript.Unknown);
    }

    [Fact]
    public void DetectScript_WithLatinText_ShouldDetectLatin()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("Hello World");

        // Assert
        result.Should().Be(UnicodeScript.Latin);
    }

    [Fact]
    public void DetectScript_WithCyrillicText_ShouldDetectCyrillic()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("Москва");

        // Assert
        result.Should().Be(UnicodeScript.Cyrillic);
    }

    [Fact]
    public void DetectScript_WithArabicText_ShouldDetectArabic()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("العربية");

        // Assert
        result.Should().Be(UnicodeScript.Arabic);
    }

    [Fact]
    public void DetectScript_WithChineseText_ShouldDetectHan()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("北京");

        // Assert
        result.Should().Be(UnicodeScript.Han);
    }

    [Fact]
    public void DetectScript_WithKoreanText_ShouldDetectHangul()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("서울");

        // Assert
        result.Should().Be(UnicodeScript.Hangul);
    }

    [Fact]
    public void DetectScript_WithGreekText_ShouldDetectGreek()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("Αθήνα");

        // Assert
        result.Should().Be(UnicodeScript.Greek);
    }

    [Fact]
    public void DetectScript_WithHebrewText_ShouldDetectHebrew()
    {
        // Act
        var result = UnicodeScriptDetector.DetectScript("ירושלים");

        // Assert
        result.Should().Be(UnicodeScript.Hebrew);
    }

    [Fact]
    public void DetectScript_WithMixedText_ShouldDetectDominantScript()
    {
        // Act - mostly Latin with one Chinese character
        var result = UnicodeScriptDetector.DetectScript("Hello 北 World");

        // Assert - Latin should dominate
        result.Should().Be(UnicodeScript.Latin);
    }

    [Fact]
    public void DetectScript_WithSingleCharacter_ShouldDetectCorrectly()
    {
        // Act
        var resultLatin = UnicodeScriptDetector.DetectScript("A");
        var resultCyrillic = UnicodeScriptDetector.DetectScript("А"); // Cyrillic A
        var resultHan = UnicodeScriptDetector.DetectScript("中");

        // Assert
        resultLatin.Should().Be(UnicodeScript.Latin);
        resultCyrillic.Should().Be(UnicodeScript.Cyrillic);
        resultHan.Should().Be(UnicodeScript.Han);
    }
}
