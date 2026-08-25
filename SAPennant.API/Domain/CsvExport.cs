using System.Globalization;

namespace SAPennant.API.Domain;

/// Helpers for the admin database-backup export.
public static class CsvExport
{
    public static string Escape(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        var mustQuote = field.Contains(',') || field.Contains('"') ||
                        field.Contains('\n') || field.Contains('\r');
        return mustQuote ? $"\"{field.Replace("\"", "\"\"")}\"" : field;
    }

    /// Culture-invariant, round-trippable text for a database value.
    public static string FormatValue(object? value) => value switch
    {
        null or DBNull => "",
        bool b => b ? "true" : "false",
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? "",
        _ => value.ToString() ?? ""
    };
}
