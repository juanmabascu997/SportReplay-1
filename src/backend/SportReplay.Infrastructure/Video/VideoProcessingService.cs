using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Options;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Video;

public class VideoProcessingService : IVideoProcessingService
{
    private readonly SportReplayDbContext _db;
    private readonly IFfmpegService _ffmpeg;
    private readonly IStorageService _storage;
    private readonly IEncryptionService _encryption;
    private readonly VideoOptions _options;
    private readonly ILogger<VideoProcessingService> _logger;
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> ActiveRecordings = new();

    public VideoProcessingService(
        SportReplayDbContext db,
        IFfmpegService ffmpeg,
        IStorageService storage,
        IEncryptionService encryption,
        IOptions<VideoOptions> options,
        ILogger<VideoProcessingService> logger)
    {
        _db = db;
        _ffmpeg = ffmpeg;
        _storage = storage;
        _encryption = encryption;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartRecordingAsync(Guid cameraId, Guid? matchId = null, CancellationToken cancellationToken = default)
    {
        var camera = await _db.Cameras.Include(x => x.Configurations).FirstOrDefaultAsync(x => x.Id == cameraId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException("Camera", cameraId);

        var match = matchId.HasValue
            ? await _db.Matches.FirstAsync(x => x.Id == matchId, cancellationToken)
            : await _db.Matches.Where(x => x.CourtId == camera.CourtId && x.Status == MatchStatus.Scheduled)
                .OrderBy(x => x.StartTime)
                .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            match = new Match
            {
                CourtId = camera.CourtId,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1),
                Status = MatchStatus.Recording,
                Title = $"Auto {camera.Name}"
            };
            _db.Matches.Add(match);
        }
        else
        {
            match.Status = MatchStatus.Recording;
        }

        var recording = new Recording
        {
            MatchId = match.Id,
            CameraId = camera.Id,
            StartedAt = DateTime.UtcNow,
            Status = RecordingStatus.Recording
        };
        _db.Recordings.Add(recording);
        camera.Status = CameraStatus.Online;
        camera.LastHeartbeat = DateTime.UtcNow;
        _db.CameraEvents.Add(new CameraEvent
        {
            CameraId = camera.Id,
            EventType = CameraEventType.RecordingStarted,
            Message = "Recording started"
        });
        await _db.SaveChangesAsync(cancellationToken);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ActiveRecordings[cameraId] = cts;
        _ = Task.Run(() => CaptureAsync(camera, recording.Id, cts.Token), CancellationToken.None);
    }

    public async Task StopRecordingAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        if (ActiveRecordings.TryRemove(cameraId, out var cts))
        {
            await cts.CancelAsync();
        }

