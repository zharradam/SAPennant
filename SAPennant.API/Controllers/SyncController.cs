using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Services;

namespace SAPennant.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly GolfboxSyncService _sync;
    private readonly AppDbContext _db;

    public SyncController(GolfboxSyncService sync, AppDbContext db)
    {
        _sync = sync;
        _db = db;
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run()
    {
        await _sync.SyncAllAsync();
        return Ok(new { message = "Sync complete" });
    }

    [HttpPost("refresh/{year}")]
    public async Task<IActionResult> Refresh(int year)
    {
        await _sync.RefreshYearAsync(year);
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
}