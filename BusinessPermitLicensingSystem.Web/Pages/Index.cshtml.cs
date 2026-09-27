using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Pages;

public sealed record CollectionMonth(DateTime Month, decimal Rent, decimal Vehicles);

public class IndexModel(IConfiguration configuration, ILogger<IndexModel> logger) : PageModel
{
    public int? ActiveProfiles { get; private set; }
    public int? ArchivedProfiles { get; private set; }
    public int? ActiveVehicles { get; private set; }
    public int? UnverifiedProfiles { get; private set; }
    public int? UnpaidPeriods { get; private set; }
    public int? ReviewNeededPeriods { get; private set; }
    public string? ReviewNeededSin { get; private set; }
    public IReadOnlyList<CollectionMonth> Collections { get; private set; } = [];
    public decimal? RentCollectedThisMonth { get; private set; }
    public decimal? VehicleCollectedThisMonth { get; private set; }

    public async Task OnGetAsync()
    {
        string? connectionString = configuration.GetConnectionString("BPLS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Dashboard counts unavailable: BPLS connection string is not configured.");
            return;
        }
        if (!string.Equals(new SqlConnectionStringBuilder(connectionString).InitialCatalog,
                "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("Dashboard counts require BPLS_Dev.");
            return;
        }

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(HttpContext.RequestAborted);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Dashboard counts require BPLS_Dev.");
            await using var command = new SqlCommand("""
                SELECT
                    (SELECT COUNT(*) FROM Profiling WHERE IsArchived = 0),
                    (SELECT COUNT(*) FROM Profiling WHERE IsArchived = 1),
                    (SELECT COUNT(*) FROM VehiclePermits WHERE IsArchived = 0),
                    (SELECT COUNT(*) FROM Profiling WHERE IsArchived = 0 AND PaymentStatus = 'Unverified'),
                    (SELECT COUNT(*) FROM MonthlyBilling WHERE PaymentStatus = 'Unpaid'),
                    (SELECT COUNT(*) FROM MonthlyBilling WHERE PaymentStatus = 'Unpaid' AND WebRentBasis IS NULL AND AdditionalCharge <> 0),
                    (SELECT TOP (1) SIN FROM MonthlyBilling WHERE PaymentStatus = 'Unpaid' AND WebRentBasis IS NULL AND AdditionalCharge <> 0 ORDER BY SIN)
                """, connection);
            await using var reader = await command.ExecuteReaderAsync(HttpContext.RequestAborted);
            if (await reader.ReadAsync(HttpContext.RequestAborted))
            {
                ActiveProfiles = reader.GetInt32(0);
                ArchivedProfiles = reader.GetInt32(1);
                ActiveVehicles = reader.GetInt32(2);
                UnverifiedProfiles = reader.GetInt32(3);
                UnpaidPeriods = reader.GetInt32(4);
                ReviewNeededPeriods = reader.GetInt32(5);
                ReviewNeededSin = reader.IsDBNull(6) ? null : reader.GetString(6);
            }
            await reader.CloseAsync();

            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var firstMonth = currentMonth.AddMonths(-5);
            var nextMonth = currentMonth.AddMonths(1);
            await using var collectionsCommand = new SqlCommand("""
                SELECT DATEFROMPARTS(YEAR(DatePaid), MONTH(DatePaid), 1) AS CollectionMonth,
                       SUM(CASE WHEN Kind = 'Rent' THEN AmountPaid ELSE 0 END) AS Rent,
                       SUM(CASE WHEN Kind = 'Vehicle' THEN AmountPaid ELSE 0 END) AS Vehicles
                FROM (
                    SELECT DatePaid, AmountPaid, 'Rent' AS Kind FROM PaymentHistory
                    WHERE DatePaid >= @first AND DatePaid < @next
                    UNION ALL
                    SELECT DatePaid, AmountPaid, 'Vehicle' AS Kind FROM VehiclePermitHistory
                    WHERE DatePaid >= @first AND DatePaid < @next
                ) AS RecordedPayments
                GROUP BY DATEFROMPARTS(YEAR(DatePaid), MONTH(DatePaid), 1)
                ORDER BY CollectionMonth
                """, connection);
            collectionsCommand.Parameters.Add("@first", System.Data.SqlDbType.DateTime2).Value = firstMonth;
            collectionsCommand.Parameters.Add("@next", System.Data.SqlDbType.DateTime2).Value = nextMonth;
            var amounts = new Dictionary<DateTime, (decimal Rent, decimal Vehicles)>();
            await using var collectionReader = await collectionsCommand.ExecuteReaderAsync(HttpContext.RequestAborted);
            while (await collectionReader.ReadAsync(HttpContext.RequestAborted))
                amounts[collectionReader.GetDateTime(0)] = (collectionReader.GetDecimal(1), collectionReader.GetDecimal(2));
            Collections = Enumerable.Range(0, 6).Select(i => {
                var month = firstMonth.AddMonths(i);
                var value = amounts.GetValueOrDefault(month);
                return new CollectionMonth(month, value.Rent, value.Vehicles);
            }).ToArray();
            RentCollectedThisMonth = Collections[^1].Rent;
            VehicleCollectedThisMonth = Collections[^1].Vehicles;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Dashboard counts could not be loaded.");
        }
    }
}
