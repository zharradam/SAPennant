using SAPennant.API.Models;

namespace SAPennant.API.Domain;

/// Golfbox fixtures are stored twice — once from the home player's point of
/// view and once from the away player's (see GolfboxSyncService.ParseTeamMatch).
/// Every team-level aggregate has to drop the mirrored half first.
public static class TeamScoring
{
    /// Keeps the home-side row of each mirrored pair. Rows are inserted
    /// home-then-away, so every second row in Id order is a home row.
    public static List<PennantMatch> Dedupe(IEnumerable<PennantMatch> matches) =>
        matches.OrderBy(m => m.Id).Where((_, i) => i % 2 == 0).ToList();

    /// A win scores 1 point and a halved match 0.5, read from the home side.
    public static double HomePoints(IEnumerable<PennantMatch> deduped) =>
        deduped.Sum(m => m.PlayerWon switch { true => 1.0, null => 0.5, false => 0.0 });

    public static double AwayPoints(IEnumerable<PennantMatch> deduped) =>
        deduped.Sum(m => m.PlayerWon switch { false => 1.0, null => 0.5, true => 0.0 });
}
