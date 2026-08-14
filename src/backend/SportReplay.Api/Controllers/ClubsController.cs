using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Contracts.Clubs;
using SportReplay.Domain.Enums;

namespace SportReplay.Api.Controllers;

[ApiController]
[Route("api/clubs")]
[Authorize]
public class ClubsController : ControllerBase
{
    private readonly IClubService _clubs;
    private readonly ICourtService _courts;
    private readonly IDashboardService _dashboard;

    public ClubsController(IClubService clubs, ICourtService courts, IDashboardService dashboard)
    {
        _clubs = clubs;
        _courts = courts;
        _dashboard = dashboard;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await _clubs.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await _clubs.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> Create(CreateClubRequest request, CancellationToken cancellationToken)
        => Ok(await _clubs.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> Update(Guid id, UpdateClubRequest request, CancellationToken cancellationToken)
        => Ok(await _clubs.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _clubs.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{clubId:guid}/courts")]
    public async Task<IActionResult> Courts(Guid clubId, CancellationToken cancellationToken)
        => Ok(await _courts.GetByClubAsync(clubId, cancellationToken));

    [HttpPost("{clubId:guid}/courts")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> CreateCourt(Guid clubId, SportReplay.Application.Contracts.Courts.CreateCourtRequest request, CancellationToken cancellationToken)
        => Ok(await _courts.CreateAsync(clubId, request, cancellationToken));

    [HttpGet("{clubId:guid}/settings")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> Settings(Guid clubId, CancellationToken cancellationToken)
        => Ok(await _clubs.GetSettingsAsync(clubId, cancellationToken));

    [HttpPut("{clubId:guid}/settings")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> UpdateSettings(Guid clubId, UpdateClubSettingsRequest request, CancellationToken cancellationToken)
        => Ok(await _clubs.UpdateSettingsAsync(clubId, request, cancellationToken));

    [HttpGet("{clubId:guid}/dashboard")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> Dashboard(Guid clubId, CancellationToken cancellationToken)
        => Ok(await _dashboard.GetClubDashboardAsync(clubId, cancellationToken));
}
