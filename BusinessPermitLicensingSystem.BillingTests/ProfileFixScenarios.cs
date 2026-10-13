using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Billing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
internal static class ProfileFixScenarios
{
 public static async Task RunAsync()
 {
  var config=QaDatabase.Configuration();QaDatabase.RequireIsolated(config);
  await using var cn=new SqlConnection(config.GetConnectionString("BPLS"));await cn.OpenAsync();
  await using var user=new SqlCommand("SELECT TOP 1 Id FROM Users ORDER BY Id",cn);int id=(int)(await user.ExecuteScalarAsync() ?? throw new Exception("QA user missing"));
  var service=new ProfileService(config);
  await using var seed=new SqlCommand("IF NOT EXISTS(SELECT 1 FROM RentalRates WHERE Section='QA Profile Fixed') INSERT INTO RentalRates(Section,RatePerSqm,FlatRate,RateType) VALUES('QA Profile Fixed',0,600,'Flat')",cn);await seed.ExecuteNonQueryAsync();
  var rate=(await service.GetRatesAsync(default)).First();
  var input=new ProfileInput {FullName="Synthetic Optional Charge",BusinessName="QA",BusinessSection=rate.Section,StallNumber=Guid.NewGuid().ToString("N"),StallSize="10",PaymentStatus="Unpaid",StartDate=new(2026,10,13),IncludeAdditionalCharge=true,AdditionalCharge=null,FirstBillingMonth=new(2026,10,1)};
  var result=await service.CreateAsync(input,id,default);if(!result.Success)throw new Exception(result.Error);
  var record=await service.GetAsync(result.Sin!,default);if(record?.AdditionalCharge!=0)throw new Exception("Blank charge did not save as zero");
  Console.WriteLine("PASS blank optional additional charge saves as zero");
  var billing=new BillingService(config);await billing.GenerateAsync(result.Sin!,new(2026,10,13),default);
  var view=await billing.GetAsync(result.Sin!,new(2026,10,13),default);
  if(view?.Rows.Count!=1||view.Rows[0].Year!=2026||view.Rows[0].Month!=10||record.StartDate!="2026-10-13")throw new Exception("Custom billing month or occupancy changed");
  Console.WriteLine("PASS chosen occupancy-month billing generated; occupancy date preserved");
  input.FirstBillingMonth=new(2026,9,1);var bad=await service.CreateAsync(input,id,default);if(bad.Success)throw new Exception("Early month accepted");
  Console.WriteLine("PASS billing month before occupancy rejected");
 }
}
