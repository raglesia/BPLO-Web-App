using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Vehicles;

public sealed record VehicleRecord(string Vin, string CompanyName, string DriverName,
    string PlateNo, string SecRegNo, string DtiNumber, bool Archived);
public sealed record VehicleRecordResult(bool Success, string? Error, string? Vin = null);

public sealed class VehicleRecordService(IConfiguration configuration)
{
    public async Task<VehicleRecord?> GetAsync(string vin, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT VIN, CompanyName, DriverName, PlateNo, SECRegNo, DTINumber, IsArchived
            FROM VehiclePermits WHERE VIN=@vin
            """, connection);
        command.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.IsDBNull(4) ? "" : reader.GetString(4),
            reader.IsDBNull(5) ? "" : reader.GetString(5),
            !reader.IsDBNull(6) && reader.GetInt32(6) != 0);
    }

    public async Task<VehicleRecordResult> SaveAsync(string? vin, string company, string driver,
        string plate, string sec, string dti, int userId, CancellationToken cancellationToken)
    {
        company = company.Trim(); driver = driver.Trim(); plate = plate.Trim().ToUpperInvariant();
        sec = sec.Trim(); dti = dti.Trim();
        if (company.Length is 0 or > 255) return new(false, "Company name is required (maximum 255 characters).");
        if (plate.Length is 0 or > 100) return new(false, "Plate number is required (maximum 100 characters).");
        if (driver.Length > 255 || sec.Length > 100 || dti.Length > 100)
            return new(false, "One or more fields exceed the permitted length.");
        if (userId <= 0) return new(false, "Sign in again before saving.");

        bool create = string.IsNullOrWhiteSpace(vin);
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (create)
            {
                int year = DateTime.Now.Year;
                await using var next = new SqlCommand("""
                    SELECT ISNULL(MAX(TRY_CONVERT(int, SUBSTRING(VIN, 10, 100))), 0) + 1
                    FROM VehiclePermits WITH (UPDLOCK, HOLDLOCK) WHERE VIN LIKE @prefix
                    """, connection, transaction);
                next.Parameters.Add("@prefix", SqlDbType.NVarChar, 100).Value = $"VIN-{year}-%";
                int number = Convert.ToInt32(await next.ExecuteScalarAsync(cancellationToken));
                vin = $"VIN-{year}-{number:D4}";
            }
            else
            {
                await using var existing = new SqlCommand(
                    "SELECT IsArchived FROM VehiclePermits WITH (UPDLOCK, HOLDLOCK) WHERE VIN=@vin",
                    connection, transaction);
                existing.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin!;
                object? archived = await existing.ExecuteScalarAsync(cancellationToken);
                if (archived is null) return new(false, "Vehicle record was not found.");
                if (Convert.ToInt32(archived) != 0) return new(false, "Restore this vehicle before editing.");
            }

            await using var write = new SqlCommand(create ? """
                INSERT INTO VehiclePermits(VIN, CompanyName, DriverName, PlateNo, SECRegNo, DTINumber)
                VALUES(@vin, @company, @driver, @plate, @sec, @dti)
                """ : """
                UPDATE VehiclePermits SET CompanyName=@company, DriverName=@driver,
                    PlateNo=@plate, SECRegNo=@sec, DTINumber=@dti WHERE VIN=@vin AND IsArchived=0
                """, connection, transaction);
            write.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin!;
            write.Parameters.Add("@company", SqlDbType.NVarChar, 255).Value = company;
            write.Parameters.Add("@driver", SqlDbType.NVarChar, 255).Value = driver;
            write.Parameters.Add("@plate", SqlDbType.NVarChar, 100).Value = plate;
            write.Parameters.Add("@sec", SqlDbType.NVarChar, 100).Value = sec;
            write.Parameters.Add("@dti", SqlDbType.NVarChar, 100).Value = dti;
            if (await write.ExecuteNonQueryAsync(cancellationToken) != 1)
                return new(false, "Vehicle record changed; reload and try again.");

            await using var audit = new SqlCommand("""
                INSERT INTO AuditTrail(Action, SIN, UserId, Details)
                VALUES(@action, @vin, @user, @details)
                """, connection, transaction);
            audit.Parameters.Add("@action", SqlDbType.NVarChar, 100).Value =
                create ? "Add Vehicle Permit" : "Update Vehicle Permit";
            audit.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin!;
            audit.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            audit.Parameters.Add("@details", SqlDbType.NVarChar, 1000).Value = $"{company} | Plate: {plate}";
            await audit.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, null, vin);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return new(false, "A vehicle with this plate number or VIN already exists.");
        }
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        string connectionString = configuration.GetConnectionString("BPLS")
            ?? throw new InvalidOperationException("BPLS connection is not configured.");
        if (!string.Equals(new SqlConnectionStringBuilder(connectionString).InitialCatalog,
                "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Vehicle record work requires BPLS_Dev.");
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Vehicle record work requires BPLS_Dev.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }
}
