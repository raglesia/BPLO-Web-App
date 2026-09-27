using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Administration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.RentalRates;

public class EditModel(RentalRateService rates, ILogger<EditModel> logger) : PageModel
{
    [BindProperty] public RateInput Input { get; set; } = new();
    public bool IsNew { get; private set; }
    public string? Section { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? section)
    {
        IsNew = string.IsNullOrWhiteSpace(section); Section = section;
        if (IsNew) return Page();
        try
        {
            var record = (await rates.ListAsync(HttpContext.RequestAborted)).FirstOrDefault(x => x.Section == section);
            if (record is null) return NotFound();
            Input = new() { Section = record.Section, RateType = record.RateType,
                RatePerSqm = record.RatePerSqm, FlatRate = record.FlatRate };
            return Page();
        }
        catch (Exception exception) { logger.LogError(exception, "Rental rate edit could not be loaded."); Error = "Rate could not be loaded."; return Page(); }
    }

    public async Task<IActionResult> OnPostAsync(string? section)
    {
        IsNew = string.IsNullOrWhiteSpace(section); Section = section;
        if (!ModelState.IsValid) { Error = "Correct the highlighted fields."; return Page(); }
        if (!Input.Confirm) { Error = "Confirm the rate change before saving."; return Page(); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        { Error = "Sign in again before saving a rate."; return Page(); }
        string target = IsNew ? Input.Section : section!;
        try
        {
            var result = await rates.SaveAsync(target, Input.RateType, Input.RatePerSqm, Input.FlatRate,
                IsNew, userId, HttpContext.RequestAborted);
            if (!result.Success) { Error = result.Message; return Page(); }
            TempData["RateNotice"] = result.Message;
            return RedirectToPage("/RentalRates/Index");
        }
        catch (Exception exception) { logger.LogError(exception, "Rental rate save failed for {Section}", target); Error = "Rate was not saved. Try again."; return Page(); }
    }

    public class RateInput
    {
        public string Section { get; set; } = "";
        public string RateType { get; set; } = "PerSqm";
        public decimal RatePerSqm { get; set; }
        public decimal FlatRate { get; set; }
        public bool Confirm { get; set; }
    }
}
