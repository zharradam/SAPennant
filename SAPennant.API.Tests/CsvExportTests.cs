using SAPennant.API.Domain;

namespace SAPennant.API.Tests;

public class CsvExportTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("plain", "plain")]
    [InlineData("has,comma", "\"has,comma\"")]
    [InlineData("has \"quotes\"", "\"has \"\"quotes\"\"\"")]
    [InlineData("line\nbreak", "\"line\nbreak\"")]
    [InlineData("O'Brien & Smith", "O'Brien & Smith")]
    public void Escape_QuotesOnlyWhenNeeded(string? input, string expected)
    {
        Assert.Equal(expected, CsvExport.Escape(input));
    }

    [Fact]
    public void FormatValue_HandlesDatabaseTypes()
    {
        Assert.Equal("", CsvExport.FormatValue(null));
        Assert.Equal("", CsvExport.FormatValue(DBNull.Value));
        Assert.Equal("true", CsvExport.FormatValue(true));
        Assert.Equal("false", CsvExport.FormatValue(false));
        Assert.Equal("2026-05-03", CsvExport.FormatValue(new DateOnly(2026, 5, 3)));
        Assert.Equal("2026-05-03 08:15:00.000", CsvExport.FormatValue(new DateTime(2026, 5, 3, 8, 15, 0)));
        Assert.Equal("2.5", CsvExport.FormatValue(2.5m));  // invariant culture, no comma decimals
        Assert.Equal("42", CsvExport.FormatValue(42));
        Assert.Equal("text", CsvExport.FormatValue("text"));
    }
}
