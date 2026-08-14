using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class Payment : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? VideoClipId { get; set; }
    public Guid? VideoRequestId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ExternalPaymentId { get; set; }
    public string? ExternalOrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PaymentMethod { get; set; }
    public string? InitPoint { get; set; }
    public string? PreferenceId { get; set; }

    public User User { get; set; } = null!;
    public VideoClip? VideoClip { get; set; }
    public VideoRequest? VideoRequest { get; set; }
    public Product? Product { get; set; }
}
