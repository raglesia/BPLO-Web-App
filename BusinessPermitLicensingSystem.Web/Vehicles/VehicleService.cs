using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Vehicles;

public sealed record VehicleSummary(string Vin, string Company, string Driver, string Plate,
    string StoredStatus, int StoredYear, string CurrentYearStatus);
public sealed record VehiclePayment(int Id, string OrNumber, decimal Amount, int Year, DateTime PaidAt, string Recorder);
public sealed record VehicleDetails(string Vin, string Company, string Driver, string Plate,
    string StoredStatus, int StoredYear, bool Archived, int SelectedYear,
    DraftSnapshot? Draft, string? DraftError, IReadOnlyList<VehiclePayment> History)
{
    public bool PaidForSelectedYear => History.Any(x => x.Year == SelectedYear);
    public bool StoredPaidForSelectedYear => StoredStatus == "Paid" && StoredYear == SelectedYear;
    public string SelectedYearStatus => PaidForSelectedYear || StoredPaidForSelectedYear ? "Paid" : "Unpaid";
    public bool Eligible => !Archived && !PaidForSelectedYear && !StoredPaidForSelectedYear;
}
public sealed record VehicleList(IReadOnlyList<VehicleSummary> Items, int Total);
public sealed record DraftSaveResult(bool Success, string? Error, decimal Total = 0);
public sealed record VehiclePaymentResult(bool Success, string? Error, string? OrNumber = null,
    decimal Amount = 0, int Year = 0);

public sealed class VehicleService(IConfiguration configuration)
{
    public const int PageSize = 25;

