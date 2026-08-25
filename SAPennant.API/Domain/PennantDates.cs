using System.Globalization;

namespace SAPennant.API.Domain;

public static class PennantDates
{
    private static readonly string[] DisplayFormats = { "dd MMM yyyy", "dd MMMM yyyy" };

    /// Parses a display date like "05 Sept 2026" as stored in PennantMatch.Date.
    public static DateOnly? ParseDisplayDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;

        var normalised = date
            .Replace("Sept ", "Sep ")
            .Replace("June ", "Jun ")
            .Replace("July ", "Jul ");

        return DateOnly.TryParseExact(
            normalised,
            DisplayFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var d
        ) ? d : null;
    }
}
