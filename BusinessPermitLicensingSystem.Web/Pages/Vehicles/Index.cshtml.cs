using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace BusinessPermitLicensingSystem.Web.Pages.Vehicles;

public class IndexModel(VehicleService vehicles, ILogger<IndexModel> logger) : PageModel
{
    public VehicleList? Result { get; private set; }
    public string? Error { get; private set; }
    public string Search { get; private set; } = "";
    public int CurrentPage { get; private set; }

    public async Task<IActionResult> OnGetResultsAsync(string? search, int pageNumber = 1)
    {
        await OnGetAsync(search, pageNumber);
        if (Error is not null) return StatusCode(503);
        return Partial("_Results", this);
    }

    public async Task OnGetAsync(string? search, int pageNumber = 1)
    {
        Search = search ?? "";
        CurrentPage = Math.Max(1, pageNumber);
        try { Result = await vehicles.ListAsync(Search, CurrentPage, HttpContext.RequestAborted); }
        catch (Exception exception)
        { logger.LogError(exception, "Vehicle list failed."); Error = "Vehicle permits could not be loaded."; }
    }
}
