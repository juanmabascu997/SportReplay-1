using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportReplay.Domain.Entities;

namespace SportReplay.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasOne(x => x.RoleEntity).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsRevoked);
        builder.Ignore(x => x.IsActive);
    }
}

public class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.ToTable("clubs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(80);
        builder.HasIndex(x => x.Name);
        builder.HasOne(x => x.Owner).WithMany(x => x.OwnedClubs).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Settings).WithOne(x => x.Club).HasForeignKey<ClubSettings>(x => x.ClubId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClubSettingsConfiguration : IEntityTypeConfiguration<ClubSettings>
{
    public void Configure(EntityTypeBuilder<ClubSettings> builder)
    {
        builder.ToTable("club_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClipPrice).HasPrecision(12, 2);
        builder.Property(x => x.FullMatchPrice).HasPrecision(12, 2);
        builder.Property(x => x.PricePerMinute).HasPrecision(12, 2);
        builder.Property(x => x.CommissionPercent).HasPrecision(5, 2);
        builder.HasIndex(x => x.ClubId).IsUnique();
    }
}

public class CourtConfiguration : IEntityTypeConfiguration<Court>
{
    public void Configure(EntityTypeBuilder<Court> builder)
    {
        builder.ToTable("courts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => x.ClubId);
        builder.HasOne(x => x.Club).WithMany(x => x.Courts).HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CameraConfigurationEntity : IEntityTypeConfiguration<Camera>
{
    public void Configure(EntityTypeBuilder<Camera> builder)
    {
        builder.ToTable("cameras");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(80);
        builder.Property(x => x.RtspUrl).HasMaxLength(500);
        builder.Property(x => x.OnvifUrl).HasMaxLength(500);
        builder.Property(x => x.Username).HasMaxLength(120);
        builder.Property(x => x.PasswordEncrypted).HasMaxLength(500);
        builder.HasIndex(x => x.CourtId);
        builder.HasOne(x => x.Court).WithMany(x => x.Cameras).HasForeignKey(x => x.CourtId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CameraConfigConfiguration : IEntityTypeConfiguration<CameraConfiguration>
{
    public void Configure(EntityTypeBuilder<CameraConfiguration> builder)
    {
        builder.ToTable("camera_configurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Resolution).HasMaxLength(40);
        builder.HasOne(x => x.Camera).WithMany(x => x.Configurations).HasForeignKey(x => x.CameraId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CameraEventConfiguration : IEntityTypeConfiguration<CameraEvent>
{
    public void Configure(EntityTypeBuilder<CameraEvent> builder)
    {
        builder.ToTable("camera_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => x.CameraId);
        builder.HasOne(x => x.Camera).WithMany(x => x.Events).HasForeignKey(x => x.CameraId).OnDelete(DeleteBehavior.Cascade);
    }
}
