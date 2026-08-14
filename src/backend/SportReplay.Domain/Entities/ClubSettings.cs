using SportReplay.Domain.Common;

namespace SportReplay.Domain.Entities;

public class ClubSettings : EntityBase
{
    public Guid ClubId { get; set; }
    public decimal ClipPrice { get; set; } = 1500m;
    public decimal FullMatchPrice { get; set; } = 4500m;
    public decimal PricePerMinute { get; set; } = 250m;
    public decimal CommissionPercent { get; set; } = 10m;
    public int VideoRetentionDays { get; set; } = 7;
    public int MaxClipDurationSeconds { get; set; } = 60;
    public bool AllowFullMatchDownload { get; set; }
    public string Currency { get; set; } = "ARS";

    public Club Club { get; set; } = null!;
}
