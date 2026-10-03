using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class LegacyArrearsScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

    public static async Task RunAsync()
    {
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var billing = new BillingService(settings);
        var reports = new ReportService(settings);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        Check((string?)await Scalar(connection, "SELECT DB_NAME()") == "BPLS_Dev", "arrears tests use BPLS_Dev");
        int user = (int)(await Scalar(connection, "SELECT TOP(1) Id FROM Users ORDER BY Id") ?? 0);
        Check(user > 0, "arrears verifier exists");
        string run = Guid.NewGuid().ToString("N")[..10];
        string legacy = $"SIN-ARREARS-{run}-L", normal = $"SIN-ARREARS-{run}-N";
        string rollback = $"SIN-ARREARS-{run}-R";
        foreach (var (sin, isLegacy, start) in new[]
        { (legacy, true, "2026-12-01"), (normal, false, "2026-12-01"), (rollback, true, "2026-12-01") })
        {
            await using var insert = new SqlCommand("""
                INSERT INTO Profiling (SIN, FullName, BusinessName, BusinessSection, StallNumber,
                    StallSize, MonthlyRental, PaymentStatus, StartDate, AdditionalCharge, IsArchived, IsLegacyBaseline)
                VALUES (@sin, @sin, @sin, 'Corridor', @sin, '0', 1100, 'Unpaid', @start, 100, 0, @legacy)
                """, connection);
            insert.Parameters.AddWithValue("@sin", sin);
            insert.Parameters.AddWithValue("@start", start);
            insert.Parameters.AddWithValue("@legacy", isLegacy);
            await insert.ExecuteNonQueryAsync();
        }
        var december = new DateTime(2026, 12, 1);
        var overdue = new DateTime(2026, 12, 22);
        Check(BillingRules.MissingPeriods(december, december, [], true).SequenceEqual([(2026, 12)]),
            "legacy December starts immediately");
        Check(BillingRules.MissingPeriods(december, december, [], false).Count == 0,
            "new profile excludes occupancy month");
        Check(await billing.GenerateAsync(legacy, new DateTime(2026, 10, 4), default) == 1 &&
              await billing.GenerateAsync(normal, december, default) == 0,
            "only marked legacy profile generates December bill");
        var initial = (await billing.GetAsync(legacy, december, default))!;
        Check(initial.Rows.Count == 1 && initial.Rows[0].Status == "Unpaid" &&
              initial.Rows[0].Month == 12 && initial.Arrears.Count == 0 &&
              initial.TotalDue == 1100m && initial.Payments.Count == 0,
            "December initial report has unpaid rent and no OR");
        var firstReport = (await reports.ProfileAsync(legacy, default))!;
        Check(firstReport.IsLegacyBaseline && firstReport.AssessmentBills(december).Count == 1 &&
              firstReport.Arrears.Count == 0, "initial rental report reads one December bill");
        var october = await billing.SaveVerifiedArrearsAsync(legacy, null, 2026, 10,
            1000m, 100m, "Logbook October", user, overdue, default);
        var november = await billing.SaveVerifiedArrearsAsync(legacy, null, 2026, 11,
            1000m, 100m, "Logbook November", user, overdue, default);
        Check(october.Success && november.Success, "Treasury enters two prior periods");
        Check(!(await billing.SaveVerifiedArrearsAsync(legacy, null, 2026, 10,
            1000m, 100m, "Duplicate", user, overdue, default)).Success,
            "duplicate arrears period rejected");
        var updated = (await billing.GetAsync(legacy, overdue, default))!;
        Check(updated.Arrears.Count == 2 && updated.Arrears.All(x => x.CurrentPenalty(overdue) == 250m &&
              x.Total(overdue) == 1350m) && updated.TotalDue == 4050m && updated.Payments.Count == 0,
            "arrears penalty applies once to base rent; no payment on entry");
        var refreshedReport = (await reports.ProfileAsync(legacy, default))!;
        Check(refreshedReport.Arrears.Count == 2 && refreshedReport.Arrears.All(x => !x.IsPaid),
            "reprinted rental report reads current saved arrears");
        await using (var collision = new SqlCommand("""
            INSERT INTO MonthlyBilling (SIN, BillingYear, BillingMonth, MonthlyRental,
                AdditionalCharge, Penalty, PaymentStatus, WebRentBasis)
            VALUES (@sin, 2026, 9, 1000, 0, 0, 'Unpaid', 'BaseOnly')
            """, connection))
        { collision.Parameters.AddWithValue("@sin", legacy); await collision.ExecuteNonQueryAsync(); }
        var collisionResult = await billing.SaveVerifiedArrearsAsync(legacy, null, 2026, 9,
            1000m, 0, "Collision", user, overdue, default);
        Check(!collisionResult.Success && collisionResult.Error!.Contains("regular billing"),
            "regular bill collision rejected clearly");
        int octoberId = updated.Arrears.Single(x => x.Month == 10).Id;
        int novemberId = updated.Arrears.Single(x => x.Month == 11).Id;
        Check(!(await billing.SaveVerifiedArrearsAsync(legacy, novemberId, 2026, 10,
            1000m, 100m, "Collision correction", user, overdue, default)).Success,
            "arrears period correction rejects another existing period");
        Check((await billing.SaveVerifiedArrearsAsync(legacy, novemberId, 2026, 8,
            1000m, 100m, "Corrected period", user, overdue, default)).Success &&
              (await billing.SaveVerifiedArrearsAsync(legacy, novemberId, 2026, 11,
                  1000m, 100m, "Restored period", user, overdue, default)).Success,
            "unpaid arrears period can be corrected without adding a second row");
        Check((await billing.SaveVerifiedArrearsAsync(legacy, octoberId, 2026, 10,
            1000m, 100m, "Corrected reference", user, overdue, default)).Success,
            "unpaid arrears correction saved");
        string orNumber = $"ARREARS-{run}-OR";
        var paid = await billing.PayAsync(legacy, orNumber, user, overdue, default);
        Check(paid.Success && paid.Bills == 4 && paid.Amount == 5300m,
            "one OR settles two arrears and two regular bills");
        Check((int)(await Scalar(connection, $"SELECT COUNT(*) FROM PaymentHistoryArrears WHERE PaymentHistoryId=(SELECT Id FROM PaymentHistory WHERE ORNumber='{orNumber}')") ?? 0) == 2 &&
              (int)(await Scalar(connection, $"SELECT COUNT(*) FROM PaymentHistoryBilling WHERE PaymentHistoryId=(SELECT Id FROM PaymentHistory WHERE ORNumber='{orNumber}')") ?? 0) == 2 &&
              (await billing.GetAsync(legacy, overdue, default))!.Arrears.All(x => x.IsPaid),
            "payment links both obligation types and marks arrears paid");
        Check(!(await billing.SaveVerifiedArrearsAsync(legacy, octoberId, 2026, 10,
            2000m, 0, "After payment", user, overdue, default)).Success,
            "paid arrears cannot be edited");
        Check(!(await billing.PayAsync(legacy, orNumber, user, overdue, default)).Success,
            "duplicate or repeated payment rejected");
        Check(await billing.GenerateAsync(rollback, december, default) == 1 &&
              (await billing.SaveVerifiedArrearsAsync(rollback, null, 2026, 10,
                  1000m, 100m, "Rollback", user, overdue, default)).Success,
            "rollback fixture ready");
        const string trigger = "dbo.TR_BPLS_Arrears_TestRollback";
        await Execute(connection, $"DROP TRIGGER IF EXISTS {trigger}");
        try
        {
            await Execute(connection, $"""
                CREATE TRIGGER {trigger} ON dbo.PaymentHistoryArrears AFTER INSERT AS
                BEGIN
                  IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.StallOwnerArrears a
                      ON a.Id=i.StallOwnerArrearsId WHERE a.SIN='{rollback}')
                    THROW 51000, 'Synthetic arrears rollback', 1;
                END
                """);
            bool failed = false;
            try { await billing.PayAsync(rollback, $"ARREARS-{run}-FAIL", user, overdue, default); }
            catch (SqlException exception) when (exception.Number == 51000) { failed = true; }
            Check(failed && (int)(await Scalar(connection,
                $"SELECT COUNT(*) FROM PaymentHistory WHERE SIN='{rollback}'") ?? 0) == 0 &&
                (await billing.GetAsync(rollback, overdue, default))!.Arrears.All(x => !x.IsPaid),
                "arrears link failure rolls back OR and payment states");
        }
        finally { await Execute(connection, $"DROP TRIGGER IF EXISTS {trigger}"); }
        Console.WriteLine($"Synthetic arrears profiles retained in BPLS_Dev: {legacy}, {normal}, {rollback}");
    }

    private static async Task<object?> Scalar(SqlConnection connection, string sql)
    { await using var command = new SqlCommand(sql, connection); return await command.ExecuteScalarAsync(); }
    private static async Task Execute(SqlConnection connection, string sql)
    { await using var command = new SqlCommand(sql, connection); await command.ExecuteNonQueryAsync(); }
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
}
