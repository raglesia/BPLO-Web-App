using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Administration;

namespace BusinessPermitLicensingSystem.Web.Pages.Account;

public class LogoutModel(AuditTrailService audit, ILogger<LogoutModel> logger) : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (User.Identity?.IsAuthenticated == true && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            try { await audit.RecordSessionAsync(userId, "Logout", CancellationToken.None); }
            catch (Exception exception) { logger.LogError(exception, "Failed to record Logout audit for user {UserId}.", userId); }
        }
        return RedirectToPage("/Account/Login");
    }
}
