namespace SportReplay.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "SportReplay";
    public string Audience { get; set; } = "SportReplay";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}

public class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "Minio";
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Bucket { get; set; } = "sportreplay-videos";
    public bool UseSsl { get; set; }
    public string Region { get; set; } = "us-east-1";
    public int SignedUrlExpiryMinutes { get; set; } = 15;
}

public class VideoOptions
{
    public const string SectionName = "Video";
    public int RetentionDays { get; set; } = 7;
    public int MaxClipDurationSeconds { get; set; } = 60;
    public string FfmpegPath { get; set; } = "ffmpeg";
    public string FfprobePath { get; set; } = "ffprobe";
    public int DefaultSegmentDurationSeconds { get; set; } = 6;
    public string WorkingDirectory { get; set; } = "./video-work";
}

public class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string NotificationUrl { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = "http://localhost:5173/payments?status=success";
    public string FailureUrl { get; set; } = "http://localhost:5173/payments?status=failure";
    public string PendingUrl { get; set; } = "http://localhost:5173/payments?status=pending";
}

public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";
    public string AccessToken { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string BusinessAccountId { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v21.0";
    public string VerifyToken { get; set; } = "sportreplay-verify";
}

public class EncryptionOptions
{
    public const string SectionName = "Encryption";
    public string Key { get; set; } = string.Empty;
}

public class CorsAppOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
}
