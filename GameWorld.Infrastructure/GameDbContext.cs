using GameWorld.Domain.Entities;
using GameWorld.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Infrastructure;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountSession> AccountSessions => Set<AccountSession>();
    public DbSet<AccountLoginAttempt> AccountLoginAttempts => Set<AccountLoginAttempt>();
    public DbSet<AccountBan> AccountBans => Set<AccountBan>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterStats> CharacterStats => Set<CharacterStats>();
    public DbSet<CharacterActionLog> CharacterActionLogs => Set<CharacterActionLog>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<RegionSeasonData> RegionSeasonData => Set<RegionSeasonData>();
    public DbSet<WeatherCondition> WeatherConditions => Set<WeatherCondition>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<FarmingPlot> FarmingPlots => Set<FarmingPlot>();
    public DbSet<Npc> Npcs => Set<Npc>();
    public DbSet<NpcShopItem> NpcShopItems => Set<NpcShopItem>();
    public DbSet<SkillTemplate> SkillTemplates => Set<SkillTemplate>();
    public DbSet<SkillUnlockRequirement> SkillUnlockRequirements => Set<SkillUnlockRequirement>();
    public DbSet<CharacterSkill> CharacterSkills => Set<CharacterSkill>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<QuestProgress> QuestProgresses => Set<QuestProgress>();
    public DbSet<ItemTemplate> ItemTemplates => Set<ItemTemplate>();
    public DbSet<CharacterInventory> CharacterInventories => Set<CharacterInventory>();
    public DbSet<CurrencyLedger> CurrencyLedgers => Set<CurrencyLedger>();
    public DbSet<MarketListing> MarketListings => Set<MarketListing>();
    public DbSet<MarketTransaction> MarketTransactions => Set<MarketTransaction>();
    public DbSet<MarketPriceHistory> MarketPriceHistories => Set<MarketPriceHistory>();
    public DbSet<EconomyEvent> EconomyEvents => Set<EconomyEvent>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<GuildWarehouse> GuildWarehouses => Set<GuildWarehouse>();
    public DbSet<CaravanRoute> CaravanRoutes => Set<CaravanRoute>();
    public DbSet<CaravanMission> CaravanMissions => Set<CaravanMission>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminActionLog> AdminActionLogs => Set<AdminActionLog>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportTicketMessage> SupportTicketMessages => Set<SupportTicketMessage>();
    public DbSet<GiftCode> GiftCodes => Set<GiftCode>();
    public DbSet<GiftCodeUsage> GiftCodeUsages => Set<GiftCodeUsage>();
    public DbSet<MaintenanceAnnouncement> MaintenanceAnnouncements => Set<MaintenanceAnnouncement>();
    public DbSet<ServerStatus> ServerStatuses => Set<ServerStatus>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<DailyRevenue> DailyRevenues => Set<DailyRevenue>();
    public DbSet<OnlineUsersSummary> OnlineUsersSummaries => Set<OnlineUsersSummary>();
    public DbSet<CheatDetectionLog> CheatDetectionLogs => Set<CheatDetectionLog>();
    public DbSet<LiveEvent> LiveEvents => Set<LiveEvent>();
    public DbSet<GachaEntry> GachaEntries => Set<GachaEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<Account>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<PlayerProfile>().HasIndex(x => x.AccountId).IsUnique();
        modelBuilder.Entity<Character>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Character>().HasOne(x => x.Stats).WithOne(x => x.Character)
            .HasForeignKey<CharacterStats>(x => x.CharacterId);
        modelBuilder.Entity<AccountSession>().HasIndex(x => x.RefreshTokenHash).IsUnique();
        modelBuilder.Entity<AdminUser>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<CharacterSkill>().HasIndex(x => new { x.CharacterId, x.SkillId }).IsUnique();
        modelBuilder.Entity<GiftCode>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<GiftCodeUsage>().HasIndex(x => new { x.GiftCodeId, x.AccountId }).IsUnique();
        modelBuilder.Entity<Guild>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<GuildMember>().HasIndex(x => x.CharacterId).IsUnique();
        modelBuilder.Entity<CharacterActionLog>().HasIndex(x => new { x.CharacterId, x.ActionType, x.CreatedAt });
        modelBuilder.Entity<CharacterInventory>().HasIndex(x => new { x.CharacterId, x.ItemId });
        modelBuilder.Entity<MarketListing>().HasIndex(x => new { x.RegionId, x.Status });
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                { property.SetPrecision(18); property.SetScale(2); }
            foreach (var foreignKey in entity.GetForeignKeys()) foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
        // Fixed seed timestamps keep the model deterministic across builds and migrations.
        SeedData.Seed(modelBuilder);
    }
}
