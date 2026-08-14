using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportReplay.Domain.Entities;

namespace SportReplay.Infrastructure.Persistence.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.HasIndex(x => new { x.CourtId, x.StartTime });
        builder.HasOne(x => x.Court).WithMany(x => x.Matches).HasForeignKey(x => x.CourtId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecordingConfiguration : IEntityTypeConfiguration<Recording>
{
    public void Configure(EntityTypeBuilder<Recording> builder)
    {
        builder.ToTable("recordings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StoragePath).HasMaxLength(500);
        builder.Property(x => x.HlsPath).HasMaxLength(500);
        builder.HasIndex(x => x.MatchId);
        builder.HasOne(x => x.Match).WithMany(x => x.Recordings).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Camera).WithMany(x => x.Recordings).HasForeignKey(x => x.CameraId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VideoSegmentConfiguration : IEntityTypeConfiguration<VideoSegment>
{
    public void Configure(EntityTypeBuilder<VideoSegment> builder)
    {
        builder.ToTable("video_segments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
        builder.HasOne(x => x.Recording).WithMany(x => x.Segments).HasForeignKey(x => x.RecordingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class VideoClipConfiguration : IEntityTypeConfiguration<VideoClip>
{
    public void Configure(EntityTypeBuilder<VideoClip> builder)
    {
        builder.ToTable("video_clips");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StoragePath).HasMaxLength(500);
        builder.Property(x => x.ThumbnailPath).HasMaxLength(500);
        builder.HasIndex(x => x.MatchId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Match).WithMany(x => x.Clips).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Recording).WithMany(x => x.Clips).HasForeignKey(x => x.RecordingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VideoRequestConfiguration : IEntityTypeConfiguration<VideoRequest>
{
    public void Configure(EntityTypeBuilder<VideoRequest> builder)
    {
        builder.ToTable("video_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PhoneNumber).HasMaxLength(40).IsRequired();
        builder.HasOne(x => x.User).WithMany(x => x.VideoRequests).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Match).WithMany(x => x.VideoRequests).HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.VideoClip).WithMany(x => x.VideoRequests).HasForeignKey(x => x.VideoClipId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.ExternalPaymentId).HasMaxLength(80);
        builder.Property(x => x.ExternalOrderId).HasMaxLength(80);
        builder.Property(x => x.PaymentMethod).HasMaxLength(80);
        builder.Property(x => x.InitPoint).HasMaxLength(1000);
        builder.Property(x => x.PreferenceId).HasMaxLength(120);
        builder.HasIndex(x => x.ExternalPaymentId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.User).WithMany(x => x.Payments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.VideoClip).WithMany(x => x.Payments).HasForeignKey(x => x.VideoClipId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Product).WithMany(x => x.Payments).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.VideoRequest).WithMany().HasForeignKey(x => x.VideoRequestId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PaymentWebhookConfiguration : IEntityTypeConfiguration<PaymentWebhook>
{
    public void Configure(EntityTypeBuilder<PaymentWebhook> builder)
    {
        builder.ToTable("payment_webhooks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(40).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ExternalId).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.Provider, x.EventType, x.ExternalId }).IsUnique();
    }
}

public class WhatsAppMessageConfiguration : IEntityTypeConfiguration<WhatsAppMessage>
{
    public void Configure(EntityTypeBuilder<WhatsAppMessage> builder)
    {
        builder.ToTable("whatsapp_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PhoneNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.MessageId).HasMaxLength(120);
        builder.Property(x => x.MediaUrl).HasMaxLength(1000);
        builder.HasIndex(x => x.MessageId);
        builder.HasOne(x => x.VideoRequest).WithMany(x => x.WhatsAppMessages).HasForeignKey(x => x.VideoRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Plan).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.HasOne(x => x.Club).WithMany(x => x.Subscriptions).HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Entity).HasMaxLength(80).IsRequired();
        builder.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.HasOne(x => x.Club).WithMany(x => x.Products).HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        builder.HasIndex(x => new { x.ClubId, x.Code }).IsUnique();
        builder.HasOne(x => x.Club).WithMany(x => x.Promotions).HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("processing_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.JobType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.Status, x.JobType });
    }
}
