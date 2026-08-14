using System.Net.Sockets;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Cameras;
using SportReplay.Application.Exceptions;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Services;

public class CameraService : ICameraService
{
    private readonly SportReplayDbContext _db;
    private readonly IClubAuthorization _auth;
    private readonly IEncryptionService _encryption;
    private readonly IFfmpegService _ffmpeg;
    private readonly IVideoProcessingService _video;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CameraService> _logger;

    public CameraService(
        SportReplayDbContext db,
        IClubAuthorization auth,
        IEncryptionService encryption,
        IFfmpegService ffmpeg,
        IVideoProcessingService video,
        IHttpClientFactory httpClientFactory,
        ILogger<CameraService> logger)
    {
        _db = db;
        _auth = auth;
        _encryption = encryption;
        _ffmpeg = ffmpeg;
        _video = video;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CameraDto>> GetByCourtAsync(Guid courtId, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCourtAsync(courtId, cancellationToken);
        var cameras = await _db.Cameras
            .Include(x => x.Court)
            .Include(x => x.Configurations)
            .AsNoTracking()
            .Where(x => x.CourtId == courtId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return cameras.Select(Map).ToList();
    }

    public async Task<CameraDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCameraAsync(id, cancellationToken);
        var camera = await LoadAsync(id, cancellationToken);
        return Map(camera);
    }

    public async Task<CameraDto> CreateAsync(Guid courtId, CreateCameraRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCourtAsync(courtId, cancellationToken);
        var camera = new Camera
        {
            CourtId = courtId,
            Name = request.Name.Trim(),
            IpAddress = request.IpAddress,
            RtspUrl = request.RtspUrl,
            OnvifUrl = request.OnvifUrl,
            Username = request.Username,
            PasswordEncrypted = string.IsNullOrWhiteSpace(request.Password) ? null : _encryption.Encrypt(request.Password),
            Protocol = request.IsSimulated ? CameraProtocol.Simulated : request.Protocol,
            IsSimulated = request.IsSimulated,
            Status = request.IsSimulated ? CameraStatus.Online : CameraStatus.Offline,
            LastHeartbeat = request.IsSimulated ? DateTime.UtcNow : null
        };
        _db.Cameras.Add(camera);
        _db.CameraConfigurations.Add(new CameraConfiguration
        {
            CameraId = camera.Id,
            Resolution = string.IsNullOrWhiteSpace(request.Resolution) ? "1280x720" : request.Resolution,
            Fps = request.Fps == 0 ? 25 : request.Fps,
            Bitrate = request.Bitrate == 0 ? 2000 : request.Bitrate,
            SegmentDurationSeconds = request.SegmentDurationSeconds == 0 ? 6 : request.SegmentDurationSeconds
        });
        await AddEventAsync(camera.Id, CameraEventType.Connected, request.IsSimulated ? "Simulated camera registered" : "Camera registered", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(camera.Id, cancellationToken);
    }

    public async Task<CameraDto> UpdateAsync(Guid id, UpdateCameraRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCameraAsync(id, cancellationToken);
        var camera = await _db.Cameras.Include(x => x.Configurations).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Camera", id);
        camera.Name = request.Name.Trim();
        camera.IpAddress = request.IpAddress;
        camera.RtspUrl = request.RtspUrl;
        camera.OnvifUrl = request.OnvifUrl;
        camera.Username = request.Username ?? camera.Username;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            camera.PasswordEncrypted = _encryption.Encrypt(request.Password);
        }

        camera.Protocol = request.Protocol;
        camera.IsActive = request.IsActive;
        var config = camera.Configurations.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        if (config is not null)
        {
            config.Resolution = request.Resolution;
            config.Fps = request.Fps;
            config.Bitrate = request.Bitrate;
            config.RecordingEnabled = request.RecordingEnabled;
            config.SegmentDurationSeconds = request.SegmentDurationSeconds;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCameraAsync(id, cancellationToken);
        var camera = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Camera", id);
        camera.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CameraConnectionResult> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCameraAsync(id, cancellationToken);
        var camera = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Camera", id);

        if (camera.IsSimulated || camera.Protocol == CameraProtocol.Simulated)
        {
            camera.Status = CameraStatus.Online;
            camera.LastHeartbeat = DateTime.UtcNow;
            await AddEventAsync(camera.Id, CameraEventType.ConnectionTest, "Simulated camera connection test succeeded", cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return new CameraConnectionResult(true, CameraStatus.Online, "Simulated camera is online.", "SportReplay Simulated Camera");
        }

        var reachable = await IsReachableAsync(camera.IpAddress, cancellationToken);
        var rtspOk = false;
        if (!string.IsNullOrWhiteSpace(camera.RtspUrl))
        {
            rtspOk = await _ffmpeg.ProbeRtspAsync(BuildRtspUrl(camera), cancellationToken);
        }

        string? deviceInfo = null;
        if (!string.IsNullOrWhiteSpace(camera.OnvifUrl))
        {
            deviceInfo = await TryOnvifAsync(camera, cancellationToken);
            if (deviceInfo is not null)
            {
                await AddEventAsync(camera.Id, CameraEventType.OnvifDiscovered, "ONVIF device information retrieved", cancellationToken, new { deviceInfo });
            }
        }

        var success = reachable || rtspOk;
        camera.Status = success ? CameraStatus.Online : CameraStatus.Error;
        camera.LastHeartbeat = success ? DateTime.UtcNow : camera.LastHeartbeat;
        await AddEventAsync(
            camera.Id,
            success ? CameraEventType.ConnectionTest : CameraEventType.Error,
            success ? "Connection test succeeded" : "Connection test failed",
            cancellationToken,
            new { reachable, rtspOk });
        await _db.SaveChangesAsync(cancellationToken);
        return new CameraConnectionResult(success, camera.Status, success ? "Camera reachable." : "Unable to reach camera.", deviceInfo);
    }

    public async Task StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCameraAsync(id, cancellationToken);
        await _video.StartRecordingAsync(id, null, cancellationToken);
    }

    public async Task StopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCameraAsync(id, cancellationToken);
        await _video.StopRecordingAsync(id, cancellationToken);
    }

    public async Task<CameraStatusDto> GetStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCameraAsync(id, cancellationToken);
        var camera = await _db.Cameras.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Camera", id);
        return new CameraStatusDto(camera.Id, camera.Status, camera.LastHeartbeat, camera.IsActive);
    }

