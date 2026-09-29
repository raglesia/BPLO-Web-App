using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Reports;

public class VehicleBillingPreviewModel(VehicleService vehicles,
    ILogger<VehicleBillingPreviewModel> logger) : PageModel
{
    public VehicleDetails? Vehicle { get; private set; }
    public string? Error { get; private set; }
    public string ProcessedBy => User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
    public string Position => User.FindFirst("Position")?.Value ?? "";
    public DateTime PrintedAt { get; private set; }

    public async Task OnGetAsync(string vin)
    {
        try
        {
            var vehicle = await vehicles.GetAsync(vin, DateTime.Today, HttpContext.RequestAborted);
            if (vehicle is null) Error = "Vehicle was not found.";
            else if (vehicle.Archived) Error = "Current-year billing is unavailable for archived vehicles.";
            else if (vehicle.Draft is null) Error = "Save a valid current-year fee assessment before printing billing.";
            else { Vehicle = vehicle; PrintedAt = DateTime.Now; }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Vehicle billing preview failed for {Vin}", vin);
            Error = "Vehicle billing report could not be loaded.";
        }
    }
}
