using Microsoft.Extensions.Configuration;

namespace SportReplay.Infrastructure.Persistence;

public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var raw = configuration["DATABASE_CONNECTION_STRING"]
                  ?? configuration["DATABASE_URL"]
                  ?? configuration.GetConnectionString("Default")
                  ?? "Host=localhost;Port=5432;Database=sportreplay;Username=sportreplay;Password=sportreplay_dev";

        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return FromUri(raw);
        }

        var isLocal = raw.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                      || raw.Contains("127.0.0.1");
        if (!isLocal
            && !raw.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase)
            && !raw.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true";
        }

        return raw;
    }

    private static string FromUri(string raw)
    {
        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.Trim('/');
        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        return $"Host={uri.Host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
