using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.UserAccounts;

public class IndexModel(UserAccountService accounts, ILogger<IndexModel> logger) : PageModel
{
    public UserAccountPage Result { get; private set; } = new([], 0, 1);
    public string Search { get; private set; } = "";
    public string? Error { get; private set; }
    public string? Notice { get; private set; }

    public async Task OnGetAsync(string? search, int pageNumber = 1)
    {
        Search = (search ?? "").Trim();
        Notice = TempData["AccountNotice"] as string;
        try { Result = await accounts.ListAsync(Search, pageNumber, HttpContext.RequestAborted); }
        catch (Exception exception)
        { logger.LogError(exception, "User accounts could not be listed."); Error = "User accounts could not be loaded."; }
    }
}
