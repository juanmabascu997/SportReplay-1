using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Videos;
using SportReplay.Application.Exceptions;
using SportReplay.Application.Options;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Services;

public class VideoClipService : IVideoClipService
{
    private readonly SportReplayDbContext _db;
    private readonly IStorageService _storage;
    private readonly VideoOptions _videoOptions;
    private readonly StorageOptions _storageOptions;

    public VideoClipService(
        SportReplayDbContext db,
        IStorageService storage,
        IOptions<VideoOptions> videoOptions,
        IOptions<StorageOptions> storageOptions)
    {
        _db = db;
        _storage = storage;
        _videoOptions = videoOptions.Value;
        _storageOptions = storageOptions.Value;
    }

    public async Task<IReadOnlyList<VideoClipDto>> GetByMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var clips = await _db.VideoClips.AsNoTracking().Where(x => x.MatchId == matchId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var result = new List<VideoClipDto>();
        foreach (var clip in clips)
        {
            result.Add(await MapAsync(clip, cancellationToken));
        }

        return result;
    }

    public async Task<VideoClipDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var clip = await _db.VideoClips.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("VideoClip", id);
        return await MapAsync(clip, cancellationToken);
    }

    public async Task<CreateVideoClipResponse> CreateAsync(CreateVideoClipRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndTime <= request.StartTime)
        {
            throw new AppException("endTime must be greater than startTime.");
        }

        var duration = (int)(request.EndTime - request.StartTime).TotalSeconds;
        var match = await _db.Matches.Include(x => x.Court).ThenInclude(x => x.Club).ThenInclude(x => x.Settings)
            .FirstOrDefaultAsync(x => x.Id == request.MatchId, cancellationToken)
            ?? throw new NotFoundException("Match", request.MatchId);

        var maxDuration = match.Court.Club.Settings?.MaxClipDurationSeconds ?? _videoOptions.MaxClipDurationSeconds;
        if (duration > maxDuration)
        {
            throw new AppException($"Clip duration cannot exceed {maxDuration} seconds.");
        }

        var recording = await _db.Recordings.FirstOrDefaultAsync(x => x.Id == request.RecordingId && x.MatchId == request.MatchId, cancellationToken)
            ?? throw new NotFoundException("Recording", request.RecordingId);

        var retention = match.Court.Club.Settings?.VideoRetentionDays ?? _videoOptions.RetentionDays;
        var clip = new VideoClip
        {
            MatchId = request.MatchId,
            RecordingId = recording.Id,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            DurationSeconds = duration,
            Status = VideoClipStatus.Processing,
            ExpiresAt = DateTime.UtcNow.AddDays(retention)
        };
        _db.VideoClips.Add(clip);
        _db.ProcessingJobs.Add(new ProcessingJob
        {
            JobType = "ProcessClip",
            Payload = clip.Id.ToString(),
            Status = "pending"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new CreateVideoClipResponse(clip.Id, clip.Status);
    }

    public async Task<VideoStreamDto> GetStreamAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var clip = await _db.VideoClips.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("VideoClip", id);
        if (clip.Status != VideoClipStatus.Ready || string.IsNullOrWhiteSpace(clip.StoragePath))
        {
            throw new AppException("Clip is not ready for playback.", 409);
        }

        var url = await SignAsync(clip.StoragePath, cancellationToken);
        return new VideoStreamDto(clip.Id, url, "mp4", DateTime.UtcNow.AddMinutes(_storageOptions.SignedUrlExpiryMinutes));
    }

    public async Task<VideoStreamDto> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetStreamAsync(id, cancellationToken);
    }

    public async Task<VideoStreamDto> GetRecordingStreamAsync(Guid recordingId, CancellationToken cancellationToken = default)
    {
        var recording = await _db.Recordings.Include(x => x.Match).ThenInclude(x => x.Court).ThenInclude(x => x.Club)
            .FirstOrDefaultAsync(x => x.Id == recordingId, cancellationToken)
            ?? throw new NotFoundException("Recording", recordingId);

        var path = recording.StoragePath ?? recording.HlsPath ?? throw new AppException("Recording is not ready.", 409);
        var url = await SignAsync(path, cancellationToken);
        var kind = path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) ? "hls" : "mp4";
        return new VideoStreamDto(recording.Id, url, kind, DateTime.UtcNow.AddMinutes(_storageOptions.SignedUrlExpiryMinutes));
    }

    private async Task<VideoClipDto> MapAsync(VideoClip clip, CancellationToken cancellationToken)
    {
        string? thumb = null;
        string? stream = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(clip.ThumbnailPath))
            {
                thumb = await SignAsync(clip.ThumbnailPath, cancellationToken);
            }

            if (clip.Status == VideoClipStatus.Ready && !string.IsNullOrWhiteSpace(clip.StoragePath))
            {
                stream = await SignAsync(clip.StoragePath, cancellationToken);
            }
        }
        catch
        {
            // Storage may be unavailable in local tests.
        }

        return new VideoClipDto(clip.Id, clip.MatchId, clip.RecordingId, clip.StartTime, clip.EndTime, clip.DurationSeconds, clip.Status, clip.IsPublic, clip.ExpiresAt, thumb, stream);
    }

    private Task<string> SignAsync(string key, CancellationToken cancellationToken)
        => _storage.GetSignedUrlAsync(key, TimeSpan.FromMinutes(_storageOptions.SignedUrlExpiryMinutes), cancellationToken);
}

public class VideoRequestService : IVideoRequestService
{
    private readonly SportReplayDbContext _db;
    private readonly ICurrentUser _currentUser;

    public VideoRequestService(SportReplayDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<VideoRequestDto> CreateAsync(CreateVideoRequestRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAppException();
        }

        Guid? clipId = request.VideoClipId;
        if (clipId.HasValue)
        {
            var exists = await _db.VideoClips.AnyAsync(x => x.Id == clipId.Value, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException("VideoClip", clipId.Value);
            }
        }
        else
        {
            var matchExists = await _db.Matches.AnyAsync(x => x.Id == request.MatchId, cancellationToken);
            if (!matchExists)
            {
                throw new NotFoundException("Match", request.MatchId);
            }
        }

        var entity = new VideoRequest
        {
            UserId = _currentUser.UserId.Value,
            MatchId = request.MatchId,
            VideoClipId = clipId,
            PhoneNumber = request.PhoneNumber,
            Status = VideoRequestStatus.AwaitingPayment,
            RequestedAt = DateTime.UtcNow
        };
        _db.VideoRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<VideoRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.VideoRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("VideoRequest", id);
        return Map(entity);
    }

    private static VideoRequestDto Map(VideoRequest entity) => new(
        entity.Id, entity.UserId, entity.MatchId, entity.VideoClipId, entity.PhoneNumber, entity.Status, entity.RequestedAt, entity.CompletedAt);
}
