using Npgsql;

namespace PortalDocente.Data.Connection;

public static class NeonDatabaseConnectionString
{
    public static string FromDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("postgres" or "postgresql"))
        {
            throw new InvalidOperationException(
                "DATABASE_URL must be an absolute postgres:// or postgresql:// URI.");
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (credentials.Length != 2
            || string.IsNullOrWhiteSpace(credentials[0])
            || string.IsNullOrWhiteSpace(credentials[1])
            || string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException(
                "DATABASE_URL must include a database, username, and password.");
        }

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = parameter.Split('=', 2);
            var name = Uri.UnescapeDataString(pair[0]);
            var value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1]) : string.Empty;

            if (name.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                var normalizedValue = value.Replace("-", string.Empty, StringComparison.Ordinal);
                if (!Enum.TryParse<SslMode>(normalizedValue, true, out var sslMode))
                    throw new InvalidOperationException("DATABASE_URL contains an unsupported sslmode.");
                connectionString.SslMode = sslMode;
            }
            else if (name.Equals("channel_binding", StringComparison.OrdinalIgnoreCase))
            {
                connectionString["Channel Binding"] = value;
            }
            else
            {
                throw new InvalidOperationException(
                    $"DATABASE_URL contains an unsupported parameter: {name}.");
            }
        }

        return connectionString.ConnectionString;
    }
}
