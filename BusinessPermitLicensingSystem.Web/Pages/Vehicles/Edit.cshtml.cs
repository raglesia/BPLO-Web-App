using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BusinessPermitLicensingSystem.Web.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BusinessPermitLicensingSystem.Web.Pages.Vehicles;

public class EditModel(VehicleRecordService records, ILogger<EditModel> logger) : PageModel
{
    [BindProperty]
    public VehicleInput Input { get; set; } = new();
    public bool IsNew => string.IsNullOrWhiteSpace(RouteData.Values["vin"]?.ToString());
    public string? Vin => RouteData.Values["vin"]?.ToString();

    public async Task<IActionResult> OnGetAsync(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin)) return Page();
        try
        {
            VehicleRecord? record = await records.GetAsync(vin, HttpContext.RequestAborted);
            if (record is null) return NotFound();
            if (record.Archived) return RedirectToPage("/Archive/Vehicles");
            Input = new() { CompanyName = record.CompanyName, DriverName = record.DriverName,
                PlateNo = record.PlateNo, SecRegNo = record.SecRegNo, DtiNumber = record.DtiNumber };
            return Page();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Vehicle edit form could not load.");
            TempData["Error"] = "Vehicle record could not be loaded.";
            return RedirectToPage("/Vehicles/Index");
        }
    }

    public async Task<IActionResult> OnPostAsync(string? vin)
    {
        if (!ModelState.IsValid) return Page();
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId)) return Forbid();
        try
        {
            VehicleRecordResult result = await records.SaveAsync(vin, Input.CompanyName, Input.DriverName ?? "",
                Input.PlateNo, Input.SecRegNo ?? "", Input.DtiNumber ?? "", userId, HttpContext.RequestAborted);
            if (result.Success)
            {
                TempData["VehicleNotice"] = string.IsNullOrWhiteSpace(vin)
                    ? $"Vehicle created. VIN {result.Vin}." : "Vehicle changes saved.";
                return RedirectToPage("/Vehicles/Details", new { vin = result.Vin });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "Vehicle could not be saved.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Vehicle record could not be saved.");
            ModelState.AddModelError(string.Empty, "Vehicle could not be saved. Try again later.");
        }
        return Page();
    }

    public class VehicleInput
    {
        [Required, StringLength(255)] public string CompanyName { get; set; } = "";
        [StringLength(255)] public string? DriverName { get; set; }
        [Required, StringLength(100)] public string PlateNo { get; set; } = "";
        [StringLength(100)] public string? SecRegNo { get; set; }
        [StringLength(100)] public string? DtiNumber { get; set; }
    }
}
