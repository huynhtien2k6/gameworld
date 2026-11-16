using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using GameWorld.Api.Services;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Controllers;

[ApiController, Route("api/player"), Authorize(Policy = "Player")]
public class PlayerController(GameDbContext db, OperationsService ops) : ControllerBase
{
    private long AccountId => OperationsService.AccountId(User);
    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets() => Ok(await db.SupportTickets.AsNoTracking().Where(x => x.AccountId == AccountId).OrderByDescending(x => x.Id).Take(100)
        .Select(x => new { x.Id, x.Subject, x.Message, x.Category, x.Status, x.CreatedAt,
            messages = x.Messages.OrderBy(m => m.Id).Select(m => new { m.SenderType, m.Message, m.CreatedAt }) }).ToListAsync());
    [HttpPost("tickets")]
    public async Task<IActionResult> Ticket(CreateTicketRequest request)
    {
        if (request.CharacterId.HasValue) await ops.OwnedCharacter(AccountId, request.CharacterId.Value);
        if (await db.SupportTickets.CountAsync(x => x.AccountId == AccountId && x.Status != 2) >= 10)
            return BadRequest(new { message = "Bạn đã có 10 yêu cầu đang chờ xử lý." });
        var ticket = new SupportTicket { AccountId = AccountId, CharacterId = request.CharacterId, Category = request.Category, Subject = request.Subject, Message = request.Message };
        db.SupportTickets.Add(ticket); await db.SaveChangesAsync(); return Ok(new { ticket.Id, message = "Đã gửi yêu cầu hỗ trợ." });
    }
    [HttpGet("mail")]
    public async Task<IActionResult> Mail()
    {
        var now = DateTime.UtcNow;
        return Ok(await db.MaintenanceAnnouncements.AsNoTracking().Where(x => x.IsActive && x.StartAt <= now && (x.EndAt == null || x.EndAt > now))
            .OrderByDescending(x => x.Id).Take(100).Select(x => new { x.Id, x.Title, x.Content, x.StartAt }).ToListAsync());
    }
    [HttpGet("events")]
    public async Task<IActionResult> Events()
    {
        var now = DateTime.UtcNow;
        return Ok(await db.LiveEvents.AsNoTracking().Where(x => x.IsActive && x.StartsAt <= now && (x.EndsAt == null || x.EndsAt > now)).ToListAsync());
    }
    [HttpPost("giftcodes/redeem")]
    public async Task<IActionResult> Redeem(RedeemRequest request)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await ops.OwnedCharacter(AccountId, request.CharacterId); var now = DateTime.UtcNow;
        var code = request.Code.Trim().ToUpperInvariant();
        var gift = await db.GiftCodes.SingleOrDefaultAsync(x => x.Code == code && x.IsActive && x.ValidFrom <= now && (x.ValidTo == null || x.ValidTo > now))
            ?? throw new KeyNotFoundException("Giftcode không tồn tại hoặc hết hạn.");
        if (gift.UsedCount >= gift.MaxUsage) return BadRequest(new { message = "Giftcode đã hết lượt nhận." });
        if (await db.GiftCodeUsages.AnyAsync(x => x.GiftCodeId == gift.Id && x.AccountId == AccountId))
            return Conflict(new { message = "Tài khoản đã nhận Giftcode này." });
        if (gift.RewardType == "Gold") ops.Gold(character, gift.RewardAmount, "Giftcode " + gift.Code, "GiftCode");
        else if (gift.RewardType == "Item" && gift.RewardItemId.HasValue) await ops.GrantItem(character.Id, gift.RewardItemId.Value, gift.RewardAmount);
        else throw new GameRuleException("Phần thưởng chưa được cấu hình hợp lệ.");
        gift.UsedCount++; db.GiftCodeUsages.Add(new GiftCodeUsage { GiftCodeId = gift.Id, AccountId = AccountId, CharacterId = character.Id });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Đã nhận quà thành công.", gift.RewardType, gift.RewardAmount });
    }
    [HttpGet("gacha")]
    public async Task<IActionResult> Rates()
    {
        var entries = await db.GachaEntries.AsNoTracking().Where(x => x.IsActive && x.Weight > 0)
            .Select(x => new { x.ItemId, itemName = x.Item.Name, x.Weight }).ToListAsync();
        var total = entries.Sum(x => x.Weight);
        return Ok(new { cost = 20, currency = "Gold", rewards = entries.Select(x => new { x.ItemId, x.itemName, x.Weight, chance = total == 0 ? 0 : (decimal)x.Weight / total }) });
    }
    [HttpPost("gacha/pull")]
    public async Task<IActionResult> Pull(CharacterIdRequest request)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await ops.OwnedCharacter(AccountId, request.CharacterId);
        var recent = DateTime.UtcNow.AddSeconds(-2);
        if (await db.CharacterActionLogs.AnyAsync(x => x.CharacterId == character.Id && x.ActionType == "Gacha" && x.CreatedAt > recent))
            return BadRequest(new { message = "Hãy đợi 2 giây giữa các lượt quay." });
        var entries = await db.GachaEntries.Include(x => x.Item).Where(x => x.IsActive && x.Weight > 0).ToListAsync();
        var total = entries.Sum(x => x.Weight);
        if (total <= 0) throw new GameRuleException("Vòng quay đang tạm đóng.");
        var roll = RandomNumberGenerator.GetInt32(total); var reward = entries[0];
        foreach (var entry in entries) { roll -= entry.Weight; if (roll < 0) { reward = entry; break; } }
        ops.Gold(character, -20, "Vòng quay Gacha", "Gacha"); await ops.GrantItem(character.Id, reward.ItemId, 1);
        db.CharacterActionLogs.Add(new CharacterActionLog { CharacterId = character.Id, ActionType = "Gacha", ActionValue = 1, ItemId = reward.ItemId });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Bạn nhận được " + reward.Item.Name, reward.ItemId, itemName = reward.Item.Name, quantity = 1 });
    }
}
public record CreateTicketRequest([Required, StringLength(150)] string Subject, [Required, StringLength(3000)] string Message,
    [Required, StringLength(50)] string Category, long? CharacterId = null);
public record RedeemRequest(long CharacterId, [Required, StringLength(50)] string Code);
public record CharacterIdRequest(long CharacterId);
