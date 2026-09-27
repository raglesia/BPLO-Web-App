using BusinessPermitLicensingSystem.Web.Billing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class PaymentScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

    public static async Task RunAsync()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var service = new BillingService(settings);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        Check((string?)await Scalar(connection, "SELECT DB_NAME()") == "BPLS_Dev", "payment tests use BPLS_Dev");
        var users = new List<int>();
        await using (var command = new SqlCommand("SELECT TOP (2) Id FROM Users ORDER BY Id", connection))
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) users.Add(reader.GetInt32(0));
        Check(users.Count == 2, "two development users available");

        string run = Guid.NewGuid().ToString("N")[..10];
        string normal = $"SIN-PAYTEST-{run}-1", single = $"SIN-PAYTEST-{run}-2";
        string duplicate = $"SIN-PAYTEST-{run}-3", concurrent = $"SIN-PAYTEST-{run}-4";
        string rollback = $"SIN-PAYTEST-{run}-5";
        foreach (var (sin, start) in new[]
        { (normal, "2026-01-15"), (single, "2026-03-15"), (duplicate, "2026-03-15"),
          (concurrent, "2026-01-15"), (rollback, "2026-03-15") })
            await InsertProfile(connection, sin, start);

        var paidAt = new DateTime(2026, 4, 21, 11, 30, 0);
        string normalOr = $"DEV-{run}-N", singleOr = $"DEV-{run}-S";
        var normalResult = await service.PayAsync(normal, normalOr, users[0], paidAt, default);
        Check(normalResult.Success && normalResult.Amount == 405m && normalResult.Bills == 3,
            "normal payment settles three periods with corrected total");
        Check(await Count(connection, "PaymentHistory", "SIN", normal) == 1 &&
              await LinkedCount(connection, normalOr) == 3 &&
              await PaidCount(connection, normal) == 3,
            "payment history, exact links, and three paid bills");
        Check(await Recorder(connection, normalOr) == users[0] &&
              await AuditRecorder(connection, normalOr) == users[0],
            "payment and audit record first acting user");
        var normalView = (await service.GetAsync(normal, paidAt, default))!;
        Check(normalView.Payments.Count == 1 && normalView.Payments[0].OrNumber == normalOr &&
              normalView.Payments[0].Periods.Contains("2026-02") && normalView.TotalDue == 0,
            "payment-history view lists linked periods and clears balance");

        var singleResult = await service.PayAsync(single, singleOr, users[1], paidAt, default);
        Check(singleResult.Success && singleResult.Amount == 135m && singleResult.Bills == 1,
            "100 base plus 10 additional plus 25 penalty equals 135");
        Check(await Recorder(connection, singleOr) == users[1] &&
              await AuditRecorder(connection, singleOr) == users[1],
            "payment and audit record second acting user");
        await using (var amount = new SqlCommand("""
            SELECT mb.MonthlyRental, mb.AdditionalCharge, mb.Penalty, mb.DatePaid, mb.RecordedBy, mb.WebRentBasis
            FROM MonthlyBilling mb WHERE mb.SIN=@sin
            """, connection))
        {
            amount.Parameters.AddWithValue("@sin", single);
            await using var row = await amount.ExecuteReaderAsync();
            Check(await row.ReadAsync() && row.GetDecimal(0) == 100m && row.GetDecimal(1) == 10m &&
                  row.GetDecimal(2) == 25m && row.GetDateTime(3) == paidAt &&
                  row.GetInt32(4) == users[1] && row.GetString(5) == "BaseOnly",
                "paid bill keeps corrected snapshot, penalty, date, and recorder");
        }

        await service.GenerateAsync(duplicate, paidAt, default);
        var duplicateResult = await service.PayAsync(duplicate, normalOr, users[0], paidAt, default);
        Check(!duplicateResult.Success && duplicateResult.Error == "OR number already exists." &&
              await PaidCount(connection, duplicate) == 0 &&
              await Count(connection, "PaymentHistory", "SIN", duplicate) == 0,
            "duplicate stall OR rejected without payment");
        var alreadyPaid = await service.PayAsync(single, $"DEV-{run}-again", users[0], paidAt, default);
        Check(!alreadyPaid.Success && await Count(connection, "PaymentHistory", "SIN", single) == 1,
            "already-paid bills cannot be paid again");

        var attempts = await Task.WhenAll(
            service.PayAsync(concurrent, $"DEV-{run}-C1", users[0], paidAt, default),
            service.PayAsync(concurrent, $"DEV-{run}-C2", users[1], paidAt, default));
        Check(attempts.Count(result => result.Success) == 1 &&
              await Count(connection, "PaymentHistory", "SIN", concurrent) == 1 &&
              await LinkedCountForSin(connection, concurrent) == 3 && await PaidCount(connection, concurrent) == 3,
            "concurrent payment has one winner and no duplicate bill links");

        await service.GenerateAsync(rollback, paidAt, default);
        const string trigger = "dbo.TR_BPLS_Phase6_TestRollback";
        await Execute(connection, $"DROP TRIGGER IF EXISTS {trigger}");
        try
        {
            await Execute(connection, $"""
                CREATE TRIGGER {trigger} ON dbo.PaymentHistoryBilling AFTER INSERT AS
                BEGIN
                    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.MonthlyBilling mb
                               ON mb.Id=i.MonthlyBillingId WHERE mb.SIN='{rollback}')
                        THROW 51000, 'Synthetic rollback test', 1;
                END
                """);
            bool failed = false;
            try { await service.PayAsync(rollback, $"DEV-{run}-R", users[0], paidAt, default); }
            catch (SqlException exception) when (exception.Number == 51000) { failed = true; }
            Check(failed && await Count(connection, "PaymentHistory", "SIN", rollback) == 0 &&
                  await LinkedCountForSin(connection, rollback) == 0 &&
                  await PaidCount(connection, rollback) == 0 &&
                  await AuditCount(connection, rollback) == 0,
                "failure after history insert rolls back history, links, bills, audit");
        }
        finally { await Execute(connection, $"DROP TRIGGER IF EXISTS {trigger}"); }

        Console.WriteLine($"Synthetic payment profiles retained in BPLS_Dev: {normal}, {single}, {duplicate}, {concurrent}, {rollback}");
    }

    private static async Task InsertProfile(SqlConnection connection, string sin, string start)
    {
        await using var command = new SqlCommand("""
            INSERT INTO Profiling(SIN,FullName,BusinessName,BusinessSection,StallNumber,StallSize,
                MonthlyRental,PaymentStatus,StartDate,AdditionalCharge,IsArchived)
            VALUES(@sin,@name,@business,'Public Market Stalls',@stall,'2',110,'Unpaid',@start,10,0)
            """, connection);
        command.Parameters.AddWithValue("@sin", sin);
        command.Parameters.AddWithValue("@name", "Synthetic Payment " + sin);
        command.Parameters.AddWithValue("@business", "Synthetic Payment Business " + sin);
        command.Parameters.AddWithValue("@stall", sin);
        command.Parameters.AddWithValue("@start", start);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> Scalar(SqlConnection connection, string sql)
    { await using var command = new SqlCommand(sql, connection); return await command.ExecuteScalarAsync(); }
    private static async Task Execute(SqlConnection connection, string sql)
    { await using var command = new SqlCommand(sql, connection); await command.ExecuteNonQueryAsync(); }
    private static async Task<int> Count(SqlConnection connection, string table, string column, string value)
    {
        await using var command = new SqlCommand($"SELECT COUNT(*) FROM {table} WHERE {column}=@value", connection);
        command.Parameters.AddWithValue("@value", value);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static async Task<int> LinkedCount(SqlConnection connection, string orNumber)
    {
        await using var command = new SqlCommand("""
            SELECT COUNT(*) FROM PaymentHistoryBilling l JOIN PaymentHistory p ON p.Id=l.PaymentHistoryId
            WHERE p.ORNumber=@or
            """, connection);
        command.Parameters.AddWithValue("@or", orNumber);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static async Task<int> LinkedCountForSin(SqlConnection connection, string sin)
    {
        await using var command = new SqlCommand("""
            SELECT COUNT(*) FROM PaymentHistoryBilling l JOIN MonthlyBilling b ON b.Id=l.MonthlyBillingId
            WHERE b.SIN=@sin
            """, connection);
        command.Parameters.AddWithValue("@sin", sin);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static Task<int> PaidCount(SqlConnection connection, string sin) =>
        CountPaid(connection, sin);
    private static async Task<int> CountPaid(SqlConnection connection, string sin)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) FROM MonthlyBilling WHERE SIN=@sin AND PaymentStatus='Paid'", connection);
        command.Parameters.AddWithValue("@sin", sin);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static async Task<int> Recorder(SqlConnection connection, string orNumber)
    {
        await using var command = new SqlCommand("SELECT RecordedBy FROM PaymentHistory WHERE ORNumber=@or", connection);
        command.Parameters.AddWithValue("@or", orNumber);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static async Task<int> AuditRecorder(SqlConnection connection, string orNumber)
    {
        await using var command = new SqlCommand("SELECT TOP(1) UserId FROM AuditTrail WHERE Details LIKE @pattern ORDER BY Id DESC", connection);
        command.Parameters.AddWithValue("@pattern", "%OR#: " + orNumber + "%");
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static Task<int> AuditCount(SqlConnection connection, string sin) => Count(connection, "AuditTrail", "SIN", sin);
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception(name); Console.WriteLine($"PASS {name}"); }
}
