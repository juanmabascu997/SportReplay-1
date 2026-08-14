using SportReplay.Domain.Enums;

namespace SportReplay.Application.Contracts.Cameras;

public record CameraDto(
    Guid Id,
    Guid CourtId,
    string CourtName,
    string Name,
    string? IpAddress,
    CameraProtocol Protocol,
    CameraStatus Status,
    DateTime? LastHeartbeat,
    bool IsActive,
    bool IsSimulated,
    string? Resolution,
    int? Fps,
    int? Bitrate);

public record CreateCameraRequest(
    string Name,
    string? IpAddress,
    string? RtspUrl,
    string? OnvifUrl,
    string? Username,
    string? Password,
    CameraProtocol Protocol,
    bool IsSimulated,
    string Resolution,
    int Fps,
    int Bitrate,
    int SegmentDurationSeconds);

public record UpdateCameraRequest(
    string Name,
    string? IpAddress,
    string? RtspUrl,
    string? OnvifUrl,
    string? Username,
    string? Password,
    CameraProtocol Protocol,
    bool IsActive,
    string Resolution,
    int Fps,
    int Bitrate,
    bool RecordingEnabled,
    int SegmentDurationSeconds);

public record CameraConnectionResult(
    bool Success,
    CameraStatus Status,
    string Message,
    string? DeviceInfo);

public record CameraStatusDto(
    Guid Id,
    CameraStatus Status,
    DateTime? LastHeartbeat,
    bool IsActive);
