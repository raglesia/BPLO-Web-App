using System.Globalization;
using System.Text.Json;

namespace BusinessPermitLicensingSystem.Web.Vehicles;

public static class VehicleFeeDraft
{
    public static IReadOnlyList<(string Field, string Message)> ValidatePermitDetails(DraftData details)
    {
        var errors = new List<(string, string)>();
        foreach (var (field, value) in new[] { (nameof(DraftData.LineOfBusiness), details.LineOfBusiness),
                     (nameof(DraftData.Description), details.Description), (nameof(DraftData.Location), details.Location) })
            if ((value?.Trim().Length ?? 0) > 255) errors.Add((field, "Use at most 255 characters."));

        if (!int.TryParse(details.Employees, NumberStyles.None, CultureInfo.InvariantCulture, out int employees) || employees < 0)
            errors.Add((nameof(DraftData.Employees), "Enter a whole number of employees, 0 or greater."));
        if (!int.TryParse(details.Stickers, NumberStyles.None, CultureInfo.InvariantCulture, out int stickers) || stickers < 0)
            errors.Add((nameof(DraftData.Stickers), "Enter a whole number of stickers, 0 or greater."));
        if (!decimal.TryParse(details.Capital, NumberStyles.Number, AmountCulture, out decimal capital) ||
            capital < 0 || capital > MaximumAmount || decimal.Round(capital, 2) != capital)
            errors.Add((nameof(DraftData.Capital), "Enter non-negative capital with at most two decimal places."));

        CheckOption(nameof(DraftData.Organization), details.Organization,
            ["Individual", "Sole Proprietorship", "Partnership", "Corporation", "Cooperative", "Other"]);
        CheckOption(nameof(DraftData.Quarter), details.Quarter,
            ["1ST QUARTER", "2ND QUARTER", "3RD QUARTER", "4TH QUARTER"]);
        CheckOption(nameof(DraftData.SanitaryType), details.SanitaryType, ["OTHER", "FOOD", "NON-FOOD"]);
        CheckOption(nameof(DraftData.FireType), details.FireType, ["ESTAB", "OTHER"]);
        return errors;

        void CheckOption(string field, string? value, string[] options)
        {
            if (!string.IsNullOrEmpty(value) && !options.Contains(value, StringComparer.Ordinal))
                errors.Add((field, "Select a listed option."));
        }
    }

    public static DraftData NormalizePermitDetails(DraftData details)
    {
        if (ValidatePermitDetails(details).Count != 0)
            throw new ArgumentException("Permit details contain invalid values.");
        return new DraftData
        {
            LineOfBusiness = details.LineOfBusiness?.Trim() ?? "",
            Description = details.Description?.Trim() ?? "",
            Location = details.Location?.Trim() ?? "",
            Employees = int.Parse(details.Employees, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            Capital = decimal.Parse(details.Capital, NumberStyles.Number, AmountCulture).ToString("N2", AmountCulture),
            Stickers = int.Parse(details.Stickers, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            Organization = details.Organization ?? "",
            Quarter = details.Quarter ?? "",
            SanitaryType = details.SanitaryType ?? "",
            FireType = details.FireType ?? ""
        };
    }
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

    public static decimal OccupationalPermitAmount(string employees)
    {
        if (!int.TryParse(employees, NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count < 0)
            throw new ArgumentException("Enter a whole number of employees, 0 or greater.");
        return count * 300m;
    }

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
