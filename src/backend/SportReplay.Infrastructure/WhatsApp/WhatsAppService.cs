using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Contracts.WhatsApp;
using SportReplay.Application.Exceptions;
using SportReplay.Application.Options;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly SportReplayDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IStorageService _storage;
    private readonly WhatsAppOptions _options;
    private readonly StorageOptions _storageOptions;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        SportReplayDbContext db,
        IHttpClientFactory httpClientFactory,
        IStorageService storage,
        IOptions<WhatsAppOptions> options,
        IOptions<StorageOptions> storageOptions,
        ILogger<WhatsAppService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _storage = storage;
        _options = options.Value;
        _storageOptions = storageOptions.Value;
        _logger = logger;
    }

    public async Task<WhatsAppMessageDto> SendVideoAsync(Guid videoRequestId, CancellationToken cancellationToken = default)
    {
        var request = await _db.VideoRequests.Include(x => x.VideoClip)
            .FirstOrDefaultAsync(x => x.Id == videoRequestId, cancellationToken)
            ?? throw new NotFoundException("VideoRequest", videoRequestId);

        if (request.Status is not VideoRequestStatus.Paid and not VideoRequestStatus.Sending and not VideoRequestStatus.Failed)
        {
            throw new AppException("Video request is not paid yet.", 409);
        }

        if (request.VideoClip is null || string.IsNullOrWhiteSpace(request.VideoClip.StoragePath))
        {
            throw new AppException("Clip is not ready.", 409);
        }

        request.Status = VideoRequestStatus.Sending;
        var mediaUrl = await _storage.GetSignedUrlAsync(request.VideoClip.StoragePath, TimeSpan.FromMinutes(_storageOptions.SignedUrlExpiryMinutes), cancellationToken);
        var message = new WhatsAppMessage
        {
            VideoRequestId = request.Id,
            PhoneNumber = request.PhoneNumber,
            MediaUrl = mediaUrl,
            Status = WhatsAppMessageStatus.Queued
        };
        _db.WhatsAppMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(_options.AccessToken) || string.IsNullOrWhiteSpace(_options.PhoneNumberId))
        {
            message.Status = WhatsAppMessageStatus.Sent;
            message.MessageId = $"wamid.sandbox.{message.Id:N}";
            message.SentAt = DateTime.UtcNow;
            request.Status = VideoRequestStatus.Sent;
            request.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("WhatsApp sandbox send recorded for request {RequestId}", request.Id);
            return Map(message);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("whatsapp");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            var payload = new
            {
                messaging_product = "whatsapp",
                to = request.PhoneNumber.TrimStart('+'),
                type = "video",
                video = new { link = mediaUrl }
            };
            using var response = await client.PostAsync(
                $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                message.Status = WhatsAppMessageStatus.Failed;
                message.ErrorMessage = "WhatsApp API rejected the message";
                request.Status = VideoRequestStatus.Failed;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("WhatsApp send failed: {Body}", json);
                throw new AppException("WhatsApp send failed.", 502);
            }

            using var doc = JsonDocument.Parse(json);
            var wamid = doc.RootElement.GetProperty("messages")[0].GetProperty("id").GetString();
            message.MessageId = wamid;
            message.Status = WhatsAppMessageStatus.Sent;
            message.SentAt = DateTime.UtcNow;
            request.Status = VideoRequestStatus.Sent;
            request.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return Map(message);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp send failed");
            message.Status = WhatsAppMessageStatus.Failed;
            message.ErrorMessage = "Unexpected WhatsApp error";
            request.Status = VideoRequestStatus.Failed;
            await _db.SaveChangesAsync(cancellationToken);
            throw new AppException("WhatsApp send failed.", 502);
        }
    }

    public async Task ProcessWebhookAsync(string payload, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("entry", out var entry))
        {
            return;
        }

        foreach (var item in entry.EnumerateArray())
        {
            if (!item.TryGetProperty("changes", out var changes))
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value) || !value.TryGetProperty("statuses", out var statuses))
                {
                    continue;
                }

                foreach (var status in statuses.EnumerateArray())
                {
                    var id = status.GetProperty("id").GetString();
                    var name = status.GetProperty("status").GetString();
                    var message = await _db.WhatsAppMessages.FirstOrDefaultAsync(x => x.MessageId == id, cancellationToken);
                    if (message is null)
                    {
                        continue;
                    }

                    message.Status = name switch
                    {
                        "delivered" => WhatsAppMessageStatus.Delivered,
                        "read" => WhatsAppMessageStatus.Read,
                        "failed" => WhatsAppMessageStatus.Failed,
                        _ => WhatsAppMessageStatus.Sent
                    };
                    if (message.Status == WhatsAppMessageStatus.Delivered)
                    {
                        message.DeliveredAt = DateTime.UtcNow;
                    }

                    if (message.Status == WhatsAppMessageStatus.Read)
                    {
                        message.ReadAt = DateTime.UtcNow;
                    }
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static WhatsAppMessageDto Map(WhatsAppMessage message) => new(
        message.Id, message.VideoRequestId, message.PhoneNumber, message.MessageId, message.MediaUrl,
        message.Status, message.SentAt, message.DeliveredAt, message.ReadAt, message.ErrorMessage);
}
