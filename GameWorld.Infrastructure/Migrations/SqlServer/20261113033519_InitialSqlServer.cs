using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GameWorld.Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Crops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GrowthTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    BaseYield = table.Column<int>(type: "int", nullable: false),
                    HarvestItemId = table.Column<long>(type: "bigint", nullable: true),
                    ClimateRequirement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyRevenues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RevenueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrencyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRevenues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rarity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxStack = table.Column<int>(type: "int", nullable: false),
                    IsTradeable = table.Column<bool>(type: "bit", nullable: false),
                    IsConsumable = table.Column<bool>(type: "bit", nullable: false),
                    IsSellable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiveEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnlineUsersSummaries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecordTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConcurrentUsers = table.Column<int>(type: "int", nullable: false),
                    PeakConcurrentUsers = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnlineUsersSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Regions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Climate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartMonth = table.Column<int>(type: "int", nullable: false),
                    EndMonth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServerStatuses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CPUUsage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MemoryUsage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActiveConnections = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillTemplates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnlockType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnlockRequirementValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountBans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    BannedByAdminId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BannedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPermanent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LiftedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LiftedByAdminId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountBans_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountLoginAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Result = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttemptAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountLoginAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountLoginAttempts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountSessions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegionId = table.Column<long>(type: "bigint", nullable: true),
                    CurrentMapId = table.Column<long>(type: "bigint", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    Gold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Gem = table.Column<int>(type: "int", nullable: false),
                    Stamina = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerProfiles_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceAnnouncements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByAdminId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceAnnouncements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceAnnouncements_AdminUsers_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GachaEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GachaEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GachaEntries_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiftCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RewardType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RewardItemId = table.Column<long>(type: "bigint", nullable: true),
                    RewardAmount = table.Column<int>(type: "int", nullable: false),
                    RegionScope = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxUsage = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByAdminId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCodes_AdminUsers_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCodes_ItemTemplates_RewardItemId",
                        column: x => x.RewardItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaravanRoutes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartRegionId = table.Column<long>(type: "bigint", nullable: false),
                    EndRegionId = table.Column<long>(type: "bigint", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    TravelTimeMinutes = table.Column<int>(type: "int", nullable: false),
                    RiskMultiplier = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaravanRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaravanRoutes_Regions_EndRegionId",
                        column: x => x.EndRegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaravanRoutes_Regions_StartRegionId",
                        column: x => x.StartRegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EconomyEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Modifier = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomyEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EconomyEvents_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EconomyEvents_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsSafeZone = table.Column<bool>(type: "bit", nullable: false),
                    MinLevel = table.Column<int>(type: "int", nullable: false),
                    TravelCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maps_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketPriceHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPriceHistories_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketPriceHistories_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeatherConditions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    WeatherType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Intensity = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectOnMovement = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectOnCrops = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeatherConditions_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegionSeasonData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    SeasonId = table.Column<int>(type: "int", nullable: false),
                    CropModifier = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TravelModifier = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionSeasonData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionSeasonData_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegionSeasonData_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkillUnlockRequirements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillId = table.Column<long>(type: "bigint", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequiredCount = table.Column<int>(type: "int", nullable: false),
                    NPCId = table.Column<long>(type: "bigint", nullable: true),
                    RegionId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillUnlockRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillUnlockRequirements_SkillTemplates_SkillId",
                        column: x => x.SkillId,
                        principalTable: "SkillTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlayerProfileId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: false),
                    RaceId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    CurrentHP = table.Column<int>(type: "int", nullable: false),
                    CurrentMP = table.Column<int>(type: "int", nullable: false),
                    PositionX = table.Column<float>(type: "real", nullable: false),
                    PositionY = table.Column<float>(type: "real", nullable: false),
                    PositionZ = table.Column<float>(type: "real", nullable: false),
                    MapId = table.Column<long>(type: "bigint", nullable: false),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_PlayerProfiles_PlayerProfileId",
                        column: x => x.PlayerProfileId,
                        principalTable: "PlayerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Npcs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    MapId = table.Column<long>(type: "bigint", nullable: false),
                    PositionX = table.Column<float>(type: "real", nullable: false),
                    PositionY = table.Column<float>(type: "real", nullable: false),
                    PositionZ = table.Column<float>(type: "real", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Npcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Npcs_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Npcs_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminActionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<long>(type: "bigint", nullable: false),
                    TargetAccountId = table.Column<long>(type: "bigint", nullable: true),
                    TargetCharacterId = table.Column<long>(type: "bigint", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminActionLogs_Accounts_TargetAccountId",
                        column: x => x.TargetAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminActionLogs_AdminUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminActionLogs_Characters_TargetCharacterId",
                        column: x => x.TargetCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterActionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionValue = table.Column<int>(type: "int", nullable: false),
                    RegionId = table.Column<long>(type: "bigint", nullable: true),
                    NPCId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterActionLogs_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterInventories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    Durability = table.Column<int>(type: "int", nullable: true),
                    EnchantLevel = table.Column<int>(type: "int", nullable: false),
                    IsEquipped = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterInventories_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterInventories_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    SkillId = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Experience = table.Column<int>(type: "int", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "bit", nullable: false),
                    LearnedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_SkillTemplates_SkillId",
                        column: x => x.SkillId,
                        principalTable: "SkillTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterStats",
                columns: table => new
                {
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Strength = table.Column<int>(type: "int", nullable: false),
                    Agility = table.Column<int>(type: "int", nullable: false),
                    Vitality = table.Column<int>(type: "int", nullable: false),
                    Intelligence = table.Column<int>(type: "int", nullable: false),
                    Luck = table.Column<int>(type: "int", nullable: false),
                    Attack = table.Column<int>(type: "int", nullable: false),
                    Defense = table.Column<int>(type: "int", nullable: false),
                    CritRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Speed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStats", x => x.CharacterId);
                    table.ForeignKey(
                        name: "FK_CharacterStats_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CheatDetectionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: true),
                    RuleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheatDetectionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheatDetectionLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CheatDetectionLogs_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyLedgers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyLedgers_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FarmingPlots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    CropId = table.Column<long>(type: "bigint", nullable: true),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    ReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedYield = table.Column<int>(type: "int", nullable: false),
                    PlantTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrowthStage = table.Column<int>(type: "int", nullable: false),
                    Watered = table.Column<bool>(type: "bit", nullable: false),
                    Fertilized = table.Column<bool>(type: "bit", nullable: false),
                    IsHarvested = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmingPlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmingPlots_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FarmingPlots_Crops_CropId",
                        column: x => x.CropId,
                        principalTable: "Crops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiftCodeUsages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GiftCodeId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCodeUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCodeUsages_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCodeUsages_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCodeUsages_GiftCodes_GiftCodeId",
                        column: x => x.GiftCodeId,
                        principalTable: "GiftCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LeaderCharacterId = table.Column<long>(type: "bigint", nullable: false),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Treasury = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guilds_Characters_LeaderCharacterId",
                        column: x => x.LeaderCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Guilds_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketListings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerCharacterId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketListings_Characters_SellerCharacterId",
                        column: x => x.SellerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketListings_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketListings_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedAdminId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_AdminUsers_AssignedAdminId",
                        column: x => x.AssignedAdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportTickets_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NpcShopItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NpcId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    IsSell = table.Column<bool>(type: "bit", nullable: false),
                    IsBuy = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcShopItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcShopItems_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NpcShopItems_Npcs_NpcId",
                        column: x => x.NpcId,
                        principalTable: "Npcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    NpcId = table.Column<long>(type: "bigint", nullable: false),
                    ObjectiveType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObjectiveTarget = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GoalCount = table.Column<int>(type: "int", nullable: false),
                    RewardItemId = table.Column<long>(type: "bigint", nullable: true),
                    RewardGold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RewardExp = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quests_Npcs_NpcId",
                        column: x => x.NpcId,
                        principalTable: "Npcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Quests_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaravanMissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    RouteId = table.Column<long>(type: "bigint", nullable: false),
                    CargoItemId = table.Column<long>(type: "bigint", nullable: false),
                    CargoQuantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RewardGold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RewardExp = table.Column<long>(type: "bigint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaravanMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaravanMissions_CaravanRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "CaravanRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaravanMissions_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaravanMissions_ItemTemplates_CargoItemId",
                        column: x => x.CargoItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Contribution = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildMembers_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildMembers_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuildWarehouses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SlotIndex = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildWarehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildWarehouses_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuildWarehouses_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuyerCharacterId = table.Column<long>(type: "bigint", nullable: false),
                    SellerCharacterId = table.Column<long>(type: "bigint", nullable: false),
                    ListingId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RegionId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketTransactions_Characters_BuyerCharacterId",
                        column: x => x.BuyerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketTransactions_Characters_SellerCharacterId",
                        column: x => x.SellerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketTransactions_ItemTemplates_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ItemTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketTransactions_MarketListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "MarketListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketTransactions_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportTicketMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    SenderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SenderId = table.Column<long>(type: "bigint", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTicketMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportTicketMessages_SupportTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "SupportTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestProgresses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CharacterId = table.Column<long>(type: "bigint", nullable: false),
                    QuestId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentProgress = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestProgresses_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestProgresses_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Crops",
                columns: new[] { "Id", "BaseYield", "ClimateRequirement", "CreatedAt", "GrowthTimeMinutes", "HarvestItemId", "Name" },
                values: new object[] { 1L, 4, "Temperate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1L, "Lúa Mì" });

            migrationBuilder.InsertData(
                table: "ItemTemplates",
                columns: new[] { "Id", "BasePrice", "Category", "CreatedAt", "IsConsumable", "IsSellable", "IsTradeable", "MaxStack", "Name", "Rarity" },
                values: new object[,]
                {
                    { 1L, 25m, "Resource", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, true, true, 99, "Lúa Mì", "Common" },
                    { 2L, 35m, "Food", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, true, 50, "Thịt Săn", "Common" },
                    { 3L, 20m, "Material", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, true, true, 99, "Gỗ", "Common" },
                    { 4L, 1m, "Currency", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, false, false, 999999, "Đồng Xu", "Common" },
                    { 5L, 45m, "Food", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, true, true, 50, "Bánh Nướng", "Common" },
                    { 6L, 120m, "Equipment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, true, true, 1, "Kiếm Lữ Hành", "Rare" },
                    { 7L, 15m, "Material", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, true, true, 99, "Thảo Dược", "Common" }
                });

            migrationBuilder.InsertData(
                table: "Regions",
                columns: new[] { "Id", "Climate", "CreatedAt", "Description", "Name", "Type" },
                values: new object[,]
                {
                    { 1L, "Temperate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Làng nông thôn nơi bắt đầu cuộc hành trình.", "Làng Tân Thủ", "Village" },
                    { 2L, "Temperate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Thị trường sầm uất và giao thương lớn.", "Thành Phố Pháo Đài", "City" },
                    { 3L, "Cold", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nơi quái vật mạnh và kho báu ẩn giấu.", "Đồi Núi Hắc Ám", "Mountain" },
                    { 4L, "Dry", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nông trại và thương nhân phải đối mặt với cát nóng.", "Sa Mạc Gió Hè", "Desert" }
                });

            migrationBuilder.InsertData(
                table: "Seasons",
                columns: new[] { "Id", "EndMonth", "Name", "StartMonth" },
                values: new object[,]
                {
                    { 1, 3, "Xuân", 1 },
                    { 2, 6, "Hè", 4 },
                    { 3, 9, "Thu", 7 },
                    { 4, 12, "Đông", 10 }
                });

            migrationBuilder.InsertData(
                table: "SkillTemplates",
                columns: new[] { "Id", "Category", "Description", "Name", "UnlockRequirementValue", "UnlockType" },
                values: new object[,]
                {
                    { 1L, "Crafting", "Biết cách nấu ăn.", "Nấu Ăn", 10, "Action" },
                    { 2L, "Trade", "Biết cách mặc cả.", "Thương Lượng", 50, "Action" },
                    { 3L, "Survival", "Biết cách trồng trọt.", "Trồng Trọt", 20, "Action" },
                    { 4L, "Trade", "Biết cách vận chuyển hàng hóa.", "Lái Xe", 25, "Action" }
                });

            migrationBuilder.InsertData(
                table: "GachaEntries",
                columns: new[] { "Id", "IsActive", "ItemId", "Weight" },
                values: new object[,]
                {
                    { 1L, true, 1L, 70 },
                    { 2L, true, 7L, 25 },
                    { 3L, true, 6L, 5 }
                });

            migrationBuilder.InsertData(
                table: "Maps",
                columns: new[] { "Id", "CreatedAt", "IsSafeZone", "MinLevel", "Name", "RegionId", "TravelCost", "Type" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 1, "Làng Tân Thủ", 1L, 0m, "SafeZone" },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 1, "Chợ Trung Tâm", 2L, 10m, "City" },
                    { 3L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 10, "Đỉnh Núi", 3L, 20m, "Mountain" },
                    { 4L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 15, "Bãi Cát", 4L, 25m, "Desert" },
                    { 5L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 1, "Rừng Ven Làng", 1L, 0m, "Forest" }
                });

            migrationBuilder.InsertData(
                table: "RegionSeasonData",
                columns: new[] { "Id", "CropModifier", "Description", "RegionId", "SeasonId", "TravelModifier" },
                values: new object[,]
                {
                    { 1L, 1.25m, null, 1L, 1, 1m },
                    { 2L, 1m, null, 1L, 2, 1m },
                    { 3L, 1m, null, 1L, 3, 1m },
                    { 4L, 0.5m, null, 1L, 4, 0.8m },
                    { 5L, 1.25m, null, 2L, 1, 1m },
                    { 6L, 1m, null, 2L, 2, 1m },
                    { 7L, 1m, null, 2L, 3, 1m },
                    { 8L, 0.5m, null, 2L, 4, 0.8m },
                    { 9L, 1.25m, null, 3L, 1, 1m },
                    { 10L, 1m, null, 3L, 2, 1m },
                    { 11L, 1m, null, 3L, 3, 1m },
                    { 12L, 0.5m, null, 3L, 4, 0.8m },
                    { 13L, 1.25m, null, 4L, 1, 1m },
                    { 14L, 1m, null, 4L, 2, 1m },
                    { 15L, 1m, null, 4L, 3, 1m },
                    { 16L, 0.5m, null, 4L, 4, 0.8m }
                });

            migrationBuilder.InsertData(
                table: "SkillUnlockRequirements",
                columns: new[] { "Id", "ActionType", "NPCId", "RegionId", "RequiredCount", "SkillId" },
                values: new object[,]
                {
                    { 1L, "Cook", 4L, null, 10, 1L },
                    { 2L, "Trade", 1L, null, 50, 2L },
                    { 3L, "Harvest", 5L, null, 20, 3L },
                    { 4L, "Travel", 3L, null, 25, 4L }
                });

            migrationBuilder.InsertData(
                table: "Npcs",
                columns: new[] { "Id", "IsActive", "MapId", "Name", "PositionX", "PositionY", "PositionZ", "RegionId", "Type" },
                values: new object[,]
                {
                    { 1L, true, 1L, "Bà Lan", 0f, 0f, 0f, 1L, "Merchant" },
                    { 2L, true, 2L, "Thợ Rèn", 10f, 0f, 10f, 2L, "Blacksmith" },
                    { 3L, true, 3L, "Lão Trăm Năm", 20f, 0f, 30f, 3L, "Trainer" },
                    { 4L, true, 1L, "Chủ Quán", 5f, 0f, 5f, 1L, "Chef" },
                    { 5L, true, 1L, "Bác Năm", 8f, 0f, 0f, 1L, "Farmer" }
                });

            migrationBuilder.InsertData(
                table: "NpcShopItems",
                columns: new[] { "Id", "IsBuy", "IsSell", "ItemId", "NpcId", "Price", "Stock" },
                values: new object[,]
                {
                    { 1L, true, true, 1L, 1L, 25m, 10000 },
                    { 2L, true, true, 2L, 1L, 35m, 10000 },
                    { 3L, true, true, 3L, 1L, 20m, 10000 },
                    { 4L, true, true, 1L, 2L, 25m, 10000 },
                    { 5L, true, true, 6L, 2L, 120m, 1000 },
                    { 6L, true, true, 5L, 4L, 45m, 10000 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountBans_AccountId",
                table: "AccountBans",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLoginAttempts_AccountId",
                table: "AccountLoginAttempts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountSessions_AccountId",
                table: "AccountSessions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSessions_RefreshTokenHash",
                table: "AccountSessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_AdminId",
                table: "AdminActionLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_TargetAccountId",
                table: "AdminActionLogs",
                column: "TargetAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLogs_TargetCharacterId",
                table: "AdminActionLogs",
                column: "TargetCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Username",
                table: "AdminUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaravanMissions_CargoItemId",
                table: "CaravanMissions",
                column: "CargoItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CaravanMissions_GuildId",
                table: "CaravanMissions",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_CaravanMissions_RouteId",
                table: "CaravanMissions",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_CaravanRoutes_EndRegionId",
                table: "CaravanRoutes",
                column: "EndRegionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaravanRoutes_StartRegionId",
                table: "CaravanRoutes",
                column: "StartRegionId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterActionLogs_CharacterId_ActionType_CreatedAt",
                table: "CharacterActionLogs",
                columns: new[] { "CharacterId", "ActionType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterInventories_CharacterId_ItemId",
                table: "CharacterInventories",
                columns: new[] { "CharacterId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterInventories_ItemId",
                table: "CharacterInventories",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Characters_PlayerProfileId",
                table: "Characters",
                column: "PlayerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CharacterId_SkillId",
                table: "CharacterSkills",
                columns: new[] { "CharacterId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_SkillId",
                table: "CharacterSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatDetectionLogs_AccountId",
                table: "CheatDetectionLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatDetectionLogs_CharacterId",
                table: "CheatDetectionLogs",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyLedgers_CharacterId",
                table: "CurrencyLedgers",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyEvents_ItemId",
                table: "EconomyEvents",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyEvents_RegionId",
                table: "EconomyEvents",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmingPlots_CharacterId",
                table: "FarmingPlots",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmingPlots_CropId",
                table: "FarmingPlots",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_GachaEntries_ItemId",
                table: "GachaEntries",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodes_Code",
                table: "GiftCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodes_CreatedByAdminId",
                table: "GiftCodes",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodes_RewardItemId",
                table: "GiftCodes",
                column: "RewardItemId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodeUsages_AccountId",
                table: "GiftCodeUsages",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodeUsages_CharacterId",
                table: "GiftCodeUsages",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCodeUsages_GiftCodeId_AccountId",
                table: "GiftCodeUsages",
                columns: new[] { "GiftCodeId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMembers_CharacterId",
                table: "GuildMembers",
                column: "CharacterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuildMembers_GuildId",
                table: "GuildMembers",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_LeaderCharacterId",
                table: "Guilds",
                column: "LeaderCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_Name",
                table: "Guilds",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_RegionId",
                table: "Guilds",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildWarehouses_GuildId",
                table: "GuildWarehouses",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildWarehouses_ItemId",
                table: "GuildWarehouses",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceAnnouncements_CreatedByAdminId",
                table: "MaintenanceAnnouncements",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_RegionId",
                table: "Maps",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_ItemId",
                table: "MarketListings",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_RegionId_Status",
                table: "MarketListings",
                columns: new[] { "RegionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketListings_SellerCharacterId",
                table: "MarketListings",
                column: "SellerCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPriceHistories_ItemId",
                table: "MarketPriceHistories",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPriceHistories_RegionId",
                table: "MarketPriceHistories",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTransactions_BuyerCharacterId",
                table: "MarketTransactions",
                column: "BuyerCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTransactions_ItemId",
                table: "MarketTransactions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTransactions_ListingId",
                table: "MarketTransactions",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTransactions_RegionId",
                table: "MarketTransactions",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketTransactions_SellerCharacterId",
                table: "MarketTransactions",
                column: "SellerCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_MapId",
                table: "Npcs",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Npcs_RegionId",
                table: "Npcs",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcShopItems_ItemId",
                table: "NpcShopItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcShopItems_NpcId",
                table: "NpcShopItems",
                column: "NpcId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProfiles_AccountId",
                table: "PlayerProfiles",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestProgresses_CharacterId",
                table: "QuestProgresses",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestProgresses_QuestId",
                table: "QuestProgresses",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_NpcId",
                table: "Quests",
                column: "NpcId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_RegionId",
                table: "Quests",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSeasonData_RegionId",
                table: "RegionSeasonData",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionSeasonData_SeasonId",
                table: "RegionSeasonData",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillUnlockRequirements_SkillId",
                table: "SkillUnlockRequirements",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTicketMessages_TicketId",
                table: "SupportTicketMessages",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AccountId",
                table: "SupportTickets",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_AssignedAdminId",
                table: "SupportTickets",
                column: "AssignedAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CharacterId",
                table: "SupportTickets",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_WeatherConditions_RegionId",
                table: "WeatherConditions",
                column: "RegionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBans");

            migrationBuilder.DropTable(
                name: "AccountLoginAttempts");

            migrationBuilder.DropTable(
                name: "AccountSessions");

            migrationBuilder.DropTable(
                name: "AdminActionLogs");

            migrationBuilder.DropTable(
                name: "CaravanMissions");

            migrationBuilder.DropTable(
                name: "CharacterActionLogs");

            migrationBuilder.DropTable(
                name: "CharacterInventories");

            migrationBuilder.DropTable(
                name: "CharacterSkills");

            migrationBuilder.DropTable(
                name: "CharacterStats");

            migrationBuilder.DropTable(
                name: "CheatDetectionLogs");

            migrationBuilder.DropTable(
                name: "CurrencyLedgers");

            migrationBuilder.DropTable(
                name: "DailyRevenues");

            migrationBuilder.DropTable(
                name: "EconomyEvents");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "FarmingPlots");

            migrationBuilder.DropTable(
                name: "GachaEntries");

            migrationBuilder.DropTable(
                name: "GiftCodeUsages");

            migrationBuilder.DropTable(
                name: "GuildMembers");

            migrationBuilder.DropTable(
                name: "GuildWarehouses");

            migrationBuilder.DropTable(
                name: "LiveEvents");

            migrationBuilder.DropTable(
                name: "MaintenanceAnnouncements");

            migrationBuilder.DropTable(
                name: "MarketPriceHistories");

            migrationBuilder.DropTable(
                name: "MarketTransactions");

            migrationBuilder.DropTable(
                name: "NpcShopItems");

            migrationBuilder.DropTable(
                name: "OnlineUsersSummaries");

            migrationBuilder.DropTable(
                name: "QuestProgresses");

            migrationBuilder.DropTable(
                name: "RegionSeasonData");

            migrationBuilder.DropTable(
                name: "ServerStatuses");

            migrationBuilder.DropTable(
                name: "SkillUnlockRequirements");

            migrationBuilder.DropTable(
                name: "SupportTicketMessages");

            migrationBuilder.DropTable(
                name: "WeatherConditions");

            migrationBuilder.DropTable(
                name: "CaravanRoutes");

            migrationBuilder.DropTable(
                name: "Crops");

            migrationBuilder.DropTable(
                name: "GiftCodes");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "MarketListings");

            migrationBuilder.DropTable(
                name: "Quests");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "SkillTemplates");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "ItemTemplates");

            migrationBuilder.DropTable(
                name: "Npcs");

            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "PlayerProfiles");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
