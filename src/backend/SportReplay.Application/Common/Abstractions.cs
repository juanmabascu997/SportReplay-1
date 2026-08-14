namespace SportReplay.Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsClubOwner { get; }
}

public interface IClubAuthorization
{
    Task EnsureClubAccessAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<Guid> GetClubIdForCourtAsync(Guid courtId, CancellationToken cancellationToken = default);
    Task<Guid> GetClubIdForCameraAsync(Guid cameraId, CancellationToken cancellationToken = default);
    Task<Guid> GetClubIdForMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
}
