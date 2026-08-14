using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportReplay.Application.Abstractions;
using SportReplay.Application.Common;
using SportReplay.Application.Options;
using SportReplay.Infrastructure.Identity;
using SportReplay.Infrastructure.Payments;
using SportReplay.Infrastructure.Persistence;
using SportReplay.Infrastructure.Security;
using SportReplay.Infrastructure.Services;
using SportReplay.Infrastructure.Storage;
using SportReplay.Infrastructure.Video;
using SportReplay.Infrastructure.WhatsApp;
using SportReplay.Infrastructure.Workers;

namespace SportReplay.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DATABASE_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=sportreplay;Username=sportreplay;Password=sportreplay_dev";

        services.AddDbContext<SportReplayDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHttpContextAccessor();
        services.AddHttpClient("mercadopago");
        services.AddHttpClient("whatsapp");
        services.AddHttpClient("onvif");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<VideoOptions>(configuration.GetSection(VideoOptions.SectionName));
        services.Configure<MercadoPagoOptions>(configuration.GetSection(MercadoPagoOptions.SectionName));
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.SectionName));
        services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));
        services.Configure<CorsAppOptions>(configuration.GetSection(CorsAppOptions.SectionName));

        BindEnv(configuration);

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IClubAuthorization, ClubAuthorization>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IStorageService, MinioStorageService>();
        services.AddSingleton<IFfmpegService, FfmpegService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClubService, ClubService>();
        services.AddScoped<ICourtService, CourtService>();
        services.AddScoped<ICameraService, CameraService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IVideoClipService, VideoClipService>();
        services.AddScoped<IVideoRequestService, VideoRequestService>();
        services.AddScoped<IVideoProcessingService, VideoProcessingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IMercadoPagoService, MercadoPagoService>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddHostedService<VideoRecordingWorker>();
        services.AddHostedService<VideoProcessingWorker>();
        services.AddHostedService<VideoCleanupWorker>();
        services.AddHostedService<WhatsAppDispatchWorker>();
        return services;
    }

    private static void BindEnv(IConfiguration configuration)
    {
        // Environment variables from .env.example take precedence when present.
        var jwt = configuration.GetSection(JwtOptions.SectionName);
        if (!string.IsNullOrWhiteSpace(configuration["JWT_SECRET"])) jwt["Secret"] = configuration["JWT_SECRET"];
        if (!string.IsNullOrWhiteSpace(configuration["JWT_ISSUER"])) jwt["Issuer"] = configuration["JWT_ISSUER"];
        if (!string.IsNullOrWhiteSpace(configuration["JWT_AUDIENCE"])) jwt["Audience"] = configuration["JWT_AUDIENCE"];

        var storage = configuration.GetSection(StorageOptions.SectionName);
        if (!string.IsNullOrWhiteSpace(configuration["STORAGE_ENDPOINT"])) storage["Endpoint"] = configuration["STORAGE_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(configuration["STORAGE_ACCESS_KEY"])) storage["AccessKey"] = configuration["STORAGE_ACCESS_KEY"];
        if (!string.IsNullOrWhiteSpace(configuration["STORAGE_SECRET_KEY"])) storage["SecretKey"] = configuration["STORAGE_SECRET_KEY"];
        if (!string.IsNullOrWhiteSpace(configuration["STORAGE_BUCKET"])) storage["Bucket"] = configuration["STORAGE_BUCKET"];

        var video = configuration.GetSection(VideoOptions.SectionName);
        if (!string.IsNullOrWhiteSpace(configuration["VIDEO_RETENTION_DAYS"])) video["RetentionDays"] = configuration["VIDEO_RETENTION_DAYS"];
        if (!string.IsNullOrWhiteSpace(configuration["MAX_CLIP_DURATION_SECONDS"])) video["MaxClipDurationSeconds"] = configuration["MAX_CLIP_DURATION_SECONDS"];

        var mp = configuration.GetSection(MercadoPagoOptions.SectionName);
        if (!string.IsNullOrWhiteSpace(configuration["MERCADOPAGO_ACCESS_TOKEN"])) mp["AccessToken"] = configuration["MERCADOPAGO_ACCESS_TOKEN"];
        if (!string.IsNullOrWhiteSpace(configuration["MERCADOPAGO_WEBHOOK_SECRET"])) mp["WebhookSecret"] = configuration["MERCADOPAGO_WEBHOOK_SECRET"];

        var wa = configuration.GetSection(WhatsAppOptions.SectionName);
        if (!string.IsNullOrWhiteSpace(configuration["WHATSAPP_ACCESS_TOKEN"])) wa["AccessToken"] = configuration["WHATSAPP_ACCESS_TOKEN"];
        if (!string.IsNullOrWhiteSpace(configuration["WHATSAPP_PHONE_NUMBER_ID"])) wa["PhoneNumberId"] = configuration["WHATSAPP_PHONE_NUMBER_ID"];
        if (!string.IsNullOrWhiteSpace(configuration["WHATSAPP_BUSINESS_ACCOUNT_ID"])) wa["BusinessAccountId"] = configuration["WHATSAPP_BUSINESS_ACCOUNT_ID"];
        if (!string.IsNullOrWhiteSpace(configuration["WHATSAPP_API_VERSION"])) wa["ApiVersion"] = configuration["WHATSAPP_API_VERSION"];
    }
}
