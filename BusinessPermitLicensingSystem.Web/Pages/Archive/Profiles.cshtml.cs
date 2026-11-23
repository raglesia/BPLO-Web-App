using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Archive;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Archive;

public class ProfilesModel(ArchiveService archive, ILogger<ProfilesModel> logger) : PageModel
{
    public IReadOnlyList<ArchivedProfile> Records { get; private set; } = [];
    public string? Error { get; private set; }
    public string? Notice { get; private set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int TotalCount { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)ArchiveService.PageSize));
    [BindProperty] public bool ConfirmRestore { get; set; }

    public Task OnGetAsync() => LoadAsync();

    public async Task<IActionResult> OnPostRestoreAsync(string sin)
    {
        if (!ConfirmRestore) { Error = "Confirm restore before submitting."; await LoadAsync(); return Page(); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        { Error = "Sign in again before restoring."; await LoadAsync(); return Page(); }
        try
        {
            var result = await archive.ProfileAsync(sin, false, userId, HttpContext.RequestAborted);
            if (result.Success) { TempData["ArchiveNotice"] = result.Message; return RedirectToPage(new { Search, PageNumber }); }
            Error = result.Message;
        }
        catch (Exception exception) { logger.LogError(exception, "Profile restore failed for {Sin}", sin); Error = "Restore failed. No change was saved."; }
        await LoadAsync(); return Page();
    }

    private async Task LoadAsync()
    {
        Notice = TempData["ArchiveNotice"] as string;
        try
        {
            PageNumber = Math.Max(1, PageNumber);
            var result = await archive.ProfilesAsync(Search, PageNumber, HttpContext.RequestAborted);
            if (result.Items.Count == 0 && PageNumber > 1)
            {
                PageNumber = 1;
                result = await archive.ProfilesAsync(Search, PageNumber, HttpContext.RequestAborted);
            }
            Records = result.Items;
            TotalCount = result.TotalCount;
        }
        catch (Exception exception) { logger.LogError(exception, "Archived profiles could not be loaded."); Error = "Archived profiles could not be loaded."; }
    }
}
