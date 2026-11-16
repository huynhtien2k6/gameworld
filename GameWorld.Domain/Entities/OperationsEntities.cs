using System.ComponentModel.DataAnnotations;

namespace GameWorld.Domain.Entities;

public class LiveEvent
{
    public long Id { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(1000)] public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class GachaEntry
{
    public long Id { get; set; }
    public long ItemId { get; set; }
    public int Weight { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public ItemTemplate Item { get; set; } = null!;
}
