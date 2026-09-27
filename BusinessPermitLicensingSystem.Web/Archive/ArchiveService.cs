using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Archive;

public sealed record ArchivedProfile(string Sin, string Owner, string Business, string Stall, string Status);
public sealed record ArchivedVehicle(string Vin, string Company, string Driver, string Plate, string Status, int Year);
public sealed record ArchiveResult(bool Success, string Message);

public sealed class ArchiveService(IConfiguration configuration)
{
    public async Task<IReadOnlyList<ArchivedProfile>> ProfilesAsync(string? search, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = new SqlCommand("""
            SELECT TOP (100) SIN, FullName, BusinessName, StallNumber, PaymentStatus
            FROM Profiling WHERE IsArchived=1 AND
            (@search='' OR SIN LIKE @pattern OR FullName LIKE @pattern OR BusinessName LIKE @pattern OR StallNumber LIKE @pattern)
            ORDER BY SIN
            """, connection);
        AddSearch(command, search);
        var result = new List<ArchivedProfile>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.IsDBNull(3) ? "" : reader.GetString(3), reader.IsDBNull(4) ? "" : reader.GetString(4)));
        return result;
    }

    public async Task<IReadOnlyList<ArchivedVehicle>> VehiclesAsync(string? search, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = new SqlCommand("""
            SELECT TOP (100) VIN, CompanyName, DriverName, PlateNo, PermitStatus, PermitYear
            FROM VehiclePermits WHERE IsArchived=1 AND
            (@search='' OR VIN LIKE @pattern OR CompanyName LIKE @pattern OR DriverName LIKE @pattern OR PlateNo LIKE @pattern)
            ORDER BY VIN
            """, connection);
        AddSearch(command, search);
        var result = new List<ArchivedVehicle>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetString(4), reader.GetInt32(5)));
        return result;
    }

    public Task<ArchiveResult> ProfileAsync(string sin, bool archive, int userId, CancellationToken token) =>
        ChangeAsync("Profiling", "SIN", "FullName", sin, archive, userId,
            archive ? "Archive" : "Restore", archive ? "Archived profile for " : "Restored profile for ", token);

    public Task<ArchiveResult> VehicleAsync(string vin, bool archive, int userId, CancellationToken token) =>
        ChangeAsync("VehiclePermits", "VIN", "CompanyName", vin, archive, userId,
            archive ? "Archive Vehicle Permit" : "Restore Vehicle Permit", archive ? "Archived: " : "Restored: ", token);

    private async Task<ArchiveResult> ChangeAsync(string table, string key, string nameColumn, string id,
        bool archive, int userId, string action, string detailPrefix, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100) return new(false, "Record identifier is invalid.");
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            await using var read = new SqlCommand($"SELECT {nameColumn}, IsArchived FROM {table} WITH (UPDLOCK, HOLDLOCK) WHERE {key}=@id", connection, transaction);
            read.Parameters.Add("@id", SqlDbType.NVarChar, 100).Value = id;
            string name;
            bool current;
            await using (var reader = await read.ExecuteReaderAsync(token))
            {
                if (!await reader.ReadAsync(token)) return new(false, "Record was not found.");
                name = reader.GetString(0);
                current = !reader.IsDBNull(1) && reader.GetInt32(1) != 0;
            }
            if (current == archive) return new(false, archive ? "Record is already archived." : "Record is already active.");
            await using var update = new SqlCommand($"UPDATE {table} SET IsArchived=@state WHERE {key}=@id AND IsArchived=@old", connection, transaction);
            update.Parameters.Add("@state", SqlDbType.Int).Value = archive ? 1 : 0;
            update.Parameters.Add("@old", SqlDbType.Int).Value = archive ? 0 : 1;
            update.Parameters.Add("@id", SqlDbType.NVarChar, 100).Value = id;
            if (await update.ExecuteNonQueryAsync(token) != 1) return new(false, "Record state changed. Refresh and try again.");
            await using var audit = new SqlCommand("""
                INSERT INTO AuditTrail (Action, SIN, UserId, Timestamp, Details)
                VALUES (@action, @id, @user, GETDATE(), @details)
                """, connection, transaction);
            audit.Parameters.Add("@action", SqlDbType.NVarChar, 255).Value = action;
            audit.Parameters.Add("@id", SqlDbType.NVarChar, 100).Value = id;
            audit.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            audit.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value = detailPrefix + name + (table == "Profiling" ? "." : "");
            await audit.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
            return new(true, archive ? "Record archived." : "Record restored.");
        }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken token)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Archive work requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(token);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Archive work requires BPLS_Dev.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static void AddSearch(SqlCommand command, string? search)
    {
        string term = (search ?? "").Trim();
        if (term.Length > 100) term = term[..100];
        command.Parameters.Add("@search", SqlDbType.NVarChar, 100).Value = term;
        command.Parameters.Add("@pattern", SqlDbType.NVarChar, 512).Value = "%" +
            term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
    }
}
