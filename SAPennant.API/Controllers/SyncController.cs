using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Services;
using System.Runtime;

namespace SAPennant.API.Controllers;

//[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly GolfboxSyncService _sync;
    private readonly AppDbContext _db;
    private readonly TelemetryClient _telemetry;
    private readonly SettingsService _settings;

    public SyncController(GolfboxSyncService sync, AppDbContext db, TelemetryClient telemetry, SettingsService settings)
    {
        _sync = sync;
        _db = db;
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
    public IActionResult GetSeasons()
    {
        var seasons = _db.Seasons
            .OrderByDescending(s => s.Year)
            .Select(s => new { s.Year, s.RegularId, s.FinalsId })
            .ToList();
        return Ok(seasons);
    }

    [HttpPut("seasons/{year}/finals-id")]
    public async Task<IActionResult> UpdateFinalsId(int year, [FromBody] UpdateFinalsIdRequest request)
    {
        var season = _db.Seasons.FirstOrDefault(s => s.Year == year);
        if (season == null) return NotFound();
        season.FinalsId = request.FinalsId;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Finals ID updated for {year}" });
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
    public IActionResult GetSyncStatus()
    {
        var enabled = _settings.GetBool("AutoSyncEnabled", true);
        return Ok(new { enabled });
    }

    [HttpPost("sync-toggle")]
    [Authorize]
    public async Task<IActionResult> ToggleSync([FromBody] bool enabled)
    {
        await _settings.SetBoolAsync("AutoSyncEnabled", enabled);
        return Ok(new { enabled });
    }
}