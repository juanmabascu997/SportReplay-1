namespace SportReplay.Application.Contracts.Dashboard;

public record ClubDashboardDto(
    int CourtCount,
    int CamerasOnline,
    int CamerasOffline,
    int CamerasError,
    int MatchesToday,
    int VideosGenerated,
    int ClipsSold,
    decimal Revenue,
    int PendingPayments,
    IReadOnlyList<CameraErrorDto> CameraErrors);

public record CameraErrorDto(Guid CameraId, string CameraName, string Message, DateTime OccurredAt);

public record AdminDashboardDto(
    int Clubs,
    int Users,
    int ActiveCameras,
    int MatchesToday,
    decimal RevenueToday);
