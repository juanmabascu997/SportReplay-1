using SportReplay.Domain.Enums;

namespace SportReplay.Application.Contracts.Videos;

public record VideoClipDto(
    Guid Id,
    Guid MatchId,
    Guid RecordingId,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int DurationSeconds,
    VideoClipStatus Status,
    bool IsPublic,
    DateTime? ExpiresAt,
    string? ThumbnailUrl,
    string? StreamUrl);

public record CreateVideoClipRequest(Guid MatchId, Guid RecordingId, TimeSpan StartTime, TimeSpan EndTime);
public record CreateVideoClipResponse(Guid ClipId, VideoClipStatus Status);

public record VideoStreamDto(Guid Id, string Url, string Kind, DateTime ExpiresAt);

public record VideoRequestDto(
    Guid Id,
    Guid UserId,
    Guid MatchId,
    Guid? VideoClipId,
    string PhoneNumber,
    VideoRequestStatus Status,
    DateTime RequestedAt,
    DateTime? CompletedAt);

public record CreateVideoRequestRequest(Guid MatchId, Guid VideoClipId, string PhoneNumber);
