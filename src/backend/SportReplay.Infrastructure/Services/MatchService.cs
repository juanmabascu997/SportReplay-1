using Microsoft.EntityFrameworkCore;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Matches;
using SportReplay.Application.Exceptions;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Services;

public class MatchService : IMatchService
{
    private readonly SportReplayDbContext _db;
    private readonly IClubAuthorization _auth;

    public MatchService(SportReplayDbContext db, IClubAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PagedResult<MatchDto>> SearchAsync(MatchSearchQuery query, CancellationToken cancellationToken = default)
    {
        var matches = _db.Matches
            .Include(x => x.Court).ThenInclude(x => x.Club)
            .AsNoTracking()
            .AsQueryable();

        if (query.ClubId.HasValue)
        {
            matches = matches.Where(x => x.Court.ClubId == query.ClubId);
        }

        if (query.CourtId.HasValue)
        {
            matches = matches.Where(x => x.CourtId == query.CourtId);
        }

        if (query.Date.HasValue)
        {
            var start = query.Date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            matches = matches.Where(x => x.StartTime >= start && x.StartTime < end);
        }

        if (query.Time.HasValue)
        {
            var hour = query.Time.Value.Hour;
            matches = matches.Where(x => x.StartTime.Hour == hour);
        }

        var total = await matches.CountAsync(cancellationToken);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = await matches.OrderByDescending(x => x.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MatchDto>
        {
            Items = items.Select(Map).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MatchDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var match = await _db.Matches.Include(x => x.Court).ThenInclude(x => x.Club)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Match", id);
        return Map(match);
    }

    public async Task<MatchDto> CreateAsync(CreateMatchRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCourtAsync(request.CourtId, cancellationToken);
        var match = new Match
        {
            CourtId = request.CourtId,
            StartTime = request.StartTime.ToUniversalTime(),
            EndTime = request.EndTime.ToUniversalTime(),
            Title = request.Title,
            Status = MatchStatus.Scheduled
        };
        _db.Matches.Add(match);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(match.Id, cancellationToken);
    }

    public async Task<MatchDto> UpdateAsync(Guid id, UpdateMatchRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForMatchAsync(id, cancellationToken);
        var match = await _db.Matches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Match", id);
        match.StartTime = request.StartTime.ToUniversalTime();
        match.EndTime = request.EndTime.ToUniversalTime();
        match.Status = request.Status;
        match.Title = request.Title;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForMatchAsync(id, cancellationToken);
        var match = await _db.Matches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Match", id);
        match.Status = MatchStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecordingDto>> GetRecordingsAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var recordings = await _db.Recordings.Include(x => x.Camera)
            .AsNoTracking()
            .Where(x => x.MatchId == matchId)
            .ToListAsync(cancellationToken);
        return recordings.Select(MapRecording).ToList();
    }

    public async Task<RecordingDto> CreateRecordingAsync(CreateRecordingRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForMatchAsync(request.MatchId, cancellationToken);
        var recording = new Recording
        {
            MatchId = request.MatchId,
            CameraId = request.CameraId,
            StartedAt = DateTime.UtcNow,
            Status = RecordingStatus.Recording
        };
        _db.Recordings.Add(recording);
        var match = await _db.Matches.FirstAsync(x => x.Id == request.MatchId, cancellationToken);
        match.Status = MatchStatus.Recording;
        await _db.SaveChangesAsync(cancellationToken);
        var created = await _db.Recordings.Include(x => x.Camera).FirstAsync(x => x.Id == recording.Id, cancellationToken);
        return MapRecording(created);
    }

    private static MatchDto Map(Match match) => new(
        match.Id,
        match.CourtId,
        match.Court.Name,
        match.Court.ClubId,
        match.Court.Club.Name,
        match.StartTime,
        match.EndTime,
        match.Status,
        match.Title);

    private static RecordingDto MapRecording(Recording recording) => new(
        recording.Id,
        recording.MatchId,
        recording.CameraId,
        recording.Camera.Name,
        recording.StartedAt,
        recording.EndedAt,
        recording.Status,
        recording.HlsPath,
        recording.DurationSeconds);
}
