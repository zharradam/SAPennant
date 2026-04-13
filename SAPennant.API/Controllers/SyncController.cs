using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Interfaces;
using SAPennant.API.Services;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly GolfboxSyncService _sync;
    private readonly ISeasonRepository _seasons;
    private readonly IAppSettingRepository _appSettings;
    private readonly TelemetryClient _telemetry;
    private readonly SettingsService _settings;

    public SyncController(
        GolfboxSyncService sync,
        ISeasonRepository seasons,
        IAppSettingRepository appSettings,
        TelemetryClient telemetry,
        SettingsService settings)
    {
        _sync = sync;
        _seasons = seasons;
        _appSettings = appSettings;
        _telemetry = telemetry;
        _settings = settings;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run()
    {
        await _sync.SyncAllAsync();
        _telemetry.TrackEvent("SyncCompleted", new Dictionary<string, string>
        {
            { "year", "all" },
            { "type", "full" }
        });
        return Ok(new { message = "Sync complete" });
    }

    [HttpPost("refresh/{year}")]
    public async Task<IActionResult> Refresh(int year)
    {
        await _sync.RefreshYearAsync(year);
        _telemetry.TrackEvent("SyncCompleted", new Dictionary<string, string>
        {
            { "year", year.ToString() },
            { "type", "refresh" }
        });
        return Ok(new { message = $"Refresh complete for {year}" });
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
    [Authorize]
    public async Task<IActionResult> SyncUnsettled()
    {
        await _sync.SyncCurrentYearUnsettledAsync();
        return Ok(new { message = "Unsettled sync complete" });
    }

    [HttpGet("sync-status")]
    [Authorize]
    public async Task<IActionResult> GetSyncStatus()
    {
        var enabled = await _settings.GetBoolAsync("AutoSyncEnabled", true);
        return Ok(new { enabled });
    }

    [HttpPost("sync-toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleSync([FromBody] bool enabled)
    {
        await _settings.SetBoolAsync("AutoSyncEnabled", enabled);
        return Ok(new { enabled });
    }

    [HttpGet("maintenance")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMaintenance()
    {
        var setting = await _appSettings.GetAsync("MaintenanceMode");
        return Ok(new { enabled = setting?.Value == "true" });
    }

    [HttpPost("maintenance")]
    [Authorize]
    public async Task<IActionResult> SetMaintenance([FromBody] bool enabled)
    {
        await _appSettings.SetAsync("MaintenanceMode", enabled ? "true" : "false");
        return Ok(new { enabled });
    }
}