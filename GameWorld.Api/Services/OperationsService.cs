using System.Security.Claims;
using System.Text.Json;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Services;

public class OperationsService(GameDbContext db)
{
    public static long AccountId(ClaimsPrincipal user) => long.Parse(user.FindFirstValue("accountId")!);
    public static long AdminId(ClaimsPrincipal user) => long.Parse(user.FindFirstValue("adminId")!);
    public void Audit(ClaimsPrincipal user, string action, object details, long? account = null, long? character = null) =>
        db.AdminActionLogs.Add(new AdminActionLog { AdminId = AdminId(user), ActionType = action,
            Details = JsonSerializer.Serialize(details), TargetAccountId = account, TargetCharacterId = character });
    public async Task<Character> OwnedCharacter(long accountId, long characterId) =>
        await db.Characters.Include(x => x.PlayerProfile).SingleOrDefaultAsync(x => x.Id == characterId && x.PlayerProfile.AccountId == accountId)
        ?? throw new KeyNotFoundException("Không tìm thấy nhân vật của bạn.");
    public Task GrantItem(long characterId, long itemId, int quantity) => InventoryService.GrantAsync(db, characterId, itemId, quantity);
    public void Gold(Character character, decimal amount, string reason, string source)
    {
        var balance = character.PlayerProfile.Gold + amount;
        if (balance < 0 || balance > 1_000_000_000_000m) throw new GameRuleException("Số dư vàng không hợp lệ hoặc không đủ vàng.");
        character.PlayerProfile.Gold = balance; character.PlayerProfile.UpdatedAt = DateTime.UtcNow;
        db.CurrencyLedgers.Add(new CurrencyLedger { CharacterId = character.Id, Amount = amount, Reason = reason, Source = source });
    }
}
