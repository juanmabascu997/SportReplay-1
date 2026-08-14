using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using SportReplay.Infrastructure.Persistence;
using SportReplay.Infrastructure.Security;
using SportReplay.Infrastructure.Services;
using SportReplay.Application.Contracts.Auth;
using SportReplay.Application.Options;
using Microsoft.Extensions.Options;
using SportReplay.Domain.Entities;
using SportReplay.Domain.Enums;

namespace SportReplay.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_creates_player_and_tokens()
    {
        var options = new DbContextOptionsBuilder<SportReplayDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new SportReplayDbContext(options);
        db.Roles.Add(new Role { Name = UserRoles.Player });
        await db.SaveChangesAsync();
        var tokens = new TokenService(Options.Create(new JwtOptions { Secret = "sportreplay-dev-jwt-secret-key-change-me-32", Issuer = "SportReplay", Audience = "SportReplay" }));
        var auth = new AuthService(db, tokens);
        var result = await auth.RegisterAsync(new RegisterRequest("Ana", "Perez", "ana@test.local", "Player123!", null, "Player"));
        result.Email.Should().Be("ana@test.local");
        result.Role.Should().Be(UserRoles.Player);
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
    }
}
