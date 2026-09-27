using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Administration;

public sealed record AuditEntry(DateTime At, string Username, string Action, string Reference, string Details);
public sealed record AuditPage(IReadOnlyList<AuditEntry> Entries, int Total, int Page);

public sealed class AuditTrailService(IConfiguration configuration)
{
    public const int PageSize = 50;
    public async Task<AuditPage> ListAsync(string? category, string? search, int page, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        bool users = !string.Equals(category, "activity", StringComparison.OrdinalIgnoreCase);
        string term = (search ?? "").Trim();
        if (term.Length > 100) term = term[..100];
        string pattern = "%" + term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        const string from = "FROM AuditTrail a LEFT JOIN Users u ON u.Id=a.UserId";
        const string where = "WHERE ((@users=1 AND a.Action IN ('Login','Logout')) OR (@users=0 AND a.Action NOT IN ('Login','Logout'))) AND (@term='' OR a.Action LIKE @pattern OR a.SIN LIKE @pattern OR a.Details LIKE @pattern OR u.Username LIKE @pattern)";
        await using var count = new SqlCommand($"SELECT COUNT(*) {from} {where}", connection);
        AddParameters(count, users, term, pattern);
        int total = (int)(await count.ExecuteScalarAsync(token) ?? 0);
        page = Math.Clamp(page, 1, Math.Max(1, (total + PageSize - 1) / PageSize));
        await using var command = new SqlCommand($"""
            SELECT a.Timestamp, COALESCE(u.Username, CONCAT('User ', a.UserId)), a.Action,
                   COALESCE(a.SIN,''), COALESCE(a.Details,'')
            {from} {where}
            ORDER BY a.Timestamp DESC, a.Id DESC
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """, connection);
        AddParameters(command, users, term, pattern);
        command.Parameters.Add("@skip", SqlDbType.Int).Value = (page - 1) * PageSize;
        command.Parameters.Add("@take", SqlDbType.Int).Value = PageSize;
        var entries = new List<AuditEntry>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            entries.Add(new(reader.GetDateTime(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        return new(entries, total, page);
    }

    private static void AddParameters(SqlCommand command, bool users, string term, string pattern)
    {
        command.Parameters.Add("@users", SqlDbType.Bit).Value = users;
        command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = term;
        command.Parameters.Add("@pattern", SqlDbType.NVarChar, 512).Value = pattern;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken token)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Audit viewing requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        try { await connection.OpenAsync(token);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Audit viewing requires BPLS_Dev.");
            return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
