using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.RentalRates;

public class IndexModel(RentalRateService rates, ILogger<IndexModel> logger) : PageModel
{
    public IReadOnlyList<RentalRateRecord> Records { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    public async Task OnGetAsync()
    {
        Notice = TempData["RateNotice"] as string;
        try { Records = await rates.ListAsync(HttpContext.RequestAborted); }
        catch (Exception exception) { logger.LogError(exception, "Rental rates could not be loaded."); Error = "Rental rates could not be loaded."; }
    }
}
