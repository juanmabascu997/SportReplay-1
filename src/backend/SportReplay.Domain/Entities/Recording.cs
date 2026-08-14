using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class Recording : EntityBase
{
    public Guid MatchId { get; set; }
    public Guid CameraId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public RecordingStatus Status { get; set; } = RecordingStatus.Recording;
    public string? StoragePath { get; set; }
    public string? HlsPath { get; set; }
    public int? DurationSeconds { get; set; }
    public long? FileSize { get; set; }

    public Match Match { get; set; } = null!;
    public Camera Camera { get; set; } = null!;
    public ICollection<VideoSegment> Segments { get; set; } = new List<VideoSegment>();
    public ICollection<VideoClip> Clips { get; set; } = new List<VideoClip>();
}
