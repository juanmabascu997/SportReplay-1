namespace SportReplay.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum WhatsAppMessageStatus
{
    Queued = 1,
    Sent = 2,
    Delivered = 3,
    Read = 4,
    Failed = 5
}

public enum SubscriptionStatus
{
    Active = 1,
    Cancelled = 2,
    Expired = 3,
    PastDue = 4
}

public enum ProductType
{
    Clip = 1,
    FullMatch = 2,
    Subscription = 3
}
