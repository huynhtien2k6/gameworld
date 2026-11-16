using GameWorld.Api.Services;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Controllers;

[ApiController]
[Route("api/world")]
[Authorize]
public class WorldController(GameDbContext db, WorldSimulation world) : ControllerBase
{
    [HttpGet("state")]
    public async Task<IActionResult> State(CancellationToken ct) => Ok(await world.GetWorldAsync(ct));

    [HttpGet("regions")]
    public async Task<IActionResult> Regions(CancellationToken ct) => Ok((await world.GetWorldAsync(ct)).Regions);

    [HttpGet("npcs/{regionId:long}")]
    public async Task<IActionResult> Npcs(long regionId, CancellationToken ct)
    {
        if (!await db.Regions.AnyAsync(r => r.Id == regionId, ct)) return NotFound(new { message = "Không tìm thấy vùng đất." });
        return Ok(await world.GetNpcsAsync(regionId, null, ct));
    }
}
