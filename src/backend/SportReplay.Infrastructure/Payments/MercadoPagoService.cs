using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Payments;
using SportReplay.Application.Exceptions;
using SportReplay.Application.Options;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Payments;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly SportReplayDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(
        SportReplayDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentDto> CreatePaymentAsync(
        Guid userId,
        Guid? videoClipId,
        Guid? matchId,
        Guid? videoRequestId,
        ProductType productType,
        string? promotionCode,
        CancellationToken cancellationToken = default)
    {
        Domain.Entities.Match? match = null;
        VideoClip? clip = null;
        ClubSettings? settings = null;
        Guid clubId;

        if (videoClipId.HasValue)
        {
            clip = await _db.VideoClips.Include(x => x.Match).ThenInclude(x => x.Court).ThenInclude(x => x.Club).ThenInclude(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == videoClipId, cancellationToken)
                ?? throw new NotFoundException("VideoClip", videoClipId.Value);
            match = clip.Match;
            settings = clip.Match.Court.Club.Settings;
            clubId = clip.Match.Court.ClubId;
        }
        else if (matchId.HasValue)
        {
            match = await _db.Matches.Include(x => x.Court).ThenInclude(x => x.Club).ThenInclude(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == matchId, cancellationToken)
                ?? throw new NotFoundException("Match", matchId.Value);
            settings = match.Court.Club.Settings;
            clubId = match.Court.ClubId;
        }
        else
        {
            throw new AppException("A match or clip is required to create a payment.");
        }

        var amount = productType switch
        {
            ProductType.FullMatch => settings?.FullMatchPrice ?? 4500m,
            ProductType.Subscription => 29000m,
            _ => settings?.ClipPrice ?? 1500m
        };

        if (!string.IsNullOrWhiteSpace(promotionCode))
        {
            var promo = await _db.Promotions.FirstOrDefaultAsync(
                x => x.ClubId == clubId && x.Code == promotionCode && x.IsActive && x.StartsAt <= DateTime.UtcNow && x.EndsAt >= DateTime.UtcNow,
                cancellationToken);
            if (promo is not null)
            {
                amount -= amount * (promo.DiscountPercent / 100m);
            }
        }

        var product = await _db.Products.FirstOrDefaultAsync(x => x.ClubId == clubId && x.Type == productType && x.IsActive, cancellationToken);
        var payment = new Payment
        {
            UserId = userId,
            VideoClipId = productType == ProductType.FullMatch ? null : clip?.Id,
            VideoRequestId = videoRequestId,
            ProductId = product?.Id,
            Amount = decimal.Round(amount, 2),
            Currency = settings?.Currency ?? "ARS",
            Status = PaymentStatus.Pending
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            payment.InitPoint = $"/payments?status=pending&sandbox=true&paymentId={payment.Id}";
            payment.PreferenceId = $"sandbox-{payment.Id:N}";
            await _db.SaveChangesAsync(cancellationToken);
            return Map(payment);
        }

        var client = CreateClient();
        var body = new
        {
            items = new[]
            {
                new
                {
                    title = productType == ProductType.Clip
                        ? "SportReplay clip"
                        : match?.Title ?? "SportReplay partido",
                    quantity = 1,
                    currency_id = payment.Currency,
                    unit_price = payment.Amount
                }
            },
            external_reference = payment.Id.ToString(),
            notification_url = string.IsNullOrWhiteSpace(_options.NotificationUrl) ? null : _options.NotificationUrl,
            back_urls = new
            {
                success = _options.SuccessUrl,
                failure = _options.FailureUrl,
                pending = _options.PendingUrl
            },
            auto_return = "approved"
        };

        using var response = await client.PostAsync("https://api.mercadopago.com/checkout/preferences",
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"), cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mercado Pago preference failed: {Body}", json);
            throw new AppException("Unable to create Mercado Pago preference.", 502);
        }

        using var doc = JsonDocument.Parse(json);
        payment.PreferenceId = doc.RootElement.GetProperty("id").GetString();
        payment.InitPoint = doc.RootElement.TryGetProperty("init_point", out var init)
            ? init.GetString()
            : doc.RootElement.GetProperty("sandbox_init_point").GetString();
        await _db.SaveChangesAsync(cancellationToken);
        return Map(payment);
    }

    public async Task<PaymentStatus> GetPaymentAsync(string externalPaymentId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        using var response = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{externalPaymentId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return MapStatus(doc.RootElement.GetProperty("status").GetString());
    }

    public async Task ProcessWebhookAsync(string payload, string? signature, string? requestId, CancellationToken cancellationToken = default)
    {
        ValidateSignature(payload, signature, requestId);

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : root.TryGetProperty("action", out var actionEl) ? actionEl.GetString() : "unknown";
        var externalId = root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idEl)
            ? idEl.ToString()
            : requestId ?? Guid.NewGuid().ToString("N");

        var existing = await _db.PaymentWebhooks.FirstOrDefaultAsync(
            x => x.Provider == "mercadopago" && x.EventType == type && x.ExternalId == externalId, cancellationToken);
        if (existing?.Processed == true)
        {
            return;
        }

        if (existing is null)
        {
            existing = new PaymentWebhook
            {
                Provider = "mercadopago",
                EventType = type ?? "unknown",
                ExternalId = externalId,
                Payload = payload,
                Signature = signature
            };
            _db.PaymentWebhooks.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            await ApplySandboxAsync(root, cancellationToken);
        }
        else
        {
            await ApplyLiveAsync(externalId, cancellationToken);
        }

        existing.Processed = true;
        existing.ProcessedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyLiveAsync(string externalId, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        using var response = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{externalId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Mercado Pago payment lookup failed for {Id}", externalId);
            return;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var status = MapStatus(doc.RootElement.GetProperty("status").GetString());
        var method = doc.RootElement.TryGetProperty("payment_method_id", out var methodEl) ? methodEl.GetString() : null;
        var externalReference = doc.RootElement.TryGetProperty("external_reference", out var ext) ? ext.GetString() : null;
        await ApplyStatusAsync(externalId, externalReference, status, method, cancellationToken);
    }

    private async Task ApplySandboxAsync(JsonElement root, CancellationToken cancellationToken)
    {
        var paymentId = root.TryGetProperty("paymentId", out var pid) ? pid.GetGuid() : Guid.Empty;
        var statusName = root.TryGetProperty("status", out var st) ? st.GetString() : "approved";
        if (paymentId == Guid.Empty)
        {
            return;
        }

        await ApplyStatusAsync($"sandbox-{paymentId:N}", paymentId.ToString(), MapStatus(statusName), "sandbox", cancellationToken);
    }

    private async Task ApplyStatusAsync(string externalPaymentId, string? paymentId, PaymentStatus status, string? method, CancellationToken cancellationToken)
    {
        Payment? payment = null;
        if (Guid.TryParse(paymentId, out var id))
        {
            payment = await _db.Payments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        payment ??= await _db.Payments.FirstOrDefaultAsync(x => x.ExternalPaymentId == externalPaymentId, cancellationToken);
        if (payment is null)
        {
            return;
        }

        payment.ExternalPaymentId = externalPaymentId;
        payment.Status = status;
        payment.PaymentMethod = method;

        if (status == PaymentStatus.Approved && payment.VideoRequestId.HasValue)
        {
            var request = await _db.VideoRequests.FirstOrDefaultAsync(x => x.Id == payment.VideoRequestId, cancellationToken);
            if (request is not null && request.Status is VideoRequestStatus.Pending or VideoRequestStatus.AwaitingPayment)
            {
                request.Status = VideoRequestStatus.Paid;
                _db.ProcessingJobs.Add(new ProcessingJob
                {
                    JobType = "SendWhatsApp",
                    Payload = request.Id.ToString(),
                    Status = "pending"
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private void ValidateSignature(string payload, string? signature, string? requestId)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            throw new AppException("Missing Mercado Pago signature.", 401);
        }

        var parts = signature.Split(',');
        string? ts = null;
        string? v1 = null;
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            if (kv[0].Trim() == "ts") ts = kv[1].Trim();
            if (kv[0].Trim() == "v1") v1 = kv[1].Trim();
        }

        if (ts is null || v1 is null)
        {
            throw new AppException("Invalid Mercado Pago signature.", 401);
        }

        var manifest = $"id:{requestId};request-id:{requestId};ts:{ts};";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest))).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(v1.ToLowerInvariant())))
        {
            // Some local/sandbox payloads use payload HMAC instead of the official manifest.
            var payloadHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(payloadHash), Encoding.UTF8.GetBytes(v1.ToLowerInvariant())))
            {
                throw new AppException("Invalid Mercado Pago signature.", 401);
            }
        }
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient("mercadopago");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        return client;
    }

    private static PaymentStatus MapStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "approved" => PaymentStatus.Approved,
        "rejected" => PaymentStatus.Rejected,
        "cancelled" or "canceled" => PaymentStatus.Cancelled,
        "refunded" => PaymentStatus.Refunded,
        _ => PaymentStatus.Pending
    };

    private static PaymentDto Map(Payment payment) => new(
        payment.Id, payment.UserId, payment.VideoClipId, payment.ExternalPaymentId, payment.Amount, payment.Currency,
        payment.Status, payment.PaymentMethod, payment.InitPoint, payment.CreatedAt);
}

public class PaymentService : IPaymentService
{
    private readonly SportReplayDbContext _db;
    private readonly IMercadoPagoService _mercadoPago;
    private readonly ICurrentUser _currentUser;

    public PaymentService(SportReplayDbContext db, IMercadoPagoService mercadoPago, ICurrentUser currentUser)
    {
        _db = db;
        _mercadoPago = mercadoPago;
        _currentUser = currentUser;
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAppException();
        }

        return await _mercadoPago.CreatePaymentAsync(
            _currentUser.UserId.Value,
            request.VideoClipId,
            request.MatchId,
            request.VideoRequestId,
            request.ProductType,
            request.PromotionCode,
            cancellationToken);
    }

    public async Task<PaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Payment", id);
        if (!_currentUser.IsAdmin && payment.UserId != _currentUser.UserId)
        {
            throw new ForbiddenException();
        }

        return new PaymentDto(payment.Id, payment.UserId, payment.VideoClipId, payment.ExternalPaymentId, payment.Amount, payment.Currency, payment.Status, payment.PaymentMethod, payment.InitPoint, payment.CreatedAt);
    }
}
