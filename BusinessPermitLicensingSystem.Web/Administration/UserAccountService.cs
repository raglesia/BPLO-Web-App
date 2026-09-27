using System.Data;
using BusinessPermitLicensingSystem.Web.Authentication;
using Microsoft.Data.SqlClient;

namespace BusinessPermitLicensingSystem.Web.Administration;

public sealed record UserAccountRecord(int Id, string FullName, string Username, string Position);
public sealed record UserAccountPage(IReadOnlyList<UserAccountRecord> Accounts, int Total, int Page);
public sealed record AccountChangeResult(bool Success, string Message, int Id = 0);

public sealed class UserAccountService(IConfiguration configuration)
{
    public const int PageSize = 25;

    public async Task<AccountChangeResult> CreatePublicAsync(string fullName, string username, string position,
        string password, CancellationToken token)
    {
        var fields = ValidateFields(fullName, username, position, 1);
        if (fields is not null) return new(false, fields);
        var passwordError = ValidatePassword(password);
        if (passwordError is not null) return new(false, passwordError);
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);
        try
        {
            await using var insert = new SqlCommand("""
                INSERT INTO Users(FullName, Username, Position, Password)
                OUTPUT INSERTED.Id VALUES(@name,@username,@position,@hash)
                """, connection, transaction);
            AddFields(insert, fullName.Trim(), username.Trim(), position.Trim());
            insert.Parameters.Add("@hash", SqlDbType.NVarChar, 512).Value = PasswordCompatibility.HashPbkdf2(password);
            int id = (int)(await insert.ExecuteScalarAsync(token) ?? throw new InvalidOperationException("User insert returned no ID."));
            // The public registrant is the subject of this event; the action does not claim an authenticated actor.
            await AuditAsync(connection, transaction, "Public Account Registration", username.Trim(), id, token);
            await transaction.CommitAsync(token);
            return new(true, "Account created successfully.", id);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { await transaction.RollbackAsync(CancellationToken.None); return new(false, "That username is already in use."); }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task<UserAccountPage> ListAsync(string? search, int page, CancellationToken token)
    {
        await using var connection = await OpenAsync(token);
        string term = (search ?? "").Trim();
        if (term.Length > 100) term = term[..100];
        string pattern = "%" + term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
        const string where = "WHERE @term='' OR FullName LIKE @pattern OR Username LIKE @pattern OR Position LIKE @pattern";
        await using var count = new SqlCommand($"SELECT COUNT(*) FROM Users {where}", connection);
        AddSearch(count, term, pattern);
        int total = (int)(await count.ExecuteScalarAsync(token) ?? 0);
        page = Math.Clamp(page, 1, Math.Max(1, (total + PageSize - 1) / PageSize));
        await using var query = new SqlCommand($"""
            SELECT Id, FullName, Username, Position FROM Users {where}
            ORDER BY FullName, Id OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
            """, connection);
        AddSearch(query, term, pattern);
        query.Parameters.Add("@skip", SqlDbType.Int).Value = (page - 1) * PageSize;
        query.Parameters.Add("@take", SqlDbType.Int).Value = PageSize;
        var accounts = new List<UserAccountRecord>();
        await using var reader = await query.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) accounts.Add(Read(reader));
        return new(accounts, total, page);
    }

    public async Task<UserAccountRecord?> GetAsync(int id, CancellationToken token)
    {
        if (id <= 0) return null;
        await using var connection = await OpenAsync(token);
        await using var query = new SqlCommand("SELECT Id, FullName, Username, Position FROM Users WHERE Id=@id", connection);
        query.Parameters.Add("@id", SqlDbType.Int).Value = id;
        await using var reader = await query.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? Read(reader) : null;
    }

