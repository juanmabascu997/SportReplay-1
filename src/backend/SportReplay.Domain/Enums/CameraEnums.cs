namespace SportReplay.Domain.Enums;

public enum CameraStatus
{
    Offline = 0,
    Online = 1,
    Error = 2
}

public enum CameraProtocol
{
    Rtsp = 1,
    Onvif = 2,
    Simulated = 3
}

public enum CameraEventType
{
    Heartbeat = 1,
    Connected = 2,
    Disconnected = 3,
    ConnectionTest = 4,
    RecordingStarted = 5,
    RecordingStopped = 6,
    Error = 7,
    OnvifDiscovered = 8
}
