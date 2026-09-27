using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Vehicles;

public class IndexModel(VehicleService vehicles, ILogger<IndexModel> logger) : PageModel
{
    public VehicleList? Result { get; private set; }
    public string? Error { get; private set; }
    public string Search { get; private set; } = "";
    public int CurrentPage { get; private set; }

    public async Task OnGetAsync(string? search, int page = 1)
    {
        Search = search ?? "";
        CurrentPage = Math.Max(1, page);
        try { Result = await vehicles.ListAsync(Search, CurrentPage, HttpContext.RequestAborted); }
        catch (Exception exception)
        { logger.LogError(exception, "Vehicle list failed."); Error = "Vehicle permits could not be loaded."; }
    }
}
