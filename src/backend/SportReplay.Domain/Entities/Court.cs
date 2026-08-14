using SportReplay.Domain.Common;
using SportReplay.Domain.Enums;

namespace SportReplay.Domain.Entities;

public class Court : EntityBase
{
    public Guid ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SportType SportType { get; set; } = SportType.Padel;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Club Club { get; set; } = null!;
    public ICollection<Camera> Cameras { get; set; } = new List<Camera>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
