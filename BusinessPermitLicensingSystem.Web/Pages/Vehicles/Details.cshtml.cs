using BusinessPermitLicensingSystem.Web.Vehicles;
using BusinessPermitLicensingSystem.Web.Archive;
using BusinessPermitLicensingSystem.Web.Pages.Payments;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace BusinessPermitLicensingSystem.Web.Pages.Vehicles;
public class DetailsModel(VehicleService vehicles, ArchiveService archive, ILogger<VehiclePaymentsModel> logger) : VehiclePaymentsModel(vehicles,logger)
{
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
        catch (Exception exception) { Log.LogError(exception, "Vehicle archive failed for {Vin}", vin); Error = "Archive failed. No change was saved."; }
        return await LoadAsync(vin, true);
    }

}
