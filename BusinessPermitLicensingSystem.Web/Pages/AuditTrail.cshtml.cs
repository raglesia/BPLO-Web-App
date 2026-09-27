using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages;

public class AuditTrailModel(AuditTrailService audit, ILogger<AuditTrailModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Category { get; set; } = "users";
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public AuditPage? Result { get; private set; }
    public string? Error { get; private set; }
    public async Task OnGetAsync()
    {
        Category = Category == "activity" ? "activity" : "users";
        try { Result = await audit.ListAsync(Category, Search, PageNumber, HttpContext.RequestAborted); }
        catch (Exception exception) { logger.LogError(exception, "Audit trail could not be loaded."); Error = "Audit trail could not be loaded."; }
    }
}
