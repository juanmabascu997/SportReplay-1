using FluentAssertions;
using SportReplay.Application.Contracts.Videos;
using SportReplay.Application.Validation;
using SportReplay.Infrastructure.Security;
using Microsoft.Extensions.Options;
using SportReplay.Application.Options;

namespace SportReplay.UnitTests;

public class ValidatorTests
{
    [Fact]
    public void Clip_end_must_be_after_start()
    {
        var validator = new CreateVideoClipRequestValidator();
        var result = validator.Validate(new CreateVideoClipRequest(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10)));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Clip_valid_range_passes()
    {
        var validator = new CreateVideoClipRequestValidator();
        var result = validator.Validate(new CreateVideoClipRequest(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40)));
        result.IsValid.Should().BeTrue();
    }
}

public class EncryptionServiceTests
{
    [Fact]
    public void Encrypt_and_decrypt_roundtrip()
    {
        var service = new EncryptionService(Options.Create(new EncryptionOptions { Key = "unit-test-key" }));
        var cipher = service.Encrypt("rtsp-password");
        cipher.Should().NotBe("rtsp-password");
        service.Decrypt(cipher).Should().Be("rtsp-password");
    }
}

public class TokenServiceTests
{
    [Fact]
    public void Access_token_is_created()
    {
        var service = new TokenService(Options.Create(new JwtOptions
        {
            Secret = "sportreplay-dev-jwt-secret-key-change-me-32",
            Issuer = "SportReplay",
            Audience = "SportReplay"
        }));
        var (token, expires) = service.CreateAccessToken(Guid.NewGuid(), "admin@sportreplay.local", "Admin");
        token.Should().NotBeNullOrWhiteSpace();
        expires.Should().BeAfter(DateTime.UtcNow);
    }
}
