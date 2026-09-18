using SAPennant.API.Domain;

namespace SAPennant.API.Tests;

public class PennantPoolsTests
{
    [Theory]
    // The senior seasons Golfbox labels the long way (2021-2024).
    [InlineData("Division 1", "Div 1")]
    [InlineData("Division 2", "Div 2")]
    [InlineData("Division 3", "Div 3")]
    // Already in the short form (2025+) — left alone.
    [InlineData("Div 1", "Div 1")]
    [InlineData("Div 3", "Div 3")]
    public void SeniorDivisionsNormaliseToTheShortForm(string raw, string expected)
    {
        Assert.Equal(expected, PennantPools.Normalise(raw));
    }

    [Theory]
    // Non-senior pool names must survive untouched — especially the junior
    // pools, whose names merely start with a word ending in "Division".
    [InlineData("Simpson Cup")]
    [InlineData("Bonnar Cup")]
    [InlineData("Men's A2")]
    [InlineData("Women's Cleek 1")]
    [InlineData("Junior Div 2")]
    [InlineData("Junior Division 2")]
    [InlineData("Sharp Cup")]
    [InlineData("Pike Cup")]
    public void OtherPoolNamesAreUnchanged(string pool)
    {
        Assert.Equal(pool, PennantPools.Normalise(pool));
    }

    [Fact]
    public void SurroundingWhitespaceIsTrimmed()
    {
        Assert.Equal("Div 1", PennantPools.Normalise("  Division 1 "));
        Assert.Equal("Simpson Cup", PennantPools.Normalise(" Simpson Cup  "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyInputIsReturnedAsIs(string pool)
    {
        Assert.Equal(pool, PennantPools.Normalise(pool));
    }
}
