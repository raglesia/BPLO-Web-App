using System.ComponentModel.DataAnnotations;

namespace BusinessPermitLicensingSystem.Web.Profiles;

public sealed record ProfileRecord(
    string Sin, string FullName, string BusinessName, string BusinessSection,
    string StallNumber, string StallSize, decimal MonthlyRental, string PaymentStatus,
    string StartDate, decimal Penalty, decimal AdditionalCharge);

public sealed record RentalRate(string Section, decimal RatePerSqm, decimal FlatRate, string RateType);

public sealed record ProfileListResult(IReadOnlyList<ProfileRecord> Profiles, int TotalCount);

public sealed record ProfileSaveResult(string? Sin, string? Error = null, string? Field = null)
{
    public bool Success => Error is null;
}

public sealed class ProfileInput
{
    [Required, StringLength(255)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, StringLength(255)]
    [Display(Name = "Business name")]
    public string BusinessName { get; set; } = "";

    [Required, StringLength(255)]
    [Display(Name = "Business section")]
    public string BusinessSection { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Stall number")]
    public string StallNumber { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Stall size")]
    public string StallSize { get; set; } = "";

    [Required]
    [Display(Name = "Verification status")]
    public string PaymentStatus { get; set; } = "Unverified";

    [DataType(DataType.Date)]
    [Display(Name = "Date of occupancy")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "Include additional charge")]
    public bool IncludeAdditionalCharge { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    [Display(Name = "Additional charge")]
    public decimal AdditionalCharge { get; set; }

    public static ProfileInput FromRecord(ProfileRecord record) => new()
    {
        FullName = record.FullName,
        BusinessName = record.BusinessName,
        BusinessSection = record.BusinessSection,
        StallNumber = record.StallNumber,
        StallSize = record.StallSize,
        PaymentStatus = record.PaymentStatus,
        StartDate = DateTime.TryParse(record.StartDate, out DateTime date) ? date : null,
        IncludeAdditionalCharge = record.AdditionalCharge > 0,
        AdditionalCharge = record.AdditionalCharge
    };
}
