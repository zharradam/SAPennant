using System.Text.RegularExpressions;

namespace SAPennant.API.Domain;

public static class ResultParser
{
    /// Extracts the winning margin from a result string like "5&4" or "3 Holes".
    public static int ExtractMargin(string? result)
    {
        if (string.IsNullOrEmpty(result)) return 0;
        var match = Regex.Match(result, @"^(\d+)&");
        if (match.Success) return int.Parse(match.Groups[1].Value);
        match = Regex.Match(result, @"^(\d+) Hole");
        if (match.Success) return int.Parse(match.Groups[1].Value);
        return 0;
    }
}
