using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Repositories.Interfaces;
using SAPennant.API.Services;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamPennantController : ControllerBase
{
    private readonly IPennantMatchRepository _matches;
    private readonly IRoundStatusRepository _roundStatuses;
    private readonly ILogger<TeamPennantController> _logger;
    private readonly IPoolFinalistConfigRepository _poolFinalistConfigs;
    private readonly DataCacheService _cache;

    public TeamPennantController(
        IPennantMatchRepository matches,
        IRoundStatusRepository roundStatuses,
        IPoolFinalistConfigRepository poolFinalistConfigs,
        ILogger<TeamPennantController> logger,
        DataCacheService cache)
    {
        _matches = matches;
        _roundStatuses = roundStatuses;
        _poolFinalistConfigs = poolFinalistConfigs;
        _logger = logger;
        _cache = cache;
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int year, [FromQuery] string pool)
    {
        var result = await _cache.GetOrCreateAsync(
            $"team-leaderboard:{year}:{pool}",
            async () =>
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
                })
                .ToList();

                return leaderboard;
            });

        return Ok(result);
    }

    [HttpGet("rounds")]
    public async Task<IActionResult> GetRound(
        [FromQuery] int year, [FromQuery] string pool, [FromQuery] string round)
    {
        var result = await _cache.GetOrCreateAsync(
            $"team-round:{year}:{pool}:{round}",
            async () =>
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
                    })
                    .ToList();

                return teamMatches;
            });

        return Ok(result);
    }

    [HttpGet("match")]
    public async Task<IActionResult> GetMatch(
        [FromQuery] int year, [FromQuery] string pool,
        [FromQuery] string round, [FromQuery] string home, [FromQuery] string away)
    {
        var result = await _cache.GetOrCreateAsync(
            $"team-match:{year}:{pool}:{round}:{home}:{away}",
            async () =>
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

                return deduped;
            });

        return Ok(result);
    }

    [HttpGet("rounds-list")]
    public async Task<IActionResult> GetRoundsList([FromQuery] int year, [FromQuery] string pool)
    {
        var result = await _cache.GetOrCreateAsync(
            $"team-rounds-list:{year}:{pool}",
            async () =>
            {
                var rounds = await _matches.GetRoundsListAsync(year, pool);

                var ordered = rounds.OrderBy(r =>
                {
                    if (r == "Final") return 999;
                    if (r == "Semi Final") return 998;
                    var match = System.Text.RegularExpressions.Regex.Match(r, @"\d+");
                    return match.Success ? int.Parse(match.Value) : 0;
                })
                .ToList();

                return ordered;
            });

        return Ok(result);
    }

    [HttpGet("champion")]
    public async Task<IActionResult> GetChampion([FromQuery] int year, [FromQuery] string pool)
    {
        var result = await _cache.GetOrCreateAsync<object?>(
            $"team-champion:{year}:{pool}",
            async () =>
            {
                var matches = await _matches.GetByYearAndPoolAndRoundAsync(year, pool, "Final");
                var matchList = matches.ToList();

                if (!matchList.Any()) return null;

                var final = matchList.First();

                var deduped = matchList
                    .OrderBy(m => m.Id)
                    .Where((m, i) => i % 2 == 0)
                    .ToList();

                var homePoints = deduped.Sum(m => m.PlayerWon == true ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0);
                var awayPoints = deduped.Sum(m => m.PlayerWon == false ? 1.0 : m.PlayerWon == null ? 0.5 : 0.0);

                return new
                {
                    Champion = homePoints > awayPoints ? final.HomeClub : final.AwayClub,
                    RunnerUp = homePoints > awayPoints ? final.AwayClub : final.HomeClub,
                    Score = homePoints > awayPoints
                        ? $"{(homePoints % 1 == 0 ? homePoints.ToString("0") : homePoints.ToString("0.0"))} - {(awayPoints % 1 == 0 ? awayPoints.ToString("0") : awayPoints.ToString("0.0"))}"
                        : $"{(awayPoints % 1 == 0 ? awayPoints.ToString("0") : awayPoints.ToString("0.0"))} - {(homePoints % 1 == 0 ? homePoints.ToString("0") : homePoints.ToString("0.0"))}"
                };
            });

        return Ok(result);
    }

    [HttpGet("club-rounds")]
    public async Task<IActionResult> GetClubRounds(
        [FromQuery] int year, [FromQuery] string pool, [FromQuery] string club)
    {
        var result = await _cache.GetOrCreateAsync(
            $"team-club-rounds:{year}:{pool}:{club}",
            async () =>
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

                return rounds;
            });

        return Ok(result);
    }

    [HttpGet("active-round")]
    public async Task<IActionResult> GetActiveRound([FromQuery] int year, [FromQuery] string pool)
    {
        var result = await _cache.GetOrCreateAsync(
            $"active-round:{year}:{pool}",
            async () =>
            {
                var all = await _roundStatuses.GetByYearAsync(year);
                var poolStatuses = all.Where(r => r.Pool == pool).ToList();

                var activeRound = poolStatuses.FirstOrDefault(r => !r.IsSettled)?.Round;

                return new { activeRound };
            });

        return Ok(result);
    }

    [HttpGet("finalists")]
    public async Task<IActionResult> GetFinalists([FromQuery] int year, [FromQuery] string pool)
    {
        var result = await _cache.GetOrCreateAsync<object>(
            $"finalists:{year}:{pool}",
            async () =>
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

                    return new { finalists = actualFinalists, source = "actual" };
                }

                // Current season — always use FinalistCount config to mark the cutoff line
                var config = await _poolFinalistConfigs.GetAsync(pool);
                if (config == null)
                    return new { finalists = new List<string>(), source = "none" };

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

                return new { finalists = projectedFinalists, source = "projected" };
            });

        return Ok(result);
    }
}
