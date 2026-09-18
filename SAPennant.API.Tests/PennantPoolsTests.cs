using SAPennant.API.Domain;

namespace SAPennant.API.Tests;

public class PennantPoolsTests
{
    [Theory]
    // Golfbox spells these the long way in 2021-2024 and the short way from
    // 2025; both become the same unambiguous name.
    [InlineData("Division 1", "Senior Div 1")]
    [InlineData("Division 2", "Senior Div 2")]
    [InlineData("Division 3", "Senior Div 3")]
    [InlineData("Div 1", "Senior Div 1")]
    [InlineData("Div 3", "Senior Div 3")]
    public void SeniorDivisionsBecomeSeniorDivN(string raw, string expected)
    {
        Assert.Equal(expected, PennantPools.Normalise(raw, isSenior: true));
    }

    [Fact]
    public void NormalisingAnAlreadyNormalisedNameIsStable()
    {
        var once = PennantPools.Normalise("Division 2", isSenior: true);
        Assert.Equal("Senior Div 2", once);
        Assert.Equal(once, PennantPools.Normalise(once, isSenior: true));
    }

    [Theory]
    // Non-senior pools keep their names, including the junior pools whose
    // names would otherwise look like a senior division.
    [InlineData("Junior Div 2")]
    [InlineData("Junior Division 2")]
    [InlineData("Simpson Cup")]
    [InlineData("Bonnar Cup")]
    [InlineData("Men's A2")]
    [InlineData("Women's Cleek 1")]
    [InlineData("Sharp Cup")]
    public void NonSeniorPoolsAreUnchanged(string pool)
    {
        Assert.Equal(pool, PennantPools.Normalise(pool, isSenior: false));
    }

    [Fact]
    public void ASeniorPoolWithARealNameKeepsIt()
    {
        // Not every senior pool is numbered; only the "Div/Division N" shape
        // is rewritten.
        Assert.Equal("Senior Shield", PennantPools.Normalise("Senior Shield", isSenior: true));
    }

    [Fact]
    public void SurroundingWhitespaceIsTrimmed()
    {
        Assert.Equal("Senior Div 1", PennantPools.Normalise("  Division 1 ", isSenior: true));
        Assert.Equal("Simpson Cup", PennantPools.Normalise(" Simpson Cup  ", isSenior: false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyInputIsReturnedAsIs(string pool)
    {
        Assert.Equal(pool, PennantPools.Normalise(pool, isSenior: true));
    }
}
