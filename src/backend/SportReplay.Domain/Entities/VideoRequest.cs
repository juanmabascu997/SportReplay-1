using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class VideoRequest : EntityBase
{
    public Guid UserId { get; set; }
    public Guid MatchId { get; set; }
    public Guid? VideoClipId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public VideoRequestStatus Status { get; set; } = VideoRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public User User { get; set; } = null!;
    public Match Match { get; set; } = null!;
    public VideoClip? VideoClip { get; set; }
    public ICollection<WhatsAppMessage> WhatsAppMessages { get; set; } = new List<WhatsAppMessage>();
}
