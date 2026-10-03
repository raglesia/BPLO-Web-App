using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Profiles;

public sealed class ProfileService(IConfiguration configuration)
{
    public const int PageSize = 25;
    private const decimal MaximumMoney = 9999999999999999.99m;
    private const string Columns = "SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize, MonthlyRental, PaymentStatus, StartDate, Penalty, AdditionalCharge, IsLegacyBaseline";

    public async Task<ProfileListResult> ListAsync(string? search, int page, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        string term = (search ?? "").Trim();
        string where = """
            IsArchived = 0 AND (
                @term = '' OR SIN LIKE @pattern OR FullName LIKE @pattern OR
                BusinessName LIKE @pattern OR BusinessSection LIKE @pattern OR
                StallNumber LIKE @pattern OR StallSize LIKE @pattern OR
                CONVERT(NVARCHAR(50), MonthlyRental) LIKE @pattern OR
                PaymentStatus = @term OR CONVERT(NVARCHAR(50), Penalty) LIKE @pattern OR
                CONVERT(NVARCHAR(10), TRY_CONVERT(date, StartDate, 23), 101) LIKE @pattern)
            """;
        string pattern = "%" + EscapeLike(term) + "%";

        await using var countCommand = new SqlCommand($"SELECT COUNT(*) FROM Profiling WHERE {where}", connection);
        AddSearchParameters(countCommand, term, pattern);
        int total = (int)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0);
        int currentPage = Math.Clamp(page, 1, Math.Max(1, (total + PageSize - 1) / PageSize));

