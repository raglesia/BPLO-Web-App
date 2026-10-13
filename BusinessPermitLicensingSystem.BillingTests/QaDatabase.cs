using System.Numerics;
using BusinessPermitLicensingSystem.Web.Data;
using Microsoft.Extensions.Configuration;

internal static class QaDatabase
{
    public static IConfiguration Configuration()
    {
        string name = Environment.GetEnvironmentVariable("BPLO_QA_DATABASE") ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^BPLS_Staging_RQA_[A-Za-z0-9_]+$"))
            throw new InvalidOperationException("Financial regression requires an explicitly named isolated BPLS_Staging_RQA_ database.");
        string server = Environment.GetEnvironmentVariable("BPLO_QA_SERVER") ?? @".\SQLEXPRESS";
        string identity = Environment.GetEnvironmentVariable("BPLO_QA_SERVER_IDENTITY") ?? @"John\SQLEXPRESS";
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["Database:Environment"] = "Staging", ["Database:ApprovedDatabase"] = name,
            ["Database:ApprovedServer"] = server, ["Database:ApprovedServerIdentity"] = identity,
            ["ConnectionStrings:BPLS"] = $"Server={server};Database={name};Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
        }).Build();
    }
    public static void RequireIsolated(IConfiguration config)
    {
        var approved = DatabaseConnection.Validate(config);
        if (config["Database:Environment"] != "Staging" ||
            !System.Text.RegularExpressions.Regex.IsMatch(approved.InitialCatalog, "^BPLS_Staging_RQA_[A-Za-z0-9_]+$"))
            throw new InvalidOperationException("Mutating release QA must use its isolated staging target.");
    }
    public static string OrRun() => new BigInteger(Guid.NewGuid().ToByteArray(), isUnsigned:true).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
