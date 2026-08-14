using Microsoft.EntityFrameworkCore;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Courts;
using SportReplay.Application.Exceptions;
using SportReplay.Domain.Entities;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Services;

public class CourtService : ICourtService
{
    private readonly SportReplayDbContext _db;
    private readonly IClubAuthorization _auth;

    public CourtService(SportReplayDbContext db, IClubAuthorization auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<IReadOnlyList<CourtDto>> GetByClubAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(clubId, cancellationToken);
        var courts = await _db.Courts.AsNoTracking().Where(x => x.ClubId == clubId && x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return courts.Select(Map).ToList();
    }

    public async Task<CourtDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCourtAsync(id, cancellationToken);
        var court = await _db.Courts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Court", id);
        return Map(court);
    }

    public async Task<CourtDto> CreateAsync(Guid clubId, CreateCourtRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(clubId, cancellationToken);
        var court = new Court
        {
            ClubId = clubId,
            Name = request.Name.Trim(),
            SportType = request.SportType,
            Description = request.Description
        };
        _db.Courts.Add(court);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(court);
    }

    public async Task<CourtDto> UpdateAsync(Guid id, UpdateCourtRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCourtAsync(id, cancellationToken);
        var court = await _db.Courts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Court", id);
        court.Name = request.Name.Trim();
        court.SportType = request.SportType;
        court.Description = request.Description;
        court.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(court);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.GetClubIdForCourtAsync(id, cancellationToken);
        var court = await _db.Courts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Court", id);
        court.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static CourtDto Map(Court court) => new(court.Id, court.ClubId, court.Name, court.SportType, court.Description, court.IsActive);
}
