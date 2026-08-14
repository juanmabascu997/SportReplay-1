using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class VideoClip : EntityBase
{
    public Guid MatchId { get; set; }
    public Guid RecordingId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public string? StoragePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public VideoClipStatus Status { get; set; } = VideoClipStatus.Processing;
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? FailureReason { get; set; }

    public Match Match { get; set; } = null!;
    public Recording Recording { get; set; } = null!;
    public ICollection<VideoRequest> VideoRequests { get; set; } = new List<VideoRequest>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
