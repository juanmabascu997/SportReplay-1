using SportReplay.Domain.Enums;

namespace SportReplay.Application.Contracts.WhatsApp;

public record SendVideoRequest(Guid VideoRequestId);
public record WhatsAppMessageDto(
    Guid Id,
    Guid VideoRequestId,
    string PhoneNumber,
    string? MessageId,
    string? MediaUrl,
    WhatsAppMessageStatus Status,
    DateTime? SentAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    string? ErrorMessage);

public record WhatsAppWebhookPayload(string? Object, IReadOnlyList<WhatsAppWebhookEntry>? Entry);
public record WhatsAppWebhookEntry(string? Id, IReadOnlyList<WhatsAppWebhookChange>? Changes);
public record WhatsAppWebhookChange(string? Field, WhatsAppWebhookValue? Value);
public record WhatsAppWebhookValue(IReadOnlyList<WhatsAppStatus>? Statuses);
public record WhatsAppStatus(string? Id, string? Status, string? Timestamp, IReadOnlyList<WhatsAppError>? Errors);
public record WhatsAppError(int? Code, string? Title);
