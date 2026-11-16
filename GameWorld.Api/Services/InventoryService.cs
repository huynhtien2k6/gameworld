using GameWorld.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Services;

/// <summary>Shared inventory grants. Call within the caller's economic transaction, then save once.</summary>
public static class InventoryService
{
    public const int Capacity = 80;

    public static async Task GrantAsync(GameDbContext db, long characterId, long itemId, int quantity, CancellationToken ct = default)
    {
        if (quantity is < 1 or > 10000) throw new ArgumentException("Số lượng phải từ 1 đến 10.000.");
        var item = await db.ItemTemplates.FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new KeyNotFoundException("Vật phẩm không tồn tại.");
        var rows = await db.CharacterInventories.Where(i => i.CharacterId == characterId).OrderBy(i => i.SlotIndex).ToListAsync(ct);
        // Earlier rewards can still be unsaved in the same transaction. They already occupy their slots.
        rows.AddRange(db.CharacterInventories.Local.Where(i => i.CharacterId == characterId && !rows.Contains(i)));
        var stack = Math.Max(1, item.MaxStack);
        foreach (var row in rows.Where(i => i.ItemId == itemId && !i.IsEquipped && i.Quantity > 0
            && i.EnchantLevel == 0 && i.Durability == null && i.Quantity < stack))
        {
            var add = Math.Min(quantity, stack - row.Quantity);
            row.Quantity += add; quantity -= add; row.UpdatedAt = DateTime.UtcNow;
            if (quantity == 0) return;
        }
        var occupied = rows.Where(i => i.Quantity > 0).Select(i => i.SlotIndex).ToHashSet();
        var activeStacks = rows.Count(i => i.Quantity > 0);
        while (quantity > 0)
        {
            var slot = Enumerable.Range(0, Capacity).FirstOrDefault(i => !occupied.Contains(i), -1);
            if (slot < 0 || activeStacks >= Capacity) throw new GameRuleException($"Túi đồ đã đầy ({Capacity} ô).");
            var add = Math.Min(quantity, stack);
            var empty = rows.FirstOrDefault(i => i.Quantity == 0 && i.SlotIndex == slot);
            if (empty != null)
            {
                empty.ItemId = itemId; empty.Item = item; empty.Quantity = add; empty.IsEquipped = false;
                empty.Durability = null; empty.EnchantLevel = 0; empty.UpdatedAt = DateTime.UtcNow;
            }
            else db.CharacterInventories.Add(new() { CharacterId = characterId, ItemId = itemId, Quantity = add, SlotIndex = slot });
            occupied.Add(slot); activeStacks++; quantity -= add;
        }
    }
}
