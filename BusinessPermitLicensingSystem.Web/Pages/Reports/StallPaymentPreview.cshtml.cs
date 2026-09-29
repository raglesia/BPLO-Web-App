using BusinessPermitLicensingSystem.Web.Billing;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace BusinessPermitLicensingSystem.Web.Pages.Reports;
public class StallPaymentPreviewModel(BillingService billing, ILogger<StallPaymentPreviewModel> logger) : PageModel
{
    public BillingView? Record { get; private set; }
    public PaymentRecord? Payment { get; private set; }
    public string Sin { get; private set; } = "";
    public string? Error { get; private set; }
    public async Task OnGetAsync(string sin, string orNumber)
    {
        Sin = sin;
        try {
            Record = await billing.GetAsync(sin, DateTime.Today, HttpContext.RequestAborted);
            Payment = Record?.Payments.SingleOrDefault(x => x.OrNumber == orNumber);
            if (Payment is null) Error = "Recorded payment was not found.";
        } catch(Exception exception) { logger.LogError(exception,"Stall payment preview failed"); Error = "Payment report could not be loaded."; }
    }
}
