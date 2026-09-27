using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.UserAccounts;

public class CreateModel(UserAccountService accounts, ILogger<CreateModel> logger) : PageModel
{
    [BindProperty] public AccountInput Input { get; set; } = new();
    [BindProperty] public string? Password { get; set; }
    [BindProperty] public string? ConfirmPassword { get; set; }
    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) { Error = "Correct the highlighted fields."; return Page(); }
        if (string.IsNullOrWhiteSpace(Password)) { Error = "Password is required."; return Page(); }
        if (string.IsNullOrWhiteSpace(ConfirmPassword)) { Error = "Confirm Password is required."; return Page(); }
        if (Password != ConfirmPassword) { Error = "Password and Confirm Password do not match."; return Page(); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int actorId))
        { Error = "Sign in again before creating an account."; return Page(); }
        try
        {
            var result = await accounts.CreateAsync(Input.FullName, Input.Username, Input.Position,
                Password, actorId, HttpContext.RequestAborted);
            if (!result.Success) { Error = result.Message; return Page(); }
            TempData["AccountNotice"] = result.Message;
            return RedirectToPage("/UserAccounts/Index");
        }
        catch (Exception exception)
        { logger.LogError(exception, "User account creation failed."); Error = "Account was not created. Try again."; return Page(); }
    }
}
