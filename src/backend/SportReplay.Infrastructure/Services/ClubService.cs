using Microsoft.EntityFrameworkCore;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Clubs;
using SportReplay.Application.Exceptions;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;

namespace SportReplay.Infrastructure.Services;

public class ClubService : IClubService
{
    private readonly SportReplayDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClubAuthorization _auth;
    private readonly IAuditService _audit;

    public ClubService(SportReplayDbContext db, ICurrentUser currentUser, IClubAuthorization auth, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _auth = auth;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ClubDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = _db.Clubs.AsNoTracking().Where(x => x.IsActive);
        if (_currentUser.IsClubOwner && _currentUser.UserId is not null)
        {
            query = query.Where(x => x.OwnerUserId == _currentUser.UserId);
        }

        var clubs = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return clubs.Select(Map).ToList();
    }

    public async Task<ClubDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(id, cancellationToken);
        var club = await _db.Clubs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Club", id);
        return Map(club);
    }

    public async Task<ClubDto> CreateAsync(CreateClubRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedAppException();
        }

        var club = new Club
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            Country = request.Country ?? "Argentina",
            Phone = request.Phone,
            Email = request.Email,
            OwnerUserId = _currentUser.UserId.Value
        };
        _db.Clubs.Add(club);
        _db.ClubSettings.Add(new ClubSettings { ClubId = club.Id });
        _db.Products.AddRange(
            new Product { ClubId = club.Id, Type = ProductType.Clip, Name = "Clip", Price = 1500 },
            new Product { ClubId = club.Id, Type = ProductType.FullMatch, Name = "Partido completo", Price = 4500 },
            new Product { ClubId = club.Id, Type = ProductType.Subscription, Name = "Suscripcion", Price = 29000 });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("create", "Club", club.Id, new { club.Name }, cancellationToken);
        return Map(club);
    }

    public async Task<ClubDto> UpdateAsync(Guid id, UpdateClubRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(id, cancellationToken);
        var club = await _db.Clubs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Club", id);
        club.Name = request.Name.Trim();
        club.Description = request.Description;
        club.Address = request.Address;
        club.City = request.City;
        club.Province = request.Province;
        club.Country = request.Country ?? club.Country;
        club.Phone = request.Phone;
        club.Email = request.Email;
        club.IsActive = request.IsActive;
        club.AllowFullMatchDownload = request.AllowFullMatchDownload;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(club);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(id, cancellationToken);
        var club = await _db.Clubs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Club", id);
        club.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ClubPriceDto> GetPricesAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        var club = await _db.Clubs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == clubId && x.IsActive, cancellationToken)
            ?? throw new NotFoundException("Club", clubId);
        var settings = await _db.ClubSettings.AsNoTracking().FirstOrDefaultAsync(x => x.ClubId == clubId, cancellationToken);
        return new ClubPriceDto(
            club.Id,
            settings?.ClipPrice ?? 1500m,
            settings?.FullMatchPrice ?? 4500m,
            settings?.Currency ?? "ARS",
            settings?.AllowFullMatchDownload ?? club.AllowFullMatchDownload);
    }

    public async Task<ClubSettingsDto> GetSettingsAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(clubId, cancellationToken);
        var settings = await _db.ClubSettings.AsNoTracking().FirstOrDefaultAsync(x => x.ClubId == clubId, cancellationToken)
            ?? throw new NotFoundException("ClubSettings", clubId);
        return MapSettings(settings);
    }

    public async Task<ClubSettingsDto> UpdateSettingsAsync(Guid clubId, UpdateClubSettingsRequest request, CancellationToken cancellationToken = default)
    {
        await _auth.EnsureClubAccessAsync(clubId, cancellationToken);
        var settings = await _db.ClubSettings.FirstOrDefaultAsync(x => x.ClubId == clubId, cancellationToken)
            ?? throw new NotFoundException("ClubSettings", clubId);
        settings.ClipPrice = request.ClipPrice;
        settings.FullMatchPrice = request.FullMatchPrice;
        settings.PricePerMinute = request.PricePerMinute;
        settings.CommissionPercent = request.CommissionPercent;
        settings.VideoRetentionDays = request.VideoRetentionDays;
        settings.MaxClipDurationSeconds = request.MaxClipDurationSeconds;
        settings.AllowFullMatchDownload = request.AllowFullMatchDownload;
        settings.Currency = request.Currency;
        await _db.SaveChangesAsync(cancellationToken);
        return MapSettings(settings);
    }

    private static ClubDto Map(Club club) => new(
        club.Id, club.Name, club.Description, club.Address, club.City, club.Province, club.Country,
        club.Phone, club.Email, club.OwnerUserId, club.IsActive, club.AllowFullMatchDownload, club.CreatedAt);

    private static ClubSettingsDto MapSettings(ClubSettings s) => new(
        s.ClubId, s.ClipPrice, s.FullMatchPrice, s.PricePerMinute, s.CommissionPercent,
        s.VideoRetentionDays, s.MaxClipDurationSeconds, s.AllowFullMatchDownload, s.Currency);
}