        await using var listCommand = new SqlCommand($"""
            SELECT {Columns} FROM Profiling WHERE {where}
            ORDER BY SIN ASC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """, connection);
        AddSearchParameters(listCommand, term, pattern);
        listCommand.Parameters.Add("@skip", SqlDbType.Int).Value = (currentPage - 1) * PageSize;
        listCommand.Parameters.Add("@take", SqlDbType.Int).Value = PageSize;
        var profiles = new List<ProfileRecord>();
        await using var reader = await listCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) profiles.Add(ReadProfile(reader));
        return new ProfileListResult(profiles, total);
    }

    public async Task<ProfileRecord?> GetAsync(string sin, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await GetAsync(connection, null, sin, cancellationToken);
    }

    public async Task<string?> GetImportReviewAsync(string sin, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        await using var command = new SqlCommand("SELECT TOP (1) Details FROM AuditTrail WHERE SIN=@sin AND Action='Import Review' ORDER BY Id DESC", connection);
        command.Parameters.AddWithValue("@sin", sin);
        return await command.ExecuteScalarAsync(token) as string;
    }

    public async Task<IReadOnlyList<RentalRate>> GetRatesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(
            "SELECT Section, RatePerSqm, FlatRate, RateType FROM RentalRates ORDER BY Section", connection);
        var rates = new List<RentalRate>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rates.Add(new RentalRate(reader.GetString(0), reader.GetDecimal(1), reader.GetDecimal(2), reader.GetString(3)));
        return rates;
    }

    public async Task<ProfileSaveResult> CreateAsync(ProfileInput input, int userId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var (data, error) = await ValidateAsync(connection, transaction, input, null, cancellationToken);
            if (error is not null) return error;

            string year = DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
            await using var numberCommand = new SqlCommand("""
                SELECT MAX(CAST(RIGHT(SIN, 4) AS INT))
                FROM Profiling WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN LIKE @pattern
                """, connection, transaction);
            numberCommand.Parameters.Add("@pattern", SqlDbType.NVarChar, 50).Value = $"SIN-{year}-%";
            object? maximum = await numberCommand.ExecuteScalarAsync(cancellationToken);
            int nextNumber = maximum is null or DBNull ? 1 : Convert.ToInt32(maximum) + 1;
            if (nextNumber > 9999) return new(null, "No SIN numbers remain for this year.");
            string sin = $"SIN-{year}-{nextNumber:D4}";

            await using var insert = new SqlCommand("""
                INSERT INTO Profiling
                    (SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize,
                     MonthlyRental, PaymentStatus, StartDate, AdditionalCharge, IsLegacyBaseline)
                VALUES
                    (@sin, @name, @business, @section, @stall, @size,
                     @rental, @status, @start, @additional, 0)
                """, connection, transaction);
            AddProfileParameters(insert, sin, data!);
            await insert.ExecuteNonQueryAsync(cancellationToken);
            await InsertAuditAsync(connection, transaction, "Add", sin, userId,
                $"Added profile for {data!.FullName}, Status: {data.PaymentStatus}", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(sin);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return new(null, "This profile already exists (duplicate).");
        }
    }

    public async Task<ProfileSaveResult> UpdateAsync(string sin, ProfileInput input, int userId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            ProfileRecord? existing = await GetAsync(connection, transaction, sin, cancellationToken);
            if (existing is null) return new(null, "Profile not found.");
            var (data, error) = await ValidateAsync(connection, transaction, input, existing, cancellationToken);
            if (error is not null) return error;

            await using var update = new SqlCommand("""
                UPDATE Profiling SET
                    FullName = @name, BusinessName = @business, BusinessSection = @section,
                    StallNumber = @stall, StallSize = @size, MonthlyRental = @rental,
                    PaymentStatus = @status,
                    StartDate = CASE WHEN IsLegacyBaseline=1 THEN StartDate ELSE @start END,
                    AdditionalCharge = @additional,
                    Penalty = CASE WHEN @status = 'Unverified' THEN 0 ELSE Penalty END
                WHERE SIN = @sin AND IsArchived = 0
                """, connection, transaction);
            AddProfileParameters(update, sin, data!);
            if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
                return new(null, "Profile is no longer available.");
            await InsertAuditAsync(connection, transaction, "Update", sin, userId,
                $"Updated profile for {data!.FullName}, Status: {data.PaymentStatus}", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(sin);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return new(null, "This profile already exists (duplicate).");
        }
    }

    private async Task<(ValidatedProfile? Data, ProfileSaveResult? Error)> ValidateAsync(
        SqlConnection connection, SqlTransaction transaction, ProfileInput input,
        ProfileRecord? existing, CancellationToken cancellationToken)
    {
        string name = input.FullName.Trim();
        string business = input.BusinessName.Trim();
        string section = input.BusinessSection.Trim();
        string stall = input.StallNumber.Trim();
        if (name.Length == 0 || !Regex.IsMatch(name, @"^[\p{L}. ]+$"))
            return (null, new(null, "Enter a full name using letters, spaces, or periods.", "Input.FullName"));
        if (business.Length == 0)
            return (null, new(null, "Business name is required.", "Input.BusinessName"));
        if (stall.Length == 0 || !Regex.IsMatch(stall, @"^[A-Za-z0-9][A-Za-z0-9 ,&/\-]*$"))
            return (null, new(null, "Stall number may contain letters, digits, spaces, commas, hyphens, slashes, or ampersands.", "Input.StallNumber"));

        bool existingPaid = existing?.PaymentStatus == "Paid";
        if (existingPaid ? input.PaymentStatus != "Paid" : input.PaymentStatus is not ("Unverified" or "Unpaid"))
            return (null, new(null, "Paid status is handled in the payment phase.", "Input.PaymentStatus"));
        if (existing?.IsLegacyBaseline == true &&
            (input.PaymentStatus != existing.PaymentStatus ||
             input.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != existing.StartDate))
            return (null, new(null, "Legacy baseline date and status cannot be changed in profile editing.", "Input.StartDate"));
        if (input.PaymentStatus != "Unverified" && !input.StartDate.HasValue)
            return (null, new(null, "Verify the Date of Occupancy before marking this owner Paid or Unpaid.", "Input.StartDate"));

        await using var rateCommand = new SqlCommand(
            "SELECT RatePerSqm, FlatRate, RateType FROM RentalRates WHERE Section = @section", connection, transaction);
        rateCommand.Parameters.Add("@section", SqlDbType.NVarChar, 255).Value = section;
        await using var rateReader = await rateCommand.ExecuteReaderAsync(cancellationToken);
        if (!await rateReader.ReadAsync(cancellationToken))
            return (null, new(null, "Choose an existing business section.", "Input.BusinessSection"));
        decimal ratePerSqm = rateReader.GetDecimal(0);
        decimal flatRate = rateReader.GetDecimal(1);
        string rateType = rateReader.GetString(2);
        await rateReader.CloseAsync();

        string size = input.StallSize.Trim();
        decimal baseRental;
        if (rateType == "Flat")
        {
            size = "0";
            baseRental = flatRate;
        }
        else if (!decimal.TryParse(size, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsedSize)
                 || parsedSize <= 0)
        {
            return (null, new(null, "Stall size must be a positive number.", "Input.StallSize"));
        }
        else
        {
            try { baseRental = checked(parsedSize * ratePerSqm); }
            catch (OverflowException) { return (null, new(null, "Calculated rental is too large.", "Input.StallSize")); }
        }

        decimal additional = input.IncludeAdditionalCharge ? decimal.Round(input.AdditionalCharge, 2) : 0;
        if (additional < 0)
            return (null, new(null, "Additional charge cannot be negative.", "Input.AdditionalCharge"));
        decimal rental;
        try { rental = decimal.Round(checked(baseRental + additional), 2); }
        catch (OverflowException) { return (null, new(null, "Calculated rental is too large.")); }
        if (rental > MaximumMoney || additional > MaximumMoney)
            return (null, new(null, "Calculated rental is too large."));

        // WinForms keeps the loaded rental when only unrelated fields change.
        if (existing is not null && existing.BusinessSection == section && existing.StallSize == size
            && existing.AdditionalCharge == additional)
            rental = existing.MonthlyRental;

        return (new(name, business, section, stall, size, rental, input.PaymentStatus,
            input.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "", additional), null);
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        string connectionString = configuration.GetConnectionString("BPLS")
            ?? throw new InvalidOperationException("BPLS connection is not configured.");
        if (!string.Equals(new SqlConnectionStringBuilder(connectionString).InitialCatalog,
                "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Profile work requires BPLS_Dev.");
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Profile work requires BPLS_Dev.");
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task<ProfileRecord?> GetAsync(
        SqlConnection connection, SqlTransaction? transaction, string sin, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(
            $"SELECT {Columns} FROM Profiling WHERE SIN = @sin AND IsArchived = 0", connection, transaction);
        command.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProfile(reader) : null;
    }

    private static ProfileRecord ReadProfile(SqlDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
        reader.GetString(4), reader.GetString(5), reader.GetDecimal(6), reader.GetString(7),
        reader.IsDBNull(8) ? "" : reader.GetString(8),
        reader.IsDBNull(9) ? 0 : reader.GetDecimal(9),
        reader.IsDBNull(10) ? 0 : reader.GetDecimal(10))
        { IsLegacyBaseline = reader.GetBoolean(11) };

    private static void AddProfileParameters(SqlCommand command, string sin, ValidatedProfile data)
    {
        command.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        command.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = data.FullName;
        command.Parameters.Add("@business", SqlDbType.NVarChar, 255).Value = data.BusinessName;
        command.Parameters.Add("@section", SqlDbType.NVarChar, 255).Value = data.BusinessSection;
        command.Parameters.Add("@stall", SqlDbType.NVarChar, 100).Value = data.StallNumber;
        command.Parameters.Add("@size", SqlDbType.NVarChar, 100).Value = data.StallSize;
        AddMoney(command, "@rental", data.MonthlyRental);
        command.Parameters.Add("@status", SqlDbType.NVarChar, 50).Value = data.PaymentStatus;
        command.Parameters.Add("@start", SqlDbType.NVarChar, 50).Value = data.StartDate;
        AddMoney(command, "@additional", data.AdditionalCharge);
    }

    private static void AddMoney(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value;
    }

    private static async Task InsertAuditAsync(SqlConnection connection, SqlTransaction transaction,
        string action, string sin, int userId, string details, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            INSERT INTO AuditTrail (Action, SIN, UserId, Timestamp, Details)
            VALUES (@action, @sin, @user, GETDATE(), @details)
            """, connection, transaction);
        command.Parameters.Add("@action", SqlDbType.NVarChar, 255).Value = action;
        command.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        command.Parameters.Add("@user", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value = details;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddSearchParameters(SqlCommand command, string term, string pattern)
    {
        command.Parameters.Add("@term", SqlDbType.NVarChar, 255).Value = term;
        command.Parameters.Add("@pattern", SqlDbType.NVarChar, 512).Value = pattern;
    }

    private static string EscapeLike(string value) => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

    private sealed record ValidatedProfile(string FullName, string BusinessName, string BusinessSection,
        string StallNumber, string StallSize, decimal MonthlyRental, string PaymentStatus,
        string StartDate, decimal AdditionalCharge);
}
