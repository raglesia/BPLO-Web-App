using BusinessPermitLicensingSystem.Web.Administration;
using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class AdminScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private static void Check(bool yes, string label) { if (!yes) throw new Exception(label); Console.WriteLine("PASS " + label); }
    public static async Task RunAsync()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var rates = new RentalRateService(configuration);
        var audit = new AuditTrailService(configuration);
        var profiles = new ProfileService(configuration);
        var billing = new BillingService(configuration);
        await using var connection = new SqlConnection(ConnectionString); await connection.OpenAsync();
        Check((string?)await Scalar("SELECT DB_NAME()") == "BPLS_Dev", "admin tests use BPLS_Dev");
        int user = (int)(await Scalar("SELECT TOP (1) Id FROM Users ORDER BY Id") ?? throw new Exception("No user"));
        string suffix = Guid.NewGuid().ToString("N")[..10];
        string section = "Synthetic Rate " + suffix;
        Check(RentalRateService.Validate("", "PerSqm", 100, 0) is not null &&
              RentalRateService.Validate(section, "Other", 100, 0) is not null &&
              RentalRateService.Validate(section, "PerSqm", -1, 0) is not null &&
              RentalRateService.Validate(section, "Flat", 0, 0) is not null &&
              RentalRateService.Validate(section, "PerSqm", 1.001m, 0) is not null,
              "rental rate server validation");
        Check((await rates.SaveAsync(section, "PerSqm", 100, 0, true, user, default)).Success,
            "add per-square-meter section");
        Check(!(await rates.SaveAsync(section, "PerSqm", 100, 0, true, user, default)).Success,
            "duplicate section rejected");
        int stall = Random.Shared.Next(1000000, 9999999);
        var firstInput = new ProfileInput { FullName = "Synthetic Rate Owner", BusinessName = "Synthetic Rate Business",
            BusinessSection = section, StallNumber = stall.ToString(), StallSize = "2", PaymentStatus = "Unpaid",
            StartDate = new DateTime(2026, 1, 1) };
        var first = await profiles.CreateAsync(firstInput, user, default);
        Check(first.Success && first.Sin is not null, "profile uses new rate section");
        string sin = first.Sin!;
        Check((await profiles.GetAsync(sin, default))?.MonthlyRental == 200m,
            "first profile rent uses 100 per sqm times two");
        Check(await billing.GenerateAsync(sin, new DateTime(2026, 2, 21), default) == 1,
            "first profile billing snapshot created");
        Check((await rates.SaveAsync(section, "PerSqm", 150, 0, false, user, default)).Success,
            "update section rate");
        Check((await profiles.GetAsync(sin, default))?.MonthlyRental == 200m &&
              Convert.ToDecimal(await Scalar("SELECT MonthlyRental FROM MonthlyBilling WHERE SIN=@sin AND BillingYear=2026 AND BillingMonth=2", ("@sin", sin))) == 200m,
              "rate change leaves existing profile and billing snapshot unchanged");
        var secondInput = new ProfileInput { FullName = "Another Synthetic Rate Owner", BusinessName = "Another Synthetic Rate Business",
            BusinessSection = section, StallNumber = (stall + 1).ToString(), StallSize = "2", PaymentStatus = "Unpaid",
            StartDate = new DateTime(2026, 1, 1) };
        var second = await profiles.CreateAsync(secondInput, user, default);
        Check(second.Success && (await profiles.GetAsync(second.Sin!, default))?.MonthlyRental == 300m,
            "new profile uses updated rate");
        Check((await rates.SaveAsync(section, "Flat", 999, 1200, false, user, default)).Success &&
              (await rates.ListAsync(default)).Single(x => x.Section == section) is { RateType: "Flat", RatePerSqm: 0, FlatRate: 1200 },
              "flat-rate update clears inactive per-square-meter value");
        Check((int)(await Scalar("SELECT COUNT(*) FROM AuditTrail WHERE Action IN ('AddRate','UpdateRate') AND Details LIKE @pattern AND UserId=@user AND SIN IS NULL",
            ("@pattern", "%" + section + "%"), ("@user", user)) ?? 0) == 3,
            "rate writes have acting-user audit entries");
        var activity = await audit.ListAsync("activity", section, 1, default);
        var users = await audit.ListAsync("users", section, 1, default);
        Check(activity.Total == 3 && activity.Entries.All(x => x.Details.Contains(section)) && users.Total == 0,
            "read-only audit viewer separates business activity and login/logout");
        Check((await audit.ListAsync("activity", "no-such-phase11-audit", 1, default)).Entries.Count == 0,
            "audit viewer empty search state");

        var vehicles = new VehicleRecordService(configuration);
        string plate = "T" + suffix.ToUpperInvariant();
        Check(!(await vehicles.SaveAsync(null, "", "", plate, "", "", user, default)).Success,
            "vehicle entry requires company");
        VehicleRecordResult created = await vehicles.SaveAsync(null, "Synthetic Vehicle Company", "Driver",
            plate, "SEC-1", "DTI-1", user, default);
        Check(created.Success && created.Vin?.StartsWith($"VIN-{DateTime.Now.Year}-") == true,
            "single vehicle create allocates VIN");
        Check((await vehicles.GetAsync(created.Vin!, default)) is { CompanyName: "Synthetic Vehicle Company", PlateNo: var p } && p == plate,
            "new vehicle details retained");
        Check(!(await vehicles.SaveAsync(null, "Duplicate", "", plate, "", "", user, default)).Success,
            "single vehicle duplicate plate rejected");
        Check((await vehicles.SaveAsync(created.Vin, "Edited Vehicle Company", "Driver 2",
            plate, "SEC-2", "DTI-2", user, default)).Success &&
            (await vehicles.GetAsync(created.Vin!, default)) is { CompanyName: "Edited Vehicle Company", SecRegNo: "SEC-2" },
            "single vehicle edit retains VIN and updates fields");
        Check((int)(await Scalar("SELECT COUNT(*) FROM AuditTrail WHERE SIN=@vin AND UserId=@user AND Action IN ('Add Vehicle Permit','Update Vehicle Permit')",
            ("@vin", created.Vin!), ("@user", user)) ?? 0) == 2,
            "single vehicle create/edit audit owner");

        async Task<object?> Scalar(string sql, params (string, object)[] args)
        { await using var command = new SqlCommand(sql, connection); foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value); return await command.ExecuteScalarAsync(); }
    }

}
