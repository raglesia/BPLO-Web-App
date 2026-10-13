using BusinessPermitLicensingSystem.Web.Administration;
using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Data;
using BusinessPermitLicensingSystem.Web.Reports;
using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

internal static class ReleaseQaScenarios
{
    public static async Task RunAsync()
    {
        var config=QaDatabase.Configuration(); QaDatabase.RequireIsolated(config);
        await using var db=await DatabaseConnection.OpenAsync(config,default);
        int users=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM Users"));
        if(users==0)
        {
            var accounts=new UserAccountService(config);
            foreach(string suffix in new[]{"One","Two","Three"})
                Check((await accounts.CreatePublicAsync("Synthetic RQA "+suffix,"rqa"+suffix,"QA Staff","Rqa2026!Test",default)).Success,"isolated QA staff setup "+suffix);
        }
        await PaymentScenarios.RunAsync();
        await LegacyArrearsScenarios.RunAsync();
        await VehicleScenarios.RunAsync();
        await GlobalReceiptScenarios.RunAsync(config);
        await ArchiveScenarios.RunAsync();
        await ReportScenarios.RunAsync();
        var vehicles=new VehicleService(config); var rental=new BillingService(config);
        var reports=new VehiclePaymentReportService(config);var mayor=new VehicleMayorPermitService(config);
        int user=Convert.ToInt32(await Scalar("SELECT TOP(1) Id FROM Users ORDER BY Id"));
        var date=new DateTime(2026,10,7,12,0,0);
        string run=Guid.NewGuid().ToString("N")[..10],orRun=QaDatabase.OrRun();int n=0;
        string NextOr()=>orRun+"-"+(++n);
        var fees=new decimal[VehicleFeeDraft.FeeNames.Length];fees[0]=726;fees[15]=44;
        var details=new DraftData{Location="Masinloc",Description="Delivery Truck",LineOfBusiness="Transport",Organization="Individual",Quarter="4TH QUARTER",Employees="0"};
        string vin=await Car();
        Check((await vehicles.SaveDraftAsync(vin,2026,fees,["Special charge","","",""],date,default,details)).Success,"unpaid assessment editable");
        string paidOr=NextOr();Check((await vehicles.PayAsync(vin,paidOr,2026,user,date,default)).Success,"snapshot payment success");
        int id=await PaymentId(vin);var before=(await reports.GetAsync(id,default))!;
        Check(before.Assessment is not null && before.Assessment.GrandTotal==770 && before.AmountPaid==770 && before.Assessment.Data.OtherDescriptions[0]=="Special charge" && before.Assessment.Data.Description=="Delivery Truck","payment-time fee names/descriptions/details preserved");
        Check(!(await vehicles.SaveDraftAsync(vin,2026,fees,["Changed","","",""],date,default,details)).Success,"normal paid assessment save rejected");
        bool immutable=false;
        try{await Exec("UPDATE VehiclePaymentAssessmentSnapshots SET CompanyName='Tampered' WHERE PaymentHistoryId=@id",("@id",id));}
        catch(SqlException e)when(e.Number==51003){immutable=true;}
        Check(immutable,"database snapshot update rejected");
        immutable=false;try{await Exec("DELETE FROM VehiclePaymentAssessmentSnapshots WHERE PaymentHistoryId=@id",("@id",id));}catch(SqlException e)when(e.Number==51003){immutable=true;}
        Check(immutable,"database snapshot deletion rejected");
        // Deliberate bypass of the UI, confined to disposable QA: prove reports ignore current draft and identity.
        var changedFees=new decimal[VehicleFeeDraft.FeeNames.Length];changedFees[0]=1;
        var changed=VehicleFeeDraft.Create(changedFees,["Changed","","",""],new DraftData{Description="Changed"});
        await Exec("UPDATE VehiclePermitFeeDrafts SET DetailsJson=@json,GrandTotal=1 WHERE VIN=@vin; UPDATE VehiclePermits SET CompanyName='Changed',DriverName='Changed',PlateNo='Changed' WHERE VIN=@vin",("@json",changed.Json),("@vin",vin));
        var after=(await reports.GetAsync(id,default))!;
        Check(after.Company==before.Company && after.Driver==before.Driver && after.Plate==before.Plate && after.Assessment!.GrandTotal==770 && after.Assessment.Amounts[0]==726 && after.Assessment.Data.OtherDescriptions[0]=="Special charge" && after.Assessment.Data.Description=="Delivery Truck","historical payment and Mayor assessment stable after draft/identity bypass edit");
        Check(await mayor.PrepareAsync(id,user,date,default)=="2026-0001","clean install first Mayor number");
        var repeated=await Task.WhenAll(Enumerable.Range(0,6).Select(_=>mayor.PrepareAsync(id,user,date,default)));
        Check(repeated.All(x=>x=="2026-0001") && await mayor.GetNumberAsync(id,default)=="2026-0001","repeat/concurrent same payment reuses one permit number");
        var ids=new List<int>();
        for(int i=0;i<6;i++){string c=await Car();await vehicles.SaveDraftAsync(c,2026,fees,["","","",""],date,default,details);Check((await vehicles.PayAsync(c,NextOr(),2026,user,date,default)).Success,"numbering fixture payment "+i);ids.Add(await PaymentId(c));}
        var numbers=await Task.WhenAll(ids.Select(i=>mayor.PrepareAsync(i,user,date,default)));
        Check(numbers.Distinct().Count()==6 && numbers.Select(x=>int.Parse(x.Split('-')[1])).Order().SequenceEqual(Enumerable.Range(2,6)),"distinct concurrent permit numbering continuous 2 through 7");
        bool duplicate=false;try{await Exec("INSERT VehicleMayorPermits SELECT @id,IssueYear,SequenceNumber,PreparedAt,PreparedBy FROM VehicleMayorPermits WHERE PaymentHistoryId=@id",("@id",id));}catch(SqlException e)when(e.Number is 2601 or 2627){duplicate=true;}
        Check(duplicate,"duplicate permit identity rejected");
        string nr=await Car();await vehicles.SaveDraftAsync(nr,2026,fees,["","","",""],date,default,details);await vehicles.PayAsync(nr,NextOr(),2026,user,date,default);int rollbackId=await PaymentId(nr);
        string trigger="TR_RQA_NumberFailure_"+run;
        await Exec($"CREATE TRIGGER dbo.{trigger} ON dbo.VehicleMayorPermits AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE PaymentHistoryId={rollbackId}) THROW 51000,'QA numbering failure',1; END");
        try {bool failure=false;try{await mayor.PrepareAsync(rollbackId,user,date,default);}catch(SqlException e)when(e.Number==51000){failure=true;}Check(failure && await mayor.GetNumberAsync(rollbackId,default) is null && Convert.ToInt32(await Scalar("SELECT LastNumber FROM VehicleMayorPermitCounters WHERE IssueYear=2026"))==7,"numbering failure rolls back permit and counter");}
        finally{await Exec($"DROP TRIGGER dbo.{trigger}");}
        Check(await mayor.PrepareAsync(rollbackId,user,date,default)=="2026-0008","numbering retry consumes next number without gap");
        Check(await mayor.PrepareAsync(id,user,date.AddYears(1),default)=="2026-0001","reprint retains assigned number across issue-year change");
        foreach(var fault in new[]{("VehiclePaymentAssessmentSnapshots","INSERT"),("VehiclePermitHistory","INSERT"),("VehiclePermits","UPDATE")})
        {
            string c=await Car();await vehicles.SaveDraftAsync(c,2026,fees,["","","",""],date,default,details);string or=NextOr();
            string tr="TR_RQA_Failure_"+run;
            string condition=fault.Item1=="VehiclePaymentAssessmentSnapshots"?$"EXISTS(SELECT 1 FROM inserted i JOIN VehiclePermitHistory h ON h.Id=i.PaymentHistoryId WHERE h.VIN='{c}')":$"EXISTS(SELECT 1 FROM inserted WHERE VIN='{c}')";
            await Exec($"CREATE TRIGGER dbo.{tr} ON dbo.{fault.Item1} AFTER {fault.Item2} AS BEGIN IF {condition} THROW 51000,'QA forced payment failure',1; END");
            try{bool failure=false;try{await vehicles.PayAsync(c,or,2026,user,date,default);}catch(SqlException e)when(e.Number==51000){failure=true;}Check(failure && Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM VehiclePermitHistory WHERE VIN=@vin",("@vin",c)))==0 && Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM ReceiptRegistry WHERE ORNumber=@or",("@or",or)))==0 && Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM VehiclePaymentAssessmentSnapshots s JOIN VehiclePermitHistory h ON h.Id=s.PaymentHistoryId WHERE h.VIN=@vin",("@vin",c)))==0 && (string?)await Scalar("SELECT PermitStatus FROM VehiclePermits WHERE VIN=@vin",("@vin",c))=="Unpaid","forced "+fault.Item1+" failure rolls back ledger, receipt, snapshot and paid state");}
            finally{await Exec($"DROP TRIGGER dbo.{tr}");}
            Check((await vehicles.PayAsync(c,or,2026,user,date,default)).Success,"failed payment OR reusable after rollback");
        }
        for(int i=0;i<8;i++)
        {
            string c=await Car();await vehicles.SaveDraftAsync(c,2026,fees,["Original","","",""],date,default,details);
            var newFees=(decimal[])fees.Clone();newFees[0]=900;
            var save=vehicles.SaveDraftAsync(c,2026,newFees,["New","","",""],date,default,details);
            var pay=vehicles.PayAsync(c,NextOr(),2026,user,date,default);
            await Task.WhenAll(save,pay);Check(pay.Result.Success,"save/payment race payment succeeds "+i);
            var r=(await reports.GetAsync(await PaymentId(c),default))!;
            Check(r.Assessment!.GrandTotal==r.AmountPaid && (r.AmountPaid==770 || r.AmountPaid==944) &&
                r.Assessment.Data.OtherDescriptions[0]==(save.Result.Success?"New":"Original") &&
                !(await vehicles.SaveDraftAsync(c,2026,fees,["Later","","",""],date,default,details)).Success,"save/payment race has coherent snapshot and subsequent lock "+i);
        }
        Check(!(await vehicles.PayAsync(vin,"ABC-12",2026,user,date,default)).Success && !(await rental.PayAsync("QA invalid input","ABC-12",user,date,default)).Success,"alphabetic OR validation retained separately");
        foreach(string invalid in new[]{"12/34","12\\34","12 34","１２３",""})
            Check(!(await vehicles.PayAsync(vin,invalid,2026,user,date,default)).Success && !(await rental.PayAsync("QA invalid input",invalid,user,date,default)).Success,"invalid OR rejected "+invalid);
        // An old row is deliberately marked by the absence of evidence, never backfilled from its draft.
        string legacy=await Car();string legacyOr=NextOr();await Exec("INSERT VehiclePermitHistory(VIN,ORNumber,AmountPaid,PermitYear,DatePaid,RecordedBy) VALUES(@vin,@or,300,2026,@date,@user)",("@vin",legacy),("@or",legacyOr),("@date",date),("@user",user));
        int legacyId=await PaymentId(legacy);var old=(await reports.GetAsync(legacyId,default))!;
        Check(old.Assessment is null && old.AssessmentWarning!.Contains("Legacy/pre-snapshot") && old.AmountPaid==300,"legacy ledger preserved without fabricated breakdown");
        bool refused=false;try{await mayor.PrepareAsync(legacyId,user,date,default);}catch(SqlException e)when(e.Number==51000){refused=true;}Check(refused,"legacy missing-snapshot permit preparation refused");
        Check(Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM VehiclePaymentAssessmentSnapshots s LEFT JOIN VehiclePermitHistory h ON h.Id=s.PaymentHistoryId WHERE h.Id IS NULL OR h.AmountPaid<>s.GrandTotal"))==0,"no orphan or payment amount mismatch snapshots");
        Check(Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM VehiclePermitHistory h WHERE h.VIN LIKE @tag AND h.VIN<>@legacy AND NOT EXISTS(SELECT 1 FROM VehiclePaymentAssessmentSnapshots s WHERE s.PaymentHistoryId=h.Id)",("@tag","VIN-RQA-"+run+"%"),("@legacy",legacy)))==0,"every successful service payment has a snapshot");
        var evidence=new{Database=config["Database:ApprovedDatabase"],Vin=vin,PaymentId=id,LegacyId=legacyId,Number="2026-0001",Username="rqaOne",Password="Rqa2026!Test"};
        string? file=Environment.GetEnvironmentVariable("BPLO_RQA_HTTP_FIXTURE");if(file is not null)await File.WriteAllTextAsync(file,System.Text.Json.JsonSerializer.Serialize(evidence));
        Console.WriteLine("Release QA transaction evidence retained in isolated database only.");
        async Task<string> Car(){string c="VIN-RQA-"+run+"-"+Guid.NewGuid().ToString("N")[..6];await Exec("INSERT VehiclePermits(VIN,CompanyName,DriverName,PlateNo,PermitStatus,PermitYear,IsArchived) VALUES(@vin,'Original Company','Original Driver',@vin,'Unpaid',2026,0)",("@vin",c));return c;}
        async Task<int> PaymentId(string c)=>Convert.ToInt32(await Scalar("SELECT Id FROM VehiclePermitHistory WHERE VIN=@vin AND PermitYear=2026",("@vin",c)));
        async Task<object?> Scalar(string sql,params(string,object)[] args){await using var cmd=new SqlCommand(sql,db);foreach(var(k,v)in args)cmd.Parameters.AddWithValue(k,v);return await cmd.ExecuteScalarAsync();}
        async Task Exec(string sql,params(string,object)[] args){await using var cmd=new SqlCommand(sql,db);foreach(var(k,v)in args)cmd.Parameters.AddWithValue(k,v);await cmd.ExecuteNonQueryAsync();}
    }
    private static void Check(bool condition,string label){if(!condition)throw new Exception("FAIL RQA "+label);Console.WriteLine("PASS RQA "+label);}
}
