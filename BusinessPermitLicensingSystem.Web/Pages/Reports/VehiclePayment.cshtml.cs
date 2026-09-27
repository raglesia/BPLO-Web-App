using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Reports;

public class VehiclePaymentModel(VehiclePaymentReportService reports, ILogger<VehiclePaymentModel> logger) : PageModel
{
    public string Search { get; private set; } = "";
    public int? Year { get; private set; }
    public IReadOnlyList<VehiclePaymentReport> Results { get; private set; } = [];
    public IReadOnlyList<VehiclePaymentReport> Selected { get; private set; } = [];
    public int PageNumber { get; private set; } = 1;
    public int TotalPages { get; private set; } = 1;
    public string? Error { get; private set; }

    public async Task OnGetAsync(string? search, int? year, int[]? selected, int? add, int? remove, int pageNumber = 1)
    {
        Search = (search ?? "").Trim();
        if (Search.Length > 255) Search = Search[..255];
        Year = year;
        if (year is not null && year is < 2000 or > 2100)
        { Error = "Choose a valid permit year."; return; }
        var ids = (selected ?? []).ToList();
        if (remove is not null) ids.RemoveAll(x => x == remove);
        if (add is not null) ids.Add(add.Value);
        Error = VehiclePaymentSelection.Validate(ids);
        if (Error is not null) return;
        try
        {
            var chosen = new List<VehiclePaymentReport>();
            foreach (int id in ids)
            {
                var report = await reports.GetAsync(id, HttpContext.RequestAborted);
                if (report is null) { Error = "A selected payment is no longer available. Choose payments again."; return; }
                chosen.Add(report);
            }
            Selected = chosen;
            var result = await reports.SearchAsync(Search, year, pageNumber, HttpContext.RequestAborted);
            Results = result.Items;
            TotalPages = Math.Max(1, (result.Total + VehiclePaymentReportService.PageSize - 1) / VehiclePaymentReportService.PageSize);
            PageNumber = Math.Clamp(pageNumber, 1, TotalPages);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Vehicle payment selection could not be loaded.");
            Error = "Recorded vehicle payments could not be loaded.";
        }
    }
}
