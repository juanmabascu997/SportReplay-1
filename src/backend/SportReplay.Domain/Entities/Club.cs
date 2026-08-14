using SportReplay.Domain.Common;

namespace SportReplay.Domain.Entities;

public class Club : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string Country { get; set; } = "Argentina";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid OwnerUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AllowFullMatchDownload { get; set; }

    public User Owner { get; set; } = null!;
    public ClubSettings? Settings { get; set; }
    public ICollection<Court> Courts { get; set; } = new List<Court>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
}
