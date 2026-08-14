using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SportReplay.Infrastructure.Persistence;

public class SportReplayDbContextFactory : IDesignTimeDbContextFactory<SportReplayDbContext>
{
    public SportReplayDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SportReplayDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=sportreplay;Username=sportreplay;Password=sportreplay_dev")
            .Options;
        return new SportReplayDbContext(options);
    }
}
