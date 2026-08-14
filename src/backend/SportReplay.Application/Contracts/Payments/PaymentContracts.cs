using SportReplay.Domain.Enums;

namespace SportReplay.Application.Contracts.Payments;

public record CreatePaymentRequest(Guid VideoClipId, Guid? VideoRequestId, ProductType ProductType, string? PromotionCode);
public record PaymentDto(
    Guid Id,
    Guid UserId,
    Guid? VideoClipId,
    string? ExternalPaymentId,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string? PaymentMethod,
    string? InitPoint,
    DateTime CreatedAt);

public record MercadoPagoWebhookPayload(string? Action, string? Type, MercadoPagoWebhookData? Data);
public record MercadoPagoWebhookData(string? Id);
