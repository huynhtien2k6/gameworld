using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using GameWorld.Api.Services;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "Admin")]
public class AdminController(GameDbContext db, OperationsService ops) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview()
    {
        var now = DateTime.UtcNow; var recent = now.AddMinutes(-2); var start = now.Date.AddDays(-29);
        var revenue = await db.DailyRevenues.AsNoTracking().Where(x => x.RevenueDate >= start).ToListAsync();
        var gold = await db.PlayerProfiles.Select(x => x.Gold).ToListAsync();
        return Ok(new { accounts = await db.Accounts.CountAsync(), characters = await db.Characters.CountAsync(),
            ccu = await db.AccountSessions.Where(x => x.IsActive && x.RevokedAt == null && x.ExpiresAt > now && x.LastSeenAt >= recent)
                .Select(x => x.AccountId).Distinct().CountAsync(),
            revenueToday = revenue.Where(x => x.RevenueDate.Date == now.Date).Sum(x => x.Amount), totalGold = gold.Sum(),
            openTickets = await db.SupportTickets.CountAsync(x => x.Status != 2), errorsToday = await db.ErrorLogs.CountAsync(x => x.Level == "Error" && x.CreatedAt >= now.Date),
            serverTime = now, uptimeSeconds = (long)(now - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds,
            revenue = Enumerable.Range(0, 30).Select(i => new { date = start.AddDays(i).ToString("yyyy-MM-dd"), amount = revenue.Where(x => x.RevenueDate.Date == start.AddDays(i)).Sum(x => x.Amount) }),
            logs = await db.ErrorLogs.AsNoTracking().OrderByDescending(x => x.Id).Take(50).Select(x => new { x.Id, x.Level, x.Source, x.Message, x.CreatedAt }).ToListAsync() });
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts([FromQuery, StringLength(100)] string? search)
    {
        var query = db.Accounts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) { search = search.Trim().ToLowerInvariant(); query = query.Where(x => x.Username.Contains(search) || x.Email.Contains(search)); }
        return Ok(await query.OrderByDescending(x => x.Id).Take(100).Select(x => new { x.Id, x.Username, x.Email, x.Status, x.LastLoginAt,
            gold = x.PlayerProfile == null ? 0 : x.PlayerProfile.Gold,
            characters = x.PlayerProfile!.Characters.Select(c => new { c.Id, c.Name, c.Level, c.MapId }) }).ToListAsync());
    }

    [HttpPost("accounts/{id:long}/ban")]
    public async Task<IActionResult> Ban(long id, BanRequest request)
    {
        if (request.Until <= DateTime.UtcNow) return BadRequest(new { message = "Thời điểm hết hạn khóa phải trong tương lai." });
        var account = await db.Accounts.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        var now = DateTime.UtcNow;
        account.Status = 2; account.UpdatedAt = now;
        db.AccountBans.Add(new AccountBan { AccountId = id, BannedByAdminId = OperationsService.AdminId(User), Reason = request.Reason.Trim(),
            BannedUntil = request.Until, IsPermanent = request.Until == null });
        foreach (var session in await db.AccountSessions.Where(x => x.AccountId == id && x.IsActive).ToListAsync())
        { session.IsActive = false; session.RevokedAt = now; }
        ops.Audit(User, "Ban", request, id); await db.SaveChangesAsync();
        return Ok(new { message = "Đã khóa tài khoản và thu hồi các phiên đăng nhập." });
    }

    [HttpPost("accounts/{id:long}/unban")]
    public async Task<IActionResult> Unban(long id, ReasonRequest request)
    {
        var account = await db.Accounts.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");
        foreach (var ban in await db.AccountBans.Where(x => x.AccountId == id && x.LiftedAt == null).ToListAsync())
        { ban.LiftedAt = DateTime.UtcNow; ban.LiftedByAdminId = OperationsService.AdminId(User); }
        account.Status = 0; account.UpdatedAt = DateTime.UtcNow;
        ops.Audit(User, "Unban", request, id); await db.SaveChangesAsync(); return Ok(new { message = "Đã mở khóa tài khoản." });
    }

    [HttpPost("characters/{id:long}/restore")]
    public async Task<IActionResult> Restore(long id, RestoreRequest request)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await db.Characters.Include(x => x.PlayerProfile).SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Không tìm thấy nhân vật.");
        await ops.GrantItem(id, request.ItemId, request.Quantity);
        ops.Audit(User, "RestoreItem", request, character.PlayerProfile.AccountId, id);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Đã khôi phục vật phẩm và ghi nhật ký quản trị." });
    }

    [HttpPost("characters/{id:long}/currency")]
    public async Task<IActionResult> Currency(long id, CurrencyRequest request)
    {
        if (request.Amount == 0 || decimal.Round(request.Amount, 2) != request.Amount) return BadRequest(new { message = "Số tiền phải khác 0 và có tối đa 2 chữ số thập phân." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await db.Characters.Include(x => x.PlayerProfile).SingleOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException("Không tìm thấy nhân vật.");
        ops.Gold(character, request.Amount, request.Reason, "Admin");
        ops.Audit(User, "AdjustGold", request, character.PlayerProfile.AccountId, id);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Đã điều chỉnh vàng.", gold = character.PlayerProfile.Gold });
    }

    [HttpGet("items")]
    public async Task<IActionResult> Items() => Ok(await db.ItemTemplates.AsNoTracking().OrderBy(x => x.Id)
        .Select(x => new { x.Id, x.Name, x.Category, x.Rarity, x.BasePrice }).ToListAsync());

    [HttpPut("items/{id:long}/price")]
    public async Task<IActionResult> Price(long id, PriceRequest request)
    {
        if (decimal.Round(request.Price, 2) != request.Price) return BadRequest(new { message = "Giá có tối đa 2 chữ số thập phân." });
        var item = await db.ItemTemplates.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy vật phẩm.");
        var old = item.BasePrice; item.BasePrice = request.Price;
        ops.Audit(User, "ItemPrice", new { itemId = id, before = old, after = request.Price, request.Reason });
        await db.SaveChangesAsync(); return Ok(new { message = "Đã cập nhật giá gốc. Giá tại từng vùng được tính lại ngay." });
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets() => Ok(await db.SupportTickets.AsNoTracking().OrderByDescending(x => x.Id).Take(100)
        .Select(x => new { x.Id, x.AccountId, x.CharacterId, x.Subject, x.Message, x.Category, x.Status, x.CreatedAt,
            messages = x.Messages.OrderBy(m => m.Id).Select(m => new { m.Id, m.SenderType, m.Message, m.CreatedAt }) }).ToListAsync());

    [HttpPost("tickets/{id:long}/reply")]
    public async Task<IActionResult> Reply(long id, TicketReplyRequest request)
    {
        var ticket = await db.SupportTickets.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu hỗ trợ.");
        ticket.AssignedAdminId = OperationsService.AdminId(User); ticket.Status = request.Resolve ? 2 : 1;
        ticket.ResolvedAt = request.Resolve ? DateTime.UtcNow : null;
        db.SupportTicketMessages.Add(new SupportTicketMessage { TicketId = id, SenderType = "Admin", SenderId = OperationsService.AdminId(User), Message = request.Message });
        ops.Audit(User, "SupportReply", new { ticketId = id, request.Resolve }, ticket.AccountId, ticket.CharacterId);
        await db.SaveChangesAsync(); return Ok(new { message = "Đã gửi phản hồi hỗ trợ." });
    }

    [HttpGet("events")]
    public async Task<IActionResult> Events() => Ok(await db.LiveEvents.AsNoTracking().OrderByDescending(x => x.Id).Take(100).ToListAsync());
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        CheckDates(request.StartsAt, request.EndsAt);
        var entry = new LiveEvent { Name = request.Name.Trim(), Description = request.Description, StartsAt = request.StartsAt, EndsAt = request.EndsAt };
        db.LiveEvents.Add(entry); ops.Audit(User, "CreateEvent", request); await db.SaveChangesAsync(); return Ok(new { entry.Id, message = "Đã lên lịch sự kiện." });
    }
    [HttpPut("events/{id:long}/toggle")]
    public async Task<IActionResult> ToggleEvent(long id, ToggleRequest request)
    {
        var entry = await db.LiveEvents.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy sự kiện.");
        entry.IsActive = request.IsActive; ops.Audit(User, "ToggleEvent", new { id, request.IsActive });
        await db.SaveChangesAsync(); return Ok(new { message = "Đã thay đổi trạng thái sự kiện." });
    }
    [HttpGet("announcements")]
    public async Task<IActionResult> Announcements() => Ok(await db.MaintenanceAnnouncements.AsNoTracking().OrderByDescending(x => x.Id).Take(100)
        .Select(x => new { x.Id, x.Title, x.Content, x.StartAt, x.EndAt, x.IsActive }).ToListAsync());
    [HttpPost("announcements")]
    public async Task<IActionResult> Announcement(AnnouncementRequest request)
    {
        CheckDates(request.StartAt, request.EndAt);
        db.MaintenanceAnnouncements.Add(new MaintenanceAnnouncement { Title = request.Title, Content = request.Content, StartAt = request.StartAt,
            EndAt = request.EndAt, CreatedByAdminId = OperationsService.AdminId(User) });
        ops.Audit(User, "SystemMail", request); await db.SaveChangesAsync(); return Ok(new { message = "Đã gửi thư hệ thống toàn máy chủ." });
    }
    [HttpPut("announcements/{id:long}/toggle")]
    public async Task<IActionResult> ToggleAnnouncement(long id, ToggleRequest request)
    {
        var entry = await db.MaintenanceAnnouncements.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy thư.");
        entry.IsActive = request.IsActive; ops.Audit(User, "ToggleMail", new { id, request.IsActive }); await db.SaveChangesAsync(); return Ok(new { message = "Đã cập nhật thư." });
    }
    [HttpGet("giftcodes")]
    public async Task<IActionResult> GiftCodes() => Ok(await db.GiftCodes.AsNoTracking().OrderByDescending(x => x.Id).Take(100)
        .Select(x => new { x.Id, x.Code, x.RewardType, x.RewardItemId, x.RewardAmount, x.MaxUsage, x.UsedCount, x.ValidFrom, x.ValidTo, x.IsActive }).ToListAsync());
    [HttpPost("giftcodes")]
    public async Task<IActionResult> CreateGift(GiftRequest request)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (request.ValidTo <= DateTime.UtcNow) return BadRequest(new { message = "Giftcode phải còn hiệu lực trong tương lai." });
        if (request.RewardType == "Item" && (!request.RewardItemId.HasValue || !await db.ItemTemplates.AnyAsync(x => x.Id == request.RewardItemId)))
            return BadRequest(new { message = "Vật phẩm thưởng không tồn tại." });
        if (await db.GiftCodes.AnyAsync(x => x.Code == code)) return Conflict(new { message = "Giftcode đã tồn tại." });
        db.GiftCodes.Add(new GiftCode { Code = code, RewardType = request.RewardType, RewardItemId = request.RewardType == "Item" ? request.RewardItemId : null,
            RewardAmount = request.RewardAmount, MaxUsage = request.MaxUsage, ValidTo = request.ValidTo, CreatedByAdminId = OperationsService.AdminId(User) });
        ops.Audit(User, "CreateGiftCode", request); await db.SaveChangesAsync(); return Ok(new { message = "Đã phát hành Giftcode. Mỗi tài khoản chỉ nhận một lần." });
    }
    [HttpGet("gacha")]
    public async Task<IActionResult> Gacha() => Ok(await db.GachaEntries.AsNoTracking().OrderBy(x => x.Id)
        .Select(x => new { x.Id, x.ItemId, itemName = x.Item.Name, x.Weight, x.IsActive }).ToListAsync());
    [HttpPut("gacha/{id:long}")]
    public async Task<IActionResult> GachaRate(long id, GachaRequest request)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entry = await db.GachaEntries.FindAsync(id) ?? throw new KeyNotFoundException("Không tìm thấy phần thưởng.");
        if ((!request.IsActive || request.Weight == 0) && !await db.GachaEntries.AnyAsync(x => x.Id != id && x.IsActive && x.Weight > 0))
            return BadRequest(new { message = "Phải giữ ít nhất một phần thưởng có trọng số dương." });
        ops.Audit(User, "GachaRate", new { id, oldWeight = entry.Weight, request.Weight, request.IsActive, request.Reason });
        entry.Weight = request.Weight; entry.IsActive = request.IsActive;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Đã cập nhật trọng số rơi đồ." });
    }
    [HttpGet("audit")]
    public async Task<IActionResult> Audit() => Ok(await db.AdminActionLogs.AsNoTracking().OrderByDescending(x => x.Id).Take(100)
        .Select(x => new { x.Id, x.AdminId, x.TargetAccountId, x.TargetCharacterId, x.ActionType, x.Details, x.CreatedAt }).ToListAsync());
    [HttpPost("economy-events")]
    public async Task<IActionResult> Economy(EconomyRequest request)
    {
        CheckDates(request.StartsAt, request.EndsAt);
        if (!await db.Regions.AnyAsync(x => x.Id == request.RegionId) || !await db.ItemTemplates.AnyAsync(x => x.Id == request.ItemId))
            return BadRequest(new { message = "Vùng hoặc vật phẩm không tồn tại." });
        db.EconomyEvents.Add(new EconomyEvent { RegionId = request.RegionId, ItemId = request.ItemId, Modifier = request.Modifier,
            EventType = request.EventType, Description = request.Description, StartsAt = request.StartsAt, EndsAt = request.EndsAt });
        ops.Audit(User, "EconomyEvent", request); await db.SaveChangesAsync(); return Ok(new { message = "Đã cập nhật biến động kinh tế vùng." });
    }
    private static void CheckDates(DateTime starts, DateTime? ends)
    { if (ends <= starts) throw new ArgumentException("Thời điểm kết thúc phải sau thời điểm bắt đầu."); }
}

