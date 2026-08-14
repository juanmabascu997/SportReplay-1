using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SportReplay.Application.Abstractions;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Workers;

public class VideoRecordingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoRecordingWorker> _logger;

    public VideoRecordingWorker(IServiceScopeFactory scopeFactory, ILogger<VideoRecordingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SportReplayDbContext>();
                var video = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();
                var now = DateTime.UtcNow;
                var due = await db.Matches
                    .Include(x => x.Court).ThenInclude(x => x.Cameras).ThenInclude(c => c.Configurations)
                    .Where(x => x.Status == MatchStatus.Scheduled && x.StartTime <= now && x.EndTime > now)
                    .ToListAsync(stoppingToken);

                foreach (var match in due)
                {
                    foreach (var camera in match.Court.Cameras.Where(c => c.IsActive))
                    {
                        var config = camera.Configurations.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                        if (config?.RecordingEnabled == false)
                        {
                            continue;
                        }

                        var already = await db.Recordings.AnyAsync(x => x.MatchId == match.Id && x.CameraId == camera.Id && x.Status == RecordingStatus.Recording, stoppingToken);
                        if (already)
                        {
                            continue;
                        }

                        await video.StartRecordingAsync(camera.Id, match.Id, stoppingToken);
                    }
                }

                var ended = await db.Recordings.Include(x => x.Match)
                    .Where(x => x.Status == RecordingStatus.Recording && x.Match.EndTime <= now)
                    .ToListAsync(stoppingToken);
                foreach (var recording in ended)
                {
                    await video.StopRecordingAsync(recording.CameraId, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VideoRecordingWorker loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}

public class VideoProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoProcessingWorker> _logger;

    public VideoProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<VideoProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SportReplayDbContext>();
                var video = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();
                var jobs = await db.ProcessingJobs.Where(x => x.Status == "pending" && x.JobType == "ProcessClip").OrderBy(x => x.CreatedAt).Take(5).ToListAsync(stoppingToken);
                foreach (var job in jobs)
                {
                    job.Status = "processing";
                    job.Attempts++;
                    await db.SaveChangesAsync(stoppingToken);
                    try
                    {
                        if (Guid.TryParse(job.Payload, out var clipId))
                        {
                            await video.ProcessClipAsync(clipId, stoppingToken);
                        }

                        job.Status = "completed";
                        job.ProcessedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        job.Status = job.Attempts >= 3 ? "failed" : "pending";
                        job.LastError = "processing failed";
                        _logger.LogError(ex, "Clip job {JobId} failed", job.Id);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VideoProcessingWorker loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

public class VideoCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VideoCleanupWorker> _logger;

    public VideoCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<VideoCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var video = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();
                await video.DeleteExpiredVideosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VideoCleanupWorker loop failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

public class WhatsAppDispatchWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WhatsAppDispatchWorker> _logger;

    public WhatsAppDispatchWorker(IServiceScopeFactory scopeFactory, ILogger<WhatsAppDispatchWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SportReplayDbContext>();
                var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var jobs = await db.ProcessingJobs.Where(x => x.Status == "pending" && x.JobType == "SendWhatsApp").Take(5).ToListAsync(stoppingToken);
                foreach (var job in jobs)
                {
                    job.Status = "processing";
                    job.Attempts++;
                    await db.SaveChangesAsync(stoppingToken);
                    try
                    {
                        if (Guid.TryParse(job.Payload, out var requestId))
                        {
                            await whatsApp.SendVideoAsync(requestId, stoppingToken);
                        }

                        job.Status = "completed";
                        job.ProcessedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        job.Status = job.Attempts >= 3 ? "failed" : "pending";
                        job.LastError = "whatsapp failed";
                        _logger.LogError(ex, "WhatsApp job {JobId} failed", job.Id);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WhatsAppDispatchWorker loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        }
    }
}
