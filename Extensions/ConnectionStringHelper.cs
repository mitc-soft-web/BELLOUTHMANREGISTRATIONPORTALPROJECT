using MySqlConnector;
using System.Text.RegularExpressions;

namespace Mitc_Registration_Portal.Extensions
{
    public static class ConnectionStringHelper
    {
        public static string NormalizeMySqlConnectionString(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            raw = raw.Trim();

            // Strip surrounding quotes if present
            if ((raw.StartsWith("\"") && raw.EndsWith("\"")) || (raw.StartsWith("'") && raw.EndsWith("'")))
            {
                raw = raw[1..^1].Trim();
            }

            // If it is already in ADO.NET key-value format (e.g. Server=... or Data Source=...)
            if (!raw.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) &&
                !raw.StartsWith("mysqls://", StringComparison.OrdinalIgnoreCase))
            {
                return raw;
            }

            // Parse URI: mysql://user:password@host:port/database?options
            var uri = new Uri(raw);

            var builder = new MySqlConnectionStringBuilder
            {
                Server = uri.Host,
                Port = (uint)(uri.Port > 0 ? uri.Port : 3306),
                Database = uri.AbsolutePath.TrimStart('/'),
                AllowPublicKeyRetrieval = true
            };

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userParts = uri.UserInfo.Split(':', 2);
                builder.UserID = Uri.UnescapeDataString(userParts[0]);
                if (userParts.Length > 1)
                {
                    builder.Password = Uri.UnescapeDataString(userParts[1]);
                }
            }

            // Default SSL mode for remote cloud hosts
            var isLocal = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                          uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

            builder.SslMode = isLocal ? MySqlSslMode.None : MySqlSslMode.Required;

            // Parse query string options (e.g. ?ssl-mode=REQUIRED&charset=utf8mb4)
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var query = uri.Query.TrimStart('?');
                var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var kvp = pair.Split('=', 2);
                    var key = Uri.UnescapeDataString(kvp[0]).ToLowerInvariant();
                    var value = kvp.Length > 1 ? Uri.UnescapeDataString(kvp[1]) : "";

                    switch (key)
                    {
                        case "ssl-mode":
                        case "sslmode":
                            builder.SslMode = value.ToLowerInvariant() switch
                            {
                                "disabled" or "none" or "disable" => MySqlSslMode.None,
                                "preferred" or "prefer" => MySqlSslMode.Preferred,
                                "required" or "require" => MySqlSslMode.Required,
                                "verify_ca" or "verify-ca" => MySqlSslMode.VerifyCA,
                                "verify_identity" or "verify-full" => MySqlSslMode.VerifyFull,
                                _ => MySqlSslMode.Required
                            };
                            break;

                        case "charset":
                        case "characterset":
                            builder.CharacterSet = value;
                            break;

                        case "connectiontimeout":
                        case "connect_timeout":
                        case "timeout":
                            if (uint.TryParse(value, out var timeout))
                            {
                                builder.ConnectionTimeout = timeout;
                            }
                            break;

                        case "allowpublickeyretrieval":
                            if (bool.TryParse(value, out var allowPubKey))
                            {
                                builder.AllowPublicKeyRetrieval = allowPubKey;
                            }
                            break;
                    }
                }
            }

            return builder.ConnectionString;
        }
    }
}
