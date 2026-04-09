using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TelemetryClient _telemetry;

    public SearchController(AppDbContext db, TelemetryClient telemetry)
    {
        _db = db;
        _telemetry = telemetry;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? source = "search")
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters" });

        var searchTerm = q.Trim().ToLower();

        _telemetry.TrackEvent("PlayerSearch", new Dictionary<string, string>
        {
            { "query", searchTerm },
            { "source", source ?? "search" }
        });

        var results = await _db.PennantMatches
            .Where(m =>
                (EF.Functions.Like(m.PlayerName.ToLower(), $"{searchTerm}%") ||
                EF.Functions.Like(m.PlayerName.ToLower(), $"% {searchTerm}%")) &&
                !m.PlayerName.StartsWith("-") &&
                m.PlayerName.Length > 3)
            .ToListAsync();

        return Ok(results.OrderByDescending(m => m.SortDate));
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new List<string>());

        var searchTerm = q.Trim().ToLower();

        var names = await _db.PennantMatches
            .Where(m =>
                (EF.Functions.Like(m.PlayerName.ToLower(), $"{searchTerm}%") ||
                EF.Functions.Like(m.PlayerName.ToLower(), $"% {searchTerm}%")) &&
                !m.PlayerName.StartsWith("-") &&
                m.PlayerName.Length > 3)
            .Select(m => m.PlayerName)
            .Distinct()
            .ToListAsync();

        var filtered = names
            .Where(n => n.Split(' ')
                .Any(word => word.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(n => n)
            .Take(10)
            .ToList();

        return Ok(filtered);
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

        var query = _db.PennantMatches.AsQueryable();

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);

        if (!string.IsNullOrWhiteSpace(division))
        {
            var d = division.ToLower();
            if (d == "men's")
                query = query.Where(m => m.Division.ToLower().Contains("men's") && !m.Division.ToLower().Contains("women's"));
            else if (d == "women's")
                query = query.Where(m => m.Division.ToLower().Contains("women's"));
            else if (d == "junior")
                query = query.Where(m => m.Division.ToLower().Contains("junior"));
        }

        if (!string.IsNullOrWhiteSpace(pool))
            query = query.Where(m => m.Pool == pool);

        var matches = await query.ToListAsync();

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
            //.Take(100)
            .ToList();

        return Ok(leaderboard);
    }

    private static int ExtractMargin(string? result)
    {
        if (string.IsNullOrEmpty(result)) return 0;
        // Handle X&Y format
        var match = System.Text.RegularExpressions.Regex.Match(result, @"^(\d+)&");
        if (match.Success) return int.Parse(match.Groups[1].Value);
        // Handle X Hole format
        match = System.Text.RegularExpressions.Regex.Match(result, @"^(\d+) Hole");
        if (match.Success) return int.Parse(match.Groups[1].Value);
        return 0;
    }

    [HttpGet("filters")]
    public async Task<IActionResult> Filters()
    {
        var years = await _db.PennantMatches
            .Select(m => m.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        var pools = await _db.PennantMatches
            .Select(m => m.Pool)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        return Ok(new
        {
            years,
            pools,
            divisions = new[] { "Men's", "Women's", "Junior" }
        });
    }

    [HttpGet("last-updated")]
    public async Task<IActionResult> LastUpdated()
    {
        var last = await _db.SyncLogs
            .OrderByDescending(s => s.SyncedAt)
            .FirstOrDefaultAsync();

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

        var clubs = await _db.PennantMatches
            .Where(m =>
                m.PlayerClub != null &&
                m.PlayerClub.Length > 0 &&
                EF.Functions.Like(m.PlayerClub.ToLower(), $"%{q.Trim().ToLower()}%"))
            .Select(m => m.PlayerClub!)
            .Distinct()
            .OrderBy(c => c)
            .Take(15)
            .ToListAsync();

        return Ok(clubs);
    }

    [HttpGet("clubs/{clubName}/players")]
    public async Task<IActionResult> ClubPlayers(string clubName, [FromQuery] int minGames = 1)
    {
        _telemetry.TrackEvent("ClubSearch", new Dictionary<string, string>
        {
            { "club", clubName }
        });

        var matches = await _db.PennantMatches
            .Where(m => m.PlayerClub == clubName &&
                   !string.IsNullOrWhiteSpace(m.PlayerName) &&
                   !m.PlayerName.StartsWith("-") &&
                   m.PlayerName.Length > 3)
            .ToListAsync();

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
}