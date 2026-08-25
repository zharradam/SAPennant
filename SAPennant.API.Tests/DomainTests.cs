using SAPennant.API.Domain;

namespace SAPennant.API.Tests;

public class PennantDatesTests
{
    [Theory]
    [InlineData("03 May 2026", 2026, 5, 3)]
    [InlineData("05 Sept 2026", 2026, 9, 5)]  // non-standard month name from legacy rows
    [InlineData("14 June 2025", 2025, 6, 14)]
    [InlineData("21 July 2024", 2024, 7, 21)]
    [InlineData("01 Jan 2021", 2021, 1, 1)]
    [InlineData("28 February 2023", 2023, 2, 28)]
    public void ParseDisplayDate_ParsesKnownFormats(string input, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), PennantDates.ParseDisplayDate(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    [InlineData("2026-05-03")]
    [InlineData("32 Jan 2026")]
    public void ParseDisplayDate_ReturnsNullForUnparseable(string? input)
    {
        Assert.Null(PennantDates.ParseDisplayDate(input));
    }
}

public class PlayerRulesTests
{
    [Theory]
    [InlineData("John Smith")]
    [InlineData("Bob & Jane Jones")]
    [InlineData("Amy Lee")]
    public void IsRealPlayerName_AcceptsRealNames(string name)
    {
        Assert.True(PlayerRules.IsRealPlayerName(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData("- ")]
    [InlineData("- Placeholder")]
    [InlineData("abc")]  // too short to be a real "First Last"
    public void IsRealPlayerName_RejectsPlaceholders(string? name)
    {
        Assert.False(PlayerRules.IsRealPlayerName(name));
    }

    [Theory]
    [InlineData("2.1", 2.1)]
    [InlineData("-10", -10)]
    [InlineData("54", 54)]
    [InlineData("0", 0)]
    public void TryParseHandicap_AcceptsValidRange(string raw, decimal expected)
    {
        Assert.True(PlayerRules.TryParseHandicap(raw, out var h));
        Assert.Equal(expected, h);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-11")]
    [InlineData("55")]
    [InlineData("999")]
    public void TryParseHandicap_RejectsInvalid(string? raw)
    {
        Assert.False(PlayerRules.TryParseHandicap(raw, out _));
    }
}

public class ResultParserTests
{
    [Theory]
    [InlineData("5&4", 5)]
    [InlineData("1&0", 1)]
    [InlineData("10&8", 10)]
    [InlineData("3 Holes", 3)]
    [InlineData("1 Hole", 1)]
    [InlineData("A/S", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    [InlineData("Walkover", 0)]
    public void ExtractMargin_ParsesResultStrings(string? result, int expected)
    {
        Assert.Equal(expected, ResultParser.ExtractMargin(result));
    }
}
