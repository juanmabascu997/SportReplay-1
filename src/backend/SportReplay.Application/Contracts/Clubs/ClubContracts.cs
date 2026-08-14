namespace SportReplay.Application.Contracts.Clubs;

public record ClubDto(
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    string? City,
    string? Province,
    string Country,
    string? Phone,
    string? Email,
    Guid OwnerUserId,
    bool IsActive,
    bool AllowFullMatchDownload,
    DateTime CreatedAt);

public record CreateClubRequest(
    string Name,
    string? Description,
    string? Address,
    string? City,
    string? Province,
    string? Country,
    string? Phone,
    string? Email);

public record UpdateClubRequest(
    string Name,
    string? Description,
    string? Address,
    string? City,
    string? Province,
    string? Country,
    string? Phone,
    string? Email,
    bool IsActive,
    bool AllowFullMatchDownload);

public record ClubSettingsDto(
    Guid ClubId,
    decimal ClipPrice,
    decimal FullMatchPrice,
    decimal PricePerMinute,
    decimal CommissionPercent,
    int VideoRetentionDays,
    int MaxClipDurationSeconds,
    bool AllowFullMatchDownload,
    string Currency);

public record UpdateClubSettingsRequest(
    decimal ClipPrice,
    decimal FullMatchPrice,
    decimal PricePerMinute,
    decimal CommissionPercent,
    int VideoRetentionDays,
    int MaxClipDurationSeconds,
    bool AllowFullMatchDownload,
    string Currency);
