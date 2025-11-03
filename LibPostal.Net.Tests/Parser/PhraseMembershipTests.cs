using FluentAssertions;
using LibPostal.Net.Parser;

namespace LibPostal.Net.Tests.Parser;

/// <summary>
/// Tests for PhraseMembership - tracks which tokens belong to which phrases.
/// </summary>
public class PhraseMembershipTests
{
    [Fact]
    public void Constructor_WithValidTokenCount_ShouldInitialize()
    {
        // Act
        var membership = new PhraseMembership(5);

        // Assert
        membership.Should().NotBeNull();
        membership.TokenCount.Should().Be(5);
    }

    [Fact]
    public void Constructor_WithNegativeTokenCount_ShouldThrow()
    {
        // Act
        Action act = () => new PhraseMembership(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithZeroTokenCount_ShouldInitialize()
    {
        // Act
        var membership = new PhraseMembership(0);

        // Assert
        membership.TokenCount.Should().Be(0);
    }

    [Fact]
    public void AssignPhrase_WithSingleToken_ShouldAssign()
    {
        // Arrange
        var membership = new PhraseMembership(5);
        var phrase = new PhraseMatch
        {
            PhraseText = "street",
            PhraseId = 1,
            StartIndex = 2,
            EndIndex = 2,
            Length = 1
        };

        // Act
        membership.AssignPhrase(phrase);

        // Assert
        var assigned = membership.GetPhraseAt(2);
        assigned.Should().NotBeNull();
        assigned.PhraseText.Should().Be("street");
    }

    [Fact]
    public void AssignPhrase_WithMultiTokenPhrase_ShouldAssignAllTokens()
    {
        // Arrange
        var membership = new PhraseMembership(6);
        var phrase = new PhraseMatch
        {
            PhraseText = "main street",
            PhraseId = 10,
            StartIndex = 1,
            EndIndex = 2,
            Length = 2
        };

        // Act
        membership.AssignPhrase(phrase);

        // Assert
        var assigned1 = membership.GetPhraseAt(1);
        var assigned2 = membership.GetPhraseAt(2);

        assigned1.Should().NotBeNull();
        assigned1.PhraseText.Should().Be("main street");

        assigned2.Should().NotBeNull();
        assigned2.PhraseText.Should().Be("main street");
    }

    [Fact]
    public void AssignPhrase_WithOverlap_ShouldPreferFirst()
    {
        // Arrange
        var membership = new PhraseMembership(5);

        var phrase1 = new PhraseMatch
        {
            PhraseText = "main street",
            PhraseId = 1,
            StartIndex = 1,
            EndIndex = 2,
            Length = 2
        };

        var phrase2 = new PhraseMatch
        {
            PhraseText = "street avenue",
            PhraseId = 2,
            StartIndex = 2,
            EndIndex = 3,
            Length = 2
        };

        // Act - assign first phrase
        membership.AssignPhrase(phrase1);

        // Try to assign overlapping phrase
        membership.AssignPhrase(phrase2);

        // Assert - token 2 should still have phrase1
        var assigned = membership.GetPhraseAt(2);
        assigned.Should().NotBeNull();
        assigned.PhraseId.Should().Be(1);
        assigned.PhraseText.Should().Be("main street");
    }

    [Fact]
    public void GetPhraseAt_WithUnassignedToken_ShouldReturnNull()
    {
        // Arrange
        var membership = new PhraseMembership(5);

        // Act
        var result = membership.GetPhraseAt(2);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetPhraseAt_WithInvalidIndex_ShouldThrow()
    {
        // Arrange
        var membership = new PhraseMembership(5);

        // Act
        Action act1 = () => membership.GetPhraseAt(-1);
        Action act2 = () => membership.GetPhraseAt(5);

        // Assert
        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsStartOfPhrase_WithPhraseStart_ShouldReturnTrue()
    {
        // Arrange
        var membership = new PhraseMembership(5);
        var phrase = new PhraseMatch
        {
            PhraseText = "oak avenue",
            PhraseId = 1,
            StartIndex = 1,
            EndIndex = 2,
            Length = 2
        };

        membership.AssignPhrase(phrase);

        // Act & Assert
        membership.IsStartOfPhrase(1).Should().BeTrue();
        membership.IsStartOfPhrase(2).Should().BeFalse(); // middle/end, not start
    }

    [Fact]
    public void IsEndOfPhrase_WithPhraseEnd_ShouldReturnTrue()
    {
        // Arrange
        var membership = new PhraseMembership(5);
        var phrase = new PhraseMatch
        {
            PhraseText = "oak avenue",
            PhraseId = 1,
            StartIndex = 1,
            EndIndex = 2,
            Length = 2
        };

        membership.AssignPhrase(phrase);

        // Act & Assert
        membership.IsEndOfPhrase(2).Should().BeTrue();
        membership.IsEndOfPhrase(1).Should().BeFalse(); // start, not end
    }

    [Fact]
    public void IsMiddleOfPhrase_WithThreeTokenPhrase_ShouldIdentifyMiddle()
    {
        // Arrange
        var membership = new PhraseMembership(5);
        var phrase = new PhraseMatch
        {
            PhraseText = "central park west",
            PhraseId = 1,
            StartIndex = 0,
            EndIndex = 2,
            Length = 3
        };

        membership.AssignPhrase(phrase);

        // Act & Assert
        membership.IsStartOfPhrase(0).Should().BeTrue();
        membership.IsMiddleOfPhrase(1).Should().BeTrue();
        membership.IsEndOfPhrase(2).Should().BeTrue();

        // Token 1 is middle - not start and not end
        membership.IsStartOfPhrase(1).Should().BeFalse();
        membership.IsEndOfPhrase(1).Should().BeFalse();
    }

    [Fact]
    public void HasPhrase_WithAssignedToken_ShouldReturnTrue()
    {
        // Arrange
        var membership = new PhraseMembership(3);
        var phrase = new PhraseMatch
        {
            PhraseText = "test",
            PhraseId = 1,
            StartIndex = 1,
            EndIndex = 1,
            Length = 1
        };

        membership.AssignPhrase(phrase);

        // Act & Assert
        membership.HasPhrase(1).Should().BeTrue();
        membership.HasPhrase(0).Should().BeFalse();
        membership.HasPhrase(2).Should().BeFalse();
    }
}