    public async Task<AccountChangeResult> CreateAsync(string fullName, string username, string position,
        string password, int actorId, CancellationToken token)
    {
        var fields = ValidateFields(fullName, username, position, actorId);
        if (fields is not null) return new(false, fields);
        var passwordError = ValidatePassword(password);
        if (passwordError is not null) return new(false, passwordError);
        fullName = fullName.Trim(); username = username.Trim(); position = position.Trim();
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);
        try
        {
            await using var insert = new SqlCommand("""
                INSERT INTO Users(FullName, Username, Position, Password)
                OUTPUT INSERTED.Id VALUES(@name,@username,@position,@hash)
                """, connection, transaction);
            AddFields(insert, fullName, username, position);
            insert.Parameters.Add("@hash", SqlDbType.NVarChar, 512).Value = PasswordCompatibility.HashPbkdf2(password);
            int id = (int)(await insert.ExecuteScalarAsync(token) ?? throw new InvalidOperationException("User insert returned no ID."));
            await AuditAsync(connection, transaction, "Create User Account", username, actorId, token);
            await transaction.CommitAsync(token);
            return new(true, "Account created.", id);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { await transaction.RollbackAsync(CancellationToken.None); return new(false, "That username is already in use."); }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task<AccountChangeResult> EditAsync(int id, string fullName, string username,
        string position, int actorId, CancellationToken token)
    {
        if (id <= 0) return new(false, "Account was not found.");
        var fields = ValidateFields(fullName, username, position, actorId);
        if (fields is not null) return new(false, fields);
        fullName = fullName.Trim(); username = username.Trim(); position = position.Trim();
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);
        try
        {
            await using var update = new SqlCommand("""
                UPDATE Users SET FullName=@name, Username=@username, Position=@position WHERE Id=@id
                """, connection, transaction);
            AddFields(update, fullName, username, position);
            update.Parameters.Add("@id", SqlDbType.Int).Value = id;
            if (await update.ExecuteNonQueryAsync(token) != 1)
            { await transaction.RollbackAsync(token); return new(false, "Account was not found."); }
            await AuditAsync(connection, transaction, "Update User Account", username, actorId, token);
            await transaction.CommitAsync(token);
            return new(true, "Account updated.", id);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { await transaction.RollbackAsync(CancellationToken.None); return new(false, "That username is already in use."); }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task<AccountChangeResult> ResetPasswordAsync(int id, string password, int actorId, CancellationToken token)
    {
        if (id <= 0) return new(false, "Account was not found.");
        if (actorId <= 0) return new(false, "Sign in again before changing an account.");
        var passwordError = ValidatePassword(password);
        if (passwordError is not null) return new(false, passwordError);
        await using var connection = await OpenAsync(token);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);
        try
        {
            await using var update = new SqlCommand("""
                UPDATE Users SET Password=@hash OUTPUT INSERTED.Username WHERE Id=@id
                """, connection, transaction);
            update.Parameters.Add("@hash", SqlDbType.NVarChar, 512).Value = PasswordCompatibility.HashPbkdf2(password);
            update.Parameters.Add("@id", SqlDbType.Int).Value = id;
            string? username = (string?)await update.ExecuteScalarAsync(token);
            if (username is null)
            { await transaction.RollbackAsync(token); return new(false, "Account was not found."); }
            await AuditAsync(connection, transaction, "Reset User Password", username, actorId, token);
            await transaction.CommitAsync(token);
            return new(true, "Password reset. Existing signed-in sessions remain active.", id);
        }
        catch { await transaction.RollbackAsync(CancellationToken.None); throw; }
    }

    private static string? ValidateFields(string fullName, string username, string position, int actorId)
    {
        if (actorId <= 0) return "Sign in again before changing an account.";
        if (string.IsNullOrWhiteSpace(fullName)) return "Full Name is required.";
        if (string.IsNullOrWhiteSpace(username)) return "Username is required.";
        if (string.IsNullOrWhiteSpace(position)) return "Position is required.";
        if (fullName.Trim().Length > 255 || username.Trim().Length > 255 || position.Trim().Length > 255)
            return "Account fields must be 255 characters or fewer.";
        return null;
    }

    private static string? ValidatePassword(string password) =>
        string.IsNullOrWhiteSpace(password) || password.Length < 8
            ? "Password must contain at least 8 characters." : null;

    private static void AddFields(SqlCommand command, string name, string username, string position)
    {
        command.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = name;
        command.Parameters.Add("@username", SqlDbType.NVarChar, 255).Value = username;
        command.Parameters.Add("@position", SqlDbType.NVarChar, 255).Value = position;
    }

    private static void AddSearch(SqlCommand command, string term, string pattern)
    {
        command.Parameters.Add("@term", SqlDbType.NVarChar, 100).Value = term;
        command.Parameters.Add("@pattern", SqlDbType.NVarChar, 512).Value = pattern;
    }

    private static UserAccountRecord Read(SqlDataReader reader) =>
        new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));

    private static async Task AuditAsync(SqlConnection connection, SqlTransaction transaction,
        string action, string username, int actorId, CancellationToken token)
    {
        await using var audit = new SqlCommand("""
            INSERT INTO AuditTrail(Action, SIN, UserId, Details)
            VALUES(@action, NULL, @actor, @detail)
            """, connection, transaction);
        audit.Parameters.Add("@action", SqlDbType.NVarChar, 255).Value = action;
        audit.Parameters.Add("@actor", SqlDbType.Int).Value = actorId;
        audit.Parameters.Add("@detail", SqlDbType.NVarChar, -1).Value = $"{action}: {username}";
        if (await audit.ExecuteNonQueryAsync(token) != 1) throw new InvalidOperationException("Account audit was not saved.");
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken token)
    {
        var builder = new SqlConnectionStringBuilder(configuration.GetConnectionString("BPLS"));
        if (!string.Equals(builder.InitialCatalog, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("User management requires BPLS_Dev.");
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(token);
            if (!string.Equals(connection.Database, "BPLS_Dev", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("User management requires BPLS_Dev.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }
}
