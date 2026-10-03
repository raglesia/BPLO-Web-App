using System.Data;
using BusinessPermitLicensingSystem.Web.Billing;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Reports;

public sealed record ReportBill(int Year, int Month, decimal StoredRent, decimal? BaseRent,
    decimal Additional, decimal Penalty, string Status, string? OrNumber, DateTime? PaidAt)
{
    public decimal? CurrentPenalty(DateTime asOf) => Status == "Unpaid"
        ? BaseRent is decimal rent ? BillingRules.Penalty(rent, Year, Month, asOf) : null
        : Penalty;
    public bool ReviewNeeded => BaseRent is null;
    public decimal? Total => BaseRent is decimal value ? BillingRules.Total(value, Additional, Penalty) : null;
}
public sealed record ReportPayment(string Sin, string Owner, string OrNumber, DateTime PaidAt, decimal Amount, decimal Penalty, string Recorder, string Periods);
public sealed record ProfileReport(string Sin, string Owner, string Business, string Section, string Stall,
    string StallSize, string Status, bool Archived, IReadOnlyList<ReportBill> Bills, IReadOnlyList<ReportPayment> Payments, decimal MonthlyRental = 0, decimal AdditionalCharge = 0, string StartDate = "")
{
    public bool IsLegacyBaseline { get; init; }
    public IReadOnlyList<ArrearsRow> Arrears { get; init; } = [];
    public DateTime RentalCutoff(DateTime asOf) => IsLegacyBaseline &&
        DateTime.TryParse(StartDate, out var baseline) && asOf < baseline ? baseline : asOf;
    public bool CanAssess(DateTime asOf) => Status != "Unverified" && !Archived &&
        DateTime.TryParse(StartDate, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var occupancy) &&
        (occupancy.Date <= asOf.Date || IsLegacyBaseline);

    public IReadOnlyList<ReportBill> AssessmentBills(DateTime asOf)
    {
        if (!CanAssess(asOf)) return Bills;
        var occupancy = DateTime.Parse(StartDate, System.Globalization.CultureInfo.InvariantCulture);
        var result = Bills.ToList();
        var missing = BillingRules.MissingPeriods(occupancy, RentalCutoff(asOf),
            Bills.Select(x => (x.Year, x.Month)), IsLegacyBaseline);
        if (missing.Count > 0)
        {
            decimal baseRent = BillingRules.BaseFromCombinedProfile(MonthlyRental, AdditionalCharge);
            result.AddRange(missing.Select(x => new ReportBill(x.Year, x.Month, baseRent, baseRent,
                AdditionalCharge, 0m, "Unpaid", null, null)));
        }
        return result;
    }

    public bool ReviewNeeded => Bills.Any(x => x.ReviewNeeded);
    public decimal? BilledTotal => ReviewNeeded ? null : Bills.Sum(x => x.Total ?? 0);
    public decimal PaymentTotal => Payments.Sum(x => x.Amount);
}
public sealed record MonthlyBill(string Sin, string Owner, string Business, string Section, bool Archived, ReportBill Bill);
public sealed record MonthlyReport(DateTime From, DateTime To, IReadOnlyList<MonthlyBill> Bills, IReadOnlyList<ReportPayment> Payments)
{
    public bool ReviewNeeded => Bills.Any(x => x.Bill.ReviewNeeded);
    public int OwnerCount => Bills.Select(x => x.Sin).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    public int PaidCount => Bills.Count(x => x.Bill.Status == "Paid");
    public int UnpaidCount => Bills.Count(x => x.Bill.Status == "Unpaid");
    public decimal? BilledTotal => ReviewNeeded ? null : Bills.Sum(x => x.Bill.Total ?? 0);
    public decimal? UnpaidTotal => Bills.Any(x => x.Bill.Status == "Unpaid" && x.Bill.ReviewNeeded) ? null :
        Bills.Where(x => x.Bill.Status == "Unpaid").Sum(x => x.Bill.Total ?? 0);
    public decimal CollectedTotal => Payments.Sum(x => x.Amount);
    public decimal PenaltyCollected => Payments.Sum(x => x.Penalty);
}

public sealed class ReportService(IConfiguration configuration)
{
    public static string? ValidateRange(int fromYear, int fromMonth, int toYear, int toMonth)
    {
        if (fromYear is < 2000 or > 2100 || toYear is < 2000 or > 2100 || fromMonth is < 1 or > 12 || toMonth is < 1 or > 12)
            return "Choose valid years and months.";
        if (fromYear * 12 + fromMonth > toYear * 12 + toMonth) return "From period must not be later than To period.";
        if (toYear * 12 + toMonth - (fromYear * 12 + fromMonth) > 59) return "Choose a range of at most five years.";
        return null;
    }

