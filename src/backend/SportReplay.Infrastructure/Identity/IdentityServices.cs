using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Exceptions;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Identity;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : Guid.TryParse(Principal?.FindFirstValue("sub"), out var sub) ? sub : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");
    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsAdmin => Role == UserRoles.Admin;
    public bool IsClubOwner => Role == UserRoles.ClubOwner;
}

public class ClubAuthorization : IClubAuthorization
{
    private readonly SportReplayDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ClubAuthorization(SportReplayDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task EnsureClubAccessAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.IsAdmin)
        {
            return;
        }

        var club = await _db.Clubs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == clubId, cancellationToken)
            ?? throw new NotFoundException("Club", clubId);

        if (_currentUser.UserId is null || club.OwnerUserId != _currentUser.UserId)
        {
            if (_currentUser.Role is UserRoles.Player)
            {
                return;
            }

            throw new ForbiddenException("You cannot access another club.");
        }
    }

    public async Task<Guid> GetClubIdForCourtAsync(Guid courtId, CancellationToken cancellationToken = default)
    {
        var court = await _db.Courts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == courtId, cancellationToken)
            ?? throw new NotFoundException("Court", courtId);
        await EnsureClubAccessAsync(court.ClubId, cancellationToken);
        return court.ClubId;
    }

    public async Task<Guid> GetClubIdForCameraAsync(Guid cameraId, CancellationToken cancellationToken = default)
    {
        var camera = await _db.Cameras.Include(x => x.Court).AsNoTracking().FirstOrDefaultAsync(x => x.Id == cameraId, cancellationToken)
            ?? throw new NotFoundException("Camera", cameraId);
        await EnsureClubAccessAsync(camera.Court.ClubId, cancellationToken);
        return camera.Court.ClubId;
    }

    public async Task<Guid> GetClubIdForMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var match = await _db.Matches.Include(x => x.Court).AsNoTracking().FirstOrDefaultAsync(x => x.Id == matchId, cancellationToken)
            ?? throw new NotFoundException("Match", matchId);
        await EnsureClubAccessAsync(match.Court.ClubId, cancellationToken);
        return match.Court.ClubId;
    }
}

public class AuditService : IAuditService
{
    private readonly SportReplayDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditService(SportReplayDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(string action, string entity, Guid? entityId, object? payload = null, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new Domain.Entities.AuditLog
        {
            UserId = _currentUser.UserId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            IpAddress = _currentUser.IpAddress,
            Payload = payload is null ? null : JsonSerializer.Serialize(payload)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
