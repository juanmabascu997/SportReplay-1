using SportReplay.Domain.Common;

namespace SportReplay.Domain.Entities;

public class VideoSegment : EntityBase
{
    public Guid RecordingId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }

    public Recording Recording { get; set; } = null!;
}
