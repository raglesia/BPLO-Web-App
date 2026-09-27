using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Account;

public class LoginModel(UserAuthenticationService users, ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToPage("/Index");
        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        AuthenticatedUser? user;
        try
        {
            user = await users.AuthenticateAsync(
                Input.Username, Input.Password, HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Login database access failed.");
            ModelState.AddModelError(string.Empty, "Login is temporarily unavailable.");
            return Page();
        }

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return Page();
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
            new Claim("Position", user.Position)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Index");
    }

    public class LoginInput
    {
        [Required]
        [StringLength(255)]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