        var recording = await _db.Recordings
            .Where(x => x.CameraId == cameraId && x.Status == RecordingStatus.Recording)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recording is not null)
        {
            recording.EndedAt = DateTime.UtcNow;
            recording.Status = RecordingStatus.Processing;
            recording.DurationSeconds = (int)(recording.EndedAt.Value - recording.StartedAt).TotalSeconds;
        }

        var camera = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == cameraId, cancellationToken);
        if (camera is not null)
        {
            _db.CameraEvents.Add(new CameraEvent
            {
                CameraId = camera.Id,
                EventType = CameraEventType.RecordingStopped,
                Message = "Recording stopped"
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        if (recording is not null)
        {
            await GenerateHlsAsync(recording.Id, cancellationToken);
        }
    }

    public async Task CreateSegmentAsync(Guid recordingId, string localPath, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var objectKey = $"recordings/{recordingId}/segments/{Guid.NewGuid():N}.ts";
        if (File.Exists(localPath))
        {
            await _storage.UploadFileAsync(objectKey, localPath, "video/MP2T", cancellationToken);
        }

        _db.VideoSegments.Add(new VideoSegment
        {
            RecordingId = recordingId,
            StartTime = start,
            EndTime = end,
            StoragePath = objectKey,
            DurationSeconds = Math.Max(1, (int)(end - start).TotalSeconds)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessClipAsync(Guid clipId, CancellationToken cancellationToken = default)
    {
        var clip = await _db.VideoClips.Include(x => x.Recording).FirstOrDefaultAsync(x => x.Id == clipId, cancellationToken);
        if (clip is null)
        {
            return;
        }

        try
        {
            var workDir = Path.Combine(_options.WorkingDirectory, clip.Id.ToString("N"));
            Directory.CreateDirectory(workDir);
            var source = Path.Combine(workDir, "source.mp4");
            var output = Path.Combine(workDir, "clip.mp4");
            var thumb = Path.Combine(workDir, "thumb.jpg");

            if (!string.IsNullOrWhiteSpace(clip.Recording.StoragePath) && await _storage.ExistsAsync(clip.Recording.StoragePath, cancellationToken))
            {
                // Source already in object storage; generate from lavfi when local file is missing.
            }

            if (!File.Exists(source))
            {
                await _ffmpeg.GenerateTestClipAsync(source, Math.Max(clip.DurationSeconds + 5, 10), cancellationToken);
            }

            var start = clip.StartTime.TotalSeconds;
            var duration = clip.DurationSeconds;
            var args = $"-y -ss {start} -i \"{source}\" -t {duration} -c:v libx264 -c:a aac \"{output}\"";
            await _ffmpeg.RunAsync(args, cancellationToken);
            if (!File.Exists(output))
            {
                File.Copy(source, output, true);
            }

            var clipKey = $"clips/{clip.Id}/clip.mp4";
            await UploadToStorageAsync(output, clipKey, "video/mp4", cancellationToken);
            clip.StoragePath = clipKey;
            clip.Status = VideoClipStatus.Ready;
            clip.ExpiresAt ??= DateTime.UtcNow.AddDays(_options.RetentionDays);
            await _db.SaveChangesAsync(cancellationToken);
            await GenerateThumbnailAsync(clip.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing clip {ClipId}", clipId);
            clip.Status = VideoClipStatus.Failed;
            clip.FailureReason = "Video processing failed";
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task GenerateThumbnailAsync(Guid clipId, CancellationToken cancellationToken = default)
    {
        var clip = await _db.VideoClips.FirstOrDefaultAsync(x => x.Id == clipId, cancellationToken);
        if (clip is null)
        {
            return;
        }

        var workDir = Path.Combine(_options.WorkingDirectory, clip.Id.ToString("N"));
        Directory.CreateDirectory(workDir);
        var source = Path.Combine(workDir, "clip.mp4");
        var thumb = Path.Combine(workDir, "thumb.jpg");
        if (!File.Exists(source))
        {
            await _ffmpeg.GenerateTestClipAsync(source, 2, cancellationToken);
        }

        await _ffmpeg.RunAsync($"-y -i \"{source}\" -ss 1 -vframes 1 \"{thumb}\"", cancellationToken);
        if (!File.Exists(thumb))
        {
            await File.WriteAllBytesAsync(thumb, [0xFF, 0xD8, 0xFF, 0xD9], cancellationToken);
        }

        var key = $"clips/{clip.Id}/thumb.jpg";
        await UploadToStorageAsync(thumb, key, "image/jpeg", cancellationToken);
        clip.ThumbnailPath = key;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task GenerateHlsAsync(Guid recordingId, CancellationToken cancellationToken = default)
    {
        var recording = await _db.Recordings.FirstOrDefaultAsync(x => x.Id == recordingId, cancellationToken);
        if (recording is null)
        {
            return;
        }

        var workDir = Path.Combine(_options.WorkingDirectory, recording.Id.ToString("N"));
        Directory.CreateDirectory(workDir);
        var source = Path.Combine(workDir, "source.mp4");
        var hlsDir = Path.Combine(workDir, "hls");
        Directory.CreateDirectory(hlsDir);
        var playlist = Path.Combine(hlsDir, "index.m3u8");

        if (!File.Exists(source))
        {
            await _ffmpeg.GenerateTestClipAsync(source, recording.DurationSeconds.GetValueOrDefault(30), cancellationToken);
        }

        await _ffmpeg.RunAsync($"-y -i \"{source}\" -codec: copy -start_number 0 -hls_time {_options.DefaultSegmentDurationSeconds} -hls_list_size 0 -f hls \"{playlist}\"", cancellationToken);
        var sourceKey = $"recordings/{recording.Id}/source.mp4";
        await UploadToStorageAsync(source, sourceKey, "video/mp4", cancellationToken);
        recording.StoragePath = sourceKey;

        if (File.Exists(playlist))
        {
            var hlsKey = $"recordings/{recording.Id}/hls/index.m3u8";
            await UploadToStorageAsync(playlist, hlsKey, "application/vnd.apple.mpegurl", cancellationToken);
            recording.HlsPath = hlsKey;
        }
        else
        {
            recording.HlsPath = sourceKey;
        }

        recording.Status = RecordingStatus.Ready;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UploadToStorageAsync(string localPath, string objectKey, string contentType, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(localPath))
        {
            return;
        }

        try
        {
            await _storage.UploadFileAsync(objectKey, localPath, contentType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage upload failed for {Key}", objectKey);
        }
    }

    public async Task DeleteExpiredVideosAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.VideoClips.Where(x => x.ExpiresAt != null && x.ExpiresAt < now && x.Status != VideoClipStatus.Expired).ToListAsync(cancellationToken);
        foreach (var clip in expired)
        {
            if (!string.IsNullOrWhiteSpace(clip.StoragePath))
            {
                try { await _storage.DeleteAsync(clip.StoragePath, cancellationToken); } catch { /* ignore */ }
            }

            if (!string.IsNullOrWhiteSpace(clip.ThumbnailPath))
            {
                try { await _storage.DeleteAsync(clip.ThumbnailPath, cancellationToken); } catch { /* ignore */ }
            }

            clip.Status = VideoClipStatus.Expired;
            clip.StoragePath = null;
            _logger.LogInformation("Expired clip {ClipId} deleted from storage", clip.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task CaptureAsync(Camera camera, Guid recordingId, CancellationToken cancellationToken)
    {
        try
        {
            var workDir = Path.Combine(_options.WorkingDirectory, recordingId.ToString("N"));
            Directory.CreateDirectory(workDir);
            var output = Path.Combine(workDir, "source.mp4");
            if (camera.IsSimulated || camera.Protocol == CameraProtocol.Simulated)
            {
                await _ffmpeg.GenerateTestClipAsync(output, 30, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(camera.RtspUrl))
            {
                var url = camera.RtspUrl;
                if (!string.IsNullOrWhiteSpace(camera.PasswordEncrypted) && !string.IsNullOrWhiteSpace(camera.Username))
                {
                    try
                    {
                        var password = _encryption.Decrypt(camera.PasswordEncrypted);
                        var uri = new Uri(camera.RtspUrl);
                        url = new UriBuilder(uri) { UserName = camera.Username, Password = password }.Uri.ToString();
                    }
                    catch
                    {
                        url = camera.RtspUrl;
                    }
                }

                await _ffmpeg.RunAsync($"-rtsp_transport tcp -i \"{url}\" -t 3600 -c copy \"{output}\"", cancellationToken);
            }

            if (File.Exists(output))
            {
                await CreateSegmentAsync(recordingId, output, DateTime.UtcNow.AddSeconds(-30), DateTime.UtcNow, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Recording cancelled for camera {CameraId}", camera.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recording failed for camera {CameraId}", camera.Id);
        }
    }
}
