using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Reports;
using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS {name}");
}

Check(VehicleFeeDraft.OccupationalPermitAmount("0") == 0m, "zero employees: no occupational fee");
Check(VehicleFeeDraft.OccupationalPermitAmount("1") == 300m, "one employee: 300 pesos");
Check(VehicleFeeDraft.OccupationalPermitAmount("2") == 600m, "two employees: 600 pesos");
var rentalPenaltyReport = new ReportBill(2026, 9, 1200m, 1200m, 0m, 0m, "Unpaid", null, null);
Check(rentalPenaltyReport.CurrentPenalty(new DateTime(2026, 9, 20)) == 0m &&
      rentalPenaltyReport.CurrentPenalty(new DateTime(2026, 9, 21)) == 300m,
      "rental print penalty automatically begins on the 21st");
Check(rentalPenaltyReport.CurrentPenalty(new DateTime(2026, 10, 21)) == 300m,
      "rental print penalty is fixed and does not compound");
Check((rentalPenaltyReport with { Status = "Paid", Penalty = 100m }).CurrentPenalty(new DateTime(2026, 10, 21)) == 100m,
      "rental print preserves recorded paid penalties");
Check((rentalPenaltyReport with { BaseRent = null }).CurrentPenalty(new DateTime(2026, 9, 29)) is null,
      "rental print does not invent penalties for unknown rent basis");

var noPreparedBills = new ProfileReport("SIN-DEMO", "Owner", "Business", "Section", "FC4", "0", "Unpaid", false, [], [], 1200m, 0m, "2026-07-29");
var automaticAssessment = noPreparedBills.AssessmentBills(new DateTime(2026, 9, 29));
Check(automaticAssessment.Count == 2 && automaticAssessment.Sum(x => x.BaseRent) == 2400m &&
      automaticAssessment.Sum(x => x.CurrentPenalty(new DateTime(2026, 9, 29))) == 600m,
      "verified owner with no prepared bills automatically assesses August and September rent and penalties");
Check(noPreparedBills.Bills.Count == 0, "automatic print assessment does not create stored bills");
var alreadyPaid = automaticAssessment[0] with { Status = "Paid", Penalty = 300m };
var partialAssessment = (noPreparedBills with { Bills = [alreadyPaid] }).AssessmentBills(new DateTime(2026, 9, 29));
Check(partialAssessment.Count == 2 && partialAssessment.Count(x => x.Status == "Unpaid") == 1 && partialAssessment[0] == alreadyPaid,
      "automatic assessment preserves paid periods and adds only missing periods");
Check((noPreparedBills with { Status = "Unverified" }).AssessmentBills(new DateTime(2026, 9, 29)).Count == 0 &&
      (noPreparedBills with { StartDate = "" }).AssessmentBills(new DateTime(2026, 9, 29)).Count == 0,
      "automatic assessment still requires verified occupancy");
Check(noPreparedBills.AssessmentBills(new DateTime(2026, 7, 29)).Count == 0,
      "automatic assessment excludes occupancy month");

var permitDetails = new DraftData { LineOfBusiness = "Transport", Description = "Shuttle",
    Location = "Masinloc", Employees = "3", Capital = "100000.00", Stickers = "2",
    Organization = "Sole Proprietorship", Quarter = "3RD QUARTER",
    SanitaryType = "NON-FOOD", FireType = "ESTAB" };
var draftAmounts = Enumerable.Repeat(0m, VehicleFeeDraft.FeeNames.Length).ToArray();
draftAmounts[0] = 125m;
var (permitJson, permitTotal) = VehicleFeeDraft.Create(draftAmounts, ["", "", "", ""], permitDetails);
var permitRoundTrip = VehicleFeeDraft.Read(permitJson, permitTotal);
Check(permitRoundTrip.GrandTotal == 125m && permitRoundTrip.Data.LineOfBusiness == "Transport" &&
      permitRoundTrip.Data.Description == "Shuttle" && permitRoundTrip.Data.Location == "Masinloc" &&
      permitRoundTrip.Data.Employees == "3" && permitRoundTrip.Data.Capital == "100000.00" &&
      permitRoundTrip.Data.Stickers == "2" && permitRoundTrip.Data.Organization == "Sole Proprietorship" &&
      permitRoundTrip.Data.Quarter == "3RD QUARTER" && permitRoundTrip.Data.SanitaryType == "NON-FOOD" &&
      permitRoundTrip.Data.FireType == "ESTAB", "vehicle permit details survive fee draft JSON round trip");
