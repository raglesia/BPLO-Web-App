using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Transfer;

public sealed record ImportIssue(int Row, string Kind, string Reason);
public sealed record ImportResult(int Total, int Imported, IReadOnlyList<ImportIssue> Issues)
{
    public int Duplicates => Issues.Count(x => x.Kind == "Duplicate");
    public int Invalid => Issues.Count(x => x.Kind == "Invalid");
    public int Failed => Issues.Count(x => x.Kind == "Failed");
    public int Skipped => Issues.Count(x => x.Kind == "Skipped");
}

public sealed class TransferService(IConfiguration configuration)
{
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public static readonly string[] ProfileHeaders = ["SIN", "FullName", "BusinessName", "BusinessSection", "StallNumber", "StallSize", "MonthlyRental", "PaymentStatus", "StartDate", "Penalty", "AdditionalCharge"];
    public static readonly string[] VehicleHeaders = ["CompanyName", "DriverName", "PlateNo", "SECRegNo", "DTINumber"];
    private static readonly string[] ProfileExportHeaders = ["SIN", "Full Name", "Business Name", "Business Section", "Stall Number", "Stall Size", "Monthly Rental", "Payment Status", "Penalty", "Additional Charge", "Date of Occupancy"];
    private static readonly string[] VehicleExportHeaders = ["VIN", "Company Name", "Driver Name", "Plate No", "SEC Reg No", "DTI Number", "Permit Status", "Permit Year", "Date Added"];

