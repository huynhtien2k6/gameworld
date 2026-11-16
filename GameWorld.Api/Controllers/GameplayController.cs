using System.ComponentModel.DataAnnotations;
using GameWorld.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameWorld.Api.Controllers;

[ApiController]
[Route("api/gameplay/{characterId:long}")]
[Authorize]
public sealed class GameplayController(PlayerActions actions) : ControllerBase
{
    private long AccountId => long.TryParse(User.FindFirst("accountId")?.Value, out var id) ? id : throw new UnauthorizedAccessException("Hãy đăng nhập bằng tài khoản người chơi.");

    [HttpGet("state")]
    public async Task<IActionResult> State(long characterId, CancellationToken ct) => Ok(await actions.GetStateAsync(AccountId, characterId, ct));
    [HttpPost("travel")]
    public async Task<IActionResult> Travel(long characterId, MapRequest request, CancellationToken ct) => Ok(await actions.TravelAsync(AccountId, characterId, request.MapId, ct));
    [HttpPost("trade")]
    public async Task<IActionResult> Trade(long characterId, TradeRequest request, CancellationToken ct) => Ok(await actions.TradeAsync(AccountId, characterId, request, ct));
    [HttpPost("plant")]
    public async Task<IActionResult> Plant(long characterId, CropRequest request, CancellationToken ct) => Ok(await actions.PlantAsync(AccountId, characterId, request.CropId, ct));
    [HttpPost("harvest")]
    public async Task<IActionResult> Harvest(long characterId, PlotRequest request, CancellationToken ct) => Ok(await actions.HarvestAsync(AccountId, characterId, request.PlotId, ct));
    [HttpPost("cook")]
    public async Task<IActionResult> Cook(long characterId, RecipeRequest request, CancellationToken ct) => Ok(await actions.CookAsync(AccountId, characterId, request.RecipeId, ct));
    [HttpPost("fight")]
    public async Task<IActionResult> Fight(long characterId, MonsterRequest request, CancellationToken ct) => Ok(await actions.FightAsync(AccountId, characterId, request.MonsterId, ct));
    [HttpPost("learn")]
    public async Task<IActionResult> Learn(long characterId, LearnRequest request, CancellationToken ct) => Ok(await actions.LearnAsync(AccountId, characterId, request.NpcId, request.SkillId, ct));
    [HttpPost("rest")]
    public async Task<IActionResult> Rest(long characterId, CancellationToken ct) => Ok(await actions.RestAsync(AccountId, characterId, ct));
    [HttpPost("consume")]
    [HttpPost("use")]
    public async Task<IActionResult> Consume(long characterId, InventoryRequest request, CancellationToken ct) => Ok(await actions.ConsumeAsync(AccountId, characterId, request.InventoryId, ct));
    [HttpPost("equip")]
    public async Task<IActionResult> Equip(long characterId, InventoryRequest request, CancellationToken ct) => Ok(await actions.EquipAsync(AccountId, characterId, request.InventoryId, ct));
}

public sealed record MapRequest([Range(1, long.MaxValue)] long MapId);
public sealed record CropRequest([Range(1, long.MaxValue)] long CropId);
public sealed record PlotRequest([Range(1, long.MaxValue)] long PlotId);
public sealed record RecipeRequest([Range(1, long.MaxValue)] long RecipeId);
public sealed record MonsterRequest([Range(1, long.MaxValue)] long MonsterId);
public sealed record InventoryRequest([Range(1, long.MaxValue)] long InventoryId);
public sealed record LearnRequest([Range(1, long.MaxValue)] long NpcId, [Range(1, long.MaxValue)] long SkillId);
