using System.Data;
using System.Security.Cryptography;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Services;

public sealed class PlayerActions(GameDbContext db, WorldSimulation world)
{
    public async Task<object> GetStateAsync(long accountId, long characterId, CancellationToken ct)
    {
        var character = await OwnAsync(accountId, characterId, ct);
        var map = await db.Maps.AsNoTracking().FirstAsync(m => m.Id == character.MapId, ct);
        var inventory = await db.CharacterInventories.AsNoTracking().Include(i => i.Item)
            .Where(i => i.CharacterId == characterId && i.Quantity > 0).OrderBy(i => i.SlotIndex)
            .Select(i => new { i.Id, i.ItemId, i.Item.Name, i.Quantity, i.Item.Category, i.Item.Rarity, i.IsEquipped }).ToListAsync(ct);
        var plots = await db.FarmingPlots.AsNoTracking().Include(p => p.Crop)
            .Where(p => p.CharacterId == characterId && !p.IsHarvested).OrderBy(p => p.Id).ToListAsync(ct);
        var skills = await db.SkillTemplates.AsNoTracking().Include(s => s.UnlockRequirements).OrderBy(s => s.Id).ToListAsync(ct);
        var learned = await db.CharacterSkills.AsNoTracking().Where(s => s.CharacterId == characterId && s.IsUnlocked).Select(s => s.SkillId).ToListAsync(ct);
        var logs = await db.CharacterActionLogs.AsNoTracking().Where(l => l.CharacterId == characterId)
            .GroupBy(l => new { l.ActionType, l.RegionId })
            .Select(g => new { g.Key.ActionType, g.Key.RegionId, Count = g.Count() }).ToListAsync(ct);
        var equippedWeapon = inventory.Any(i => i.ItemId == 6 && i.IsEquipped) ? 12 : 0;
        var npcs = await world.GetNpcsAsync(null, character.MapId, ct);
        if (learned.Contains(2))
            npcs = npcs.Select(n => n with { ShopItems = n.ShopItems.Select(s => s with
            {
                BuyPrice = Math.Round(s.BuyPrice * 0.95m, 2),
                SellPrice = Math.Floor(s.BuyPrice * 0.75m * 100) / 100m
            }).ToArray() }).ToArray();
        return new
        {
            Character = new
            {
                character.Id, character.Name, character.Level, character.Experience,
                ExperienceToNextLevel = character.Level * 100L,
                character.CurrentHP, MaxHP = MaxHP(character), character.CurrentMP,
                character.MapId, character.PlayerProfile.Gold,
                Stats = new { Attack = (character.Stats?.Attack ?? 20) + equippedWeapon, Defense = character.Stats?.Defense ?? 15 }
            },
            Map = WorldSimulation.ToMap(map), Weather = await world.GetWeatherAsync(map.RegionId, ct: ct),
            Inventory = inventory,
            Plots = plots.Select(p => new
            {
                p.Id, p.CropId, Name = p.Crop?.Name, p.RegionId,
                PlantTime = p.PlantTime is { } planted ? DateTime.SpecifyKind(planted, DateTimeKind.Utc) : (DateTime?)null,
                ReadyAt = p.ReadyAt is { } ready ? DateTime.SpecifyKind(ready, DateTimeKind.Utc) : (DateTime?)null,
                p.ExpectedYield, p.IsHarvested, Ready = p.ReadyAt <= DateTime.UtcNow
            }),
            Skills = skills.Select(s => new
            {
                s.Id, s.Name, s.Description, IsUnlocked = learned.Contains(s.Id),
                TrainerNpcId = s.UnlockRequirements.FirstOrDefault()?.NPCId,
                Requirements = s.UnlockRequirements.Select(r => new
                {
                    r.ActionType, r.RequiredCount,
                    CurrentCount = logs.Where(l => l.ActionType == r.ActionType && (r.RegionId == null || l.RegionId == r.RegionId)).Sum(l => l.Count)
                })
            }),
            Npcs = npcs,
            Monsters = WorldSimulation.Monsters.Where(m => m.MapId == character.MapId),
            Crops = await db.Crops.AsNoTracking().Select(c => new { c.Id, c.Name, c.GrowthTimeMinutes, c.BaseYield }).ToListAsync(ct),
            Recipes = WorldSimulation.Recipes,
            Maps = (await db.Maps.AsNoTracking().OrderBy(m => m.Id).ToListAsync(ct)).Select(WorldSimulation.ToMap),
            ServerTime = DateTime.UtcNow
        };
    }

