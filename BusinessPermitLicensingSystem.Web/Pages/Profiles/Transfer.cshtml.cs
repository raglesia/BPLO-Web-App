using BusinessPermitLicensingSystem.Web.Transfer;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Profiles;

public class TransferModel(TransferService transfer, ILogger<TransferModel> logger) : PageModel
{
    [BindProperty] public IFormFile? Upload { get; set; }
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    public ImportSummary? Result { get; private set; }
    public string? Error { get; private set; }
    public void OnGet()
    {
        if (TempData["ProfileImportSummary"] is string json)
            Result = JsonSerializer.Deserialize<ImportSummary>(json);
    }

    public async Task<IActionResult> OnPostImportAsync()
    {
        if (Upload is null) { Error = "Choose a CSV or XLSX file."; return Page(); }
        try
        {
            Result = ImportSummary.From(await transfer.ImportAsync(Upload, false, HttpContext.RequestAborted));
            TempData["ProfileImportSummary"] = JsonSerializer.Serialize(Result);
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is ArgumentException or CsvHelper.CsvHelperException or InvalidDataException or FormatException)
        { Error = "The file could not be imported: " + (exception is ArgumentException ? exception.Message : "Check the file format and headers."); }
        catch (Exception exception) { logger.LogError(exception, "Profile import failed."); Error = "Import failed. Check the file and try again."; }
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        try { return File(await transfer.ExportAsync(false, Search, HttpContext.RequestAborted),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Stall_Owners_Profiling.xlsx"); }
        catch (Exception exception) { logger.LogError(exception, "Profile export failed."); Error = "Export failed. Try again."; return Page(); }
    }
}
