using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Contracts.Auth;

namespace SportReplay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
        => Ok(await _auth.RegisterAsync(request, cancellationToken));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _auth.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken)
        => Ok(await _auth.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _auth.LogoutAsync(request, cancellationToken);
        return NoContent();
    }
}
