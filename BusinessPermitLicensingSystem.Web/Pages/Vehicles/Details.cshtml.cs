using BusinessPermitLicensingSystem.Web.Vehicles;
using BusinessPermitLicensingSystem.Web.Archive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BusinessPermitLicensingSystem.Web.Pages.Vehicles;

public class DetailsModel(VehicleService vehicles, ArchiveService archive, ILogger<DetailsModel> logger) : PageModel
{
    public VehicleDetails? Vehicle { get; private set; }
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    [BindProperty] public List<decimal> FeeAmounts { get; set; } = new();
    [BindProperty] public List<string> OtherDescriptions { get; set; } = new();
    [BindProperty] public string OrNumber { get; set; } = "";
    [BindProperty] public bool ConfirmPayment { get; set; }
    [BindProperty] public bool ConfirmArchive { get; set; }

    public async Task<IActionResult> OnPostArchiveAsync(string vin)
    {
        if (!ConfirmArchive) { Error = "Confirm archive before submitting."; return await LoadAsync(vin, true); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        { Error = "Sign in again before archiving."; return await LoadAsync(vin, true); }
        try
        {
            var result = await archive.VehicleAsync(vin, true, userId, HttpContext.RequestAborted);
            if (result.Success) { TempData["ArchiveNotice"] = result.Message; return RedirectToPage("/Archive/Vehicles"); }
            Error = result.Message;
        }
        catch (Exception exception) { logger.LogError(exception, "Vehicle archive failed for {Vin}", vin); Error = "Archive failed. No change was saved."; }
        return await LoadAsync(vin, true);
    }

    public async Task<IActionResult> OnGetAsync(string vin) => await LoadAsync(vin, true);

    public async Task<IActionResult> OnPostSaveDraftAsync(string vin)
    {
        if (ModelState.Any(entry => entry.Key.StartsWith("FeeAmounts[", StringComparison.Ordinal) && entry.Value?.Errors.Count > 0))
        { Error = "Enter valid fee amounts using numbers with at most two decimal places."; return await LoadAsync(vin, false); }
        try
        {
            var result = await vehicles.SaveDraftAsync(vin, DateTime.Today.Year,
                FeeAmounts, OtherDescriptions, DateTime.Today, HttpContext.RequestAborted);
            if (!result.Success)
            { Error = result.Error; return await LoadAsync(vin, false); }
            TempData["VehicleNotice"] = $"{DateTime.Today.Year} fee draft saved: ₱{result.Total:N2}. No payment was recorded.";
            return RedirectToPage(new { vin });
        }
        catch (Exception exception)
        { logger.LogError(exception, "Vehicle fee draft failed for {Vin}", vin);
          Error = "Fee draft could not be saved."; return await LoadAsync(vin, false); }
    }

    public async Task<IActionResult> OnPostPayAsync([FromRoute] string vin)
    {
        if (!ConfirmPayment)
        { Error = "Confirm vehicle payment before recording it."; return await LoadAsync(vin, true); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        { Error = "Sign in again before recording payment."; return await LoadAsync(vin, true); }
        try
        {
            var result = await vehicles.PayAsync(vin, OrNumber, DateTime.Today.Year,
                userId, DateTime.Now, HttpContext.RequestAborted);
            if (!result.Success)
            { Error = result.Error; return await LoadAsync(vin, true); }
            TempData["VehicleNotice"] = $"Vehicle payment recorded for {result.Year}. OR {result.OrNumber}; amount ₱{result.Amount:N2}.";
            return RedirectToPage(new { vin });
        }
        catch (Exception exception)
        { logger.LogError(exception, "Vehicle payment failed for {Vin}", vin);
          Error = "Vehicle payment was not recorded. Refresh and try again.";
          return await LoadAsync(vin, true); }
    }

    private async Task<IActionResult> LoadAsync(string vin, bool hydrate)
    {
        Notice = TempData["VehicleNotice"] as string;
        try
        {
            Vehicle = await vehicles.GetAsync(vin, DateTime.Today, HttpContext.RequestAborted);
            if (Vehicle is null) return NotFound();
            if (hydrate)
            {
                FeeAmounts = Vehicle.Draft?.Amounts.ToList() ?? Enumerable.Repeat(0m, VehicleFeeDraft.FeeNames.Length).ToList();
                OtherDescriptions = Vehicle.Draft?.Data.OtherDescriptions.ToList() ?? ["", "", "", ""];
            }
            else
            {
                while (FeeAmounts.Count < VehicleFeeDraft.FeeNames.Length) FeeAmounts.Add(0m);
                while (OtherDescriptions.Count < 4) OtherDescriptions.Add("");
            }
            return Page();
        }
        catch (Exception exception)
        { logger.LogError(exception, "Vehicle details failed for {Vin}", vin);
          Error = "Vehicle details could not be loaded."; return Page(); }
    }
}
