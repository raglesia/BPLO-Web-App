using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Reports;

public class MonthlyModel(ReportService reports, ILogger<MonthlyModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public int FromYear { get; set; } = DateTime.Today.Year;
    [BindProperty(SupportsGet = true)] public int FromMonth { get; set; } = DateTime.Today.Month;
    [BindProperty(SupportsGet = true)] public int ToYear { get; set; } = DateTime.Today.Year;
    [BindProperty(SupportsGet = true)] public int ToMonth { get; set; } = DateTime.Today.Month;
    public MonthlyReport? Report { get; private set; }
    public string? Error { get; private set; }

    public async Task OnGetAsync()
    {
        if (!Request.Query.ContainsKey("FromYear")) return;
        Error = ReportService.ValidateRange(FromYear, FromMonth, ToYear, ToMonth);
        if (Error is not null) return;
        try { Report = await reports.MonthlyAsync(FromYear, FromMonth, ToYear, ToMonth, HttpContext.RequestAborted); }
        catch (Exception exception) { logger.LogError(exception, "Monthly report failed."); Error = "Report could not be loaded."; }
    }

    public async Task<IActionResult> OnGetDownloadAsync()
    {
        Error = ReportService.ValidateRange(FromYear, FromMonth, ToYear, ToMonth);
        if (Error is not null) return Page();
        try
        {
            Report = await reports.MonthlyAsync(FromYear, FromMonth, ToYear, ToMonth, HttpContext.RequestAborted);
            string preparedBy = User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
            var bytes = reports.MonthlyExcel(Report, preparedBy, DateTime.Now);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"MonthlyReport_{FromYear}-{FromMonth:D2}_to_{ToYear}-{ToMonth:D2}.xlsx");
        }
        catch (Exception exception) { logger.LogError(exception, "Monthly report download failed."); Error = "Report download failed."; return Page(); }
    }
}
