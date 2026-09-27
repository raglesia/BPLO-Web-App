using BusinessPermitLicensingSystem.Web.Archive;
using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class ArchiveScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private static void Check(bool yes, string label) { if (!yes) throw new Exception(label); Console.WriteLine("PASS " + label); }
    public static async Task RunAsync()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var archive = new ArchiveService(configuration);
        var profiles = new ProfileService(configuration);
        var vehicles = new VehicleService(configuration);
        var billing = new BillingService(configuration);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        Check((string?)await Scalar("SELECT DB_NAME()") == "BPLS_Dev", "archive tests use BPLS_Dev");
        int user = (int)(await Scalar("SELECT TOP (1) Id FROM Users ORDER BY Id") ?? throw new Exception("No test user"));
        string run = Guid.NewGuid().ToString("N")[..10];
        string sin = "SIN-ARCHTEST-" + run, vin = "VIN-ARCHTEST-" + run;
        await Execute("""
            INSERT INTO Profiling (SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize,
                MonthlyRental, PaymentStatus, StartDate, AdditionalCharge, IsArchived)
            VALUES (@sin, 'Synthetic Archive Owner', 'Synthetic Archive Business', 'Public Market Stalls',
                @sin, '2', 100, 'Paid', '2026-01-01', 0, 0)
            """, ("@sin", sin));
        await Execute("""
            INSERT INTO MonthlyBilling (SIN, BillingYear, BillingMonth, MonthlyRental, AdditionalCharge, Penalty, PaymentStatus, WebRentBasis)
            VALUES (@sin, 2026, 2, 100, 0, 0, 'Paid', 'BaseOnly')
            """, ("@sin", sin));
        await Execute("""
            INSERT INTO PaymentHistory (SIN, ORNumber, AmountPaid, DatePaid, RecordedBy)
            VALUES (@sin, @or, 100, GETDATE(), @user)
            """, ("@sin", sin), ("@or", "DEV-ARCH-" + run), ("@user", user));
        await Execute("""
            INSERT INTO PaymentHistoryBilling (PaymentHistoryId, MonthlyBillingId)
            SELECT p.Id, b.Id FROM PaymentHistory p CROSS JOIN MonthlyBilling b
            WHERE p.SIN=@sin AND b.SIN=@sin AND p.ORNumber=@or AND b.BillingYear=2026 AND b.BillingMonth=2
            """, ("@sin", sin), ("@or", "DEV-ARCH-" + run));
        await Execute("""
            INSERT INTO VehiclePermits (VIN, CompanyName, DriverName, PlateNo, SECRegNo, DTINumber, PermitStatus, PermitYear, IsArchived)
            VALUES (@vin, 'Synthetic Archive Company', 'Synthetic Driver', @vin, '', '', 'Paid', 2026, 0)
            """, ("@vin", vin));
        await Execute("""
            INSERT INTO VehiclePermitHistory (VIN, ORNumber, AmountPaid, PermitYear, DatePaid, RecordedBy)
            VALUES (@vin, @or, 50, 2026, GETDATE(), @user)
            """, ("@vin", vin), ("@or", "DEV-VARCH-" + run), ("@user", user));
        // A saved draft remains attached to the same VIN through archive and restore.
        var amounts = Enumerable.Repeat(0m, VehicleFeeDraft.FeeNames.Length).ToArray(); amounts[0] = 50;
        var draft = await vehicles.SaveDraftAsync(vin, 2026, amounts, ["", "", "", ""], new DateTime(2026, 9, 26), default);
        Check(draft.Success, "synthetic vehicle draft saved");
        Check((await archive.ProfileAsync(sin, true, user, default)).Success, "profile archived");
        Check((await archive.VehicleAsync(vin, true, user, default)).Success, "vehicle archived");
        Check((await archive.ProfilesAsync(sin, default)).Any(x => x.Sin == sin) &&
              (await archive.VehiclesAsync(vin, default)).Any(x => x.Vin == vin), "archive lists contain records");
        Check(!(await profiles.ListAsync(sin, 1, default)).Profiles.Any(x => x.Sin == sin) &&
              !(await vehicles.ListAsync(vin, 1, default)).Items.Any(x => x.Vin == vin), "active lists exclude archived records");
        Check(!(await archive.ProfileAsync(sin, true, user, default)).Success &&
              !(await archive.VehicleAsync(vin, true, user, default)).Success, "repeat archive rejected cleanly");
        Check((await billing.GenerateAsync(sin, new DateTime(2026, 9, 26), default)) == 0,
            "archived profile billing rejected");
        Check(!(await billing.PayAsync(sin, "DEV-ARCH-PAY-" + run, user, new DateTime(2026, 9, 26), default)).Success,
            "archived profile payment rejected");
        Check(!(await vehicles.PayAsync(vin, "DEV-VARCH-PAY-" + run, 2026, user, new DateTime(2026, 9, 26), default)).Success,
            "archived vehicle payment rejected");
        Check((int)(await Scalar("SELECT COUNT(*) FROM MonthlyBilling WHERE SIN=@sin", ("@sin", sin)) ?? 0) == 1 &&
              (int)(await Scalar("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@sin", ("@sin", sin)) ?? 0) == 1 &&
              (int)(await Scalar("SELECT COUNT(*) FROM PaymentHistoryBilling l JOIN PaymentHistory p ON p.Id=l.PaymentHistoryId WHERE p.SIN=@sin", ("@sin", sin)) ?? 0) == 1 &&
              (int)(await Scalar("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin", ("@vin", vin)) ?? 0) == 1 &&
              (int)(await Scalar("SELECT COUNT(*) FROM VehiclePermitFeeDrafts WHERE VIN=@vin", ("@vin", vin)) ?? 0) == 1,
              "billing, payment, permit, and draft history retained");
        Check((await archive.ProfileAsync(sin, false, user, default)).Success &&
              (await archive.VehicleAsync(vin, false, user, default)).Success, "both records restored");
        Check((await profiles.ListAsync(sin, 1, default)).Profiles.Any(x => x.Sin == sin) &&
              (await vehicles.ListAsync(vin, 1, default)).Items.Any(x => x.Vin == vin), "restored records return to active lists");
        Check((int)(await Scalar("SELECT COUNT(*) FROM MonthlyBilling WHERE SIN=@sin", ("@sin", sin)) ?? 0) == 1 &&
              (int)(await Scalar("SELECT COUNT(*) FROM AuditTrail WHERE SIN=@sin AND Action IN ('Archive','Restore')", ("@sin", sin)) ?? 0) == 2 &&
              (int)(await Scalar("SELECT COUNT(*) FROM AuditTrail WHERE SIN=@vin AND Action IN ('Archive Vehicle Permit','Restore Vehicle Permit')", ("@vin", vin)) ?? 0) == 2,
              "restore creates no bill and audit entries match both transitions");

        async Task<object?> Scalar(string sql, params (string, object)[] args)
        { await using var command = new SqlCommand(sql, connection); foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value); return await command.ExecuteScalarAsync(); }
        async Task Execute(string sql, params (string, object)[] args)
        { await using var command = new SqlCommand(sql, connection); foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(); }
    }

    private sealed class DevelopmentEnvironment : Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "ArchiveTests";
        public string ContentRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
