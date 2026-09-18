using System.Text.RegularExpressions;

namespace SAPennant.API.Domain;

public static class PennantPools
{
    private static readonly Regex SeniorDivision =
        new(@"^Division\s+(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// Golfbox labels the senior pools "Division N" in some seasons (2021-2024)
    /// and "Div N" in others (2025+). A pool has to keep one identity across
    /// seasons or the same division shows up twice in the pool list and its
    /// ladder splits, so settle on the shorter form at ingest.
    public static string Normalise(string pool)
    {
        if (string.IsNullOrWhiteSpace(pool)) return pool;

        var trimmed = pool.Trim();
        var match = SeniorDivision.Match(trimmed);
        return match.Success ? $"Div {match.Groups[1].Value}" : trimmed;
    }
}
