using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Domain;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Interfaces;
using SAPennant.API.Services;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
// Everything here either mutates data or is admin-only. Endpoints the public
// site needs are opted out individually with [AllowAnonymous].
[Authorize]
public class SyncController : ControllerBase
{
    private readonly GolfboxSyncService _sync;
    private readonly ISeasonRepository _seasons;
    private readonly IAppSettingRepository _appSettings;
    private readonly SettingsService _settings;
    private readonly IUsageEventRepository _usage;

    public SyncController(
        GolfboxSyncService sync,
        ISeasonRepository seasons,
        IAppSettingRepository appSettings,
        SettingsService settings,
        IUsageEventRepository usage)
    {
        _usage = usage;
        _sync = sync;
        _seasons = seasons;
        _appSettings = appSettings;
        _settings = settings;
    }

    // A full sync or refresh takes minutes and Azure drops requests at ~230s,
    // so these start the work and return. The service owns its own DI scope,
    // logs its own failures and refuses to overlap with a running sync.
    [HttpPost("run")]
    public IActionResult Run()
    {
        _ = _sync.SyncAllAsync();
        return Accepted(new { message = "Full sync started" });
    }

    [HttpPost("refresh/{year}")]
    public IActionResult Refresh(int year)
    {
        _ = _sync.RefreshYearAsync(year);
        return Accepted(new { message = $"Refresh of {year} started" });
    }

    [HttpGet("seasons")]
    public async Task<IActionResult> GetSeasons()
    {
        var seasons = await _seasons.GetAllOrderedAsync();
        return Ok(seasons.Select(s => new
        {
            s.Year,
            s.RegularId,
            s.FinalsId,
            s.SeniorRegularId,
            s.SeniorFinalsId
        }));
    }

    [HttpPut("seasons/{year}/finals-id")]
    public async Task<IActionResult> UpdateFinalsId(int year, [FromBody] UpdateFinalsIdRequest request)
    {
        var season = await _seasons.GetByYearAsync(year);
        if (season == null) return NotFound();
        season.FinalsId = request.FinalsId;
        _seasons.Update(season);
        await _seasons.SaveChangesAsync();
        return Ok(new { message = $"Finals ID updated for {year}" });
    }

    [HttpPut("seasons/{year}/senior-regular-id")]
    public async Task<IActionResult> UpdateSeniorRegularId(int year, [FromBody] UpdateFinalsIdRequest request)
    {
        var season = await _seasons.GetByYearAsync(year);
        if (season == null) return NotFound();
        season.SeniorRegularId = request.FinalsId;
        _seasons.Update(season);
        await _seasons.SaveChangesAsync();
        return Ok(new { message = $"Senior Regular ID updated for {year}" });
    }

    [HttpPut("seasons/{year}/senior-finals-id")]
    public async Task<IActionResult> UpdateSeniorFinalsId(int year, [FromBody] UpdateFinalsIdRequest request)
    {
        var season = await _seasons.GetByYearAsync(year);
        if (season == null) return NotFound();
        season.SeniorFinalsId = request.FinalsId;
        _seasons.Update(season);
        await _seasons.SaveChangesAsync();
        return Ok(new { message = $"Senior Finals ID updated for {year}" });
    }

    [HttpPost("sync-unsettled")]
    public async Task<IActionResult> SyncUnsettled()
    {
        await _sync.SyncCurrentYearUnsettledAsync();
        return Ok(new { message = "Unsettled sync complete" });
    }

    [HttpGet("sync-status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        var enabled = await _settings.GetBoolAsync("AutoSyncEnabled", true);
        return Ok(new { enabled });
    }

    [HttpPost("sync-toggle")]
    public async Task<IActionResult> ToggleSync([FromBody] bool enabled)
    {
        await _settings.SetBoolAsync("AutoSyncEnabled", enabled);
        return Ok(new { enabled });
    }

    /// Usage summary for the last `days` Adelaide days; 0 means everything.
    [HttpGet("usage")]
    public async Task<IActionResult> Usage([FromQuery] int days = 30)
    {
        var today = UsageEvents.AdelaideDay(DateTime.UtcNow);
        var from = days <= 0 ? DateOnly.MinValue : today.AddDays(-(days - 1));
        var previousFrom = days <= 0 ? (DateOnly?)null : from.AddDays(-days);
        var adminIds = (await _settings.GetStringAsync(AuthController.AdminVisitorIdsKey) ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries);
        return Ok(await _usage.GetSummaryAsync(from, previousFrom, adminIds));
    }

    [HttpGet("maintenance")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMaintenance()
    {
        var setting = await _appSettings.GetAsync("MaintenanceMode");
        return Ok(new { enabled = setting?.Value == "true" });
    }

    [HttpPost("maintenance")]
    public async Task<IActionResult> SetMaintenance([FromBody] bool enabled)
    {
        await _appSettings.SetAsync("MaintenanceMode", enabled ? "true" : "false");
        return Ok(new { enabled });
    }

    [HttpPost("polling-interval")]
    public async Task<IActionResult> SetPollingInterval([FromBody] int minutes)
    {
        if (minutes < 5 || minutes > 1440)
            return BadRequest("Must be between 5 and 1440 minutes");

        await _settings.SetIntAsync("PollingIntervalMinutes", minutes);
        return Ok();
    }

    [HttpGet("polling-interval")]
    public async Task<IActionResult> GetPollingInterval()
    {
        var minutes = await _settings.GetIntAsync("PollingIntervalMinutes", 60);
        return Ok(new { minutes });
    }
}