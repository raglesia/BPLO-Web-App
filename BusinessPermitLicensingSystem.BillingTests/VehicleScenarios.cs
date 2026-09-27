using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class VehicleScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private static readonly DateTime PaidAt = new(2026, 9, 26, 10, 30, 0);

    public static async Task RunAsync()
    {
        var amounts = Enumerable.Repeat(0m, VehicleFeeDraft.FeeNames.Length).ToArray();
        amounts[0] = 100m; amounts[1] = 10m;
        Check(VehicleFeeDraft.Total(amounts) == 110m, "draft total sums desktop fee items");
        var (json, total) = VehicleFeeDraft.Create(amounts, ["one", "", "", ""]);
        var parsed = VehicleFeeDraft.Read(json, total);
        Check(parsed.GrandTotal == 110m && parsed.Data.OtherDescriptions[0] == "one" &&
              parsed.Amounts[0] == 100m, "desktop draft JSON round trip");
        var bad = amounts.ToArray(); bad[0] = -1m;
        Check(Rejects(() => VehicleFeeDraft.Total(bad)), "negative fee rejected");
        bad[0] = 1.001m;
        Check(Rejects(() => VehicleFeeDraft.Total(bad)), "sub-cent fee rejected");
        Check(Rejects(() => VehicleFeeDraft.Read(json, 111m)), "draft total mismatch rejected");

        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var service = new VehicleService(settings);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        Check((string?)await Scalar(connection, "SELECT DB_NAME()") == "BPLS_Dev", "vehicle tests use BPLS_Dev");
        Check((int)(await Scalar(connection, "SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.VehiclePermitHistory') AND name='UX_VehiclePermitHistory_VIN_PermitYear' AND is_unique=1") ?? 0) == 1,
            "unique VIN and permit-year index exists");
        var users = new List<int>();
        await using (var command = new SqlCommand("SELECT TOP (2) Id FROM Users ORDER BY Id", connection))
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) users.Add(reader.GetInt32(0));
        Check(users.Count == 2, "two vehicle payment test users available");
        int stallPaymentsBefore = (int)(await Scalar(connection, "SELECT COUNT(*) FROM PaymentHistory") ?? 0);
        int stallLinksBefore = (int)(await Scalar(connection, "SELECT COUNT(*) FROM PaymentHistoryBilling") ?? 0);
        string run = Guid.NewGuid().ToString("N")[..10];
        string first = $"VIN-PAYTEST-{run}-1", second = $"VIN-PAYTEST-{run}-2";
        string concurrent = $"VIN-PAYTEST-{run}-3", rollback = $"VIN-PAYTEST-{run}-4";
        foreach (string vin in new[] { first, second, concurrent, rollback }) await InsertVehicle(connection, vin);
        foreach (string vin in new[] { first, second, concurrent, rollback })
        {
            var save = await service.SaveDraftAsync(vin, 2026, amounts, ["one", "", "", ""], PaidAt, default);
            Check(save.Success && save.Total == 110m && await CountHistory(connection, vin) == 0,
                "saving draft does not create payment for " + vin);
        }
        Check((await service.GetAsync(first, PaidAt, default)) is { Eligible: true, SelectedYearStatus: "Unpaid", Draft.GrandTotal: 110m },
            "current year eligibility and stored draft display");
        string orFirst = $"DEV-VEH-{run}-1";
        amounts[0] = 999m; // Browser-side amount changes after draft save cannot affect payment.
        var payment = await service.PayAsync(first, orFirst, 2026, users[0], PaidAt, default);
        Check(payment.Success && payment.Amount == 110m && payment.Year == 2026,
            "payment re-reads saved draft amount, ignores changed client amount");
        Check(await CountHistory(connection, first) == 1 && await VehicleStatus(connection, first) == ("Paid", 2026) &&
              await Recorder(connection, orFirst) == users[0] && await HistoryAmount(connection, orFirst) == 110m,
            "history, vehicle status/year, and first recorder stored atomically");
        Check(await CountAudit(connection, first) == 0, "desktop vehicle payment creates no audit row");
        amounts[0] = 200m;
        Check((await service.SaveDraftAsync(first, 2026, amounts, ["changed", "", "", ""], PaidAt, default)).Success &&
              await HistoryAmount(connection, orFirst) == 110m,
            "later draft edit does not rewrite historical payment amount");
        amounts[0] = 100m;

        var duplicateOr = await service.PayAsync(second, orFirst, 2026, users[1], PaidAt, default);
        Check(!duplicateOr.Success && duplicateOr.Error == "Vehicle OR number already exists." &&
              await CountHistory(connection, second) == 0 && await VehicleStatus(connection, second) == ("Unpaid", 0),
            "duplicate vehicle OR rejected without status change");
        string orSecond = $"DEV-VEH-{run}-2";
        Check((await service.PayAsync(second, orSecond, 2026, users[1], PaidAt, default)).Success &&
              await Recorder(connection, orSecond) == users[1],
            "second authenticated user recorded separately");
        var alreadyPaid = await service.PayAsync(first, $"DEV-VEH-{run}-again", 2026, users[0], PaidAt, default);
        Check(!alreadyPaid.Success && await CountHistory(connection, first) == 1,
            "already-paid VIN/year cannot be paid again");
        var wrongYear = await service.PayAsync(second, $"DEV-VEH-{run}-past", 2025, users[0], PaidAt, default);
        Check(!wrongYear.Success && await CountHistory(connection, second) == 1,
            "requested permit year checked server-side");

        var attempts = await Task.WhenAll(
            service.PayAsync(concurrent, $"DEV-VEH-{run}-C1", 2026, users[0], PaidAt, default),
            service.PayAsync(concurrent, $"DEV-VEH-{run}-C2", 2026, users[1], PaidAt, default));
        Check(attempts.Count(x => x.Success) == 1 && await CountHistory(connection, concurrent) == 1 &&
              await VehicleStatus(connection, concurrent) == ("Paid", 2026),
            "concurrent same VIN/year produces exactly one payment");

        const string trigger = "dbo.TR_BPLS_Phase7_TestRollback";
        await Execute(connection, $"DROP TRIGGER IF EXISTS {trigger}");
        try
        {
            await Execute(connection, $"""
                CREATE TRIGGER {trigger} ON dbo.VehiclePermitHistory AFTER INSERT AS
                BEGIN
                    IF EXISTS (SELECT 1 FROM inserted WHERE VIN='{rollback}')
                        THROW 51000, 'Synthetic vehicle rollback test', 1;
                END
                """);
            bool failed = false;
            try { await service.PayAsync(rollback, $"DEV-VEH-{run}-R", 2026, users[0], PaidAt, default); }
            catch (SqlException exception) when (exception.Number == 51000) { failed = true; }
            Check(failed && await CountHistory(connection, rollback) == 0 &&
                  await VehicleStatus(connection, rollback) == ("Unpaid", 0) &&
                  await CountAudit(connection, rollback) == 0,
                "intermediate failure rolls back history and vehicle state");
        }
        finally { await Execute(connection, $"DROP TRIGGER IF EXISTS {trigger}"); }

        await Execute(connection, $"UPDATE VehiclePermits SET IsArchived=1 WHERE VIN='{rollback}'");
        var archivedPay = await service.PayAsync(rollback, $"DEV-VEH-{run}-archived", 2026, users[0], PaidAt, default);
        Check(!archivedPay.Success && await CountHistory(connection, rollback) == 0 &&
              await VehicleStatus(connection, rollback) == ("Unpaid", 0),
            "archived vehicle cannot receive annual payment");

        var nextYear = new DateTime(2027, 1, 2, 9, 0, 0);
        var beforeRenewal = (await service.GetAsync(first, nextYear, default))!;
        Check(beforeRenewal.Eligible && beforeRenewal.SelectedYearStatus == "Unpaid" &&
              await VehicleStatus(connection, first) == ("Paid", 2026),
            "new year eligible without page-load reset or database mutation");
        Check((await service.SaveDraftAsync(first, 2027, amounts, ["", "", "", ""], nextYear, default)).Success,
            "new year has independent fee draft");
        Check((await service.PayAsync(first, $"DEV-VEH-{run}-2027", 2027, users[0], nextYear, default)).Success &&
              await CountHistory(connection, first) == 2 && await VehicleStatus(connection, first) == ("Paid", 2027),
            "same VIN can pay later permit year once");
        var listed = (await service.ListAsync(first, 1, default)).Items.Single(x => x.Vin == first);
        string expectedListStatus = DateTime.Today.Year is 2026 or 2027 ? "Paid" : "Unpaid";
        Check(listed.CurrentYearStatus == expectedListStatus,
            "vehicle list shows current-year status from annual history, not only last stored year");
        Check((int)(await Scalar(connection, "SELECT COUNT(*) FROM PaymentHistory") ?? 0) == stallPaymentsBefore &&
              (int)(await Scalar(connection, "SELECT COUNT(*) FROM PaymentHistoryBilling") ?? 0) == stallLinksBefore,
            "stall payment tables unchanged by vehicle tests");
        Console.WriteLine($"Synthetic vehicle profiles retained in BPLS_Dev: {first}, {second}, {concurrent}, {rollback}");
    }

    private static bool Rejects(Action action)
    { try { action(); return false; } catch (ArgumentException) { return true; }
      catch (InvalidOperationException) { return true; } }
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception(name); Console.WriteLine($"PASS {name}"); }
    private static async Task<object?> Scalar(SqlConnection connection, string sql)
    { await using var command = new SqlCommand(sql, connection); return await command.ExecuteScalarAsync(); }
    private static async Task Execute(SqlConnection connection, string sql)
    { await using var command = new SqlCommand(sql, connection); await command.ExecuteNonQueryAsync(); }
    private static async Task InsertVehicle(SqlConnection connection, string vin)
    {
        await using var command = new SqlCommand("""
            INSERT INTO VehiclePermits(VIN, CompanyName, DriverName, PlateNo, PermitStatus, PermitYear, IsArchived)
            VALUES(@vin,@company,@driver,@plate,'Unpaid',0,0)
            """, connection);
        command.Parameters.AddWithValue("@vin", vin);
        command.Parameters.AddWithValue("@company", "Synthetic Vehicle " + vin);
        command.Parameters.AddWithValue("@driver", "Synthetic Driver");
        command.Parameters.AddWithValue("@plate", "PL-" + vin);
        await command.ExecuteNonQueryAsync();
    }
    private static async Task<int> CountHistory(SqlConnection connection, string vin)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin", connection);
        command.Parameters.AddWithValue("@vin", vin);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static async Task<(string, int)> VehicleStatus(SqlConnection connection, string vin)
    {
        await using var command = new SqlCommand("SELECT PermitStatus, PermitYear FROM VehiclePermits WHERE VIN=@vin", connection);
        command.Parameters.AddWithValue("@vin", vin);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw new Exception("Vehicle missing");
        return (reader.GetString(0), reader.GetInt32(1));
    }
    private static async Task<int> Recorder(SqlConnection connection, string orNumber)
    {
        await using var command = new SqlCommand("SELECT RecordedBy FROM VehiclePermitHistory WHERE ORNumber=@or", connection);
        command.Parameters.AddWithValue("@or", orNumber);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    private static async Task<decimal> HistoryAmount(SqlConnection connection, string orNumber)
    {
        await using var command = new SqlCommand("SELECT AmountPaid FROM VehiclePermitHistory WHERE ORNumber=@or", connection);
        command.Parameters.AddWithValue("@or", orNumber);
        return (decimal)(await command.ExecuteScalarAsync() ?? 0m);
    }
    private static async Task<int> CountAudit(SqlConnection connection, string vin)
    {
        await using var command = new SqlCommand("SELECT COUNT(*) FROM AuditTrail WHERE SIN=@vin", connection);
        command.Parameters.AddWithValue("@vin", vin);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
}
