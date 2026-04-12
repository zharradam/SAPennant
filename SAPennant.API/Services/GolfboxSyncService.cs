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
    private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

    public GolfboxSyncService(AppDbContext db, HttpClient http, ILogger<GolfboxSyncService> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task SyncAllAsync()
    {
        await _syncLock.WaitAsync();
        try
        {
            _logger.LogInformation("Starting Golfbox sync...");

            var seasons = _db.Seasons.OrderByDescending(s => s.Year).ToList();

            foreach (var season in seasons)
            {
                await SyncSeasonIfNeededAsync(season.Year, season.RegularId, false, isSenior: false);
                if (season.FinalsId.HasValue)
                    await SyncSeasonIfNeededAsync(season.Year, season.FinalsId.Value, true, isSenior: false);

                // Senior Pennant
                if (season.SeniorRegularId.HasValue)
                {
                    await SyncSeasonAsync(season.Year, season.SeniorRegularId.Value, false, isSenior: true);
                    var seniorFinalsId = season.SeniorFinalsId ?? season.SeniorRegularId.Value;
                    await SyncSeasonAsync(season.Year, seniorFinalsId, true, isSenior: true);
                }
            }

            _db.SyncLogs.Add(new SyncLog { SyncedAt = DateTime.UtcNow, Type = "Full" });
            await _db.SaveChangesAsync();

            _logger.LogInformation("Golfbox sync complete.");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task RefreshYearAsync(int year)
    {
        await _syncLock.WaitAsync();
        try
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

            // also clear round statuses for this year so they get re-evaluated
            var roundStatuses = _db.RoundStatuses.Where(r => r.Year == year);
            _db.RoundStatuses.RemoveRange(roundStatuses);

            await _db.SaveChangesAsync();

            await SyncSeasonAsync(year, season.RegularId, false, isSenior: false);

            if (season.FinalsId.HasValue)
                await SyncSeasonAsync(year, season.FinalsId.Value, true, isSenior: false);
            else
                _logger.LogInformation("No finals ID for {Year} — finals may not be available yet", year);

            // Senior Pennant
            if (season.SeniorRegularId.HasValue)
            {
                await SyncSeasonAsync(year, season.SeniorRegularId.Value, false, isSenior: true);
                var seniorFinalsId = season.SeniorFinalsId ?? season.SeniorRegularId.Value;
                await SyncSeasonAsync(year, seniorFinalsId, true, isSenior: true);
            }

            // backfill RoundStatuses for all synced rounds
            var syncedRounds = _db.PennantMatches
                .Where(m => m.Year == year && !m.IsFinals)
                .Select(m => new { m.Pool, m.Round })
                .Distinct()
                .ToList();

            foreach (var r in syncedRounds)
            {
                var exists = _db.RoundStatuses.Any(rs => rs.Year == year && rs.Pool == r.Pool && rs.Round == r.Round);
                if (!exists)
                {
                    _db.RoundStatuses.Add(new RoundStatus
                    {
                        Year = year,
                        Pool = r.Pool,
                        Round = r.Round,
                        IsSettled = false,
                        LastChecked = DateTime.UtcNow,
                        SettledAt = null
                    });
                }
            }
            await _db.SaveChangesAsync();

            _db.SyncLogs.Add(new SyncLog { SyncedAt = DateTime.UtcNow, Type = $"Refresh {year}" });
            await _db.SaveChangesAsync();

            _logger.LogInformation("Refresh complete for {Year}", year);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task SyncCurrentYearUnsettledAsync()
    {
        if (!await _syncLock.WaitAsync(0))
        {
            _logger.LogInformation("Sync already in progress, skipping unsettled sync.");
            return;
        }
        try
        {
            var currentYear = DateTime.UtcNow.Year;
            _logger.LogInformation("Checking unsettled rounds for {Year}...", currentYear);

            var season = _db.Seasons.FirstOrDefault(s => s.Year == currentYear);
            if (season == null)
            {
                _logger.LogWarning("No season found for {Year}", currentYear);
                return;
            }

            await SyncUnsettledPoolsAsync(currentYear, season.RegularId, false, isSenior: false);

            // Senior Pennant
            if (season.SeniorRegularId.HasValue)
            {
                await SyncUnsettledPoolsAsync(currentYear, season.SeniorRegularId.Value, false, isSenior: true);
                var seniorFinalsId = season.SeniorFinalsId ?? season.SeniorRegularId.Value;
                await SyncUnsettledPoolsAsync(currentYear, seniorFinalsId, true, isSenior: true);
            }

            _db.SyncLogs.Add(new SyncLog { SyncedAt = DateTime.UtcNow, Type = $"UnsettledSync {currentYear}" });
            await _db.SaveChangesAsync();
        }
        finally
        {
            _syncLock.Release();
        }
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

    private async Task SyncSeasonIfNeededAsync(int year, int interclubId, bool isFinals, bool isSenior = false)
    {
        var exists = _db.PennantMatches.Any(m => m.Year == year && m.IsFinals == isFinals && m.IsSenior == isSenior);
        if (exists)
        {
            _logger.LogInformation("Skipping {Year} {Type} {Senior}— already loaded",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "");
            return;
        }

        await SyncSeasonAsync(year, interclubId, isFinals, isSenior);
    }

    private async Task SyncSeasonAsync(int year, int interclubId, bool isFinals, bool isSenior = false)
    {
        _logger.LogInformation("Syncing {Year} {Type} {Senior}(id={Id})",
            year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", interclubId);

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
                await SyncPoolAsync(year, isFinals, isSenior, divisionName, poolName, competitionId);
                await Task.Delay(200);
            }
        }
    }

    private async Task SyncPoolAsync(int year, bool isFinals, bool isSenior, string division, string poolName, long competitionId)
    {
        var url = isFinals
            ? $"{BASE_URL}/TeamMatchplayBracketHandler/GetTeamMatchplayScores/CompetitionId/{competitionId}/language/2057/"
            : $"{BASE_URL}/RoundRobinHandler/GetRoundRobin/CompetitionId/{competitionId}/language/2057/";

        var data = await GetJsonpAsync(url);
        if (data == null)
        {
            _logger.LogWarning("Skipping {Year} {Type} {Senior}{Division} {Pool} — no data returned",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName);
            return;
        }

        if (!data.Value.TryGetProperty("Matchplay", out var matchplay))
        {
            _logger.LogWarning("Skipping {Year} {Type} {Senior}{Division} {Pool} — no Matchplay key",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName);
            return;
        }

        var firstClassProp = matchplay.EnumerateObject().FirstOrDefault();
        if (firstClassProp.Value.ValueKind == JsonValueKind.Undefined)
        {
            _logger.LogWarning("Skipping {Year} {Type} {Senior}{Division} {Pool} — Matchplay is empty",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName);
            return;
        }

        var firstClass = firstClassProp.Value;
        var matches = new List<PennantMatch>();

        if (firstClass.TryGetProperty("TeamMatches", out var teamMatchesProp))
        {
            await ProcessTeamMatches(teamMatchesProp, competitionId, year, isFinals, isSenior, division, poolName, matches, 1);
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
                    await ProcessTeamMatches(roundTeamMatches, competitionId, year, isFinals, isSenior, division, poolName, matches, totalRounds);
                }
            }
        }
        else
        {
            _logger.LogWarning("Skipping {Year} {Type} {Senior}{Division} {Pool} — no TeamMatches or Rounds found",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName);
            return;
        }

        _logger.LogInformation("Saved {Count} matches for {Year} {Type} {Senior}{Division} {Pool}",
            matches.Count, year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName);
        _db.PennantMatches.AddRange(matches);
        await _db.SaveChangesAsync();
    }

    private async Task ProcessTeamMatches(
        JsonElement teamMatchesProp,
        long competitionId,
        int year,
        bool isFinals,
        bool isSenior,
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

            var tmMatches = await GetTeamMatchAsync(competitionId, teamMatchId, year, isFinals, isSenior, division, poolName, roundNumber, startTime, totalRounds);
            matches.AddRange(tmMatches);

            await Task.Delay(100);
        }
    }

    private async Task<List<PennantMatch>> GetTeamMatchAsync(
        long competitionId, long teamMatchId, int year, bool isFinals, bool isSenior,
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
                    IsSenior = isSenior,
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
                    IsSenior = isSenior,
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

    private async Task SyncUnsettledPoolsAsync(int year, int interclubId, bool isFinals, bool isSenior = false)
    {
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
                await SyncUnsettledRoundsForPoolAsync(year, isFinals, isSenior, divisionName, poolName, competitionId);
                await Task.Delay(200);
            }
        }
    }

    private async Task SyncUnsettledRoundsForPoolAsync(int year, bool isFinals, bool isSenior, string division, string poolName, long competitionId)
    {
        var url = $"{BASE_URL}/RoundRobinHandler/GetRoundRobin/CompetitionId/{competitionId}/language/2057/";
        var data = await GetJsonpAsync(url);
        if (data == null) { _logger.LogWarning("No data returned for {Pool}", poolName); return; }

        if (!data.Value.TryGetProperty("Matchplay", out var matchplay)) { _logger.LogWarning("No Matchplay for {Pool}", poolName); return; }

        var firstClassProp = matchplay.EnumerateObject().FirstOrDefault();
        if (firstClassProp.Value.ValueKind == JsonValueKind.Undefined) { _logger.LogWarning("Matchplay empty for {Pool}", poolName); return; }

        var firstClass = firstClassProp.Value;

        if (firstClass.TryGetProperty("Rounds", out var roundsProp))
        {
            _logger.LogInformation("Pool {Pool} has Rounds structure", poolName);
            var allRounds = roundsProp.EnumerateObject().ToList();
            int totalRounds = allRounds.Count;

            foreach (var roundProp in allRounds)
            {
                if (!roundProp.Value.TryGetProperty("TeamMatches", out var teamMatches)) continue;
                var roundNumber = int.TryParse(roundProp.Name, out var rn) ? rn : 1;
                var roundName = GetRoundName(roundNumber, isFinals, totalRounds);
                await ProcessUnsettledRoundAsync(year, isFinals, isSenior, division, poolName, competitionId, roundName, roundNumber, teamMatches, totalRounds);
            }
        }
        else if (firstClass.TryGetProperty("TeamMatches", out var teamMatchesDirect))
        {
            _logger.LogInformation("Pool {Pool} has direct TeamMatches structure", poolName);

            var matchesByRound = teamMatchesDirect.EnumerateObject()
                .GroupBy(tm => tm.Value.TryGetProperty("InterclubRoundNumber", out var rn) ? rn.GetInt32() : 1)
                .OrderBy(g => g.Key);

            int totalRounds = matchesByRound.Max(g => g.Key);

            foreach (var roundGroup in matchesByRound)
            {
                var roundNumber = roundGroup.Key;
                var roundName = GetRoundName(roundNumber, isFinals, totalRounds);
                await ProcessUnsettledRoundFromListAsync(year, isFinals, isSenior, division, poolName, competitionId,
                    roundName, roundNumber, roundGroup.ToList(), totalRounds);
            }
        }
        else
        {
            _logger.LogWarning("No Rounds or TeamMatches found for {Pool}", poolName);
        }
    }

    private async Task ProcessUnsettledRoundAsync(
        int year, bool isFinals, bool isSenior, string division, string poolName,
        long competitionId, string roundName, int roundNumber,
        JsonElement teamMatches, int totalRounds)
    {
        var roundStatus = _db.RoundStatuses.FirstOrDefault(r =>
            r.Year == year && r.Pool == poolName && r.Round == roundName);

        if (roundStatus?.IsSettled == true)
        {
            var hasData = _db.PennantMatches.Any(m => m.Year == year && m.Pool == poolName && m.Round == roundName && m.IsSenior == isSenior);
            if (hasData)
            {
                _logger.LogInformation("Skipping {Pool} {Round} — already settled", poolName, roundName);
                return;
            }
            _logger.LogInformation("Re-syncing {Pool} {Round} — marked settled but no data found", poolName, roundName);
        }

        var teamMatchList = teamMatches.EnumerateObject().ToList();
        if (!teamMatchList.Any()) return;

        var allSettled = teamMatchList.All(tm => tm.Value.GetProperty("IsSettled").GetBoolean());
        var anySettled = teamMatchList.Any(tm => tm.Value.GetProperty("IsSettled").GetBoolean());

        _logger.LogInformation("Pool={Pool} Round={Round} TeamMatches={Count} AnySettled={AnySettled} AllSettled={AllSettled}",
            poolName, roundName, teamMatchList.Count, anySettled, allSettled);

        if (!anySettled) return;

        _logger.LogInformation("Syncing {Pool} {Round} — allSettled={AllSettled}", poolName, roundName, allSettled);

        var existing = _db.PennantMatches
            .Where(m => m.Year == year && m.Pool == poolName && m.Round == roundName && m.IsSenior == isSenior);
        _db.PennantMatches.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var matches = new List<PennantMatch>();
        foreach (var tm in teamMatchList)
        {
            var tmVal = tm.Value;
            if (tmVal.GetProperty("IsBye").GetBoolean()) continue;
            if (!tmVal.GetProperty("IsSettled").GetBoolean()) continue;

            var teamMatchId = tmVal.GetProperty("TeamMatchID").GetInt64();
            var startTime = tmVal.GetProperty("StartTime").GetString() ?? "";

            var tmMatches = await GetTeamMatchAsync(competitionId, teamMatchId, year, isFinals, isSenior, division, poolName, roundNumber, startTime, totalRounds);
            tmMatches = tmMatches.Where(m => !(m.Result == "" && m.PlayerWon == null)).ToList();
            matches.AddRange(tmMatches);
            await Task.Delay(100);
        }

        _db.PennantMatches.AddRange(matches);

        if (roundStatus == null)
        {
            roundStatus = new RoundStatus { Year = year, Pool = poolName, Round = roundName };
            _db.RoundStatuses.Add(roundStatus);
        }

        roundStatus.LastChecked = DateTime.UtcNow;
        if (allSettled)
        {
            roundStatus.IsSettled = true;
            roundStatus.SettledAt = DateTime.UtcNow;
            _logger.LogInformation("{Pool} {Round} is now fully settled.", poolName, roundName);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Synced {Count} matches for {Pool} {Round}", matches.Count, poolName, roundName);
    }

    private async Task ProcessUnsettledRoundFromListAsync(
        int year, bool isFinals, bool isSenior, string division, string poolName,
        long competitionId, string roundName, int roundNumber,
        List<System.Text.Json.JsonProperty> teamMatchList, int totalRounds)
    {
        var roundStatus = _db.RoundStatuses.FirstOrDefault(r =>
            r.Year == year && r.Pool == poolName && r.Round == roundName);

        if (roundStatus?.IsSettled == true)
        {
            var hasData = _db.PennantMatches.Any(m => m.Year == year && m.Pool == poolName && m.Round == roundName && m.IsSenior == isSenior);
            if (hasData)
            {
                _logger.LogInformation("Skipping {Pool} {Round} — already settled", poolName, roundName);
                return;
            }
            _logger.LogInformation("Re-syncing {Pool} {Round} — marked settled but no data found", poolName, roundName);
        }

        if (!teamMatchList.Any()) return;

        var allSettled = teamMatchList.All(tm => tm.Value.GetProperty("IsSettled").GetBoolean());
        var anySettled = teamMatchList.Any(tm => tm.Value.GetProperty("IsSettled").GetBoolean());

        _logger.LogInformation("Pool={Pool} Round={Round} TeamMatches={Count} AnySettled={AnySettled} AllSettled={AllSettled}",
            poolName, roundName, teamMatchList.Count, anySettled, allSettled);

        if (!anySettled) return;

        _logger.LogInformation("Syncing {Pool} {Round} — allSettled={AllSettled}", poolName, roundName, allSettled);

        var existing = _db.PennantMatches
            .Where(m => m.Year == year && m.Pool == poolName && m.Round == roundName && m.IsSenior == isSenior);
        _db.PennantMatches.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var matches = new List<PennantMatch>();
        foreach (var tm in teamMatchList)
        {
            var tmVal = tm.Value;
            if (tmVal.GetProperty("IsBye").GetBoolean()) continue;
            if (!tmVal.GetProperty("IsSettled").GetBoolean()) continue;

            var teamMatchId = tmVal.GetProperty("TeamMatchID").GetInt64();
            var startTime = tmVal.GetProperty("StartTime").GetString() ?? "";

            var tmMatches = await GetTeamMatchAsync(competitionId, teamMatchId, year, isFinals, isSenior, division, poolName, roundNumber, startTime, totalRounds);
            tmMatches = tmMatches.Where(m => !(m.Result == "" && m.PlayerWon == null)).ToList();
            matches.AddRange(tmMatches);
            await Task.Delay(100);
        }

        _db.PennantMatches.AddRange(matches);

        if (roundStatus == null)
        {
            roundStatus = new RoundStatus { Year = year, Pool = poolName, Round = roundName };
            _db.RoundStatuses.Add(roundStatus);
        }

        roundStatus.LastChecked = DateTime.UtcNow;
        if (allSettled)
        {
            roundStatus.IsSettled = true;
            roundStatus.SettledAt = DateTime.UtcNow;
            _logger.LogInformation("{Pool} {Round} is now fully settled.", poolName, roundName);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Synced {Count} matches for {Pool} {Round}", matches.Count, poolName, roundName);
    }
}