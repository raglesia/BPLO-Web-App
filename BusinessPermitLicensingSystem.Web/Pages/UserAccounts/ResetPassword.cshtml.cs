using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.UserAccounts;

public class ResetPasswordModel(UserAccountService accounts, ILogger<ResetPasswordModel> logger) : PageModel
{
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public string? ConfirmPassword { get; set; }
    public UserAccountRecord? Account { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id) => await LoadAsync(id);

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var page = await LoadAsync(id);
        if (page is not PageResult || Account is null) return page;
        if (string.IsNullOrWhiteSpace(Password)) { Error = "New Password is required."; return Page(); }
        if (string.IsNullOrWhiteSpace(ConfirmPassword)) { Error = "Confirm Password is required."; return Page(); }
        if (Password != ConfirmPassword) { Error = "Password and Confirm Password do not match."; return Page(); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actorId))
        { Error = "Sign in again before resetting a password."; return Page(); }
        try
        {
            var result = await accounts.ResetPasswordAsync(id, Password, actorId, HttpContext.RequestAborted);
            if (!result.Success) { Error = result.Message; return Page(); }
            TempData["AccountNotice"] = result.Message;
            return RedirectToPage("/UserAccounts/Index");
        }
        catch (Exception exception)
        { logger.LogError(exception, "Password reset failed for {AccountId}", id); Error = "Password was not reset. Try again."; return Page(); }
    }

    private async Task<IActionResult> LoadAsync(int id)
    {
        try
        {
            Account = await accounts.GetAsync(id, HttpContext.RequestAborted);
            return Account is null ? NotFound() : Page();
        }
        catch (Exception exception)
        { logger.LogError(exception, "Password reset page failed for {AccountId}", id); Error = "Account could not be loaded."; return Page(); }
    }
}
