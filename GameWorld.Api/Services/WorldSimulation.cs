using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Services;

// Simulation uses the server clock; clients cannot choose the season or weather for an action.
public sealed class WorldSimulation(GameDbContext db)
{
    public static readonly MonsterDefinition[] Monsters =
    [
        new(1, "Slime đồng cỏ", 5, 1, 28, 7, 12m, 35, 2),
        new(2, "Sói núi", 3, 10, 220, 32, 65m, 180, 2),
        new(3, "Bọ cạp cát", 4, 15, 340, 48, 100m, 260, 7)
    ];

    public static readonly RecipeDefinition[] Recipes =
    [new(1, "Bánh nướng", [new(1, 2)], 5, 1)];

    public async Task<WorldState> GetWorldAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var regions = await db.Regions.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
        var maps = await db.Maps.AsNoTracking().OrderBy(m => m.Id).ToListAsync(ct);
        var states = new List<RegionState>();
        foreach (var region in regions)
            states.Add(new(region.Id, region.Name, region.Type, region.Description, region.Climate,
                await GetWeatherAsync(region.Id, now, ct), maps.Where(m => m.RegionId == region.Id).Select(ToMap).ToArray()));
        return new(now, (now.Hour + 7) % 24, states.ToArray());
    }

    public async Task<WeatherState> GetWeatherAsync(long regionId, DateTime? at = null, CancellationToken ct = default)
    {
        var now = at ?? DateTime.UtcNow;
        var region = await db.Regions.AsNoTracking().FirstAsync(r => r.Id == regionId, ct);
        var seasonId = (now.Month - 1) / 3 + 1;
        var seasonNames = new[] { "Xuân", "Hè", "Thu", "Đông" };
        var seasonal = await db.RegionSeasonData.AsNoTracking()
            .FirstOrDefaultAsync(s => s.RegionId == regionId && s.SeasonId == seasonId, ct);
        var crop = seasonal?.CropModifier ?? (seasonId == 4 ? 0.65m : seasonId == 1 ? 1.15m : 1m);
        var movement = Math.Clamp(seasonal?.TravelModifier ?? 1m, 0.3m, 1.5m);
        var weather = await db.WeatherConditions.AsNoTracking()
            .Where(w => w.RegionId == regionId && w.IsActive && w.StartsAt <= now && (w.EndsAt == null || w.EndsAt > now))
            .OrderByDescending(w => w.StartsAt).FirstOrDefaultAsync(ct);
        if (weather != null)
            return new(weather.WeatherType, seasonNames[seasonId - 1],
                Math.Clamp(crop * weather.EffectOnCrops, 0.2m, 2m),
                Math.Clamp(movement * weather.EffectOnMovement, 0.3m, 1.5m),
                weather.Intensity, DateTime.SpecifyKind(weather.StartsAt, DateTimeKind.Utc),
                weather.EndsAt is { } endsAt ? DateTime.SpecifyKind(endsAt, DateTimeKind.Utc) : null);

        var hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        // A stable hourly mix gives every API caller the same weather for a region.
        var phase = (int)(((hour.Ticks / TimeSpan.TicksPerHour) * 31 + regionId * 17) % 10);
        string type;
        decimal weatherCrop = 1m, weatherMovement = 1m;
        if (region.Climate == "Dry")
        {
            type = phase < 2 ? "Sandstorm" : "Sunny";
            weatherCrop = type == "Sandstorm" ? 0.5m : 0.8m;
            weatherMovement = type == "Sandstorm" ? 0.55m : 0.9m;
        }
        else if (seasonId == 4 && region.Climate == "Cold")
        {
            type = phase < 6 ? "Snow" : "Cloudy";
            weatherCrop = type == "Snow" ? 0.6m : 0.8m;
            weatherMovement = type == "Snow" ? 0.6m : 0.85m;
        }
        else if (phase < (seasonId == 2 ? 4 : 3))
        {
            type = "Rain"; weatherCrop = 1.25m; weatherMovement = 0.8m;
        }
        else if (phase == 4 || (now.Hour + 7) % 24 < 7)
        {
            type = "Fog"; weatherCrop = 1.05m; weatherMovement = 0.9m;
        }
        else type = phase < 7 ? "Cloudy" : "Sunny";
        return new(type, seasonNames[seasonId - 1], Math.Clamp(crop * weatherCrop, 0.2m, 2m),
            Math.Clamp(movement * weatherMovement, 0.3m, 1.5m), phase < 2 ? 2 : 1, hour, hour.AddHours(1));
    }

    public async Task<decimal> QuoteAsync(ItemTemplate item, long regionId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var region = await db.Regions.AsNoTracking().FirstAsync(r => r.Id == regionId, ct);
        var weather = await GetWeatherAsync(regionId, now, ct);
        var modifier = region.Type == "Village" ? 0.5m : region.Type == "City" ? 1.4m : region.Type == "Desert" ? 1.8m : 1.2m;
        if (item.Category is "Resource" or "Food")
            modifier *= weather.Season == "Đông" ? 1.8m : 1m;
        var localStocks = await db.NpcShopItems.AsNoTracking()
            .Where(s => s.ItemId == item.Id && s.Npc.RegionId == regionId && s.Npc.IsActive && s.IsSell)
            .Select(s => s.Stock).ToListAsync(ct);
        if (localStocks.Count > 0)
        {
            var stock = localStocks.Sum(s => (long)s);
            modifier *= stock < 20 ? 1.15m : stock > 100 ? 0.95m : 1m;
        }
        var events = await db.EconomyEvents.AsNoTracking()
            .Where(e => e.RegionId == regionId && e.ItemId == item.Id && e.StartsAt <= now && (e.EndsAt == null || e.EndsAt > now))
            .Select(e => e.Modifier).ToListAsync(ct);
        // Sum logarithms so overlapping events cannot overflow decimal before the final bound.
        var logModifier = Math.Log((double)modifier);
        foreach (var effect in events)
            if (effect > 0) logModifier += Math.Log((double)effect);
        var boundedModifier = (decimal)Math.Exp(Math.Clamp(logModifier, Math.Log(0.1), Math.Log(10)));
        return Math.Round(item.BasePrice * boundedModifier, 2, MidpointRounding.AwayFromZero);
    }

    public async Task<NpcState[]> GetNpcsAsync(long? regionId, long? mapId, CancellationToken ct = default)
    {
        var npcs = await db.Npcs.AsNoTracking().Include(n => n.ShopItems).ThenInclude(s => s.Item)
            .Where(n => n.IsActive && (regionId == null || n.RegionId == regionId) && (mapId == null || n.MapId == mapId))
            .OrderBy(n => n.Id).ToListAsync(ct);
        var result = new List<NpcState>();
        foreach (var npc in npcs)
        {
            var shop = new List<ShopItemState>();
            foreach (var entry in npc.ShopItems)
            {
                var price = await QuoteAsync(entry.Item, npc.RegionId, ct);
                shop.Add(new(entry.ItemId, entry.Item.Name, entry.Stock, price, Math.Floor(price * 0.7m * 100) / 100m, entry.IsSell, entry.IsBuy));
            }
            result.Add(new(npc.Id, npc.Name, npc.Type, npc.MapId, shop.ToArray()));
        }
        return result.ToArray();
    }

    public static MapState ToMap(Map map) => new(map.Id, map.Name, map.RegionId, map.MinLevel, map.TravelCost, map.IsSafeZone);
}

public sealed record WorldState(DateTime ServerTime, int GameHour, RegionState[] Regions);
public sealed record RegionState(long Id, string Name, string Type, string? Description, string Climate, WeatherState Weather, MapState[] Maps);
public sealed record WeatherState(string Type, string Season, decimal CropModifier, decimal MovementModifier, int Intensity, DateTime StartsAt, DateTime? EndsAt);
public sealed record MapState(long Id, string Name, long RegionId, int MinLevel, decimal TravelCost, bool IsSafeZone);
public sealed record NpcState(long Id, string Name, string Type, long MapId, ShopItemState[] ShopItems);
public sealed record ShopItemState(long ItemId, string Name, int Stock, decimal BuyPrice, decimal SellPrice, bool IsSell, bool IsBuy);
public sealed record MonsterDefinition(long Id, string Name, long MapId, int Level, int HP, int Attack, decimal Gold, int Experience, long LootItemId);
public sealed record RecipeDefinition(long Id, string Name, IngredientDefinition[] Ingredients, long ResultItemId, int Quantity);
public sealed record IngredientDefinition(long ItemId, int Quantity);