    public Task<object> TravelAsync(long accountId, long characterId, long mapId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            await CooldownAsync(c.Id, "Travel", 1, ct);
            var map = await db.Maps.FirstOrDefaultAsync(m => m.Id == mapId, ct) ?? throw new KeyNotFoundException("Vùng đất không tồn tại.");
            if (c.MapId == map.Id) throw new GameRuleException("Bạn đang ở địa điểm này.");
            if (c.Level < map.MinLevel) throw new GameRuleException($"Cần cấp {map.MinLevel} để đến đây.");
            var weather = await world.GetWeatherAsync(map.RegionId, ct: ct);
            var skilled = await db.CharacterSkills.AnyAsync(s => s.CharacterId == c.Id && s.SkillId == 4 && s.IsUnlocked, ct);
            var cost = Math.Ceiling(map.TravelCost / weather.MovementModifier * (skilled ? 0.8m : 1m));
            ChangeGold(c, -cost, "Di chuyển", "Travel");
            c.MapId = map.Id; c.PositionX = c.PositionY = c.PositionZ = 0;
            c.PlayerProfile.CurrentMapId = map.Id; c.PlayerProfile.RegionId = map.RegionId;
            Log(c, "Travel", map.RegionId);
            return new { Message = $"Đã đến {map.Name}.", Cost = cost, MapId = map.Id };
        }, ct);

    public Task<object> TradeAsync(long accountId, long characterId, TradeRequest request, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            if (request.Quantity is < 1 or > 99) throw new GameRuleException("Số lượng phải từ 1 đến 99.");
            await CooldownAsync(c.Id, "Trade", 0.5, ct);
            var npc = await db.Npcs.FirstOrDefaultAsync(n => n.Id == request.NpcId && n.IsActive && n.MapId == c.MapId, ct)
                ?? throw new GameRuleException("Hãy đến đúng vị trí của thương nhân.");
            var entry = await db.NpcShopItems.Include(s => s.Item).FirstOrDefaultAsync(s => s.NpcId == npc.Id && s.ItemId == request.ItemId, ct)
                ?? throw new GameRuleException("Thương nhân không giao dịch vật phẩm này.");
            var quote = await world.QuoteAsync(entry.Item, npc.RegionId, ct);
            var skilled = await db.CharacterSkills.AnyAsync(s => s.CharacterId == c.Id && s.SkillId == 2 && s.IsUnlocked, ct);
            var price = request.Buy ? Math.Round(quote * (skilled ? 0.95m : 1m), 2) : Math.Floor(quote * (skilled ? 0.75m : 0.7m) * 100) / 100m;
            var total = price * request.Quantity;
            if (request.Buy)
            {
                if (!entry.IsSell || entry.Stock < request.Quantity) throw new GameRuleException("Cửa hàng không đủ hàng để bán.");
                ChangeGold(c, -total, $"Mua {entry.Item.Name}", "NpcTrade");
                await GrantAsync(c.Id, entry.ItemId, request.Quantity, ct);
                entry.Stock -= request.Quantity;
            }
            else
            {
                if (!entry.IsBuy || !entry.Item.IsSellable) throw new GameRuleException("Thương nhân không thu mua vật phẩm này.");
                await RemoveAsync(c.Id, entry.ItemId, request.Quantity, ct);
                ChangeGold(c, total, $"Bán {entry.Item.Name}", "NpcTrade");
                entry.Stock = checked(entry.Stock + request.Quantity);
            }
            Log(c, "Trade", npc.RegionId, npc.Id, entry.ItemId);
            db.MarketPriceHistories.Add(new() { RegionId = npc.RegionId, ItemId = entry.ItemId, Price = price });
            return new { Message = request.Buy ? "Mua hàng thành công." : "Bán hàng thành công.", UnitPrice = price, Total = total, Gold = c.PlayerProfile.Gold };
        }, ct);

    public Task<object> PlantAsync(long accountId, long characterId, long cropId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            var map = await db.Maps.Include(m => m.Region).FirstAsync(m => m.Id == c.MapId, ct);
            if (map.Region.Type != "Village" || !map.IsSafeZone) throw new GameRuleException("Hãy trở về ruộng tại Làng Tân Thủ để gieo trồng.");
            if (await db.FarmingPlots.CountAsync(p => p.CharacterId == c.Id && !p.IsHarvested, ct) >= 3)
                throw new GameRuleException("Bạn đã sử dụng đủ 3 ô ruộng. Hãy thu hoạch trước.");
            var crop = await db.Crops.FirstOrDefaultAsync(p => p.Id == cropId, ct) ?? throw new KeyNotFoundException("Giống cây không tồn tại.");
            if (crop.HarvestItemId == null) throw new GameRuleException("Giống cây này chưa thể thu hoạch.");
            if (!string.IsNullOrEmpty(crop.ClimateRequirement) && crop.ClimateRequirement != map.Region.Climate)
                throw new GameRuleException("Khí hậu không phù hợp với giống cây này.");
            await RemoveAsync(c.Id, crop.HarvestItemId.Value, 1, ct);
            var now = DateTime.UtcNow;
            var weather = await world.GetWeatherAsync(map.RegionId, now, ct);
            var skilled = await db.CharacterSkills.AnyAsync(s => s.CharacterId == c.Id && s.SkillId == 3 && s.IsUnlocked, ct);
            var plot = new FarmingPlot
            {
                CharacterId = c.Id, CropId = crop.Id, RegionId = map.RegionId,
                PlantTime = now, ReadyAt = now.AddMinutes(Math.Max(0.1, crop.GrowthTimeMinutes / (double)weather.CropModifier)),
                ExpectedYield = Math.Max(1, (int)Math.Floor(crop.BaseYield * weather.CropModifier * (skilled ? 1.25m : 1m))),
                Watered = weather.Type == "Rain", GrowthStage = 1
            };
            db.FarmingPlots.Add(plot);
            Log(c, "Plant", map.RegionId, itemId: crop.HarvestItemId);
            return new { Message = "Đã gieo trồng. Thời tiết và mùa ảnh hưởng thời gian lớn, sản lượng của cây.", plot.ReadyAt, plot.ExpectedYield };
        }, ct);

    public Task<object> HarvestAsync(long accountId, long characterId, long plotId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            var plot = await db.FarmingPlots.Include(p => p.Crop)
                .FirstOrDefaultAsync(p => p.Id == plotId && p.CharacterId == c.Id, ct) ?? throw new KeyNotFoundException("Ô ruộng không tồn tại.");
            if (plot.IsHarvested) throw new GameRuleException("Ô ruộng đã được thu hoạch.");
            var map = await db.Maps.FirstAsync(m => m.Id == c.MapId, ct);
            if (map.RegionId != plot.RegionId || !map.IsSafeZone) throw new GameRuleException("Hãy trở về làng có ô ruộng để thu hoạch.");
            if (plot.ReadyAt == null || plot.ReadyAt > DateTime.UtcNow) throw new GameRuleException("Cây chưa trưởng thành.");
            if (plot.Crop?.HarvestItemId == null) throw new GameRuleException("Chưa cấu hình nông sản cho giống cây này.");
            await GrantAsync(c.Id, plot.Crop.HarvestItemId.Value, plot.ExpectedYield, ct);
            plot.IsHarvested = true; plot.GrowthStage = 3;
            Log(c, "Harvest", plot.RegionId, itemId: plot.Crop.HarvestItemId);
            return new { Message = $"Thu hoạch {plot.ExpectedYield} {plot.Crop.Name}.", Quantity = plot.ExpectedYield };
        }, ct);

    public Task<object> CookAsync(long accountId, long characterId, long recipeId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            await CooldownAsync(c.Id, "Cook", 1, ct);
            var recipe = WorldSimulation.Recipes.FirstOrDefault(r => r.Id == recipeId) ?? throw new KeyNotFoundException("Công thức không tồn tại.");
            var chef = await db.Npcs.FirstOrDefaultAsync(n => n.Type == "Chef" && n.MapId == c.MapId && n.IsActive, ct)
                ?? throw new GameRuleException("Hãy đến bếp của Chủ Quán để nấu ăn.");
            foreach (var ingredient in recipe.Ingredients) await RemoveAsync(c.Id, ingredient.ItemId, ingredient.Quantity, ct);
            var skilled = await db.CharacterSkills.AnyAsync(s => s.CharacterId == c.Id && s.SkillId == 1 && s.IsUnlocked, ct);
            var quantity = recipe.Quantity + (skilled ? 1 : 0);
            await GrantAsync(c.Id, recipe.ResultItemId, quantity, ct);
            Log(c, "Cook", chef.RegionId, chef.Id, recipe.ResultItemId);
            return new { Message = $"Đã nấu {quantity} {recipe.Name}.", ItemId = recipe.ResultItemId, Quantity = quantity };
        }, ct);

    public Task<object> FightAsync(long accountId, long characterId, long monsterId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            await CooldownAsync(c.Id, "Fight", 2, ct);
            var monster = WorldSimulation.Monsters.FirstOrDefault(m => m.Id == monsterId && m.MapId == c.MapId)
                ?? throw new GameRuleException("Không có quái vật này tại vị trí hiện tại.");
            if (c.Level + 2 < monster.Level) throw new GameRuleException("Quái vật quá mạnh so với cấp hiện tại.");
            var map = await db.Maps.FirstAsync(m => m.Id == c.MapId, ct);
            if (map.IsSafeZone) throw new GameRuleException("Không thể chiến đấu trong vùng an toàn.");
            var weapon = await db.CharacterInventories.AnyAsync(i => i.CharacterId == c.Id && i.ItemId == 6 && i.IsEquipped && i.Quantity > 0, ct);
            var attack = Math.Max(1, (c.Stats?.Attack ?? 20) + (weapon ? 12 : 0));
            var rounds = (int)Math.Ceiling((double)monster.HP / attack);
            var weather = await world.GetWeatherAsync(map.RegionId, ct: ct);
            var weatherDamage = weather.Type is "Snow" or "Sandstorm" ? 3 : 0;
            var damage = Math.Max(2, monster.Attack - (c.Stats?.Defense ?? 15) / 2) * rounds + weatherDamage;
            c.CurrentHP = Math.Max(0, c.CurrentHP - damage);
            Log(c, "Fight", map.RegionId, itemId: monster.LootItemId);
            if (c.CurrentHP == 0)
            {
                var penalty = Math.Floor(c.PlayerProfile.Gold * 0.05m * 100) / 100m;
                ChangeGold(c, -penalty, "Hồi sinh sau thất bại", "Combat");
                c.MapId = 1; c.PlayerProfile.CurrentMapId = 1; c.PlayerProfile.RegionId = 1;
                c.CurrentHP = Math.Max(1, MaxHP(c) / 3);
                return new { Message = "Bạn đã gục ngã và được đưa về Làng Tân Thủ, mất 5% vàng.", Victory = false, Damage = damage, GoldLost = penalty };
            }
            ChangeGold(c, monster.Gold, $"Đánh bại {monster.Name}", "Combat");
            c.Experience += monster.Experience;
            while (c.Level < 60 && c.Experience >= c.Level * 100L)
            {
                c.Experience -= c.Level * 100L; c.Level++;
                if (c.Stats != null) { c.Stats.Attack += 3; c.Stats.Defense += 2; c.Stats.Vitality++; }
                c.CurrentHP = MaxHP(c);
            }
            c.PlayerProfile.Level = Math.Max(c.PlayerProfile.Level, c.Level);
            await GrantAsync(c.Id, monster.LootItemId, 1, ct);
            var bonusItemId = RandomNumberGenerator.GetInt32(100) < 8 ? 6L : (long?)null;
            if (bonusItemId != null) await GrantAsync(c.Id, bonusItemId.Value, 1, ct);
            return new { Message = $"Đánh bại {monster.Name}! +{monster.Experience} XP, +{monster.Gold} vàng.", Victory = true, Damage = damage, monster.Experience, monster.Gold, LootItemId = monster.LootItemId, BonusItemId = bonusItemId, c.Level };
        }, ct);

    public Task<object> LearnAsync(long accountId, long characterId, long npcId, long skillId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            var npc = await db.Npcs.FirstOrDefaultAsync(n => n.Id == npcId && n.IsActive && n.MapId == c.MapId, ct)
                ?? throw new GameRuleException("Hãy đến gặp người dạy kỹ năng.");
            var skill = await db.SkillTemplates.Include(s => s.UnlockRequirements).FirstOrDefaultAsync(s => s.Id == skillId, ct)
                ?? throw new KeyNotFoundException("Kỹ năng không tồn tại.");
            if (skill.UnlockRequirements.Count == 0 || skill.UnlockRequirements.Any(r => r.NPCId != null && r.NPCId != npc.Id))
                throw new GameRuleException("NPC này không dạy kỹ năng đó.");
            if (await db.CharacterSkills.AnyAsync(s => s.CharacterId == c.Id && s.SkillId == skillId && s.IsUnlocked, ct))
                throw new GameRuleException("Bạn đã học kỹ năng này.");
            foreach (var requirement in skill.UnlockRequirements)
            {
                // NPCId identifies the trainer; action counts come exclusively from successful server actions.
                var count = await db.CharacterActionLogs.CountAsync(l => l.CharacterId == c.Id && l.ActionType == requirement.ActionType
                    && (requirement.RegionId == null || l.RegionId == requirement.RegionId), ct);
                if (count < requirement.RequiredCount)
                    throw new GameRuleException($"Chưa đủ kinh nghiệm thực tế: {requirement.ActionType} {count}/{requirement.RequiredCount}.");
            }
            var learned = await db.CharacterSkills.FirstOrDefaultAsync(s => s.CharacterId == c.Id && s.SkillId == skillId, ct);
            if (learned == null) { learned = new() { CharacterId = c.Id, SkillId = skillId }; db.CharacterSkills.Add(learned); }
            learned.IsUnlocked = true; learned.LearnedAt = DateTime.UtcNow; learned.Level = 1;
            Log(c, "Learn", npc.RegionId, npc.Id);
            return new { Message = $"{npc.Name} đã truyền dạy kỹ năng {skill.Name}.", SkillId = skillId };
        }, ct);

    public Task<object> RestAsync(long accountId, long characterId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            if (!await db.Maps.AnyAsync(m => m.Id == c.MapId && m.IsSafeZone, ct)) throw new GameRuleException("Hãy trở về vùng an toàn để nghỉ ngơi.");
            c.CurrentHP = MaxHP(c); c.CurrentMP = 50 + (c.Level - 1) * 5;
            return new { Message = "Bạn đã nghỉ ngơi và hồi phục sức khỏe.", c.CurrentHP };
        }, ct);

    public Task<object> ConsumeAsync(long accountId, long characterId, long inventoryId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            var row = await db.CharacterInventories.Include(i => i.Item).FirstOrDefaultAsync(i => i.Id == inventoryId && i.CharacterId == c.Id && i.Quantity > 0, ct)
                ?? throw new KeyNotFoundException("Không có vật phẩm trong túi.");
            if (!row.Item.IsConsumable || row.ItemId is not (2 or 5 or 7)) throw new GameRuleException("Không thể dùng vật phẩm này để hồi máu.");
            if (c.CurrentHP >= MaxHP(c)) throw new GameRuleException("Sức khỏe đã đầy.");
            row.Quantity--; row.UpdatedAt = DateTime.UtcNow;
            c.CurrentHP = Math.Min(MaxHP(c), c.CurrentHP + (row.ItemId == 5 ? 50 : 25));
            return new { Message = $"Đã sử dụng {row.Item.Name}.", c.CurrentHP };
        }, ct);

    public Task<object> EquipAsync(long accountId, long characterId, long inventoryId, CancellationToken ct) =>
        ExecuteAsync(accountId, characterId, async c =>
        {
            var row = await db.CharacterInventories.Include(i => i.Item).FirstOrDefaultAsync(i => i.Id == inventoryId && i.CharacterId == c.Id && i.Quantity > 0, ct)
                ?? throw new KeyNotFoundException("Không có trang bị trong túi.");
            if (row.ItemId != 6) throw new GameRuleException("Vật phẩm này không phải trang bị.");
            var equip = !row.IsEquipped;
            foreach (var old in await db.CharacterInventories.Where(i => i.CharacterId == c.Id && i.IsEquipped).ToListAsync(ct)) old.IsEquipped = false;
            row.IsEquipped = equip;
            return new { Message = equip ? $"Đã trang bị {row.Item.Name}." : $"Đã tháo {row.Item.Name}.", IsEquipped = equip };
        }, ct);

    private async Task<Character> OwnAsync(long accountId, long characterId, CancellationToken ct) =>
        await db.Characters.Include(c => c.PlayerProfile).Include(c => c.Stats)
            .FirstOrDefaultAsync(c => c.Id == characterId && c.PlayerProfile.AccountId == accountId, ct)
        ?? throw new KeyNotFoundException("Không tìm thấy nhân vật thuộc tài khoản của bạn.");

    private async Task<object> ExecuteAsync(long accountId, long characterId, Func<Character, Task<object>> action, CancellationToken ct)
    {
        // Read/check/write happen within one serializable transaction to prevent duplicate loot and overspending.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var character = await OwnAsync(accountId, characterId, ct);
        if (character.CurrentHP <= 0) throw new GameRuleException("Nhân vật cần hồi sinh trước khi hoạt động.");
        var result = await action(character);
        character.UpdatedAt = DateTime.UtcNow; character.PlayerProfile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return result;
    }

    private async Task CooldownAsync(long characterId, string action, double seconds, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddSeconds(-seconds);
        if (await db.CharacterActionLogs.AnyAsync(l => l.CharacterId == characterId && l.ActionType == action && l.CreatedAt > since, ct))
            throw new GameRuleException("Thao tác quá nhanh, vui lòng chờ một chút.");
    }

    private void ChangeGold(Character character, decimal amount, string reason, string source)
    {
        var balance = character.PlayerProfile.Gold + amount;
        if (balance < 0) throw new GameRuleException("Không đủ vàng.");
        if (balance > 1_000_000_000_000m) throw new GameRuleException("Số dư vàng vượt giới hạn cho phép.");
        character.PlayerProfile.Gold = balance;
        if (amount != 0) db.CurrencyLedgers.Add(new() { CharacterId = character.Id, Amount = amount, Reason = reason, Source = source });
    }

    private void Log(Character character, string action, long? regionId = null, long? npcId = null, long? itemId = null) =>
        db.CharacterActionLogs.Add(new() { CharacterId = character.Id, ActionType = action, ActionValue = 1, RegionId = regionId, NPCId = npcId, ItemId = itemId });

    private async Task RemoveAsync(long characterId, long itemId, int quantity, CancellationToken ct)
    {
        var rows = await db.CharacterInventories.Where(i => i.CharacterId == characterId && i.ItemId == itemId && i.Quantity > 0 && !i.IsEquipped)
            .OrderBy(i => i.SlotIndex).ToListAsync(ct);
        if (rows.Sum(i => (long)i.Quantity) < quantity) throw new GameRuleException("Không đủ nguyên liệu/vật phẩm trong túi.");
        foreach (var row in rows)
        {
            var take = Math.Min(quantity, row.Quantity); row.Quantity -= take; quantity -= take; row.UpdatedAt = DateTime.UtcNow;
            if (quantity == 0) break;
        }
    }

    private Task GrantAsync(long characterId, long itemId, int quantity, CancellationToken ct) =>
        InventoryService.GrantAsync(db, characterId, itemId, quantity, ct);

    private static int MaxHP(Character character) => 100 + (character.Level - 1) * 20;
}

public sealed record TradeRequest(long NpcId, long ItemId, int Quantity, bool Buy);
