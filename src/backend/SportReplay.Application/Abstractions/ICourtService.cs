using SportReplay.Application.Contracts.Courts;

namespace SportReplay.Application.Abstractions;

public interface ICourtService
{
    Task<IReadOnlyList<CourtDto>> GetByClubAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<CourtDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CourtDto> CreateAsync(Guid clubId, CreateCourtRequest request, CancellationToken cancellationToken = default);
    Task<CourtDto> UpdateAsync(Guid id, UpdateCourtRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
