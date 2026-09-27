using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using BusinessPermitLicensingSystem.Web.Transfer;
using Microsoft.Data.SqlClient;

internal static class BrowserScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    private static readonly Regex TokenPattern = new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex SinPattern = new(@"SIN-\d{4}-\d{4}", RegexOptions.Compiled);
    private static readonly Regex VinPattern = new(@"VIN-\d{4}-\d{4,}", RegexOptions.Compiled);
    private static readonly Regex AnchorPattern = new("<a\\b[^>]*href=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private sealed record Account(string Username, string Password, string FullName, string Kind);
    private sealed record Page(string Html, Uri Url, HttpStatusCode Status);
    private sealed record Downloaded(byte[] Bytes, string Type, HttpStatusCode Status);

    private sealed class Browser : IDisposable
    {
        private readonly CookieContainer cookies = new();
        private readonly HttpClient client;
        public Uri Root { get; }
        public Browser(Uri root)
        {
            Root = root;
            client = new(new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = true });
        }
        public void SetCookie(string name, string value) => cookies.Add(Root, new Cookie(name, value));

        public async Task<Page> Get(string path)
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri(Root, path));
            return new(await response.Content.ReadAsStringAsync(), response.RequestMessage!.RequestUri!, response.StatusCode);
        }

        public async Task<Downloaded> Download(string path)
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri(Root, path));
            return new(await response.Content.ReadAsByteArrayAsync(),
                response.Content.Headers.ContentType?.MediaType ?? "", response.StatusCode);
        }

        public async Task<Page> Post(string path, Page source, params (string Key, string Value)[] fields)
        {
            var data = fields.ToDictionary(x => x.Key, x => x.Value);
            data["__RequestVerificationToken"] = Token(source);
            using var content = new FormUrlEncodedContent(data);
            using HttpResponseMessage response = await client.PostAsync(new Uri(Root, path), content);
            return new(await response.Content.ReadAsStringAsync(), response.RequestMessage!.RequestUri!, response.StatusCode);
        }

        public async Task<Page> PostWithoutToken(string path, params (string Key, string Value)[] fields)
        {
            using var content = new FormUrlEncodedContent(fields.ToDictionary(x => x.Key, x => x.Value));
            using HttpResponseMessage response = await client.PostAsync(new Uri(Root, path), content);
            return new(await response.Content.ReadAsStringAsync(), response.RequestMessage!.RequestUri!, response.StatusCode);
        }

        public async Task<Page> Upload(string path, Page source, string filename, byte[] bytes)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(Token(source)), "__RequestVerificationToken");
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue(filename.EndsWith(".xlsx") ?
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv");
            content.Add(file, "Upload", filename);
            using HttpResponseMessage response = await client.PostAsync(new Uri(Root, path), content);
            return new(await response.Content.ReadAsStringAsync(), response.RequestMessage!.RequestUri!, response.StatusCode);
        }

        public void Dispose() => client.Dispose();
    }

    private static string Token(Page page)
    {
        Match match = TokenPattern.Match(page.Html);
        if (!match.Success) throw new Exception("Antiforgery token missing on " + page.Url.AbsolutePath);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new Exception("FAIL " + label);
        Console.WriteLine("PASS browser " + label);
    }
    private static bool Has(Page page, string text) => page.Html.Contains(text, StringComparison.OrdinalIgnoreCase);
    private static string Unique() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    private static string[] Links(Page page) => AnchorPattern.Matches(page.Html)
        .Select(x => WebUtility.HtmlDecode(x.Groups[1].Value))
        .Where(x => x.StartsWith('/') && !x.StartsWith("//"))
        .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    private static byte[] Workbook(string[] headers, string[] values)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Import");
        for (int i = 0; i < headers.Length; i++)
        { sheet.Cell(1, i + 1).SetValue(headers[i]); sheet.Cell(2, i + 1).SetValue(values[i]); }
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }
    private static string Extract(Regex regex, Page page)
    {
        Match match = regex.Match(page.Html);
        if (!match.Success) throw new Exception("Expected identifier missing from " + page.Url);
        return match.Value;
    }
    private static async Task Login(Browser browser, Account account)
    {
        Page form = await browser.Get("/Account/Login");
        Page result = await browser.Post("/Account/Login", form,
            ("Input.Username", account.Username), ("Input.Password", account.Password));
        Check(result.Url.AbsolutePath == "/" && Has(result, account.FullName), "login " + account.Kind);
    }

    public static async Task RunAsync(string baseUrl)
    {
        Uri root = new(baseUrl.TrimEnd('/') + "/");
        if (!root.IsLoopback) throw new InvalidOperationException("Browser tests require a loopback server.");
        string accountsFile = Path.Combine(Path.GetTempPath(), "BPLS_Dev.test-accounts.json");
        Account[] accounts = JsonSerializer.Deserialize<Account[]>(await File.ReadAllTextAsync(accountsFile),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new Exception("No accounts");
        Account primary = accounts.First(x => x.Kind == "PBKDF2");
        Account secondary = accounts.Last(x => x.Kind == "PBKDF2");
        string tag = "PHASE12-" + Unique();
        await using var sql = new SqlConnection(ConnectionString);
        await sql.OpenAsync();
        await using (var check = new SqlCommand("SELECT DB_NAME()", sql))
            Check((string?)await check.ExecuteScalarAsync() == "BPLS_Dev", "database guard");
        async Task<int> Count(string query, params (string Key, object Value)[] values)
        {
            await using var command = new SqlCommand(query, sql);
            foreach (var (key, value) in values) command.Parameters.AddWithValue(key, value);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        using var anonymous = new Browser(root);
        using var invalidCookie = new Browser(root);
        invalidCookie.SetCookie("BPLS.Auth", "invalid-phase12-cookie");
        Check((await invalidCookie.Get("/Profiles/Index")).Url.AbsolutePath == "/Account/Login",
            "invalid auth cookie redirects cleanly");
        string legacyUser = "phase12_legacy_" + Unique();
        string legacyPassword = "P12!" + Guid.NewGuid().ToString("N");
        string legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(legacyPassword)));
        await using (var seedLegacy = new SqlCommand("""
            INSERT INTO Users(Username, FullName, Position, Password)
            VALUES(@name, 'Phase Twelve Legacy', 'Development', @hash)
            """, sql))
        {
            seedLegacy.Parameters.AddWithValue("@name", legacyUser);
            seedLegacy.Parameters.AddWithValue("@hash", legacyHash);
            await seedLegacy.ExecuteNonQueryAsync();
        }
        using (var legacyBrowser = new Browser(root))
            await Login(legacyBrowser, new Account(legacyUser, legacyPassword, "Phase Twelve Legacy", "fresh SHA256"));
        Check(await Count("SELECT COUNT(*) FROM Users WHERE Username=@name AND Password LIKE 'pbkdf2:%'",
            ("@name", legacyUser)) == 1, "legacy hash upgraded after valid login");
        Page protectedPage = await anonymous.Get("/Profiles/Index");
        Check(protectedPage.Url.AbsolutePath == "/Account/Login", "anonymous redirect");
        Page login = await anonymous.Get("/Account/Login");
        Page invalid = await anonymous.Post("/Account/Login", login,
            ("Input.Username", primary.Username), ("Input.Password", "wrong-password"));
        Page unknown = await anonymous.Post("/Account/Login", invalid,
            ("Input.Username", "unknown-" + tag), ("Input.Password", "wrong-password"));
        Check(Has(invalid, "Invalid username or password") && Has(unknown, "Invalid username or password"),
            "invalid and unknown credentials share message");
        using var first = new Browser(root);
        using var second = new Browser(root);
        await Login(first, primary);
        await Login(second, secondary);
        Check(Has(await first.Get("/Profiles/Index"), "Stall owners") &&
              Has(await second.Get("/Vehicles/Index"), "Vehicle permits"), "independent authenticated sessions");
        Page dashboard = await first.Get("/");
        string[] homeLinks = Links(dashboard);
        Check(homeLinks.Any(x => x.StartsWith("/Profiles")) &&
              homeLinks.Any(x => x.StartsWith("/Vehicles")) &&
              homeLinks.Any(x => x.StartsWith("/RentalRates")) &&
              homeLinks.Any(x => x.StartsWith("/AuditTrail")) &&
              homeLinks.Any(x => x.StartsWith("/Archive")) &&
              homeLinks.Any(x => x.StartsWith("/Reports")), "dashboard links reach all modules");
        foreach (string link in homeLinks)
        {
            Page destination = await first.Get(link);
            Check(destination.Status == HttpStatusCode.OK, "dashboard link " + link.Split('?')[0]);
        }
        foreach (string path in new[] { "/", "/Profiles/Index", "/Vehicles/Index", "/RentalRates", "/AuditTrail",
            "/Archive/Profiles", "/Archive/Vehicles", "/Profiles/Transfer", "/Vehicles/Transfer",
            "/Reports", "/Reports/Monthly" })
        {
            Page p = await first.Get(path);
            Check(p.Status == HttpStatusCode.OK && p.Url.AbsolutePath != "/Account/Login", "navigation " + path);
        }
        Page unsafeLogin = await anonymous.Post("/Account/Login", await anonymous.Get("/Account/Login"),
            ("Input.Username", primary.Username), ("Input.Password", primary.Password),
            ("ReturnUrl", "https://example.com/steal"));
        Check(unsafeLogin.Url.Host == root.Host && unsafeLogin.Url.AbsolutePath == "/", "external return URL rejected");

        string section = "Phase 12 Rate " + tag;
        Page rateForm = await first.Get("/RentalRates/Edit");
        Page rateSaved = await first.Post("/RentalRates/Edit", rateForm,
            ("Input.Section", section), ("Input.RateType", "PerSqm"), ("Input.RatePerSqm", "100"),
            ("Input.FlatRate", "0"), ("Input.Confirm", "true"));
        Check(rateSaved.Url.AbsolutePath == "/RentalRates" && Has(rateSaved, section), "rate create PRG");

        async Task<(string Sin, Page Page)> CreateProfile(Browser browser, string label, string sectionName,
            string size, string status = "Unpaid", string? start = "2025-01-31", string additional = "0")
        {
            Page form = await browser.Get("/Profiles/Create");
            string suffix = Unique();
            Page saved = await browser.Post("/Profiles/Create", form,
                ("Input.FullName", "Phase Twelve " + label), ("Input.BusinessName", tag + " " + label + " " + suffix),
                ("Input.BusinessSection", sectionName), ("Input.StallNumber", Random.Shared.Next(100000, 999999).ToString()),
                ("Input.StallSize", size), ("Input.PaymentStatus", status), ("Input.StartDate", start ?? ""),
                ("Input.IncludeAdditionalCharge", additional == "0" ? "false" : "true"),
                ("Input.AdditionalCharge", additional));
            string sin = Extract(SinPattern, saved);
            Check(saved.Url.AbsolutePath.Contains("/Profiles/Details/") && Has(saved, label), "profile create " + label);
            return (sin, saved);
        }

        var (sin, details) = await CreateProfile(first, "Alpha", section, "2", additional: "10.25");
        Check(Has(details, "210.25"), "profile rent and additional charge");
        Check(Has(await first.Get("/Profiles/Index?search=" + Uri.EscapeDataString(tag)), sin), "profile search");
        Page edit = await first.Get("/Profiles/Edit/" + sin);
        Page edited = await first.Post("/Profiles/Edit/" + sin, edit,
            ("Input.FullName", "Phase Twelve Updated"), ("Input.BusinessName", tag + " Updated"),
            ("Input.BusinessSection", section), ("Input.StallNumber", "123456"),
            ("Input.StallSize", "2"), ("Input.PaymentStatus", "Unpaid"),
            ("Input.StartDate", "2025-01-31"), ("Input.IncludeAdditionalCharge", "true"),
            ("Input.AdditionalCharge", "10.25"));
        Check(Has(edited, "Phase Twelve Updated") && edited.Url.AbsolutePath.Contains("/Profiles/Details/"),
            "profile edit PRG");
        Page billBefore = await first.Get($"/Profiles/{sin}/Billing");
        Check(Has(billBefore, "Billing history"), "billing page");
        Page generated = await first.Post($"/Profiles/{sin}/Billing?handler=Generate", billBefore);
        Check(Has(generated, "Generated") && generated.Url.AbsolutePath.EndsWith("/Billing"), "billing generation PRG");
        Check(Has(await first.Get($"/Profiles/{sin}/Billing"), "2025-02"), "first billable month after occupancy");
        Page repeated = await first.Post($"/Profiles/{sin}/Billing?handler=Generate", generated);
        Check(Has(repeated, "Generated 0"), "billing generation idempotent");

        Page rateEdit = await first.Get("/RentalRates/Edit/" + Uri.EscapeDataString(section));
        Page changed = await first.Post("/RentalRates/Edit/" + Uri.EscapeDataString(section), rateEdit,
            ("Input.RateType", "PerSqm"), ("Input.RatePerSqm", "150"),
            ("Input.FlatRate", "0"), ("Input.Confirm", "true"));
        Check(Has(changed, section), "rate update");
        Check(Has(await first.Get("/Profiles/Details/" + sin), "210.25"), "existing profile rent unchanged");
        var (newSin, newDetails) = await CreateProfile(first, "Newrate", section, "2");
        Check(Has(newDetails, "300.00"), "new profile uses updated rate");
        Page flatForm = await first.Get("/RentalRates/Edit");
        string flatSection = "Phase 12 Flat " + tag;
        Page flatSaved = await first.Post("/RentalRates/Edit", flatForm,
            ("Input.Section", flatSection), ("Input.RateType", "Flat"), ("Input.RatePerSqm", "0"),
            ("Input.FlatRate", "1200.25"), ("Input.Confirm", "true"));
        Check(Has(flatSaved, flatSection), "flat rate section saved");
        var (flatSin, flatDetails) = await CreateProfile(first, "Flatowner", flatSection, "0");
        Check(Has(flatDetails, "1,200.25"), "flat rate profile calculation");
        var (unverifiedSin, _) = await CreateProfile(first, "Unverifiedowner", section, "1", status: "Unverified", start: null);
        Page unverifiedBill = await first.Get($"/Profiles/{unverifiedSin}/Billing");
        Page unverifiedAttempt = await first.Post($"/Profiles/{unverifiedSin}/Billing?handler=Generate", unverifiedBill);
        Check(Has(unverifiedAttempt, "Generated 0") &&
              await Count("SELECT COUNT(*) FROM MonthlyBilling WHERE SIN=@sin", ("@sin", unverifiedSin)) == 0,
            "unverified profile excluded from billing");
        Page badOccupancyForm = await first.Get("/Profiles/Create");
        Page badOccupancy = await first.Post("/Profiles/Create", badOccupancyForm,
            ("Input.FullName", "Phase Twelve No Date"), ("Input.BusinessName", tag + " No Date"),
            ("Input.BusinessSection", section), ("Input.StallNumber", "987654"),
            ("Input.StallSize", "1"), ("Input.PaymentStatus", "Unpaid"),
            ("Input.StartDate", ""), ("Input.IncludeAdditionalCharge", "false"), ("Input.AdditionalCharge", "0"));
        Check(Has(badOccupancy, "Date of Occupancy") && badOccupancy.Url.AbsolutePath == "/Profiles/Create",
            "unpaid profile requires occupancy date");

        var concurrentProfiles = await Task.WhenAll(
            CreateProfile(first, "ConcurrentOne", section, "1"),
            CreateProfile(second, "ConcurrentTwo", section, "1"));
        Check(concurrentProfiles[0].Sin != concurrentProfiles[1].Sin &&
              await Count("SELECT COUNT(*) FROM AuditTrail WHERE Action='Add' AND SIN IN (@a,@b)",
                  ("@a", concurrentProfiles[0].Sin), ("@b", concurrentProfiles[1].Sin)) == 2,
            "concurrent HTTP profile creation allocates distinct audited SINs");
        Page billA = await first.Get($"/Profiles/{newSin}/Billing");
        Page billB = await second.Get($"/Profiles/{newSin}/Billing");
        Page[] concurrentBills = await Task.WhenAll(
            first.Post($"/Profiles/{newSin}/Billing?handler=Generate", billA),
            second.Post($"/Profiles/{newSin}/Billing?handler=Generate", billB));
        Check(concurrentBills.All(x => x.Status == HttpStatusCode.OK && !Has(x, "Billing generation failed")) &&
              await Count("SELECT COUNT(*) FROM MonthlyBilling WHERE SIN=@sin", ("@sin", newSin)) ==
              await Count("SELECT COUNT(DISTINCT BillingYear*100+BillingMonth) FROM MonthlyBilling WHERE SIN=@sin", ("@sin", newSin)),
            "concurrent HTTP billing creates each period once");

        Page duplicateForm = await first.Get("/Profiles/Create");
        string duplicateBusiness = tag + " Duplicate Profile";
        (string Key, string Value)[] duplicateFields = [
            ("Input.FullName", "Phase Twelve Duplicate"), ("Input.BusinessName", duplicateBusiness),
            ("Input.BusinessSection", section), ("Input.StallNumber", "765432"),
            ("Input.StallSize", "1"), ("Input.PaymentStatus", "Unverified"),
            ("Input.StartDate", ""), ("Input.IncludeAdditionalCharge", "false"),
            ("Input.AdditionalCharge", "0")];
        Page duplicateFirst = await first.Post("/Profiles/Create", duplicateForm, duplicateFields);
        Page duplicateSecond = await first.Post("/Profiles/Create", duplicateForm, duplicateFields);
        Check(duplicateFirst.Url.AbsolutePath.Contains("/Profiles/Details/") &&
              Has(duplicateSecond, "already exists") &&
              await Count("SELECT COUNT(*) FROM Profiling WHERE BusinessName=@business",
                  ("@business", duplicateBusiness)) == 1,
            "repeated profile POST does not create duplicate");

        Page audit = await first.Get("/AuditTrail?Category=activity&Search=" + Uri.EscapeDataString(sin));
        Check(Has(audit, sin) && Has(audit, primary.Username), "audit search and acting user");
        Page emptyAudit = await first.Get("/AuditTrail?Category=activity&Search=no-such-" + tag);
        Check(Has(emptyAudit, "0 record"), "audit empty search");

        string plate = "P12" + Unique();
        Page vehicleForm = await first.Get("/Vehicles/Edit");
        Page vehicleSaved = await first.Post("/Vehicles/Edit", vehicleForm,
            ("Input.CompanyName", tag + " Transport"), ("Input.DriverName", "Phase Driver"),
            ("Input.PlateNo", plate), ("Input.SecRegNo", "SEC-P12"), ("Input.DtiNumber", "DTI-P12"));
        string vin = Extract(VinPattern, vehicleSaved);
        Check(vehicleSaved.Url.AbsolutePath.Contains("/Vehicles/Details/") && Has(vehicleSaved, plate), "vehicle create PRG");
        Page duplicatePlate = await first.Post("/Vehicles/Edit", vehicleForm,
            ("Input.CompanyName", tag + " Duplicate"), ("Input.DriverName", ""),
            ("Input.PlateNo", plate), ("Input.SecRegNo", ""), ("Input.DtiNumber", ""));
        if (!Has(duplicatePlate, "already exists") || duplicatePlate.Url.AbsolutePath != "/Vehicles/Edit")
            Console.WriteLine($"duplicate plate diagnostic: path={duplicatePlate.Url.AbsolutePath}, status={duplicatePlate.Status}, hasMessage={Has(duplicatePlate, "already exists")}, validation={Has(duplicatePlate, "validation-summary-errors")}");
        Check(Has(duplicatePlate, "already exists") && duplicatePlate.Url.AbsolutePath == "/Vehicles/Edit", "duplicate plate message");
        Check(Has(await first.Get("/Vehicles/Index?search=" + plate), vin), "vehicle search");
        Page vehicleEdit = await first.Get("/Vehicles/Edit/" + vin);
        Page vehicleUpdated = await first.Post("/Vehicles/Edit/" + vin, vehicleEdit,
            ("Input.CompanyName", tag + " Transport Updated"), ("Input.DriverName", "Phase Driver"),
            ("Input.PlateNo", plate), ("Input.SecRegNo", "SEC-P12"), ("Input.DtiNumber", "DTI-P12"));
        Check(Has(vehicleUpdated, vin) && Has(vehicleUpdated, "Transport Updated"), "vehicle edit keeps VIN");

        Page vehicleFormOne = await first.Get("/Vehicles/Edit");
        Page vehicleFormTwo = await second.Get("/Vehicles/Edit");
        Task<Page> vehicleCreateOne = first.Post("/Vehicles/Edit", vehicleFormOne,
            ("Input.CompanyName", tag + " Concurrent A"), ("Input.DriverName", ""),
            ("Input.PlateNo", "P12" + Unique()), ("Input.SecRegNo", ""), ("Input.DtiNumber", ""));
        Task<Page> vehicleCreateTwo = second.Post("/Vehicles/Edit", vehicleFormTwo,
            ("Input.CompanyName", tag + " Concurrent B"), ("Input.DriverName", ""),
            ("Input.PlateNo", "P12" + Unique()), ("Input.SecRegNo", ""), ("Input.DtiNumber", ""));
        Page[] concurrentVehicles = await Task.WhenAll(vehicleCreateOne, vehicleCreateTwo);
        Check(concurrentVehicles.All(x => x.Url.AbsolutePath.Contains("/Vehicles/Details/")) &&
              Extract(VinPattern, concurrentVehicles[0]) != Extract(VinPattern, concurrentVehicles[1]),
            "concurrent HTTP vehicle creation allocates distinct VINs");

        string orNumber = "P12-OR-" + Unique();
        Page staleBill = await second.Get($"/Profiles/{sin}/Billing");
        Page billToPay = await first.Get($"/Profiles/{sin}/Billing");
        Check(Has(billToPay, "Record full payment"), "stall payment action available");
        Page paid = await first.Post($"/Profiles/{sin}/Billing?handler=Pay", billToPay,
            ("OrNumber", orNumber), ("ConfirmPayment", "true"));
        Check(Has(paid, "Payment recorded") && Has(paid, orNumber), "stall payment PRG and history");
        Check(await Count("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@sin AND ORNumber=@or",
                ("@sin", sin), ("@or", orNumber)) == 1 &&
              await Count("SELECT COUNT(*) FROM PaymentHistoryBilling l JOIN PaymentHistory p ON p.Id=l.PaymentHistoryId WHERE p.SIN=@sin AND p.ORNumber=@or",
                ("@sin", sin), ("@or", orNumber)) > 0,
            "stall payment exact bill links");
        Page stalePay = await second.Post($"/Profiles/{sin}/Billing?handler=Pay", staleBill,
            ("OrNumber", "P12-STALE-" + Unique()), ("ConfirmPayment", "true"));
        Check(Has(stalePay, "no outstanding") || Has(stalePay, "already"),
            "stale stall payment refused");
        Check(await Count("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@sin", ("@sin", sin)) == 1,
            "stale stall payment makes no second history row");
        Page repeatedStallPay = await first.Post($"/Profiles/{sin}/Billing?handler=Pay", billToPay,
            ("OrNumber", orNumber), ("ConfirmPayment", "true"));
        Check(Has(repeatedStallPay, "no outstanding") &&
              await Count("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@sin", ("@sin", sin)) == 1,
            "double-submit stall payment refused");
        var (paySin, _) = await CreateProfile(first, "ConcurrentPay", section, "1");
        Page paySetup = await first.Get($"/Profiles/{paySin}/Billing");
        await first.Post($"/Profiles/{paySin}/Billing?handler=Generate", paySetup);
        Page payFormA = await first.Get($"/Profiles/{paySin}/Billing");
        Page payFormB = await second.Get($"/Profiles/{paySin}/Billing");
        Page[] competingPays = await Task.WhenAll(
            first.Post($"/Profiles/{paySin}/Billing?handler=Pay", payFormA,
                ("OrNumber", "P12-PAY-A-" + Unique()), ("ConfirmPayment", "true")),
            second.Post($"/Profiles/{paySin}/Billing?handler=Pay", payFormB,
                ("OrNumber", "P12-PAY-B-" + Unique()), ("ConfirmPayment", "true")));
        Check(competingPays.Count(x => Has(x, "Payment recorded")) == 1 &&
              await Count("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@sin", ("@sin", paySin)) == 1,
            "concurrent HTTP stall payment has one winner");
        var (duplicateOrSin, _) = await CreateProfile(first, "DuplicateOr", section, "1");
        Page duplicateOrBill = await first.Get($"/Profiles/{duplicateOrSin}/Billing");
        await first.Post($"/Profiles/{duplicateOrSin}/Billing?handler=Generate", duplicateOrBill);
        duplicateOrBill = await first.Get($"/Profiles/{duplicateOrSin}/Billing");
        Page duplicateOrResult = await first.Post($"/Profiles/{duplicateOrSin}/Billing?handler=Pay", duplicateOrBill,
            ("OrNumber", orNumber), ("ConfirmPayment", "true"));
        Check(Has(duplicateOrResult, "OR number already exists") &&
              await Count("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@sin", ("@sin", duplicateOrSin)) == 0,
            "duplicate stall OR rejected without payment");
        Check(Has(await first.Get($"/Reports/Billing?sin={sin}"), orNumber), "printable rental report includes payment");
        Page rentalSearch = await first.Get("/Reports/RentalPayment?search=" + Uri.EscapeDataString("Phase Twelve Updated"));
        Check(Has(rentalSearch, "rental-result") && Has(rentalSearch, "Phase Twelve Updated"), "rental selector searches owner name");
        Check(Has(await first.Get("/Reports/RentalPayment?search=" + Uri.EscapeDataString(tag + " Updated")), sin), "rental selector searches business name");
        Check(Has(await first.Get("/Reports/RentalPayment?search=123456"), "rental-result"), "rental selector searches stall number");
        Check(Has(await first.Get("/Reports/RentalPayment?search=" + sin), sin), "rental selector searches SIN");
        string selectedQuery = $"selected={sin}&selected={newSin}&selected={flatSin}";
        Page selectedThree = await first.Get("/Reports/RentalPayment?" + selectedQuery);
        Check(Has(selectedThree, "3 of 3 selected") && Has(selectedThree, "rental-limit-message") &&
              Has(selectedThree, "Preview combined report"), "rental selector keeps three ordered owners and client limit message");
        Check(Has(await first.Get($"/Reports/RentalPayment?{selectedQuery}&add={unverifiedSin}"),
              "You can print up to 3 billing reports at a time."), "rental selector rejects fourth owner on server");
        Check(Has(await first.Get($"/Reports/RentalPayment?selected={sin}&selected={sin}"),
              "same stall owner cannot be selected twice"), "rental selector rejects duplicate SIN on server");
        Check(Has(await first.Get($"/Reports/RentalPayment?{selectedQuery}&remove={newSin}"), "2 of 3 selected"),
              "rental selector removes owner");
        int rentalBillsBefore = await Count("SELECT COUNT(*) FROM MonthlyBilling");
        int rentalPaymentsBefore = await Count("SELECT COUNT(*) FROM PaymentHistory");
        int rentalAuditsBefore = await Count("SELECT COUNT(*) FROM AuditTrail");
        Page rentalPreview = await first.Get("/Reports/RentalPaymentPreview?" + selectedQuery);
        Check(Regex.Matches(rentalPreview.Html, "class=\"rental-payment-block\"").Count == 3 &&
              Regex.Matches(rentalPreview.Html, "masinloc-logo.jpg").Count == 3 &&
              rentalPreview.Html.IndexOf(sin, StringComparison.Ordinal) < rentalPreview.Html.IndexOf(newSin, StringComparison.Ordinal) &&
              rentalPreview.Html.IndexOf(newSin, StringComparison.Ordinal) < rentalPreview.Html.IndexOf(flatSin, StringComparison.Ordinal),
              "rental preview renders three ordered self-contained blocks and logos");
        Check(Has(rentalPreview, primary.FullName) && Has(rentalPreview, "Date Processed") &&
              Has(rentalPreview, "Date Printed") && Has(rentalPreview, "Monthly Stall Rental Fee") &&
              Has(rentalPreview, "Total Amount Due") && !Has(rentalPreview, @"C:\Users\"),
              "rental preview shows staff, dates, billing fields, and web-relative logo");
        Check(Has(await first.Get($"/Reports/RentalPaymentPreview?{selectedQuery}&selected={unverifiedSin}"),
              "You can print up to 3 billing reports at a time.") &&
              Has(await first.Get($"/Reports/RentalPaymentPreview?selected={sin}&selected={sin}"),
              "same stall owner cannot be selected twice"), "rental preview rejects crafted fourth and duplicate");
        Check(await Count("SELECT COUNT(*) FROM MonthlyBilling") == rentalBillsBefore &&
              await Count("SELECT COUNT(*) FROM PaymentHistory") == rentalPaymentsBefore &&
              await Count("SELECT COUNT(*) FROM AuditTrail") == rentalAuditsBefore,
              "rental preview leaves bills, payments, and audit unchanged");
        string currentMonth = $"FromMonth={DateTime.Today.Month}&FromYear={DateTime.Today.Year}&ToMonth={DateTime.Today.Month}&ToYear={DateTime.Today.Year}";
        Check(Has(await first.Get("/Reports/Monthly?" + currentMonth), orNumber), "monthly report includes collection");

        Page draftForm = await first.Get("/Vehicles/Details/" + vin);
        var draftFields = Enumerable.Range(0, 19).Select(i => ($"FeeAmounts[{i}]", i == 0 ? "123.45" : "0"))
            .Concat(Enumerable.Range(0, 4).Select(i => ($"OtherDescriptions[{i}]", ""))).ToArray();
        Page draftSaved = await first.Post($"/Vehicles/Details/{vin}?handler=SaveDraft", draftForm, draftFields);
        Check(Has(draftSaved, "Fee draft saved") && Has(draftSaved, "123.45"), "vehicle draft persists through PRG");
        Page draftReloaded = await first.Get("/Vehicles/Details/" + vin);
        Check(Has(draftReloaded, "Saved total:") && Has(draftReloaded, "₱123.45"), "vehicle draft survives reload");
        string competingVin = Extract(VinPattern, concurrentVehicles[0]);
        Page noTokenPay = await first.PostWithoutToken($"/Vehicles/Details/{vin}?handler=Pay",
            ("OrNumber", "P15-NO-TOKEN-" + Unique()), ("ConfirmPayment", "true"));
        Check(noTokenPay.Status == HttpStatusCode.BadRequest &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin", ("@vin", vin)) == 0,
            "vehicle payment without anti-forgery token refused");
        Page staleVehicle = await second.Get("/Vehicles/Details/" + vin);
        Page vehiclePaid = await first.Post($"/Vehicles/Details/{vin}?handler=Pay", draftReloaded,
            ("OrNumber", orNumber), ("ConfirmPayment", "true"), ("Amount", "0.01"),
            ("PermitYear", (DateTime.Today.Year + 1).ToString()), ("RecordedBy", "-1"),
            ("vin", competingVin));
        Check(Has(vehiclePaid, "Vehicle payment recorded") && Has(vehiclePaid, "123.45"),
            "vehicle payment uses route VIN and stored year/draft despite tampered form fields");
        Check(await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin AND ORNumber=@or AND AmountPaid=123.45",
            ("@vin", vin), ("@or", orNumber)) == 1 &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin AND ORNumber=@or AND PermitYear=@year AND RecordedBy=(SELECT Id FROM Users WHERE Username=@user)",
                  ("@vin", vin), ("@or", orNumber), ("@year", DateTime.Today.Year), ("@user", primary.Username)) == 1 &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin", ("@vin", competingVin)) == 0,
            "vehicle payment stores authenticated user and authoritative VIN/year; same OR allowed in separate ledgers");
        int vehiclePaymentId = await Count("SELECT Id FROM VehiclePermitHistory WHERE VIN=@vin AND ORNumber=@or",
            ("@vin", vin), ("@or", orNumber));
        Check(Has(await first.Get("/Vehicles/Details/" + vin), $"/Reports/VehiclePaymentPreview?selected={vehiclePaymentId}"),
            "vehicle payment history links exact recorded payment report");
        int vehicleHistoryBeforeReport = await Count("SELECT COUNT(*) FROM VehiclePermitHistory");
        int vehicleDraftBeforeReport = await Count("SELECT COUNT(*) FROM VehiclePermitFeeDrafts");
        int vehicleAuditBeforeReport = await Count("SELECT COUNT(*) FROM AuditTrail");
        Page vehicleReport = await first.Get($"/Reports/VehiclePaymentPreview?selected={vehiclePaymentId}");
        Check(Has(vehicleReport, "SPECIAL VEHICLE PERMIT PAYMENT") && Has(vehicleReport, vin) &&
              Has(vehicleReport, plate) && Has(vehicleReport, orNumber) && Has(vehicleReport, "₱123.45") &&
              Has(vehicleReport, primary.FullName) && Has(vehicleReport, "Date Printed") &&
              Has(vehicleReport, "Permit Fee") && Has(vehicleReport, "masinloc-logo.jpg"),
              "vehicle report uses history amount, recorder, exact year assessment, and municipality logo");
        Check(Has(await first.Get("/Reports/VehiclePayment?search=" + Uri.EscapeDataString(orNumber)), vin) &&
              Has(await first.Get("/Reports/VehiclePayment?search=" + Uri.EscapeDataString(plate)), orNumber) &&
              Has(await first.Get("/Reports/VehiclePayment?search=" + Uri.EscapeDataString(tag + " Transport")), orNumber) &&
              Has(await first.Get($"/Reports/VehiclePayment?search={vin}&year={DateTime.Today.Year}"), orNumber),
              "vehicle report selection searches OR, plate, company, VIN, and permit year");
        Check(Has(await first.Get($"/Reports/VehiclePayment?search={competingVin}"), "No recorded payments"),
              "fee draft without payment is absent from report selection");
        Check(Has(await first.Get("/Reports/VehiclePaymentPreview?selected=2147483647"), "was not found") &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory") == vehicleHistoryBeforeReport &&
              await Count("SELECT COUNT(*) FROM VehiclePermitFeeDrafts") == vehicleDraftBeforeReport &&
              await Count("SELECT COUNT(*) FROM AuditTrail") == vehicleAuditBeforeReport,
              "vehicle report rejects nonexistent payment and leaves business tables unchanged");
        Page staleVehiclePay = await second.Post($"/Vehicles/Details/{vin}?handler=Pay", staleVehicle,
            ("OrNumber", "P12-VEH-STALE-" + Unique()), ("ConfirmPayment", "true"));
        Check(Has(staleVehiclePay, "already") || Has(staleVehiclePay, "paid"), "stale vehicle payment refused");
        Check(await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin AND PermitYear=@year",
            ("@vin", vin), ("@year", DateTime.Today.Year)) == 1, "one vehicle payment per year");
        Page repeatedVehiclePay = await first.Post($"/Vehicles/Details/{vin}?handler=Pay", draftReloaded,
            ("OrNumber", orNumber), ("ConfirmPayment", "true"));
        Check(Has(repeatedVehiclePay, "already paid") &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin AND PermitYear=@year",
                  ("@vin", vin), ("@year", DateTime.Today.Year)) == 1,
            "double-submit vehicle payment refused");
        Page competingDraftForm = await first.Get("/Vehicles/Details/" + competingVin);
        await first.Post($"/Vehicles/Details/{competingVin}?handler=SaveDraft", competingDraftForm, draftFields);
        Page competingVehicleA = await first.Get("/Vehicles/Details/" + competingVin);
        Page competingVehicleB = await second.Get("/Vehicles/Details/" + competingVin);
        Page[] competingVehiclePays = await Task.WhenAll(
            first.Post($"/Vehicles/Details/{competingVin}?handler=Pay", competingVehicleA,
                ("OrNumber", "P12-VPAY-A-" + Unique()), ("ConfirmPayment", "true")),
            second.Post($"/Vehicles/Details/{competingVin}?handler=Pay", competingVehicleB,
                ("OrNumber", "P12-VPAY-B-" + Unique()), ("ConfirmPayment", "true")));
        Check(competingVehiclePays.Count(x => Has(x, "Vehicle payment recorded")) == 1 &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin AND PermitYear=@year",
                  ("@vin", competingVin), ("@year", DateTime.Today.Year)) == 1,
            "concurrent HTTP vehicle payment has one winner");
        string duplicateVehicleVin = Extract(VinPattern, concurrentVehicles[1]);
        Page duplicateVehicleDraft = await first.Get("/Vehicles/Details/" + duplicateVehicleVin);
        await first.Post($"/Vehicles/Details/{duplicateVehicleVin}?handler=SaveDraft", duplicateVehicleDraft, draftFields);
        Page duplicateVehicleBill = await first.Get("/Vehicles/Details/" + duplicateVehicleVin);
        Page duplicateVehicleOrResult = await first.Post($"/Vehicles/Details/{duplicateVehicleVin}?handler=Pay", duplicateVehicleBill,
            ("OrNumber", orNumber), ("ConfirmPayment", "true"));
        Check(Has(duplicateVehicleOrResult, "OR number already exists") &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin", ("@vin", duplicateVehicleVin)) == 0,
            "duplicate vehicle OR rejected without payment");
        int secondVehiclePaymentId = await Count("SELECT Id FROM VehiclePermitHistory WHERE VIN=@vin",
            ("@vin", competingVin));
        int thirdVehiclePaymentId = await Count("SELECT TOP (1) Id FROM VehiclePermitHistory WHERE Id<>@first AND Id<>@second ORDER BY Id",
            ("@first", vehiclePaymentId), ("@second", secondVehiclePaymentId));
        Page twoVehicleReports = await first.Get($"/Reports/VehiclePaymentPreview?selected={vehiclePaymentId}&selected={secondVehiclePaymentId}");
        Check(Regex.Matches(twoVehicleReports.Html, "class=\"rental-payment-block vehicle-payment-block\"").Count == 2,
            "two selected vehicle payments render two report blocks");
        string vehicleSelected = $"selected={vehiclePaymentId}&selected={secondVehiclePaymentId}&selected={thirdVehiclePaymentId}";
        Page vehicleSelectedThree = await first.Get("/Reports/VehiclePayment?" + vehicleSelected);
        Check(Has(vehicleSelectedThree, "3 of 3 selected") && Has(vehicleSelectedThree, "vehicle-report-limit"),
            "vehicle selector retains three ordered payment records and client limit message");
        Check(Has(await first.Get($"/Reports/VehiclePayment?{vehicleSelected}&add=2147483647"),
              "You can print up to 3 vehicle payment reports at a time.") &&
              Has(await first.Get($"/Reports/VehiclePaymentPreview?{vehicleSelected}&selected=2147483647"),
              "You can print up to 3 vehicle payment reports at a time."),
            "vehicle selector and preview reject fourth payment on server");
        Check(Has(await first.Get($"/Reports/VehiclePaymentPreview?selected={vehiclePaymentId}&selected={vehiclePaymentId}"),
              "same annual payment cannot be selected twice") &&
              Has(await first.Get($"/Reports/VehiclePayment?{vehicleSelected}&remove={secondVehiclePaymentId}"), "2 of 3 selected"),
            "vehicle selector rejects duplicate and removes selected payment");
        Page threeVehicleReports = await first.Get("/Reports/VehiclePaymentPreview?" + vehicleSelected);
        Check(Regex.Matches(threeVehicleReports.Html, "class=\"rental-payment-block vehicle-payment-block\"").Count == 3 &&
              Regex.Matches(threeVehicleReports.Html, "masinloc-logo.jpg").Count == 3 &&
              threeVehicleReports.Html.IndexOf(vin, StringComparison.Ordinal) <
                  threeVehicleReports.Html.IndexOf(competingVin, StringComparison.Ordinal),
            "three vehicle reports keep selection order and repeat municipal logos");
        var changedDraftFields = Enumerable.Range(0, 19).Select(i => ($"FeeAmounts[{i}]", i == 0 ? "124.45" : "0"))
            .Concat(Enumerable.Range(0, 4).Select(i => ($"OtherDescriptions[{i}]", ""))).ToArray();
        Page paidDraftForm = await first.Get("/Vehicles/Details/" + vin);
        Page changedDraft = await first.Post($"/Vehicles/Details/{vin}?handler=SaveDraft", paidDraftForm, changedDraftFields);
        Check(Has(changedDraft, "124.45"), "synthetic paid-year draft update creates mismatch for report test");
        Page mismatchReport = await first.Get($"/Reports/VehiclePaymentPreview?selected={vehiclePaymentId}");
        Check(Has(mismatchReport, "₱124.45") && Has(mismatchReport, "differs from recorded amount paid") &&
              Has(mismatchReport, "Total Amount Paid") && Has(mismatchReport, "₱123.45") &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory WHERE Id=@id AND AmountPaid=123.45",
                  ("@id", vehiclePaymentId)) == 1,
            "draft mismatch is flagged; historical payment amount stays authoritative");

        var (legacySin, _) = await CreateProfile(first, "LegacyReview", section, "1");
        await using (var legacy = new SqlCommand("""
            INSERT INTO MonthlyBilling(SIN, BillingYear, BillingMonth, MonthlyRental,
                AdditionalCharge, Penalty, PaymentStatus, WebRentBasis)
            VALUES(@sin, 2025, 2, 110, 10, 0, 'Unpaid', NULL)
            """, sql))
        {
            legacy.Parameters.AddWithValue("@sin", legacySin);
            await legacy.ExecuteNonQueryAsync();
        }
        Page legacyBill = await first.Get($"/Profiles/{legacySin}/Billing");
        Check(Has(legacyBill, "Review needed") && !Has(legacyBill, "Record full payment"),
            "ambiguous legacy bill cannot be paid from page");
        Page forcedLegacyPay = await first.Post($"/Profiles/{legacySin}/Billing?handler=Pay", legacyBill,
            ("OrNumber", "P12-LEGACY-" + Unique()), ("ConfirmPayment", "true"));
        Check(Has(forcedLegacyPay, "need review") &&
              await Count("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@sin", ("@sin", legacySin)) == 0,
            "forged legacy payment refused");
        Check(Has(await first.Get($"/Reports/Billing?sin={legacySin}"), "Review needed"),
            "legacy bill report remains viewable");
        Page legacyRentalPreview = await first.Get($"/Reports/RentalPaymentPreview?selected={legacySin}");
        Check(Has(legacyRentalPreview, "Review Needed") && Has(legacyRentalPreview, "unknown rent basis") &&
              !Has(legacyRentalPreview, "₱125.00"), "rental payment preview preserves ambiguous legacy amount");

        Page profileToArchive = await first.Get("/Profiles/Details/" + newSin);
        Page archivedProfile = await first.Post($"/Profiles/Details/{newSin}?handler=Archive", profileToArchive,
            ("ConfirmArchive", "true"));
        Check(archivedProfile.Url.AbsolutePath == "/Archive/Profiles" && Has(archivedProfile, newSin),
            "profile archive PRG");
        Page repeatProfileArchive = await first.Post($"/Profiles/Details/{newSin}?handler=Archive", profileToArchive,
            ("ConfirmArchive", "true"));
        Check(Has(repeatProfileArchive, "already archived"), "repeat profile archive handled");
        Check(!Has(await first.Get("/Profiles/Index?search=" + newSin), "/Profiles/Details/" + newSin), "archived profile excluded from active list");
        Check(Has(await first.Get($"/Reports/RentalPaymentPreview?selected={newSin}"), "no longer active"),
            "rental preview handles archived owner safely");
        Page restoredProfile = await first.Post($"/Archive/Profiles?handler=Restore&sin={newSin}", archivedProfile,
            ("ConfirmRestore", "true"));
        Check(Has(restoredProfile, "restored") && Has(await first.Get("/Profiles/Index?search=" + newSin), newSin),
            "profile restore PRG");
        Page repeatProfileRestore = await first.Post($"/Archive/Profiles?handler=Restore&sin={newSin}", archivedProfile,
            ("ConfirmRestore", "true"));
        Check(Has(repeatProfileRestore, "already active") &&
              await Count("SELECT COUNT(*) FROM AuditTrail WHERE SIN=@sin AND Action IN ('Archive','Restore')",
                  ("@sin", newSin)) == 2, "repeat profile restore has no extra audit");

        Page vehicleToArchive = await first.Get("/Vehicles/Details/" + vin);
        Page archivedVehicle = await first.Post($"/Vehicles/Details/{vin}?handler=Archive", vehicleToArchive,
            ("ConfirmArchive", "true"));
        Check(archivedVehicle.Url.AbsolutePath == "/Archive/Vehicles" && Has(archivedVehicle, vin),
            "vehicle archive PRG");
        Page repeatVehicleArchive = await first.Post($"/Vehicles/Details/{vin}?handler=Archive", vehicleToArchive,
            ("ConfirmArchive", "true"));
        Check(Has(repeatVehicleArchive, "already archived"), "repeat vehicle archive handled");
        Check(!Has(await first.Get("/Vehicles/Index?search=" + vin), "/Vehicles/Details/" + vin), "archived vehicle excluded from active list");
        Check(Has(await first.Get($"/Reports/VehiclePaymentPreview?selected={vehiclePaymentId}"), orNumber),
            "archived vehicle retains printable historical payment");
        Page restoredVehicle = await first.Post($"/Archive/Vehicles?handler=Restore&vin={vin}", archivedVehicle,
            ("ConfirmRestore", "true"));
        Check(Has(restoredVehicle, "restored") && Has(await first.Get("/Vehicles/Index?search=" + vin), vin),
            "vehicle restore PRG");
        Page repeatVehicleRestore = await first.Post($"/Archive/Vehicles?handler=Restore&vin={vin}", archivedVehicle,
            ("ConfirmRestore", "true"));
        Check(Has(repeatVehicleRestore, "already active") &&
              await Count("SELECT COUNT(*) FROM AuditTrail WHERE SIN=@vin AND Action IN ('Archive Vehicle Permit','Restore Vehicle Permit')",
                  ("@vin", vin)) == 2, "repeat vehicle restore has no extra audit");

        string staleSin = concurrentProfiles[0].Sin;
        Page staleEdit = await first.Get("/Profiles/Edit/" + staleSin);
        Page staleArchiveForm = await second.Get("/Profiles/Details/" + staleSin);
        Page staleArchive = await second.Post($"/Profiles/Details/{staleSin}?handler=Archive", staleArchiveForm,
            ("ConfirmArchive", "true"));
        Page staleEditPost = await first.Post("/Profiles/Edit/" + staleSin, staleEdit,
            ("Input.FullName", "Phase Twelve Stale Edit"), ("Input.BusinessName", tag + " Stale"),
            ("Input.BusinessSection", section), ("Input.StallNumber", "555555"),
            ("Input.StallSize", "1"), ("Input.PaymentStatus", "Unpaid"),
            ("Input.StartDate", "2025-01-31"), ("Input.IncludeAdditionalCharge", "false"),
            ("Input.AdditionalCharge", "0"));
        int staleChanged = await Count("SELECT COUNT(*) FROM Profiling WHERE SIN=@sin AND IsArchived=1 AND FullName='Phase Twelve Stale Edit'",
            ("@sin", staleSin));
        Check(staleEditPost.Url.AbsolutePath == "/Archive/Profiles" &&
              Has(staleEditPost, "no longer active") && staleChanged == 0,
            "stale profile edit cannot change archived record");
        await second.Post($"/Archive/Profiles?handler=Restore&sin={staleSin}", staleArchive,
            ("ConfirmRestore", "true"));

        string staleVin = Extract(VinPattern, concurrentVehicles[1]);
        Page staleVehicleEdit = await first.Get("/Vehicles/Edit/" + staleVin);
        Page staleVehicleArchiveForm = await second.Get("/Vehicles/Details/" + staleVin);
        Page staleVehicleArchive = await second.Post($"/Vehicles/Details/{staleVin}?handler=Archive", staleVehicleArchiveForm,
            ("ConfirmArchive", "true"));
        Page staleVehicleEditPost = await first.Post("/Vehicles/Edit/" + staleVin, staleVehicleEdit,
            ("Input.CompanyName", tag + " Stale Vehicle Edit"), ("Input.DriverName", ""),
            ("Input.PlateNo", "STALE" + Unique()), ("Input.SecRegNo", ""), ("Input.DtiNumber", ""));
        Check(Has(staleVehicleEditPost, "Restore this vehicle") &&
              await Count("SELECT COUNT(*) FROM VehiclePermits WHERE VIN=@vin AND IsArchived=1 AND CompanyName LIKE '%Stale Vehicle Edit%'",
                  ("@vin", staleVin)) == 0, "stale vehicle edit cannot change archived record");
        await second.Post($"/Archive/Vehicles?handler=Restore&vin={staleVin}", staleVehicleArchive,
            ("ConfirmRestore", "true"));

        int billsBeforeImport = await Count("SELECT COUNT(*) FROM MonthlyBilling");
        int paymentsBeforeImport = await Count("SELECT COUNT(*) FROM PaymentHistory");
        int vehiclePaymentsBeforeImport = await Count("SELECT COUNT(*) FROM VehiclePermitHistory");
        string importedSin = "SIN-P12-" + Unique();
        string importedPlate = "IMP12" + Unique();
        string profileHeader = string.Join(',', TransferService.ProfileHeaders);
        string profileRow = $"{importedSin},Phase Twelve Import,{tag} Import,{section},123456,2,100,Unverified,,0,0";
        string profileCsv = string.Join('\n', profileHeader, profileRow, profileRow,
            $"SIN-P12-BAD-{Unique()},,Bad Business,{section},123456,2,100,Unverified,,0,0");
        Page profileImportForm = await first.Get("/Profiles/Transfer");
        Page profileImported = await first.Upload("/Profiles/Transfer?handler=Import", profileImportForm,
            "phase12-profiles.csv", Encoding.UTF8.GetBytes(profileCsv));
        Check(profileImported.Url.AbsolutePath == "/Profiles/Transfer" && Has(profileImported, "imported 1") &&
              Has(profileImported, "duplicates 1") && Has(profileImported, "invalid 1"),
            "profile multipart mixed CSV and PRG");
        Check(await Count("SELECT COUNT(*) FROM Profiling WHERE SIN=@sin", ("@sin", importedSin)) == 1 &&
              (await first.Get("/Profiles/Transfer")).Url.AbsolutePath == "/Profiles/Transfer",
            "profile import refresh does not resubmit");
        Page badProfileHeader = await first.Upload("/Profiles/Transfer?handler=Import", await first.Get("/Profiles/Transfer"),
            "bad-header.csv", Encoding.UTF8.GetBytes("Bad,Header\n1,2"));
        Check(Has(badProfileHeader, "Missing required column"), "profile bad header error");
        string xlsxSin = "SIN-P12-XLSX-" + Unique();
        string[] profileValues = [xlsxSin, "Phase Twelve Sheet", tag + " Sheet", section,
            "888888", "2", "100", "Unverified", "", "0", "0"];
        Page profileXlsx = await first.Upload("/Profiles/Transfer?handler=Import", await first.Get("/Profiles/Transfer"),
            "phase12-profiles.xlsx", Workbook(TransferService.ProfileHeaders, profileValues));
        Check(Has(profileXlsx, "imported 1") &&
              await Count("SELECT COUNT(*) FROM Profiling WHERE SIN=@sin", ("@sin", xlsxSin)) == 1,
            "profile XLSX multipart import");

        string vehicleCsv = string.Join('\n', string.Join(',', TransferService.VehicleHeaders),
            $"{tag} Imported,Driver,{importedPlate},SEC,DTI",
            $"{tag} Imported,Driver,{importedPlate},SEC,DTI",
            ",Driver,BADPLATE,SEC,DTI");
        Page vehicleImportForm = await first.Get("/Vehicles/Transfer");
        Page vehicleImported = await first.Upload("/Vehicles/Transfer?handler=Import", vehicleImportForm,
            "../phase12-vehicles.csv", Encoding.UTF8.GetBytes(vehicleCsv));
        Check(vehicleImported.Url.AbsolutePath == "/Vehicles/Transfer" && Has(vehicleImported, "imported 1") &&
              Has(vehicleImported, "duplicates 1") && Has(vehicleImported, "invalid 1"),
            "vehicle multipart mixed CSV and path-like filename");
        Check(await Count("SELECT COUNT(*) FROM VehiclePermits WHERE PlateNo=@plate", ("@plate", importedPlate)) == 1,
            "vehicle import creates one record");
        string xlsxPlate = "XLS12" + Unique();
        Page vehicleXlsx = await first.Upload("/Vehicles/Transfer?handler=Import", await first.Get("/Vehicles/Transfer"),
            "phase12-vehicles.xlsx", Workbook(TransferService.VehicleHeaders,
                [tag + " XLSX", "Driver", xlsxPlate, "SEC", "DTI"]));
        Check(Has(vehicleXlsx, "imported 1") &&
              await Count("SELECT COUNT(*) FROM VehiclePermits WHERE PlateNo=@plate", ("@plate", xlsxPlate)) == 1,
            "vehicle XLSX multipart import");
        Page emptyUpload = await first.Upload("/Vehicles/Transfer?handler=Import", await first.Get("/Vehicles/Transfer"),
            "empty.csv", []);
        Check(Has(emptyUpload, "nonempty"), "empty upload rejected");
        Page unsupportedUpload = await first.Upload("/Vehicles/Transfer?handler=Import", await first.Get("/Vehicles/Transfer"),
            "wrong.txt", Encoding.UTF8.GetBytes("test"));
        Check(Has(unsupportedUpload, "Only CSV and XLSX"), "unsupported upload rejected");
        Page oversizedUpload = await first.Upload("/Vehicles/Transfer?handler=Import", await first.Get("/Vehicles/Transfer"),
            "large.csv", new byte[TransferService.MaxFileBytes + 1]);
        Check(Has(oversizedUpload, "5 MB") || oversizedUpload.Status == HttpStatusCode.RequestEntityTooLarge,
            "oversized upload rejected");
        Check(await Count("SELECT COUNT(*) FROM MonthlyBilling") == billsBeforeImport &&
              await Count("SELECT COUNT(*) FROM PaymentHistory") == paymentsBeforeImport &&
              await Count("SELECT COUNT(*) FROM VehiclePermitHistory") == vehiclePaymentsBeforeImport,
            "imports have no financial side effects");

        Downloaded profileExport = await first.Download("/Profiles/Transfer?handler=Export&Search=" + importedSin);
        using (var stream = new MemoryStream(profileExport.Bytes))
        using (var workbook = new XLWorkbook(stream))
            Check(profileExport.Status == HttpStatusCode.OK && workbook.Worksheets.First().Cell(4, 1).GetString() == importedSin,
                "profile Excel export opens with search result");
        Downloaded vehicleExport = await first.Download("/Vehicles/Transfer?handler=Export&Search=" + importedPlate);
        using (var stream = new MemoryStream(vehicleExport.Bytes))
        using (var workbook = new XLWorkbook(stream))
            Check(vehicleExport.Status == HttpStatusCode.OK && workbook.Worksheets.First().Cell(4, 4).GetString() == importedPlate,
                "vehicle Excel export opens with search result");
        Downloaded emptyExport = await first.Download("/Profiles/Transfer?handler=Export&Search=no-such-" + tag);
        using (var stream = new MemoryStream(emptyExport.Bytes))
        using (var workbook = new XLWorkbook(stream))
            Check(workbook.Worksheets.First().Cell(4, 1).IsEmpty(), "empty profile export opens");
        Downloaded monthlyExcel = await first.Download("/Reports/Monthly?handler=Download&" + currentMonth);
        using (var stream = new MemoryStream(monthlyExcel.Bytes))
        using (var workbook = new XLWorkbook(stream))
            Check(monthlyExcel.Status == HttpStatusCode.OK && workbook.Worksheets.Count > 0, "monthly report Excel opens");

        Page invalidReport = await first.Get("/Reports/Monthly?FromMonth=13&FromYear=2026&ToMonth=1&ToYear=2026");
        Check(Has(invalidReport, "month") && Has(invalidReport, "text-danger"), "invalid report period handled");
        Page emptyReport = await first.Get("/Reports/Monthly?FromMonth=1&FromYear=2000&ToMonth=1&ToYear=2000");
        Check(emptyReport.Status == HttpStatusCode.OK && Has(emptyReport, "0.00"), "empty report period handled");
        Page auditPageOne = await first.Get("/AuditTrail?Category=activity&PageNumber=1");
        Check(Has(auditPageOne, "Audit trail") && Has(auditPageOne, "Next"), "audit first page and pagination");
        Page auditPageTwo = await first.Get("/AuditTrail?Category=activity&PageNumber=2");
        Check(auditPageTwo.Status == HttpStatusCode.OK && Has(auditPageTwo, "Previous"), "audit second page");

        int[] readOnlyBefore = [await Count("SELECT COUNT(*) FROM MonthlyBilling"),
            await Count("SELECT COUNT(*) FROM PaymentHistory"),
            await Count("SELECT COUNT(*) FROM VehiclePermitHistory"),
            await Count("SELECT COUNT(*) FROM VehiclePermitFeeDrafts"),
            await Count("SELECT COUNT(*) FROM Profiling WHERE IsArchived=1"),
            await Count("SELECT COUNT(*) FROM VehiclePermits WHERE IsArchived=1")];
        foreach (string path in new[] { "/", "/Profiles/Index", "/Vehicles/Index", "/RentalRates",
            "/AuditTrail", "/Archive/Profiles", "/Archive/Vehicles", "/Reports",
            "/Reports/Monthly?" + currentMonth, $"/Profiles/{sin}/Billing", "/Vehicles/Details/" + vin })
            Check((await first.Get(path)).Status == HttpStatusCode.OK, "read-only GET " + path.Split('?')[0]);
        int[] readOnlyAfter = [await Count("SELECT COUNT(*) FROM MonthlyBilling"),
            await Count("SELECT COUNT(*) FROM PaymentHistory"),
            await Count("SELECT COUNT(*) FROM VehiclePermitHistory"),
            await Count("SELECT COUNT(*) FROM VehiclePermitFeeDrafts"),
            await Count("SELECT COUNT(*) FROM Profiling WHERE IsArchived=1"),
            await Count("SELECT COUNT(*) FROM VehiclePermits WHERE IsArchived=1")];
        Check(readOnlyBefore.SequenceEqual(readOnlyAfter), "normal GETs do not mutate financial or archive state");
        Page genericError = await first.Get("/Error");
        Check(Has(genericError, "An error occurred") && !Has(genericError, "SqlException") &&
              !Has(genericError, "C:\\Users") && !Has(genericError, "Server=.") &&
              !Has(genericError, "Password"), "generic error page omits technical details");
        foreach (Page errorResponse in new[] { invalid, unknown, duplicatePlate, badProfileHeader,
                     forcedLegacyPay, oversizedUpload, staleEditPost, staleVehicleEditPost })
            Check(!Has(errorResponse, "SqlException") && !Has(errorResponse, "C:\\Users") &&
                  !Has(errorResponse, "Server=.") && !Has(errorResponse, "pbkdf2:"),
                "expected browser error omits internal details");

        Page logoutSource = await first.Get("/");
        Page loggedOut = await first.Post("/Account/Logout", logoutSource);
        Check(loggedOut.Url.AbsolutePath == "/Account/Login" &&
              (await first.Get("/Profiles/Index")).Url.AbsolutePath == "/Account/Login", "logout and back access");
        Check((await second.Get("/Profiles/Index")).Url.AbsolutePath == "/Profiles/Index", "second session remains valid");
        Console.WriteLine($"Browser fixture retained in BPLS_Dev: {tag}; SIN {sin}, {newSin}; VIN {vin}");
    }
}
