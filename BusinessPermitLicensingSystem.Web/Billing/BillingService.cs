using System.Data;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Billing;

public sealed record BillingRow(int Year, int Month, decimal Rental, decimal Additional,
    decimal StoredPenalty, string Status, string? OrNumber, DateTime? DatePaid, string? RentBasis)
{
    public decimal? BaseRent => BillingRules.BaseFromBill(Rental, Additional, RentBasis);
    public decimal? CurrentPenalty(DateTime asOf) => Status == "Unpaid"
        ? BaseRent is decimal value ? BillingRules.Penalty(value, Year, Month, asOf) : null
        : StoredPenalty;
}

public sealed record BillingView(string Name, string Status, bool Archived, string StartDate,
    IReadOnlyList<BillingRow> Rows, decimal? RentDue, decimal? AdditionalDue, decimal? PenaltyDue,
    IReadOnlyList<PaymentRecord> Payments)
{
    public bool IsLegacyBaseline { get; init; }
    public IReadOnlyList<ArrearsRow> Arrears { get; init; } = [];
    public decimal? TotalDue => RentDue is decimal rent && AdditionalDue is decimal additional &&
        PenaltyDue is decimal penalty ? BillingRules.Total(rent, additional, penalty) : null;
}

public sealed record PaymentRecord(string OrNumber, DateTime DatePaid, decimal Amount,
    decimal Penalty, string RecordedBy, string Periods);

public sealed record ArrearsRow(int Id, int Year, int Month, decimal BaseRent,
    decimal Additional, decimal StoredPenalty, bool IsPaid, DateTime? PaidAt,
    string TreasuryReference)
{
    public decimal CurrentPenalty(DateTime asOf) => IsPaid ? StoredPenalty :
        BillingRules.Penalty(BaseRent, Year, Month, asOf);
    public decimal Total(DateTime asOf) => BaseRent + Additional + CurrentPenalty(asOf);
}

public sealed record ArrearsResult(bool Success, string? Error);

public sealed record PaymentResult(bool Success, string? Error, string? OrNumber = null,
    decimal Amount = 0, int Bills = 0);

