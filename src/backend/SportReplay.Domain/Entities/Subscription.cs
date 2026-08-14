using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class Subscription : EntityBase
{
    public Guid ClubId { get; set; }
    public string? ExternalSubscriptionId { get; set; }
    public string Plan { get; set; } = "basic";
    public decimal Amount { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public Club Club { get; set; } = null!;
}
