using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Profiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Profiles;

public class ProfileEditModel(ProfileService profiles, ILogger<ProfileEditModel> logger) : PageModel
{
    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public string Sin { get; private set; } = "";
    public bool IsPaidRecord { get; private set; }
    public IReadOnlyList<RentalRate> Rates { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string sin)
    {
        Sin = sin;
        try
        {
            ProfileRecord? record = await profiles.GetAsync(sin, HttpContext.RequestAborted);
            if (record is null)
            {
                TempData["ArchiveNotice"] = "Profile is no longer active. Check archived profiles before editing.";
                return RedirectToPage("/Archive/Profiles");
            }
            Input = ProfileInput.FromRecord(record);
            IsPaidRecord = record.PaymentStatus == "Paid";
            Rates = await profiles.GetRatesAsync(HttpContext.RequestAborted);
            return Page();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Profile edit form could not be loaded.");
            return RedirectToPage("/Profiles/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(string sin)
    {
        Sin = sin;
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId)) return Forbid();
        if (!ModelState.IsValid) return await ShowFormAsync(sin);
        try
        {
            ProfileSaveResult result = await profiles.UpdateAsync(sin, Input, userId, HttpContext.RequestAborted);
            if (result.Success)
            {
                TempData["ProfileNotice"] = "Profile changes saved.";
                return RedirectToPage("/Profiles/Details", new { sin });
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Profile could not be updated.");
            ModelState.AddModelError(string.Empty, "Profile could not be saved. Try again later.");
        }
        return await ShowFormAsync(sin);
    }

    private async Task<IActionResult> ShowFormAsync(string sin)
    {
        try
        {
            ProfileRecord? record = await profiles.GetAsync(sin, HttpContext.RequestAborted);
            if (record is null)
            {
                TempData["ArchiveNotice"] = "Profile is no longer active. Check archived profiles before editing.";
                return RedirectToPage("/Archive/Profiles");
            }
            IsPaidRecord = record.PaymentStatus == "Paid";
            Rates = await profiles.GetRatesAsync(HttpContext.RequestAborted);
            return Page();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Profile edit form could not be reloaded.");
            return RedirectToPage("/Profiles/Index");
        }
    }
}