    public async Task<VehicleList> ListAsync(string? search, int page, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        string term = (search ?? "").Trim();
        string pattern = "%" + term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        const string where = "v.IsArchived=0 AND (@term='' OR v.VIN LIKE @pattern OR v.CompanyName LIKE @pattern OR v.DriverName LIKE @pattern OR v.PlateNo LIKE @pattern)";
        await using var count = new SqlCommand($"SELECT COUNT(*) FROM VehiclePermits v WHERE {where}", connection);
        Search(count, term, pattern);
        int total = (int)(await count.ExecuteScalarAsync(cancellationToken) ?? 0);
        page = Math.Clamp(page, 1, Math.Max(1, (total + PageSize - 1) / PageSize));
        await using var command = new SqlCommand($"""
            SELECT v.VIN, v.CompanyName, v.DriverName, v.PlateNo, v.PermitStatus, v.PermitYear,
                   CASE WHEN (v.PermitStatus='Paid' AND v.PermitYear=@year)
                             OR EXISTS (SELECT 1 FROM VehiclePermitHistory h
                                        WHERE h.VIN=v.VIN AND h.PermitYear=@year)
                        THEN 'Paid' ELSE 'Unpaid' END
            FROM VehiclePermits v WHERE {where}
            ORDER BY v.VIN OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """, connection);
        Search(command, term, pattern);
        command.Parameters.Add("@skip", SqlDbType.Int).Value = (page - 1) * PageSize;
        command.Parameters.Add("@take", SqlDbType.Int).Value = PageSize;
        command.Parameters.Add("@year", SqlDbType.Int).Value = DateTime.Today.Year;
        var items = new List<VehicleSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            items.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetInt32(5), reader.GetString(6)));
        return new(items, total);
    }

    public async Task<VehicleDetails?> GetAsync(string vin, DateTime asOf, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT VIN, CompanyName, DriverName, PlateNo, PermitStatus, PermitYear, IsArchived
            FROM VehiclePermits WHERE VIN=@vin
            """, connection);
        command.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
        string company, driver, plate, status;
        int storedYear;
        bool archived;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;
            company = reader.GetString(1); driver = reader.GetString(2);
            plate = reader.GetString(3); status = reader.GetString(4);
            storedYear = reader.GetInt32(5);
            archived = !reader.IsDBNull(6) && reader.GetInt32(6) != 0;
        }

        await using var historyCommand = new SqlCommand("""
            SELECT h.Id, h.ORNumber, h.AmountPaid, h.PermitYear, h.DatePaid,
                   COALESCE(u.FullName, CONCAT('User ', h.RecordedBy))
            FROM VehiclePermitHistory h LEFT JOIN Users u ON u.Id=h.RecordedBy
            WHERE h.VIN=@vin ORDER BY h.PermitYear DESC, h.DatePaid DESC, h.Id DESC
            """, connection);
        historyCommand.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
        var history = new List<VehiclePayment>();
        await using (var reader = await historyCommand.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                history.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader.GetInt32(3),
                    reader.GetDateTime(4), reader.GetString(5)));

        await using var draftCommand = new SqlCommand("""
            SELECT DetailsJson, GrandTotal FROM VehiclePermitFeeDrafts
            WHERE VIN=@vin AND PermitYear=@year
            """, connection);
        draftCommand.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
        draftCommand.Parameters.Add("@year", SqlDbType.Int).Value = asOf.Year;
        DraftSnapshot? draft = null;
        string? draftError = null;
        await using (var reader = await draftCommand.ExecuteReaderAsync(cancellationToken))
            if (await reader.ReadAsync(cancellationToken))
            {
                try { draft = VehicleFeeDraft.Read(reader.GetString(0), reader.GetDecimal(1)); }
                catch (Exception exception) when (exception is ArgumentException or OverflowException or
                    InvalidOperationException or System.Text.Json.JsonException)
                { draftError = "Saved fee draft needs review before payment."; }
            }
        return new(vin, company, driver, plate, status, storedYear, archived, asOf.Year,
            draft, draftError, history);
    }

    public async Task<DraftSaveResult> SaveDraftAsync(string vin, int year,
        IReadOnlyList<decimal> amounts, IReadOnlyList<string> otherDescriptions,
        DateTime asOf, CancellationToken cancellationToken)
    {
        if (year != asOf.Year) return new(false, "Only the current permit-year draft can be updated.");
        decimal total;
        try { total = VehicleFeeDraft.Total(amounts); }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        { return new(false, "Enter non-negative fee amounts with at most two decimals."); }
        if (otherDescriptions.Count != 4 || otherDescriptions.Any(x => x is not null && x.Length > 255))
            return new(false, "Enter four other-fee descriptions of at most 255 characters.");

        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var vehicle = new SqlCommand("""
                SELECT 1 FROM VehiclePermits WITH (UPDLOCK, HOLDLOCK)
                WHERE VIN=@vin AND IsArchived=0
                """, connection, transaction);
            vehicle.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            if (await vehicle.ExecuteScalarAsync(cancellationToken) is null)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Active vehicle not found."); }

            await using var priorCommand = new SqlCommand("""
                SELECT DetailsJson, GrandTotal FROM VehiclePermitFeeDrafts WITH (UPDLOCK, HOLDLOCK)
                WHERE VIN=@vin AND PermitYear=@year
                """, connection, transaction);
            priorCommand.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            priorCommand.Parameters.Add("@year", SqlDbType.Int).Value = year;
            DraftData? prior = null;
            await using (var reader = await priorCommand.ExecuteReaderAsync(cancellationToken))
                if (await reader.ReadAsync(cancellationToken))
                    prior = VehicleFeeDraft.Read(reader.GetString(0), reader.GetDecimal(1)).Data;
            var (json, _) = VehicleFeeDraft.Create(amounts, otherDescriptions, prior);
            await using var save = new SqlCommand("""
                UPDATE VehiclePermitFeeDrafts
                SET DetailsJson=@json, GrandTotal=@total, UpdatedAt=SYSUTCDATETIME()
                WHERE VIN=@vin AND PermitYear=@year;
                IF @@ROWCOUNT=0
                    INSERT INTO VehiclePermitFeeDrafts (VIN, PermitYear, DetailsJson, GrandTotal)
                    VALUES (@vin, @year, @json, @total);
                """, connection, transaction);
            save.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            save.Parameters.Add("@year", SqlDbType.Int).Value = year;
            save.Parameters.Add("@json", SqlDbType.NVarChar, -1).Value = json;
            var money = save.Parameters.Add("@total", SqlDbType.Decimal);
            money.Precision = 18; money.Scale = 2; money.Value = total;
            await save.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(true, null, total);
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<VehiclePaymentResult> PayAsync(string vin, string orNumber, int year,
        int userId, DateTime paidAt, CancellationToken cancellationToken)
    {
        orNumber = orNumber.Trim();
        if (string.IsNullOrWhiteSpace(vin)) return new(false, "Vehicle is required.");
        if (orNumber.Length is 0 or > 100) return new(false, "Enter a vehicle OR number up to 100 characters.");
        if (year != paidAt.Year) return new(false, "Only the current permit year can be paid.");
        if (userId <= 0) return new(false, "Sign in again before recording payment.");

        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var vehicle = new SqlCommand("""
                SELECT IsArchived, PermitStatus, PermitYear
                FROM VehiclePermits WITH (UPDLOCK, HOLDLOCK) WHERE VIN=@vin
                """, connection, transaction);
            vehicle.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            bool found, archived = false;
            string status = "";
            int storedYear = 0;
            await using (var reader = await vehicle.ExecuteReaderAsync(cancellationToken))
            {
                found = await reader.ReadAsync(cancellationToken);
                if (found)
                {
                    archived = !reader.IsDBNull(0) && reader.GetInt32(0) != 0;
                    status = reader.GetString(1);
                    storedYear = reader.GetInt32(2);
                }
            }
            if (!found || archived)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Active vehicle not found."); }
            if (status == "Paid" && storedYear == year)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "This permit year is already paid."); }

            await using var paidCheck = new SqlCommand("""
                SELECT 1 FROM VehiclePermitHistory WITH (UPDLOCK, HOLDLOCK)
                WHERE VIN=@vin AND PermitYear=@year
                """, connection, transaction);
            paidCheck.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            paidCheck.Parameters.Add("@year", SqlDbType.Int).Value = year;
            if (await paidCheck.ExecuteScalarAsync(cancellationToken) is not null)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "This permit year is already paid."); }

            await using var draftCommand = new SqlCommand("""
                SELECT DetailsJson, GrandTotal FROM VehiclePermitFeeDrafts WITH (UPDLOCK, HOLDLOCK)
                WHERE VIN=@vin AND PermitYear=@year
                """, connection, transaction);
            draftCommand.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            draftCommand.Parameters.Add("@year", SqlDbType.Int).Value = year;
            string? draftJson = null;
            decimal storedTotal = 0;
            await using (var reader = await draftCommand.ExecuteReaderAsync(cancellationToken))
                if (await reader.ReadAsync(cancellationToken))
                { draftJson = reader.GetString(0); storedTotal = reader.GetDecimal(1); }
            if (draftJson is null)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Save a fee draft before payment."); }
            decimal amount;
            try { amount = VehicleFeeDraft.Read(draftJson, storedTotal).GrandTotal; }
            catch (Exception exception) when (exception is ArgumentException or OverflowException or
                InvalidOperationException or System.Text.Json.JsonException)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Fee draft needs review before payment."); }
            if (amount <= 0)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Fee draft total must be greater than zero."); }

            await using var duplicate = new SqlCommand("""
                SELECT 1 FROM VehiclePermitHistory WITH (UPDLOCK, HOLDLOCK) WHERE ORNumber=@or
                """, connection, transaction);
            duplicate.Parameters.Add("@or", SqlDbType.NVarChar, 100).Value = orNumber;
            if (await duplicate.ExecuteScalarAsync(cancellationToken) is not null)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Vehicle OR number already exists."); }

            await using var history = new SqlCommand("""
                INSERT INTO VehiclePermitHistory (VIN, ORNumber, AmountPaid, PermitYear, DatePaid, RecordedBy)
                VALUES (@vin, @or, @amount, @year, @date, @user)
                """, connection, transaction);
            history.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            history.Parameters.Add("@or", SqlDbType.NVarChar, 100).Value = orNumber;
            var money = history.Parameters.Add("@amount", SqlDbType.Decimal);
            money.Precision = 18; money.Scale = 2; money.Value = amount;
            history.Parameters.Add("@year", SqlDbType.Int).Value = year;
            history.Parameters.Add("@date", SqlDbType.DateTime).Value = paidAt;
            history.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            if (await history.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("Vehicle payment-history insert failed.");

            await using var update = new SqlCommand("""
                UPDATE VehiclePermits SET PermitStatus='Paid', PermitYear=@year
                WHERE VIN=@vin AND IsArchived=0
                """, connection, transaction);
            update.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            update.Parameters.Add("@year", SqlDbType.Int).Value = year;
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("Vehicle permit status update failed.");

            await transaction.CommitAsync(cancellationToken);
            return new(true, null, orNumber, amount, year);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(cancellationToken);
            return exception.Message.Contains("UX_VehiclePermitHistory_VIN_PermitYear", StringComparison.Ordinal)
                ? new(false, "This permit year is already paid.")
                : new(false, "Vehicle OR number already exists.");
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Vehicle work requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Vehicle work requires BPLS_Dev.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static void Search(SqlCommand command, string term, string pattern)
    {
        command.Parameters.Add("@term", SqlDbType.NVarChar, 255).Value = term;
        command.Parameters.Add("@pattern", SqlDbType.NVarChar, 512).Value = pattern;
    }
}
