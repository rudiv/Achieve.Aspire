using Achieve.Aspire.AzureProvisioning.Extensions;

namespace Achieve.Aspire.AzureProvisioning.Tests.Extensions;

public class StringExtensionTests
{
    [Fact]
    public void ShouldFailWhenLengthIsTooShort()
    {
        const string test = "a";
        var result = test.MatchesConstraints(3, 12, StringExtensions.CharacterClass.Any);
        Assert.False(result);
    }

    [Fact]
    public void ShouldFailWhenLengthIsTooLong()
    {
        const string test = "ThisIsATest";
        var result = test.MatchesConstraints(3, 5, StringExtensions.CharacterClass.Any);
        Assert.False(result);
    }

    [Fact]
    public void ShoudlAcceptValidLength()
    {
        const string test = "ThisIsATest";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.Any);
        Assert.True(result);
    }

    [Fact]
    public void ShouldPassWithOnlyAlphabetCharacters()
    {
        const string test = "ThisIsATest";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.LowercaseLetter | StringExtensions.CharacterClass.UppercaseLetter);
        Assert.True(result);
    }
    
    [Fact]
    public void ShouldAcceptAlphabeticCharacters()
    {
        const string test = "ThisIsATest";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.Alphabetic);
        Assert.True(result);
    }

    [Fact]
    public void ShouldNotAcceptNonAlphabeticCharacters()
    {
        const string test = "ThisI_sAnother4Test-";
        var result = test.MatchesConstraints(2, 25, StringExtensions.CharacterClass.Alphabetic);
        Assert.False(result);
    }
    
    [Fact]
    public void ShouldAcceptAlphanumericCharacters()
    {
        const string test = "Test3";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.Alphanumeric);
        Assert.True(result);
    }

    [Fact]
    public void ShouldFailWhenExpectingOnlyAlphaCharacters()
    {
        const string test = "This is a big-test";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.LowercaseLetter | StringExtensions.CharacterClass.UppercaseLetter);
        Assert.False(result);
    }

    [Fact]
    public void ShouldOnlyAllowAlphaNumeric()
    {
        const string test = "Test007";
        var result = test.MatchesConstraints(3, 25,
            StringExtensions.CharacterClass.LowercaseLetter | StringExtensions.CharacterClass.UppercaseLetter | StringExtensions.CharacterClass.Number);
        Assert.True(result);
    }

    [Fact]
    public void ShouldFailWhenExpectingOnlyAlphaNumeric()
    {
        const string test = "Test#007-";
        var result = test.MatchesConstraints(3, 25,
            StringExtensions.CharacterClass.LowercaseLetter | StringExtensions.CharacterClass.UppercaseLetter | StringExtensions.CharacterClass.Number);
        Assert.False(result);
    }

    [Fact]
    public void ShouldOnlyAllowLowerAlphaNumeric()
    {
        const string test = "test007";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.LowercaseLetter | StringExtensions.CharacterClass.Number);
        Assert.True(result);
    }

    [Fact]
    public void ShouldAllowUnderscores()
    {
        const string test = "test_993";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.Underscore | StringExtensions.CharacterClass.LowercaseLetter | StringExtensions.CharacterClass.Number);
        Assert.True(result);
    }

    [Fact]
    public void ShouldAllowHyphens()
    {
        const string test = "test--993";
        var result = test.MatchesConstraints(3, 25, StringExtensions.CharacterClass.LowercaseLetter | StringExtensions.CharacterClass.Hyphen | StringExtensions.CharacterClass.Number);
        Assert.True(result);
    }
}
