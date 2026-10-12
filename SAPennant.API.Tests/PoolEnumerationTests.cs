using System.Text.Json;
using SAPennant.API.Services;

namespace SAPennant.API.Tests;

public class PoolEnumerationTests
{
    // Mirrors the real SA - SENIOR PENNANT (interclub 2335) overview as it
    // looked mid-season 2026: Div 1 undrawn and the whole Grand Final
    // division still pending, so those pools carry a null CompetitionID.
    private const string SeniorPennantOverview = """
    {
      "Tournament": {
        "Name": "SA - SENIOR PENNANT",
        "Divisions": [
          {
            "Name": "Senior Pennant",
            "Pools": [
              { "Name": "Div 1", "CompetitionID": null },
              { "Name": "Div 2", "CompetitionID": 5827923 },
              { "Name": "Div 3", "CompetitionID": 5829066 }
            ]
          },
          {
            "Name": "Grand Final",
            "Pools": [
              { "Name": "Div 1", "CompetitionID": null },
              { "Name": "Div 2", "CompetitionID": null },
              { "Name": "Div 3", "CompetitionID": null }
            ]
          }
        ]
      }
    }
    """;

    private static List<(string Division, string Pool, long CompetitionId)> Enumerate(
        string json, bool isSenior = true) =>
        GolfboxSyncService.EnumeratePlayablePools(JsonSerializer.Deserialize<JsonElement>(json), isSenior).ToList();

    [Fact]
    public void SkipsPoolsWithoutACompetitionId()
    {
        var pools = Enumerate(SeniorPennantOverview);

        Assert.Equal(2, pools.Count);
        Assert.All(pools, p => Assert.Equal("Senior Pennant", p.Division));
        Assert.Equal(new[] { "Senior Div 2", "Senior Div 3" }, pools.Select(p => p.Pool));
        Assert.Equal(new[] { 5827923L, 5829066L }, pools.Select(p => p.CompetitionId));
    }

    [Fact]
    public void TrimsDivisionAndPoolNames()
    {
        var pools = Enumerate("""
        {
          "Tournament": {
            "Divisions": [
              { "Name": "  Senior Pennant  ", "Pools": [ { "Name": " Div 2 ", "CompetitionID": 1 } ] }
            ]
          }
        }
        """);

        var pool = Assert.Single(pools);
        Assert.Equal("Senior Pennant", pool.Division);
        Assert.Equal("Senior Div 2", pool.Pool);
    }

    [Fact]
    public void ReturnsNothingWhenNoPoolIsDrawnYet()
    {
        var pools = Enumerate("""
        {
          "Tournament": {
            "Divisions": [
              { "Name": "Grand Final", "Pools": [ { "Name": "Div 1", "CompetitionID": null } ] }
            ]
          }
        }
        """);

        Assert.Empty(pools);
    }
}

public class RoundEnumerationTests
{
    private static List<GolfboxSyncService.PoolRound>? Rounds(string json, bool isFinals) =>
        GolfboxSyncService.EnumerateRounds(JsonSerializer.Deserialize<JsonElement>(json), isFinals);

    [Fact]
    public void BracketRoundsAreNumberedFromTheirNumberProperty()
    {
        // Mirrors TeamMatchplayBracketHandler: keys are "R1"/"R2", not integers.
        var rounds = Rounds("""
        {
          "IsCompleted": true,
          "Rounds": {
            "R1": { "Number": 1, "TeamMatches": { "TM1": { "IsBye": false }, "TM2": { "IsBye": false } } },
            "R2": { "Number": 2, "TeamMatches": { "TM3": { "IsBye": false } } }
          }
        }
        """, isFinals: true)!;

        Assert.Equal(new[] { "Semi Final", "Final" }, rounds.Select(r => r.Name));
        Assert.Equal(new[] { 2, 1 }, rounds.Select(r => r.TeamMatches.Count));
        Assert.All(rounds, r => Assert.Equal(2, r.TotalRounds));
    }

    [Fact]
    public void FlatTeamMatchesAreGroupedByInterclubRoundNumber()
    {
        var rounds = Rounds("""
        {
          "TeamMatches": {
            "TM1": { "InterclubRoundNumber": 2 },
            "TM2": { "InterclubRoundNumber": 1 },
            "TM3": { "InterclubRoundNumber": 2 }
          }
        }
        """, isFinals: false)!;

        Assert.Equal(new[] { "Round 1", "Round 2" }, rounds.Select(r => r.Name));
        Assert.Equal(new[] { 1, 2 }, rounds.Select(r => r.TeamMatches.Count));
    }

    [Fact]
    public void NeitherShapeReturnsNull()
    {
        Assert.Null(Rounds("""{ "Name": "Simpson Cup" }""", isFinals: false));
    }
}
