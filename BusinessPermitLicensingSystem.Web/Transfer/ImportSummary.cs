namespace BusinessPermitLicensingSystem.Web.Transfer;

public sealed record ImportSummary(int Total, int Imported, int Skipped, int Duplicates,
    int Invalid, int Failed, IReadOnlyList<ImportIssue> Issues, bool MoreIssues)
{
    public static string CreateIssueDownload(IReadOnlyList<ImportIssue> issues)
    {
        using var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
        {
            csv.WriteField("Row");
            csv.WriteField("Result");
            csv.WriteField("Reason");
            csv.NextRecord();
            foreach (var issue in issues)
            {
                csv.WriteField(issue.Row);
                csv.WriteField(issue.Kind);
                csv.WriteField(issue.Reason);
                csv.NextRecord();
            }
        }
        return "data:text/csv;charset=utf-8;base64," +
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(writer.ToString()));
    }

    public static ImportSummary From(ImportResult result) => new(result.Total, result.Imported,
        result.Skipped, result.Duplicates, result.Invalid, result.Failed,
        result.Issues.Take(20).ToArray(), result.Issues.Count > 20);
}
