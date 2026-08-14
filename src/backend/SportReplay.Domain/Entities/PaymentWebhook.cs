namespace SportReplay.Domain.Entities;

public class PaymentWebhook
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = "mercadopago";
    public string EventType { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
