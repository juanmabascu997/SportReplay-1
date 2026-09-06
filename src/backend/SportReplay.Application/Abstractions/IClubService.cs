using SportReplay.Application.Contracts.Clubs;

namespace SportReplay.Application.Abstractions;

public interface IClubService
{
    Task<IReadOnlyList<ClubDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ClubDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClubDto> CreateAsync(CreateClubRequest request, CancellationToken cancellationToken = default);
    Task<ClubDto> UpdateAsync(Guid id, UpdateClubRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClubPriceDto> GetPricesAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<ClubSettingsDto> GetSettingsAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<ClubSettingsDto> UpdateSettingsAsync(Guid clubId, UpdateClubSettingsRequest request, CancellationToken cancellationToken = default);
}
