using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Pages;

public class DatabaseHealthModel(IConfiguration configuration, ILogger<DatabaseHealthModel> logger) : PageModel
{
    public bool IsConnected { get; private set; }

    public async Task OnGetAsync()
    {
        string? connectionString = configuration.GetConnectionString("BPLS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Database check skipped: BPLS connection string is not configured.");
            return;
        }
        if (!string.Equals(new SqlConnectionStringBuilder(connectionString).InitialCatalog,
                "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("Database check requires BPLS_Dev.");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(HttpContext.RequestAborted);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Database check requires BPLS_Dev.");
            await using var command = new SqlCommand("SELECT 1;", connection);
            IsConnected = Convert.ToInt32(await command.ExecuteScalarAsync(HttpContext.RequestAborted)) == 1;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database connectivity check failed.");
        }
    }
}
