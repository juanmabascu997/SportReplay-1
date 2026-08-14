using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Contracts.Matches;
using SportReplay.Application.Contracts.Videos;
using SportReplay.Domain.Enums;

namespace SportReplay.Api.Controllers;

[ApiController]
[Authorize]
public class MatchesVideosController : ControllerBase
{
    private readonly IMatchService _matches;
    private readonly IVideoClipService _clips;
    private readonly IVideoRequestService _requests;

    public MatchesVideosController(IMatchService matches, IVideoClipService clips, IVideoRequestService requests)
    {
        _matches = matches;
        _clips = clips;
        _requests = requests;
    }

    [HttpGet("/api/matches")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] Guid? clubId, [FromQuery] Guid? courtId, [FromQuery] DateOnly? date, [FromQuery] TimeOnly? time, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(await _matches.SearchAsync(new MatchSearchQuery(clubId, courtId, date, time, page, pageSize), cancellationToken));

    [HttpGet("/api/matches/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await _matches.GetByIdAsync(id, cancellationToken));

    [HttpPost("/api/matches")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> Create(CreateMatchRequest request, CancellationToken cancellationToken)
        => Ok(await _matches.CreateAsync(request, cancellationToken));

    [HttpPut("/api/matches/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> Update(Guid id, UpdateMatchRequest request, CancellationToken cancellationToken)
        => Ok(await _matches.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("/api/matches/{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _matches.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("/api/matches/{id:guid}/recordings")]
    [AllowAnonymous]
    public async Task<IActionResult> Recordings(Guid id, CancellationToken cancellationToken)
        => Ok(await _matches.GetRecordingsAsync(id, cancellationToken));

    [HttpPost("/api/matches/{id:guid}/recordings")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.ClubOwner},{UserRoles.Operator}")]
    public async Task<IActionResult> CreateRecording(Guid id, [FromBody] Guid cameraId, CancellationToken cancellationToken)
        => Ok(await _matches.CreateRecordingAsync(new CreateRecordingRequest(id, cameraId), cancellationToken));

    [HttpGet("/api/matches/{id:guid}/clips")]
    public async Task<IActionResult> Clips(Guid id, CancellationToken cancellationToken)
        => Ok(await _clips.GetByMatchAsync(id, cancellationToken));

    [HttpPost("/api/video-clips")]
    public async Task<IActionResult> CreateClip(CreateVideoClipRequest request, CancellationToken cancellationToken)
        => Accepted(await _clips.CreateAsync(request, cancellationToken));

    [HttpGet("/api/video-clips/{id:guid}")]
    public async Task<IActionResult> GetClip(Guid id, CancellationToken cancellationToken)
        => Ok(await _clips.GetByIdAsync(id, cancellationToken));

    [HttpGet("/api/videos/{id:guid}/stream")]
    public async Task<IActionResult> Stream(Guid id, CancellationToken cancellationToken)
        => Ok(await _clips.GetStreamAsync(id, cancellationToken));

    [HttpGet("/api/videos/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        => Ok(await _clips.GetDownloadAsync(id, cancellationToken));

    [HttpGet("/api/recordings/{id:guid}/stream")]
    public async Task<IActionResult> RecordingStream(Guid id, CancellationToken cancellationToken)
        => Ok(await _clips.GetRecordingStreamAsync(id, cancellationToken));

    [HttpPost("/api/video-requests")]
    public async Task<IActionResult> CreateRequest(CreateVideoRequestRequest request, CancellationToken cancellationToken)
        => Ok(await _requests.CreateAsync(request, cancellationToken));
}
