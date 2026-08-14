using SportReplay.Domain.Enums;

namespace SportReplay.Application.Contracts.Matches;

public record MatchDto(
    Guid Id,
    Guid CourtId,
    string CourtName,
    Guid ClubId,
    string ClubName,
    DateTime StartTime,
    DateTime EndTime,
    MatchStatus Status,
    string? Title);

public record CreateMatchRequest(Guid CourtId, DateTime StartTime, DateTime EndTime, string? Title);
public record UpdateMatchRequest(DateTime StartTime, DateTime EndTime, MatchStatus Status, string? Title);

public record MatchSearchQuery(Guid? ClubId, Guid? CourtId, DateOnly? Date, TimeOnly? Time, int Page = 1, int PageSize = 20);

public record RecordingDto(
    Guid Id,
    Guid MatchId,
    Guid CameraId,
    string CameraName,
    DateTime StartedAt,
    DateTime? EndedAt,
    RecordingStatus Status,
    string? HlsPath,
    int? DurationSeconds);

public record CreateRecordingRequest(Guid MatchId, Guid CameraId);
