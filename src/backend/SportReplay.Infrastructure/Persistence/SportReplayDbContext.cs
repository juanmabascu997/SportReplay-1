using Microsoft.EntityFrameworkCore;
using SportReplay.Domain.Entities;

namespace SportReplay.Infrastructure.Persistence;

public class SportReplayDbContext : DbContext
{
    public SportReplayDbContext(DbContextOptions<SportReplayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubSettings> ClubSettings => Set<ClubSettings>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<CameraConfiguration> CameraConfigurations => Set<CameraConfiguration>();
    public DbSet<CameraEvent> CameraEvents => Set<CameraEvent>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Recording> Recordings => Set<Recording>();
    public DbSet<VideoSegment> VideoSegments => Set<VideoSegment>();
    public DbSet<VideoClip> VideoClips => Set<VideoClip>();
    public DbSet<VideoRequest> VideoRequests => Set<VideoRequest>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentWebhook> PaymentWebhooks => Set<PaymentWebhook>();
    public DbSet<WhatsAppMessage> WhatsAppMessages => Set<WhatsAppMessage>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Domain.Common.EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SportReplayDbContext).Assembly);
    }
}
