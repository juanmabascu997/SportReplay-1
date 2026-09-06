using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Contracts.Videos;
using SportReplay.Application.Exceptions;
using SportReplay.Application.Options;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;
using SportReplay.Infrastructure.Persistence;
using SportReplay.Infrastructure.Services;

namespace SportReplay.UnitTests;

public class VideoClipServiceTests
{
    [Fact]
    public async Task Create_rejects_clip_longer_than_limit()
    {
        var options = new DbContextOptionsBuilder<SportReplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new SportReplayDbContext(options);
        var club = new Club { Name = "Club", OwnerUserId = Guid.NewGuid() };
        db.Clubs.Add(club);
        db.ClubSettings.Add(new ClubSettings { ClubId = club.Id, MaxClipDurationSeconds = 60 });
        var court = new Court { ClubId = club.Id, Name = "1" };
        db.Courts.Add(court);
        var match = new Domain.Entities.Match { CourtId = court.Id, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) };
        db.Matches.Add(match);
        var camera = new Camera { CourtId = court.Id, Name = "cam", IsSimulated = true, Protocol = CameraProtocol.Simulated };
        db.Cameras.Add(camera);
        var recording = new Recording { MatchId = match.Id, CameraId = camera.Id, StartedAt = DateTime.UtcNow, Status = RecordingStatus.Ready };
        db.Recordings.Add(recording);
        await db.SaveChangesAsync();

        var service = new VideoClipService(
            db,
            Mock.Of<IStorageService>(),
            Options.Create(new VideoOptions { MaxClipDurationSeconds = 60, RetentionDays = 7 }),
            Options.Create(new StorageOptions()));

        var act = () => service.CreateAsync(new CreateVideoClipRequest(match.Id, recording.Id, TimeSpan.Zero, TimeSpan.FromSeconds(90)));
        await act.Should().ThrowAsync<AppException>();
    }
}
