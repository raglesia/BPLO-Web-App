namespace BusinessPermitLicensingSystem.Web.Transfer;

public sealed record ImportSummary(int Total, int Imported, int Skipped, int Duplicates,
    int Invalid, int Failed, IReadOnlyList<ImportIssue> Issues, bool MoreIssues)
{
    public static ImportSummary From(ImportResult result) => new(result.Total, result.Imported,
        result.Skipped, result.Duplicates, result.Invalid, result.Failed,
        result.Issues.Take(20).ToArray(), result.Issues.Count > 20);
}