    public async Task<ImportResult> ImportAsync(IFormFile file, bool vehicle, CancellationToken token)
    {
        if (file.Length == 0 || file.Length > MaxFileBytes) throw new ArgumentException("Choose a nonempty CSV or XLSX file no larger than 5 MB.");
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".csv" or ".xlsx")) throw new ArgumentException("Only CSV and XLSX files are supported.");
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, token);
        if (stream.Length != file.Length || stream.Length > MaxFileBytes) throw new ArgumentException("Upload size changed or exceeds 5 MB.");
        stream.Position = 0;
        if (extension == ".xlsx" && !IsZip(stream)) throw new ArgumentException("The XLSX file is not a valid workbook.");
        if (extension == ".csv" && IsZip(stream)) throw new ArgumentException("The CSV file is not text.");
        stream.Position = 0;
        var rows = extension == ".csv" ? ReadCsv(stream) : ReadExcel(stream);
        string[] headers = vehicle ? VehicleHeaders : ProfileHeaders;
        var missing = headers.Where(h => !rows.Headers.Contains(h, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (missing.Length > 0) throw new ArgumentException("Missing required column(s): " + string.Join(", ", missing));
        await using var connection = await OpenAsync(token);
        var issues = new List<ImportIssue>();
        int imported = 0;
        foreach (var row in rows.Rows)
        {
            if (row.Values.Values.All(string.IsNullOrWhiteSpace)) { issues.Add(new(row.Number, "Skipped", "Empty row.")); continue; }
            string? error = vehicle ? ValidateVehicle(row.Values) : await ValidateProfileAsync(connection, row.Values, token);
            if (error is not null) { issues.Add(new(row.Number, "Invalid", error)); continue; }
            try
            {
                if (vehicle) await InsertVehicleAsync(connection, row.Values, token);
                else await InsertProfileAsync(connection, row.Values, token);
                imported++;
            }
            catch (SqlException exception) when (exception.Number is 2601 or 2627)
            { issues.Add(new(row.Number, "Duplicate", vehicle ? "VIN or plate already exists." : "SIN or profile already exists.")); }
            catch (SqlException)
            { issues.Add(new(row.Number, "Failed", "Database rejected this row. Check field lengths and values.")); }
            catch (InvalidOperationException exception)
            { issues.Add(new(row.Number, "Failed", exception.Message == "VIN sequence is full for this year." ? exception.Message : "Row could not be saved.")); }
        }
        return new(rows.Rows.Count, imported, issues);
    }

    public async Task<byte[]> ExportAsync(bool vehicle, string? search, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        string term = (search ?? "").Trim();
        if (term.Length > 100) term = term[..100];
        string pattern = "%" + term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        string sql = vehicle ? """
            SELECT VIN, CompanyName, DriverName, PlateNo, SECRegNo, DTINumber, PermitStatus, PermitYear,
                   CONVERT(varchar(10), DateAdded, 101)
            FROM VehiclePermits WHERE IsArchived=0 AND
            (@term='' OR VIN LIKE @pattern OR CompanyName LIKE @pattern OR DriverName LIKE @pattern OR PlateNo LIKE @pattern OR SECRegNo LIKE @pattern OR DTINumber LIKE @pattern)
            ORDER BY VIN
            """ : """
            SELECT SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize,
                   MonthlyRental, PaymentStatus, Penalty, AdditionalCharge,
                   CASE WHEN TRY_CONVERT(date, StartDate, 23) IS NOT NULL
                        THEN CONVERT(varchar(10), TRY_CONVERT(date, StartDate, 23), 101) ELSE '' END
            FROM Profiling WHERE IsArchived=0 AND
            (@term='' OR SIN LIKE @pattern OR FullName LIKE @pattern OR BusinessName LIKE @pattern OR BusinessSection LIKE @pattern OR StallNumber LIKE @pattern OR StallSize LIKE @pattern OR CONVERT(nvarchar(50), MonthlyRental) LIKE @pattern OR PaymentStatus=@term OR CONVERT(nvarchar(50), Penalty) LIKE @pattern OR CONVERT(nvarchar(10), TRY_CONVERT(date, StartDate, 23), 101) LIKE @pattern)
            ORDER BY SIN
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = term;
        command.Parameters.Add("@pattern", SqlDbType.NVarChar, 512).Value = pattern;
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(vehicle ? "Vehicle Permits" : "Profiles");
        var headers = vehicle ? VehicleExportHeaders : ProfileExportHeaders;
        sheet.Cell(1, 1).Value = vehicle ? "Special Vehicle Permit Records" : "Stall Owners Profiling Report";
        sheet.Range(1, 1, 1, headers.Length).Merge();
        for (int i = 0; i < headers.Length; i++) sheet.Cell(3, i + 1).Value = headers[i];
        int row = 4;
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(row, i + 1);
                if (reader.IsDBNull(i)) { cell.SetValue(""); continue; }
                if (reader.GetFieldType(i) == typeof(decimal)) cell.SetValue(reader.GetDecimal(i));
                else if (reader.GetFieldType(i) == typeof(int)) cell.SetValue(reader.GetInt32(i));
                else cell.SetValue(reader.GetValue(i).ToString() ?? "");
            }
            row++;
        }
        sheet.Columns().AdjustToContents();
        using var output = new MemoryStream(); workbook.SaveAs(output); return output.ToArray();
    }

    private static (HashSet<string> Headers, List<(int Number, Dictionary<string, string> Values)> Rows) ReadCsv(Stream stream)
    {
        using var text = new StreamReader(stream, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvReader(text, new CsvConfiguration(CultureInfo.InvariantCulture) { BadDataFound = null, MissingFieldFound = null });
        if (!csv.Read()) throw new ArgumentException("File has no header row.");
        csv.ReadHeader();
        var headers = new HashSet<string>((csv.HeaderRecord ?? []).Select(h => h.Trim()), StringComparer.OrdinalIgnoreCase);
        var rows = new List<(int, Dictionary<string, string>)>();
        while (csv.Read())
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string header in headers) values[header] = csv.GetField(header) ?? "";
            rows.Add((csv.Parser.Row, values));
        }
        return (headers, rows);
    }

    private static (HashSet<string> Headers, List<(int Number, Dictionary<string, string> Values)> Rows) ReadExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);
        var headers = new Dictionary<int, string>();
        foreach (var cell in sheet.Row(1).CellsUsed()) headers[cell.Address.ColumnNumber] = cell.GetString().Trim();
        var rows = new List<(int, Dictionary<string, string>)>();
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (column, header) in headers) if (header.Length > 0) values[header] = row.Cell(column).GetFormattedString();
            rows.Add((row.RowNumber(), values));
        }
        return (new HashSet<string>(headers.Values, StringComparer.OrdinalIgnoreCase), rows);
    }

    private static string? ValidateVehicle(Dictionary<string, string> values)
    {
        foreach (string field in new[] { "CompanyName", "PlateNo" }) if (Value(values, field).Length == 0) return field + " is required.";
        if (Value(values, "CompanyName").Length > 255 || Value(values, "DriverName").Length > 255 ||
            new[] { "PlateNo", "SECRegNo", "DTINumber" }.Any(f => Value(values, f).Length > 100)) return "A field exceeds its database length.";
        return null;
    }

    private static async Task<string?> ValidateProfileAsync(SqlConnection connection, Dictionary<string, string> values, CancellationToken token)
    {
        foreach (string field in new[] { "SIN", "FullName", "BusinessName", "BusinessSection", "StallNumber", "StallSize", "MonthlyRental", "PaymentStatus" })
            if (Value(values, field).Length == 0) return field + " is required.";
        if (Value(values, "SIN").Length > 100 || Value(values, "FullName").Length > 255 || Value(values, "BusinessName").Length > 255 ||
            Value(values, "BusinessSection").Length > 255 || Value(values, "StallNumber").Length > 100 || Value(values, "StallSize").Length > 100)
            return "A field exceeds its database length.";
        if (!Regex.IsMatch(Value(values, "FullName"), @"^[\p{L}. ]+$")) return "FullName may contain only letters, spaces, or periods.";
        if (!Regex.IsMatch(Value(values, "StallNumber"), @"^[0-9,]+$")) return "StallNumber may contain only digits and commas.";
        string status = Value(values, "PaymentStatus");
        if (status is not ("Unverified" or "Unpaid" or "Paid")) return "PaymentStatus must be Unverified, Unpaid, or Paid.";
        string date = Value(values, "StartDate");
        if (date.Length > 0 && !DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) return "Invalid StartDate.";
        if (status != "Unverified" && date.Length == 0) return "StartDate is required for a verified profile.";
        foreach (string field in new[] { "MonthlyRental", "Penalty", "AdditionalCharge" })
            if (!decimal.TryParse(MoneyValue(values, field), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount) || amount < 0 || amount > 9999999999999999.99m || decimal.Round(amount, 2) != amount)
                return field + " must be a nonnegative amount with at most two decimals.";
        await using var rate = new SqlCommand("SELECT RateType FROM RentalRates WHERE Section=@section", connection);
        rate.Parameters.Add("@section", SqlDbType.NVarChar, 255).Value = Value(values, "BusinessSection");
        string? rateType = (string?)await rate.ExecuteScalarAsync(token);
        if (rateType is null) return "BusinessSection does not exist.";
        if (rateType != "Flat" && (!decimal.TryParse(Value(values, "StallSize"), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal size) || size <= 0))
            return "StallSize must be a positive number.";
        return null;
    }

    private static async Task InsertProfileAsync(SqlConnection connection, Dictionary<string, string> values, CancellationToken token)
    {
        await using var command = new SqlCommand("""
            INSERT INTO Profiling (SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize,
                MonthlyRental, PaymentStatus, StartDate, Penalty, AdditionalCharge, IsArchived)
            VALUES (@sin, @name, @business, @section, @stall, @size, @rental, @status, @date, @penalty, @additional, 0)
            """, connection);
        foreach (var (name, field, size) in new[] { ("@sin", "SIN", 100), ("@name", "FullName", 255), ("@business", "BusinessName", 255),
            ("@section", "BusinessSection", 255), ("@stall", "StallNumber", 100), ("@size", "StallSize", 100), ("@status", "PaymentStatus", 50) })
            command.Parameters.Add(name, SqlDbType.NVarChar, size).Value = Value(values, field);
        string rawDate = Value(values, "StartDate");
        command.Parameters.Add("@date", SqlDbType.NVarChar, 50).Value = rawDate.Length == 0 ? "" : DateTime.Parse(rawDate, CultureInfo.InvariantCulture).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        foreach (var (name, field) in new[] { ("@rental", "MonthlyRental"), ("@penalty", "Penalty"), ("@additional", "AdditionalCharge") })
        { var p = command.Parameters.Add(name, SqlDbType.Decimal); p.Precision = 18; p.Scale = 2; p.Value = decimal.Parse(MoneyValue(values, field), CultureInfo.InvariantCulture); }
        await command.ExecuteNonQueryAsync(token);
    }

    private static async Task InsertVehicleAsync(SqlConnection connection, Dictionary<string, string> values, CancellationToken token)
    {
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            int year = DateTime.Today.Year;
            await using var next = new SqlCommand("""
                SELECT ISNULL(MAX(TRY_CONVERT(int, RIGHT(VIN, 4))), 0) + 1
                FROM VehiclePermits WITH (UPDLOCK, HOLDLOCK) WHERE VIN LIKE @prefix
                """, connection, transaction);
            next.Parameters.Add("@prefix", SqlDbType.NVarChar, 100).Value = $"VIN-{year}-%";
            int number = (int)(await next.ExecuteScalarAsync(token) ?? 1);
            if (number > 9999) throw new InvalidOperationException("VIN sequence is full for this year.");
            string vin = $"VIN-{year}-{number:D4}";
            await using var insert = new SqlCommand("""
                INSERT INTO VehiclePermits (VIN, CompanyName, DriverName, PlateNo, SECRegNo, DTINumber)
                VALUES (@vin, @company, @driver, @plate, @sec, @dti)
                """, connection, transaction);
            insert.Parameters.Add("@vin", SqlDbType.NVarChar, 100).Value = vin;
            foreach (var (name, field, size) in new[] { ("@company", "CompanyName", 255), ("@driver", "DriverName", 255),
                ("@plate", "PlateNo", 100), ("@sec", "SECRegNo", 100), ("@dti", "DTINumber", 100) })
                insert.Parameters.Add(name, SqlDbType.NVarChar, size).Value = field == "PlateNo" ? Value(values, field).ToUpperInvariant() : Value(values, field);
            await insert.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
        }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken token)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Transfer requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        try { await connection.OpenAsync(token);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Transfer requires BPLS_Dev.");
            return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static bool IsZip(Stream stream)
    { int a = stream.ReadByte(), b = stream.ReadByte(); stream.Position = 0; return a == 'P' && b == 'K'; }
    private static string Value(Dictionary<string, string> row, string field) => row.TryGetValue(field, out var value) ? value.Trim() : "";
    private static string MoneyValue(Dictionary<string, string> row, string field) =>
        field is "Penalty" or "AdditionalCharge" && Value(row, field).Length == 0 ? "0" : Value(row, field);
}
