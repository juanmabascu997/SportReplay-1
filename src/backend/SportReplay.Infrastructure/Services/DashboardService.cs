using Microsoft.EntityFrameworkCore;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Dashboard;
using SportReplay.Application.Exceptions;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly SportReplayDbContext _db;
    private readonly IClubAuthorization _auth;
    private readonly ICurrentUser _currentUser;

    public DashboardService(SportReplayDbContext db, IClubAuthorization auth, ICurrentUser currentUser)
    {
        _db = db;
        _auth = auth;
        _currentUser = currentUser;
    }

    public async Task<ClubDashboardDto> GetClubDashboardAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(clubId, cancellationToken);
        var courtIds = await _db.Courts.Where(x => x.ClubId == clubId).Select(x => x.Id).ToListAsync(cancellationToken);
        var cameras = await _db.Cameras.Where(x => courtIds.Contains(x.CourtId)).ToListAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var matchesToday = await _db.Matches.CountAsync(x => courtIds.Contains(x.CourtId) && x.StartTime >= today && x.StartTime < today.AddDays(1), cancellationToken);
        var matchIds = await _db.Matches.Where(x => courtIds.Contains(x.CourtId)).Select(x => x.Id).ToListAsync(cancellationToken);
        var videos = await _db.Recordings.CountAsync(x => matchIds.Contains(x.MatchId), cancellationToken);
        var sold = await _db.Payments.CountAsync(x => x.Status == PaymentStatus.Approved && x.VideoClip != null && matchIds.Contains(x.VideoClip.MatchId), cancellationToken);
        var revenue = await _db.Payments.Where(x => x.Status == PaymentStatus.Approved && x.VideoClip != null && matchIds.Contains(x.VideoClip.MatchId)).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var pending = await _db.Payments.CountAsync(x => x.Status == PaymentStatus.Pending && x.VideoClip != null && matchIds.Contains(x.VideoClip.MatchId), cancellationToken);
        var errors = await _db.CameraEvents
            .Where(x => cameras.Select(c => c.Id).Contains(x.CameraId) && x.EventType == CameraEventType.Error)
            .OrderByDescending(x => x.OccurredAt)
            .Take(10)
            .Select(x => new CameraErrorDto(x.CameraId, x.Camera.Name, x.Message, x.OccurredAt))
            .ToListAsync(cancellationToken);

        return new ClubDashboardDto(
            courtIds.Count,
            cameras.Count(x => x.Status == CameraStatus.Online),
            cameras.Count(x => x.Status == CameraStatus.Offline),
            cameras.Count(x => x.Status == CameraStatus.Error),
            matchesToday,
            videos,
            sold,
            revenue,
            pending,
            errors);
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new ForbiddenException();
        }

        var today = DateTime.UtcNow.Date;
        return new AdminDashboardDto(
            await _db.Clubs.CountAsync(x => x.IsActive, cancellationToken),
            await _db.Users.CountAsync(cancellationToken),
            await _db.Cameras.CountAsync(x => x.Status == CameraStatus.Online, cancellationToken),
            await _db.Matches.CountAsync(x => x.StartTime >= today && x.StartTime < today.AddDays(1), cancellationToken),
            await _db.Payments.Where(x => x.Status == PaymentStatus.Approved && x.CreatedAt >= today).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0);
    }
}
