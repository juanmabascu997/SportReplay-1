using SportReplay.Domain.Enums;

namespace SportReplay.Application.Contracts.Auth;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string? Phone, string? Role);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);

public record AuthResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);

public record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Role,
    bool IsActive);
