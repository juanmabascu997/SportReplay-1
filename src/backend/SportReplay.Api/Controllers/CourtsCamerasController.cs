using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Contracts.Cameras;
using SportReplay.Application.Contracts.Courts;
using SportReplay.Domain.Enums;

namespace SportReplay.Api.Controllers;

[ApiController]
[Authorize]
public class CourtsCamerasController : ControllerBase
{
    private readonly ICourtService _courts;
    private readonly ICameraService _cameras;
    private readonly IClubService _clubs;

    public CourtsCamerasController(ICourtService courts, ICameraService cameras, IClubService clubs)
    {
        _courts = courts;
        _cameras = cameras;
        _clubs = clubs;
    }

    [HttpPut("/api/courts/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> UpdateCourt(Guid id, UpdateCourtRequest request, CancellationToken cancellationToken)
        => Ok(await _courts.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("/api/courts/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> DeleteCourt(Guid id, CancellationToken cancellationToken)
    {
        await _courts.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("/api/cameras")]
    public async Task<IActionResult> GetAllCameras(CancellationToken cancellationToken)
    {
        var clubs = await _clubs.GetAllAsync(cancellationToken);
        var result = new List<CameraDto>();
        foreach (var club in clubs)
        {
            var courts = await _courts.GetByClubAsync(club.Id, cancellationToken);
            foreach (var court in courts)
            {
                result.AddRange(await _cameras.GetByCourtAsync(court.Id, cancellationToken));
            }
        }

        return Ok(result);
    }

    [HttpGet("/api/courts/{courtId:guid}/cameras")]
    public async Task<IActionResult> GetCameras(Guid courtId, CancellationToken cancellationToken)
        => Ok(await _cameras.GetByCourtAsync(courtId, cancellationToken));

    [HttpPost("/api/courts/{courtId:guid}/cameras")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> CreateCamera(Guid courtId, CreateCameraRequest request, CancellationToken cancellationToken)
        => Ok(await _cameras.CreateAsync(courtId, request, cancellationToken));

    [HttpPut("/api/cameras/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> UpdateCamera(Guid id, UpdateCameraRequest request, CancellationToken cancellationToken)
        => Ok(await _cameras.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("/api/cameras/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> DeleteCamera(Guid id, CancellationToken cancellationToken)
    {
        await _cameras.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("/api/cameras/{id:guid}/test-connection")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> Test(Guid id, CancellationToken cancellationToken)
        => Ok(await _cameras.TestConnectionAsync(id, cancellationToken));

    [HttpPost("/api/cameras/{id:guid}/start")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        await _cameras.StartAsync(id, cancellationToken);
        return Accepted();
    }

    [HttpPost("/api/cameras/{id:guid}/stop")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> Stop(Guid id, CancellationToken cancellationToken)
    {
        await _cameras.StopAsync(id, cancellationToken);
        return Accepted();
    }

    [HttpGet("/api/cameras/{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, CancellationToken cancellationToken)
        => Ok(await _cameras.GetStatusAsync(id, cancellationToken));
}
