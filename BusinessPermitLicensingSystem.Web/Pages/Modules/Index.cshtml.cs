using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Modules;

public class ModuleModel : PageModel
{
    private static readonly Dictionary<string, string> Destinations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["stall-owners"] = "/Profiles/Index",
        ["rental-rates"] = "/RentalRates/Index",
        ["billing-payments"] = "/Profiles/Index",
        ["archive"] = "/Archive/Profiles",
        ["audit-trail"] = "/AuditTrail",
        ["vehicle-permits"] = "/Vehicles/Index",
        ["reports"] = "/Reports/Index"
    };

    public IActionResult OnGet(string module)
    {
        if (!Destinations.TryGetValue(module, out string? destination)) return NotFound();
        return RedirectToPage(destination);
    }
}
