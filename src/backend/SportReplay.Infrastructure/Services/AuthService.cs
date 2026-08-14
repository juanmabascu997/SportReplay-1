using Microsoft.EntityFrameworkCore;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Contracts.Auth;
using SportReplay.Application.Exceptions;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly SportReplayDbContext _db;
    private readonly ITokenService _tokens;

    public AuthService(SportReplayDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new ConflictException("A user with that email already exists.");
        }

        var roleName = string.IsNullOrWhiteSpace(request.Role) ? UserRoles.Player : request.Role;
        if (roleName == UserRoles.Admin)
        {
            roleName = UserRoles.Player;
        }

        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = roleName,
            RoleId = role?.Id
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return await IssueTokensAsync(user, null, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
            ?? throw new UnauthorizedAppException();

        if (!user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException();
        }

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var stored = await _db.RefreshTokens.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedAppException("Invalid refresh token.");

        if (!stored.IsActive)
        {
            throw new UnauthorizedAppException("Refresh token is no longer valid.");
        }

        stored.RevokedAt = DateTime.UtcNow;
        var response = await IssueTokensAsync(stored.User, ipAddress, cancellationToken);
        stored.ReplacedByToken = response.RefreshToken;
        await _db.SaveChangesAsync(cancellationToken);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);
        if (stored is null)
        {
            return;
        }

        stored.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);
        return new UserProfileDto(user.Id, user.FirstName, user.LastName, user.Email, user.Phone, user.Role, user.IsActive);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var (access, expires) = _tokens.CreateAccessToken(user.Id, user.Email, user.Role);
        var refresh = _tokens.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh,
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedByIp = ipAddress
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, user.Role, access, refresh, expires);
    }
}
