using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Reports;

public class BillingModel(ReportService reports, ILogger<BillingModel> logger) : PageModel
{
    public ProfileReport? Report { get; private set; }
    public string? Error { get; private set; }
    public string? Sin { get; private set; }
    public async Task OnGetAsync(string? sin)
    {
        Sin = sin;
        if (string.IsNullOrWhiteSpace(sin) || sin.Length > 100) { Error = "Enter a valid SIN."; return; }
        try { Report = await reports.ProfileAsync(sin, HttpContext.RequestAborted);
            if (Report is null) Error = "Profile was not found."; }
        catch (Exception exception) { logger.LogError(exception, "Billing report failed for {Sin}", sin); Error = "Report could not be loaded."; }
    }
}
