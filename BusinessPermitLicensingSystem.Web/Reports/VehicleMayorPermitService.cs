using BusinessPermitLicensingSystem.Web.Data;
using Microsoft.Data.SqlClient;
using System.Data;
namespace BusinessPermitLicensingSystem.Web.Reports;

public sealed class VehicleMayorPermitService(IConfiguration configuration)
{
    public async Task<string?> GetNumberAsync(int paymentId, CancellationToken token)
    {
        await using var db = await DatabaseConnection.OpenAsync(configuration, token);
        await using var cmd = new SqlCommand("SELECT IssueYear,SequenceNumber FROM VehicleMayorPermits WHERE PaymentHistoryId=@id", db);
        cmd.Parameters.Add("@id", SqlDbType.Int).Value = paymentId;
        await using var reader = await cmd.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? $"{reader.GetInt32(0)}-{reader.GetInt32(1):D4}" : null;
    }

    public async Task<string> PrepareAsync(int paymentId, int userId, DateTime printedAt, CancellationToken token)
    {
        await using var db = await DatabaseConnection.OpenAsync(configuration, token);
        await using var transaction = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await using var command = new SqlCommand("""
            DECLARE @lock int;
            EXEC @lock=sys.sp_getapplock @Resource='BPLO.VehicleMayorPermit.Number',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
            IF @lock<0 THROW 51000,'Permit numbering is busy. Try again.',1;
            IF NOT EXISTS(SELECT 1 FROM VehiclePermitHistory h JOIN VehiclePermits v ON v.VIN=h.VIN
              JOIN VehiclePaymentAssessmentSnapshots s ON s.PaymentHistoryId=h.Id AND s.GrandTotal=h.AmountPaid
              WHERE h.Id=@id AND v.IsArchived=0 AND LEN(LTRIM(RTRIM(h.ORNumber)))>0)
              THROW 51000,'An active vehicle with a payment-time assessment snapshot is required.',1;
            IF NOT EXISTS(SELECT 1 FROM VehicleMayorPermits WHERE PaymentHistoryId=@id)
            BEGIN
              IF NOT EXISTS(SELECT 1 FROM VehicleMayorPermitCounters WHERE IssueYear=@year)
                INSERT VehicleMayorPermitCounters VALUES(@year,1);
              ELSE UPDATE VehicleMayorPermitCounters SET LastNumber=LastNumber+1 WHERE IssueYear=@year;
              INSERT VehicleMayorPermits(PaymentHistoryId,IssueYear,SequenceNumber,PreparedAt,PreparedBy)
                SELECT @id,@year,LastNumber,@date,@user FROM VehicleMayorPermitCounters WHERE IssueYear=@year;
            END;
            SELECT IssueYear,SequenceNumber FROM VehicleMayorPermits WHERE PaymentHistoryId=@id;
            """, db, transaction);
        command.Parameters.Add("@id", SqlDbType.Int).Value=paymentId;
        command.Parameters.Add("@year", SqlDbType.Int).Value=printedAt.Year;
        command.Parameters.Add("@date", SqlDbType.DateTime2).Value=printedAt;
        command.Parameters.Add("@user", SqlDbType.Int).Value=userId;
        string number;
        await using(var reader=await command.ExecuteReaderAsync(token))
        { await reader.ReadAsync(token); number=$"{reader.GetInt32(0)}-{reader.GetInt32(1):D4}"; }
        await transaction.CommitAsync(token);
        return number;
    }
}
