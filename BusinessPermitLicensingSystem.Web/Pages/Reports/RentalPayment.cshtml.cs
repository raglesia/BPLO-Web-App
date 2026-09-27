using BusinessPermitLicensingSystem.Web.Profiles;
using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Reports;

public class RentalPaymentModel(ProfileService profiles, ILogger<RentalPaymentModel> logger) : PageModel
{
    public string Search { get; private set; } = "";
    public IReadOnlyList<ProfileRecord> Results { get; private set; } = [];
    public IReadOnlyList<ProfileRecord> Selected { get; private set; } = [];
    public int PageNumber { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;
    public string? Error { get; private set; }

    public async Task OnGetAsync(string? search, string[]? selected, string? add, string? remove, int pageNumber = 1)
    {
        Search = (search ?? "").Trim();
        if (Search.Length > 255) Search = Search[..255];
        var ids = (selected ?? []).ToList();
        if (!string.IsNullOrWhiteSpace(remove)) ids.RemoveAll(x => string.Equals(x, remove, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(add)) ids.Add(add);
        Error = RentalPaymentSelection.Validate(ids);
        if (Error is not null) return;
        try
        {
            var chosen = new List<ProfileRecord>();
            foreach (var id in ids)
            {
                var profile = await profiles.GetAsync(id, HttpContext.RequestAborted);
                if (profile is null) { Error = "A selected stall owner is no longer active. Remove it and search again."; return; }
                chosen.Add(profile);
            }
            Selected = chosen;
            var result = await profiles.ListAsync(Search, pageNumber, HttpContext.RequestAborted);
            Results = result.Profiles;
            TotalPages = Math.Max(1, (result.TotalCount + ProfileService.PageSize - 1) / ProfileService.PageSize);
            PageNumber = Math.Clamp(pageNumber, 1, TotalPages);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rental payment selection could not be loaded.");
            Error = "Stall owners could not be loaded. Try again later.";
        }
    }
}
