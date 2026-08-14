using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class Camera : EntityBase
{
    public Guid CourtId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? RtspUrl { get; set; }
    public string? OnvifUrl { get; set; }
    public string? Username { get; set; }
    public string? PasswordEncrypted { get; set; }
    public CameraProtocol Protocol { get; set; } = CameraProtocol.Rtsp;
    public CameraStatus Status { get; set; } = CameraStatus.Offline;
    public DateTime? LastHeartbeat { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSimulated { get; set; }

    public Court Court { get; set; } = null!;
    public ICollection<CameraConfiguration> Configurations { get; set; } = new List<CameraConfiguration>();
    public ICollection<Recording> Recordings { get; set; } = new List<Recording>();
    public ICollection<CameraEvent> Events { get; set; } = new List<CameraEvent>();
}
