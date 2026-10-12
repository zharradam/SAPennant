using SAPennant.API.Domain;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Interfaces;
using System.Text.Json;

namespace SAPennant.API.Services;

public class GolfboxSyncService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _http;
    private readonly ILogger<GolfboxSyncService> _logger;
    private readonly DataCacheService _dataCache;
    private const string BASE_URL = "https://scores.golfbox.dk/Handlers";

    /// Golfbox serves these endpoints to golf.com.au's browser front end, so
    /// present as that page rather than as a bare .NET client.
    public static void ConfigureHttpClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Referer", "https://golf.com.au/");
    }
    private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

    public GolfboxSyncService(
        IServiceScopeFactory scopeFactory,
        HttpClient http,
        ILogger<GolfboxSyncService> logger,
        DataCacheService dataCache)
    {
        _scopeFactory = scopeFactory;
        _http = http;
        _logger = logger;
        _dataCache = dataCache;
    }

    private IDisposable? BeginSyncScope() =>
        _logger.BeginScope(new Dictionary<string, object> { ["category"] = "sync" });

    public async Task SyncAllAsync()
    {
        if (!await _syncLock.WaitAsync(0))
        {
            _logger.LogInformation("Sync already in progress, skipping full sync.");
            return;
        }
        using var _ = BeginSyncScope();
        try
        {
            _logger.LogInformation("Starting Golfbox sync...");

            using var scope = _scopeFactory.CreateScope();
            var matches = scope.ServiceProvider.GetRequiredService<IPennantMatchRepository>();
            var seasons = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
            var syncLogs = scope.ServiceProvider.GetRequiredService<ISyncLogRepository>();

            var allSeasons = await seasons.GetAllOrderedAsync();

            foreach (var season in allSeasons)
            {
                await SyncSeasonIfNeededAsync(matches, season.Year, season.RegularId, false, isSenior: false);
                if (season.FinalsId.HasValue)
                    await SyncSeasonIfNeededAsync(matches, season.Year, season.FinalsId.Value, true, isSenior: false);

                if (season.SeniorRegularId.HasValue)
                {
                    await SyncSeasonIfNeededAsync(matches, season.Year, season.SeniorRegularId.Value, false, isSenior: true);
                    var seniorFinalsId = season.SeniorFinalsId ?? season.SeniorRegularId.Value;
                    await SyncSeasonIfNeededAsync(matches, season.Year, seniorFinalsId, true, isSenior: true);
                }
            }

            await syncLogs.AddAsync(new SyncLog { SyncedAt = DateTime.UtcNow, Type = "Full" });
            await syncLogs.SaveChangesAsync();

            _dataCache.Invalidate();
            _logger.LogInformation("Golfbox sync complete.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Full sync failed.");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task RefreshYearAsync(int year)
    {
        if (!await _syncLock.WaitAsync(0))
        {
            _logger.LogInformation("Sync already in progress, skipping refresh of {Year}.", year);
            return;
        }
        using var _ = BeginSyncScope();
        try
        {
            _logger.LogInformation("Refreshing {Year}...", year);

            using var scope = _scopeFactory.CreateScope();
            var matches = scope.ServiceProvider.GetRequiredService<IPennantMatchRepository>();
            var seasons = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
            var syncLogs = scope.ServiceProvider.GetRequiredService<ISyncLogRepository>();

            var season = await seasons.GetByYearAsync(year);
            if (season == null)
            {
                _logger.LogWarning("No season found for year {Year}", year);
                return;
            }

            await SyncSeasonAsync(matches, year, season.RegularId, false, isSenior: false);

            if (season.FinalsId.HasValue)
                await SyncSeasonAsync(matches, year, season.FinalsId.Value, true, isSenior: false);
            else
                _logger.LogInformation("No finals ID for {Year} — finals may not be available yet", year);

            if (season.SeniorRegularId.HasValue)
            {
                await SyncSeasonAsync(matches, year, season.SeniorRegularId.Value, false, isSenior: true);
                var seniorFinalsId = season.SeniorFinalsId ?? season.SeniorRegularId.Value;
                await SyncSeasonAsync(matches, year, seniorFinalsId, true, isSenior: true);
            }

            await syncLogs.AddAsync(new SyncLog { SyncedAt = DateTime.UtcNow, Type = $"Refresh {year}" });
            await syncLogs.SaveChangesAsync();

            _dataCache.Invalidate();
            _logger.LogInformation("Refresh complete for {Year}", year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh of {Year} failed.", year);
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
        using var _ = BeginSyncScope();
        try
        {
            var currentYear = DateTime.UtcNow.Year;
            _logger.LogDebug("Checking unsettled rounds for {Year}...", currentYear);

            using var scope = _scopeFactory.CreateScope();
            var matches = scope.ServiceProvider.GetRequiredService<IPennantMatchRepository>();
            var seasons = scope.ServiceProvider.GetRequiredService<ISeasonRepository>();
            var syncLogs = scope.ServiceProvider.GetRequiredService<ISyncLogRepository>();
            var roundStatuses = scope.ServiceProvider.GetRequiredService<IRoundStatusRepository>();

            var season = await seasons.GetByYearAsync(currentYear);
            if (season == null)
            {
                _logger.LogWarning("No season found for {Year}", currentYear);
                return;
            }

            await SyncUnsettledPoolsAsync(matches, roundStatuses, currentYear, season.RegularId, false, isSenior: false);

            if (season.FinalsId.HasValue)
                await SyncUnsettledPoolsAsync(matches, roundStatuses, currentYear, season.FinalsId.Value, true, isSenior: false);

            if (season.SeniorRegularId.HasValue)
            {
                await SyncUnsettledPoolsAsync(matches, roundStatuses, currentYear, season.SeniorRegularId.Value, false, isSenior: true);
                var seniorFinalsId = season.SeniorFinalsId ?? season.SeniorRegularId.Value;
                await SyncUnsettledPoolsAsync(matches, roundStatuses, currentYear, seniorFinalsId, true, isSenior: true);
            }

            await syncLogs.AddAsync(new SyncLog { SyncedAt = DateTime.UtcNow, Type = $"UnsettledSync {currentYear}" });
            await syncLogs.SaveChangesAsync();

            _dataCache.Invalidate();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task SyncSeasonIfNeededAsync(
        IPennantMatchRepository matches,
        int year, int interclubId, bool isFinals, bool isSenior = false)
    {
        var exists = await matches.ExistsAsync(year, isFinals, isSenior);
        if (exists)
        {
            _logger.LogDebug("Skipping {Year} {Type} {Senior}— already loaded",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "");
            return;
        }

        await SyncSeasonAsync(matches, year, interclubId, isFinals, isSenior);
    }

    private async Task SyncSeasonAsync(
        IPennantMatchRepository matches,
        int year, int interclubId, bool isFinals, bool isSenior = false)
    {
        _logger.LogInformation("Syncing {Year} {Type} {Senior}(id={Id})",
            year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", interclubId);

        var overview = await GetJsonpAsync(
            $"{BASE_URL}/InterclubHandler/GetInterclubData/interclubID/{interclubId}/language/2057/");
        if (overview == null) return;

        foreach (var (divisionName, poolName, competitionId) in EnumeratePlayablePools(overview.Value, isSenior))
        {
            await SyncPoolAsync(matches, year, isFinals, isSenior, divisionName, poolName, competitionId);
            await Task.Delay(200);
        }
    }

    /// Flattens the tournament overview into the pools that can actually be
    /// fetched. Pools that haven't been drawn yet carry a null CompetitionID
    /// (e.g. a Grand Final before the regular season finishes, or a division
    /// with no entries) and are skipped rather than throwing.
    internal static IEnumerable<(string Division, string Pool, long CompetitionId)> EnumeratePlayablePools(
        JsonElement overview, bool isSenior)
    {
        var divisions = overview
            .GetProperty("Tournament")
            .GetProperty("Divisions")
            .EnumerateArray();

        foreach (var div in divisions)
        {
            var divisionName = (div.GetProperty("Name").GetString() ?? "").Trim();
            foreach (var pool in div.GetProperty("Pools").EnumerateArray())
            {
                var poolName = PennantPools.Normalise(pool.GetProperty("Name").GetString() ?? "", isSenior);
                var competitionId = pool.GetProperty("CompetitionID");
                if (competitionId.ValueKind == JsonValueKind.Null) continue;

                yield return (divisionName, poolName, competitionId.GetInt64());
            }
        }
    }

    /// Fetches a pool's Matchplay class (the object holding its rounds or team
    /// matches). Null, after a warning, when Golfbox returns nothing usable.
    private async Task<JsonElement?> FetchPoolClassAsync(
        int year, bool isFinals, bool isSenior, string division, string poolName, long competitionId)
    {
        var url = isFinals
            ? $"{BASE_URL}/TeamMatchplayBracketHandler/GetTeamMatchplayScores/CompetitionId/{competitionId}/language/2057/"
            : $"{BASE_URL}/RoundRobinHandler/GetRoundRobin/CompetitionId/{competitionId}/language/2057/";

        var data = await GetJsonpAsync(url);
        if (data == null) return Skip("no data returned");
        if (!data.Value.TryGetProperty("Matchplay", out var matchplay)) return Skip("no Matchplay key");
        var firstClass = matchplay.EnumerateObject().FirstOrDefault().Value;
        if (firstClass.ValueKind == JsonValueKind.Undefined) return Skip("Matchplay is empty");
        return firstClass;

        JsonElement? Skip(string reason)
        {
            _logger.LogWarning("Skipping {Year} {Type} {Senior}{Division} {Pool} — {Reason}",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName, reason);
            return null;
        }
    }

    internal record PoolRound(string Name, int Number, int TotalRounds, List<JsonProperty> TeamMatches);

    /// Splits a pool's Matchplay class into rounds. Golfbox uses two shapes:
    /// brackets have a "Rounds" object (one entry per round, each with its own
    /// TeamMatches); round robins have a flat "TeamMatches" object with an
    /// InterclubRoundNumber on each match. Null when neither is present.
    internal static List<PoolRound>? EnumerateRounds(JsonElement firstClass, bool isFinals)
    {
        if (firstClass.TryGetProperty("Rounds", out var roundsProp))
        {
            var rounds = roundsProp.EnumerateObject().ToList();
            var result = new List<PoolRound>();
            for (var i = 0; i < rounds.Count; i++)
            {
                if (!rounds[i].Value.TryGetProperty("TeamMatches", out var teamMatches)) continue;
                var number = rounds[i].Value.TryGetProperty("Number", out var n) ? n.GetInt32() : i + 1;
                result.Add(new PoolRound(GetRoundName(number, isFinals, rounds.Count), number, rounds.Count,
                    teamMatches.EnumerateObject().ToList()));
            }
            return result;
        }

        if (firstClass.TryGetProperty("TeamMatches", out var flat))
        {
            var byRound = flat.EnumerateObject()
                .GroupBy(tm => tm.Value.TryGetProperty("InterclubRoundNumber", out var rn) ? rn.GetInt32() : 1)
                .OrderBy(g => g.Key)
                .ToList();
            var total = byRound.Count == 0 ? 1 : byRound.Max(g => g.Key);
            return byRound
                .Select(g => new PoolRound(GetRoundName(g.Key, isFinals, total), g.Key, total, g.ToList()))
                .ToList();
        }

        return null;
    }

    /// Returns true when the pool's rows were saved and Golfbox reports the
    /// competition completed (only brackets are checked for this by callers).
    private async Task<bool> SyncPoolAsync(
        IPennantMatchRepository matches,
        int year, bool isFinals, bool isSenior, string division, string poolName, long competitionId)
    {
        var firstClass = await FetchPoolClassAsync(year, isFinals, isSenior, division, poolName, competitionId);
        if (firstClass == null) return false;

        var rounds = EnumerateRounds(firstClass.Value, isFinals);
        if (rounds == null)
        {
            _logger.LogWarning("Skipping {Year} {Type} {Senior}{Division} {Pool} — no TeamMatches or Rounds found",
                year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName);
            return false;
        }

        var newMatches = new List<PennantMatch>();
        foreach (var round in rounds)
        {
            var fetched = await FetchTeamMatchesAsync(round, competitionId, year, isFinals, isSenior, division, poolName);
            if (fetched == null) return false;
            newMatches.AddRange(fetched);
        }
        if (newMatches.Count == 0) return false;

        // Replace rather than append, so syncing a pool twice is idempotent.
        // Deliberately after every fetch succeeded: a failed or empty Golfbox
        // response must never clear or thin out rows we already hold.
        await matches.DeleteByYearPoolAsync(year, poolName, isFinals, isSenior);
        await matches.SaveChangesAsync();

        _logger.LogInformation("Saved {Count} matches for {Year} {Type} {Senior}{Division} {Pool}",
            newMatches.Count, year, isFinals ? "Finals" : "Regular", isSenior ? "Senior " : "", division, poolName);
        await matches.AddRangeAsync(newMatches);
        await matches.SaveChangesAsync();

        return firstClass.Value.TryGetProperty("IsCompleted", out var isCompleted)
            && isCompleted.ValueKind == JsonValueKind.True;
    }

    /// Fetches the rubbers of every settled, non-bye team match in a round.
    /// Null if any fetch failed, so callers never save a partial round.
    private async Task<List<PennantMatch>?> FetchTeamMatchesAsync(
        PoolRound round, long competitionId, int year, bool isFinals, bool isSenior, string division, string poolName)
    {
        var result = new List<PennantMatch>();
        foreach (var tm in round.TeamMatches)
        {
            var tmVal = tm.Value;
            if (tmVal.GetProperty("IsBye").GetBoolean()) continue;
            if (!tmVal.GetProperty("IsSettled").GetBoolean()) continue;

            var teamMatchId = tmVal.GetProperty("TeamMatchID").GetInt64();
            var startTime = tmVal.GetProperty("StartTime").GetString() ?? "";

            var data = await GetJsonpAsync($"{BASE_URL}/TeamMatchHandler/GetTeamMatch/CompetitionId/{competitionId}/TeamMatchId/{teamMatchId}/language/2057/");
            if (data == null)
            {
                _logger.LogWarning("Skipping {Pool} {Round} — team match {TeamMatchId} could not be fetched",
                    poolName, round.Name, teamMatchId);
                return null;
            }

            result.AddRange(ParseTeamMatch(data.Value, year, isFinals, isSenior, division, poolName, round.Number, startTime, round.TotalRounds));
            await Task.Delay(100);
        }
        return result;
    }

    internal static List<PennantMatch> ParseTeamMatch(
        JsonElement data, int year, bool isFinals, bool isSenior,
        string division, string poolName, int roundNumber, string startTime, int totalRounds = 1)
    {
        var results = new List<PennantMatch>();
        var tm = data.GetProperty("TeamMatch");
        var homeClub = (tm.GetProperty("Home").GetProperty("Name").GetString() ?? "").Trim();
        var awayClub = (tm.GetProperty("Away").GetProperty("Name").GetString() ?? "").Trim();
        // Golfbox sometimes leaves IsBye false on bracket byes; never store them as fixtures.
        if (homeClub.Equals("BYE", StringComparison.OrdinalIgnoreCase) || awayClub.Equals("BYE", StringComparison.OrdinalIgnoreCase)) return results;
        var venue = tm.TryGetProperty("InterclubHostingClub", out var v) ? v.GetString()?.Trim() : null;
        var (date, matchDate) = ParseStartTime(startTime);
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
                    MatchDate = matchDate,
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
                    MatchDate = matchDate,
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

    internal static string GetRoundName(int roundNumber, bool isFinals, int totalRounds)
    {
        if (!isFinals) return $"Round {roundNumber}";
        if (roundNumber == totalRounds) return "Final";
        if (roundNumber == totalRounds - 1) return "Semi Final";
        if (roundNumber == totalRounds - 2) return "Quarter Final";
        return $"Round {roundNumber}";
    }

    internal static string ToTitleCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLower());
    }

    internal static string GetPlayerNames(JsonElement team)
    {
        if (!team.TryGetProperty("Entries", out var entries)) return "";
        return string.Join(" & ", entries.EnumerateArray()
            .Select(e => $"{e.GetProperty("FirstName").GetString()?.Trim()} {e.GetProperty("LastName").GetString()?.Trim()}".Trim())
            .Where(n => !string.IsNullOrEmpty(n))).Trim();
    }

    internal static string? GetPlayerHandicap(JsonElement team)
    {
        if (!team.TryGetProperty("Entries", out var entries)) return null;
        var handicaps = entries.EnumerateArray()
            .Select(e => e.TryGetProperty("HCP", out var hcp) ? hcp.GetString()?.Trim() : null)
            .Where(h => !string.IsNullOrEmpty(h));
        return string.Join(" & ", handicaps);
    }

    internal static string GetPlayerClub(JsonElement team, string fallback)
    {
        if (!team.TryGetProperty("Entries", out var entries)) return fallback.Trim();
        var first = entries.EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined) return fallback.Trim();
        return (first.GetProperty("ClubName").GetString() ?? fallback).Trim();
    }

    internal static (string Display, DateOnly? Date) ParseStartTime(string startTime)
    {
        if (startTime.Length < 8) return ("", null);
        var y = startTime[..4];
        var m = startTime[4..6];
        var d = startTime[6..8];
        return DateOnly.TryParse($"{y}-{m}-{d}", out var dt)
            ? (dt.ToString("dd MMM yyyy"), dt)
            : ("", null);
    }

    private async Task<JsonElement?> GetJsonpAsync(string url)
    {
        try
        {
            var cbName = $"cb{Guid.NewGuid():N}";
            var fullUrl = $"{url}{(url.Contains('?') ? "&" : "?")}callback={cbName}&_={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var response = await _http.GetStringAsync(fullUrl);

            var json = ExtractJson(response);
            if (json == null)
            {
                _logger.LogWarning("No JSON found in response from {Url}", url);
                return null;
            }

            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Url}", url);
            return null;
        }
    }

    /// Golfbox responses are JSONP with JS-minified booleans (!0/!1); strip the
    /// callback wrapper and normalise to plain JSON.
    internal static string? ExtractJson(string response)
    {
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');

        if (start < 0 || end <= start) return null;

        return response[start..(end + 1)]
            .Replace(":!0", ":true")
            .Replace(":!1", ":false");
    }

    private async Task SyncUnsettledPoolsAsync(
        IPennantMatchRepository matches,
        IRoundStatusRepository roundStatuses,
        int year, int interclubId, bool isFinals, bool isSenior = false)
    {
        var overview = await GetJsonpAsync(
            $"{BASE_URL}/InterclubHandler/GetInterclubData/interclubID/{interclubId}/language/2057/");
        if (overview == null) return;

        foreach (var (divisionName, poolName, competitionId) in EnumeratePlayablePools(overview.Value, isSenior))
        {
            if (isFinals)
                await SyncUnsettledFinalsPoolAsync(matches, roundStatuses, year, isSenior, divisionName, poolName, competitionId);
            else
                await SyncUnsettledRoundsForPoolAsync(matches, roundStatuses, year, isSenior, divisionName, poolName, competitionId);

            await Task.Delay(200);
        }
    }

    /// Finals are brackets, so they're tracked per pool rather than per round:
    /// a single RoundStatus row keyed by FinalsStatusRound, written only once
    /// Golfbox reports the bracket completed.
    private const string FinalsStatusRound = "Finals";

    private async Task SyncUnsettledFinalsPoolAsync(
        IPennantMatchRepository matches,
        IRoundStatusRepository roundStatuses,
        int year, bool isSenior, string division, string poolName, long competitionId)
    {
        var status = await roundStatuses.GetAsync(year, poolName, FinalsStatusRound);
        if (status?.IsSettled == true)
        {
            var poolMatches = await matches.GetByYearAndPoolAsync(year, poolName);
            if (poolMatches.Any(m => m.IsFinals && m.IsSenior == isSenior))
            {
                _logger.LogDebug("Skipping {Pool} finals — already completed", poolName);
                return;
            }
            _logger.LogInformation("Re-syncing {Pool} finals — marked completed but no data found", poolName);
        }

        // SyncPoolAsync replaces the pool's rows only after a successful fetch,
        // so a failed Golfbox call leaves existing finals data untouched.
        var completed = await SyncPoolAsync(matches, year, true, isSenior, division, poolName, competitionId);
        if (!completed) return;

        if (status == null)
        {
            status = new RoundStatus { Year = year, Pool = poolName, Round = FinalsStatusRound };
            await roundStatuses.AddAsync(status);
        }
        status.IsSettled = true;
        status.SettledAt = DateTime.UtcNow;
        status.LastChecked = DateTime.UtcNow;
        await roundStatuses.SaveChangesAsync();
        _logger.LogInformation("{Pool} finals are now completed.", poolName);
    }

    private async Task SyncUnsettledRoundsForPoolAsync(
        IPennantMatchRepository matches,
        IRoundStatusRepository roundStatuses,
        int year, bool isSenior, string division, string poolName, long competitionId)
    {
        var firstClass = await FetchPoolClassAsync(year, false, isSenior, division, poolName, competitionId);
        if (firstClass == null) return;

        var rounds = EnumerateRounds(firstClass.Value, isFinals: false);
        if (rounds == null)
        {
            _logger.LogWarning("No Rounds or TeamMatches found for {Pool}", poolName);
            return;
        }

        foreach (var round in rounds)
            await ProcessUnsettledRoundAsync(matches, roundStatuses, year, isSenior, division, poolName, competitionId, round);
    }

    private async Task ProcessUnsettledRoundAsync(
        IPennantMatchRepository matches,
        IRoundStatusRepository roundStatuses,
        int year, bool isSenior, string division, string poolName, long competitionId, PoolRound round)
    {
        var roundStatus = await roundStatuses.GetAsync(year, poolName, round.Name);

        if (roundStatus?.IsSettled == true)
        {
            var allMatches = await matches.GetByYearAndPoolAndRoundAsync(year, poolName, round.Name);
            if (allMatches.Any(m => m.IsSenior == isSenior))
            {
                _logger.LogDebug("Skipping {Pool} {Round} — already settled", poolName, round.Name);
                return;
            }
            _logger.LogInformation("Re-syncing {Pool} {Round} — marked settled but no data found", poolName, round.Name);
        }

        if (round.TeamMatches.Count == 0) return;

        var allSettled = round.TeamMatches.All(tm => tm.Value.GetProperty("IsSettled").GetBoolean());
        var anySettled = round.TeamMatches.Any(tm => tm.Value.GetProperty("IsSettled").GetBoolean());

        _logger.LogDebug("Pool={Pool} Round={Round} TeamMatches={Count} AnySettled={AnySettled} AllSettled={AllSettled}",
            poolName, round.Name, round.TeamMatches.Count, anySettled, allSettled);

        if (!anySettled) return;

        // Fetch everything before touching the database: a failed fetch leaves
        // the round's existing rows and its status exactly as they were.
        var newMatches = await FetchTeamMatchesAsync(round, competitionId, year, false, isSenior, division, poolName);
        if (newMatches == null) return;
        newMatches = newMatches.Where(m => !(m.Result == "" && m.PlayerWon == null)).ToList();

        await matches.DeleteByYearPoolRoundAsync(year, poolName, round.Name, isSenior);
        await matches.SaveChangesAsync();
        await matches.AddRangeAsync(newMatches);

        if (roundStatus == null)
        {
            roundStatus = new RoundStatus { Year = year, Pool = poolName, Round = round.Name };
            await roundStatuses.AddAsync(roundStatus);
        }

        roundStatus.LastChecked = DateTime.UtcNow;
        if (allSettled)
        {
            roundStatus.IsSettled = true;
            roundStatus.SettledAt = DateTime.UtcNow;
            _logger.LogInformation("{Pool} {Round} is now fully settled.", poolName, round.Name);
        }

        await roundStatuses.SaveChangesAsync();
        if (newMatches.Count > 0)
            _logger.LogInformation("Synced {Count} matches for {Pool} {Round}", newMatches.Count, poolName, round.Name);
    }
}