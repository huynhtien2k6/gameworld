using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameWorld.Domain.Entities
{
    public class Account
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? PasswordSalt { get; set; }

        [MaxLength(128)]
        public string? SecurityStamp { get; set; }

        public int Status { get; set; } = 0;
        public bool IsVerified { get; set; } = false;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public PlayerProfile? PlayerProfile { get; set; }
        public ICollection<AccountSession> Sessions { get; set; } = new List<AccountSession>();
        public ICollection<AccountLoginAttempt> LoginAttempts { get; set; } = new List<AccountLoginAttempt>();
        public ICollection<AccountBan> Bans { get; set; } = new List<AccountBan>();
        public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
        public ICollection<GiftCodeUsage> GiftCodeUsages { get; set; } = new List<GiftCodeUsage>();
    }

    public class AccountSession
    {
        [Key]
        public long Id { get; set; }

        public long AccountId { get; set; }

        [Required, MaxLength(255)]
        public string RefreshTokenHash { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? DeviceId { get; set; }

        [MaxLength(50)]
        public string? DeviceType { get; set; }

        [MaxLength(45)]
        public string? IPAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
    }

    public class AccountLoginAttempt
    {
        [Key]
        public long Id { get; set; }

        public long? AccountId { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(45)]
        public string? IPAddress { get; set; }

        public int Result { get; set; }

        [MaxLength(255)]
        public string? Reason { get; set; }

        public DateTime AttemptAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AccountId))]
        public Account? Account { get; set; }
    }

    public class AccountBan
    {
        [Key]
        public long Id { get; set; }

        public long AccountId { get; set; }
        public long? BannedByAdminId { get; set; }

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime? BannedUntil { get; set; }
        public bool IsPermanent { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LiftedAt { get; set; }
        public long? LiftedByAdminId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;
    }

    public class PlayerProfile
    {
        [Key]
        public long Id { get; set; }

        public long AccountId { get; set; }

        [Required, MaxLength(50)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        public long? RegionId { get; set; }
        public long? CurrentMapId { get; set; }

        public int Level { get; set; } = 1;
        public long Experience { get; set; } = 0;
        public decimal Gold { get; set; } = 0;
        public int Gem { get; set; } = 0;
        public int Stamina { get; set; } = 100;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;

        public ICollection<Character> Characters { get; set; } = new List<Character>();
    }

    public class Character
    {
        [Key]
        public long Id { get; set; }

        public long PlayerProfileId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public int RaceId { get; set; }

        public int Level { get; set; } = 1;
        public long Experience { get; set; } = 0;
        public int CurrentHP { get; set; } = 100;
        public int CurrentMP { get; set; } = 50;

        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }

        public long MapId { get; set; }
        public bool IsMain { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(PlayerProfileId))]
        public PlayerProfile PlayerProfile { get; set; } = null!;

        public CharacterStats? Stats { get; set; }
        public ICollection<CharacterInventory> Inventory { get; set; } = new List<CharacterInventory>();
        public ICollection<CharacterSkill> Skills { get; set; } = new List<CharacterSkill>();
        public ICollection<CharacterActionLog> ActionLogs { get; set; } = new List<CharacterActionLog>();
        public ICollection<CurrencyLedger> CurrencyLogs { get; set; } = new List<CurrencyLedger>();
        public ICollection<MarketListing> MarketListings { get; set; } = new List<MarketListing>();
        public ICollection<QuestProgress> QuestProgresses { get; set; } = new List<QuestProgress>();
        public ICollection<FarmingPlot> FarmingPlots { get; set; } = new List<FarmingPlot>();
        public ICollection<GuildMember> GuildMemberships { get; set; } = new List<GuildMember>();
        public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
        public ICollection<GiftCodeUsage> GiftCodeUsages { get; set; } = new List<GiftCodeUsage>();
    }

    public class CharacterStats
    {
        [Key]
        public long CharacterId { get; set; }

        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Vitality { get; set; }
        public int Intelligence { get; set; }
        public int Luck { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public decimal CritRate { get; set; } = 0m;
        public int Speed { get; set; }

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;
    }

    public class CharacterActionLog
    {
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }

        [Required, MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        public int ActionValue { get; set; } = 0;
        public long? RegionId { get; set; }
        public long? NPCId { get; set; }
        public long? ItemId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;
    }

    public class Region
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string Climate { get; set; } = "Temperate";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Map> Maps { get; set; } = new List<Map>();
        public ICollection<Npc> Npcs { get; set; } = new List<Npc>();
        public ICollection<WeatherCondition> WeatherConditions { get; set; } = new List<WeatherCondition>();
        public ICollection<RegionSeasonData> RegionSeasonData { get; set; } = new List<RegionSeasonData>();
    }

    public class Map
    {
        [Key]
        public long Id { get; set; }

        public long RegionId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public bool IsSafeZone { get; set; } = false;
        public int MinLevel { get; set; } = 1;
        public decimal TravelCost { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        public ICollection<Npc> Npcs { get; set; } = new List<Npc>();
    }

    public class Season
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public int StartMonth { get; set; }
        public int EndMonth { get; set; }

        public ICollection<RegionSeasonData> RegionSeasonData { get; set; } = new List<RegionSeasonData>();
    }

    public class RegionSeasonData
    {
        [Key]
        public long Id { get; set; }

        public long RegionId { get; set; }
        public int SeasonId { get; set; }

        public decimal CropModifier { get; set; } = 1.0m;
        public decimal TravelModifier { get; set; } = 1.0m;

        [MaxLength(255)]
        public string? Description { get; set; }

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        [ForeignKey(nameof(SeasonId))]
        public Season Season { get; set; } = null!;
    }

    public class WeatherCondition
    {
        [Key]
        public long Id { get; set; }

        public long RegionId { get; set; }

        [Required, MaxLength(50)]
        public string WeatherType { get; set; } = "Clear";

        public int Intensity { get; set; } = 1;
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal EffectOnMovement { get; set; } = 1.0m;
        public decimal EffectOnCrops { get; set; } = 1.0m;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;
    }

    public class Crop
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public int GrowthTimeMinutes { get; set; }
        public int BaseYield { get; set; } = 1;
        public long? HarvestItemId { get; set; }

        [MaxLength(100)]
        public string? ClimateRequirement { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<FarmingPlot> FarmingPlots { get; set; } = new List<FarmingPlot>();
    }

    public class FarmingPlot
    {
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }
        public long? CropId { get; set; }
        public long RegionId { get; set; } = 1;
        public DateTime? ReadyAt { get; set; }
        public int ExpectedYield { get; set; } = 1;

        public DateTime? PlantTime { get; set; }
        public int GrowthStage { get; set; } = 0;
        public bool Watered { get; set; } = false;
        public bool Fertilized { get; set; } = false;
        public bool IsHarvested { get; set; } = false;

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;

        [ForeignKey(nameof(CropId))]
        public Crop? Crop { get; set; }
    }

    public class Npc
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public long RegionId { get; set; }
        public long MapId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        [ForeignKey(nameof(MapId))]
        public Map Map { get; set; } = null!;

        public ICollection<NpcShopItem> ShopItems { get; set; } = new List<NpcShopItem>();
    }

    public class NpcShopItem
    {
        [Key]
        public long Id { get; set; }

        public long NpcId { get; set; }
        public long ItemId { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; } = 0;
        public bool IsSell { get; set; } = true;
        public bool IsBuy { get; set; } = false;

        [ForeignKey(nameof(NpcId))]
        public Npc Npc { get; set; } = null!;

        [ForeignKey(nameof(ItemId))]
        public ItemTemplate Item { get; set; } = null!;
    }

    public class SkillTemplate
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required, MaxLength(50)]
        public string UnlockType { get; set; } = string.Empty;
        public int UnlockRequirementValue { get; set; } = 0;

        public ICollection<CharacterSkill> CharacterSkills { get; set; } = new List<CharacterSkill>();
        public ICollection<SkillUnlockRequirement> UnlockRequirements { get; set; } = new List<SkillUnlockRequirement>();
    }

    public class SkillUnlockRequirement
    {
        [Key]
        public long Id { get; set; }

        public long SkillId { get; set; }

        [Required, MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        public int RequiredCount { get; set; } = 0;
        public long? NPCId { get; set; }
        public long? RegionId { get; set; }

        [ForeignKey(nameof(SkillId))]
        public SkillTemplate Skill { get; set; } = null!;
    }

    public class CharacterSkill
    {
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }
        public long SkillId { get; set; }

        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public bool IsUnlocked { get; set; } = false;
        public DateTime? LearnedAt { get; set; }

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;

        [ForeignKey(nameof(SkillId))]
        public SkillTemplate Skill { get; set; } = null!;
    }

    public class Quest
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public long RegionId { get; set; }
        public long NpcId { get; set; }

        [Required, MaxLength(100)]
        public string ObjectiveType { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string ObjectiveTarget { get; set; } = string.Empty;

        public int GoalCount { get; set; } = 1;
        public long? RewardItemId { get; set; }
        public decimal RewardGold { get; set; } = 0;
        public long RewardExp { get; set; } = 0;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        [ForeignKey(nameof(NpcId))]
        public Npc Npc { get; set; } = null!;
    }

    public class QuestProgress
    {
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }
        public long QuestId { get; set; }

        public int CurrentProgress { get; set; } = 0;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;

        [ForeignKey(nameof(QuestId))]
        public Quest Quest { get; set; } = null!;
    }

    public class ItemTemplate
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Rarity { get; set; } = "Common";

        public decimal BasePrice { get; set; } = 0;
        public int MaxStack { get; set; } = 1;
        public bool IsTradeable { get; set; } = true;
        public bool IsConsumable { get; set; } = false;
        public bool IsSellable { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CharacterInventory> InventoryItems { get; set; } = new List<CharacterInventory>();
        public ICollection<NpcShopItem> NpcShopItems { get; set; } = new List<NpcShopItem>();
        public ICollection<MarketListing> MarketListings { get; set; } = new List<MarketListing>();
        public ICollection<MarketTransaction> Transactions { get; set; } = new List<MarketTransaction>();
    }

    public class CharacterInventory
    {
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }
        public long ItemId { get; set; }

        public int Quantity { get; set; } = 0;
        public int SlotIndex { get; set; } = 0;
        public int? Durability { get; set; }
        public int EnchantLevel { get; set; } = 0;
        public bool IsEquipped { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;

        [ForeignKey(nameof(ItemId))]
        public ItemTemplate Item { get; set; } = null!;
    }

    public class CurrencyLedger
    {
        [Key]
        public long Id { get; set; }

        public long CharacterId { get; set; }

        [Required, MaxLength(50)]
        public string CurrencyType { get; set; } = "Gold";

        public decimal Amount { get; set; }

        [Required, MaxLength(200)]
        public string Reason { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Source { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;
    }

    public class MarketListing
    {
        [Key]
        public long Id { get; set; }

        public long SellerCharacterId { get; set; }
        public long ItemId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public long RegionId { get; set; }
        public int Status { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        [ForeignKey(nameof(SellerCharacterId))]
        public Character Seller { get; set; } = null!;

        [ForeignKey(nameof(ItemId))]
        public ItemTemplate Item { get; set; } = null!;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        public ICollection<MarketTransaction> Transactions { get; set; } = new List<MarketTransaction>();
    }

    public class MarketTransaction
    {
        [Key]
        public long Id { get; set; }

        public long BuyerCharacterId { get; set; }
        public long SellerCharacterId { get; set; }
        public long ListingId { get; set; }
        public long ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public long RegionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(BuyerCharacterId))]
        public Character Buyer { get; set; } = null!;

        [ForeignKey(nameof(SellerCharacterId))]
        public Character Seller { get; set; } = null!;

        [ForeignKey(nameof(ListingId))]
        public MarketListing Listing { get; set; } = null!;

        [ForeignKey(nameof(ItemId))]
        public ItemTemplate Item { get; set; } = null!;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;
    }

    public class MarketPriceHistory
    {
        [Key]
        public long Id { get; set; }

        public long ItemId { get; set; }
        public long RegionId { get; set; }
        public decimal Price { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ItemId))]
        public ItemTemplate Item { get; set; } = null!;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;
    }

    public class EconomyEvent
    {
        [Key]
        public long Id { get; set; }

        public long RegionId { get; set; }
        public long ItemId { get; set; }

        [Required, MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        public decimal Modifier { get; set; } = 1.0m;
        public DateTime StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        [ForeignKey(nameof(ItemId))]
        public ItemTemplate Item { get; set; } = null!;
    }

    public class Guild
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public long LeaderCharacterId { get; set; }
        public long RegionId { get; set; }

        public int Level { get; set; } = 1;
        public decimal Treasury { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(LeaderCharacterId))]
        public Character Leader { get; set; } = null!;

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        public ICollection<GuildMember> Members { get; set; } = new List<GuildMember>();
        public ICollection<GuildWarehouse> Warehouses { get; set; } = new List<GuildWarehouse>();
        public ICollection<CaravanMission> CaravanMissions { get; set; } = new List<CaravanMission>();
    }

    public class GuildMember
    {
        [Key]
        public long Id { get; set; }

        public long GuildId { get; set; }
        public long CharacterId { get; set; }

        [Required, MaxLength(50)]
        public string Role { get; set; } = "Member";

        public int Contribution { get; set; } = 0;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(GuildId))]
        public Guild Guild { get; set; } = null!;

        [ForeignKey(nameof(CharacterId))]
        public Character Character { get; set; } = null!;
    }

    public class GuildWarehouse
    {
        [Key]
        public long Id { get; set; }

        public long GuildId { get; set; }
        public long ItemId { get; set; }

        public int Quantity { get; set; } = 0;
        public int SlotIndex { get; set; } = 0;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(GuildId))]
        public Guild Guild { get; set; } = null!;

        [ForeignKey(nameof(ItemId))]
        public ItemTemplate Item { get; set; } = null!;
    }

    public class CaravanRoute
    {
        [Key]
        public long Id { get; set; }

        public long StartRegionId { get; set; }
        public long EndRegionId { get; set; }
        public int Difficulty { get; set; } = 1;
        public int TravelTimeMinutes { get; set; } = 60;
        public decimal RiskMultiplier { get; set; } = 1.0m;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(StartRegionId))]
        public Region StartRegion { get; set; } = null!;

        [ForeignKey(nameof(EndRegionId))]
        public Region EndRegion { get; set; } = null!;
    }

    public class CaravanMission
    {
        [Key]
        public long Id { get; set; }

        public long GuildId { get; set; }
        public long RouteId { get; set; }
        public long CargoItemId { get; set; }

        public int CargoQuantity { get; set; } = 0;
        public int Status { get; set; } = 0;
        public decimal RewardGold { get; set; } = 0;
        public long RewardExp { get; set; } = 0;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        [ForeignKey(nameof(GuildId))]
        public Guild Guild { get; set; } = null!;

        [ForeignKey(nameof(RouteId))]
        public CaravanRoute Route { get; set; } = null!;

        [ForeignKey(nameof(CargoItemId))]
        public ItemTemplate CargoItem { get; set; } = null!;
    }

    public class AdminUser
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Role { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Permissions { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AdminActionLog> ActionLogs { get; set; } = new List<AdminActionLog>();
        public ICollection<GiftCode> GiftCodes { get; set; } = new List<GiftCode>();
        public ICollection<MaintenanceAnnouncement> MaintenanceAnnouncements { get; set; } = new List<MaintenanceAnnouncement>();
    }

    public class AdminActionLog
    {
        [Key]
        public long Id { get; set; }

        public long AdminId { get; set; }
        public long? TargetAccountId { get; set; }
        public long? TargetCharacterId { get; set; }

        [Required, MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AdminId))]
        public AdminUser Admin { get; set; } = null!;

        [ForeignKey(nameof(TargetAccountId))]
        public Account? TargetAccount { get; set; }

        [ForeignKey(nameof(TargetCharacterId))]
        public Character? TargetCharacter { get; set; }
    }

    public class SupportTicket
    {
        [Key]
        public long Id { get; set; }

        public long AccountId { get; set; }
        public long? CharacterId { get; set; }

        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public int Status { get; set; } = 0;
        public long? AssignedAdminId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;

        [ForeignKey(nameof(CharacterId))]
        public Character? Character { get; set; }

        [ForeignKey(nameof(AssignedAdminId))]
        public AdminUser? AssignedAdmin { get; set; }

        public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
    }

    public class SupportTicketMessage
    {
        [Key]
        public long Id { get; set; }

        public long TicketId { get; set; }

        [Required, MaxLength(20)]
        public string SenderType { get; set; } = string.Empty;

        public long? SenderId { get; set; }

        [Required, MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(TicketId))]
        public SupportTicket Ticket { get; set; } = null!;
    }

    public class GiftCode
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string RewardType { get; set; } = string.Empty;

        public long? RewardItemId { get; set; }
        public int RewardAmount { get; set; } = 0;

        [MaxLength(100)]
        public string? RegionScope { get; set; }

        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime? ValidTo { get; set; }
        public int MaxUsage { get; set; } = 1;
        public int UsedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public long CreatedByAdminId { get; set; }

        [ForeignKey(nameof(CreatedByAdminId))]
        public AdminUser CreatedByAdmin { get; set; } = null!;

        [ForeignKey(nameof(RewardItemId))]
        public ItemTemplate? RewardItem { get; set; }

        public ICollection<GiftCodeUsage> Usages { get; set; } = new List<GiftCodeUsage>();
    }

    public class GiftCodeUsage
    {
        [Key]
        public long Id { get; set; }

        public long GiftCodeId { get; set; }
        public long AccountId { get; set; }
        public long? CharacterId { get; set; }
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(GiftCodeId))]
        public GiftCode GiftCode { get; set; } = null!;

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;

        [ForeignKey(nameof(CharacterId))]
        public Character? Character { get; set; }
    }

    public class MaintenanceAnnouncement
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public bool IsActive { get; set; } = true;

        public long CreatedByAdminId { get; set; }

        [ForeignKey(nameof(CreatedByAdminId))]
        public AdminUser CreatedByAdmin { get; set; } = null!;
    }

    public class ServerStatus
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string ServerName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Region { get; set; }

        public decimal CPUUsage { get; set; }
        public decimal MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
        public int ErrorCount { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ErrorLog
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(20)]
        public string Level { get; set; } = "Info";

        [Required, MaxLength(200)]
        public string Source { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string? StackTrace { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class DailyRevenue
    {
        [Key]
        public long Id { get; set; }

        public DateTime RevenueDate { get; set; }

        [Required, MaxLength(50)]
        public string CurrencyType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        [Required, MaxLength(100)]
        public string Source { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Region { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class OnlineUsersSummary
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(100)]
        public string Region { get; set; } = string.Empty;

        public DateTime RecordTime { get; set; } = DateTime.UtcNow;
        public int ConcurrentUsers { get; set; }
        public int PeakConcurrentUsers { get; set; }
    }

    public class CheatDetectionLog
    {
        [Key]
        public long Id { get; set; }

        public long AccountId { get; set; }
        public long? CharacterId { get; set; }

        [Required, MaxLength(150)]
        public string RuleName { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Evidence { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Severity { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(AccountId))]
        public Account Account { get; set; } = null!;

        [ForeignKey(nameof(CharacterId))]
        public Character? Character { get; set; }
    }
}
