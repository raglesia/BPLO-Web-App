using BusinessPermitLicensingSystem.Web.Billing;
using BusinessPermitLicensingSystem.Web.Pages.Payments;
using Microsoft.AspNetCore.Mvc;
namespace BusinessPermitLicensingSystem.Web.Pages.Profiles;
public class BillingModel(BillingService billing, ILogger<StallPaymentsModel> logger) : StallPaymentsModel(billing,logger)
{
    public override Task<IActionResult> OnGetAsync(string sin) => Task.FromResult<IActionResult>(RedirectToPage("/Payments/Stalls",new {sin}));
}
