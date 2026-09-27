using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Authentication;

public sealed record AuthenticatedUser(int Id, string Username, string FullName, string Position);

public sealed class UserAuthenticationService(
    IConfiguration configuration,
    ILogger<UserAuthenticationService> logger)
{
    public async Task<AuthenticatedUser?> AuthenticateAsync(
        string username, string password, CancellationToken cancellationToken)
    {
        string? connectionString = configuration.GetConnectionString("BPLS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError("Login unavailable: BPLS connection string is not configured.");
            return null;
        }

        if (!string.Equals(new SqlConnectionStringBuilder(connectionString).InitialCatalog,
                "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Authentication requires BPLS_Dev.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Authentication requires BPLS_Dev.");
        await using var command = new SqlCommand(
            "SELECT Id, Username, FullName, Position, Password FROM Users WHERE Username = @username", connection);
        command.Parameters.Add("@username", SqlDbType.NVarChar, 255).Value = username.Trim();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var user = new AuthenticatedUser(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));
        string storedHash = reader.GetString(4);
        await reader.CloseAsync();

        if (!PasswordCompatibility.Verify(password, storedHash, out bool isLegacy)) return null;

        if (isLegacy)
        {
            try
            {
                await using var upgrade = new SqlCommand(
                    "UPDATE Users SET Password = @newHash WHERE Id = @id AND Password = @oldHash", connection);
                upgrade.Parameters.Add("@newHash", SqlDbType.NVarChar, 512).Value =
                    PasswordCompatibility.HashPbkdf2(password);
                upgrade.Parameters.Add("@oldHash", SqlDbType.NVarChar, 512).Value = storedHash;
                upgrade.Parameters.Add("@id", SqlDbType.Int).Value = user.Id;
                await upgrade.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                // Existing WinForms login also succeeds if a verified legacy hash cannot be upgraded.
                logger.LogWarning(exception, "Legacy password upgrade failed for user ID {UserId}.", user.Id);
            }
        }

        return user;
    }
}
