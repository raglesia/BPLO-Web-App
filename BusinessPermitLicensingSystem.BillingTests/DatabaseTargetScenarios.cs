using BusinessPermitLicensingSystem.Web.Data;
using BusinessPermitLicensingSystem.Web.Administration;
using BusinessPermitLicensingSystem.Web.Authentication;
using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Vehicles;
using BusinessPermitLicensingSystem.Web.Reports;
using BusinessPermitLicensingSystem.Web.Archive;
using BusinessPermitLicensingSystem.Web.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.SqlClient;

internal static class DatabaseTargetScenarios
{
    private const string Stage = "BPLS_Staging_QA01";
    public static async Task RunAsync()
    {
        var settings = new Dictionary<string, string?> {
            ["Database:Environment"]="Staging", ["Database:ApprovedServer"]=@".\SQLEXPRESS",
            ["Database:ApprovedServerIdentity"]=@"John\SQLEXPRESS", ["Database:ApprovedDatabase"]=Stage,
            ["ConnectionStrings:BPLS"]=$"Server=.\\SQLEXPRESS;Database={Stage};Integrated Security=True;Encrypt=True;TrustServerCertificate=True" };
        IConfigurationRoot Config(Dictionary<string,string?> data) => new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var config=Config(settings);
        Check(DatabaseConnection.Validate(config).InitialCatalog==Stage,"explicit staging target accepted");
        foreach(var key in new[]{"Database:ApprovedServer", "Database:ApprovedServerIdentity", "Database:ApprovedDatabase", "ConnectionStrings:BPLS"})
        {
            var bad=new Dictionary<string,string?>(settings);bad.Remove(key);Reject(Config(bad),"missing "+key);
        }
        foreach(var target in new[]{"BPLS_Dev","master","model","msdb","tempdb","Unexpected"})
        {
            var bad=new Dictionary<string,string?>(settings);bad["ConnectionStrings:BPLS"]=$"Server=.\\SQLEXPRESS;Database={target};Integrated Security=True";
            Reject(Config(bad),"connection database mismatch "+target);
        }
        var wrongServer=new Dictionary<string,string?>(settings);wrongServer["Database:ApprovedServer"]="unapproved-host";Reject(Config(wrongServer),"server mismatch before connection");
        var unknown=new Dictionary<string,string?>(settings);unknown["Database:Environment"]="Unknown";Reject(Config(unknown),"unknown environment");
        var production=new Dictionary<string,string?>(settings);production["Database:Environment"]="Production";Reject(Config(production),"production cannot select QA staging");
        production["Database:ApprovedDatabase"]="BPLO_Approved_Example";
        production["ConnectionStrings:BPLS"]="Server=.\\SQLEXPRESS;Database=BPLO_Approved_Example;Integrated Security=True";
        Check(DatabaseConnection.Validate(Config(production)).InitialCatalog=="BPLO_Approved_Example","production explicit configuration validates without opening any database");
        var dev=new Dictionary<string,string?>{["ConnectionStrings:BPLS"]="Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True"};
        Check(DatabaseConnection.Validate(Config(dev)).InitialCatalog=="BPLS_Dev","existing development configuration remains supported");
        dev["ConnectionStrings:BPLS"]=settings["ConnectionStrings:BPLS"];Reject(Config(dev),"development cannot select staging");
        var wrongIdentity=new Dictionary<string,string?>(settings);wrongIdentity["Database:ApprovedServerIdentity"]="WrongServer";
        try {await using var unexpected=await DatabaseConnection.OpenAsync(Config(wrongIdentity),default);throw new Exception("Wrong identity accepted");}
        catch(InvalidOperationException e) when(e.Message.Contains("identity")){Check(true,"actual SQL identity mismatch rejected");}

        await using var db=await DatabaseConnection.OpenAsync(config,default);
        var users=new UserAccountService(config);
        string password="Qa01!"+Guid.NewGuid().ToString("N"), username="QA01_"+Guid.NewGuid().ToString("N")[..10];
        var account=await users.CreatePublicAsync("Synthetic QA01 Employee",username,"QA only",password,default);
        Check(account.Success,"staging account creation");
        var auth=new UserAuthenticationService(config,NullLogger<UserAuthenticationService>.Instance);
        Check((await auth.AuthenticateAsync(username,password,default))?.Id==account.Id,"staging authentication");
        Check(await auth.AuthenticateAsync(username,"wrong",default) is null,"invalid password rejected");
        string tag=Guid.NewGuid().ToString("N")[..10];
        string section="QA01 Synthetic Flat "+tag;
        var rates=new RentalRateService(config);
        Check((await rates.SaveAsync(section,"Flat",0,1200,!(await rates.ListAsync(default)).Any(r=>r.Section==section),account.Id,default)).Success,"staging rate write");
        Check((await rates.ListAsync(default)).Any(r=>r.Section==section),"staging rate read");
        var profiles=new ProfileService(config);
        var result=await profiles.CreateAsync(new ProfileInput {FullName="Synthetic QA Owner "+new string(tag.Select(c=>(char)('A'+c%26)).ToArray()),BusinessName="QA01 Business",BusinessSection=section,StallNumber="QA01-A",StallSize="1",PaymentStatus="Unpaid",StartDate=new DateTime(2026,3,1)},account.Id,default);
        Check(result.Sin is not null,"staging profile creation: "+result.Error);string sin=result.Sin!;
        Check((await profiles.GetAsync(sin,default))?.MonthlyRental==1200,"profile rent unchanged");
        var billing=new BillingService(config);var asOf=new DateTime(2026,4,21);
        Check(await billing.GenerateAsync(sin,asOf,default)==1,"staging regular billing generation");
        Check(await billing.GenerateAsync(sin,asOf,default)==0,"billing retry does not duplicate period");
        Check((await billing.GetAsync(sin,asOf,default))?.TotalDue==1500,"existing 25 percent penalty preserved");
        string orRun=QaDatabase.OrRun();
        string rentOr=orRun+"-1";
        Check((await billing.PayAsync(sin,rentOr,account.Id,asOf,default)).Success,"staging rental payment");
        var report=new ReportService(config);
        Check((await report.ProfileAsync(sin,default)) is not null,"staging rental report");
        Check((await report.MonthlyAsync(2026,4,2026,4,default)).Payments.Count>0,"staging collection report");
        await using(var baseline=new SqlCommand("UPDATE Profiling SET IsLegacyBaseline=1,StartDate='2026-12-01',PaymentStatus='Unpaid' WHERE SIN=@sin",db))
        {baseline.Parameters.AddWithValue("@sin",sin);await baseline.ExecuteNonQueryAsync();}
        Check(await billing.GenerateAsync(sin,new DateTime(2026,11,30),default)==1,"legacy December baseline obligation prepared before commencement");
        await using(var historical=new SqlCommand("SELECT COUNT(*) FROM MonthlyBilling WHERE SIN=@sin AND PaymentStatus='Unpaid' AND (BillingYear<2026 OR (BillingYear=2026 AND BillingMonth<12))",db))
        {historical.Parameters.AddWithValue("@sin",sin);Check((int)(await historical.ExecuteScalarAsync())! == 0,"no historical unpaid regular bills generated");}
        var december=new DateTime(2026,12,21);
        Check(await billing.GenerateAsync(sin,december,default)==0,"legacy December obligation generated once");
        Check((await billing.SaveVerifiedArrearsAsync(sin,null,2026,11,100,0,"Synthetic Treasury QA01",account.Id,december,default)).Success,"Treasury verified arrears staging workflow");
        Check((await billing.GetAsync(sin,december,default))?.TotalDue==1625,"legacy rent plus verified arrears penalty remains correct");
        Check((await billing.PayAsync(sin,orRun+"-2",account.Id,december,default)).Success,"legacy arrears settlement on staging");
        var record=new VehicleRecordService(config);
        var car=await record.SaveAsync(null,"Synthetic QA01 Vehicle","QA Driver","QA01-PLATE-"+tag,"","",account.Id,default);
        Check(car.Success,"staging vehicle creation");
        var vehicles=new VehicleService(config);var fees=new decimal[VehicleFeeDraft.FeeNames.Length];fees[0]=300;
        Check((await vehicles.SaveDraftAsync(car.Vin!,2026,fees,["","","",""],asOf,default)).Success,"staging vehicle assessment");
        Check(!(await vehicles.PayAsync(car.Vin!,rentOr,2026,account.Id,asOf,default)).Success,"cross-ledger OR rejected on staging");
        Check((await vehicles.PayAsync(car.Vin!,orRun+"-3",2026,account.Id,asOf,default)).Success,"staging vehicle payment");
        Check((await new VehiclePaymentReportService(config).SearchAsync(orRun+"-3",2026,1,default)).Total==1,"staging vehicle report");
        var archive=new ArchiveService(config);
        Check((await archive.ProfileAsync(sin,true,account.Id,default)).Success,"staging archive");
        Check((await archive.ProfileAsync(sin,false,account.Id,default)).Success,"staging restore");
        Check((await new TransferService(config).ExportAsync(false,null,default)).Length>0,"staging export");
        var audit=new AuditTrailService(config);await audit.RecordSessionAsync(account.Id,"Login",default);await audit.RecordSessionAsync(account.Id,"Logout",default);
        Check((await audit.ListAsync("users",username,1,default)).Total==2,"staging session audits");
        Check((await audit.ListAsync("activity",sin,1,default)).Total>0,"staging business audits");
        // Global financial concurrency is run by --release-qa on a guarded release staging database.
        // Credentials are kept outside the repository for the subsequent HTTP acceptance checks.
        string? credentialFile=Environment.GetEnvironmentVariable("BPLO_QA01_CREDENTIAL_FILE");
        if(credentialFile is not null)await File.WriteAllTextAsync(credentialFile,System.Text.Json.JsonSerializer.Serialize(new {Username=username,Password=password,Id=account.Id,Sin=sin,Vin=car.Vin}));
    }
    private static void Reject(IConfiguration config,string label)
    {
        try { DatabaseConnection.Validate(config); }
        catch(InvalidOperationException) {Check(true,"reject "+label);return;}
        throw new Exception("FAIL accepted "+label);
    }
    private static void Check(bool value,string label){if(!value)throw new Exception("FAIL QA01 "+label);Console.WriteLine("PASS QA01 "+label);}
}


