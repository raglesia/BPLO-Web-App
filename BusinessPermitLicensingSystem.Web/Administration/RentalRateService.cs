using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Administration;

public sealed record RentalRateRecord(string Section, decimal RatePerSqm, decimal FlatRate, string RateType)
{
    public decimal CurrentRate => RateType == "Flat" ? FlatRate : RatePerSqm;
}
public sealed record RateSaveResult(bool Success, string Message);

public sealed class RentalRateService(IConfiguration configuration)
{
    public async Task<IReadOnlyList<RentalRateRecord>> ListAsync(CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = new SqlCommand("SELECT Section, RatePerSqm, FlatRate, RateType FROM RentalRates ORDER BY Section", connection);
        var result = new List<RentalRateRecord>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            result.Add(new(reader.GetString(0), reader.GetDecimal(1), reader.GetDecimal(2), reader.GetString(3)));
        return result;
    }

    public async Task<RateSaveResult> SaveAsync(string section, string rateType, decimal ratePerSqm,
        decimal flatRate, bool add, int userId, CancellationToken token)
    {
        section = section?.Trim() ?? "";
        string? error = Validate(section, rateType, ratePerSqm, flatRate);
        if (error is not null) return new(false, error);
        if (userId <= 0) return new(false, "Sign in again before saving a rate.");
        if (rateType == "Flat") ratePerSqm = 0;
        else flatRate = 0;
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            if (add)
            {
                await using var insert = new SqlCommand("""
                    INSERT INTO RentalRates (Section, RatePerSqm, FlatRate, RateType)
                    VALUES (@section, @perSqm, @flat, @type)
                    """, connection, transaction);
                Parameters(insert, section, rateType, ratePerSqm, flatRate);
                await insert.ExecuteNonQueryAsync(token);
            }
            else
            {
                await using var update = new SqlCommand("""
                    UPDATE RentalRates SET RatePerSqm=@perSqm, FlatRate=@flat, RateType=@type
                    WHERE Section=@section
                    """, connection, transaction);
                Parameters(update, section, rateType, ratePerSqm, flatRate);
                if (await update.ExecuteNonQueryAsync(token) != 1) return new(false, "Section no longer exists. Refresh the list.");
            }
            await using var audit = new SqlCommand("""
                INSERT INTO AuditTrail (Action, SIN, UserId, Timestamp, Details)
                VALUES (@action, NULL, @user, GETDATE(), @details)
                """, connection, transaction);
            audit.Parameters.Add("@action", SqlDbType.NVarChar, 255).Value = add ? "AddRate" : "UpdateRate";
            audit.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            audit.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value =
                $"{(add ? "Added new section" : "Updated rate for")}: {section}, {rateType}, PerSqm=₱{ratePerSqm:N2}, Flat=₱{flatRate:N2}";
            await audit.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
            return new(true, add ? "Section added." : "Rental rate updated.");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { return new(false, "Section already exists."); }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    public static string? Validate(string section, string rateType, decimal ratePerSqm, decimal flatRate)
    {
        if (string.IsNullOrWhiteSpace(section) || section.Length > 255) return "Enter a section name of at most 255 characters.";
        if (rateType is not ("PerSqm" or "Flat")) return "Choose PerSqm or Flat.";
        if (ratePerSqm < 0 || flatRate < 0 || ratePerSqm > 9999999999999999.99m || flatRate > 9999999999999999.99m ||
            decimal.Round(ratePerSqm, 2) != ratePerSqm || decimal.Round(flatRate, 2) != flatRate)
            return "Rates must be nonnegative amounts with at most two decimals.";
        if (rateType == "PerSqm" && ratePerSqm <= 0) return "Rate per sqm must be greater than zero.";
        if (rateType == "Flat" && flatRate <= 0) return "Flat rate must be greater than zero.";
        return null;
    }

    private static void Parameters(SqlCommand command, string section, string rateType, decimal perSqm, decimal flat)
    {
        command.Parameters.Add("@section", SqlDbType.NVarChar, 255).Value = section;
        command.Parameters.Add("@type", SqlDbType.NVarChar, 50).Value = rateType;
        var p = command.Parameters.Add("@perSqm", SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = perSqm;
        p = command.Parameters.Add("@flat", SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = flat;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken token)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Rental rate work requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        try { await connection.OpenAsync(token);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Rental rate work requires BPLS_Dev.");
            return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
