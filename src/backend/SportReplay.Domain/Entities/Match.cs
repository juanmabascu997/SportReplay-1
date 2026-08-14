using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class Match : EntityBase
{
    public Guid CourtId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public string? Title { get; set; }

    public Court Court { get; set; } = null!;
    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
    public ICollection<VideoClip> Clips { get; set; } = new List<VideoClip>();
    public ICollection<VideoRequest> VideoRequests { get; set; } = new List<VideoRequest>();
}
