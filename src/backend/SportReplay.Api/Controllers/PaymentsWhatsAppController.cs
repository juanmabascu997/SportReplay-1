using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Payments;
using SportReplay.Domain.Enums;

namespace SportReplay.Api.Controllers;

[ApiController]
public class PaymentsWhatsAppController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IMercadoPagoService _mercadoPago;
    private readonly IWhatsAppService _whatsApp;
    private readonly IDashboardService _dashboard;
    private readonly IAuthService _auth;
    private readonly ICurrentUser _currentUser;

    public PaymentsWhatsAppController(
        IPaymentService payments,
        IMercadoPagoService mercadoPago,
        IWhatsAppService whatsApp,
        IDashboardService dashboard,
        IAuthService auth,
        ICurrentUser currentUser)
    {
        _payments = payments;
        _mercadoPago = mercadoPago;
        _whatsApp = whatsApp;
        _dashboard = dashboard;
        _auth = auth;
        _currentUser = currentUser;
    }

    [Authorize]
    [HttpPost("/api/payments/create")]
    public async Task<IActionResult> Create(CreatePaymentRequest request, CancellationToken cancellationToken)
        => Ok(await _payments.CreateAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("/api/payments/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _payments.GetByIdAsync(id, cancellationToken));

    [AllowAnonymous]
    [HttpPost("/api/payments/webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["x-signature"].ToString();
        var requestId = Request.Headers["x-request-id"].ToString();
        if (string.IsNullOrWhiteSpace(payload))
        {
            payload = "{}";
        }

        await _mercadoPago.ProcessWebhookAsync(payload, signature, requestId, cancellationToken);
        return Ok();
    }

    [Authorize]
    [HttpPost("/api/whatsapp/send-video")]
    public async Task<IActionResult> SendVideo([FromBody] Guid videoRequestId, CancellationToken cancellationToken)
        => Ok(await _whatsApp.SendVideoAsync(videoRequestId, cancellationToken));

    [AllowAnonymous]
    [HttpGet("/api/whatsapp/webhook")]
    public IActionResult VerifyWhatsApp([FromQuery(Name = "hub.mode")] string? mode, [FromQuery(Name = "hub.verify_token")] string? token, [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expected = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["WHATSAPP_VERIFY_TOKEN"] ?? "sportreplay-verify";
        if (mode == "subscribe" && token == expected)
        {
            return Content(challenge ?? string.Empty);
        }

        return Unauthorized();
    }

    [AllowAnonymous]
    [HttpPost("/api/whatsapp/webhook")]
    public async Task<IActionResult> WhatsAppWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        await _whatsApp.ProcessWebhookAsync(payload, cancellationToken);
        return Ok();
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("/api/admin/dashboard")]
    public async Task<IActionResult> AdminDashboard(CancellationToken cancellationToken)
        => Ok(await _dashboard.GetAdminDashboardAsync(cancellationToken));

    [Authorize]
    [HttpGet("/api/profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
        => Ok(await _auth.GetProfileAsync(_currentUser.UserId ?? Guid.Empty, cancellationToken));
}
