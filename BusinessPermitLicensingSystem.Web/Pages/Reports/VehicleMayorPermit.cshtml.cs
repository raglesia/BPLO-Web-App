using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
namespace BusinessPermitLicensingSystem.Web.Pages.Reports;
public class VehicleMayorPermitModel(VehiclePaymentReportService payments, VehicleMayorPermitService permits, ILogger<VehicleMayorPermitModel> logger) : PageModel
{
    [BindProperty(SupportsGet=true)] public string? ReturnUrl { get; set; }
    public VehiclePaymentReport? Report { get; private set; }
    public string? PermitNumber { get; private set; }
    public string? Error { get; private set; }
    public DateTime PrintedAt { get; private set; }
    public async Task OnGetAsync(int id) => await LoadAsync(id);
    private async Task LoadAsync(int id)
    {
        PrintedAt=TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow,"Singapore Standard Time");
        try
        {
            var report=await payments.GetAsync(id,HttpContext.RequestAborted);
            if(report is null || report.Archived || string.IsNullOrWhiteSpace(report.OrNumber))
            { Error="An active Special Vehicle Permit with a recorded OR payment is required."; return; }
            if(report.Assessment is null || report.AssessmentWarning is not null)
            { Error=report.AssessmentWarning ?? "A trustworthy payment-time assessment snapshot is required before preparing this permit.";return; }
            if(report.AmountMismatch)
            { Error="The payment-time assessment differs from the recorded payment. Contact the administrator.";return; }
            var data=report.Assessment.Data;
            Report=report;
            if(new[]{report.Company,report.Driver,data.Location,data.LineOfBusiness,data.Description}.Any(string.IsNullOrWhiteSpace))
            { Error="Complete Company, Driver Name, Location, Line of Business and Description in the vehicle profile and saved assessment first.";return; }
            Report=report;PermitNumber=await permits.GetNumberAsync(id,HttpContext.RequestAborted);
        }
        catch(Exception ex){logger.LogError(ex,"Mayor's Permit load failed for payment {Id}",id);Error="Mayor’s Permit could not be loaded. Please contact the administrator.";Report=null;}
    }
    public async Task<IActionResult> OnPostPrepareAsync(int id)
    {
        await LoadAsync(id);
        if(Report is null || Error is not null)return Page();
        if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out int userId))
        {Error="Sign in again before preparing a permit.";return Page();}
        try { await permits.PrepareAsync(id,userId,PrintedAt,HttpContext.RequestAborted);return RedirectToPage(new{id,returnUrl=Url.IsLocalUrl(ReturnUrl)?ReturnUrl:null}); }
        catch(Exception ex){logger.LogError(ex,"Mayor's Permit preparation failed for {Id}",id);Error="Permit preparation failed. Refresh and try again.";return Page();}
    }
}


