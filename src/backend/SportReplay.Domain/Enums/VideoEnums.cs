namespace SportReplay.Domain.Enums;

public enum MatchStatus
{
    Scheduled = 1,
    Recording = 2,
    Completed = 3,
    Cancelled = 4
}

public enum RecordingStatus
{
    Recording = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4,
    Expired = 5
}

public enum VideoClipStatus
{
    Processing = 1,
    Ready = 2,
    Failed = 3,
    Expired = 4
}

public enum VideoRequestStatus
{
    Pending = 1,
    AwaitingPayment = 2,
    Paid = 3,
    Sending = 4,
    Sent = 5,
    Failed = 6,
    Cancelled = 7
}

public enum SportType
{
    Padel = 1,
    Tennis = 2,
    Football = 3,
    Basketball = 4,
    Volleyball = 5,
    Other = 99
}
