using BusinessPermitLicensingSystem.Web.Administration;
using BusinessPermitLicensingSystem.Web.Pages.UserAccounts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Account;

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
        try
        {
            var result = await accounts.CreatePublicAsync(Input.FullName, Input.Username, Input.Position,
                Password, HttpContext.RequestAborted);
            if (!result.Success) { Error = result.Message; return Page(); }
            TempData["AccountCreated"] = true;
            return RedirectToPage("/Account/Login");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Public account creation failed.");
            Error = "Account was not created. Try again.";
            return Page();
        }
    }
}
