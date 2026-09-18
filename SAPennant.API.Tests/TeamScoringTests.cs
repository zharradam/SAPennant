using SAPennant.API.Domain;
using SAPennant.API.Models;

namespace SAPennant.API.Tests;

public class TeamScoringTests
{
    /// Builds the mirrored pair the sync writes for one rubber: the home
    /// player's row followed by the away player's row.
    private static IEnumerable<PennantMatch> Rubber(int firstId, bool? homeWon) => new[]
    {
        new PennantMatch { Id = firstId,     PlayerName = "Home", PlayerWon = homeWon },
        new PennantMatch { Id = firstId + 1, PlayerName = "Away", PlayerWon = homeWon is null ? null : !homeWon },
    };

    private static List<PennantMatch> Fixture(params bool?[] homeResults) =>
        homeResults.SelectMany((r, i) => Rubber(i * 2 + 1, r)).ToList();

    [Fact]
    public void DedupeKeepsOneRowPerRubber()
    {
        var deduped = TeamScoring.Dedupe(Fixture(true, false, null));

        Assert.Equal(3, deduped.Count);
        Assert.All(deduped, m => Assert.Equal("Home", m.PlayerName));
    }

    [Fact]
    public void DedupeIsIndependentOfInputOrder()
    {
        var shuffled = Fixture(true, false, null).OrderByDescending(m => m.Id);

        Assert.Equal(
            new[] { 1, 3, 5 },
            TeamScoring.Dedupe(shuffled).Select(m => m.Id));
    }

    [Fact]
    public void PointsScoreWinsAsOneAndHalvesAsAHalf()
    {
        // home wins two, loses one, halves one
        var deduped = TeamScoring.Dedupe(Fixture(true, true, false, null));

        Assert.Equal(2.5, TeamScoring.HomePoints(deduped));
        Assert.Equal(1.5, TeamScoring.AwayPoints(deduped));
    }

    [Fact]
    public void PointsAlwaysSumToTheNumberOfRubbers()
    {
        var deduped = TeamScoring.Dedupe(Fixture(true, false, null, true, null));

        Assert.Equal(5.0, TeamScoring.HomePoints(deduped) + TeamScoring.AwayPoints(deduped));
    }

    [Fact]
    public void EmptyFixtureScoresNothing()
    {
        var deduped = TeamScoring.Dedupe(Array.Empty<PennantMatch>());

        Assert.Empty(deduped);
        Assert.Equal(0.0, TeamScoring.HomePoints(deduped));
        Assert.Equal(0.0, TeamScoring.AwayPoints(deduped));
    }
}

public class PennantRoundsTests
{
    [Theory]
    [InlineData("Round 1", 1)]
    [InlineData("Round 7", 7)]
    [InlineData("Round 12", 12)]
    [InlineData("Semi Final", 998)]
    [InlineData("Final", 999)]
    public void SortKeyOrdersRoundsThenFinals(string round, int expected)
    {
        Assert.Equal(expected, PennantRounds.SortKey(round));
    }

    [Fact]
    public void RoundsSortIntoPlayingOrder()
    {
        var rounds = new[] { "Final", "Round 2", "Semi Final", "Round 10", "Round 1" };

        Assert.Equal(
            new[] { "Round 1", "Round 2", "Round 10", "Semi Final", "Final" },
            rounds.OrderBy(PennantRounds.SortKey));
    }

    // Documents existing behaviour rather than endorsing it: "Quarter Final"
    // carries no digits, so it sorts to the front. See PennantRounds.SortKey.
    [Fact]
    public void QuarterFinalHasNoNumberAndSortsFirst()
    {
        Assert.Equal(0, PennantRounds.SortKey("Quarter Final"));
    }
}
