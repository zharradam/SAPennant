using SAPennant.API.Data;
using SAPennant.API.Models;
using System.Text.Json;

namespace SAPennant.API.Services;

public class GolfboxSyncService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly ILogger<GolfboxSyncService> _logger;
    private const string BASE_URL = "https://scores.golfbox.dk/Handlers";

    public GolfboxSyncService(AppDbContext db, HttpClient http, ILogger<GolfboxSyncService> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task SyncAllAsync()
    {
        _logger.LogInformation("Starting Golfbox sync...");

        var seasons = _db.Seasons.OrderByDescending(s => s.Year).ToList();

        foreach (var season in seasons)
        {
            await SyncSeasonIfNeededAsync(season.Year, season.RegularId, false);
            if (season.FinalsId.HasValue)
                await SyncSeasonIfNeededAsync(season.Year, season.FinalsId.Value, true);
        }

        _db.SyncLogs.Add(new SyncLog { SyncedAt = DateTime.UtcNow, Type = "Full" });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Golfbox sync complete.");
    }

    public async Task RefreshYearAsync(int year)
    {
        _logger.LogInformation("Refreshing {Year}...", year);

        var season = _db.Seasons.FirstOrDefault(s => s.Year == year);
        if (season == null)
        {
            _logger.LogWarning("No season found for year {Year}", year);
            return;
        }

        var existing = _db.PennantMatches.Where(m => m.Year == year);
        _db.PennantMatches.RemoveRange(existing);
        await _db.SaveChangesAsync();

        await SyncSeasonAsync(year, season.RegularId, false);

        if (season.FinalsId.HasValue)
        {
            await SyncSeasonAsync(year, season.FinalsId.Value, true);
        }
        else
        {
            _logger.LogInformation("No finals ID for {Year} — finals may not be available yet", year);
        }

        _db.SyncLogs.Add(new SyncLog { SyncedAt = DateTime.UtcNow, Type = $"Refresh {year}" });
        await _db.SaveChangesAsync();

        _logger.LogInformation("Refresh complete for {Year}", year);
    }

    public async Task<object> GetSeasonIdsAsync(int year)
    {
        var season = _db.Seasons.FirstOrDefault(s => s.Year == year);
        if (season == null)
            return new { error = $"No season found for year {year}" };

        var regularPools = await GetPoolIds(season.RegularId);
        var finalsPools = season.FinalsId.HasValue
            ? await GetPoolIds(season.FinalsId.Value)
            : new List<object>();

        return new
        {
            year,
            regular = new { interclubId = season.RegularId, pools = regularPools },
            finals = season.FinalsId.HasValue
                ? new { interclubId = season.FinalsId.Value, pools = finalsPools }
                : null
        };
    }

    private async Task<List<object>> GetPoolIds(int interclubId)
    {
        var pools = new List<object>();
        var overview = await GetJsonpAsync(
            $"{BASE_URL}/InterclubHandler/GetInterclubData/interclubID/{interclubId}/language/2057/");

        if (overview == null) return pools;

        var divisions = overview.Value
            .GetProperty("Tournament")
            .GetProperty("Divisions")
            .EnumerateArray();

        foreach (var div in divisions)
        {
            var divisionName = div.GetProperty("Name").GetString() ?? "";
            foreach (var pool in div.GetProperty("Pools").EnumerateArray())
            {
                pools.Add(new
                {
                    division = divisionName,
                    pool = pool.GetProperty("Name").GetString(),
                    competitionId = pool.GetProperty("CompetitionID").GetInt64()
                });
            }
        }

        return pools;
    }

    private async Task SyncSeasonIfNeededAsync(int year, int interclubId, bool isFinals)
    {
        var exists = _db.PennantMatches.Any(m => m.Year == year && m.IsFinals == isFinals);
        if (exists)
        {
            _logger.LogInformation("Skipping {Year} {Type} — already loaded", year, isFinals ? "Finals" : "Regular");
            return;
        }

        await SyncSeasonAsync(year, interclubId, isFinals);
    }

    private async Task SyncSeasonAsync(int year, int interclubId, bool isFinals)
    {
        _logger.LogInformation("Syncing {Year} {Type} (id={Id})", year, isFinals ? "Finals" : "Regular", interclubId);

        var overview = await GetJsonpAsync(
            $"{BASE_URL}/InterclubHandler/GetInterclubData/interclubID/{interclubId}/language/2057/");
        if (overview == null) return;

        var divisions = overview.Value
            .GetProperty("Tournament")
            .GetProperty("Divisions")
            .EnumerateArray();

        foreach (var div in divisions)
        {
            var divisionName = div.GetProperty("Name").GetString() ?? "";
            foreach (var pool in div.GetProperty("Pools").EnumerateArray())
            {
                var poolName = pool.GetProperty("Name").GetString() ?? "";
                var competitionId = pool.GetProperty("CompetitionID").GetInt64();
                await SyncPoolAsync(year, isFinals, divisionName, poolName, competitionId);
                await Task.Delay(200);
            }
        }
    }

    private async Task SyncPoolAsync(int year, bool isFinals, string division, string poolName, long competitionId)
    {
        var url = isFinals
            ? $"{BASE_URL}/TeamMatchplayBracketHandler/GetTeamMatchplayScores/CompetitionId/{competitionId}/language/2057/"
            : $"{BASE_URL}/RoundRobinHandler/GetRoundRobin/CompetitionId/{competitionId}/language/2057/";

        var data = await GetJsonpAsync(url);
        if (data == null)
        {
            _logger.LogWarning("Skipping {Year} {Type} {Division} {Pool} — no data returned", year, isFinals ? "Finals" : "Regular", division, poolName);
            return;
        }

        if (!data.Value.TryGetProperty("Matchplay", out var matchplay))
        {
            _logger.LogWarning("Skipping {Year} {Type} {Division} {Pool} — no Matchplay key", year, isFinals ? "Finals" : "Regular", division, poolName);
            return;
        }

        var firstClassProp = matchplay.EnumerateObject().FirstOrDefault();
        if (firstClassProp.Value.ValueKind == JsonValueKind.Undefined)
        {
            _logger.LogWarning("Skipping {Year} {Type} {Division} {Pool} — Matchplay is empty", year, isFinals ? "Finals" : "Regular", division, poolName);
            return;
        }

        var firstClass = firstClassProp.Value;
        var matches = new List<PennantMatch>();

        if (firstClass.TryGetProperty("TeamMatches", out var teamMatchesProp))
        {
            await ProcessTeamMatches(teamMatchesProp, competitionId, year, isFinals, division, poolName, matches, 1);
        }
        else if (firstClass.TryGetProperty("Rounds", out var roundsProp))
        {
            var allRounds = roundsProp.EnumerateObject().ToList();
            int totalRounds = allRounds.Count;
            foreach (var roundProp in allRounds)
            {
                var round = roundProp.Value;
                if (round.TryGetProperty("TeamMatches", out var roundTeamMatches))
                {
                    await ProcessTeamMatches(roundTeamMatches, competitionId, year, isFinals, division, poolName, matches, totalRounds);
                }
            }
        }
        else
        {
            _logger.LogWarning("Skipping {Year} {Type} {Division} {Pool} — no TeamMatches or Rounds found", year, isFinals ? "Finals" : "Regular", division, poolName);
            return;
        }

        _logger.LogInformation("Saved {Count} matches for {Year} {Type} {Division} {Pool}", matches.Count, year, isFinals ? "Finals" : "Regular", division, poolName);
        _db.PennantMatches.AddRange(matches);
        await _db.SaveChangesAsync();
    }

    private async Task ProcessTeamMatches(
        JsonElement teamMatchesProp,
        long competitionId,
        int year,
        bool isFinals,
        string division,
        string poolName,
        List<PennantMatch> matches,
        int totalRounds)
    {
        foreach (var tm in teamMatchesProp.EnumerateObject())
        {
            var tmVal = tm.Value;
            if (tmVal.GetProperty("IsBye").GetBoolean()) continue;
            if (!tmVal.GetProperty("IsSettled").GetBoolean()) continue;

            var teamMatchId = tmVal.GetProperty("TeamMatchID").GetInt64();
            var roundNumber = tmVal.TryGetProperty("InterclubRoundNumber", out var rn) ? rn.GetInt32() : 1;
            var startTime = tmVal.GetProperty("StartTime").GetString() ?? "";

            var tmMatches = await GetTeamMatchAsync(competitionId, teamMatchId, year, isFinals, division, poolName, roundNumber, startTime, totalRounds);
            matches.AddRange(tmMatches);

            await Task.Delay(100);
        }
    }

    private async Task<List<PennantMatch>> GetTeamMatchAsync(
        long competitionId, long teamMatchId, int year, bool isFinals,
        string division, string poolName, int roundNumber, string startTime, int totalRounds = 1)
    {
        var results = new List<PennantMatch>();
        var data = await GetJsonpAsync($"{BASE_URL}/TeamMatchHandler/GetTeamMatch/CompetitionId/{competitionId}/TeamMatchId/{teamMatchId}/language/2057/");
        if (data == null) return results;

        var tm = data.Value.GetProperty("TeamMatch");
        var homeClub = tm.GetProperty("Home").GetProperty("Name").GetString() ?? "";
        var awayClub = tm.GetProperty("Away").GetProperty("Name").GetString() ?? "";
        var venue = tm.TryGetProperty("InterclubHostingClub", out var v) ? v.GetString() : null;
        var date = ParseDate(startTime);
        var round = GetRoundName(roundNumber, isFinals, totalRounds);

        foreach (var matchProp in tm.GetProperty("Matches").EnumerateObject())
        {
            var match = matchProp.Value;
            var matchResult = match.GetProperty("Result").GetString() ?? "";
            var format = match.GetProperty("Format").GetString() ?? "single";
            var teams = match.GetProperty("Teams").EnumerateArray().ToList();
            if (teams.Count < 2) continue;

            var homeTeam = teams[0];
            var awayTeam = teams[1];

            var homeName = ToTitleCase(GetPlayerNames(homeTeam));
            var awayName = ToTitleCase(GetPlayerNames(awayTeam));
            var homeClubName = GetPlayerClub(homeTeam, homeClub);
            var awayClubName = GetPlayerClub(awayTeam, awayClub);
            var homeHandicap = GetPlayerHandicap(homeTeam);
            var awayHandicap = GetPlayerHandicap(awayTeam);

            var homeIsLead = homeTeam.GetProperty("IsLead").GetBoolean();
            var awayIsLead = awayTeam.GetProperty("IsLead").GetBoolean();
            bool? homeWon = homeIsLead ? true : awayIsLead ? false : null;

            if (!string.IsNullOrEmpty(homeName))
            {
                results.Add(new PennantMatch
                {
                    Year = year,
                    IsFinals = isFinals,
                    Division = division,
                    Pool = poolName,
                    Round = round,
                    Date = date,
                    HomeClub = homeClub,
                    AwayClub = awayClub,
                    PlayerName = homeName,
                    OpponentName = awayName,
                    PlayerClub = homeClubName,
                    OpponentClub = awayClubName,
                    PlayerHandicap = homeHandicap,
                    OpponentHandicap = awayHandicap,
                    Venue = venue,
                    Result = matchResult,
                    PlayerWon = homeWon,
                    Format = format,
                });
            }

            if (!string.IsNullOrEmpty(awayName))
            {
                results.Add(new PennantMatch
                {
                    Year = year,
                    IsFinals = isFinals,
                    Division = division,
                    Pool = poolName,
                    Round = round,
                    Date = date,
                    HomeClub = homeClub,
                    AwayClub = awayClub,
                    PlayerName = awayName,
                    OpponentName = homeName,
                    PlayerClub = awayClubName,
                    OpponentClub = homeClubName,
                    PlayerHandicap = awayHandicap,
                    OpponentHandicap = homeHandicap,
                    Venue = venue,
                    Result = matchResult,
                    PlayerWon = homeWon == null ? null : !homeWon,
                    Format = format,
                });
            }
        }

        return results;
    }

    private string GetRoundName(int roundNumber, bool isFinals, int totalRounds)
    {
        if (!isFinals) return $"Round {roundNumber}";

        if (roundNumber == totalRounds) return "Final";
        if (roundNumber == totalRounds - 1) return "Semi Final";
        if (roundNumber == totalRounds - 2) return "Quarter Final";
        return $"Round {roundNumber}";
    }

    private string ToTitleCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLower());
    }

    private string GetPlayerNames(JsonElement team)
    {
        if (!team.TryGetProperty("Entries", out var entries)) return "";
        return string.Join(" & ", entries.EnumerateArray()
            .Select(e => $"{e.GetProperty("FirstName").GetString()?.Trim()} {e.GetProperty("LastName").GetString()?.Trim()}".Trim())
            .Where(n => !string.IsNullOrEmpty(n)));
    }

    private string? GetPlayerHandicap(JsonElement team)
    {
        if (!team.TryGetProperty("Entries", out var entries)) return null;
        var handicaps = entries.EnumerateArray()
            .Select(e => e.TryGetProperty("HCP", out var hcp) ? hcp.GetString() : null)
            .Where(h => !string.IsNullOrEmpty(h));
        return string.Join(" & ", handicaps);
    }

    private string GetPlayerClub(JsonElement team, string fallback)
    {
        if (!team.TryGetProperty("Entries", out var entries)) return fallback;
        var first = entries.EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined) return fallback;
        return first.GetProperty("ClubName").GetString() ?? fallback;
    }

    private string ParseDate(string startTime)
    {
        if (startTime.Length < 8) return "";
        var y = startTime[..4];
        var m = startTime[4..6];
        var d = startTime[6..8];
        return DateTime.TryParse($"{y}-{m}-{d}", out var dt)
            ? dt.ToString("dd MMM yyyy")
            : "";
    }

    private async Task<JsonElement?> GetJsonpAsync(string url)
    {
        try
        {
            var cbName = $"cb{Guid.NewGuid():N}";
            var fullUrl = $"{url}{(url.Contains('?') ? "&" : "?")}callback={cbName}&_={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var response = await _http.GetStringAsync(fullUrl);

            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');

            if (start < 0 || end <= start)
            {
                _logger.LogWarning("No JSON found in response from {Url}", url);
                return null;
            }

            var json = response[start..(end + 1)];
            json = json.Replace(":!0", ":true")
                       .Replace(":!1", ":false");

            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Url}", url);
            return null;
        }
    }
}