public record ReasonRequest([Required, StringLength(500, MinimumLength = 3)] string Reason);
public record BanRequest([Required, StringLength(500, MinimumLength = 3)] string Reason, DateTime? Until = null);
public record RestoreRequest(long ItemId, [Range(1, 10000)] int Quantity, [Required, StringLength(500, MinimumLength = 3)] string Reason);
public record CurrencyRequest([Range(typeof(decimal), "-1000000000", "1000000000")] decimal Amount, [Required, StringLength(500, MinimumLength = 3)] string Reason);
public record PriceRequest([Range(typeof(decimal), "0.01", "1000000")] decimal Price, [Required, StringLength(500, MinimumLength = 3)] string Reason);
public record TicketReplyRequest([Required, StringLength(3000, MinimumLength = 1)] string Message, bool Resolve);
public record ToggleRequest(bool IsActive);
public record EventRequest([Required, StringLength(150)] string Name, [StringLength(1000)] string Description, DateTime StartsAt, DateTime? EndsAt = null);
public record AnnouncementRequest([Required, StringLength(200)] string Title, [Required, StringLength(3000)] string Content, DateTime StartAt, DateTime? EndAt = null);
public record GiftRequest([Required, RegularExpression(@"^[a-zA-Z0-9_-]{3,50}$")] string Code,
    [Required, RegularExpression("^(Gold|Item)$")] string RewardType, [Range(1, 10000)] int RewardAmount,
    [Range(1, 1000000)] int MaxUsage, long? RewardItemId = null, DateTime? ValidTo = null);
public record GachaRequest([Range(0, 1000000)] int Weight, bool IsActive, [Required, StringLength(500, MinimumLength = 3)] string Reason);
public record EconomyRequest(long RegionId, long ItemId, [Range(typeof(decimal), "0.1", "10")] decimal Modifier,
    [Required, StringLength(50)] string EventType, [StringLength(255)] string Description, DateTime StartsAt, DateTime? EndsAt = null);