foreach (string organization in new[] { "Individual", "Sole Proprietorship", "Partnership", "Corporation", "Cooperative", "Other" })
{
    permitDetails.Organization = organization;
    Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Count == 0, "valid organization " + organization);
}
foreach (string quarter in new[] { "1ST QUARTER", "2ND QUARTER", "3RD QUARTER", "4TH QUARTER" })
{
    permitDetails.Quarter = quarter;
    Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Count == 0, "valid quarter " + quarter);
}
foreach (string sanitary in new[] { "OTHER", "FOOD", "NON-FOOD" })
{
    permitDetails.SanitaryType = sanitary;
    Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Count == 0, "valid sanitary type " + sanitary);
}
foreach (string fire in new[] { "ESTAB", "OTHER" })
{
    permitDetails.FireType = fire;
    Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Count == 0, "valid fire type " + fire);
}
permitDetails.Organization = "unknown";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Organization"), "crafted organization rejected");
permitDetails.Organization = "Other";
permitDetails.Quarter = "5TH QUARTER";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Quarter"), "crafted quarter rejected");
permitDetails.Quarter = "3RD QUARTER";
permitDetails.SanitaryType = "UNKNOWN";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "SanitaryType"), "crafted sanitary type rejected");
permitDetails.SanitaryType = "FOOD";
permitDetails.FireType = "UNKNOWN";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "FireType"), "crafted fire type rejected");
permitDetails.FireType = "ESTAB";
permitDetails.Employees = "-1";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Employees"), "negative employees rejected");
permitDetails.Employees = "1.5";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Employees"), "fractional employees rejected");
permitDetails.Employees = "abc";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Employees"), "non-numeric employees rejected");
permitDetails.Employees = "3";
permitDetails.Stickers = "-1";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Stickers"), "negative stickers rejected");
permitDetails.Stickers = "1.5";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Stickers"), "fractional stickers rejected");
permitDetails.Stickers = "2";
permitDetails.Capital = "-1";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Capital"), "negative capital rejected");
permitDetails.Capital = "1.001";
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Capital"), "sub-cent capital rejected");
permitDetails.Capital = "1,234.56";
Check(VehicleFeeDraft.NormalizePermitDetails(permitDetails).Capital == "1,234.56" &&
      VehicleFeeDraft.Total(draftAmounts) == 125m, "Philippine capital formatting leaves fee total unchanged");
permitDetails.Location = new string('X', 256);
Check(VehicleFeeDraft.ValidatePermitDetails(permitDetails).Any(x => x.Field == "Location"),
    "oversized permit text rejected");
permitDetails.Location = "Masinloc";
var legacy = new DraftData();
Check(VehicleFeeDraft.Read(VehicleFeeDraft.Create(draftAmounts, ["", "", "", ""], legacy).Json, 125m).Data.Quarter == "",
    "older blank permit classifications remain readable");

Check(RentalPaymentSelection.Validate(["A"], true) is null &&
      RentalPaymentSelection.Validate(["B", "A", "C"], true) is null &&
      RentalPaymentSelection.Validate([], true) is not null &&
      RentalPaymentSelection.Validate(["A", "B", "C", "D"]) == RentalPaymentSelection.MaximumMessage &&
      RentalPaymentSelection.Validate(["A", "a"]) is not null,
      "rental report accepts one to three ordered unique SINs and rejects empty, fourth, and duplicate");
Check(VehiclePaymentSelection.Validate([1], true) is null &&
      VehiclePaymentSelection.Validate([3, 1, 2], true) is null &&
      VehiclePaymentSelection.Validate([], true) is not null &&
      VehiclePaymentSelection.Validate([1, 2, 3, 4]) == VehiclePaymentSelection.MaximumMessage &&
      VehiclePaymentSelection.Validate([1, 1]) is not null &&
      VehiclePaymentSelection.Validate([0]) is not null,
      "vehicle payment report accepts one to three unique history IDs and rejects empty, fourth, duplicate, and invalid");

var periods = BillingRules.MissingPeriods(new DateTime(2026, 1, 31), new DateTime(2026, 4, 1),
    [(2026, 3)]);
Check(periods.SequenceEqual([(2026, 2), (2026, 4)]), "month after occupancy; missing months only");
Check(BillingRules.MissingPeriods(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30), []).Count == 0,
    "occupancy month excluded");
Check(BillingRules.MissingPeriods(new DateTime(2026, 5, 1), new DateTime(2026, 4, 30), []).Count == 0,
    "future occupancy excluded");
Check(BillingRules.Penalty(100.02m, 2026, 4, new DateTime(2026, 4, 19)) == 0,
    "before day 20");
Check(BillingRules.Penalty(100.02m, 2026, 4, new DateTime(2026, 4, 20)) == 0,
    "on day 20");
Check(BillingRules.Penalty(100.02m, 2026, 4, new DateTime(2026, 4, 21)) == 25.00m,
    "after day 20 and midpoint-to-even rounding");
