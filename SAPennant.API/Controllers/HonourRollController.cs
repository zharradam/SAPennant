using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HonourRollController : ControllerBase
{
    private readonly IHonourRollRepository _honourRoll;

    public HonourRollController(IHonourRollRepository honourRoll)
    {
        _honourRoll = honourRoll;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? competition = null,
        [FromQuery] string? pool = null,
        [FromQuery] int? year = null,
        [FromQuery] string? club = null)
    {
        var results = await _honourRoll.GetAsync(competition, pool, year, club);
        return Ok(results.Select(h => new
        {
            h.Id,
            h.Year,
            h.Competition,
            h.Pool,
            h.Winner
        }));
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters([FromQuery] string? competition = null)
    {
        var competitions = await _honourRoll.GetCompetitionsAsync();
        var pools = await _honourRoll.GetPoolsAsync(competition);
        var clubs = await _honourRoll.GetClubsAsync();

        return Ok(new { competitions, pools, clubs });
    }

    [HttpGet("narratives")]
    public async Task<IActionResult> GetNarratives()
    {
        var narratives = await _honourRoll.GetNarrativesAsync();
        return Ok(narratives);
    }
}