    public async Task HeartbeatAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var camera = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (camera is null)
        {
            return;
        }

        camera.LastHeartbeat = DateTime.UtcNow;
        camera.Status = CameraStatus.Online;
        await AddEventAsync(id, CameraEventType.Heartbeat, "Heartbeat", cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Camera> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Cameras.Include(x => x.Court).Include(x => x.Configurations)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Camera", id);
    }

    private static CameraDto Map(Camera camera)
    {
        var config = camera.Configurations.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        return new CameraDto(
            camera.Id,
            camera.CourtId,
            camera.Court?.Name ?? string.Empty,
            camera.Name,
            camera.IpAddress,
            camera.Protocol,
            camera.Status,
            camera.LastHeartbeat,
            camera.IsActive,
            camera.IsSimulated,
            config?.Resolution,
            config?.Fps,
            config?.Bitrate);
    }

    private string BuildRtspUrl(Camera camera)
    {
        if (string.IsNullOrWhiteSpace(camera.RtspUrl))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(camera.Username) || string.IsNullOrWhiteSpace(camera.PasswordEncrypted))
        {
            return camera.RtspUrl;
        }

        try
        {
            var password = _encryption.Decrypt(camera.PasswordEncrypted);
            var uri = new Uri(camera.RtspUrl);
            var builder = new UriBuilder(uri) { UserName = camera.Username, Password = password };
            return builder.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not build authenticated RTSP URL for camera {CameraId}", camera.Id);
            return camera.RtspUrl;
        }
    }

    private static async Task<bool> IsReachableAsync(string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(ipAddress, 554);
            var completed = await Task.WhenAny(connect, Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
            return completed == connect && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> TryOnvifAsync(Camera camera, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("onvif");
            var envelope = """
                <?xml version="1.0" encoding="UTF-8"?>
                <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope">
                  <s:Body>
                    <GetDeviceInformation xmlns="http://www.onvif.org/ver10/device/wsdl"/>
                  </s:Body>
                </s:Envelope>
                """;
            using var content = new StringContent(envelope, Encoding.UTF8, "application/soap+xml");
            using var response = await client.PostAsync(camera.OnvifUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return body.Length > 400 ? body[..400] : body;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "ONVIF probe failed for camera {CameraId}", camera.Id);
            return null;
        }
    }

    private Task AddEventAsync(Guid cameraId, CameraEventType type, string message, CancellationToken cancellationToken, object? payload = null)
    {
        _db.CameraEvents.Add(new CameraEvent
        {
            CameraId = cameraId,
            EventType = type,
            Message = message,
            Payload = payload is null ? null : System.Text.Json.JsonSerializer.Serialize(payload)
        });
        return Task.CompletedTask;
    }
}
