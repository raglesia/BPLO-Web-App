using BusinessPermitLicensingSystem.Web.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BusinessPermitLicensingSystem.Web.Pages.Profiles;

public class BillingModel(BillingService billing, ILogger<BillingModel> logger) : PageModel
{
    public BillingView? Record { get; private set; }
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    public DateTime AsOf { get; private set; }
    [BindProperty] public string OrNumber { get; set; } = "";
    [BindProperty] public bool ConfirmPayment { get; set; }

    public async Task<IActionResult> OnGetAsync(string sin) => await LoadAsync(sin);

    public async Task<IActionResult> OnPostGenerateAsync(string sin)
    {
        try
        {
            int added = await billing.GenerateAsync(sin, DateTime.Today, HttpContext.RequestAborted);
            TempData["BillingNotice"] = $"Generated {added} missing billing period(s).";
            return RedirectToPage(new { sin });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Billing generation failed for {Sin}", sin);
            Error = "Billing generation failed. Try again later.";
            return await LoadAsync(sin);
        }
    }

    public async Task<IActionResult> OnPostPenaltiesAsync(string sin)
    {
        try
        {
            var amount = await billing.UpdatePenaltyAsync(sin, DateTime.Today, HttpContext.RequestAborted);
            TempData["BillingNotice"] = $"Profile penalty recalculated: {amount:N2}.";
            return RedirectToPage(new { sin });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Penalty update failed for {Sin}", sin);
            Error = "Penalty update failed. Try again later.";
            return await LoadAsync(sin);
        }
    }

    public async Task<IActionResult> OnPostPayAsync(string sin)
    {
        if (!ConfirmPayment)
        { Error = "Confirm that the full outstanding balance will be paid."; return await LoadAsync(sin); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        { Error = "Sign in again before recording payment."; return await LoadAsync(sin); }
        try
        {
            var result = await billing.PayAsync(sin, OrNumber, userId, DateTime.Now, HttpContext.RequestAborted);
            if (!result.Success)
            { Error = result.Error; return await LoadAsync(sin); }
            TempData["BillingNotice"] = $"Payment recorded. OR {result.OrNumber}; {result.Bills} period(s); amount {result.Amount:N2}.";
            return RedirectToPage(new { sin });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Payment failed for {Sin}", sin);
            Error = "Payment was not recorded. Refresh and try again.";
            return await LoadAsync(sin);
        }
    }

    private async Task<IActionResult> LoadAsync(string sin)
    {
        AsOf = DateTime.Today;
        Notice = TempData["BillingNotice"] as string;
        try
        {
            Record = await billing.GetAsync(sin, AsOf, HttpContext.RequestAborted);
            return Record is null ? NotFound() : Page();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Billing history failed for {Sin}", sin);
            Error = "Billing history could not be loaded.";
            return Page();
        }
    }
}
