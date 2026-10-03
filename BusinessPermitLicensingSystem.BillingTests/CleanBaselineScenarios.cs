using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class CleanBaselineScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

    public static async Task RunAsync(string baseUrl)
    {
        var root = new Uri(baseUrl);
        if (!root.IsLoopback) throw new InvalidOperationException("Use main localhost only.");
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var profiles = new ProfileService(settings);
        var billing = new BillingService(settings);
        var reports = new ReportService(settings);
        await using var sql = new SqlConnection(ConnectionString);
        await sql.OpenAsync();
        async Task<int> Number(string query)
        {
            await using var command = new SqlCommand(query, sql);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        Check(await Number("SELECT CASE WHEN DB_NAME()='BPLS_Dev' THEN 1 ELSE 0 END") == 1, "development database guard");
        var rows = new List<(string Sin, string Name)>();
        await using (var command = new SqlCommand("SELECT SIN,FullName FROM Profiling WHERE SIN BETWEEN 'SIN-2026-0001' AND 'SIN-2026-0538' ORDER BY FullName,SIN", sql))
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) rows.Add((reader.GetString(0), reader.GetString(1)));
        Check(rows.Count == 538 && rows.Select((x, i) => x.Sin == $"SIN-2026-{i + 1:D4}").All(x => x),
            "538 alphabetical SINs with no gaps");
        Check(await Number("SELECT COUNT(*) FROM Profiling WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200'") == 0,
            "old official identifiers absent");

        using var accounts = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetTempPath(), "BPLS_Dev.test-accounts.json")));
        var account = accounts.RootElement.EnumerateArray().First(x => x.GetProperty("Kind").GetString() == "PBKDF2");
        using var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = root };
        string login = await client.GetStringAsync("/Account/Login");
        string token = WebUtility.HtmlDecode(Regex.Match(login, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        using var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["__RequestVerificationToken"] = token, ["Input.Username"] = account.GetProperty("Username").GetString()!,
            ["Input.Password"] = account.GetProperty("Password").GetString()! }));
        Check(loginResponse.RequestMessage!.RequestUri!.AbsolutePath == "/", "authenticated localhost login");
        foreach (var batch in rows.Chunk(8))
            await Task.WhenAll(batch.Select(async row =>
            {
                string details = await client.GetStringAsync($"/Profiles/Details/{row.Sin}");
                string report = WebUtility.HtmlDecode(await client.GetStringAsync($"/Reports/RentalPaymentPreview?selected={row.Sin}"));
                if (!details.Contains(row.Sin) || !report.Contains(row.Sin) || !report.Contains("December 2026") ||
                    report.Contains(".ToString(") || !Regex.IsMatch(report, @"Additional Charges</th><td>₱[\d,]+\.\d{2}</td>") ||
                    report.Contains("Treasury-Verified Arrears</h3>"))
                    throw new Exception("Migrated route or currency rendering failed: " + row.Sin);
            }));
        Check(true, "all 538 detail/report URLs resolve; December-only reports and currency rendering");
        foreach (string sin in new[] { rows[0].Sin, rows[269].Sin, rows[^1].Sin })
            Check((await client.GetStringAsync("/Profiles/Index?search=" + sin)).Contains($"/Profiles/Details/{sin}"), "new SIN search " + sin);

        int user = await Number("SELECT TOP(1) Id FROM Users ORDER BY Id");
        string reference = "RESET-QA-" + Guid.NewGuid().ToString("N");
        const string migrated = "SIN-2026-0001";
        string? created = null;
        try
        {
            var result = await billing.SaveVerifiedArrearsAsync(migrated, null, 2026, 10, 1000m, 100m,
                reference, user, new DateTime(2026, 12, 22), default);
            Check(result.Success, "Treasury entry accepts migrated SIN");
            var view = (await billing.GetAsync(migrated, new DateTime(2026, 12, 1), default))!;
            Check(view.Rows.Count == 1 && view.Rows[0].Month == 12 && view.Arrears.Count == 1 &&
                view.Arrears[0].Total(new DateTime(2026, 12, 22)) == 1350m && view.Payments.Count == 0,
                "October remains arrears only; 250 base-only penalty; no payment");
            string revised = await client.GetStringAsync($"/Reports/RentalPaymentPreview?selected={migrated}");
            Check(revised.Contains("Treasury-Verified Arrears") && revised.Contains("October 2026") && !revised.Contains(".ToString("),
                "migrated report reprint reads verified arrears");
            int expected = await Number($"SELECT COALESCE(MAX(CAST(RIGHT(SIN,4) AS int)),0)+1 FROM Profiling WHERE SIN LIKE 'SIN-{DateTime.Now.Year}-%'");
            var save = await profiles.CreateAsync(new ProfileInput { FullName = "Baseline Allocation Verification",
                BusinessName = reference, BusinessSection = "Corridor", StallNumber = reference, StallSize = "0",
                PaymentStatus = "Unpaid", StartDate = new DateTime(2026, 9, 15) }, user, default);
            created = save.Sin;
            Check(save.Success && created == $"SIN-{DateTime.Now.Year}-{expected:D4}", "normal allocation returns next safe SIN " + created);
            var profile = (await profiles.GetAsync(created!, default))!;
            var newReport = (await reports.ProfileAsync(created!, default))!;
            Check(!profile.IsLegacyBaseline && newReport.AssessmentBills(new DateTime(2026, 10, 4)).All(x => x.Month == 10),
                "new profile remains non-legacy; occupancy month excluded");
        }
        finally
        {
            await using var cleanup = new SqlCommand("""
                SET XACT_ABORT ON; BEGIN TRANSACTION;
                IF EXISTS(SELECT 1 FROM StallOwnerArrears WHERE SIN=@sin AND TreasuryReference=@reference AND IsPaid=1)
                    THROW 50000,'QA arrears was paid; cleanup requires review',1;
                DELETE FROM StallOwnerArrears WHERE SIN=@sin AND TreasuryReference=@reference;
                DELETE FROM AuditTrail WHERE SIN=@sin AND Action='Add Verified Arrears' AND Details LIKE @details;
                DELETE FROM AuditTrail WHERE SIN=@created;
                DELETE FROM Profiling WHERE SIN=@created AND BusinessName=@reference;
                COMMIT TRANSACTION;
                """, sql);
            cleanup.Parameters.AddWithValue("@sin", migrated);
            cleanup.Parameters.AddWithValue("@reference", reference);
            cleanup.Parameters.AddWithValue("@details", "%reference " + reference);
            cleanup.Parameters.AddWithValue("@created", (object?)created ?? DBNull.Value);
            await cleanup.ExecuteNonQueryAsync();
        }
        Check(await Number("SELECT COUNT(*) FROM StallOwnerArrears WHERE SIN BETWEEN 'SIN-2026-0001' AND 'SIN-2026-0538'") == 0 &&
              await Number("SELECT COUNT(*) FROM PaymentHistory WHERE SIN BETWEEN 'SIN-2026-0001' AND 'SIN-2026-0538'") == 0,
              "official population restored clean after temporary Treasury QA");
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL clean baseline " + label);
        Console.WriteLine("PASS clean baseline " + label);
    }
}
