using SportReplay.Domain.Common;

namespace SportReplay.Domain.Entities;

public class ProcessingJob : EntityBase
{
    public string JobType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
