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

    private static List<(string Division, string Pool, long CompetitionId)> Enumerate(string json) =>
        GolfboxSyncService.EnumeratePlayablePools(JsonSerializer.Deserialize<JsonElement>(json)).ToList();

    [Fact]
    public void SkipsPoolsWithoutACompetitionId()
    {
        var pools = Enumerate(SeniorPennantOverview);

        Assert.Equal(2, pools.Count);
        Assert.All(pools, p => Assert.Equal("Senior Pennant", p.Division));
        Assert.Equal(new[] { "Div 2", "Div 3" }, pools.Select(p => p.Pool));
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
        Assert.Equal("Div 2", pool.Pool);
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
