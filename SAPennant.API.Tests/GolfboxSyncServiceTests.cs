using System.Text.Json;
using SAPennant.API.Services;

namespace SAPennant.API.Tests;

public class ExtractJsonTests
{
    [Fact]
    public void StripsJsonpWrapperAndNormalisesBooleans()
    {
        var response = "cb1a2b3c({\"IsBye\":!1,\"IsSettled\":!0,\"Name\":\"Grange\"});";
        var json = GolfboxSyncService.ExtractJson(response);

        Assert.NotNull(json);
        var doc = JsonSerializer.Deserialize<JsonElement>(json!);
        Assert.False(doc.GetProperty("IsBye").GetBoolean());
        Assert.True(doc.GetProperty("IsSettled").GetBoolean());
        Assert.Equal("Grange", doc.GetProperty("Name").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("no braces here")]
    [InlineData("}{")]
    public void ReturnsNullWhenNoJsonPresent(string response)
    {
        Assert.Null(GolfboxSyncService.ExtractJson(response));
    }
}

public class ParseStartTimeTests
{
    [Fact]
    public void ParsesGolfboxTimestamp()
    {
        var (display, date) = GolfboxSyncService.ParseStartTime("20260503T081500");
        Assert.Equal("03 May 2026", display);
        Assert.Equal(new DateOnly(2026, 5, 3), date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("99999999")]
    public void ReturnsEmptyForBadInput(string input)
    {
        var (display, date) = GolfboxSyncService.ParseStartTime(input);
        Assert.Equal("", display);
        Assert.Null(date);
    }
}

public class RoundNameTests
{
    [Theory]
    [InlineData(3, false, 3, "Round 3")]
    [InlineData(3, true, 3, "Final")]
    [InlineData(2, true, 3, "Semi Final")]
    [InlineData(1, true, 3, "Quarter Final")]
    [InlineData(1, true, 5, "Round 1")]
    public void NamesRoundsByPosition(int round, bool isFinals, int total, string expected)
    {
        Assert.Equal(expected, GolfboxSyncService.GetRoundName(round, isFinals, total));
    }
}

public class TitleCaseTests
{
    [Theory]
    [InlineData("JOHN SMITH", "John Smith")]
    [InlineData("mary o'brien", "Mary O'brien")]
    [InlineData("", "")]
    public void NormalisesCasing(string input, string expected)
    {
        Assert.Equal(expected, GolfboxSyncService.ToTitleCase(input));
    }
}

public class ParseTeamMatchTests
{
    // Shape mirrors scores.golfbox.dk TeamMatchHandler/GetTeamMatch after
    // JSONP unwrapping: one settled singles rubber and one halved foursomes.
    private const string Fixture = """
    {
      "TeamMatch": {
        "Home": { "Name": "Grange " },
        "Away": { "Name": "Kooyonga" },
        "InterclubHostingClub": "Grange",
        "Matches": {
          "1001": {
            "Result": "5&4",
            "Format": "single",
            "Teams": [
              {
                "IsLead": true,
                "Entries": [
                  { "FirstName": "JOHN", "LastName": "SMITH", "ClubName": "Grange", "HCP": "2.1" }
                ]
              },
              {
                "IsLead": false,
                "Entries": [
                  { "FirstName": "bob", "LastName": "JONES", "ClubName": "Kooyonga", "HCP": "3.4" }
                ]
              }
            ]
          },
          "1002": {
            "Result": "A/S",
            "Format": "foursome",
            "Teams": [
              {
                "IsLead": false,
                "Entries": [
                  { "FirstName": "Alice", "LastName": "One", "ClubName": "Grange", "HCP": "5.0" },
                  { "FirstName": "Beth", "LastName": "Two", "ClubName": "Grange", "HCP": "6.0" }
                ]
              },
              {
                "IsLead": false,
                "Entries": [
                  { "FirstName": "Cara", "LastName": "Three", "ClubName": "Kooyonga", "HCP": "7.0" },
                  { "FirstName": "Dana", "LastName": "Four", "ClubName": "Kooyonga", "HCP": "8.0" }
                ]
              }
            ]
          }
        }
      }
    }
    """;

    private static List<SAPennant.API.Models.PennantMatch> Parse() =>
        GolfboxSyncService.ParseTeamMatch(
            JsonSerializer.Deserialize<JsonElement>(Fixture),
            year: 2026, isFinals: false, isSenior: false,
            division: "Men's", poolName: "Simpson Cup",
            roundNumber: 3, startTime: "20260503T081500", totalRounds: 7);

    [Fact]
    public void CreatesOneRowPerPlayerPerRubber()
    {
        // 2 rubbers × home + away perspective
        Assert.Equal(4, Parse().Count);
    }

    [Fact]
    public void MapsSinglesWinnerAndLoser()
    {
        var rows = Parse();
        var winner = rows.Single(m => m.PlayerName == "John Smith");
        var loser = rows.Single(m => m.PlayerName == "Bob Jones");

        Assert.True(winner.PlayerWon);
        Assert.False(loser.PlayerWon);
        Assert.Equal("5&4", winner.Result);
        Assert.Equal("Bob Jones", winner.OpponentName);
        Assert.Equal("Grange", winner.PlayerClub);
        Assert.Equal("Kooyonga", winner.OpponentClub);
        Assert.Equal("2.1", winner.PlayerHandicap);
        Assert.Equal("3.4", winner.OpponentHandicap);
    }

    [Fact]
    public void HalvedFoursomeHasNullPlayerWonAndJoinedNames()
    {
        var rows = Parse();
        var homePair = rows.Single(m => m.PlayerName == "Alice One & Beth Two");

        Assert.Null(homePair.PlayerWon);
        Assert.Equal("Cara Three & Dana Four", homePair.OpponentName);
        Assert.Equal("5.0 & 6.0", homePair.PlayerHandicap);
        Assert.Equal("foursome", homePair.Format);
    }

    [Fact]
    public void SetsMatchContextOnEveryRow()
    {
        foreach (var row in Parse())
        {
            Assert.Equal(2026, row.Year);
            Assert.Equal("Simpson Cup", row.Pool);
            Assert.Equal("Round 3", row.Round);
            Assert.Equal("03 May 2026", row.Date);
            Assert.Equal(new DateOnly(2026, 5, 3), row.MatchDate);
            Assert.Equal("Grange", row.HomeClub);
            Assert.Equal("Kooyonga", row.AwayClub);
            Assert.Equal("Grange", row.Venue);
        }
    }
}