Check(BillingRules.Penalty(100.02m, 2026, 3, new DateTime(2026, 4, 1)) == 25.00m,
    "previous month penalized");
Check(BillingRules.Penalty(100.02m, 2026, 4, new DateTime(2026, 4, 21)) ==
      BillingRules.Penalty(100.02m, 2026, 4, new DateTime(2026, 4, 21)),
    "repeated penalty is fixed");
Check(BillingRules.BaseFromCombinedProfile(110m, 10m) == 100m,
    "known development profile separates base rent");
Check(BillingRules.BaseFromBill(110m, 10m, null) is null &&
      BillingRules.BaseFromBill(100m, 10m, "BaseOnly") == 100m,
    "old additional-charge bill remains unresolved; new bill uses base rent");
Check(BillingRules.Total(100m, 10m, 0m) == 110m,
    "additional charge once before penalty");
Check(BillingRules.Total(100m, 10m, BillingRules.Penalty(100m, 2026, 4, new DateTime(2026, 4, 21))) == 135m,
    "additional charge once and penalty on base rent");
Check(BillingRules.Total(100m, 0m, 25m) == 125m,
    "flat and per-square-meter computed rental totals");
Check(BillingRules.MissingPeriods(new DateTime(2023, 12, 31), new DateTime(2024, 2, 29), [])
      .SequenceEqual([(2024, 1), (2024, 2)]), "December to January and leap February periods");
Check(BillingRules.MissingPeriods(new DateTime(2024, 2, 29), new DateTime(2024, 3, 1), [])
      .SequenceEqual([(2024, 3)]), "leap-day occupancy starts billing in March");
Check(BillingRules.Total(0.01m, 0.00m, BillingRules.Penalty(0.01m, 2026, 4, new DateTime(2026, 4, 21))) == 0.01m &&
      BillingRules.Total(123456.78m, 0.25m, 0.00m) == 123457.03m,
    "small and reasonable large decimal amounts");

