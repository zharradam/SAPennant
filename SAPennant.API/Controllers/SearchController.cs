using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly IPennantMatchRepository _matches;
    private readonly ISyncLogRepository _syncLogs;
    private readonly TelemetryClient _telemetry;

    public SearchController(
        IPennantMatchRepository matches,
        ISyncLogRepository syncLogs,
        TelemetryClient telemetry)
    {
        _matches = matches;
        _syncLogs = syncLogs;
        _telemetry = telemetry;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? source = "search")
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters" });

        var searchTerm = q.Trim().ToLower();

        _telemetry.TrackEvent("PlayerSearch", new Dictionary<string, string>
        {
            { "query", searchTerm },
            { "source", source ?? "search" }
        });

        var results = await _matches.SearchByPlayerNameAsync(searchTerm);
        return Ok(results.OrderByDescending(m => m.SortDate));
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<string>());

        var suggestions = await _matches.GetPlayerSuggestionsAsync(q);
        return Ok(suggestions);
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard(
        [FromQuery] int? year,
        [FromQuery] string? division,
        [FromQuery] string? pool,
        [FromQuery] int minGames = 5)
    {
        _telemetry.TrackEvent("LeaderboardView", new Dictionary<string, string>
        {
            { "year", year?.ToString() ?? "all" },
            { "division", division ?? "all" },
            { "pool", pool ?? "all" }
        });

        bool? isSenior = division?.ToLower() == "senior" ? true : null;

        var matches = await _matches.GetLeaderboardDataAsync(year, division, pool, isSenior);

        // Division filtering in memory
        if (!string.IsNullOrWhiteSpace(division))
        {
            var d = division.ToLower();
            if (d == "men's")
                matches = matches.Where(m =>
                    m.Division.ToLower().Contains("men's") &&
                    !m.Division.ToLower().Contains("women's"));
            else if (d == "women's")
                matches = matches.Where(m => m.Division.ToLower().Contains("women's"));
            else if (d == "junior")
                matches = matches.Where(m => m.Division.ToLower().Contains("junior"));
            else if (d == "senior")
                matches = matches.Where(m => m.IsSenior);
        }

        var leaderboard = matches
            .GroupBy(m => m.PlayerName)
            .Where(g =>
                !string.IsNullOrWhiteSpace(g.Key) &&
                !g.Key.StartsWith("-") &&
                !g.Key.StartsWith("- ") &&
                g.Key.Length > 3)
            .Select(g =>
            {
                var all = g.ToList();
                var singles = all.Where(m => m.Format == "single").ToList();
                var foursomes = all.Where(m => m.Format == "foursome").ToList();
                var finals = all.Where(m => m.IsFinals).ToList();

                var wins = all.Count(m => m.PlayerWon == true);
                var losses = all.Count(m => m.PlayerWon == false);
                var halved = all.Count(m => m.PlayerWon == null);
                var played = all.Count;
                var winRate = played > 0 ? Math.Round((double)wins / played * 100, 1) : 0;

                var finalsWins = finals.Count(m => m.PlayerWon == true);
                var finalsPlayed = finals.Count;

                var bestResult = all
                    .Where(m => m.PlayerWon == true && m.Result != null)
                    .OrderByDescending(m => ExtractMargin(m.Result))
                    .FirstOrDefault()?.Result ?? "—";

                var club = g
                    .GroupBy(m => m.PlayerClub)
                    .OrderByDescending(c => c.Count())
                    .First().Key;

                var division2 = g
                    .GroupBy(m => m.Division)
                    .OrderByDescending(c => c.Count())
                    .First().Key;

                var pool2 = g
                    .GroupBy(m => m.Pool)
                    .OrderByDescending(c => c.Count())
                    .First().Key;

                return new
                {
                    playerName = g.Key,
                    club,
                    division = division2,
                    pool = pool2,
                    played,
                    wins,
                    losses,
                    halved,
                    winRate,
                    finalsWins,
                    finalsPlayed,
                    finalsRecord = finalsPlayed > 0 ? $"{finalsWins}/{finalsPlayed}" : "—",
                    bestResult,
                    singlesPlayed = singles.Count,
                    singlesWins = singles.Count(m => m.PlayerWon == true),
                    foursomesPlayed = foursomes.Count,
                    foursomesWins = foursomes.Count(m => m.PlayerWon == true),
                };
            })
            .Where(p => p.played >= minGames)
            .OrderByDescending(p => p.winRate)
            .ThenByDescending(p => p.played)
            .ToList();

        return Ok(leaderboard);
    }

    private static int ExtractMargin(string? result)
    {
        if (string.IsNullOrEmpty(result)) return 0;
        var match = System.Text.RegularExpressions.Regex.Match(result, @"^(\d+)&");
        if (match.Success) return int.Parse(match.Groups[1].Value);
        match = System.Text.RegularExpressions.Regex.Match(result, @"^(\d+) Hole");
        if (match.Success) return int.Parse(match.Groups[1].Value);
        return 0;
    }

    [HttpGet("filters")]
    public async Task<IActionResult> Filters([FromQuery] int? year = null)
    {
        var years = await _matches.GetDistinctYearsAsync();

        var poolDivisions = await _matches.GetPoolDivisionsAsync(year);

        var hasSenior = poolDivisions.Any(p => p.IsSenior);

        var divisionPools = new Dictionary<string, List<string>>
        {
            ["Men's"] = poolDivisions.Where(p => !p.IsSenior && (
                p.Pool.Contains("Simpson") || p.Pool.Contains("Bonnar") ||
                p.Pool.Contains("Men's")))
                .Select(p => p.Pool).Distinct().OrderBy(p => p).ToList(),

            ["Women's"] = poolDivisions.Where(p => !p.IsSenior && (
                p.Pool.Contains("Women") || p.Pool.Contains("Pike") ||
                p.Pool.Contains("Sanderson") || p.Pool.Contains("Cleek")))
                .Select(p => p.Pool).Distinct().OrderBy(p => p).ToList(),

            ["Junior"] = poolDivisions.Where(p => !p.IsSenior && (
                p.Pool.Contains("Junior") || p.Pool.Contains("Sharp")))
                .Select(p => p.Pool).Distinct().OrderBy(p => p).ToList(),
        };

        if (hasSenior)
        {
            divisionPools["Senior"] = poolDivisions.Where(p => p.IsSenior)
                .Select(p => p.Pool).Distinct().OrderBy(p => p).ToList();
        }

        var allPools = poolDivisions
            .Select(p => p.Pool)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        return Ok(new
        {
            years,
            pools = allPools,
            divisions = divisionPools.Keys.ToList(),
            divisionPools
        });
    }

    [HttpGet("last-updated")]
    public async Task<IActionResult> LastUpdated()
    {
        var last = await _syncLogs.GetLatestAsync();
        var localTime = last?.SyncedAt.ToLocalTime();

        return Ok(new
        {
            lastUpdated = localTime,
            display = localTime != null
                ? localTime.Value.ToString("dd MMM yyyy h:mm tt")
                : "Never"
        });
    }

    [HttpGet("clubs/search")]
    public async Task<IActionResult> ClubSuggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<string>());

        var clubs = await _matches.GetClubSuggestionsAsync(q);
        return Ok(clubs);
    }

    [HttpGet("clubs/players")]
    public async Task<IActionResult> ClubPlayers(
        [FromQuery] string clubName,
        [FromQuery] int minGames = 1)
    {
        _telemetry.TrackEvent("ClubSearch", new Dictionary<string, string>
        {
            { "club", clubName }
        });

        var matches = await _matches.GetByClubAsync(clubName);

        var players = matches
            .GroupBy(m => new { m.PlayerName, m.Year, m.Pool })
            .Select(g =>
            {
                var all = g.ToList();
                var wins = all.Count(m => m.PlayerWon == true);
                var losses = all.Count(m => m.PlayerWon == false);
                var halved = all.Count(m => m.PlayerWon == null);
                var played = all.Count;
                var winRate = played > 0 ? Math.Round((double)wins / played * 100, 1) : 0;

                return new
                {
                    playerName = g.Key.PlayerName,
                    club = clubName,
                    year = g.Key.Year,
                    pool = g.Key.Pool,
                    played,
                    wins,
                    losses,
                    halved,
                    winRate,
                };
            })
            .Where(p => p.played >= minGames)
            .OrderByDescending(p => p.winRate)
            .ThenByDescending(p => p.played)
            .ToList();

        return Ok(players);
    }

    [HttpGet("handicap-leaderboard")]
    public async Task<IActionResult> HandicapLeaderboard()
    {
        var matches = await _matches.GetHandicapDataAsync();

        var players = matches
            .Where(m => decimal.TryParse(m.PlayerHandicap, out var h) && h >= -10 && h <= 54)
            .GroupBy(m => m.PlayerName)
            .Where(g =>
                !string.IsNullOrWhiteSpace(g.Key) &&
                !g.Key.StartsWith("-") &&
                g.Key.Length > 3)
            .Select(g =>
            {
                var handicaps = g
                    .Where(m => decimal.TryParse(m.PlayerHandicap, out var h) && h >= -10 && h <= 54)
                    .OrderBy(m => m.SortDate)
                    .ToList();

                var lowestHcp = handicaps.Min(m => decimal.Parse(m.PlayerHandicap!));
                var latestHcp = handicaps.Last();

                var club = g
                    .GroupBy(m => m.PlayerClub)
                    .OrderByDescending(c => c.Count())
                    .First().Key;

                return new
                {
                    playerName = g.Key,
                    club,
                    lowestHandicap = lowestHcp,
                    currentHandicap = decimal.Parse(latestHcp.PlayerHandicap!),
                    dataPoints = handicaps.Count
                };
            })
            .Where(p => p.dataPoints >= 3)
            .OrderBy(p => p.lowestHandicap)
            .ToList();

        return Ok(players);
    }

    [HttpGet("handicap-history/{playerName}")]
    public async Task<IActionResult> HandicapHistory(string playerName)
    {
        var matches = await _matches.GetHandicapHistoryAsync(playerName);

        var history = matches
            .Where(m => decimal.TryParse(m.PlayerHandicap, out var h) && h >= -10 && h <= 54)
            .Select(m => new
            {
                date = m.Date,
                sortDate = m.SortDate,
                handicap = decimal.Parse(m.PlayerHandicap!),
                opponent = m.OpponentName,
                result = m.PlayerWon == true ? "Win" : m.PlayerWon == false ? "Loss" : "Halved",
                pool = m.Pool,
                year = m.Year
            })
            .OrderBy(m => m.sortDate)
            .ToList();

        return Ok(history);
    }
}