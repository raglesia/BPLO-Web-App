using BusinessPermitLicensingSystem.Web.Reports;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class ReportScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private static void Check(bool yes, string label) { if (!yes) throw new Exception(label); Console.WriteLine("PASS " + label); }
    public static async Task RunAsync()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var service = new ReportService(configuration);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        Check((string?)await Scalar("SELECT DB_NAME()") == "BPLS_Dev", "report tests use BPLS_Dev");
        Check(ReportService.ValidateRange(2026, 2, 2026, 1) is not null &&
            ReportService.ValidateRange(2026, 13, 2026, 13) is not null &&
            ReportService.ValidateRange(2026, 1, 2026, 3) is null, "report period validation");
        int user = (int)(await Scalar("SELECT TOP (1) Id FROM Users ORDER BY Id") ?? throw new Exception("No user"));
        string run = Guid.NewGuid().ToString("N")[..10];
        string sin = "SIN-REPTEST-" + run, legacySin = "SIN-LEGREP-" + run;
        await InsertProfile(sin, "Synthetic Report Owner", 1);
        await InsertProfile(legacySin, "Synthetic Legacy Owner", 1);
        await Bill(sin, 1, 100, 10, 25, "Paid", "BaseOnly", "DEV-REP-" + run);
        await Bill(sin, 2, 200, 0, 0, "Unpaid", "BaseOnly", null);
        await Bill(sin, 3, 100, 0, 25, "Unpaid", "BaseOnly", null);
        await Bill(legacySin, 1, 110, 10, 25, "Unpaid", null, null);
        await Execute("""
            INSERT INTO PaymentHistory (SIN, ORNumber, AmountPaid, Penalty, DatePaid, RecordedBy)
            VALUES (@sin, @or, 135, 25, '2026-02-15', @user)
            """, ("@sin", sin), ("@or", "DEV-REP-" + run), ("@user", user));
        await Execute("""
            INSERT INTO PaymentHistoryBilling (PaymentHistoryId, MonthlyBillingId)
            SELECT p.Id, b.Id FROM PaymentHistory p CROSS JOIN MonthlyBilling b
            WHERE p.SIN=@sin AND p.ORNumber=@or AND b.SIN=@sin AND b.BillingYear=2026 AND b.BillingMonth=1
            """, ("@sin", sin), ("@or", "DEV-REP-" + run));
        await Execute("UPDATE Profiling SET MonthlyRental=999, BusinessName='=SUM(1,1)' WHERE SIN=@sin", ("@sin", sin));
        int billsBefore = (int)(await Scalar("SELECT COUNT(*) FROM MonthlyBilling") ?? 0);
        int paymentsBefore = (int)(await Scalar("SELECT COUNT(*) FROM PaymentHistory") ?? 0);
        int auditsBefore = (int)(await Scalar("SELECT COUNT(*) FROM AuditTrail") ?? 0);
        var report = await service.ProfileAsync(sin, default) ?? throw new Exception("Report missing");
        Check(report.Archived && report.Bills.Count == 3 && report.Bills.First(x => x.Month == 1).Total == 135m &&
              report.BilledTotal == 460m, "corrected 100 + 10 + 25 = 135; three periods total 460");
        Check(report.Bills.First(x => x.Month == 1).StoredRent == 100m &&
              Convert.ToDecimal(await Scalar("SELECT MonthlyRental FROM Profiling WHERE SIN=@sin", ("@sin", sin))) == 999m,
              "billing report uses stored snapshot after profile rent changes");
        Check(report.Payments.Count == 1 && report.Payments[0].OrNumber == "DEV-REP-" + run &&
              report.Payments[0].Amount == 135m && report.Payments[0].Periods == "2026-01" &&
              report.Payments[0].PaidAt.Date == new DateTime(2026, 2, 15) && report.Payments[0].Recorder.Length > 0,
              "paid billing uses OR, date, amount, recorder, and exact linked period");
        var legacy = await service.ProfileAsync(legacySin, default) ?? throw new Exception("Legacy report missing");
        Check(legacy.Archived && legacy.ReviewNeeded && legacy.BilledTotal is null && legacy.Bills[0].StoredRent == 110m &&
              legacy.Bills[0].Total is null, "ambiguous legacy bill shows stored values but no authoritative total");
        var monthly = await service.MonthlyAsync(2026, 1, 2026, 3, default);
        decimal directCollected = (decimal)(await Scalar("""
            SELECT COALESCE(SUM(AmountPaid),0) FROM PaymentHistory
            WHERE DatePaid >= '2026-01-01' AND DatePaid < '2026-04-01'
            """) ?? 0m);
        decimal directPenalty = (decimal)(await Scalar("""
            SELECT COALESCE(SUM(Penalty),0) FROM PaymentHistory
            WHERE DatePaid >= '2026-01-01' AND DatePaid < '2026-04-01'
            """) ?? 0m);
        Check(monthly.Bills.Any(x => x.Sin == sin && x.Bill.Month == 1) &&
              monthly.Bills.Any(x => x.Sin == legacySin && x.Bill.ReviewNeeded) &&
              monthly.Payments.Any(x => x.Sin == sin && x.Amount == 135m && x.Periods == "2026-01") &&
              monthly.CollectedTotal == directCollected && monthly.PenaltyCollected == directPenalty && monthly.BilledTotal is null,
              "monthly report uses billing periods, archived payments, direct collection and penalty totals, and review state");
        byte[] bytes = service.MonthlyExcel(monthly, "Synthetic Operator", new DateTime(2026, 9, 26));
        using (var workbook = new XLWorkbook(new MemoryStream(bytes)))
        {
            var summary = workbook.Worksheet("Summary");
            var billing = workbook.Worksheet("Billing periods");
            var payment = workbook.Worksheet("Payments");
            Check(summary.Cell(14, 2).GetValue<decimal>() == directCollected && summary.Cell(15, 2).GetValue<decimal>() == directPenalty &&
                  billing.Cell(1, 1).GetString() == "Period" && payment.Cell(1, 1).GetString() == "SIN" &&
                  summary.Cell(12, 2).GetString() == "Review needed", "monthly Excel opens with numeric collection and review marker");
            int formulaRow = Enumerable.Range(2, Math.Max(0, (billing.LastRowUsed()?.RowNumber() ?? 1) - 1))
                .First(i => billing.Cell(i, 2).GetString() == sin);
            Check(billing.Cell(formulaRow, 4).GetString() == "=SUM(1,1)" && !billing.Cell(formulaRow, 4).HasFormula,
                "report user text cannot execute as spreadsheet formula");
        }
        await service.ProfileAsync(sin, default);
        await service.MonthlyAsync(2026, 1, 2026, 3, default);
        service.MonthlyExcel(monthly, "Synthetic Operator", DateTime.Now);
        Check((int)(await Scalar("SELECT COUNT(*) FROM MonthlyBilling") ?? 0) == billsBefore &&
              (int)(await Scalar("SELECT COUNT(*) FROM PaymentHistory") ?? 0) == paymentsBefore &&
              (int)(await Scalar("SELECT COUNT(*) FROM AuditTrail") ?? 0) == auditsBefore &&
              (int)(await Scalar("SELECT IsArchived FROM Profiling WHERE SIN=@sin", ("@sin", sin)) ?? 0) == 1,
              "repeated views and downloads leave database state unchanged");

        async Task<object?> Scalar(string sql, params (string, object)[] args)
        { await using var command = new SqlCommand(sql, connection); foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value); return await command.ExecuteScalarAsync(); }
        async Task Execute(string sql, params (string, object)[] args)
        { await using var command = new SqlCommand(sql, connection); foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value); await command.ExecuteNonQueryAsync(); }
        async Task InsertProfile(string id, string name, int archived) => await Execute("""
            INSERT INTO Profiling (SIN,FullName,BusinessName,BusinessSection,StallNumber,StallSize,MonthlyRental,PaymentStatus,StartDate,AdditionalCharge,IsArchived)
            VALUES (@sin,@name,'Synthetic Report Business','Public Market Stalls',@sin,'2',100,'Unpaid','2026-01-01',0,@archived)
            """, ("@sin", id), ("@name", name), ("@archived", archived));
        async Task Bill(string id, int month, decimal rent, decimal additional, decimal penalty, string status, string? basis, string? orNumber) => await Execute("""
            INSERT INTO MonthlyBilling (SIN,BillingYear,BillingMonth,MonthlyRental,AdditionalCharge,Penalty,PaymentStatus,WebRentBasis,ORNumber,DatePaid)
            VALUES (@sin,2026,@month,@rent,@additional,@penalty,@status,@basis,@or,@paid)
            """, ("@sin", id), ("@month", month), ("@rent", rent), ("@additional", additional), ("@penalty", penalty),
                ("@status", status), ("@basis", (object?)basis ?? DBNull.Value), ("@or", (object?)orNumber ?? DBNull.Value),
                ("@paid", orNumber is null ? DBNull.Value : new DateTime(2026, 2, 15)));
    }
}