if (args.Contains("--database"))
{
    const string connectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["ConnectionStrings:BPLS"] = connectionString }).Build();
    var service = new BillingService(settings);
    var run = Guid.NewGuid().ToString("N")[..10];
    string normal = $"SIN-BILLTEST-{run}-1", recent = $"SIN-BILLTEST-{run}-2";
    string unverified = $"SIN-BILLTEST-{run}-3", archived = $"SIN-BILLTEST-{run}-4";
    string flat = $"SIN-BILLTEST-{run}-5", perSquareMeter = $"SIN-BILLTEST-{run}-6";
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    await using (var check = new SqlCommand("SELECT DB_NAME()", connection))
        Check((string?)await check.ExecuteScalarAsync() == "BPLS_Dev", "development database guard");
    static async Task<int> Count(SqlConnection connection, string table)
    {
        await using var command = new SqlCommand($"SELECT COUNT(*) FROM {table}", connection);
        return (int)(await command.ExecuteScalarAsync() ?? 0);
    }
    int payments = await Count(connection, "PaymentHistory");
    int links = await Count(connection, "PaymentHistoryBilling");
    await using var legacyBefore = new SqlCommand("""
        SELECT COUNT(*), SUM(MonthlyRental), SUM(AdditionalCharge), SUM(Penalty)
        FROM MonthlyBilling WHERE WebRentBasis IS NULL
        """, connection);
    int legacyCount;
    decimal legacyRent, legacyExtra, legacyPenalty;
    await using (var reader = await legacyBefore.ExecuteReaderAsync())
    { await reader.ReadAsync(); legacyCount = reader.GetInt32(0); legacyRent = reader.GetDecimal(1);
      legacyExtra = reader.GetDecimal(2); legacyPenalty = reader.GetDecimal(3); }
    foreach (var (sin, status, start, rent, extra, isArchived) in new[]
    {
        (normal, "Unpaid", "2026-01-15", 110m, 10m, 0),
        (recent, "Unpaid", "2026-04-01", 420m, 0m, 0),
        (unverified, "Unverified", "", 420m, 0m, 0),
        (archived, "Unpaid", "2026-01-15", 420m, 0m, 1),
        (flat, "Unpaid", "2026-01-15", 1200m, 0m, 0),
        (perSquareMeter, "Unpaid", "2026-01-15", 2m * 210m, 0m, 0)
    })
    {
        await using var insert = new SqlCommand("""
            INSERT INTO Profiling(SIN,FullName,BusinessName,BusinessSection,StallNumber,StallSize,
                MonthlyRental,PaymentStatus,StartDate,AdditionalCharge,IsArchived)
            VALUES(@sin,@name,@business,@section,@stall,'2',@rent,@status,@start,@extra,@archived)
            """, connection);
        insert.Parameters.AddWithValue("@sin", sin);
        insert.Parameters.AddWithValue("@name", "Synthetic Billing " + sin);
        insert.Parameters.AddWithValue("@business", "Synthetic Business " + sin);
        insert.Parameters.AddWithValue("@section", sin == flat ? "Corridor" : "Public Market Stalls");
        insert.Parameters.AddWithValue("@stall", sin);
        insert.Parameters.AddWithValue("@rent", rent);
        insert.Parameters.AddWithValue("@status", status);
        insert.Parameters.AddWithValue("@start", start);
        insert.Parameters.AddWithValue("@extra", extra);
        insert.Parameters.AddWithValue("@archived", isArchived);
        await insert.ExecuteNonQueryAsync();
    }
    var asOf = new DateTime(2026, 4, 21);
    var attempts = await Task.WhenAll(service.GenerateAsync(normal, asOf, default),
        service.GenerateAsync(normal, asOf, default));
    Check(attempts.Sum() == 3 && attempts.Contains(0), "concurrent generation creates February-April once");
    Check(await service.GenerateAsync(normal, asOf, default) == 0, "repeated generation adds nothing");
    Check(await service.GenerateAsync(recent, asOf, default) == 0, "occupancy month excluded in database");
    Check(await service.GenerateAsync(unverified, asOf, default) == 0, "unverified excluded in database");
    Check(await service.GenerateAsync(archived, asOf, default) == 0, "archived excluded in database");
    Check(await service.GenerateAsync(flat, asOf, default) == 3 &&
          (await service.GetAsync(flat, asOf, default))!.Rows.All(row => row.Rental == 1200m),
        "flat-rate billing copies 1200 snapshot");
    Check(await service.GenerateAsync(perSquareMeter, asOf, default) == 3 &&
          (await service.GetAsync(perSquareMeter, asOf, default))!.Rows.All(row => row.Rental == 420m),
        "per-square-meter billing copies 2 times 210 snapshot");
    var view = (await service.GetAsync(normal, asOf, default))!;
    Check(view.Rows.Count == 3 && view.Rows.All(row => row.Rental == 100m && row.RentBasis == "BaseOnly") &&
          view.RentDue == 300m && view.AdditionalDue == 30m && view.PenaltyDue == 75m && view.TotalDue == 405m,
        "corrected base-rent snapshots, additional charge, penalty, outstanding balance");
    Check(await service.UpdatePenaltyAsync(normal, asOf, default) == 75m &&
          await service.UpdatePenaltyAsync(normal, asOf, default) == 75m,
        "repeated penalty processing does not compound");
    await using (var edit = new SqlCommand("UPDATE Profiling SET MonthlyRental=999 WHERE SIN=@sin", connection))
    { edit.Parameters.AddWithValue("@sin", normal); await edit.ExecuteNonQueryAsync(); }
    Check((await service.GetAsync(normal, asOf, default))!.Rows.All(row => row.Rental == 100m),
        "historical billing snapshot survives profile edit");
    await using var legacyAfter = new SqlCommand("""
        SELECT COUNT(*), SUM(MonthlyRental), SUM(AdditionalCharge), SUM(Penalty)
        FROM MonthlyBilling WHERE WebRentBasis IS NULL
        """, connection);
    await using (var reader = await legacyAfter.ExecuteReaderAsync())
    { await reader.ReadAsync();
      Check(reader.GetInt32(0) == legacyCount && reader.GetDecimal(1) == legacyRent &&
            reader.GetDecimal(2) == legacyExtra && reader.GetDecimal(3) == legacyPenalty,
            "legacy Phase 5 billing snapshots unchanged"); }
    Check(await Count(connection, "PaymentHistory") == payments &&
          await Count(connection, "PaymentHistoryBilling") == links,
        "payment history tables unchanged");
    Console.WriteLine($"Synthetic test profiles retained in BPLS_Dev: {normal}, {recent}, {unverified}, {archived}, {flat}, {perSquareMeter}");
}

if (args.Contains("--payments")) await PaymentScenarios.RunAsync();
if (args.Contains("--vehicles")) await VehicleScenarios.RunAsync();
if (args.Contains("--archive")) await ArchiveScenarios.RunAsync();
if (args.Contains("--transfer")) await TransferScenarios.RunAsync();
if (args.Contains("--reports")) await ReportScenarios.RunAsync();
if (args.Contains("--admin")) await AdminScenarios.RunAsync();
string? browserArg = args.FirstOrDefault(x => x.StartsWith("--browser=", StringComparison.Ordinal));
if (browserArg is not null) await BrowserScenarios.RunAsync(browserArg[10..]);
