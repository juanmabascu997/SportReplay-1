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

        var isUri = raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                    || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
        var isLocal = raw.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                      || raw.Contains("127.0.0.1");

        if (isUri)
        {
            if (!raw.Contains("sslmode", StringComparison.OrdinalIgnoreCase) && !isLocal)
            {
                raw += raw.Contains('?', StringComparison.Ordinal) ? "&sslmode=require" : "?sslmode=require";
            }

            return raw;
        }

        if (!isLocal
            && !raw.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase)
            && !raw.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true";
        }

        return raw;
    }
}
