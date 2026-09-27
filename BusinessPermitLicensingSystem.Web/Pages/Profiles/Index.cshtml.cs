using BusinessPermitLicensingSystem.Web.Profiles;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Profiles;

public class ProfilesIndexModel(ProfileService profiles, ILogger<ProfilesIndexModel> logger) : PageModel
{
    public string Search { get; private set; } = "";
    public int PageNumber { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;
    public int TotalCount { get; private set; }
    public IReadOnlyList<ProfileRecord> Profiles { get; private set; } = [];
    public string? LoadError { get; private set; }

    public async Task OnGetAsync(string? search, int pageNumber = 1)
    {
        Search = (search ?? "").Trim();
        if (Search.Length > 255) Search = Search[..255];
        try
        {
            var result = await profiles.ListAsync(Search, pageNumber, HttpContext.RequestAborted);
            Profiles = result.Profiles;
            TotalCount = result.TotalCount;
            TotalPages = Math.Max(1, (TotalCount + ProfileService.PageSize - 1) / ProfileService.PageSize);
            PageNumber = Math.Clamp(pageNumber, 1, TotalPages);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Profile list could not be loaded.");
            LoadError = "Profiles could not be loaded. Try again later.";
        }
    }
}