public sealed class BillingService(IConfiguration configuration)
{
    public async Task<BillingView?> GetAsync(string sin, DateTime asOf, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var profile = new SqlCommand("SELECT FullName, PaymentStatus, IsArchived, StartDate, IsLegacyBaseline FROM Profiling WHERE SIN=@sin", connection);
        profile.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        string name, status, start;
        bool archived, legacy;
        await using (var reader = await profile.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;
            name = reader.GetString(0); status = reader.GetString(1); archived = !reader.IsDBNull(2) && reader.GetInt32(2) != 0;
            start = reader.IsDBNull(3) ? "" : reader.GetString(3);
            legacy = reader.GetBoolean(4);
        }
        await using var command = new SqlCommand("""
            SELECT BillingYear, BillingMonth, MonthlyRental, AdditionalCharge, Penalty,
                   PaymentStatus, ORNumber, DatePaid, WebRentBasis FROM MonthlyBilling
            WHERE SIN=@sin ORDER BY BillingYear DESC, BillingMonth DESC
            """, connection);
        command.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        var rows = new List<BillingRow>();
        decimal rent = 0, additional = 0, penalty = 0;
        DateTime dueThrough = legacy && DateTime.TryParse(start, out var baseline) && baseline > asOf ? baseline : asOf;
        bool unresolved = false;
        await using var bills = await command.ExecuteReaderAsync(cancellationToken);
        while (await bills.ReadAsync(cancellationToken))
        {
            var row = new BillingRow(bills.GetInt32(0), bills.GetInt32(1), bills.GetDecimal(2), bills.GetDecimal(3),
                bills.GetDecimal(4), bills.GetString(5), bills.IsDBNull(6) ? null : bills.GetString(6),
                bills.IsDBNull(7) ? null : bills.GetDateTime(7),
                bills.IsDBNull(8) ? null : bills.GetString(8));
            rows.Add(row);
            if (status != "Unverified" && row.Status == "Unpaid" &&
                (row.Year < dueThrough.Year || row.Year == dueThrough.Year && row.Month <= dueThrough.Month))
            {
                if (row.BaseRent is decimal baseRent && row.CurrentPenalty(asOf) is decimal rowPenalty)
                { rent += baseRent; additional += row.Additional; penalty += rowPenalty; }
                else unresolved = true;
            }
        }
        await bills.CloseAsync();
        var arrears = await ReadArrearsAsync(connection, null, sin, cancellationToken);
        if (status != "Unverified")
            foreach (var item in arrears.Where(x => !x.IsPaid))
            { rent += item.BaseRent; additional += item.Additional; penalty += item.CurrentPenalty(asOf); }
        await using var history = new SqlCommand("""
            SELECT ph.ORNumber, ph.DatePaid, ph.AmountPaid, ph.Penalty,
                   COALESCE(u.FullName, CONCAT('User ', ph.RecordedBy)),
                   COALESCE(periods.Covered, '')
            FROM PaymentHistory ph
            LEFT JOIN Users u ON u.Id = ph.RecordedBy
            OUTER APPLY (
                SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), x.Period), ', ')
                       WITHIN GROUP (ORDER BY x.SortYear, x.SortMonth) AS Covered
                FROM (
                    SELECT mb.BillingYear SortYear, mb.BillingMonth SortMonth,
                           CONCAT(mb.BillingYear, '-', RIGHT(CONCAT('0', mb.BillingMonth), 2)) Period
                    FROM PaymentHistoryBilling link JOIN MonthlyBilling mb ON mb.Id=link.MonthlyBillingId
                    WHERE link.PaymentHistoryId=ph.Id
                    UNION ALL
                    SELECT a.BillingYear, a.BillingMonth,
                           CONCAT(a.BillingYear, '-', RIGHT(CONCAT('0', a.BillingMonth), 2), ' (arrears)')
                    FROM PaymentHistoryArrears link JOIN StallOwnerArrears a ON a.Id=link.StallOwnerArrearsId
                    WHERE link.PaymentHistoryId=ph.Id
                ) x
            ) periods
            WHERE ph.SIN=@sin
            ORDER BY ph.DatePaid DESC, ph.Id DESC
            """, connection);
        history.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        var payments = new List<PaymentRecord>();
        await using var paid = await history.ExecuteReaderAsync(cancellationToken);
        while (await paid.ReadAsync(cancellationToken))
            payments.Add(new PaymentRecord(paid.GetString(0), paid.GetDateTime(1), paid.GetDecimal(2),
                paid.GetDecimal(3), paid.GetString(4), paid.GetString(5)));
        return new BillingView(name, status, archived, start, rows,
            unresolved ? null : rent, unresolved ? null : additional, unresolved ? null : penalty, payments)
        { Arrears = arrears, IsLegacyBaseline = legacy };
    }

