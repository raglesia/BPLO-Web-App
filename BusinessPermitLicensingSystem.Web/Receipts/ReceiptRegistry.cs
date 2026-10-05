using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Receipts;

/// <summary>One OR namespace, reserved in the caller's payment transaction.</summary>
public static class ReceiptRegistry
{
    public const string DuplicateMessage = "OR number already exists. This OR Number has already been recorded in the system.";

    // Match dbo.NormalizeOR used by migration and ledger triggers. Keep case and internal characters.
    // The registry's CI_AS collation makes uniqueness case-insensitive and accent-sensitive.
    public static string NormalizeOR(string? value) => (value ?? "").Trim(' ', '\t', '\n', '\r', '\v', '\f');

    public static async Task ReserveAsync(SqlConnection connection, SqlTransaction transaction,
        string orNumber, string paymentType, int userId, DateTime paidAt, CancellationToken token)
    {
        await using var command = new SqlCommand("""
            INSERT INTO dbo.ReceiptRegistry(ORNumber,PaymentType,RecordedByUserId,RecordedAt)
            VALUES(dbo.NormalizeOR(@or),@type,@user,@date)
            """, connection, transaction);
        command.Parameters.Add("@or", SqlDbType.NVarChar, 100).Value = NormalizeOR(orNumber);
        command.Parameters.Add("@type", SqlDbType.VarChar, 20).Value = paymentType;
        command.Parameters.Add("@user", SqlDbType.Int).Value = userId;
        command.Parameters.Add("@date", SqlDbType.DateTime).Value = paidAt;
        if (await command.ExecuteNonQueryAsync(token) != 1)
            throw new InvalidOperationException("Receipt reservation failed.");
    }
}
