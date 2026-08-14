using SportReplay.Application.Contracts.Videos;

namespace SportReplay.Application.Abstractions;

public interface IVideoClipService
{
    Task<IReadOnlyList<VideoClipDto>> GetByMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<VideoClipDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreateVideoClipResponse> CreateAsync(CreateVideoClipRequest request, CancellationToken cancellationToken = default);
    Task<VideoStreamDto> GetStreamAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VideoStreamDto> GetDownloadAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VideoStreamDto> GetRecordingStreamAsync(Guid recordingId, CancellationToken cancellationToken = default);
}

public interface IVideoProcessingService
{
    Task StartRecordingAsync(Guid cameraId, Guid? matchId = null, CancellationToken cancellationToken = default);
    Task StopRecordingAsync(Guid cameraId, CancellationToken cancellationToken = default);
    Task CreateSegmentAsync(Guid recordingId, string localPath, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task ProcessClipAsync(Guid clipId, CancellationToken cancellationToken = default);
    Task GenerateThumbnailAsync(Guid clipId, CancellationToken cancellationToken = default);
    Task GenerateHlsAsync(Guid recordingId, CancellationToken cancellationToken = default);
    Task UploadToStorageAsync(string localPath, string objectKey, string contentType, CancellationToken cancellationToken = default);
    Task DeleteExpiredVideosAsync(CancellationToken cancellationToken = default);
}

public interface IVideoRequestService
{
    Task<VideoRequestDto> CreateAsync(CreateVideoRequestRequest request, CancellationToken cancellationToken = default);
    Task<VideoRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
