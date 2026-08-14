using SportReplay.Application.Contracts.Payments;
using SportReplay.Domain.Enums;

namespace SportReplay.Application.Abstractions;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IMercadoPagoService
{
    Task<PaymentDto> CreatePaymentAsync(Guid userId, Guid videoClipId, Guid? videoRequestId, ProductType productType, string? promotionCode, CancellationToken cancellationToken = default);
    Task<PaymentStatus> GetPaymentAsync(string externalPaymentId, CancellationToken cancellationToken = default);
    Task ProcessWebhookAsync(string payload, string? signature, string? requestId, CancellationToken cancellationToken = default);
}

public interface IWhatsAppService
{
    Task<Contracts.WhatsApp.WhatsAppMessageDto> SendVideoAsync(Guid videoRequestId, CancellationToken cancellationToken = default);
    Task ProcessWebhookAsync(string payload, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<Contracts.Dashboard.ClubDashboardDto> GetClubDashboardAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<Contracts.Dashboard.AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}

public interface IStorageService
{
    Task EnsureBucketAsync(CancellationToken cancellationToken = default);
    Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task UploadFileAsync(string objectKey, string filePath, string contentType, CancellationToken cancellationToken = default);
    Task<string> GetSignedUrlAsync(string objectKey, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);
}

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(Guid userId, string email, string role);
    string CreateRefreshToken();
}

public interface IFfmpegService
{
    Task<int> RunAsync(string arguments, CancellationToken cancellationToken = default);
    Task<bool> ProbeRtspAsync(string rtspUrl, CancellationToken cancellationToken = default);
    Task GenerateTestClipAsync(string outputPath, int durationSeconds, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task LogAsync(string action, string entity, Guid? entityId, object? payload = null, CancellationToken cancellationToken = default);
}
