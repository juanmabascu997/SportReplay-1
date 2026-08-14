using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Matches;

namespace SportReplay.Application.Abstractions;

public interface IMatchService
{
    Task<PagedResult<MatchDto>> SearchAsync(MatchSearchQuery query, CancellationToken cancellationToken = default);
    Task<MatchDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MatchDto> CreateAsync(CreateMatchRequest request, CancellationToken cancellationToken = default);
    Task<MatchDto> UpdateAsync(Guid id, UpdateMatchRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecordingDto>> GetRecordingsAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<RecordingDto> CreateRecordingAsync(CreateRecordingRequest request, CancellationToken cancellationToken = default);
}
