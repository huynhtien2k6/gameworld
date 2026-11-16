using GameWorld.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Infrastructure.Data
{
    public static class SeedData
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<Map>().HasData(new Map { Id = 5, RegionId = 1, Name = "Rừng Ven Làng", Type = "Forest", IsSafeZone = false, MinLevel = 1, TravelCost = 0, CreatedAt = created });
            modelBuilder.Entity<ItemTemplate>().HasData(
                new ItemTemplate { Id = 6, Name = "Kiếm Lữ Hành", Category = "Equipment", Rarity = "Rare", BasePrice = 120, MaxStack = 1, CreatedAt = created },
                new ItemTemplate { Id = 7, Name = "Thảo Dược", Category = "Material", BasePrice = 15, MaxStack = 99, CreatedAt = created });
            modelBuilder.Entity<Crop>().HasData(new Crop { Id = 1, Name = "Lúa Mì", GrowthTimeMinutes = 1, BaseYield = 4, HarvestItemId = 1, ClimateRequirement = "Temperate", CreatedAt = created });
            modelBuilder.Entity<Npc>().HasData(new Npc { Id = 5, Name = "Bác Năm", Type = "Farmer", RegionId = 1, MapId = 1, PositionX = 8, IsActive = true });
            modelBuilder.Entity<NpcShopItem>().HasData(
                new NpcShopItem { Id = 1, NpcId = 1, ItemId = 1, Price = 25, Stock = 10000, IsBuy = true },
                new NpcShopItem { Id = 2, NpcId = 1, ItemId = 2, Price = 35, Stock = 10000, IsBuy = true },
                new NpcShopItem { Id = 3, NpcId = 1, ItemId = 3, Price = 20, Stock = 10000, IsBuy = true },
                new NpcShopItem { Id = 4, NpcId = 2, ItemId = 1, Price = 25, Stock = 10000, IsBuy = true },
                new NpcShopItem { Id = 5, NpcId = 2, ItemId = 6, Price = 120, Stock = 1000, IsBuy = true },
                new NpcShopItem { Id = 6, NpcId = 4, ItemId = 5, Price = 45, Stock = 10000, IsBuy = true });
            modelBuilder.Entity<SkillUnlockRequirement>().HasData(
                new SkillUnlockRequirement { Id = 1, SkillId = 1, ActionType = "Cook", RequiredCount = 10, NPCId = 4 },
                new SkillUnlockRequirement { Id = 2, SkillId = 2, ActionType = "Trade", RequiredCount = 50, NPCId = 1 },
                new SkillUnlockRequirement { Id = 3, SkillId = 3, ActionType = "Harvest", RequiredCount = 20, NPCId = 5 },
                new SkillUnlockRequirement { Id = 4, SkillId = 4, ActionType = "Travel", RequiredCount = 25, NPCId = 3 });
            modelBuilder.Entity<GachaEntry>().HasData(
                new GachaEntry { Id = 1, ItemId = 1, Weight = 70 },
                new GachaEntry { Id = 2, ItemId = 7, Weight = 25 },
                new GachaEntry { Id = 3, ItemId = 6, Weight = 5 });
            var seasonal = new List<RegionSeasonData>();
            for (var region = 1; region <= 4; region++)
                for (var season = 1; season <= 4; season++)
                    seasonal.Add(new RegionSeasonData { Id = (region - 1) * 4 + season, RegionId = region, SeasonId = season,
                        CropModifier = season == 4 ? 0.5m : season == 1 ? 1.25m : 1m,
                        TravelModifier = season == 4 ? 0.8m : 1m });
            modelBuilder.Entity<RegionSeasonData>().HasData(seasonal);
            modelBuilder.Entity<Region>().HasData(
                new Region { Id = 1, Name = "Làng Tân Thủ", Type = "Village", Climate = "Temperate", Description = "Làng nông thôn nơi bắt đầu cuộc hành trình.", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Region { Id = 2, Name = "Thành Phố Pháo Đài", Type = "City", Climate = "Temperate", Description = "Thị trường sầm uất và giao thương lớn.", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Region { Id = 3, Name = "Đồi Núi Hắc Ám", Type = "Mountain", Climate = "Cold", Description = "Nơi quái vật mạnh và kho báu ẩn giấu.", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Region { Id = 4, Name = "Sa Mạc Gió Hè", Type = "Desert", Climate = "Dry", Description = "Nông trại và thương nhân phải đối mặt với cát nóng.", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<Map>().HasData(
                new Map { Id = 1, RegionId = 1, Name = "Làng Tân Thủ", Type = "SafeZone", IsSafeZone = true, MinLevel = 1, TravelCost = 0, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Map { Id = 2, RegionId = 2, Name = "Chợ Trung Tâm", Type = "City", IsSafeZone = false, MinLevel = 1, TravelCost = 10, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Map { Id = 3, RegionId = 3, Name = "Đỉnh Núi", Type = "Mountain", IsSafeZone = false, MinLevel = 10, TravelCost = 20, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Map { Id = 4, RegionId = 4, Name = "Bãi Cát", Type = "Desert", IsSafeZone = false, MinLevel = 15, TravelCost = 25, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<ItemTemplate>().HasData(
                new ItemTemplate { Id = 1, Name = "Lúa Mì", Category = "Resource", Rarity = "Common", BasePrice = 25, MaxStack = 99, IsTradeable = true, IsSellable = true, IsConsumable = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ItemTemplate { Id = 2, Name = "Thịt Săn", Category = "Food", Rarity = "Common", BasePrice = 35, MaxStack = 50, IsTradeable = true, IsSellable = true, IsConsumable = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ItemTemplate { Id = 3, Name = "Gỗ", Category = "Material", Rarity = "Common", BasePrice = 20, MaxStack = 99, IsTradeable = true, IsSellable = true, IsConsumable = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ItemTemplate { Id = 4, Name = "Đồng Xu", Category = "Currency", Rarity = "Common", BasePrice = 1, MaxStack = 999999, IsTradeable = false, IsSellable = false, IsConsumable = false, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new ItemTemplate { Id = 5, Name = "Bánh Nướng", Category = "Food", Rarity = "Common", BasePrice = 45, MaxStack = 50, IsTradeable = true, IsSellable = true, IsConsumable = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            modelBuilder.Entity<SkillTemplate>().HasData(
                new SkillTemplate { Id = 1, Name = "Nấu Ăn", Category = "Crafting", Description = "Biết cách nấu ăn.", UnlockType = "Action", UnlockRequirementValue = 10 },
                new SkillTemplate { Id = 2, Name = "Thương Lượng", Category = "Trade", Description = "Biết cách mặc cả.", UnlockType = "Action", UnlockRequirementValue = 50 },
                new SkillTemplate { Id = 3, Name = "Trồng Trọt", Category = "Survival", Description = "Biết cách trồng trọt.", UnlockType = "Action", UnlockRequirementValue = 20 },
                new SkillTemplate { Id = 4, Name = "Lái Xe", Category = "Trade", Description = "Biết cách vận chuyển hàng hóa.", UnlockType = "Action", UnlockRequirementValue = 25 }
            );

            modelBuilder.Entity<Npc>().HasData(
                new Npc { Id = 1, Name = "Bà Lan", Type = "Merchant", RegionId = 1, MapId = 1, PositionX = 0, PositionY = 0, PositionZ = 0, IsActive = true },
                new Npc { Id = 2, Name = "Thợ Rèn", Type = "Blacksmith", RegionId = 2, MapId = 2, PositionX = 10, PositionY = 0, PositionZ = 10, IsActive = true },
                new Npc { Id = 3, Name = "Lão Trăm Năm", Type = "Trainer", RegionId = 3, MapId = 3, PositionX = 20, PositionY = 0, PositionZ = 30, IsActive = true },
                new Npc { Id = 4, Name = "Chủ Quán", Type = "Chef", RegionId = 1, MapId = 1, PositionX = 5, PositionY = 0, PositionZ = 5, IsActive = true }
            );

            modelBuilder.Entity<Season>().HasData(
                new Season { Id = 1, Name = "Xuân", StartMonth = 1, EndMonth = 3 },
                new Season { Id = 2, Name = "Hè", StartMonth = 4, EndMonth = 6 },
                new Season { Id = 3, Name = "Thu", StartMonth = 7, EndMonth = 9 },
                new Season { Id = 4, Name = "Đông", StartMonth = 10, EndMonth = 12 }
            );
        }
    }
}

