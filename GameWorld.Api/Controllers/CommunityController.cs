using System.ComponentModel.DataAnnotations;
using System.Data;
using GameWorld.Api.Services;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Controllers;

[ApiController]
[Route("api/community")]
[Authorize(Policy = "Player")]
public sealed class CommunityController(GameDbContext db, OperationsService operations) : ControllerBase
{
    private long AccountId => OperationsService.AccountId(User);

    [HttpGet("listings")]
    public async Task<IActionResult> Listings([FromQuery] long? regionId)
    {
        var now = DateTime.UtcNow;
        var listings = await db.MarketListings.AsNoTracking()
            .Where(l => l.Status == 0 && l.Quantity > 0 && (l.ExpiresAt == null || l.ExpiresAt > now)
                && (regionId == null || l.RegionId == regionId))
            .OrderByDescending(l => l.Id).Take(100).Select(l => new
            {
                l.Id, l.SellerCharacterId, sellerName = l.Seller.Name, l.ItemId, itemName = l.Item.Name,
                l.Item.Rarity, l.Quantity, l.UnitPrice, l.RegionId, regionName = l.Region.Name, l.CreatedAt
            }).ToListAsync();
        return Ok(listings);
    }

    [HttpPost("listings")]
    public async Task<IActionResult> CreateListing(CreateListingRequest request)
    {
        if (decimal.Round(request.UnitPrice, 2) != request.UnitPrice)
            return BadRequest(new { message = "Giá bán chỉ được có tối đa 2 chữ số thập phân." });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await operations.OwnedCharacter(AccountId, request.CharacterId);
        var item = await db.ItemTemplates.FindAsync(request.ItemId)
            ?? throw new KeyNotFoundException("Vật phẩm không tồn tại.");
        if (!item.IsTradeable) return BadRequest(new { message = "Vật phẩm này không được giao dịch." });
        var regionId = await RegionId(character);
        // Listings store a template and quantity. Modified/equipped items retain their individual attributes in inventory.
        var stacks = await db.CharacterInventories.Where(i => i.CharacterId == character.Id && i.ItemId == item.Id
            && !i.IsEquipped && i.EnchantLevel == 0 && i.Durability == null && i.Quantity > 0)
            .OrderBy(i => i.Id).ToListAsync();
        if (stacks.Sum(i => (long)i.Quantity) < request.Quantity)
            return BadRequest(new { message = "Không đủ vật phẩm nguyên bản để ký gửi. Hãy tháo trang bị trước khi bán." });
        var remaining = request.Quantity;
        foreach (var stack in stacks)
        {
            var taken = Math.Min(stack.Quantity, remaining);
            stack.Quantity -= taken;
            stack.UpdatedAt = DateTime.UtcNow;
            remaining -= taken;
            if (remaining == 0) break;
        }
        var listing = new MarketListing
        {
            SellerCharacterId = character.Id, ItemId = item.Id, Quantity = request.Quantity,
            UnitPrice = request.UnitPrice, RegionId = regionId
        };
        db.MarketListings.Add(listing);
        Action(character.Id, "MarketList", request.Quantity, regionId, item.Id);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = "Đã ký gửi vật phẩm lên chợ người chơi.", listingId = listing.Id });
    }

    [HttpPost("listings/{id:long}/buy")]
    public async Task<IActionResult> Buy(long id, BuyListingRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var buyer = await operations.OwnedCharacter(AccountId, request.CharacterId);
        var listing = await db.MarketListings.Include(l => l.Item).Include(l => l.Seller).ThenInclude(s => s.PlayerProfile)
            .SingleOrDefaultAsync(l => l.Id == id) ?? throw new KeyNotFoundException("Tin rao không tồn tại.");
        if (listing.Status != 0 || listing.Quantity < request.Quantity || listing.ExpiresAt <= DateTime.UtcNow)
            return Conflict(new { message = "Tin rao đã hết hàng, hết hạn hoặc số lượng không còn đủ." });
        if (!listing.Item.IsTradeable)
            return BadRequest(new { message = "Vật phẩm này hiện không được phép giao dịch." });
        if (listing.Seller.PlayerProfile.AccountId == AccountId)
            return BadRequest(new { message = "Bạn không thể mua vật phẩm của cùng tài khoản." });
        var regionId = await RegionId(buyer);
        if (regionId != listing.RegionId)
            return BadRequest(new { message = "Hãy đến cùng khu vực với chợ để mua vật phẩm." });
        var total = listing.UnitPrice * request.Quantity;
        operations.Gold(buyer, -total, $"Mua tin rao #{listing.Id}", "PlayerMarket");
        operations.Gold(listing.Seller, total, $"Bán tin rao #{listing.Id}", "PlayerMarket");
        await operations.GrantItem(buyer.Id, listing.ItemId, request.Quantity);
        listing.Quantity -= request.Quantity;
        if (listing.Quantity == 0) listing.Status = 1;
        db.MarketTransactions.Add(new MarketTransaction
        {
            BuyerCharacterId = buyer.Id, SellerCharacterId = listing.SellerCharacterId, ListingId = listing.Id,
            ItemId = listing.ItemId, Quantity = request.Quantity, Price = total, RegionId = regionId
        });
        db.MarketPriceHistories.Add(new MarketPriceHistory
        {
            ItemId = listing.ItemId, RegionId = regionId, Price = listing.UnitPrice
        });
        Action(buyer.Id, "MarketBuy", request.Quantity, regionId, listing.ItemId);
        Action(listing.SellerCharacterId, "MarketSell", request.Quantity, regionId, listing.ItemId);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = "Mua thành công. Vàng đã được chuyển cho người bán.", quantity = request.Quantity, totalPrice = total, remaining = listing.Quantity });
    }

    [HttpPost("listings/{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CharacterReference request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await operations.OwnedCharacter(AccountId, request.CharacterId);
        var listing = await db.MarketListings.SingleOrDefaultAsync(l => l.Id == id && l.SellerCharacterId == character.Id)
            ?? throw new KeyNotFoundException("Không tìm thấy tin rao của nhân vật này.");
        if (listing.Status != 0 || listing.Quantity <= 0)
            return Conflict(new { message = "Tin rao đã đóng hoặc đã bán hết." });
        if (await RegionId(character) != listing.RegionId)
            return BadRequest(new { message = "Hãy quay lại khu vực đã ký gửi để nhận lại hàng." });
        var returned = listing.Quantity;
        await operations.GrantItem(character.Id, listing.ItemId, returned);
        listing.Quantity = 0;
        listing.Status = 2;
        Action(character.Id, "MarketCancel", returned, listing.RegionId, listing.ItemId);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = "Đã hủy tin rao và hoàn trả số hàng chưa bán.", quantity = returned });
    }

    [HttpGet("guilds")]
    public async Task<IActionResult> Guilds() => Ok(await db.Guilds.AsNoTracking().OrderByDescending(g => g.Id).Take(100)
        .Select(g => new
        {
            g.Id, g.Name, g.Level, g.RegionId, regionName = g.Region.Name, g.LeaderCharacterId,
            leaderName = g.Leader.Name, g.CreatedAt,
            members = g.Members.OrderBy(m => m.Id).Select(m => new
            {
                m.CharacterId, name = m.Character.Name, m.Role, m.Contribution, level = m.Character.Level
            })
        }).ToListAsync());

    [HttpPost("guilds")]
    public async Task<IActionResult> CreateGuild(CreateGuildRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length < 3) return BadRequest(new { message = "Tên thương đoàn cần ít nhất 3 ký tự." });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await operations.OwnedCharacter(AccountId, request.CharacterId);
        if (await db.GuildMembers.AnyAsync(m => m.CharacterId == character.Id))
            return Conflict(new { message = "Nhân vật đã thuộc một thương đoàn." });
        if (await db.Guilds.AnyAsync(g => g.Name.ToUpper() == name.ToUpper()))
            return Conflict(new { message = "Tên thương đoàn đã được sử dụng." });
        var regionId = await RegionId(character);
        var guild = new Guild
        {
            Name = name, LeaderCharacterId = character.Id, RegionId = regionId,
            Members = new List<GuildMember> { new() { CharacterId = character.Id, Role = "Leader" } }
        };
        db.Guilds.Add(guild);
        Action(character.Id, "GuildCreate", 1, regionId);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = "Thương đoàn đã được thành lập.", guildId = guild.Id });
    }

    [HttpPost("guilds/{id:long}/join")]
    public async Task<IActionResult> JoinGuild(long id, JoinGuildRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var character = await operations.OwnedCharacter(AccountId, request.CharacterId);
        var guild = await db.Guilds.FindAsync(id) ?? throw new KeyNotFoundException("Thương đoàn không tồn tại.");
        if (await db.GuildMembers.AnyAsync(m => m.CharacterId == character.Id))
            return Conflict(new { message = "Nhân vật đã thuộc một thương đoàn." });
        db.GuildMembers.Add(new GuildMember { GuildId = id, CharacterId = character.Id, Role = request.Role });
        Action(character.Id, "GuildJoin", 1, guild.RegionId);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Ok(new { message = "Đã tham gia thương đoàn.", guildId = id, role = request.Role });
    }

    private async Task<long> RegionId(Character character) => await db.Maps.Where(m => m.Id == character.MapId)
        .Select(m => m.RegionId).SingleAsync();

    private void Action(long characterId, string type, int quantity, long regionId, long? itemId = null) =>
        db.CharacterActionLogs.Add(new CharacterActionLog
        {
            CharacterId = characterId, ActionType = type, ActionValue = quantity, RegionId = regionId, ItemId = itemId
        });
}

public sealed record CreateListingRequest(
    [Range(1, long.MaxValue)] long CharacterId,
    [Range(1, long.MaxValue)] long ItemId,
    [Range(1, 999)] int Quantity,
    [Range(typeof(decimal), "0.01", "1000000")] decimal UnitPrice);
public sealed record BuyListingRequest(
    [Range(1, long.MaxValue)] long CharacterId,
    [Range(1, 999)] int Quantity);
public sealed record CharacterReference([Range(1, long.MaxValue)] long CharacterId);
public sealed record CreateGuildRequest(
    [Range(1, long.MaxValue)] long CharacterId,
    [Required, StringLength(50, MinimumLength = 3)] string Name);
public sealed record JoinGuildRequest(
    [Range(1, long.MaxValue)] long CharacterId,
    [Required, RegularExpression("^(Driver|Scout|Guard|Member)$")] string Role = "Member");
