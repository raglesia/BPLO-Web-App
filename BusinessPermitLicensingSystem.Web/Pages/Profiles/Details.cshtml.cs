using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Archive;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Profiles;

public class ProfileDetailsModel(ProfileService profiles, ArchiveService archive, ILogger<ProfileDetailsModel> logger) : PageModel
{
    public ProfileRecord? Profile { get; private set; }
    public string? LoadError { get; private set; }
    public string? Notice { get; private set; }
    [BindProperty] public bool ConfirmArchive { get; set; }

    public async Task<IActionResult> OnPostArchiveAsync(string sin)
    {
        if (!ConfirmArchive) { LoadError = "Confirm archive before submitting."; return await ReloadAsync(sin); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        { LoadError = "Sign in again before archiving."; return await ReloadAsync(sin); }
        try
        {
            var result = await archive.ProfileAsync(sin, true, userId, HttpContext.RequestAborted);
            if (result.Success) { TempData["ArchiveNotice"] = result.Message; return RedirectToPage("/Archive/Profiles"); }
            if (result.Message == "Record is already archived.")
            { TempData["ArchiveNotice"] = result.Message; return RedirectToPage("/Archive/Profiles"); }
            LoadError = result.Message;
        }
        catch (Exception exception) { logger.LogError(exception, "Profile archive failed for {Sin}", sin); LoadError = "Archive failed. No change was saved."; }
        return await ReloadAsync(sin);
    }

    public Task<IActionResult> OnGetAsync(string sin) => ReloadAsync(sin);

    private async Task<IActionResult> ReloadAsync(string sin)
    {
        Notice = TempData["ProfileNotice"] as string;
        try
        {
            Profile = await profiles.GetAsync(sin, HttpContext.RequestAborted);
            return Profile is null ? NotFound() : Page();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Profile details could not be loaded.");
            LoadError = "Profile details could not be loaded. Try again later.";
            return Page();
        }
    }
}
