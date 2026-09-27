using System.ComponentModel.DataAnnotations;

namespace BusinessPermitLicensingSystem.Web.Pages.UserAccounts;

public sealed class AccountInput
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(255)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(255)]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Position is required.")]
    [StringLength(255)]
    public string Position { get; set; } = "";
}
