using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Profiles;

public class ProfileCreateModel(ProfileService profiles, ILogger<ProfileCreateModel> logger) : PageModel
{
    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public IReadOnlyList<RentalRate> Rates { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadRatesAsync() ? Page() : RedirectToPage("/Profiles/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return await ShowFormAsync();
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId)) return Forbid();

        try
        {
            ProfileSaveResult result = await profiles.CreateAsync(Input, userId, HttpContext.RequestAborted);
            if (result.Success)
            {
                TempData["ProfileNotice"] = $"Profile created. SIN {result.Sin}.";
                return RedirectToPage("/Profiles/Details", new { sin = result.Sin });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Profile could not be created.");
            ModelState.AddModelError(string.Empty, "Profile could not be saved. Try again later.");
        }
        return await ShowFormAsync();
    }

    private async Task<IActionResult> ShowFormAsync() => await LoadRatesAsync() ? Page() : RedirectToPage("/Profiles/Index");

    private async Task<bool> LoadRatesAsync()
    {
        try
        {
            Rates = await profiles.GetRatesAsync(HttpContext.RequestAborted);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rental rates could not be loaded for profile form.");
            return false;
        }
    }
}
