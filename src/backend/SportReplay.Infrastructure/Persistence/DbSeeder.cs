using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;

namespace SportReplay.Infrastructure.Persistence;

public static class DbSeeder
{
    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid OwnerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid PlayerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ClubId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid Court1Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    public static readonly Guid Court2Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    public static readonly Guid Court3Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");
    public static readonly Guid Camera1Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
    public static readonly Guid Camera2Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
    public static readonly Guid Camera3Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3");
    public static readonly Guid MatchId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static readonly Guid RecordingId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public static readonly Guid ClipId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    public static async Task SeedAsync(SportReplayDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        logger.LogInformation("Seeding development data");

        var roles = UserRoles.All.Select(name => new Role { Name = name }).ToList();
        db.Roles.AddRange(roles);
        await db.SaveChangesAsync(cancellationToken);

        var adminRole = roles.First(r => r.Name == UserRoles.Admin);
        var ownerRole = roles.First(r => r.Name == UserRoles.ClubOwner);
        var playerRole = roles.First(r => r.Name == UserRoles.Player);

        var admin = new User
        {
            Id = AdminId,
            FirstName = "Admin",
            LastName = "SportReplay",
            Email = "admin@sportreplay.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRoles.Admin,
            RoleId = adminRole.Id,
            Phone = "+5491100000000"
        };
        var owner = new User
        {
            Id = OwnerId,
            FirstName = "Lucia",
            LastName = "ClubOwner",
            Email = "owner@sportreplay.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Owner123!"),
            Role = UserRoles.ClubOwner,
            RoleId = ownerRole.Id,
            Phone = "+5491100000001"
        };
        var player = new User
        {
            Id = PlayerId,
            FirstName = "Mateo",
            LastName = "Player",
            Email = "player@sportreplay.local",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Player123!"),
            Role = UserRoles.Player,
            RoleId = playerRole.Id,
            Phone = "+5491100000002"
        };
        db.Users.AddRange(admin, owner, player);

        var club = new Club
        {
            Id = ClubId,
            Name = "Padel Norte",
            Description = "Club de padel de desarrollo",
            Address = "Av. Libertador 1200",
            City = "Buenos Aires",
            Province = "CABA",
            Country = "Argentina",
            Phone = "+541148000000",
            Email = "club@sportreplay.local",
            OwnerUserId = OwnerId,
            AllowFullMatchDownload = false
        };
        db.Clubs.Add(club);

        db.ClubSettings.Add(new ClubSettings
        {
            ClubId = ClubId,
            ClipPrice = 1500,
            FullMatchPrice = 4500,
            PricePerMinute = 250,
            CommissionPercent = 10,
            VideoRetentionDays = 7,
            MaxClipDurationSeconds = 60,
            AllowFullMatchDownload = false
        });

        var courts = new[]
        {
            new Court { Id = Court1Id, ClubId = ClubId, Name = "Cancha 1", SportType = SportType.Padel, Description = "Central" },
            new Court { Id = Court2Id, ClubId = ClubId, Name = "Cancha 2", SportType = SportType.Padel, Description = "Techada" },
            new Court { Id = Court3Id, ClubId = ClubId, Name = "Cancha 3", SportType = SportType.Tennis, Description = "Polvo de ladrillo" }
        };
        db.Courts.AddRange(courts);

        var cameras = new[]
        {
            CreateSimulatedCamera(Camera1Id, Court1Id, "Cam Central 1", "10.0.0.11"),
            CreateSimulatedCamera(Camera2Id, Court2Id, "Cam Central 2", "10.0.0.12"),
            CreateSimulatedCamera(Camera3Id, Court3Id, "Cam Tennis 3", "10.0.0.13")
        };
        db.Cameras.AddRange(cameras);

        foreach (var camera in cameras)
        {
            db.CameraConfigurations.Add(new CameraConfiguration
            {
                CameraId = camera.Id,
                Resolution = "1280x720",
                Fps = 25,
                Bitrate = 2000,
                RecordingEnabled = true,
                SegmentDurationSeconds = 6
            });
        }

        var start = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddHours(18), DateTimeKind.Utc);
        var match = new Match
        {
            Id = MatchId,
            CourtId = Court1Id,
            StartTime = start,
            EndTime = start.AddHours(1.5),
            Status = MatchStatus.Completed,
            Title = "Partido demo - Cancha 1"
        };
        db.Matches.Add(match);

        var recording = new Recording
        {
            Id = RecordingId,
            MatchId = MatchId,
            CameraId = Camera1Id,
            StartedAt = start,
            EndedAt = start.AddHours(1.5),
            Status = RecordingStatus.Ready,
            StoragePath = $"recordings/{RecordingId}/source.mp4",
            HlsPath = $"recordings/{RecordingId}/hls/index.m3u8",
            DurationSeconds = 5400,
            FileSize = 120_000_000
        };
        db.Recordings.Add(recording);

        db.VideoClips.Add(new VideoClip
        {
            Id = ClipId,
            MatchId = MatchId,
            RecordingId = RecordingId,
            StartTime = TimeSpan.FromMinutes(12),
            EndTime = TimeSpan.FromMinutes(12.5),
            DurationSeconds = 30,
            StoragePath = $"clips/{ClipId}/clip.mp4",
            ThumbnailPath = $"clips/{ClipId}/thumb.jpg",
            Status = VideoClipStatus.Ready,
            IsPublic = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        db.Products.AddRange(
            new Product { ClubId = ClubId, Type = ProductType.Clip, Name = "Clip 60s", Price = 1500 },
            new Product { ClubId = ClubId, Type = ProductType.FullMatch, Name = "Partido completo", Price = 4500 },
            new Product { ClubId = ClubId, Type = ProductType.Subscription, Name = "Plan club mensual", Price = 29000 });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Development seed completed");
    }

    private static Camera CreateSimulatedCamera(Guid id, Guid courtId, string name, string ip) => new()
    {
        Id = id,
        CourtId = courtId,
        Name = name,
        IpAddress = ip,
        RtspUrl = $"rtsp://simulated/{ip}/stream",
        Protocol = CameraProtocol.Simulated,
        Status = CameraStatus.Online,
        LastHeartbeat = DateTime.UtcNow,
        IsSimulated = true,
        IsActive = true
    };
}
