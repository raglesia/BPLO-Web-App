using System.Text;
using BusinessPermitLicensingSystem.Web.Archive;
using BusinessPermitLicensingSystem.Web.Transfer;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class TransferScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private static void Check(bool yes, string label) { if (!yes) throw new Exception(label); Console.WriteLine("PASS " + label); }
    public static async Task RunAsync()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var service = new TransferService(configuration);
        var archive = new ArchiveService(configuration);
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        Check((string?)await Scalar("SELECT DB_NAME()") == "BPLS_Dev", "transfer tests use BPLS_Dev");
        int user = (int)(await Scalar("SELECT TOP (1) Id FROM Users ORDER BY Id") ?? throw new Exception("No test user"));
        int billsBefore = (int)(await Scalar("SELECT COUNT(*) FROM MonthlyBilling") ?? 0);
        int paymentsBefore = (int)(await Scalar("SELECT COUNT(*) FROM PaymentHistory") ?? 0);
        int permitsBefore = (int)(await Scalar("SELECT COUNT(*) FROM VehiclePermitHistory") ?? 0);
        int draftsBefore = (int)(await Scalar("SELECT COUNT(*) FROM VehiclePermitFeeDrafts") ?? 0);
        int auditsBefore = (int)(await Scalar("SELECT COUNT(*) FROM AuditTrail") ?? 0);
        string run = Guid.NewGuid().ToString("N")[..10];
        string stall = BitConverter.ToUInt32(Guid.NewGuid().ToByteArray()).ToString();
        string sin1 = "SIN-IMPTEST-" + run + "-1", sin2 = "SIN-IMPTEST-" + run + "-2";
        string plate1 = "IMP-" + run + "-1", plate2 = "IMP-" + run + "-2";
        string section = (string)(await Scalar("SELECT TOP (1) Section FROM RentalRates ORDER BY Section") ?? throw new Exception("No rental rate"));
        string header = string.Join(',', TransferService.ProfileHeaders);
        string profileRow(string sin, string name, string date = "", string business = "Synthetic Import Business") =>
            string.Join(',', new[] { sin, name, business, section, stall, "2", "100", "Unverified", date, "0", "0" });
        string csv = string.Join('\n', new[] { header, profileRow(sin1, "Synthetic Import Owner"),
            profileRow(sin1, "Synthetic Import Owner"), profileRow("SIN-BAD-" + run, ""),
            profileRow("SIN-DATE-" + run, "Synthetic Date Owner", "not-a-date") });
        var mixedProfile = await service.ImportAsync(File("profiles.csv", Encoding.UTF8.GetBytes(csv)), false, default);
        Check(mixedProfile.Total == 4 && mixedProfile.Imported == 1 && mixedProfile.Duplicates == 1 && mixedProfile.Invalid == 2,
            "profile CSV mixed rows, duplicate SIN, missing value, invalid date");
        Check((int)(await Scalar("SELECT COUNT(*) FROM Profiling WHERE SIN=@sin", ("@sin", sin1)) ?? 0) == 1,
            "import keeps supplied SIN");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Profiles");
            for (int i = 0; i < TransferService.ProfileHeaders.Length; i++) sheet.Cell(1, i + 1).Value = TransferService.ProfileHeaders[i];
            string[] data = profileRow(sin2, "Synthetic Excel Owner", business: "=SUM(1,1)").Split(',');
            // Business contains a comma: enter the cells directly instead of parsing the CSV helper line.
            data = [sin2, "Synthetic Excel Owner", "=SUM(1,1)", section, (ulong.Parse(stall) + 1).ToString(), "2", "100", "Unverified", "", "0", "0"];
            for (int i = 0; i < data.Length; i++) sheet.Cell(2, i + 1).SetValue(data[i]);
            using var output = new MemoryStream(); workbook.SaveAs(output);
            var excel = await service.ImportAsync(File("profiles.xlsx", output.ToArray()), false, default);
            Check(excel.Imported == 1 && excel.Total == 1, "profile XLSX import");
        }
        try { await service.ImportAsync(File("bad.csv", Encoding.UTF8.GetBytes("SIN,FullName\nx,y")), false, default); throw new Exception("Missing header accepted"); }
        catch (ArgumentException exception) { Check(exception.Message.Contains("Missing required column"), "missing profile header rejected"); }
        string vehicleHeader = string.Join(',', TransferService.VehicleHeaders);
        string vehicleCsv = string.Join('\n', new[] { vehicleHeader,
            $"Synthetic Import Company,Synthetic Driver,{plate1},,",
            $"Synthetic Import Company,Synthetic Driver,{plate1},,",
            "Synthetic Import Company,Synthetic Driver,,,",
            $"Synthetic Import Company,Synthetic Driver,{plate2},," });
        var vehicles = await service.ImportAsync(File("vehicles.csv", Encoding.UTF8.GetBytes(vehicleCsv)), true, default);
        Check(vehicles.Total == 4 && vehicles.Imported == 2 && vehicles.Duplicates == 1 && vehicles.Invalid == 1,
            "vehicle CSV mixed rows, duplicate plate, missing plate");
        Check((int)(await Scalar("SELECT COUNT(*) FROM VehiclePermits WHERE PlateNo IN (@p1,@p2)", ("@p1", plate1), ("@p2", plate2)) ?? 0) == 2,
            "vehicle import generates two records");
        Check((int)(await Scalar("SELECT COUNT(*) FROM VehiclePermits WHERE PlateNo=@plate AND PermitStatus='Unpaid' AND PermitYear=0", ("@plate", plate1)) ?? 0) == 1,
            "vehicle import leaves permit state unpaid with no year");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Vehicles");
            for (int i = 0; i < TransferService.VehicleHeaders.Length; i++) sheet.Cell(1, i + 1).Value = TransferService.VehicleHeaders[i];
            sheet.Cell(1, 6).Value = "PermitStatus"; sheet.Cell(1, 7).Value = "PermitYear";
            string[] data = ["Synthetic Excel Vehicle", "Synthetic Driver", "IMP-" + run + "-X", "", "", "Paid", "2025"];
            for (int i = 0; i < data.Length; i++) sheet.Cell(2, i + 1).SetValue(data[i]);
            using var output = new MemoryStream(); workbook.SaveAs(output);
            var excel = await service.ImportAsync(File("vehicles.xlsx", output.ToArray()), true, default);
            Check(excel.Imported == 1 &&
                (int)(await Scalar("SELECT COUNT(*) FROM VehiclePermits WHERE PlateNo=@plate AND PermitStatus='Unpaid' AND PermitYear=0", ("@plate", "IMP-" + run + "-X")) ?? 0) == 1,
                "vehicle XLSX import ignores unsupported paid status/year columns");
        }
        try { await service.ImportAsync(File("bad-vehicles.csv", Encoding.UTF8.GetBytes("CompanyName,PlateNo\nA,B")), true, default); throw new Exception("Missing header accepted"); }
        catch (ArgumentException exception) { Check(exception.Message.Contains("Missing required column"), "missing vehicle header rejected"); }
        string concurrentA = "IMP-" + run + "-A", concurrentB = "IMP-" + run + "-B";
        var tasks = await Task.WhenAll(
            service.ImportAsync(File("a.csv", Encoding.UTF8.GetBytes(vehicleHeader + "\nA,D," + concurrentA + ",,")), true, default),
            service.ImportAsync(File("b.csv", Encoding.UTF8.GetBytes(vehicleHeader + "\nB,D," + concurrentB + ",,")), true, default));
        Check(tasks.All(x => x.Imported == 1) &&
            (int)(await Scalar("SELECT COUNT(DISTINCT VIN) FROM VehiclePermits WHERE PlateNo IN (@p1,@p2)", ("@p1", concurrentA), ("@p2", concurrentB)) ?? 0) == 2,
            "concurrent imports allocate distinct VINs");
        var profileExport = await service.ExportAsync(false, sin1, default);
        using (var workbook = new XLWorkbook(new MemoryStream(profileExport)))
        {
            var sheet = workbook.Worksheet(1);
            Check(sheet.Cell(3, 1).GetString() == "SIN" && sheet.Cell(3, 11).GetString() == "Date of Occupancy" &&
                sheet.Cell(4, 1).GetString() == sin1 && sheet.Cell(5, 1).IsEmpty(),
                "profile export columns, values, and search filtering");
        }
        using (var workbook = new XLWorkbook(new MemoryStream(await service.ExportAsync(false, sin2, default))))
        { var cell = workbook.Worksheet(1).Cell(4, 3); Check(cell.GetString() == "=SUM(1,1)" && !cell.HasFormula, "exported text is not a formula"); }
        using (var workbook = new XLWorkbook(new MemoryStream(await service.ExportAsync(true, plate1, default))))
        { var sheet = workbook.Worksheet(1); Check(sheet.Cell(3, 1).GetString() == "VIN" && sheet.Cell(3, 9).GetString() == "Date Added" && string.Equals(sheet.Cell(4, 4).GetString(), plate1, StringComparison.OrdinalIgnoreCase) && sheet.Cell(5, 1).IsEmpty(), "vehicle export columns, values, and search filtering"); }
        Check((await archive.ProfileAsync(sin1, true, user, default)).Success, "archive imported profile for export check");
        using (var workbook = new XLWorkbook(new MemoryStream(await service.ExportAsync(false, sin1, default))))
            Check(workbook.Worksheet(1).Cell(4, 1).IsEmpty(), "profile export excludes archived records");
        Check((await archive.ProfileAsync(sin1, false, user, default)).Success, "restore imported profile after export check");
        string importedVin = (string)(await Scalar("SELECT VIN FROM VehiclePermits WHERE PlateNo=@plate", ("@plate", plate1)) ?? throw new Exception("Imported vehicle missing"));
        Check((await archive.VehicleAsync(importedVin, true, user, default)).Success, "archive imported vehicle for export check");
        using (var workbook = new XLWorkbook(new MemoryStream(await service.ExportAsync(true, plate1, default))))
            Check(workbook.Worksheet(1).Cell(4, 1).IsEmpty(), "vehicle export excludes archived records");
        Check((await archive.VehicleAsync(importedVin, false, user, default)).Success, "restore imported vehicle after export check");
        Check((int)(await Scalar("SELECT COUNT(*) FROM MonthlyBilling") ?? 0) == billsBefore &&
              (int)(await Scalar("SELECT COUNT(*) FROM PaymentHistory") ?? 0) == paymentsBefore &&
              (int)(await Scalar("SELECT COUNT(*) FROM VehiclePermitHistory") ?? 0) == permitsBefore &&
              (int)(await Scalar("SELECT COUNT(*) FROM VehiclePermitFeeDrafts") ?? 0) == draftsBefore,
              "imports create no billing, payment, permit history, or fee drafts");
        Check((int)(await Scalar("SELECT COUNT(*) FROM AuditTrail") ?? 0) == auditsBefore + 4,
            "bulk imports create no audit rows; archive and restore retain their audit behavior");

        async Task<object?> Scalar(string sql, params (string, object)[] args)
        { await using var command = new SqlCommand(sql, connection); foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value); return await command.ExecuteScalarAsync(); }
    }
    private static FormFile File(string name, byte[] bytes) => new(new MemoryStream(bytes), 0, bytes.Length, "Upload", name);
}
