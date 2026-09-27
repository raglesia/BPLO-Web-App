using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.UserAccounts;

public class EditModel(UserAccountService accounts, ILogger<EditModel> logger) : PageModel
{
    [BindProperty] public AccountInput Input { get; set; } = new();
    public UserAccountRecord? Account { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Account = await accounts.GetAsync(id, HttpContext.RequestAborted);
            if (Account is null) return NotFound();
            Input = new() { FullName = Account.FullName, Username = Account.Username, Position = Account.Position };
            return Page();
        }
        catch (Exception exception)
        { logger.LogError(exception, "User account edit page failed for {AccountId}", id); Error = "Account could not be loaded."; return Page(); }
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        try { Account = await accounts.GetAsync(id, HttpContext.RequestAborted); }
        catch (Exception exception)
        { logger.LogError(exception, "User account edit lookup failed for {AccountId}", id); Error = "Account could not be loaded."; return Page(); }
        if (Account is null) return NotFound();
        if (!ModelState.IsValid) { Error = "Correct the highlighted fields."; return Page(); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actorId))
        { Error = "Sign in again before editing an account."; return Page(); }
        try
        {
            var result = await accounts.EditAsync(id, Input.FullName, Input.Username, Input.Position,
                actorId, HttpContext.RequestAborted);
            if (!result.Success) { Error = result.Message; return Page(); }
            TempData["AccountNotice"] = actorId == id
                ? "Account updated. Your displayed name and username refresh after your next sign-in."
                : result.Message;
            return RedirectToPage("/UserAccounts/Index");
        }
        catch (Exception exception)
        { logger.LogError(exception, "User account edit failed for {AccountId}", id); Error = "Account was not updated. Try again."; return Page(); }
    }
}
