using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Vehicles;
using BusinessPermitLicensingSystem.Web.Receipts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class GlobalReceiptScenarios
{
    private const string ConnectionString = "Server=.\\SQLEXPRESS;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";
    public static async Task RunAsync()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
            { ["ConnectionStrings:BPLS"] = ConnectionString }).Build();
        var rental = new BillingService(config); var vehicle = new VehicleService(config);
        await using var db = new SqlConnection(ConnectionString); await db.OpenAsync();
        Check((string?)await Scalar("SELECT DB_NAME()") == "BPLS_Dev", "database guard");
        int user = Convert.ToInt32(await Scalar("SELECT TOP(1) Id FROM Users ORDER BY Id"));
        string tag = Guid.NewGuid().ToString("N")[..10];
        DateTime date = new(2026,4,21,12,0,0);
        var profiles = new List<string>(); var vehicles = new List<string>();
        string[] fixedOrs = ["OR-TEST-GLOBAL-001","OR-TEST-GLOBAL-002","OR-TEST-GLOBAL-003"];
        foreach(string or in fixedOrs)
            Check(await CountOr(or) == 0, "requested synthetic OR available " + or);
        try
        {
            string a=await Profile(), b=await Car();
            Check((await rental.PayAsync(a," \t"+fixedOrs[0]+"\r\n",user,date,default)).Success, "rental reserves trimmed OR");
            var duplicateVehicle=await vehicle.PayAsync(b,fixedOrs[0].ToLowerInvariant(),2026,user,date,default);
            Check(!duplicateVehicle.Success && duplicateVehicle.Error==ReceiptRegistry.DuplicateMessage && await CountVehicle(b)==0,
                "rental to vehicle duplicate rejected with no vehicle payment");
            string c=await Car(), d=await Profile();
            Check((await vehicle.PayAsync(c,fixedOrs[1],2026,user,date,default)).Success,"vehicle reserves OR");
            var duplicateRental=await rental.PayAsync(d,fixedOrs[1],user,date,default);
            Check(!duplicateRental.Success && duplicateRental.Error==ReceiptRegistry.DuplicateMessage && await CountRental(d)==0,
                "vehicle to rental duplicate rejected with no rental payment");
            // Distinct obligations, same receipt: test receipt uniqueness rather than same-profile locking.
            string rr1=await Profile(),rr2=await Profile(),orR="GLOBAL-"+tag+"-RR";
            var rr=await Task.WhenAll(rental.PayAsync(rr1,orR,user,date,default),rental.PayAsync(rr2,orR,user,date,default));
            Check(rr.Count(x=>x.Success)==1 && rr.Single(x=>!x.Success).Error==ReceiptRegistry.DuplicateMessage && await CountOr(orR)==1,
                "rental versus rental same OR has one clean winner");
            string vv1=await Car(),vv2=await Car(),orV="GLOBAL-"+tag+"-VV";
            var vv=await Task.WhenAll(vehicle.PayAsync(vv1,orV,2026,user,date,default),vehicle.PayAsync(vv2,orV,2026,user,date,default));
            Check(vv.Count(x=>x.Success)==1 && vv.Single(x=>!x.Success).Error==ReceiptRegistry.DuplicateMessage && await CountOr(orV)==1,
                "vehicle versus vehicle same OR has one clean winner");
            string cr=await Profile(),cv=await Car();
            var rentalTask=rental.PayAsync(cr,fixedOrs[2],user,date,default);
            var vehicleTask=vehicle.PayAsync(cv,fixedOrs[2],2026,user,date,default);
            await Task.WhenAll(rentalTask,vehicleTask);
            Check((rentalTask.Result.Success?1:0)+(vehicleTask.Result.Success?1:0)==1 &&
                await CountOr(fixedOrs[2])==1 && await CountRental(cr)+await CountVehicle(cv)==1,
                "cross-ledger concurrency one registry row and one payment");
            Check((rentalTask.Result.Success?vehicleTask.Result.Error:rentalTask.Result.Error)==ReceiptRegistry.DuplicateMessage,
                "cross-ledger loser receives friendly error");
            foreach(bool isRental in new[]{true,false})
            {
                string owner=isRental?await Profile():await Car();
                string or="GLOBAL-"+tag+(isRental?"-ROLL-R":"-ROLL-V");
                string trigger="TR_GlobalReceipt_QA_"+tag+(isRental?"R":"V");
                string table=isRental?"PaymentHistoryBilling":"VehiclePermitHistory";
                string predicate=isRental
                    ?$"EXISTS(SELECT 1 FROM inserted i JOIN MonthlyBilling b ON b.Id=i.MonthlyBillingId WHERE b.SIN='{owner}')"
                    :$"EXISTS(SELECT 1 FROM inserted WHERE VIN='{owner}')";
                await Execute($"CREATE TRIGGER dbo.{trigger} ON dbo.{table} AFTER INSERT AS BEGIN IF {predicate} THROW 51000,'Controlled global receipt rollback',1; END");
                try
                {
                    bool failed=false;
                    try { if(isRental) await rental.PayAsync(owner,or,user,date,default); else await vehicle.PayAsync(owner,or,2026,user,date,default); }
                    catch(SqlException e) when(e.Number==51000){failed=true;}
                    Check(failed && await CountOr(or)==0 && (isRental?await CountRental(owner):await CountVehicle(owner))==0,
                        (isRental?"rental":"vehicle")+" downstream failure releases reserved OR and history");
                }
                finally {await Execute($"DROP TRIGGER IF EXISTS dbo.{trigger}");}
                bool retried=isRental?(await rental.PayAsync(owner,or,user,date,default)).Success
                    :(await vehicle.PayAsync(owner,or,2026,user,date,default)).Success;
                Check(retried && await CountOr(or)==1,(isRental?"rental":"vehicle")+" retry can reuse rolled-back OR");
            }
            // A raw SQL writer must not bypass the registry unique constraint.
            string direct=await Car(); bool rejected=false;
            try {await Execute("INSERT INTO VehiclePermitHistory(VIN,ORNumber,AmountPaid,PermitYear,DatePaid,RecordedBy) VALUES(@vin,@or,300,2026,@date,@user)",
                ("@vin",direct),("@or",fixedOrs[0]),("@date",date),("@user",user));}
            catch(SqlException e) when(e.Number is 2601 or 2627){rejected=true;}
            Check(rejected && await CountVehicle(direct)==0,"direct ledger insert cannot bypass global uniqueness");
            Check(ReceiptRegistry.NormalizeOR(" \tAb-01  x\r\n")=="Ab-01  x","normalization preserves case and meaningful internal characters");
            await using(var command=new SqlCommand("SELECT dbo.NormalizeOR(@or)",db))
            {
                command.Parameters.AddWithValue("@or"," \tAb-01  x\r\n");
                Check((string?)await command.ExecuteScalarAsync()==ReceiptRegistry.NormalizeOR(" \tAb-01  x\r\n"),"SQL and shared C# normalization match");
            }
            Check(Convert.ToInt32(await Scalar("""
                SELECT COUNT(*) FROM ReceiptRegistry r LEFT JOIN(
                    SELECT 'StallRental' PaymentType,Id,ORNumber FROM PaymentHistory
                    UNION ALL SELECT 'VehiclePermit',Id,ORNumber FROM VehiclePermitHistory
                )p ON p.PaymentType=r.PaymentType AND p.Id=r.PaymentReferenceId
                WHERE p.Id IS NULL OR r.ORNumber<>dbo.NormalizeOR(p.ORNumber) COLLATE Latin1_General_100_CI_AS
                """))==0,"no orphan or mismatched receipt references");
        }
        finally
        {
            // Exact owned identifiers only. History DELETE triggers remove their receipt entries.
            foreach(string sin in profiles)
                await Execute("""
                    DELETE l FROM PaymentHistoryBilling l JOIN PaymentHistory h ON h.Id=l.PaymentHistoryId WHERE h.SIN=@sin;
                    DELETE l FROM PaymentHistoryArrears l JOIN PaymentHistory h ON h.Id=l.PaymentHistoryId WHERE h.SIN=@sin;
                    DELETE FROM PaymentHistory WHERE SIN=@sin;
                    DELETE FROM MonthlyBilling WHERE SIN=@sin;
                    DELETE FROM AuditTrail WHERE SIN=@sin;
                    DELETE FROM Profiling WHERE SIN=@sin;
                    """,("@sin",sin));
            foreach(string vin in vehicles)
                await Execute("DELETE FROM VehiclePermitHistory WHERE VIN=@vin; DELETE FROM VehiclePermitFeeDrafts WHERE VIN=@vin; DELETE FROM AuditTrail WHERE SIN=@vin; DELETE FROM VehiclePermits WHERE VIN=@vin;",("@vin",vin));
            Check(Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM ReceiptRegistry WHERE ORNumber LIKE @tag",("@tag","GLOBAL-"+tag+"%")))==0,
                "owned global receipt fixtures cleaned");
            foreach(string or in fixedOrs) Check(await CountOr(or)==0,"requested OR fixture cleaned "+or);
        }
        async Task<string> Profile()
        {
            string sin="SIN-GLOBAL-"+tag+"-"+profiles.Count; profiles.Add(sin);
            await Execute("""
                INSERT INTO Profiling(SIN,FullName,BusinessName,BusinessSection,StallNumber,StallSize,MonthlyRental,PaymentStatus,StartDate,AdditionalCharge,IsArchived)
                VALUES(@sin,@sin,@sin,'Public Market Stalls',@sin,'1',100,'Unpaid','2026-03-01',0,0)
                """,("@sin",sin)); return sin;
        }
        async Task<string> Car()
        {
            string vin="VIN-GLOBAL-"+tag+"-"+vehicles.Count; vehicles.Add(vin);
            await Execute("INSERT INTO VehiclePermits(VIN,CompanyName,DriverName,PlateNo,PermitStatus,PermitYear,IsArchived) VALUES(@vin,@vin,'QA',@vin,'Unpaid',2026,0)",("@vin",vin));
            var fees=Enumerable.Repeat(0m,VehicleFeeDraft.FeeNames.Length).ToArray();fees[0]=300m;
            var saved=await vehicle.SaveDraftAsync(vin,2026,fees,["","","",""],date,default);
            Check(saved.Success,"synthetic saved vehicle draft"); return vin;
        }
        async Task<int> CountOr(string or)=>Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM ReceiptRegistry WHERE ORNumber=dbo.NormalizeOR(@or)",("@or",or)));
        async Task<int> CountRental(string sin)=>Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM PaymentHistory WHERE SIN=@id",("@id",sin)));
        async Task<int> CountVehicle(string vin)=>Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@id",("@id",vin)));
        async Task<object?> Scalar(string sql,params (string,object)[] args){await using var cmd=new SqlCommand(sql,db);foreach(var(key,value) in args)cmd.Parameters.AddWithValue(key,value);return await cmd.ExecuteScalarAsync();}
        async Task Execute(string sql,params (string,object)[] args){await using var cmd=new SqlCommand(sql,db);foreach(var(key,value) in args)cmd.Parameters.AddWithValue(key,value);await cmd.ExecuteNonQueryAsync();}
    }
    private static void Check(bool yes,string label){if(!yes)throw new Exception("FAIL global OR "+label);Console.WriteLine("PASS global OR "+label);}
}
