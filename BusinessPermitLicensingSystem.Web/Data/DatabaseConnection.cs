using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Data;

/// <summary>Single fail-closed target guard; opening a connection never initializes a database.</summary>
public static class DatabaseConnection
{
    public static SqlConnectionStringBuilder Validate(IConfiguration configuration)
    {
        string environment = configuration["Database:Environment"] ?? "Development";
        bool development = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
        if (!development && environment is not ("Staging" or "Production"))
            throw new InvalidOperationException("Unsupported database environment.");
        string expectedDatabase = development ? "BPLS_Dev" : Required(configuration, "Database:ApprovedDatabase");
        string expectedServer = development ? configuration["Database:ApprovedServer"] ?? @".\SQLEXPRESS" : Required(configuration, "Database:ApprovedServer");
        if (!development && (expectedDatabase.Equals("BPLS_Dev", StringComparison.OrdinalIgnoreCase)
            || new[] { "master", "model", "msdb", "tempdb" }.Contains(expectedDatabase, StringComparer.OrdinalIgnoreCase)
            || (environment == "Production" && expectedDatabase.StartsWith("BPLS_Staging", StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("Database is not permitted for this environment.");
        if (!development) _ = Required(configuration, "Database:ApprovedServerIdentity");
        var builder = new SqlConnectionStringBuilder(Required(configuration, "ConnectionStrings:BPLS"));
        if (!builder.InitialCatalog.Equals(expectedDatabase, StringComparison.OrdinalIgnoreCase)
            || !builder.DataSource.Equals(expectedServer, StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrEmpty(builder.AttachDBFilename) || builder.UserInstance)
            throw new InvalidOperationException("Connection target does not match the approved server/database.");
        return builder;
    }

    public static async Task<SqlConnection> OpenAsync(IConfiguration configuration, CancellationToken token)
    {
        var builder = Validate(configuration);
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(token);
            await using var command = new SqlCommand("SELECT DB_NAME(), CONVERT(nvarchar(256), SERVERPROPERTY('ServerName'));", connection);
            await using var reader = await command.ExecuteReaderAsync(token);
            if (!await reader.ReadAsync(token) || !reader.GetString(0).Equals(builder.InitialCatalog, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Connected database does not match the approved target.");
            string? identity = configuration["Database:ApprovedServerIdentity"];
            if (!string.IsNullOrWhiteSpace(identity) && !reader.GetString(1).Equals(identity, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Connected SQL Server identity does not match the approved target.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static string Required(IConfiguration configuration, string key)
        => !string.IsNullOrWhiteSpace(configuration[key]) ? configuration[key]! : throw new InvalidOperationException($"Required database configuration is missing: {key}.");
}
