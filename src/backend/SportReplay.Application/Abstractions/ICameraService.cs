using SportReplay.Application.Contracts.Cameras;

namespace SportReplay.Application.Abstractions;

public interface ICameraService
{
    Task<IReadOnlyList<CameraDto>> GetByCourtAsync(Guid courtId, CancellationToken cancellationToken = default);
    Task<CameraDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CameraDto> CreateAsync(Guid courtId, CreateCameraRequest request, CancellationToken cancellationToken = default);
    Task<CameraDto> UpdateAsync(Guid id, UpdateCameraRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CameraConnectionResult> TestConnectionAsync(Guid id, CancellationToken cancellationToken = default);
    Task StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task StopAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CameraStatusDto> GetStatusAsync(Guid id, CancellationToken cancellationToken = default);
    Task HeartbeatAsync(Guid id, CancellationToken cancellationToken = default);
}
