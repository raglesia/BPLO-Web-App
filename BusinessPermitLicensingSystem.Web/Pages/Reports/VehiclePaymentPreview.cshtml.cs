using BusinessPermitLicensingSystem.Web.Reports;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Reports;

public class VehiclePaymentPreviewModel(VehiclePaymentReportService reports,
    ILogger<VehiclePaymentPreviewModel> logger) : PageModel
{
    public IReadOnlyList<VehiclePaymentReport> Reports { get; private set; } = [];
    public DateTime PrintedAt { get; private set; }
    public string? Error { get; private set; }

    public async Task OnGetAsync(int[]? selected)
    {
        var ids = selected ?? [];
        Error = VehiclePaymentSelection.Validate(ids, requireOne: true);
        if (Error is not null) return;
        try
        {
            var list = new List<VehiclePaymentReport>();
            foreach (int id in ids)
            {
                var report = await reports.GetAsync(id, HttpContext.RequestAborted);
                if (report is null) { Error = "A selected annual payment was not found. Choose recorded payments again."; return; }
                list.Add(report);
            }
            PrintedAt = DateTime.Now;
            Reports = list;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Vehicle payment preview could not be loaded.");
            Error = "Vehicle payment report could not be loaded.";
        }
    }
}
