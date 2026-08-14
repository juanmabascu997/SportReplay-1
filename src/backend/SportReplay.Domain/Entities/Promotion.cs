using SportReplay.Domain.Common;

namespace SportReplay.Domain.Entities;

public class Promotion : EntityBase
{
    public Guid ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Club Club { get; set; } = null!;
}
