using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Archive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Archive;

public class VehiclesModel(ArchiveService archive, ILogger<VehiclesModel> logger) : PageModel
{
    public IReadOnlyList<ArchivedVehicle> Records { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty] public bool ConfirmRestore { get; set; }

    public Task OnGetAsync() => LoadAsync();

    public async Task<IActionResult> OnPostRestoreAsync(string vin)
    {
        if (!ConfirmRestore) { Error = "Confirm restore before submitting."; await LoadAsync(); return Page(); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        { Error = "Sign in again before restoring."; await LoadAsync(); return Page(); }
        try
        {
            var result = await archive.VehicleAsync(vin, false, userId, HttpContext.RequestAborted);
            if (result.Success) { TempData["ArchiveNotice"] = result.Message; return RedirectToPage(new { Search }); }
            Error = result.Message;
        }
        catch (Exception exception) { logger.LogError(exception, "Vehicle restore failed for {Vin}", vin); Error = "Restore failed. No change was saved."; }
        await LoadAsync(); return Page();
    }

    private async Task LoadAsync()
    {
        Notice = TempData["ArchiveNotice"] as string;
        try { Records = await archive.VehiclesAsync(Search, HttpContext.RequestAborted); }
        catch (Exception exception) { logger.LogError(exception, "Archived vehicles could not be loaded."); Error = "Archived vehicles could not be loaded."; }
    }
}
