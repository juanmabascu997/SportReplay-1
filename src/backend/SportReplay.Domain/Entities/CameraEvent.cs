using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class CameraEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CameraId { get; set; }
    public CameraEventType EventType { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Payload { get; set; }

    public Camera Camera { get; set; } = null!;
}
