using System.Globalization;
using System.Text.Json;

namespace BusinessPermitLicensingSystem.Web.Vehicles;

public static class VehicleFeeDraft
{
    public static readonly string[] FeeNames =
    [
        "Mayor's Permit Fee", "Sanitary Fee", "Garbage Fee", "Market Clearance",
        "Occupational Permit", "Sticker Fee", "Tobacco License Fee", "Liquor License Fee",
        "Fire Inspection Fee", "Certification Fee", "Plate Fee", "Desktop Fee",
        "Videoke Fee", "Weights / Measures", "Storage Fee",
        "Other Fee 1", "Other Fee 2", "Other Fee 3", "Other Fee 4"
    ];
    private static readonly CultureInfo AmountCulture = CultureInfo.GetCultureInfo("en-PH");
    public const decimal MaximumAmount = 9999999999999999.99m;

    public static decimal Total(IReadOnlyList<decimal> amounts)
    {
        if (amounts.Count != FeeNames.Length) throw new ArgumentException("Every fee amount is required.");
        decimal total = 0;
        foreach (decimal amount in amounts)
        {
            if (amount < 0 || amount > MaximumAmount || decimal.Round(amount, 2) != amount)
                throw new ArgumentException("Fees must be non-negative amounts with at most two decimals.");
            total = checked(total + amount);
        }
        if (total > MaximumAmount) throw new ArgumentException("Fee total is too large.");
        return total;
    }

    public static DraftSnapshot Read(string json, decimal storedTotal)
    {
        var data = JsonSerializer.Deserialize<DraftData>(json)
            ?? throw new InvalidOperationException("Fee draft is invalid.");
        if (data.Amounts is null || data.OtherDescriptions is null || data.OtherDescriptions.Length != 4 ||
            data.Amounts.Count != FeeNames.Length ||
            data.Amounts.Keys.Except(FeeNames, StringComparer.Ordinal).Any())
            throw new InvalidOperationException("Fee draft has unknown or missing fee items.");
        var amounts = new List<decimal>();
        foreach (string name in FeeNames)
        {
            if (!data.Amounts.TryGetValue(name, out string? raw) ||
                !decimal.TryParse(raw, NumberStyles.Number, AmountCulture, out decimal amount))
                throw new InvalidOperationException("Fee draft contains an invalid amount.");
            amounts.Add(amount);
        }
        if (Total(amounts) != storedTotal)
            throw new InvalidOperationException("Fee draft total does not match its fee items.");
        return new DraftSnapshot(data, amounts, storedTotal);
    }

    public static (string Json, decimal Total) Create(IReadOnlyList<decimal> amounts,
        IReadOnlyList<string> otherDescriptions, DraftData? previous = null)
    {
        decimal total = Total(amounts);
        if (otherDescriptions.Count != 4 || otherDescriptions.Any(x => x is not null && x.Length > 255))
            throw new ArgumentException("Four other-fee descriptions, up to 255 characters each, are required.");
        var data = previous ?? new DraftData();
        data.Amounts = FeeNames.Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => amounts[x.index].ToString("N2", AmountCulture));
        data.OtherDescriptions = otherDescriptions.Select(x => (x ?? "").Trim()).ToArray();
        return (JsonSerializer.Serialize(data), total);
    }
}

public sealed record DraftSnapshot(DraftData Data, IReadOnlyList<decimal> Amounts, decimal GrandTotal);

public sealed class DraftData
{
    public Dictionary<string, string> Amounts { get; set; } = new();
    public string[] OtherDescriptions { get; set; } = ["", "", "", ""];
    public string LineOfBusiness { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public string Employees { get; set; } = "0";
    public string Capital { get; set; } = "0.00";
    public string Stickers { get; set; } = "0";
    public string Organization { get; set; } = "";
    public string Quarter { get; set; } = "";
    public string SanitaryType { get; set; } = "";
    public string FireType { get; set; } = "";
}