    public async Task<int> GenerateAsync(string sin, DateTime asOf, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            int added = await GenerateLockedAsync(connection, transaction, sin, asOf, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return added;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    private static async Task<int> GenerateLockedAsync(SqlConnection connection, SqlTransaction transaction,
        string sin, DateTime asOf, CancellationToken cancellationToken)
    {
            await using var profile = new SqlCommand("""
                SELECT StartDate, MonthlyRental, AdditionalCharge, IsLegacyBaseline FROM Profiling WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin AND IsArchived=0 AND PaymentStatus <> 'Unverified'
                """, connection, transaction);
            profile.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            string start;
            decimal rent, additional;
            bool legacy;
            await using (var reader = await profile.ExecuteReaderAsync(cancellationToken))
            {
                if (!await reader.ReadAsync(cancellationToken)) return 0;
                start = reader.IsDBNull(0) ? "" : reader.GetString(0);
                rent = reader.GetDecimal(1); additional = reader.GetDecimal(2);
                legacy = reader.GetBoolean(3);
            }
            if (!DateTime.TryParse(start, out var occupancy)) return 0;
            await using var existingCommand = new SqlCommand("""
                SELECT BillingYear, BillingMonth FROM MonthlyBilling WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin
                """, connection, transaction);
            existingCommand.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            var existing = new List<(int, int)>();
            await using (var reader = await existingCommand.ExecuteReaderAsync(cancellationToken))
                while (await reader.ReadAsync(cancellationToken)) existing.Add((reader.GetInt32(0), reader.GetInt32(1)));
            DateTime billThrough = legacy && occupancy > asOf ? occupancy : asOf;
            var missing = BillingRules.MissingPeriods(occupancy, billThrough, existing, legacy);
            decimal baseRent = BillingRules.BaseFromCombinedProfile(rent, additional);
            foreach (var (year, month) in missing)
            {
                await using var insert = new SqlCommand("""
                    INSERT INTO MonthlyBilling
                    (SIN, BillingYear, BillingMonth, MonthlyRental, AdditionalCharge, Penalty, PaymentStatus, WebRentBasis)
                    VALUES (@sin, @year, @month, @rent, @additional, 0, 'Unpaid', 'BaseOnly')
                    """, connection, transaction);
                insert.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
                insert.Parameters.Add("@year", SqlDbType.Int).Value = year;
                insert.Parameters.Add("@month", SqlDbType.Int).Value = month;
                Money(insert, "@rent", baseRent); Money(insert, "@additional", additional);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
            return missing.Count;
    }

    public async Task<decimal> UpdatePenaltyAsync(string sin, DateTime asOf, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await using var profile = new SqlCommand("""
                SELECT PaymentStatus FROM Profiling WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin AND IsArchived=0
                """, connection, transaction);
            profile.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            var status = (string?)await profile.ExecuteScalarAsync(cancellationToken);
            if (status is null or "Unverified") return 0;
            await using var bills = new SqlCommand("""
                SELECT BillingYear, BillingMonth, MonthlyRental, AdditionalCharge, WebRentBasis FROM MonthlyBilling
                WHERE SIN=@sin AND PaymentStatus='Unpaid'
                """, connection, transaction);
            bills.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            decimal penalty = 0;
            await using (var reader = await bills.ExecuteReaderAsync(cancellationToken))
                while (await reader.ReadAsync(cancellationToken))
                {
                    decimal rental = reader.GetDecimal(2), additional = reader.GetDecimal(3);
                    string? basis = reader.IsDBNull(4) ? null : reader.GetString(4);
                    if (BillingRules.BaseFromBill(rental, additional, basis) is not decimal baseRent)
                        throw new InvalidOperationException("Legacy billing row with additional charge needs review.");
                    penalty += BillingRules.Penalty(baseRent, reader.GetInt32(0), reader.GetInt32(1), asOf);
                }
            await using var update = new SqlCommand("UPDATE Profiling SET Penalty=@penalty WHERE SIN=@sin", connection, transaction);
            Money(update, "@penalty", penalty);
            update.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            await update.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return penalty;
        }
        catch { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    public async Task<ArrearsResult> SaveVerifiedArrearsAsync(string sin, int? arrearsId,
        int year, int month, decimal baseRent, decimal additional, string? reference,
        int userId, DateTime verifiedAt, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(sin) || sin.Length > 100)
            return new(false, "Choose a valid stall owner profile.");
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            return new(false, "Choose a valid arrears period.");
        if (baseRent <= 0 || additional < 0 || baseRent > 9999999999999999.99m - additional)
            return new(false, "Enter a positive base rent and a non-negative additional charge.");
        reference = (reference ?? "").Trim();
        if (reference.Length > 500) return new(false, "Treasury reference must be at most 500 characters.");
        if (userId <= 0) return new(false, "Sign in again before verifying arrears.");
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, token);
        try
        {
            await using var profile = new SqlCommand("""
                SELECT StartDate, IsLegacyBaseline FROM Profiling WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin AND IsArchived=0 AND PaymentStatus <> 'Unverified'
                """, connection, transaction);
            profile.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            DateTime baseline;
            await using (var reader = await profile.ExecuteReaderAsync(token))
            {
                if (!await reader.ReadAsync(token)) return new(false, "Verify an active profile before adding arrears.");
                if (!reader.GetBoolean(1) || !DateTime.TryParse(reader.GetString(0), out baseline))
                    return new(false, "Verified prior rent is available for legacy baseline profiles only.");
            }
            if (year * 12 + month >= baseline.Year * 12 + baseline.Month)
                return new(false, "Choose a period before the computerized tracking baseline.");
            if (arrearsId is int correctingId)
            {
                await using var original = new SqlCommand("""
                    SELECT IsPaid FROM StallOwnerArrears WITH (UPDLOCK, HOLDLOCK)
                    WHERE Id=@id AND SIN=@sin
                    """, connection, transaction);
                original.Parameters.Add("@id", SqlDbType.Int).Value = correctingId;
                original.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
                object? originalPaid = await original.ExecuteScalarAsync(token);
                if (originalPaid is null) return new(false, "Arrears entry changed. Refresh before correcting it.");
                if ((bool)originalPaid) return new(false, "Paid arrears cannot be changed.");
            }
            await using var regular = new SqlCommand("""
                SELECT 1 FROM MonthlyBilling WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin AND BillingYear=@year AND BillingMonth=@month
                """, connection, transaction);
            regular.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            regular.Parameters.Add("@year", SqlDbType.Int).Value = year;
            regular.Parameters.Add("@month", SqlDbType.Int).Value = month;
            if (await regular.ExecuteScalarAsync(token) is not null)
                return new(false, "This rental period already exists in the regular billing records.");
            await using var existing = new SqlCommand("""
                SELECT Id, IsPaid FROM StallOwnerArrears WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin AND BillingYear=@year AND BillingMonth=@month
                """, connection, transaction);
            existing.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            existing.Parameters.Add("@year", SqlDbType.Int).Value = year;
            existing.Parameters.Add("@month", SqlDbType.Int).Value = month;
            int? foundId = null;
            bool paid = false;
            await using (var reader = await existing.ExecuteReaderAsync(token))
                if (await reader.ReadAsync(token)) { foundId = reader.GetInt32(0); paid = reader.GetBoolean(1); }
            if (paid) return new(false, "Paid arrears cannot be changed.");
            if (arrearsId is null && foundId is not null)
                return new(false, "This arrears period already exists. Open it to correct the entry.");
            if (arrearsId is not null && foundId is not null && foundId != arrearsId)
                return new(false, "This arrears period already exists.");
            decimal penalty = BillingRules.Penalty(baseRent, year, month, verifiedAt);
            await using var save = new SqlCommand(arrearsId is null ? """
                INSERT INTO StallOwnerArrears
                  (SIN, BillingYear, BillingMonth, BaseRent, AdditionalCharge, PenaltyAmount,
                   VerifiedByUserId, VerifiedAt, TreasuryReference)
                VALUES (@sin, @year, @month, @base, @additional, @penalty, @user, @at, @reference)
                """ : """
                UPDATE StallOwnerArrears SET BillingYear=@year, BillingMonth=@month,
                    BaseRent=@base, AdditionalCharge=@additional,
                    PenaltyAmount=@penalty, VerifiedByUserId=@user, VerifiedAt=@at,
                    TreasuryReference=@reference, UpdatedAt=SYSUTCDATETIME()
                WHERE Id=@id AND SIN=@sin AND IsPaid=0
                """, connection, transaction);
            save.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            save.Parameters.Add("@year", SqlDbType.Int).Value = year;
            save.Parameters.Add("@month", SqlDbType.Int).Value = month;
            Money(save, "@base", baseRent); Money(save, "@additional", additional); Money(save, "@penalty", penalty);
            save.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            save.Parameters.Add("@at", SqlDbType.DateTime2).Value = verifiedAt;
            save.Parameters.Add("@reference", SqlDbType.NVarChar, 500).Value = reference;
            if (arrearsId is int id) save.Parameters.Add("@id", SqlDbType.Int).Value = id;
            if (await save.ExecuteNonQueryAsync(token) != 1)
                return new(false, "Arrears entry changed. Refresh before correcting it.");
            await using var audit = new SqlCommand("""
                INSERT INTO AuditTrail (Action, SIN, UserId, Timestamp, Details)
                VALUES (@action, @sin, @user, @at, @details)
                """, connection, transaction);
            audit.Parameters.Add("@action", SqlDbType.NVarChar, 255).Value = arrearsId is null ? "Add Verified Arrears" : "Correct Verified Arrears";
            audit.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            audit.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            audit.Parameters.Add("@at", SqlDbType.DateTime).Value = verifiedAt;
            audit.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value =
                $"Treasury-verified {year}-{month:D2}; base {baseRent:N2}; additional {additional:N2}; reference {reference}";
            await audit.ExecuteNonQueryAsync(token);
            await transaction.CommitAsync(token);
            return new(true, null);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { await transaction.RollbackAsync(token); return new(false, "This arrears period already exists."); }
        catch { await transaction.RollbackAsync(token); throw; }
    }

    public async Task<PaymentResult> PayAsync(string sin, string orNumber, int userId,
        DateTime paidAt, CancellationToken cancellationToken)
    {
        orNumber = Receipts.ReceiptRegistry.NormalizeOR(orNumber);
        if (string.IsNullOrWhiteSpace(sin)) return new(false, "Profile is required.");
        if (orNumber.Length is 0 or > 100) return new(false, "Enter an OR number up to 100 characters.");
        if (userId <= 0) return new(false, "Sign in again before recording payment.");

        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            // Same profile lock as explicit generation; missing periods join this payment transaction.
            await GenerateLockedAsync(connection, transaction, sin, paidAt, cancellationToken);
            await using var profile = new SqlCommand("""
                SELECT FullName, PaymentStatus, IsLegacyBaseline, StartDate FROM Profiling
                WHERE SIN=@sin AND IsArchived=0
                """, connection, transaction);
            profile.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            string name = "", status = "";
            bool legacy = false;
            string start = "";
            bool found;
            await using (var reader = await profile.ExecuteReaderAsync(cancellationToken))
            {
                found = await reader.ReadAsync(cancellationToken);
                if (found) { name = reader.GetString(0); status = reader.GetString(1);
                    legacy = reader.GetBoolean(2); start = reader.IsDBNull(3) ? "" : reader.GetString(3); }
            }
            if (!found)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Active profile not found."); }
            if (status == "Unverified")
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Verify occupancy before payment."); }

            DateTime dueThrough = legacy && DateTime.TryParse(start, out var baseline) && baseline > paidAt ? baseline : paidAt;

            await using var dueCommand = new SqlCommand("""
                SELECT Id, BillingYear, BillingMonth, MonthlyRental, AdditionalCharge, WebRentBasis
                FROM MonthlyBilling WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin AND PaymentStatus='Unpaid'
                  AND (BillingYear < @year OR (BillingYear=@year AND BillingMonth<=@month))
                ORDER BY BillingYear, BillingMonth
                """, connection, transaction);
            dueCommand.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            dueCommand.Parameters.Add("@year", SqlDbType.Int).Value = dueThrough.Year;
            dueCommand.Parameters.Add("@month", SqlDbType.Int).Value = dueThrough.Month;
            var due = new List<(int Id, decimal Penalty)>();
            decimal rent = 0, additional = 0, penalty = 0;
            bool unresolved = false;
            await using (var reader = await dueCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    decimal storedRent = reader.GetDecimal(3), charge = reader.GetDecimal(4);
                    string? basis = reader.IsDBNull(5) ? null : reader.GetString(5);
                    if (BillingRules.BaseFromBill(storedRent, charge, basis) is not decimal baseRent)
                    { unresolved = true; continue; }
                    decimal billPenalty = BillingRules.Penalty(baseRent, reader.GetInt32(1), reader.GetInt32(2), paidAt);
                    due.Add((reader.GetInt32(0), billPenalty));
                    rent += baseRent; additional += charge; penalty += billPenalty;
                }
            }
            if (unresolved)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "Legacy billing rows with additional charges need review before payment."); }
            await using var arrearsCommand = new SqlCommand("""
                SELECT Id, BillingYear, BillingMonth, BaseRent, AdditionalCharge
                FROM StallOwnerArrears WITH (UPDLOCK, HOLDLOCK)
                WHERE SIN=@sin AND IsPaid=0 ORDER BY BillingYear, BillingMonth
                """, connection, transaction);
            arrearsCommand.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            var dueArrears = new List<(int Id, decimal Penalty)>();
            await using (var reader = await arrearsCommand.ExecuteReaderAsync(cancellationToken))
                while (await reader.ReadAsync(cancellationToken))
                {
                    decimal baseRent = reader.GetDecimal(3);
                    decimal arrearsPenalty = BillingRules.Penalty(baseRent, reader.GetInt32(1), reader.GetInt32(2), paidAt);
                    dueArrears.Add((reader.GetInt32(0), arrearsPenalty));
                    rent += baseRent; additional += reader.GetDecimal(4); penalty += arrearsPenalty;
                }
            if (due.Count + dueArrears.Count == 0)
            { await transaction.RollbackAsync(cancellationToken); return new(false, "There are no outstanding rental periods to pay."); }

            await Receipts.ReceiptRegistry.ReserveAsync(connection, transaction, orNumber,
                "StallRental", userId, paidAt, cancellationToken);

            decimal amount = BillingRules.Total(rent, additional, penalty);
            await using var insert = new SqlCommand("""
                DECLARE @created TABLE(Id int);
                INSERT INTO PaymentHistory (SIN, ORNumber, AmountPaid, Penalty, DatePaid, RecordedBy)
                OUTPUT INSERTED.Id INTO @created VALUES (@sin, @or, @amount, @penalty, @date, @user);
                SELECT Id FROM @created;
                """, connection, transaction);
            insert.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            insert.Parameters.Add("@or", SqlDbType.NVarChar, 100).Value = orNumber;
            Money(insert, "@amount", amount); Money(insert, "@penalty", penalty);
            insert.Parameters.Add("@date", SqlDbType.DateTime).Value = paidAt;
            insert.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            int paymentId = (int)(await insert.ExecuteScalarAsync(cancellationToken) ??
                throw new InvalidOperationException("Payment history insert failed."));

            foreach (var bill in due)
            {
                await using var link = new SqlCommand("""
                    INSERT INTO PaymentHistoryBilling (PaymentHistoryId, MonthlyBillingId)
                    VALUES (@payment, @bill)
                    """, connection, transaction);
                link.Parameters.Add("@payment", SqlDbType.Int).Value = paymentId;
                link.Parameters.Add("@bill", SqlDbType.Int).Value = bill.Id;
                if (await link.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException("Payment billing link insert failed.");

                await using var update = new SqlCommand("""
                    UPDATE MonthlyBilling SET PaymentStatus='Paid', ORNumber=@or, DatePaid=@date,
                        RecordedBy=@user, Penalty=@penalty
                    WHERE Id=@bill AND SIN=@sin AND PaymentStatus='Unpaid'
                    """, connection, transaction);
                update.Parameters.Add("@or", SqlDbType.NVarChar, 100).Value = orNumber;
                update.Parameters.Add("@date", SqlDbType.DateTime).Value = paidAt;
                update.Parameters.Add("@user", SqlDbType.Int).Value = userId;
                Money(update, "@penalty", bill.Penalty);
                update.Parameters.Add("@bill", SqlDbType.Int).Value = bill.Id;
                update.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
                if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException("Billing row changed during payment.");
            }

            foreach (var arrear in dueArrears)
            {
                await using var link = new SqlCommand("""
                    INSERT INTO PaymentHistoryArrears (PaymentHistoryId, StallOwnerArrearsId)
                    VALUES (@payment, @arrears)
                    """, connection, transaction);
                link.Parameters.Add("@payment", SqlDbType.Int).Value = paymentId;
                link.Parameters.Add("@arrears", SqlDbType.Int).Value = arrear.Id;
                if (await link.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException("Payment arrears link insert failed.");
                await using var update = new SqlCommand("""
                    UPDATE StallOwnerArrears SET IsPaid=1, PaidAt=@date,
                        PenaltyAmount=@penalty, UpdatedAt=SYSUTCDATETIME()
                    WHERE Id=@arrears AND SIN=@sin AND IsPaid=0
                    """, connection, transaction);
                update.Parameters.Add("@date", SqlDbType.DateTime).Value = paidAt;
                Money(update, "@penalty", arrear.Penalty);
                update.Parameters.Add("@arrears", SqlDbType.Int).Value = arrear.Id;
                update.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
                if (await update.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new InvalidOperationException("Arrears row changed during payment.");
            }

            await using var count = new SqlCommand(
                "SELECT COUNT(*) FROM PaymentHistoryBilling WHERE PaymentHistoryId=@payment", connection, transaction);
            count.Parameters.Add("@payment", SqlDbType.Int).Value = paymentId;
            if ((int)(await count.ExecuteScalarAsync(cancellationToken) ?? 0) != due.Count)
                throw new InvalidOperationException("Payment billing links are incomplete.");
            await using var arrearsCount = new SqlCommand(
                "SELECT COUNT(*) FROM PaymentHistoryArrears WHERE PaymentHistoryId=@payment", connection, transaction);
            arrearsCount.Parameters.Add("@payment", SqlDbType.Int).Value = paymentId;
            if ((int)(await arrearsCount.ExecuteScalarAsync(cancellationToken) ?? 0) != dueArrears.Count)
                throw new InvalidOperationException("Payment arrears links are incomplete.");

            await using var updateProfile = new SqlCommand("""
                UPDATE Profiling SET PaymentStatus='Paid', Penalty=0
                WHERE SIN=@sin AND IsArchived=0 AND PaymentStatus <> 'Unverified'
                """, connection, transaction);
            updateProfile.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            if (await updateProfile.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("Profile status update failed.");

            await using var audit = new SqlCommand("""
                INSERT INTO AuditTrail (Action, SIN, UserId, Timestamp, Details)
                VALUES ('Update', @sin, @user, @date, @details)
                """, connection, transaction);
            audit.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
            audit.Parameters.Add("@user", SqlDbType.Int).Value = userId;
            audit.Parameters.Add("@date", SqlDbType.DateTime).Value = paidAt;
            audit.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value =
                $"Updated profile for {name}, Status: Paid, OR#: {orNumber}";
            if (await audit.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException("Payment audit insert failed.");

            await transaction.CommitAsync(cancellationToken);
            return new(true, null, orNumber, amount, due.Count + dueArrears.Count);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { await transaction.RollbackAsync(cancellationToken); return new(false, Receipts.ReceiptRegistry.DuplicateMessage); }
        catch
        { await transaction.RollbackAsync(cancellationToken); throw; }
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Billing requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
        { await connection.DisposeAsync(); throw new InvalidOperationException("Billing requires BPLS_Dev."); }
        return connection;
    }

    private static async Task<IReadOnlyList<ArrearsRow>> ReadArrearsAsync(SqlConnection connection,
        SqlTransaction? transaction, string sin, CancellationToken token)
    {
        await using var command = new SqlCommand("""
            SELECT Id, BillingYear, BillingMonth, BaseRent, AdditionalCharge, PenaltyAmount,
                   IsPaid, PaidAt, TreasuryReference
            FROM StallOwnerArrears WHERE SIN=@sin ORDER BY BillingYear DESC, BillingMonth DESC
            """, connection, transaction);
        command.Parameters.Add("@sin", SqlDbType.NVarChar, 100).Value = sin;
        var rows = new List<ArrearsRow>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
            rows.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                reader.GetDecimal(3), reader.GetDecimal(4), reader.GetDecimal(5),
                reader.GetBoolean(6), reader.IsDBNull(7) ? null : reader.GetDateTime(7), reader.GetString(8)));
        return rows;
    }

    private static void Money(SqlCommand command, string name, decimal value)
    {
        var parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18; parameter.Scale = 2; parameter.Value = value;
    }
}
