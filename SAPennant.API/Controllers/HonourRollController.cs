using Microsoft.AspNetCore.Mvc;
using SAPennant.API.Repositories.Interfaces;
using SAPennant.API.Services;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HonourRollController : ControllerBase
{
    private readonly IHonourRollRepository _honourRoll;
    private readonly DataCacheService _cache;

    public HonourRollController(IHonourRollRepository honourRoll, DataCacheService cache)
    {
        _honourRoll = honourRoll;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? competition = null,
        [FromQuery] string? pool = null,
        [FromQuery] int? year = null,
        [FromQuery] string? club = null)
    {
        var result = await _cache.GetOrCreateAsync(
            $"honour-roll:{competition}:{pool}:{year}:{club}",
            async () =>
            {
                var results = await _honourRoll.GetAsync(competition, pool, year, club);
                return results.Select(h => new
                {
                    h.Id,
                    h.Year,
                    h.Competition,
                    h.Pool,
                    h.Winner
                }).ToList();
            });

        return Ok(result);
    }

    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters([FromQuery] string? competition = null)
    {
        var result = await _cache.GetOrCreateAsync<object>(
            $"honour-roll-filters:{competition}",
            async () =>
            {
                var competitions = await _honourRoll.GetCompetitionsAsync();
                var pools = await _honourRoll.GetPoolsAsync(competition);
                var clubs = await _honourRoll.GetClubsAsync();

                return new { competitions, pools, clubs };
            });

        return Ok(result);
    }

    [HttpGet("narratives")]
    public async Task<IActionResult> GetNarratives()
    {
        var result = await _cache.GetOrCreateAsync(
            "honour-roll-narratives",
            async () => (await _honourRoll.GetNarrativesAsync()).ToList());

        return Ok(result);
    }
}