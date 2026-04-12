using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HonourRollController : ControllerBase
{
    private readonly AppDbContext _db;

    public HonourRollController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? competition = null,
        [FromQuery] string? pool = null,
        [FromQuery] int? year = null,
        [FromQuery] string? club = null)
    {
        var query = _db.HonourRoll.AsQueryable();

        if (!string.IsNullOrEmpty(competition))
            query = query.Where(h => h.Competition == competition);
        if (!string.IsNullOrEmpty(pool))
            query = query.Where(h => h.Pool == pool);
        if (year.HasValue)
            query = query.Where(h => h.Year == year.Value);
        if (!string.IsNullOrEmpty(club))
            query = query.Where(h => h.Winner == club);

        var results = await query
            .OrderByDescending(h => h.Year)
            .ThenBy(h => h.Pool)
            .Select(h => new
            {
                h.Id,
                h.Year,
                h.Competition,
                h.Pool,
                h.Winner
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters([FromQuery] string? competition = null)
    {
        var query = _db.HonourRoll.AsQueryable();

        if (!string.IsNullOrEmpty(competition))
            query = query.Where(h => h.Competition == competition);

        var competitions = await _db.HonourRoll
            .Select(h => h.Competition)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var pools = await query
            .Select(h => h.Pool)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var clubs = await _db.HonourRoll
            .Where(h => h.Winner != null)
            .Select(h => h.Winner!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(new { competitions, pools, clubs });
    }

    [HttpGet("narratives")]
    public async Task<IActionResult> GetNarratives()
    {
        var narratives = await _db.HonourRollNarratives
            .ToListAsync();

        return Ok(narratives);
    }
}