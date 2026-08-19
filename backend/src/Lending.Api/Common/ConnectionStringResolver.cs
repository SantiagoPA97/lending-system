using Npgsql;

namespace Lending.Api.Common;

public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                   || databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                ? FromUrl(databaseUrl)
                : databaseUrl;
        }

        return configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "No database connection configured. Set ConnectionStrings:Default or the DATABASE_URL environment variable.");
    }

    private static string FromUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            SslMode = SslMode.Prefer
        };
        if (userInfo.Length > 1)
            builder.Password = Uri.UnescapeDataString(userInfo[1]);
        return builder.ConnectionString;
    }
}
