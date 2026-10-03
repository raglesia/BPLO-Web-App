namespace BusinessPermitLicensingSystem.Web.Billing;

public static class BillingRules
{
    public static IReadOnlyList<(int Year, int Month)> MissingPeriods(DateTime occupancy, DateTime asOf,
        IEnumerable<(int Year, int Month)> existing, bool isLegacyBaseline = false)
    {
        var known = existing.ToHashSet();
        var result = new List<(int, int)>();
        for (var month = new DateTime(occupancy.Year, occupancy.Month, 1).AddMonths(isLegacyBaseline ? 0 : 1);
             month <= new DateTime(asOf.Year, asOf.Month, 1); month = month.AddMonths(1))
            if (!known.Contains((month.Year, month.Month))) result.Add((month.Year, month.Month));
        return result;
    }

    public static decimal Penalty(decimal rental, int year, int month, DateTime asOf) =>
        asOf.Date > new DateTime(year, month, 20)
            ? decimal.Round(rental * 0.25m, 2, MidpointRounding.ToEven) : 0m;

    // BPLS_Dev profiles created before Phase 6 store base rent plus charge in MonthlyRental.
    public static decimal BaseFromCombinedProfile(decimal combinedRental, decimal additional)
    {
        if (combinedRental < 0 || additional < 0 || combinedRental < additional)
            throw new InvalidOperationException("Profile rental needs review before billing.");
        return combinedRental - additional;
    }

    public static decimal? BaseFromBill(decimal storedRental, decimal additional, string? basis) =>
        basis == "BaseOnly" || additional == 0 ? storedRental : null;

    public static decimal Total(decimal baseRent, decimal additional, decimal penalty) =>
        baseRent + additional + penalty;
}
