using SportReplay.Domain.Common;

namespace SportReplay.Domain.Entities;

public class CameraConfiguration : EntityBase
{
    public Guid CameraId { get; set; }
    public string Resolution { get; set; } = "1280x720";
    public int Fps { get; set; } = 25;
    public int Bitrate { get; set; } = 2000;
    public bool RecordingEnabled { get; set; } = true;
    public int SegmentDurationSeconds { get; set; } = 6;

    public Camera Camera { get; set; } = null!;
}
