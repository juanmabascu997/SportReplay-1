using SportReplay.Domain.Enums;

namespace SportReplay.Application.Contracts.Courts;

public record CourtDto(
    Guid Id,
    Guid ClubId,
    string Name,
    SportType SportType,
    string? Description,
    bool IsActive);

public record CreateCourtRequest(string Name, SportType SportType, string? Description);
public record UpdateCourtRequest(string Name, SportType SportType, string? Description, bool IsActive);
