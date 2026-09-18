using System.Text.RegularExpressions;

namespace SAPennant.API.Domain;

public static class PennantRounds
{
    private static readonly Regex RoundNumber = new(@"\d+", RegexOptions.Compiled);

    /// Orders rounds for display: numbered rounds sort by their number and the
    /// finals sort to the end. Anything unrecognised sorts first.
    ///
    /// Note "Quarter Final" — which GetRoundName can emit — has no digits and
    /// so sorts to the front rather than just before "Semi Final". That is the
    /// existing behaviour, preserved here deliberately; changing it would
    /// reorder rounds already on display.
    public static int SortKey(string round)
    {
        if (round == "Final") return 999;
        if (round == "Semi Final") return 998;
        var match = RoundNumber.Match(round);
        return match.Success ? int.Parse(match.Value) : 0;
    }
}
