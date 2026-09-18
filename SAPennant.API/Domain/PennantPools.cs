using System.Text.RegularExpressions;

namespace SAPennant.API.Domain;

public static class PennantPools
{
    private static readonly Regex DivisionNumber =
        new(@"^(?:Senior\s+)?Div(?:ision)?\s+(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// Golfbox labels the senior pools "Division N" in some seasons (2021-2024)
    /// and "Div N" in others (2025+), and neither form says which competition
    /// it belongs to — unlike every other pool ("Junior Div 2", "Men's A2").
    /// Normalise both to "Senior Div N" at ingest so a division keeps one
    /// identity across seasons and reads unambiguously in the pool list.
    public static string Normalise(string pool, bool isSenior)
    {
        if (string.IsNullOrWhiteSpace(pool)) return pool;

        var trimmed = pool.Trim();
        if (!isSenior) return trimmed;

        var match = DivisionNumber.Match(trimmed);
        return match.Success ? $"Senior Div {match.Groups[1].Value}" : trimmed;
    }
}
