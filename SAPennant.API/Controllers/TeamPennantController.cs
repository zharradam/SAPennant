using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamPennantController : ControllerBase
{
    private readonly IPennantMatchRepository _matches;
    private readonly IRoundStatusRepository _roundStatuses;
    private readonly ILogger<TeamPennantController> _logger;
    private readonly IPoolFinalistConfigRepository _poolFinalistConfigs;

    public TeamPennantController(
        IPennantMatchRepository matches,
        IRoundStatusRepository roundStatuses,
        IPoolFinalistConfigRepository poolFinalistConfigs,
        ILogger<TeamPennantController> logger)
    {
        _matches = matches;
        _roundStatuses = roundStatuses;
        _poolFinalistConfigs = poolFinalistConfigs;
        _logger = logger;
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int year, [FromQuery] string pool)
    {
        var matches = await _matches.GetTeamMatchesAsync(year, pool);

        var teamScores = matches
            .GroupBy(m => new { m.Round, m.HomeClub, m.AwayClub })
            .Select(g =>
            {
                var deduped = g.OrderBy(m => m.Id)
                               .Where((m, i) => i % 2 == 0)
                               .ToList();
                return new
                {
                    g.Key.Round,
                    g.Key.HomeClub,
                    g.Key.AwayClub,
                    HomePoints = deduped.Sum(m => m.PlayerWon == true ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0),
                    AwayPoints = deduped.Sum(m => m.PlayerWon == false ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0),
                };
            })
            .ToList();

        var clubs = teamScores
            .SelectMany(m => new[] { m.HomeClub, m.AwayClub })
            .Distinct();

        var leaderboard = clubs.Select(club =>
        {
            var allMatchPoints = teamScores
                .Where(m => m.HomeClub == club)
                .Select(m => (mine: m.HomePoints, theirs: m.AwayPoints))
                .Concat(teamScores
                    .Where(m => m.AwayClub == club)
                    .Select(m => (mine: m.AwayPoints, theirs: m.HomePoints)))
                .ToList();

            var won = allMatchPoints.Count(m => m.mine > m.theirs);
            var lost = allMatchPoints.Count(m => m.mine < m.theirs);
            var tied = allMatchPoints.Count(m => m.mine == m.theirs);
            var totalFor = allMatchPoints.Sum(m => m.mine);
            var totalAgainst = allMatchPoints.Sum(m => m.theirs);
            var pts = won + (tied * 0.5);

            return new
            {
                Club = club,
                Played = allMatchPoints.Count,
                Won = won,
                Lost = lost,
                Tied = tied,
                ScoreFor = totalFor,
                ScoreAgainst = totalAgainst,
                Pts = pts
            };
        })
        .OrderByDescending(c => c.Pts)
        .ThenByDescending(c => c.ScoreFor - c.ScoreAgainst)
        .ToList()
        .Select((c, i) => new
        {
            Position = i + 1,
            c.Club,
            c.Played,
            c.Won,
            c.Lost,
            c.Tied,
            c.ScoreFor,
            c.ScoreAgainst,
            c.Pts
        });

        return Ok(leaderboard);
    }

    [HttpGet("rounds")]
    public async Task<IActionResult> GetRound(
        [FromQuery] int year, [FromQuery] string pool, [FromQuery] string round)
    {
        var matches = await _matches.GetByYearAndPoolAndRoundAsync(year, pool, round);

        var teamMatches = matches
            .GroupBy(m => new { m.HomeClub, m.AwayClub })
            .Select(g =>
            {
                var deduped = g.OrderBy(m => m.Id)
                               .Where((m, i) => i % 2 == 0)
                               .ToList();
                return new
                {
                    g.Key.HomeClub,
                    g.Key.AwayClub,
                    Venue = deduped.FirstOrDefault()?.Venue,
                    HomePoints = deduped.Sum(m => m.PlayerWon == true ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0),
                    AwayPoints = deduped.Sum(m => m.PlayerWon == false ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0),
                };
            });

        return Ok(teamMatches);
    }

    [HttpGet("match")]
    public async Task<IActionResult> GetMatch(
        [FromQuery] int year, [FromQuery] string pool,
        [FromQuery] string round, [FromQuery] string home, [FromQuery] string away)
    {
        var matches = await _matches.GetMatchAsync(year, pool, round, home, away);

        var deduped = matches
            .OrderBy(m => m.Id)
            .Where((m, i) => i % 2 == 0)
            .Select(m => new
            {
                m.PlayerName,
                m.OpponentName,
                m.PlayerClub,
                m.OpponentClub,
                m.Result,
                m.PlayerWon
            })
            .ToList();

        return Ok(deduped);
    }

    [HttpGet("rounds-list")]
    public async Task<IActionResult> GetRoundsList([FromQuery] int year, [FromQuery] string pool)
    {
        var rounds = await _matches.GetRoundsListAsync(year, pool);

        var ordered = rounds.OrderBy(r =>
        {
            if (r == "Final") return 999;
            if (r == "Semi Final") return 998;
            var match = System.Text.RegularExpressions.Regex.Match(r, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        });

        return Ok(ordered);
    }

    [HttpGet("champion")]
    public async Task<IActionResult> GetChampion([FromQuery] int year, [FromQuery] string pool)
    {
        var matches = await _matches.GetByYearAndPoolAndRoundAsync(year, pool, "Final");
        var matchList = matches.ToList();

        if (!matchList.Any()) return Ok(null);

        var final = matchList.First();

        var deduped = matchList
            .OrderBy(m => m.Id)
            .Where((m, i) => i % 2 == 0)
            .ToList();

        var homePoints = deduped.Sum(m => m.PlayerWon == true ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0);
        var awayPoints = deduped.Sum(m => m.PlayerWon == false ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0);

        return Ok(new
        {
            Champion = homePoints > awayPoints ? final.HomeClub : final.AwayClub,
            RunnerUp = homePoints > awayPoints ? final.AwayClub : final.HomeClub,
            Score = homePoints > awayPoints
                ? $"{(homePoints % 1 == 0 ? homePoints.ToString("0") : homePoints.ToString("0.0"))} - {(awayPoints % 1 == 0 ? awayPoints.ToString("0") : awayPoints.ToString("0.0"))}"
                : $"{(awayPoints % 1 == 0 ? awayPoints.ToString("0") : awayPoints.ToString("0.0"))} - {(homePoints % 1 == 0 ? homePoints.ToString("0") : homePoints.ToString("0.0"))}"
        });
    }

    [HttpGet("club-rounds")]
    public async Task<IActionResult> GetClubRounds(
        [FromQuery] int year, [FromQuery] string pool, [FromQuery] string club)
    {
        var matches = await _matches.GetClubMatchesAsync(year, pool, club);

        var rounds = matches
            .GroupBy(m => new { m.Round, m.HomeClub, m.AwayClub })
            .Select(g =>
            {
                var deduped = g.OrderBy(m => m.Id)
                               .Where((m, i) => i % 2 == 0)
                               .ToList();

                var homePoints = deduped.Sum(m => m.PlayerWon == true ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0);
                var awayPoints = deduped.Sum(m => m.PlayerWon == false ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0);
                var isHome = g.Key.HomeClub == club;

                return new
                {
                    g.Key.Round,
                    Opponent = isHome ? g.Key.AwayClub : g.Key.HomeClub,
                    IsHome = isHome,
                    ClubPoints = isHome ? homePoints : awayPoints,
                    OpponentPoints = isHome ? awayPoints : homePoints,
                };
            })
            .OrderBy(r =>
            {
                if (r.Round == "Final") return 999;
                if (r.Round == "Semi Final") return 998;
                var m = System.Text.RegularExpressions.Regex.Match(r.Round, @"\d+");
                return m.Success ? int.Parse(m.Value) : 0;
            })
            .ToList();

        return Ok(rounds);
    }

    [HttpGet("active-round")]
    public async Task<IActionResult> GetActiveRound([FromQuery] int year, [FromQuery] string pool)
    {
        var all = await _roundStatuses.GetByYearAsync(year);
        var poolStatuses = all.Where(r => r.Pool == pool).ToList();

        /** Don't need too much logging here, but keeping this around for now in case we need to troubleshoot any issues with the active round logic
        _logger.LogInformation("Found {Count} round statuses for {Year} {Pool}", poolStatuses.Count, year, pool);
        foreach (var r in poolStatuses)
            _logger.LogInformation("Round={Round} IsSettled={IsSettled}", r.Round, r.IsSettled);
        */
        var activeRound = poolStatuses.FirstOrDefault(r => !r.IsSettled)?.Round;

        return Ok(new { activeRound });
    }

    [HttpGet("finalists")]
    public async Task<IActionResult> GetFinalists([FromQuery] int year, [FromQuery] string pool)
    {
        var currentYear = DateTime.UtcNow.Year;

        if (year < currentYear)
        {
            // Previous seasons — derive from actual finals data in the database
            var finalsMatches = await _matches.GetByYearAndPoolAsync(year, pool);
            var actualFinalists = finalsMatches
                .Where(m => m.IsFinals)
                .SelectMany(m => new[] { m.HomeClub, m.AwayClub })
                .Distinct()
                .ToList();

            return Ok(new { finalists = actualFinalists, source = "actual" });
        }

        // Current season — always use FinalistCount config to mark the cutoff line
        var config = await _poolFinalistConfigs.GetAsync(pool);
        if (config == null)
            return Ok(new { finalists = new List<string>(), source = "none" });

        var allMatches = await _matches.GetTeamMatchesAsync(year, pool);
        var teamScores = allMatches
            .GroupBy(m => new { m.Round, m.HomeClub, m.AwayClub })
            .Select(g =>
            {
                var deduped = g.OrderBy(m => m.Id).Where((m, i) => i % 2 == 0).ToList();
                return new
                {
                    g.Key.HomeClub,
                    g.Key.AwayClub,
                    HomePoints = deduped.Sum(m => m.PlayerWon == true ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0),
                    AwayPoints = deduped.Sum(m => m.PlayerWon == false ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0),
                };
            }).ToList();

        var clubs = teamScores
            .SelectMany(m => new[] { m.HomeClub, m.AwayClub })
            .Distinct();

        var projectedFinalists = clubs.Select(club =>
        {
            var points = teamScores
                .Where(m => m.HomeClub == club)
                .Select(m => (mine: m.HomePoints, theirs: m.AwayPoints))
                .Concat(teamScores
                    .Where(m => m.AwayClub == club)
                    .Select(m => (mine: m.AwayPoints, theirs: m.HomePoints)))
                .ToList();

            return new
            {
                Club = club,
                Pts = points.Sum(p => p.mine > p.theirs ? 1.0 : p.mine == p.theirs ? 0.5 : 0.0),
                ScoreFor = points.Sum(p => p.mine)
            };
        })
        .OrderByDescending(c => c.Pts)
        .ThenByDescending(c => c.ScoreFor)
        .Take(config.FinalistCount)
        .Select(c => c.Club)
        .ToList();

        return Ok(new { finalists = projectedFinalists, source = "projected" });
    }
}