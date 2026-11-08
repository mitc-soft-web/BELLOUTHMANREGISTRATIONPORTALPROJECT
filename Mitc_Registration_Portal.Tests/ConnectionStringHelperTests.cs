using Mitc_Registration_Portal.Extensions;
using MySqlConnector;
using Xunit;

namespace Mitc_Registration_Portal.Tests
{
    public class ConnectionStringHelperTests
    {
        [Fact]
        public void Normalize_LeavesStandardAdoNetConnectionStringUntouched()
        {
            var standard = "Server=localhost;Database=MitcRegPortal;user ID=root;Password=secret;";
            var result = ConnectionStringHelper.NormalizeMySqlConnectionString(standard);
            Assert.Equal(standard, result);
        }

        [Fact]
        public void Normalize_ConvertsMySqlUriWithCredentialsAndSslMode()
        {
            var uri = "mysql://u_8t0p3wgyai:cnwgu3o5xvs73ph70dn44rkluo0mqtae@mitc-wds4zf.db.enzonic.com:3306/main?ssl-mode=REQUIRED";
            var result = ConnectionStringHelper.NormalizeMySqlConnectionString(uri);

            var builder = new MySqlConnectionStringBuilder(result);
            Assert.Equal("mitc-wds4zf.db.enzonic.com", builder.Server);
            Assert.Equal(3306u, builder.Port);
            Assert.Equal("main", builder.Database);
            Assert.Equal("u_8t0p3wgyai", builder.UserID);
            Assert.Equal("cnwgu3o5xvs73ph70dn44rkluo0mqtae", builder.Password);
            Assert.Equal(MySqlSslMode.Required, builder.SslMode);
            Assert.True(builder.AllowPublicKeyRetrieval);
        }

        [Fact]
        public void Normalize_ConvertsMySqlUriWithoutPortDefaultsTo3306()
        {
            var uri = "mysql://admin:pass123@db.example.com/portal_db";
            var result = ConnectionStringHelper.NormalizeMySqlConnectionString(uri);

            var builder = new MySqlConnectionStringBuilder(result);
            Assert.Equal("db.example.com", builder.Server);
            Assert.Equal(3306u, builder.Port);
            Assert.Equal("portal_db", builder.Database);
            Assert.Equal("admin", builder.UserID);
            Assert.Equal("pass123", builder.Password);
            Assert.Equal(MySqlSslMode.Required, builder.SslMode);
        }

        [Fact]
        public void Normalize_HandlesQuotedStrings()
        {
            var uri = "\"mysql://user:pass@remote.db.com:3307/mydb?ssl-mode=preferred\"";
            var result = ConnectionStringHelper.NormalizeMySqlConnectionString(uri);

            var builder = new MySqlConnectionStringBuilder(result);
            Assert.Equal("remote.db.com", builder.Server);
            Assert.Equal(3307u, builder.Port);
            Assert.Equal("mydb", builder.Database);
            Assert.Equal(MySqlSslMode.Preferred, builder.SslMode);
        }

        [Fact]
        public void Normalize_ReturnsEmptyForNullOrWhitespace()
        {
            Assert.Equal(string.Empty, ConnectionStringHelper.NormalizeMySqlConnectionString(null));
            Assert.Equal(string.Empty, ConnectionStringHelper.NormalizeMySqlConnectionString("   "));
        }
    }
}