    public async Task<ProfileReport?> ProfileAsync(string sin, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(sin) || sin.Length > 100) return null;
        await using var connection = await OpenAsync(token);
        await using var profile = new SqlCommand("""
            SELECT SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize, PaymentStatus, IsArchived, MonthlyRental, AdditionalCharge, StartDate, IsLegacyBaseline
            FROM Profiling WHERE SIN=@sin
            """, connection);
        profile.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        string id, owner, business, section, stall, size, status, startDate; bool archived, legacy; decimal monthlyRental, additionalCharge;
        await using (var reader = await profile.ExecuteReaderAsync(token))
        {
            if (!await reader.ReadAsync(token)) return null;
            id = reader.GetString(0); owner = reader.GetString(1); business = reader.GetString(2);
            section = reader.GetString(3); stall = reader.GetString(4); size = reader.GetString(5);
            status = reader.GetString(6); archived = !reader.IsDBNull(7) && reader.GetInt32(7) != 0;
            monthlyRental = reader.GetDecimal(8); additionalCharge = reader.GetDecimal(9);
            startDate = reader.IsDBNull(10) ? "" : reader.GetString(10);
            legacy = reader.GetBoolean(11);
        }
        var bills = await ReadBillsAsync(connection, "WHERE mb.SIN=@sin", ("@sin", SqlDbType.NVarChar, sin), token);
        var payments = await ReadPaymentsAsync(connection, "WHERE ph.SIN=@sin", ("@sin", SqlDbType.NVarChar, sin), token);
        var arrears = new List<ArrearsRow>();
        await using var arrearsCommand = new SqlCommand("""
            SELECT Id, BillingYear, BillingMonth, BaseRent, AdditionalCharge, PenaltyAmount,
                   IsPaid, PaidAt, TreasuryReference
            FROM StallOwnerArrears WHERE SIN=@sin ORDER BY BillingYear, BillingMonth
            """, connection);
        arrearsCommand.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        await using (var reader = await arrearsCommand.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                arrears.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                    reader.GetDecimal(3), reader.GetDecimal(4), reader.GetDecimal(5), reader.GetBoolean(6),
                    reader.IsDBNull(7) ? null : reader.GetDateTime(7), reader.GetString(8)));
        return new ProfileReport(id, owner, business, section, stall, size, status, archived,
            bills.Select(x => x.Bill).ToList(), payments, monthlyRental, additionalCharge, startDate)
        { IsLegacyBaseline = legacy, Arrears = arrears };
    }

    public async Task<MonthlyReport> MonthlyAsync(int fromYear, int fromMonth, int toYear, int toMonth, CancellationToken token)
    {
        string? error = ValidateRange(fromYear, fromMonth, toYear, toMonth);
        if (error is not null) throw new ArgumentException(error);
        await using var connection = await OpenAsync(token);
        string billWhere = "WHERE mb.BillingYear * 12 + mb.BillingMonth BETWEEN @fromPeriod AND @toPeriod";
        var bills = await ReadBillsAsync(connection, billWhere,
            ("@fromPeriod", SqlDbType.Int, fromYear * 12 + fromMonth),
            ("@toPeriod", SqlDbType.Int, toYear * 12 + toMonth), token);
        DateTime from = new(fromYear, fromMonth, 1), to = new(toYear, toMonth, 1);
        var payments = await ReadPaymentsAsync(connection, "WHERE ph.DatePaid >= @fromDate AND ph.DatePaid < @toDate",
            ("@fromDate", SqlDbType.DateTime, from), ("@toDate", SqlDbType.DateTime, to.AddMonths(1)), token);
        return new(from, to, bills, payments);
    }

    public byte[] MonthlyExcel(MonthlyReport report, string generatedBy, DateTime generatedAt)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Municipality of Masinloc";
        summary.Cell(2, 1).Value = "Business Permit and Licensing Office";
        summary.Cell(3, 1).Value = "Monthly Collection Summary Report";
        summary.Cell(4, 1).Value = report.From.ToString("MMMM yyyy") + " - " + report.To.ToString("MMMM yyyy");
        summary.Cell(5, 1).SetValue("Generated by: " + generatedBy);
        summary.Cell(6, 1).Value = "Date generated: " + generatedAt.ToString("yyyy-MM-dd HH:mm");
        summary.Cell(8, 1).Value = "Owners with bills"; summary.Cell(8, 2).Value = report.OwnerCount;
        summary.Cell(9, 1).Value = "Billing periods"; summary.Cell(9, 2).Value = report.Bills.Count;
        summary.Cell(10, 1).Value = "Paid bills"; summary.Cell(10, 2).Value = report.PaidCount;
        summary.Cell(11, 1).Value = "Unpaid bills"; summary.Cell(11, 2).Value = report.UnpaidCount;
        summary.Cell(12, 1).Value = "Billed total"; SetMoneyOrReview(summary.Cell(12, 2), report.BilledTotal);
        summary.Cell(13, 1).Value = "Unpaid total"; SetMoneyOrReview(summary.Cell(13, 2), report.UnpaidTotal);
        summary.Cell(14, 1).Value = "Collected by payment date"; summary.Cell(14, 2).Value = report.CollectedTotal;
        summary.Cell(15, 1).Value = "Penalty collected"; summary.Cell(15, 2).Value = report.PenaltyCollected;
        summary.Cell(16, 1).Value = "Collection includes payments for archived owners and may cover bills outside the selected billing range.";
        if (report.ReviewNeeded) summary.Cell(17, 1).Value = "Review needed: one or more legacy bills have an unknown rent basis.";
        var billSheet = workbook.Worksheets.Add("Billing periods");
        string[] billHeaders = ["Period", "SIN", "Owner", "Business", "Section", "Archived", "Stored rent", "Base rent", "Additional charge", "Stored penalty", "Total", "Status", "OR number", "Date paid"];
        for (int i = 0; i < billHeaders.Length; i++) billSheet.Cell(1, i + 1).Value = billHeaders[i];
        int row = 2;
        foreach (var item in report.Bills)
        {
            var cells = billSheet.Row(row++);
            cells.Cell(1).Value = $"{item.Bill.Year}-{item.Bill.Month:D2}";
            cells.Cell(2).SetValue(item.Sin); cells.Cell(3).SetValue(item.Owner); cells.Cell(4).SetValue(item.Business);
            cells.Cell(5).SetValue(item.Section); cells.Cell(6).Value = item.Archived ? "Yes" : "No";
            cells.Cell(7).Value = item.Bill.StoredRent; SetMoneyOrReview(cells.Cell(8), item.Bill.BaseRent);
            cells.Cell(9).Value = item.Bill.Additional; cells.Cell(10).Value = item.Bill.Penalty;
            SetMoneyOrReview(cells.Cell(11), item.Bill.Total); cells.Cell(12).SetValue(item.Bill.Status);
            cells.Cell(13).SetValue(item.Bill.OrNumber ?? "");
            if (item.Bill.PaidAt is DateTime date) cells.Cell(14).Value = date;
        }
        var paymentSheet = workbook.Worksheets.Add("Payments");
        string[] paymentHeaders = ["SIN", "Owner", "OR number", "Payment date", "Amount", "Penalty collected", "Recorded by", "Covered periods"];
        for (int i = 0; i < paymentHeaders.Length; i++) paymentSheet.Cell(1, i + 1).Value = paymentHeaders[i];
        row = 2;
        foreach (var payment in report.Payments)
        {
            var cells = paymentSheet.Row(row++);
            cells.Cell(1).SetValue(payment.Sin); cells.Cell(2).SetValue(payment.Owner);
            cells.Cell(3).SetValue(payment.OrNumber); cells.Cell(4).Value = payment.PaidAt;
            cells.Cell(5).Value = payment.Amount; cells.Cell(6).Value = payment.Penalty;
            cells.Cell(7).SetValue(payment.Recorder); cells.Cell(8).SetValue(payment.Periods);
        }
        foreach (var sheet in workbook.Worksheets) { sheet.Columns().AdjustToContents(); sheet.Row(1).Style.Font.Bold = true; }
        using var output = new MemoryStream(); workbook.SaveAs(output); return output.ToArray();
    }

    private static void SetMoneyOrReview(IXLCell cell, decimal? value)
    { if (value is decimal money) { cell.Value = money; cell.Style.NumberFormat.Format = "₱#,##0.00"; } else cell.Value = "Review needed"; }

    private static async Task<List<MonthlyBill>> ReadBillsAsync(SqlConnection connection, string where,
        (string Name, SqlDbType Type, object Value) parameter, CancellationToken token) =>
        await ReadBillsAsync(connection, where, parameter, default, token);

    private static async Task<List<MonthlyBill>> ReadBillsAsync(SqlConnection connection, string where,
        (string Name, SqlDbType Type, object Value) first, (string Name, SqlDbType Type, object Value) second, CancellationToken token)
    {
        await using var command = new SqlCommand($"""
            SELECT mb.BillingYear, mb.BillingMonth, mb.MonthlyRental, mb.AdditionalCharge, mb.Penalty,
                   mb.PaymentStatus, mb.ORNumber, mb.DatePaid, mb.WebRentBasis,
                   mb.SIN, COALESCE(p.FullName, ''), COALESCE(p.BusinessName, ''),
                   COALESCE(p.BusinessSection, ''), COALESCE(p.IsArchived, 0)
            FROM MonthlyBilling mb LEFT JOIN Profiling p ON p.SIN=mb.SIN
            {where} ORDER BY mb.BillingYear, mb.BillingMonth, mb.SIN
            """, connection);
        command.Parameters.Add(first.Name, first.Type).Value = first.Value;
        if (second.Name is not null) command.Parameters.Add(second.Name, second.Type).Value = second.Value;
        var result = new List<MonthlyBill>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            decimal rent = reader.GetDecimal(2), additional = reader.GetDecimal(3);
            string? basis = reader.IsDBNull(8) ? null : reader.GetString(8);
            var bill = new ReportBill(reader.GetInt32(0), reader.GetInt32(1), rent,
                BillingRules.BaseFromBill(rent, additional, basis), additional, reader.GetDecimal(4),
                reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetDateTime(7));
            result.Add(new(reader.GetString(9), reader.GetString(10), reader.GetString(11), reader.GetString(12),
                reader.GetInt32(13) != 0, bill));
        }
        return result;
    }

    private static async Task<List<ReportPayment>> ReadPaymentsAsync(SqlConnection connection, string where,
        (string Name, SqlDbType Type, object Value) parameter, CancellationToken token) =>
        await ReadPaymentsAsync(connection, where, parameter, default, token);

    private static async Task<List<ReportPayment>> ReadPaymentsAsync(SqlConnection connection, string where,
        (string Name, SqlDbType Type, object Value) first, (string Name, SqlDbType Type, object Value) second, CancellationToken token)
    {
        await using var command = new SqlCommand($"""
            SELECT ph.SIN, COALESCE(p.FullName, ''), ph.ORNumber, ph.DatePaid, ph.AmountPaid, ph.Penalty,
                   COALESCE(u.FullName, CONCAT('User ', ph.RecordedBy)),
                   COALESCE(periods.Covered, '')
            FROM PaymentHistory ph
            LEFT JOIN Profiling p ON p.SIN=ph.SIN
            LEFT JOIN Users u ON u.Id=ph.RecordedBy
            OUTER APPLY (
                SELECT STRING_AGG(CONVERT(nvarchar(max), x.Period), ', ')
                       WITHIN GROUP (ORDER BY x.SortYear, x.SortMonth) AS Covered
                FROM (
                    SELECT mb.BillingYear SortYear, mb.BillingMonth SortMonth,
                           CONCAT(mb.BillingYear, '-', RIGHT(CONCAT('0', mb.BillingMonth), 2)) Period
                    FROM PaymentHistoryBilling link JOIN MonthlyBilling mb ON mb.Id=link.MonthlyBillingId
                    WHERE link.PaymentHistoryId=ph.Id
                    UNION ALL
                    SELECT a.BillingYear, a.BillingMonth,
                           CONCAT(a.BillingYear, '-', RIGHT(CONCAT('0', a.BillingMonth), 2), ' (arrears)')
                    FROM PaymentHistoryArrears link JOIN StallOwnerArrears a ON a.Id=link.StallOwnerArrearsId
                    WHERE link.PaymentHistoryId=ph.Id
                ) x
            ) periods
            {where}
            ORDER BY ph.DatePaid, ph.Id
            """, connection);
        command.Parameters.Add(first.Name, first.Type).Value = first.Value;
        if (second.Name is not null) command.Parameters.Add(second.Name, second.Type).Value = second.Value;
        var result = new List<ReportPayment>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3), reader.GetDecimal(4),
                reader.GetDecimal(5), reader.GetString(6), reader.GetString(7)));
        return result;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken token)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Reporting requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        try { await connection.OpenAsync(token);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Reporting requires BPLS_Dev.");
            return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
