using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GameWorld.Api.Services;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Controllers;

[ApiController, Route("api/presence"), Authorize(Policy = "Player")]
public sealed class PresenceController(GameDbContext db, WorldSimulation world, WorldPresence presence) : ControllerBase
{
    private long AccountId => OperationsService.AccountId(User);
    private long SessionId => long.TryParse(User.FindFirstValue("sessionId"), out var id) ? id : throw new UnauthorizedAccessException("Phiên người chơi không hợp lệ.");

    [HttpPost("update")]
    public async Task<IActionResult> Update(PresenceUpdateRequest request, CancellationToken ct)
    {
        var actor = await OwnAsync(request.CharacterId, ct);
        var regionId = await db.Maps.Where(x => x.Id == actor.MapId).Select(x => x.RegionId).SingleAsync(ct);
        var weather = await world.GetWeatherAsync(regionId, ct: ct);
        return Ok(presence.Update(actor, SessionId, request.X, request.Z, weather.MovementModifier));
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> Nearby([FromQuery, Range(1, long.MaxValue)] long characterId, CancellationToken ct)
    {
        var actor = await OwnAsync(characterId, ct);
        var candidates = presence.Candidates(actor.MapId, actor.CharacterId);
        var sessionIds = candidates.Select(x => x.SessionId).Distinct().ToArray();
        var now = DateTime.UtcNow; var recent = now.AddMinutes(-2);
        // Logout and ban revoke database sessions, so stale in-memory avatars disappear promptly.
        var activeSessions = await db.AccountSessions.AsNoTracking()
            .Where(x => sessionIds.Contains(x.Id) && x.IsActive && x.RevokedAt == null && x.ExpiresAt > now && x.LastSeenAt >= recent)
            .Select(x => x.Id).ToListAsync(ct);
        var active = activeSessions.ToHashSet();
        var characterIds = candidates.Select(x => x.CharacterId).ToArray();
        var stillInMap = (await db.Characters.AsNoTracking()
            .Where(x => characterIds.Contains(x.Id) && x.MapId == actor.MapId).Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        return Ok(new { players = candidates.Where(x => active.Contains(x.SessionId) && stillInMap.Contains(x.CharacterId))
            .Select(x => new { x.CharacterId, x.Name, x.Level, x.MapId, x.X, x.Z }),
            messages = presence.Messages(actor.MapId), serverTime = now });
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(PresenceChatRequest request, CancellationToken ct)
    {
        var actor = await OwnAsync(request.CharacterId, ct);
        return Ok(presence.Chat(actor, SessionId, request.Text));
    }

    private async Task<PresenceActor> OwnAsync(long characterId, CancellationToken ct) =>
        await db.Characters.AsNoTracking().Where(x => x.Id == characterId && x.PlayerProfile.AccountId == AccountId)
            .Select(x => new PresenceActor(x.Id, AccountId, x.Name, x.Level, x.MapId, x.CurrentHP, x.PositionX, x.PositionZ))
            .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Không tìm thấy nhân vật của bạn.");
}

public sealed record PresenceUpdateRequest([Range(1, long.MaxValue)] long CharacterId, [Range(-42, 42)] double X, [Range(-42, 42)] double Z);
public sealed record PresenceChatRequest([Range(1, long.MaxValue)] long CharacterId, [Required, StringLength(200, MinimumLength = 1)] string Text);
