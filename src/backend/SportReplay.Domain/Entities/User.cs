using SportReplay.Domain.Common;

namespace SportReplay.Domain.Entities;

public class User : EntityBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Enums.UserRoles.Player;
    public Guid? RoleId { get; set; }
    public bool IsActive { get; set; } = true;

    public Role? RoleEntity { get; set; }
    public ICollection<Club> OwnedClubs { get; set; } = new List<Club>();
    public ICollection<VideoRequest> VideoRequests { get; set; } = new List<VideoRequest>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
