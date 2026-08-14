using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportReplay.Application.Contracts.Auth;
using SportReplay.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SportReplay.IntegrationTests;

public class CustomWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("sportreplay")
        .WithUsername("sportreplay")
        .WithPassword("sportreplay_dev")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public new async Task DisposeAsync() => await _postgres.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("DATABASE_CONNECTION_STRING", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "sportreplay-dev-jwt-secret-key-change-me-32");
        builder.UseSetting("Storage:Endpoint", "localhost:9000");
    }
}

public class AuthAndClubTests : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthAndClubTests(CustomWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_seed_admin_works()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@sportreplay.local", "Admin123!"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Role.Should().Be("Admin");
        body.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_and_login_player()
    {
        var email = $"player{Guid.NewGuid():N}@sportreplay.local";
        var register = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("Ana", "Perez", email, "Player123!", "+54911", "Player"));
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Player123!"));
        login.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Club_owner_cannot_access_other_club_settings()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("owner@sportreplay.local", "Owner123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var otherClub = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var response = await _client.GetAsync($"/api/clubs/{otherClub}/settings");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Seeded_club_and_cameras_are_listed()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@sportreplay.local", "Admin123!"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var clubs = await _client.GetAsync("/api/clubs");
        clubs.StatusCode.Should().Be(HttpStatusCode.OK);
        var cameras = await _client.GetAsync("/api/cameras");
        cameras.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class PaymentWebhookTests : IClassFixture<CustomWebAppFactory>
{
    private readonly CustomWebAppFactory _factory;
    private readonly HttpClient _client;

    public PaymentWebhookTests(CustomWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Webhook_is_idempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SportReplayDbContext>();
        var payment = await db.Payments.FirstOrDefaultAsync();
        if (payment is null)
        {
            var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("player@sportreplay.local", "Player123!"));
            var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
            var create = await _client.PostAsJsonAsync("/api/payments/create", new
            {
                videoClipId = DbSeeder.ClipId,
                productType = 1
            });
            create.EnsureSuccessStatusCode();
            payment = await db.Payments.FirstAsync();
        }

        var payload = JsonSerializer.Serialize(new { type = "payment", data = new { id = "sandbox-1" }, paymentId = payment!.Id, status = "approved" });
        var first = await _client.PostAsync("/api/payments/webhook", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
        var second = await _client.PostAsync("/api/payments/webhook", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
        first.IsSuccessStatusCode.Should().BeTrue();
        second.IsSuccessStatusCode.Should().BeTrue();
        var count = await db.PaymentWebhooks.CountAsync(x => x.ExternalId == "sandbox-1");
        count.Should().Be(1);
    }
}
