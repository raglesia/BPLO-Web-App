using BusinessPermitLicensingSystem.Web.Data;
using System.Data;
using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Reports;

public sealed record VehiclePaymentReport(int Id, string Vin, string Company, string Driver, string Plate,
    string SecRegistration, string DtiRegistration, bool Archived, int Year, string OrNumber,
    DateTime PaidAt, decimal AmountPaid, string Recorder, string RecorderPosition,
    DraftSnapshot? Assessment, string? AssessmentWarning)
{
    public bool AmountMismatch => Assessment is not null && Assessment.GrandTotal != AmountPaid;
}

public sealed record VehiclePaymentSearch(IReadOnlyList<VehiclePaymentReport> Items, int Total);

public sealed class VehiclePaymentReportService(IConfiguration configuration)
{
    public const int PageSize = 25;
    private const string SelectColumns = """
        h.Id, h.VIN, COALESCE(d.CompanyName,v.CompanyName), COALESCE(d.DriverName,v.DriverName),
        COALESCE(d.PlateNo,v.PlateNo), COALESCE(d.SECRegNo,v.SECRegNo), COALESCE(d.DTINumber,v.DTINumber),
        v.IsArchived, h.PermitYear, h.ORNumber, h.DatePaid, h.AmountPaid,
        COALESCE(d.RecorderName,u.FullName, CONCAT('User ', h.RecordedBy)), COALESCE(d.RecorderPosition,u.Position, ''),
        d.DetailsJson, d.GrandTotal
        """;
    private const string Joins = """
        FROM VehiclePermitHistory h
        INNER JOIN VehiclePermits v ON v.VIN=h.VIN
        LEFT JOIN Users u ON u.Id=h.RecordedBy
        LEFT JOIN VehiclePaymentAssessmentSnapshots d ON d.PaymentHistoryId=h.Id
        """;

    public async Task<VehiclePaymentSearch> SearchAsync(string? search, int? year, int page, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        string joins = await ReportJoinsAsync(connection, token);
        string term = (search ?? "").Trim();
        string pattern = "%" + term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        const string where = """
            WHERE (@term='' OR v.CompanyName LIKE @pattern OR v.DriverName LIKE @pattern OR
                   v.PlateNo LIKE @pattern OR h.VIN LIKE @pattern OR h.ORNumber LIKE @pattern)
              AND (@year IS NULL OR h.PermitYear=@year)
            """;
        await using var count = new SqlCommand($"SELECT COUNT(*) {joins} {where}", connection);
        AddSearch(count, term, pattern, year);
        int total = (int)(await count.ExecuteScalarAsync(token) ?? 0);
        page = Math.Clamp(page, 1, Math.Max(1, (total + PageSize - 1) / PageSize));
        await using var command = new SqlCommand($"""
            SELECT {SelectColumns} {joins} {where}
            ORDER BY h.DatePaid DESC, h.Id DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """, connection);
        AddSearch(command, term, pattern, year);
        command.Parameters.Add("@skip", SqlDbType.Int).Value = (page - 1) * PageSize;
        command.Parameters.Add("@take", SqlDbType.Int).Value = PageSize;
        var items = new List<VehiclePaymentReport>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) items.Add(Read(reader));
        return new(items, total);
    }

    public async Task<VehiclePaymentReport?> GetAsync(int id, CancellationToken token)
    {
        if (id <= 0) return null;
        await using var connection = await OpenAsync(token);
        string joins = await ReportJoinsAsync(connection, token);
        await using var command = new SqlCommand($"SELECT {SelectColumns} {joins} WHERE h.Id=@id", connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? Read(reader) : null;
    }

    private static VehiclePaymentReport Read(SqlDataReader reader)
    {
        DraftSnapshot? assessment = null;
        string? warning = null;
        if (!reader.IsDBNull(14))
        {
            try { assessment = VehicleFeeDraft.Read(reader.GetString(14), reader.GetDecimal(15)); }
            catch (Exception exception) when (exception is ArgumentException or OverflowException or
                InvalidOperationException or System.Text.Json.JsonException)
            { warning = "Payment-time assessment snapshot needs review. Recorded payment amount remains authoritative."; }
        }
        else warning = "Legacy/pre-snapshot payment: the original fee breakdown is unavailable. Recorded payment amount is preserved; current vehicle identity is shown. A Mayor’s Permit requires a trustworthy payment-time snapshot.";
        return new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.IsDBNull(5) ? "" : reader.GetString(5),
            reader.IsDBNull(6) ? "" : reader.GetString(6), !reader.IsDBNull(7) && reader.GetInt32(7) != 0,
            reader.GetInt32(8), reader.GetString(9), reader.GetDateTime(10), reader.GetDecimal(11),
            reader.GetString(12), reader.GetString(13), assessment, warning);
    }

    private static async Task<string> ReportJoinsAsync(SqlConnection connection, CancellationToken token)
    {
        await using var schema = new SqlCommand("SELECT OBJECT_ID('dbo.VehiclePaymentAssessmentSnapshots','U')", connection);
        if (await schema.ExecuteScalarAsync(token) is not DBNull) return Joins;
        // Never reconstruct an old settlement from the current draft, even when totals happen to match.
        return Joins.Replace("LEFT JOIN VehiclePaymentAssessmentSnapshots d ON d.PaymentHistoryId=h.Id", """
            LEFT JOIN (SELECT CAST(NULL AS int) PaymentHistoryId, CAST(NULL AS nvarchar(max)) DetailsJson,
                CAST(NULL AS decimal(18,2)) GrandTotal, CAST(NULL AS nvarchar(255)) CompanyName,
                CAST(NULL AS nvarchar(255)) DriverName, CAST(NULL AS nvarchar(255)) PlateNo,
                CAST(NULL AS nvarchar(255)) SECRegNo, CAST(NULL AS nvarchar(255)) DTINumber,
                CAST(NULL AS nvarchar(255)) RecorderName, CAST(NULL AS nvarchar(255)) RecorderPosition) d ON 1=0
            """);
    }

    private static void AddSearch(SqlCommand command, string term, string pattern, int? year)
    {
        command.Parameters.Add("@term", SqlDbType.NVarChar, 255).Value = term;
        command.Parameters.Add("@pattern", SqlDbType.NVarChar, 520).Value = pattern;
        command.Parameters.Add("@year", SqlDbType.Int).Value = year is null ? DBNull.Value : year.Value;
    }

    private Task<SqlConnection> OpenAsync(CancellationToken token)
        => DatabaseConnection.OpenAsync(configuration, token);
}

public static class VehiclePaymentSelection
{
    public const string MaximumMessage = "You can print up to 3 vehicle payment reports at a time.";

    public static string? Validate(IReadOnlyList<int> ids, bool requireOne = false)
    {
        if (requireOne && ids.Count == 0) return "Select at least one recorded vehicle payment.";
        if (ids.Any(x => x <= 0)) return "A selected payment identifier is invalid.";
        if (ids.Count != ids.Distinct().Count()) return "The same annual payment cannot be selected twice.";
        if (ids.Count > 3) return MaximumMessage;
        return null;
    }
}
