using BusinessPermitLicensingSystem.Web.Data;
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
        try
        {
            await using var connection = await DatabaseConnection.OpenAsync(configuration, HttpContext.RequestAborted);
            await using var command = new SqlCommand("SELECT 1;", connection);
            IsConnected = Convert.ToInt32(await command.ExecuteScalarAsync(HttpContext.RequestAborted)) == 1;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database connectivity check failed.");
        }
    }
}
