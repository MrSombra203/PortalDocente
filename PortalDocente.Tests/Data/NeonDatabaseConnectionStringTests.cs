using Npgsql;
using PortalDocente.Data.Connection;
using Xunit;

namespace PortalDocente.Tests.Data;

public class NeonDatabaseConnectionStringTests
{
    [Fact]
    public void FromDatabaseUrl_MapsCredentialsTlsAndPoolerHost()
    {
        var result = NeonDatabaseConnectionString.FromDatabaseUrl(
            "postgresql://user%40example.test:p%40ss%3Aword@ep-example-pooler.us-east-2.aws.neon.tech/neondb?sslmode=require&channel_binding=require");
        var connectionString = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("ep-example-pooler.us-east-2.aws.neon.tech", connectionString.Host);
        Assert.Equal("neondb", connectionString.Database);
        Assert.Equal("user@example.test", connectionString.Username);
        Assert.Equal("p@ss:word", connectionString.Password);
        Assert.Equal(SslMode.Require, connectionString.SslMode);
        Assert.Equal("Require", connectionString["Channel Binding"].ToString());
    }

    [Theory]
    [InlineData("https://example.test/db")]
    [InlineData("postgresql://user@example.test/db")]
    [InlineData("postgresql://user:password@example.test/")]
    [InlineData("postgresql://user:password@example.test/db?unsupported=true")]
    public void FromDatabaseUrl_RejectsUnsupportedOrIncompleteUrls(string databaseUrl)
    {
        Assert.Throws<InvalidOperationException>(
            () => NeonDatabaseConnectionString.FromDatabaseUrl(databaseUrl));
    }
}
