using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using GameWorld.Domain.Entities;
using GameWorld.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameWorld.Api.Controllers;

[ApiController]
[Route("api/character")]
[Authorize(Roles = "Player")]
public class CharacterController(GameDbContext db) : ControllerBase
{
    private static readonly SemaphoreSlim CreationGate = new(1, 1);

    [HttpPost("create")]
    public async Task<IActionResult> CreateCharacter(CreateCharacterRequest request)
    {
        if (!long.TryParse(User.FindFirstValue("accountId"), out var accountId)) return Unauthorized();
        if (request.MapId != 1)
            return BadRequest(new { message = "Mọi nhân vật mới phải bắt đầu tại Làng Tân Thủ." });
        var name = request.Name.Trim();
        if (name.Length < 3) return BadRequest(new { message = "Tên nhân vật cần ít nhất 3 ký tự." });
        await CreationGate.WaitAsync(HttpContext.RequestAborted);
        try
        {
            var profile = await db.PlayerProfiles.Include(p => p.Characters).FirstOrDefaultAsync(p => p.AccountId == accountId);
            if (profile is null) return NotFound(new { message = "Không tìm thấy hồ sơ người chơi." });
            if (profile.Characters.Count >= 3)
                return BadRequest(new { message = "Mỗi tài khoản chỉ được tạo tối đa 3 nhân vật." });
            if (await db.Characters.AnyAsync(c => c.Name.ToUpper() == name.ToUpper()))
                return Conflict(new { message = "Tên nhân vật đã được sử dụng." });
            var character = new Character
            {
                PlayerProfileId = profile.Id, Name = name, ClassId = request.ClassId, RaceId = request.RaceId,
                MapId = 1, IsMain = profile.Characters.Count == 0,
                Stats = new CharacterStats
                {
                    Strength = 10, Agility = 10, Vitality = 10, Intelligence = 10,
                    Luck = 5, Attack = 20, Defense = 15, CritRate = 0.05m, Speed = 10
                },
                Inventory = new List<CharacterInventory>
                {
                    new() { ItemId = 1, Quantity = 5, SlotIndex = 0 },
                    new() { ItemId = 2, Quantity = 3, SlotIndex = 1 }
                }
            };
            db.Characters.Add(character);
            await db.SaveChangesAsync();
            return Ok(new { message = "Nhân vật đã sẵn sàng tại Làng Tân Thủ.", characterId = character.Id });
        }
        finally { CreationGate.Release(); }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyCharacters()
    {
        if (!long.TryParse(User.FindFirstValue("accountId"), out var accountId)) return Unauthorized();
        var characters = await db.Characters.AsNoTracking().Where(c => c.PlayerProfile.AccountId == accountId)
            .OrderBy(c => c.Id).Select(c => new
            {
                c.Id, c.Name, c.ClassId, c.RaceId, c.Level, c.Experience, c.CurrentHP, c.CurrentMP,
                c.PositionX, c.PositionY, c.PositionZ, c.MapId, c.IsMain,
                gold = c.PlayerProfile.Gold, gem = c.PlayerProfile.Gem, stamina = c.PlayerProfile.Stamina,
                stats = c.Stats == null ? null : new
                {
                    c.Stats.Strength, c.Stats.Agility, c.Stats.Vitality, c.Stats.Intelligence, c.Stats.Luck,
                    c.Stats.Attack, c.Stats.Defense, c.Stats.CritRate, c.Stats.Speed
                },
                inventory = c.Inventory.Where(i => i.Quantity > 0).Select(i => new
                {
                    i.Id, i.ItemId, name = i.Item.Name, i.Item.Category, i.Item.Rarity,
                    i.Quantity, i.IsEquipped, i.SlotIndex
                }),
                skills = c.Skills.Where(s => s.IsUnlocked).Select(s => new { s.SkillId, name = s.Skill.Name, s.Level })
            }).ToListAsync();
        return Ok(characters);
    }
}

public class CreateCharacterRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;
    [Range(1, 3)]
    public int ClassId { get; set; } = 1;
    [Range(1, 3)]
    public int RaceId { get; set; } = 1;
    public long MapId { get; set; } = 1;
}
