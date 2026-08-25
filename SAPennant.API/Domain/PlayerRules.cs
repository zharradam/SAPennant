using System.Linq.Expressions;
using SAPennant.API.Models;

namespace SAPennant.API.Domain;

/// Central home for the "is this a real player row" and handicap-validity rules
/// that were previously duplicated across controllers and repositories.
public static class PlayerRules
{
    // Golfbox placeholder entries come through as "-", "- ", or other short junk.
    public const int MinNameLength = 4;

    public const decimal MinHandicap = -10m;
    public const decimal MaxHandicap = 54m;

    /// EF-translatable version, for use inside database queries.
    public static readonly Expression<Func<PennantMatch, bool>> HasRealPlayerName =
        m => !string.IsNullOrWhiteSpace(m.PlayerName) &&
             !m.PlayerName.StartsWith("-") &&
             m.PlayerName.Length >= MinNameLength;

    public static bool IsRealPlayerName(string? name) =>
        !string.IsNullOrWhiteSpace(name) &&
        !name.StartsWith('-') &&
        name.Length >= MinNameLength;

    /// A handicap string is usable when it parses and falls in the WHS range.
    public static bool TryParseHandicap(string? raw, out decimal handicap)
    {
        if (decimal.TryParse(raw, out handicap))
            return handicap >= MinHandicap && handicap <= MaxHandicap;
        return false;
    }
}
