namespace BusinessPermitLicensingSystem.Web.Reports;

public static class RentalPaymentSelection
{
    public const string MaximumMessage = "You can print up to 3 billing reports at a time.";

    public static string? Validate(IReadOnlyList<string> ids, bool requireOne = false)
    {
        if (requireOne && ids.Count == 0) return "Select at least one stall owner.";
        if (ids.Any(x => string.IsNullOrWhiteSpace(x) || x.Length > 100)) return "A selected SIN is invalid.";
        if (ids.Count != ids.Distinct(StringComparer.OrdinalIgnoreCase).Count()) return "The same stall owner cannot be selected twice.";
        if (ids.Count > 3) return MaximumMessage;
        return null;
    }
}
