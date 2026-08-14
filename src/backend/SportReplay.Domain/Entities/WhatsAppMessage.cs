using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class WhatsAppMessage : EntityBase
{
    public Guid VideoRequestId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? MediaUrl { get; set; }
    public WhatsAppMessageStatus Status { get; set; } = WhatsAppMessageStatus.Queued;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }

    public VideoRequest VideoRequest { get; set; } = null!;
}
