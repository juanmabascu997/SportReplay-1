using SportReplay.Application.Contracts.Auth;

namespace SportReplay.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
