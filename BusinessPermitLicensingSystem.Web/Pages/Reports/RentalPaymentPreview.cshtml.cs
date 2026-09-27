using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Reports;

public class RentalPaymentPreviewModel(ReportService reports, ILogger<RentalPaymentPreviewModel> logger) : PageModel
{
    public IReadOnlyList<ProfileReport> Reports { get; private set; } = [];
    public string[] SelectedIds { get; private set; } = [];
    public DateTime PrintedAt { get; private set; }
    public string ProcessedBy => User.FindFirst("FullName")?.Value ?? User.Identity?.Name ?? "";
    public string Position => User.FindFirst("Position")?.Value ?? "";
    public string? Error { get; private set; }

    public async Task OnGetAsync(string[]? selected)
    {
        SelectedIds = selected ?? [];
        Error = RentalPaymentSelection.Validate(SelectedIds, requireOne: true);
        if (Error is not null) return;
        try
        {
            var list = new List<ProfileReport>();
            foreach (var id in SelectedIds)
            {
                var report = await reports.ProfileAsync(id, HttpContext.RequestAborted);
                if (report is null || report.Archived)
                {
                    Error = "A selected stall owner is no longer active. Return to selection and choose an active owner.";
                    return;
                }
                list.Add(report);
            }
            PrintedAt = DateTime.Now;
            Reports = list;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Rental payment preview could not be loaded.");
            Error = "Rental payment report could not be loaded.";
        }
    }
}
