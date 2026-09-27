using BusinessPermitLicensingSystem.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;

namespace BusinessPermitLicensingSystem
{
    internal static class Database
    {
        // ===================== CONNECTION ===================== //

        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["BPLS"]?.ConnectionString
            ?? throw new InvalidOperationException("Connection string 'BPLS' not found in App.config.");

        public static string GetConnectionString() => ConnectionString;

        private static SqlConnection OpenConnection()
        {
            var con = new SqlConnection(ConnectionString);
            con.Open();
            return con;
        }

        private static void ExecuteNonQuery(SqlConnection con, string sql, SqlTransaction? tran = null)
        {
            using var cmd = tran != null
                ? new SqlCommand(sql, con, tran)
                : new SqlCommand(sql, con);
            cmd.ExecuteNonQuery();
        }

        private static DataTable FillDataTable(SqlCommand cmd)
        {
            using var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        private static SqlParameter ParamNVarChar(string name, string value, int size)
            => new SqlParameter(name, SqlDbType.NVarChar, size) { Value = (object)value ?? DBNull.Value };

        private static SqlParameter ParamDecimal(string name, decimal value)
            => new SqlParameter(name, SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = value };

        private static SqlParameter ParamInt(string name, int value)
            => new SqlParameter(name, SqlDbType.Int) { Value = value };

        private static SqlParameter ParamDateTime(string name, DateTime value)
            => new SqlParameter(name, SqlDbType.DateTime) { Value = value };

        private static SqlParameter ParamNullableString(string name, string? value, int size)
            => new SqlParameter(name, SqlDbType.NVarChar, size) { Value = (object?)value ?? DBNull.Value };

        private static string NormaliseDateString(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            if (DateTime.TryParse(raw, out var dt))
                return dt.ToString("yyyy-MM-dd");

            return "";
        }

        // ===================== INITIALIZE ===================== //

        public static void Initialize()
        {
            using var con = OpenConnection();

            // ── USERS ──────────────────────────────────────────────────────────
            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                CREATE TABLE Users (
                    Id       INT           IDENTITY(1,1) PRIMARY KEY,
                    FullName NVARCHAR(255) NOT NULL,
                    Username NVARCHAR(255) NOT NULL UNIQUE,
                    Position NVARCHAR(255) NOT NULL DEFAULT '',
                    Password NVARCHAR(512) NOT NULL,
                    Created  DATETIME      NOT NULL DEFAULT GETDATE()
                );");

            // ── STALL OWNERS ───────────────────────────────────────────────────
            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Profiling' AND xtype='U')
                CREATE TABLE Profiling (
                    SIN              NVARCHAR(100) PRIMARY KEY,
                    FullName         NVARCHAR(255) NOT NULL,
                    BusinessName     NVARCHAR(255) NOT NULL,
                    BusinessSection  NVARCHAR(255) NOT NULL,
                    StallNumber      NVARCHAR(100) NOT NULL,
                    StallSize        NVARCHAR(100) NOT NULL,
                    MonthlyRental    DECIMAL(18,2) NOT NULL,
                    PaymentStatus    NVARCHAR(50)  NOT NULL DEFAULT 'Unpaid',
                    StartDate        NVARCHAR(50)           DEFAULT '',
                    Penalty          DECIMAL(18,2)          DEFAULT 0,
                    AdditionalCharge DECIMAL(18,2)          DEFAULT 0,
                    IsArchived       INT                    DEFAULT 0,
                    DatePaid         NVARCHAR(50)           DEFAULT '',
                    UNIQUE(FullName, BusinessName, StallNumber)
                );");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditTrail' AND xtype='U')
                CREATE TABLE AuditTrail (
                    Id        INT           IDENTITY(1,1) PRIMARY KEY,
                    Action    NVARCHAR(255) NOT NULL,
                    SIN       NVARCHAR(100),
                    UserId    INT           NOT NULL,
                    Timestamp DATETIME      NOT NULL DEFAULT GETDATE(),
                    Details   NVARCHAR(MAX)
                );");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PaymentHistory' AND xtype='U')
                CREATE TABLE PaymentHistory (
                    Id         INT           IDENTITY(1,1) PRIMARY KEY,
                    SIN        NVARCHAR(100) NOT NULL,
                    ORNumber   NVARCHAR(100) NOT NULL UNIQUE,
                    AmountPaid DECIMAL(18,2) NOT NULL,
                    Penalty    DECIMAL(18,2) NOT NULL DEFAULT 0,
                    DatePaid   DATETIME      NOT NULL DEFAULT GETDATE(),
                    RecordedBy INT           NOT NULL,
                    FOREIGN KEY (SIN) REFERENCES Profiling(SIN)
                );");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RentalRates' AND xtype='U')
                CREATE TABLE RentalRates (
                    Section    NVARCHAR(255) PRIMARY KEY,
                    RatePerSqm DECIMAL(18,2) NOT NULL DEFAULT 0,
                    FlatRate   DECIMAL(18,2) NOT NULL DEFAULT 0,
                    RateType   NVARCHAR(50)  NOT NULL DEFAULT 'PerSqm'
                );");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT 1 FROM RentalRates)
                BEGIN
                    INSERT INTO RentalRates (Section, RatePerSqm, FlatRate, RateType) VALUES
                        ('Pharmacy (Below 100k)',     150,    0, 'PerSqm'),
                        ('Pharmacy (100k-250k)',      250,    0, 'PerSqm'),
                        ('Pharmacy (Above 250k)',     350,    0, 'PerSqm'),
                        ('Masinloc Mall Stalls',      150,    0, 'PerSqm'),
                        ('Masinloc Mall Food Court',    0, 1200, 'Flat'),
                        ('Corridor',                   0, 1200, 'Flat'),
                        ('Public Market Stalls',      210,    0, 'PerSqm'),
                        ('Carinderia',                210,    0, 'PerSqm'),
                        ('Fruits and Vegetable',      600,    0, 'PerSqm'),
                        ('Fish',                      600,    0, 'PerSqm'),
                        ('Meat',                      600,    0, 'PerSqm'),
                        ('Burger Area',                 0, 1000, 'Flat'),
                        ('Kakanin Area',                0,  300, 'Flat'),
                        ('Pasalubong Center',           0, 5500, 'Flat')
                END");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (
                    SELECT * FROM sysobjects
                    WHERE name='MonthlyBilling' AND xtype='U'
                )
                CREATE TABLE MonthlyBilling (
                    Id               INT IDENTITY(1,1) PRIMARY KEY,
                    SIN              NVARCHAR(100) NOT NULL,
                    BillingYear      INT NOT NULL,
                    BillingMonth     INT NOT NULL,
                    MonthlyRental    DECIMAL(18,2) NOT NULL,
                    AdditionalCharge DECIMAL(18,2) NOT NULL DEFAULT 0,
                    Penalty          DECIMAL(18,2) NOT NULL DEFAULT 0,
                    PaymentStatus    NVARCHAR(50) NOT NULL DEFAULT 'Unpaid',
                    ORNumber         NVARCHAR(100) NULL,
                    DatePaid         DATETIME NULL,
                    RecordedBy       INT NULL,

                    FOREIGN KEY (SIN) REFERENCES Profiling(SIN),

                    CONSTRAINT UQ_MonthlyBilling
                        UNIQUE (SIN, BillingYear, BillingMonth)
                );");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (
                    SELECT * FROM sysobjects
                    WHERE name='PaymentHistoryBilling' AND xtype='U'
                )
                CREATE TABLE PaymentHistoryBilling (
                    PaymentHistoryId INT NOT NULL,
                    MonthlyBillingId INT NOT NULL,

                    PRIMARY KEY (PaymentHistoryId, MonthlyBillingId),

                    FOREIGN KEY (PaymentHistoryId)
                        REFERENCES PaymentHistory(Id),

                    FOREIGN KEY (MonthlyBillingId)
                        REFERENCES MonthlyBilling(Id)
                );");


            // ── MONTHLY RESET ─────────────────────────────────────────
            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppSettings' AND xtype='U')
                CREATE TABLE AppSettings (
                    [Key]   NVARCHAR(100) PRIMARY KEY,
                    [Value] NVARCHAR(255) NOT NULL
                );");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = 'LastMonthlyReset')
                INSERT INTO AppSettings ([Key], [Value]) VALUES ('LastMonthlyReset', '2000-01');");

            // ── SPECIAL VEHICLE PERMIT ─────────────────────────────────────────
            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='VehiclePermits' AND xtype='U')
                CREATE TABLE VehiclePermits (
                    VIN         NVARCHAR(100) PRIMARY KEY,
                    CompanyName NVARCHAR(255) NOT NULL,
                    DriverName  NVARCHAR(255) NOT NULL DEFAULT '',
                    PlateNo     NVARCHAR(100) NOT NULL UNIQUE,
                    SECRegNo    NVARCHAR(100)          DEFAULT '',
                    DTINumber   NVARCHAR(100)          DEFAULT '',
                    IsArchived  INT                    DEFAULT 0,
                    DateAdded   DATETIME               DEFAULT GETDATE()
                );");

            ExecuteNonQuery(con, @"
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='VehiclePermitHistory' AND xtype='U')
                CREATE TABLE VehiclePermitHistory (
                    Id         INT           IDENTITY(1,1) PRIMARY KEY,
                    VIN        NVARCHAR(100) NOT NULL,
                    ORNumber   NVARCHAR(100) NOT NULL UNIQUE,
                    AmountPaid DECIMAL(18,2) NOT NULL,
                    PermitYear INT           NOT NULL,
                    DatePaid   DATETIME      NOT NULL DEFAULT GETDATE(),
                    RecordedBy INT           NOT NULL,
                    FOREIGN KEY (VIN)        REFERENCES VehiclePermits(VIN),
                    FOREIGN KEY (RecordedBy) REFERENCES Users(Id)
                );");

            ExecuteNonQuery(con, @"
                IF OBJECT_ID('dbo.VehiclePermitFeeDrafts', 'U') IS NULL
                CREATE TABLE dbo.VehiclePermitFeeDrafts (
                    VIN          NVARCHAR(100) NOT NULL,
                    PermitYear   INT NOT NULL,
                    DetailsJson  NVARCHAR(MAX) NOT NULL,
                    GrandTotal   DECIMAL(18,2) NOT NULL,
                    UpdatedAt    DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_VehiclePermitFeeDrafts PRIMARY KEY (VIN, PermitYear),
                    CONSTRAINT FK_VehiclePermitFeeDrafts_VehiclePermits
                        FOREIGN KEY (VIN) REFERENCES dbo.VehiclePermits(VIN)
                );");

            // ── MIGRATIONS ─────────────────────────────────────────────────────
            var migrations = new (string Label, string Sql)[]
            {
                ("Profiling.StartDate",
                 "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'StartDate')        ALTER TABLE Profiling ADD StartDate        NVARCHAR(50)  DEFAULT ''"),
                ("Profiling.Penalty",
                 "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'Penalty')          ALTER TABLE Profiling ADD Penalty          DECIMAL(18,2) DEFAULT 0"),
                ("Profiling.AdditionalCharge",
                 "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'AdditionalCharge') ALTER TABLE Profiling ADD AdditionalCharge DECIMAL(18,2) DEFAULT 0"),
                ("Users.Position",
                 "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users')     AND name = 'Position')         ALTER TABLE Users     ADD Position         NVARCHAR(255) NOT NULL DEFAULT ''"),
                ("Profiling.IsArchived",
                 "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'IsArchived')       ALTER TABLE Profiling ADD IsArchived        INT           DEFAULT 0"),
            };

            var vehicleMigrations = new (string Label, string Sql)[]
            {
                ("VehiclePermits.PermitStatus",
                 "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('VehiclePermits') AND name = 'PermitStatus') ALTER TABLE VehiclePermits ADD PermitStatus NVARCHAR(50) NOT NULL DEFAULT 'Unpaid'"),
                ("VehiclePermits.PermitYear",
                 "IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('VehiclePermits') AND name = 'PermitYear')   ALTER TABLE VehiclePermits ADD PermitYear   INT          NOT NULL DEFAULT 0"),
            };

            RunMigrations(con, migrations);
            RunMigrations(con, vehicleMigrations);
        }

        private static void RunMigrations(SqlConnection con, (string Label, string Sql)[] migrations)
        {
            foreach (var (label, sql) in migrations)
            {
                try
                {
                    ExecuteNonQuery(con, sql);
                }
                catch (SqlException ex)
                {
                    if (ex.Number != 2705)
                        throw new InvalidOperationException(
                            $"Migration failed [{label}]: {ex.Message}", ex);
                }
            }
        }

        // ===================== USERS ===================== //

        public static string GetFullName(long userId)
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand("SELECT FullName FROM Users WHERE Id = @id", con);
            cmd.Parameters.Add(ParamInt("@id", (int)userId));
            return cmd.ExecuteScalar()?.ToString() ?? "Unknown";
        }

        public static string GetPosition(long userId)
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand("SELECT Position FROM Users WHERE Id = @id", con);
            cmd.Parameters.Add(ParamInt("@id", (int)userId));
            return cmd.ExecuteScalar()?.ToString() ?? "";
        }

        public static (bool Success, string? ErrorMessage) CreateAccount(
            string fullname,
            string username,
            string position,
            string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(fullname)) return (false, "Full Name cannot be empty.");
            if (string.IsNullOrWhiteSpace(username)) return (false, "Username cannot be empty.");
            if (string.IsNullOrWhiteSpace(position)) return (false, "Position / Title cannot be empty.");
            if (string.IsNullOrWhiteSpace(plainPassword)) return (false, "Password cannot be empty.");

            username = username.Trim();

            try
            {
                using var con = OpenConnection();

                using var cmdCheck = new SqlCommand("SELECT 1 FROM Users WHERE Username = @u", con);
                cmdCheck.Parameters.Add(ParamNVarChar("@u", username, 255));
                if (cmdCheck.ExecuteScalar() != null)
                    return (false, "Username already exists.");

                using var cmd = new SqlCommand(@"
                    INSERT INTO Users (FullName, Username, Position, Password)
                    VALUES (@f, @u, @pos, @p)", con);

                cmd.Parameters.Add(ParamNVarChar("@f", fullname.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@u", username, 255));
                cmd.Parameters.Add(ParamNVarChar("@pos", position.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@p", HashPasswordPbkdf2(plainPassword), 512));

                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                return (false, "Username already exists.");
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        public static (bool IsValid, string? MessageOrUserId) VerifyLogin(
            string username,
            string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(plainPassword))
                return (false, "Missing username or password.");

            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(
                    "SELECT Id, Password FROM Users WHERE Username = @u", con);
                cmd.Parameters.Add(ParamNVarChar("@u", username.Trim(), 255));

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return (false, "Invalid username or password.");

                long userId = reader.GetInt32(0);
                string storedHash = reader.GetString(1);
                reader.Close();

                bool valid = false;

                if (IsPbkdf2Hash(storedHash))
                {
                    valid = VerifyPasswordPbkdf2(plainPassword, storedHash);
                }
                else
                {
                    valid = CryptographicEquals(HashPasswordSha256(plainPassword), storedHash);
                    if (valid)
                        RehashPassword(con, userId, plainPassword);
                }

                return valid
                    ? (true, userId.ToString())
                    : (false, "Invalid username or password.");
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}");
            }
        }

        private static void RehashPassword(SqlConnection con, long userId, string plainPassword)
        {
            try
            {
                using var cmd = new SqlCommand(
                    "UPDATE Users SET Password = @p WHERE Id = @id", con);
                cmd.Parameters.Add(ParamNVarChar("@p", HashPasswordPbkdf2(plainPassword), 512));
                cmd.Parameters.Add(ParamInt("@id", (int)userId));
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        // ===================== STALL OWNERS PROFILING ===================== //

        private const string ProfilingSelectColumns = @"
            SIN                                                 AS [SIN],
            FullName                                            AS [Full Name],
            BusinessName                                        AS [Business Name],
            BusinessSection                                     AS [Business Section],
            StallNumber                                         AS [Stall Number],
            StallSize                                           AS [Stall Size],
            MonthlyRental                                       AS [Monthly Rental],
            PaymentStatus                                       AS [Payment Status],
            Penalty                                             AS [Penalty],
            AdditionalCharge                                    AS [Additional Charge],
            CASE
                WHEN TRY_CONVERT(DATE, StartDate, 23) IS NOT NULL
                THEN FORMAT(TRY_CONVERT(DATE, StartDate, 23), 'MM/dd/yyyy')
                ELSE ''
            END                                                 AS [Date of Occupancy]";
        public static DataTable GetAllProfiles()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand($@"
                SELECT {ProfilingSelectColumns}
                FROM   Profiling
                WHERE  IsArchived = 0
                ORDER BY SIN ASC", con);
            return FillDataTable(cmd);
        }

        public static DataTable GetArchivedProfiles()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand($@"
                SELECT {ProfilingSelectColumns}
                FROM   Profiling
                WHERE  IsArchived = 1
                ORDER BY SIN DESC", con);
            return FillDataTable(cmd);
        }

        public static (bool Success, string? ErrorMessage) AddProfiling(
            string sin, string fullName, string businessName,
            string businessSection, string stallNumber, string stallSize,
            decimal monthlyRental, string startDate, decimal additionalCharge = 0)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(businessName) ||
                string.IsNullOrWhiteSpace(businessSection) ||
                string.IsNullOrWhiteSpace(stallNumber) ||
                string.IsNullOrWhiteSpace(stallSize))
                return (false, "All fields are required.");

            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    INSERT INTO Profiling
                        (SIN, FullName, BusinessName, BusinessSection,
                         StallNumber, StallSize, MonthlyRental, StartDate, AdditionalCharge)
                    VALUES
                        (@sin, @f, @b, @s, @n, @sz, @r, @startDate, @additional)", con);

                cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));
                cmd.Parameters.Add(ParamNVarChar("@f", fullName.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@b", businessName.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@s", businessSection.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@n", stallNumber.Trim(), 100));
                cmd.Parameters.Add(ParamNVarChar("@sz", stallSize.Trim(), 100));
                cmd.Parameters.Add(ParamDecimal("@r", monthlyRental));
                cmd.Parameters.Add(ParamNVarChar("@startDate", NormaliseDateString(startDate), 50));
                cmd.Parameters.Add(ParamDecimal("@additional", additionalCharge));

                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                return (false, "This profile already exists (duplicate).");
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        public static (bool Success, string? ErrorMessage) UpdateProfiling(
            string sin, string fullName, string businessName,
            string businessSection, string stallNumber, string stallSize,
            decimal monthlyRental, string startDate, decimal additionalCharge = 0)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    UPDATE Profiling
                    SET FullName         = @FullName,
                        BusinessName     = @BusinessName,
                        BusinessSection  = @BusinessSection,
                        StallNumber      = @StallNumber,
                        StallSize        = @StallSize,
                        MonthlyRental    = @MonthlyRental,
                        StartDate        = @StartDate,
                        AdditionalCharge = @AdditionalCharge
                    WHERE SIN = @SIN", con);

                cmd.Parameters.Add(ParamNVarChar("@FullName", fullName, 255));
                cmd.Parameters.Add(ParamNVarChar("@BusinessName", businessName, 255));
                cmd.Parameters.Add(ParamNVarChar("@BusinessSection", businessSection, 255));
                cmd.Parameters.Add(ParamNVarChar("@StallNumber", stallNumber, 100));
                cmd.Parameters.Add(ParamNVarChar("@StallSize", stallSize, 100));
                cmd.Parameters.Add(ParamDecimal("@MonthlyRental", monthlyRental));
                cmd.Parameters.Add(ParamNVarChar("@StartDate", NormaliseDateString(startDate), 50));
                cmd.Parameters.Add(ParamDecimal("@AdditionalCharge", additionalCharge));
                cmd.Parameters.Add(ParamNVarChar("@SIN", sin, 100));

                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static string GenerateUniqueSIN()
        {
            string year = DateTime.Now.Year.ToString();
            int nextNumber = 1;

            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT MAX(CAST(RIGHT(SIN, 4) AS INT))
                FROM   Profiling
                WHERE  SIN LIKE @pattern", con);
            cmd.Parameters.Add(ParamNVarChar("@pattern", $"SIN-{year}-%", 50));

            var result = cmd.ExecuteScalar();
            if (result != DBNull.Value && result != null)
                nextNumber = Convert.ToInt32(result) + 1;

            return $"SIN-{year}-{nextNumber:D4}";
        }

        // ===================== RENTAL RATES ===================== //

        public static DataTable GetRentalRates()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(
                "SELECT Section, RatePerSqm, FlatRate, RateType FROM RentalRates ORDER BY Section",
                con);
            return FillDataTable(cmd);
        }

        public static (decimal RatePerSqm, decimal FlatRate, string RateType) GetRateBySection(
            string section)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    SELECT RatePerSqm, FlatRate, RateType
                    FROM   RentalRates
                    WHERE  Section = @section", con);
                cmd.Parameters.Add(ParamNVarChar("@section", section, 255));

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                    return (
                        Convert.ToDecimal(reader["RatePerSqm"]),
                        Convert.ToDecimal(reader["FlatRate"]),
                        reader["RateType"].ToString()!
                    );

                return (0, 0, "PerSqm");
            }
            catch { return (0, 0, "PerSqm"); }
        }

        public static (bool Success, string? ErrorMessage) UpdateRentalRate(
            string section, decimal ratePerSqm, decimal flatRate, string rateType)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    UPDATE RentalRates
                    SET RatePerSqm = @ratePerSqm,
                        FlatRate   = @flatRate,
                        RateType   = @rateType
                    WHERE Section  = @section", con);

                cmd.Parameters.Add(ParamDecimal("@ratePerSqm", ratePerSqm));
                cmd.Parameters.Add(ParamDecimal("@flatRate", flatRate));
                cmd.Parameters.Add(ParamNVarChar("@rateType", rateType, 50));
                cmd.Parameters.Add(ParamNVarChar("@section", section, 255));

                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static (bool Success, string? ErrorMessage) AddRentalRate(
            string section, decimal ratePerSqm, decimal flatRate, string rateType)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    INSERT INTO RentalRates (Section, RatePerSqm, FlatRate, RateType)
                    VALUES (@section, @ratePerSqm, @flatRate, @rateType)", con);

                cmd.Parameters.Add(ParamNVarChar("@section", section.Trim(), 255));
                cmd.Parameters.Add(ParamDecimal("@ratePerSqm", ratePerSqm));
                cmd.Parameters.Add(ParamDecimal("@flatRate", flatRate));
                cmd.Parameters.Add(ParamNVarChar("@rateType", rateType, 50));

                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                return (false, "Section already exists.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        // ===================== PAYMENT STATUS ===================== //

        public static (bool Success, string? ErrorMessage) UpdatePaymentStatus(
            string sin,
            string status,
            string orNumber = "",
            decimal amountPaid = 0,
            decimal penalty = 0,
            long recordedBy = 0)
        {
            if (string.IsNullOrWhiteSpace(sin))
                return (false, "SIN is required.");

            if (status != "Paid" && status != "Unpaid" && status != "Unverified")
                return (false, "Invalid payment status.");

            if (status == "Paid" && string.IsNullOrWhiteSpace(orNumber))
                return (false, "OR Number is required.");

            // Make sure every billing period from the first billable month
            // through the current month exists before a payment is processed.
            if (status == "Paid")
            {
                using var profileCon = OpenConnection();
                using var profileCmd = new SqlCommand(@"
                    SELECT StartDate, MonthlyRental, AdditionalCharge
                    FROM Profiling
                    WHERE SIN = @sin
                    AND IsArchived = 0", profileCon);

                profileCmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));

                using var profileReader = profileCmd.ExecuteReader();
                if (!profileReader.Read())
                    return (false, "Profile not found.");

                string startDate = profileReader["StartDate"]?.ToString() ?? "";
                decimal monthlyRental = Convert.ToDecimal(profileReader["MonthlyRental"]);
                decimal additionalCharge = Convert.ToDecimal(profileReader["AdditionalCharge"]);
                profileReader.Close();

                EnsureBillingPeriodsForProfile(
                    sin,
                    startDate,
                    monthlyRental,
                    additionalCharge);
            }

            try
            {
                using var con = OpenConnection();
                using var tran = con.BeginTransaction();

                try
                {
                    if (status == "Paid")
                    {
                        DateTime today = DateTime.Today;

                        // -------------------------------------------------
                        // Management rule: no staggered/partial payments.
                        // Every unpaid billing period through this month is
                        // settled by one OR number in one transaction.
                        // -------------------------------------------------
                        decimal rentDue = 0;
                        decimal additionalDue = 0;
                        decimal calculatedPenalty = 0;
                        int outstandingCount = 0;

                        using (var cmdOutstanding = new SqlCommand(@"
                            SELECT
                                Id,
                                BillingYear,
                                BillingMonth,
                                MonthlyRental,
                                AdditionalCharge
                            FROM MonthlyBilling
                            WHERE SIN = @sin
                            AND PaymentStatus = 'Unpaid'
                            AND
                            (
                                BillingYear < @year
                                OR
                                (
                                    BillingYear = @year
                                    AND BillingMonth <= @month
                                )
                            )
                            ORDER BY BillingYear, BillingMonth",
                            con,
                            tran))
                        {
                            cmdOutstanding.Parameters.Add(
                                ParamNVarChar("@sin", sin, 100));
                            cmdOutstanding.Parameters.Add(
                                ParamInt("@year", today.Year));
                            cmdOutstanding.Parameters.Add(
                                ParamInt("@month", today.Month));

                            using var reader = cmdOutstanding.ExecuteReader();
                            while (reader.Read())
                            {
                                outstandingCount++;

                                int year = Convert.ToInt32(reader["BillingYear"]);
                                int month = Convert.ToInt32(reader["BillingMonth"]);
                                decimal rental = Convert.ToDecimal(reader["MonthlyRental"]);
                                decimal additional = Convert.ToDecimal(reader["AdditionalCharge"]);

                                rentDue += rental;
                                additionalDue += additional;

                                DateTime dueDate = new DateTime(year, month, 20);
                                if (today > dueDate)
                                    calculatedPenalty += Math.Round(rental * 0.25m, 2);
                            }
                        }

                        if (outstandingCount == 0)
                        {
                            tran.Rollback();
                            return (false, "There are no outstanding billing periods to pay.");
                        }

                        // Do not trust a caller-supplied single-month amount.
                        // The database computes the full all-or-nothing balance.
                        decimal actualAmountPaid =
                            rentDue + additionalDue + calculatedPenalty;

                        var (ok, err, paymentHistoryId) = RecordPaymentInternal(
                            con,
                            tran,
                            sin,
                            orNumber,
                            actualAmountPaid,
                            calculatedPenalty,
                            recordedBy);

                        if (!ok)
                        {
                            tran.Rollback();
                            return (false, err);
                        }

                        // Save exactly which MonthlyBilling rows this payment covers.
                        using (var cmdLink = new SqlCommand(@"
                            INSERT INTO PaymentHistoryBilling
                                (PaymentHistoryId, MonthlyBillingId)
                            SELECT
                                @paymentHistoryId,
                                Id
                            FROM MonthlyBilling
                            WHERE SIN = @sin
                            AND PaymentStatus = 'Unpaid'
                            AND
                            (
                                BillingYear < @year
                                OR
                                (
                                    BillingYear = @year
                                    AND BillingMonth <= @month
                                )
                            )",
                            con,
                            tran))
                        {
                            cmdLink.Parameters.Add(
                                ParamInt("@paymentHistoryId", paymentHistoryId));
                            cmdLink.Parameters.Add(
                                ParamNVarChar("@sin", sin, 100));
                            cmdLink.Parameters.Add(
                                ParamInt("@year", today.Year));
                            cmdLink.Parameters.Add(
                                ParamInt("@month", today.Month));
                            cmdLink.ExecuteNonQuery();
                        }

                        // Mark the exact same outstanding periods as paid.
                        using (var cmdBilling = new SqlCommand(@"
                            UPDATE MonthlyBilling
                            SET
                                PaymentStatus = 'Paid',
                                ORNumber = @or,
                                DatePaid = @datePaid,
                                RecordedBy = @recordedBy,
                                Penalty = CASE
                                    WHEN DATEFROMPARTS(BillingYear, BillingMonth, 20) < CAST(@today AS DATE)
                                    THEN ROUND(MonthlyRental * 0.25, 2)
                                    ELSE 0
                                END
                            WHERE SIN = @sin
                            AND PaymentStatus = 'Unpaid'
                            AND
                            (
                                BillingYear < @year
                                OR
                                (
                                    BillingYear = @year
                                    AND BillingMonth <= @month
                                )
                            )",
                            con,
                            tran))
                        {
                            cmdBilling.Parameters.Add(
                                ParamNVarChar("@sin", sin, 100));
                            cmdBilling.Parameters.Add(
                                ParamNVarChar("@or", orNumber.Trim(), 100));
                            cmdBilling.Parameters.Add(
                                ParamDateTime("@datePaid", DateTime.Now));
                            cmdBilling.Parameters.Add(
                                ParamDateTime("@today", today));
                            cmdBilling.Parameters.Add(
                                ParamInt("@recordedBy", (int)recordedBy));
                            cmdBilling.Parameters.Add(
                                ParamInt("@year", today.Year));
                            cmdBilling.Parameters.Add(
                                ParamInt("@month", today.Month));
                            cmdBilling.ExecuteNonQuery();
                        }

                        // Profiling is kept as the current UI snapshot.
                        using (var cmdProfile = new SqlCommand(@"
                            UPDATE Profiling
                            SET PaymentStatus = 'Paid',
                                Penalty = 0
                            WHERE SIN = @sin",
                            con,
                            tran))
                        {
                            cmdProfile.Parameters.Add(
                                ParamNVarChar("@sin", sin, 100));
                            cmdProfile.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Keep the existing Profiling snapshot behavior for Unpaid.
                        using var cmdProfile = new SqlCommand(@"
                            UPDATE Profiling
                            SET PaymentStatus = @status,
                                Penalty = CASE WHEN @status = 'Unverified' THEN 0 ELSE Penalty END
                            WHERE SIN = @sin",
                            con,
                            tran);

                        cmdProfile.Parameters.Add(
                            ParamNVarChar("@status", status, 50));
                        cmdProfile.Parameters.Add(
                            ParamNVarChar("@sin", sin, 100));
                        cmdProfile.ExecuteNonQuery();
                    }

                    tran.Commit();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return (false, $"Database error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        // ===================== MONTHLY BILLING ===================== //

        public static void EnsureMonthlyBill(
            string sin,
            decimal monthlyRental,
            decimal additionalCharge = 0)
        {
            DateTime today = DateTime.Today;

            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                IF NOT EXISTS
                (
                    SELECT 1
                    FROM MonthlyBilling
                    WHERE SIN = @sin
                    AND BillingYear = @year
                    AND BillingMonth = @month
                )
                BEGIN
                    INSERT INTO MonthlyBilling
                    (
                        SIN,
                        BillingYear,
                        BillingMonth,
                        MonthlyRental,
                        AdditionalCharge,
                        Penalty,
                        PaymentStatus
                    )
                    VALUES
                    (
                        @sin,
                        @year,
                        @month,
                        @rental,
                        @additional,
                        0,
                        'Unpaid'
                    )
                END", con);

            cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));
            cmd.Parameters.Add(ParamInt("@year", today.Year));
            cmd.Parameters.Add(ParamInt("@month", today.Month));
            cmd.Parameters.Add(ParamDecimal("@rental", monthlyRental));
            cmd.Parameters.Add(ParamDecimal("@additional", additionalCharge));
            cmd.ExecuteNonQuery();
        }

        public static (
            decimal RentDue,
            decimal AdditionalDue,
            decimal PenaltyDue,
            decimal TotalDue,
            int MonthsDue)
        GetOutstandingBalance(string sin)
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    mb.BillingYear,
                    mb.BillingMonth,
                    mb.MonthlyRental,
                    mb.AdditionalCharge
                FROM MonthlyBilling mb
                JOIN Profiling p ON p.SIN = mb.SIN
                WHERE mb.SIN = @sin
                AND p.PaymentStatus <> 'Unverified'
                AND mb.PaymentStatus = 'Unpaid'
                AND
                (
                    BillingYear < YEAR(GETDATE())
                    OR
                    (
                        BillingYear = YEAR(GETDATE())
                        AND BillingMonth <= MONTH(GETDATE())
                    )
                )
                ORDER BY BillingYear, BillingMonth", con);

            cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));

            decimal rentDue = 0;
            decimal additionalDue = 0;
            decimal penaltyDue = 0;
            int monthsDue = 0;
            DateTime today = DateTime.Today;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                monthsDue++;

                int year = Convert.ToInt32(reader["BillingYear"]);
                int month = Convert.ToInt32(reader["BillingMonth"]);
                decimal rental = Convert.ToDecimal(reader["MonthlyRental"]);
                decimal additional = Convert.ToDecimal(reader["AdditionalCharge"]);

                rentDue += rental;
                additionalDue += additional;

                DateTime dueDate = new DateTime(year, month, 20);
                if (today > dueDate)
                    penaltyDue += Math.Round(rental * 0.25m, 2);
            }

            decimal totalDue = rentDue + additionalDue + penaltyDue;

            return (
                rentDue,
                additionalDue,
                penaltyDue,
                totalDue,
                monthsDue);
        }

        public static DataTable GetMonthlyBilling(string sin)
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    Id AS [#],
                    CONCAT(
                        DATENAME(MONTH, DATEFROMPARTS(BillingYear, BillingMonth, 1)),
                        ' ',
                        BillingYear
                    ) AS [Billing Month],
                    MonthlyRental AS [Monthly Rental],
                    AdditionalCharge AS [Additional Charges],
                    Penalty AS [Penalty],
                    PaymentStatus AS [Payment Status],
                    ISNULL(ORNumber, '') AS [OR Number],
                    CASE
                        WHEN DatePaid IS NULL THEN ''
                        ELSE FORMAT(DatePaid, 'MM/dd/yyyy HH:mm')
                    END AS [Date Paid]
                FROM MonthlyBilling
                WHERE SIN = @sin
                ORDER BY BillingYear DESC, BillingMonth DESC", con);

            cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));
            return FillDataTable(cmd);
        }

        // ===================== PAYMENT HISTORY ===================== //

        private static (
            bool Success,
            string? ErrorMessage,
            int PaymentHistoryId)
        RecordPaymentInternal(
            SqlConnection con,
            SqlTransaction tran,
            string sin,
            string orNumber,
            decimal amountPaid,
            decimal penalty,
            long recordedBy)
        {
            try
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO PaymentHistory
                        (SIN, ORNumber, AmountPaid, Penalty, DatePaid, RecordedBy)
                    OUTPUT INSERTED.Id
                    VALUES
                        (@sin, @or, @amount, @penalty, @date, @recordedBy)",
                    con,
                    tran);

                cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));
                cmd.Parameters.Add(ParamNVarChar("@or", orNumber.Trim(), 100));
                cmd.Parameters.Add(ParamDecimal("@amount", amountPaid));
                cmd.Parameters.Add(ParamDecimal("@penalty", penalty));
                cmd.Parameters.Add(ParamDateTime("@date", DateTime.Now));
                cmd.Parameters.Add(ParamInt("@recordedBy", (int)recordedBy));

                int paymentHistoryId =
                    Convert.ToInt32(cmd.ExecuteScalar());

                return (true, null, paymentHistoryId);
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                return (false, "OR Number already exists.", 0);
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}", 0);
            }
        }

        public static (bool Success, string? ErrorMessage) RecordPayment(
            string sin,
            string orNumber,
            decimal amountPaid,
            decimal penalty,
            long recordedBy)
        {
            try
            {
                using var con = OpenConnection();
                using var tran = con.BeginTransaction();

                var (ok, err, _) = RecordPaymentInternal(
                    con,
                    tran,
                    sin,
                    orNumber,
                    amountPaid,
                    penalty,
                    recordedBy);

                if (ok)
                    tran.Commit();
                else
                    tran.Rollback();

                return (ok, err);
            }
            catch (Exception ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
        }

        public static DataTable GetPaymentHistory(string sin)
        {
            using var con = OpenConnection();

            using var cmd = new SqlCommand(@"
        SELECT
            ph.ORNumber AS [OR Number],

            ISNULL((
                SELECT
                    FORMAT(MIN(DATEFROMPARTS(mb.BillingYear, mb.BillingMonth, 1)), 'MMM yyyy')
                    +
                    CASE
                        WHEN COUNT(*) > 1 THEN
                            ' - ' +
                            FORMAT(MAX(DATEFROMPARTS(mb.BillingYear, mb.BillingMonth, 1)), 'MMM yyyy')
                        ELSE ''
                    END
                    +
                    ' (' + CAST(COUNT(*) AS VARCHAR(10)) +
                    CASE WHEN COUNT(*) = 1 THEN ' month)' ELSE ' months)' END
                FROM PaymentHistoryBilling phb
                JOIN MonthlyBilling mb
                    ON mb.Id = phb.MonthlyBillingId
                WHERE phb.PaymentHistoryId = ph.Id
            ), '') AS [Billing Period],

            ISNULL((
                SELECT SUM(mb.MonthlyRental)
                FROM PaymentHistoryBilling phb
                JOIN MonthlyBilling mb
                    ON mb.Id = phb.MonthlyBillingId
                WHERE phb.PaymentHistoryId = ph.Id
            ), ph.AmountPaid - ph.Penalty) AS [Rent Paid],

            ISNULL((
                SELECT SUM(mb.AdditionalCharge)
                FROM PaymentHistoryBilling phb
                JOIN MonthlyBilling mb
                    ON mb.Id = phb.MonthlyBillingId
                WHERE phb.PaymentHistoryId = ph.Id
            ), 0) AS [Additional Charges],

            ph.Penalty AS [Penalty],
            ph.AmountPaid AS [Amount Paid],
            FORMAT(ph.DatePaid, 'MM/dd/yyyy HH:mm') AS [Date Paid],
            u.FullName AS [Recorded By]

            FROM PaymentHistory ph
            LEFT JOIN Users u ON ph.RecordedBy = u.Id
            WHERE ph.SIN = @sin
            ORDER BY ph.DatePaid DESC",
                con);

            cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));

            return FillDataTable(cmd);
        }

        // ===================== STATISTICS ===================== //

        public static (int Total, int Paid, int Unpaid) GetPaymentSummary()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    COUNT(*)                                                              AS Total,
                    ISNULL(SUM(CASE WHEN PaymentStatus = 'Paid'   THEN 1 ELSE 0 END), 0) AS Paid,
                    ISNULL(SUM(CASE WHEN PaymentStatus = 'Unpaid' THEN 1 ELSE 0 END), 0) AS Unpaid
                FROM Profiling
                WHERE IsArchived = 0", con);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (
                    Convert.ToInt32(reader["Total"]),
                    Convert.ToInt32(reader["Paid"]),
                    Convert.ToInt32(reader["Unpaid"])
                );
            return (0, 0, 0);
        }

        public static (decimal TotalCollected, decimal TotalUncollected, decimal TotalPenalty)
            GetCollectionSummary()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    ISNULL(SUM(CASE WHEN PaymentStatus = 'Paid'   THEN MonthlyRental ELSE 0 END), 0) AS TotalCollected,
                    ISNULL(SUM(CASE WHEN PaymentStatus = 'Unpaid' THEN MonthlyRental ELSE 0 END), 0) AS TotalUncollected,
                    ISNULL(SUM(CASE WHEN PaymentStatus = 'Unpaid' THEN Penalty       ELSE 0 END), 0) AS TotalPenalty
                FROM Profiling
                WHERE IsArchived = 0", con);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (
                    Convert.ToDecimal(reader["TotalCollected"]),
                    Convert.ToDecimal(reader["TotalUncollected"]),
                    Convert.ToDecimal(reader["TotalPenalty"])
                );
            return (0, 0, 0);
        }

        // ===================== ARCHIVE ===================== //

        public static (bool Success, string? ErrorMessage) ArchiveProfiling(string sin)
            => SetArchiveStatus(sin, true);

        public static (bool Success, string? ErrorMessage) RestoreProfiling(string sin)
            => SetArchiveStatus(sin, false);

        private static (bool Success, string? ErrorMessage) SetArchiveStatus(
            string sin, bool archived)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    UPDATE Profiling
                    SET IsArchived = @archived
                    WHERE SIN = @sin", con);

                cmd.Parameters.Add(ParamInt("@archived", archived ? 1 : 0));
                cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));
                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        // ===================== PENALTY ===================== //
        public static void UpdatePenalty(string sin, decimal penalty)
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                UPDATE Profiling
                SET Penalty = @penalty
                WHERE SIN = @sin", con);

            cmd.Parameters.Add(ParamDecimal("@penalty", penalty));
            cmd.Parameters.Add(ParamNVarChar("@sin", sin, 100));
            cmd.ExecuteNonQuery();
        }

        public static (int Updated, int Skipped) ApplyPenaltiesToAll()
        {
            int updated = 0;
            int skipped = 0;

            using var con = OpenConnection();

            using var cmd = new SqlCommand(@"
        SELECT
            SIN,
            MonthlyRental,
            AdditionalCharge,
            StartDate
        FROM Profiling
        WHERE IsArchived = 0
        AND PaymentStatus <> 'Unverified'",
                con);

            using var reader = cmd.ExecuteReader();

            var records =
                new List<(
                    string Sin,
                    decimal Rental,
                    decimal Additional,
                    string StartDate)>();

            while (reader.Read())
            {
                records.Add((
                    reader["SIN"].ToString()!,
                    Convert.ToDecimal(reader["MonthlyRental"]),
                    Convert.ToDecimal(reader["AdditionalCharge"]),
                    reader["StartDate"].ToString()!
                ));
            }

            reader.Close();

            foreach (var record in records)
            {
                // Create every missing billing month
                // from occupancy up to current month.
                EnsureBillingPeriodsForProfile(
                    record.Sin,
                    record.StartDate,
                    record.Rental,
                    record.Additional);

                // Only unpaid + overdue monthly rows
                // are counted here.
                decimal penalty =
                    CalculateOutstandingPenalty(record.Sin);

                UpdatePenalty(
                    record.Sin,
                    penalty);

                if (penalty > 0)
                    updated++;
                else
                    skipped++;
            }

            return (updated, skipped);
        }

        // ====================== NEW PENALTY CALCULATION (DON'T FORGET TO REPLACE THE OLD ONE) ===================== //
        public static decimal CalculateOutstandingPenalty(string sin)
        {
            using var con = OpenConnection();

            using var cmd = new SqlCommand(@"
                SELECT
                    mb.BillingYear,
                    mb.BillingMonth,
                    mb.MonthlyRental
                FROM MonthlyBilling mb
                JOIN Profiling p ON p.SIN = mb.SIN
                WHERE mb.SIN = @sin
                AND p.PaymentStatus <> 'Unverified'
                AND mb.PaymentStatus = 'Unpaid'
                ORDER BY BillingYear, BillingMonth", con);

            cmd.Parameters.Add(
                ParamNVarChar("@sin", sin, 100));

            using var reader = cmd.ExecuteReader();

            decimal totalPenalty = 0;
            DateTime today = DateTime.Today;

            while (reader.Read())
            {
                int year =
                    Convert.ToInt32(reader["BillingYear"]);

                int month =
                    Convert.ToInt32(reader["BillingMonth"]);

                decimal rental =
                    Convert.ToDecimal(reader["MonthlyRental"]);

                DateTime dueDate =
                    new DateTime(year, month, 20);

                if (today > dueDate)
                {
                    totalPenalty +=
                        Math.Round(rental * 0.25m, 2);
                }
            }

            return totalPenalty;
        }

        public static void EnsureBillingPeriodsForProfile(
            string sin,
            string startDate,
            decimal monthlyRental,
            decimal additionalCharge = 0)
        {
            if (string.IsNullOrWhiteSpace(startDate))
                return;

            if (!DateTime.TryParse(startDate, out DateTime occupancyDate))
                return;

            DateTime today = DateTime.Today;

            // Your original rule:
            // first rent becomes due the month AFTER occupancy.
            DateTime billingMonth =
                new DateTime(
                    occupancyDate.Year,
                    occupancyDate.Month,
                    1)
                .AddMonths(1);

            DateTime currentMonth =
                new DateTime(
                    today.Year,
                    today.Month,
                    1);

            using var con = OpenConnection();

            while (billingMonth <= currentMonth)
            {
                using var cmd = new SqlCommand(@"
            IF NOT EXISTS (SELECT 1 FROM MonthlyBilling WHERE SIN = @sin AND BillingYear = @year AND BillingMonth = @month)
                BEGIN
            INSERT INTO MonthlyBilling(SIN, BillingYear, BillingMonth, MonthlyRental, AdditionalCharge, Penalty, PaymentStatus)
                VALUES(@sin, @year, @month, @rental, @additional, 0, 'Unpaid')
            END",
                    con);

                cmd.Parameters.Add(
                    ParamNVarChar("@sin", sin, 100));

                cmd.Parameters.Add(
                    ParamInt("@year", billingMonth.Year));

                cmd.Parameters.Add(
                    ParamInt("@month", billingMonth.Month));

                cmd.Parameters.Add(
                    ParamDecimal("@rental", monthlyRental));

                cmd.Parameters.Add(
                    ParamDecimal("@additional", additionalCharge));

                cmd.ExecuteNonQuery();

                billingMonth = billingMonth.AddMonths(1);
            }
        }


        public static int EnsureMonthlyBillingForAll()
        {
            DateTime today = DateTime.Today;
            string currentMonthKey = today.ToString("yyyy-MM");

            using var con = OpenConnection();

            using var cmdGet = new SqlCommand(
                "SELECT [Value] FROM AppSettings " +
                "WHERE [Key] = 'LastMonthlyReset'",
                con);

            string lastReset =
                cmdGet.ExecuteScalar()?.ToString()
                ?? "2000-01";

            if (lastReset == currentMonthKey)
                return 0;

            using var tran = con.BeginTransaction();

            try
            {
                using var cmdInsert = new SqlCommand(@"
            INSERT INTO MonthlyBilling (SIN, BillingYear, BillingMonth, MonthlyRental, AdditionalCharge, Penalty, PaymentStatus)
            SELECT p.SIN, @year, @month, p.MonthlyRental, p.AdditionalCharge, 0, 'Unpaid' FROM Profiling p WHERE p.IsArchived = 0
            AND p.PaymentStatus <> 'Unverified'
            AND TRY_CONVERT(date, p.StartDate, 23) IS NOT NULL
            AND DATEADD(month, 1, DATEFROMPARTS(YEAR(TRY_CONVERT(date, p.StartDate, 23)), MONTH(TRY_CONVERT(date, p.StartDate, 23)), 1)) <= DATEFROMPARTS(@year, @month, 1)
            AND NOT EXISTS
            (SELECT 1 FROM MonthlyBilling mb WHERE mb.SIN = p.SIN AND mb.BillingYear = @year AND mb.BillingMonth = @month
            )",
                    con,
                    tran);

                cmdInsert.Parameters.Add(
                    ParamInt("@year", today.Year));

                cmdInsert.Parameters.Add(
                    ParamInt("@month", today.Month));

                int rows =
                    cmdInsert.ExecuteNonQuery();

                using var cmdUpdate =
                    new SqlCommand(@"
                UPDATE AppSettings SET [Value] = @monthKey WHERE [Key] = 'LastMonthlyReset'",
                    con,
                    tran);

                cmdUpdate.Parameters.Add(
                    ParamNVarChar(
                        "@monthKey",
                        currentMonthKey,
                        20));

                cmdUpdate.ExecuteNonQuery();

                tran.Commit();

                return rows;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ===================== AUDIT TRAIL ===================== //

        public static DataTable GetAuditTrail()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT Id, Action, SIN, UserId, Timestamp, Details
                FROM   AuditTrail
                WHERE  Action NOT IN ('Login', 'Logout')
                ORDER BY Timestamp DESC", con);
            return FillDataTable(cmd);
        }

        public static DataTable GetUserAuditTrail()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    a.Id,
                    a.Action,
                    u.Username AS [Username],
                    a.Timestamp,
                    a.Details
                FROM AuditTrail a
                LEFT JOIN Users u ON a.UserId = u.Id
                WHERE  a.Action IN ('Login', 'Logout')
                ORDER BY a.Timestamp DESC", con);
            return FillDataTable(cmd);
        }

        public static void LogAudit(
            string action, string? sin, long userId, string? details = null)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    INSERT INTO AuditTrail (Action, SIN, UserId, Timestamp, Details)
                    VALUES (@action, @sin, @user, @timestamp, @details)", con);

                cmd.Parameters.Add(ParamNVarChar("@action", action, 255));
                cmd.Parameters.Add(ParamNullableString("@sin", sin, 100));
                cmd.Parameters.Add(ParamInt("@user", (int)userId));
                cmd.Parameters.Add(ParamDateTime("@timestamp", DateTime.Now));

                var detailsParam = new SqlParameter("@details", SqlDbType.NVarChar, -1)
                { Value = (object?)details ?? DBNull.Value };
                cmd.Parameters.Add(detailsParam);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuditTrail] Write failed: {ex.Message}");
            }
        }

        // ===================== MONTHLY REPORT ===================== //

        public static DataTable GetMonthlyReport(int month, int year)
            => GetMonthlyReport(month, year, month, year);

        public static DataTable GetMonthlyReport(
            int fromMonth, int fromYear, int toMonth, int toYear)
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    SIN              AS [SIN],
                    FullName         AS [Full Name],
                    BusinessName     AS [Business Name],
                    BusinessSection  AS [Business Section],
                    MonthlyRental    AS [Monthly Rental],
                    Penalty          AS [Penalty],
                    AdditionalCharge AS [Additional Charge],
                    PaymentStatus    AS [Payment Status]
                FROM Profiling
                WHERE IsArchived = 0
                AND (
                    TRY_CONVERT(DATE, StartDate, 23) IS NOT NULL
                    AND
                    (YEAR(TRY_CONVERT(DATE, StartDate, 23)) * 12 + MONTH(TRY_CONVERT(DATE, StartDate, 23)))
                        BETWEEN (@fromYear * 12 + @fromMonth)
                        AND     (@toYear   * 12 + @toMonth)
                )
                ORDER BY PaymentStatus ASC, FullName ASC", con);

            cmd.Parameters.Add(ParamInt("@fromMonth", fromMonth));
            cmd.Parameters.Add(ParamInt("@fromYear", fromYear));
            cmd.Parameters.Add(ParamInt("@toMonth", toMonth));
            cmd.Parameters.Add(ParamInt("@toYear", toYear));

            return FillDataTable(cmd);
        }

        // ===================== BILLING REPORT ===================== //

        public static List<BillingReportModel> GetProfiles()
        {
            var list = new List<BillingReportModel>();

            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT SIN, FullName, BusinessName, BusinessSection,
                       StallNumber, StallSize, MonthlyRental
                FROM   Profiling", con);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                list.Add(new BillingReportModel
                {
                    SIN = reader["SIN"].ToString()!,
                    FullName = reader["FullName"].ToString()!,
                    BusinessName = reader["BusinessName"].ToString()!,
                    BusinessSection = reader["BusinessSection"].ToString()!,
                    StallNumber = reader["StallNumber"].ToString()!,
                    StallSize = reader["StallSize"].ToString()!,
                    MonthlyRental = Convert.ToDecimal(reader["MonthlyRental"])
                });

            return list;
        }

        // ===================== OR NUMBER ===================== //

        public static bool ORNumberExists(string orNumber)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(
                    "SELECT 1 FROM PaymentHistory WHERE ORNumber = @or", con);
                cmd.Parameters.Add(ParamNVarChar("@or", orNumber.Trim(), 100));
                return cmd.ExecuteScalar() != null;
            }
            catch { return false; }
        }

        // ===================== IMPORT ===================== //

        public static HashSet<string> GetAllSINs()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand("SELECT SIN FROM Profiling", con);
            using var reader = cmd.ExecuteReader();

            var sins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
                sins.Add(reader.GetString(0));
            return sins;
        }
        public static (bool Success, string? ErrorMessage, int RowsImported, List<string> RowErrors)
            ImportProfiling(DataTable dt)
        {
            var requiredColumns = new Dictionary<string, Type>
            {
                { "SIN",             typeof(string)  },
                { "FullName",        typeof(string)  },
                { "BusinessName",    typeof(string)  },
                { "BusinessSection", typeof(string)  },
                { "StallNumber",     typeof(string)  },
                { "StallSize",       typeof(string)  },
                { "MonthlyRental",   typeof(decimal) },
                { "PaymentStatus",   typeof(string)  },
                { "StartDate",       typeof(string)  },
                { "Penalty",         typeof(decimal) },
                { "AdditionalCharge",typeof(decimal) },
                { "IsArchived",      typeof(int)     },
            };

            foreach (var col in requiredColumns.Keys)
                if (!dt.Columns.Contains(col))
                    return (false, $"Missing required column: '{col}'.", 0, new List<string>());

            var rowErrors = new List<string>();
            var cleanTable = dt.Clone();
            int rowNum = 0;

            foreach (DataRow row in dt.Rows)
            {
                rowNum++;
                var cellErrors = new List<string>();

                string sin = row["SIN"]?.ToString()?.Trim() ?? "";
                string fullName = row["FullName"]?.ToString()?.Trim() ?? "";
                string businessName = row["BusinessName"]?.ToString()?.Trim() ?? "";
                string section = row["BusinessSection"]?.ToString()?.Trim() ?? "";
                string stallNo = row["StallNumber"]?.ToString()?.Trim() ?? "";
                string stallSize = row["StallSize"]?.ToString()?.Trim() ?? "";
                string status = row["PaymentStatus"]?.ToString()?.Trim() ?? "Unpaid";
                string startDateRaw = row["StartDate"]?.ToString()?.Trim() ?? "";
                int isArchived = 0;

                if (string.IsNullOrWhiteSpace(sin)) cellErrors.Add("SIN is empty");
                if (string.IsNullOrWhiteSpace(fullName)) cellErrors.Add("FullName is empty");
                if (string.IsNullOrWhiteSpace(businessName)) cellErrors.Add("BusinessName is empty");
                if (string.IsNullOrWhiteSpace(section)) cellErrors.Add("BusinessSection is empty");
                if (string.IsNullOrWhiteSpace(stallNo)) cellErrors.Add("StallNumber is empty");
                if (string.IsNullOrWhiteSpace(stallSize)) cellErrors.Add("StallSize is empty");

                if (!decimal.TryParse(row["MonthlyRental"]?.ToString(), out decimal rental))
                    cellErrors.Add($"MonthlyRental '{row["MonthlyRental"]}' is not a valid decimal");

                decimal penalty = 0, additional = 0;
                if (!decimal.TryParse(row["Penalty"]?.ToString(), out penalty))
                    cellErrors.Add($"Penalty '{row["Penalty"]}' is not a valid decimal");
                if (!decimal.TryParse(row["AdditionalCharge"]?.ToString(), out additional))
                    cellErrors.Add($"AdditionalCharge '{row["AdditionalCharge"]}' is not a valid decimal");

                if (!int.TryParse(row["IsArchived"]?.ToString(), out isArchived))
                    isArchived = 0;

                string normDate = NormaliseDateString(startDateRaw);

                if (cellErrors.Count > 0)
                {
                    rowErrors.Add($"Row {rowNum} (SIN={sin}): {string.Join("; ", cellErrors)}");
                    continue;
                }

                DataRow newRow = cleanTable.NewRow();
                newRow["SIN"] = sin;
                newRow["FullName"] = fullName;
                newRow["BusinessName"] = businessName;
                newRow["BusinessSection"] = section;
                newRow["StallNumber"] = stallNo;
                newRow["StallSize"] = stallSize;
                newRow["MonthlyRental"] = rental;
                newRow["PaymentStatus"] = status;
                newRow["StartDate"] = normDate;
                newRow["Penalty"] = penalty;
                newRow["AdditionalCharge"] = additional;
                newRow["IsArchived"] = isArchived;
                cleanTable.Rows.Add(newRow);
            }

            if (cleanTable.Rows.Count == 0)
                return (false, "No valid rows to import.", 0, rowErrors);

            try
            {
                using var con = OpenConnection();
                using var bulk = new SqlBulkCopy(con)
                {
                    DestinationTableName = "Profiling",
                    BulkCopyTimeout = 600
                };

                string[] columns =
                {
                    "SIN", "FullName", "BusinessName", "BusinessSection",
                    "StallNumber", "StallSize", "MonthlyRental", "PaymentStatus",
                    "StartDate", "Penalty", "AdditionalCharge", "IsArchived"
                };

                foreach (string col in columns)
                    bulk.ColumnMappings.Add(col, col);

                bulk.WriteToServer(cleanTable);
                return (true, null, cleanTable.Rows.Count, rowErrors);
            }
            catch (Exception ex) { return (false, ex.Message, 0, rowErrors); }
        }

        // ===================== SPECIAL VEHICLE IMPORT ===================== //

        public static HashSet<string> GetAllPlateNumbers()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand("SELECT PlateNo FROM VehiclePermits", con);
            using var reader = cmd.ExecuteReader();

            var plates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read())
                plates.Add(reader.GetString(0));
            return plates;
        }
        public static (bool Success, string? ErrorMessage, int RowsImported, List<string> RowErrors)
            ImportVehiclePermits(DataTable dt)
        {
            string[] required = { "VIN", "CompanyName", "DriverName", "PlateNo", "SECRegNo", "DTINumber" };

            foreach (var col in required)
                if (!dt.Columns.Contains(col))
                    return (false, $"Missing required column: '{col}'.", 0, new List<string>());

            var rowErrors = new List<string>();
            var cleanTable = dt.Clone();
            int rowNum = 0;

            foreach (DataRow row in dt.Rows)
            {
                rowNum++;
                string vin = row["VIN"]?.ToString()?.Trim() ?? "";
                string companyName = row["CompanyName"]?.ToString()?.Trim() ?? "";
                string driverName = row["DriverName"]?.ToString()?.Trim() ?? "";
                string plateNo = row["PlateNo"]?.ToString()?.Trim().ToUpper() ?? "";
                string secRegNo = row["SECRegNo"]?.ToString()?.Trim() ?? "";
                string dtiNumber = row["DTINumber"]?.ToString()?.Trim() ?? "";

                var cellErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(vin)) cellErrors.Add("VIN is empty");
                if (string.IsNullOrWhiteSpace(companyName)) cellErrors.Add("CompanyName is empty");
                if (string.IsNullOrWhiteSpace(plateNo)) cellErrors.Add("PlateNo is empty");

                if (cellErrors.Count > 0)
                {
                    rowErrors.Add($"Row {rowNum} (VIN={vin}): {string.Join("; ", cellErrors)}");
                    continue;
                }

                DataRow newRow = cleanTable.NewRow();
                newRow["VIN"] = vin;
                newRow["CompanyName"] = companyName;
                newRow["DriverName"] = driverName;
                newRow["PlateNo"] = plateNo;
                newRow["SECRegNo"] = secRegNo;
                newRow["DTINumber"] = dtiNumber;
                cleanTable.Rows.Add(newRow);
            }

            if (cleanTable.Rows.Count == 0)
                return (false, "No valid rows to import.", 0, rowErrors);

            try
            {
                using var con = OpenConnection();
                using var bulk = new SqlBulkCopy(con)
                {
                    DestinationTableName = "VehiclePermits",
                    BulkCopyTimeout = 600
                };

                string[] columns = { "VIN", "CompanyName", "DriverName", "PlateNo", "SECRegNo", "DTINumber" };
                foreach (string col in columns)
                    bulk.ColumnMappings.Add(col, col);

                bulk.WriteToServer(cleanTable);
                return (true, null, cleanTable.Rows.Count, rowErrors);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, 0, rowErrors);
            }
        }

        // ===================== SPECIAL VEHICLE PERMIT ===================== //

        public static DataTable GetAllVehiclePermits()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    VIN                             AS [VIN],
                    CompanyName                     AS [Company Name],
                    DriverName                      AS [Driver Name],
                    PlateNo                         AS [Plate No],
                    SECRegNo                        AS [SEC Reg No],
                    DTINumber                       AS [DTI Number],
                    PermitStatus                    AS [Permit Status],
                    PermitYear                      AS [Permit Year],
                    FORMAT(DateAdded, 'MM/dd/yyyy') AS [Date Added]
                FROM  VehiclePermits
                WHERE IsArchived = 0
                ORDER BY VIN ASC", con);
            return FillDataTable(cmd);
        }

        public static DataTable GetArchivedVehiclePermits()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    VIN                             AS [VIN],
                    CompanyName                     AS [Company Name],
                    DriverName                      AS [Driver Name],
                    PlateNo                         AS [Plate No],
                    SECRegNo                        AS [SEC Reg No],
                    DTINumber                       AS [DTI Number],
                    PermitStatus                    AS [Permit Status],
                    PermitYear                      AS [Permit Year],
                    FORMAT(DateAdded, 'MM/dd/yyyy') AS [Date Added]
                FROM  VehiclePermits
                WHERE IsArchived = 1
                ORDER BY VIN DESC", con);
            return FillDataTable(cmd);
        }

        public static string GenerateUniqueVIN()
        {
            string year = DateTime.Now.Year.ToString();
            int nextNumber = 1;

            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT MAX(CAST(RIGHT(VIN, 4) AS INT))
                FROM   VehiclePermits
                WHERE  VIN LIKE @pattern", con);
            cmd.Parameters.Add(ParamNVarChar("@pattern", $"VIN-{year}-%", 50));

            var result = cmd.ExecuteScalar();
            if (result != DBNull.Value && result != null)
                nextNumber = Convert.ToInt32(result) + 1;

            return $"VIN-{year}-{nextNumber:D4}";
        }

        public static (bool Success, string? ErrorMessage) AddVehiclePermit(
            string vin, string companyName, string driverName,
            string plateNo, string secRegNo, string dtiNumber)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return (false, "Company name is required.");
            if (string.IsNullOrWhiteSpace(plateNo))
                return (false, "Plate number is required.");
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    INSERT INTO VehiclePermits
                        (VIN, CompanyName, DriverName, PlateNo, SECRegNo, DTINumber)
                    VALUES
                        (@vin, @company, @driver, @plate, @sec, @dti)", con);
                cmd.Parameters.Add(ParamNVarChar("@vin", vin, 100));
                cmd.Parameters.Add(ParamNVarChar("@company", companyName.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@driver", driverName.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@plate", plateNo.Trim().ToUpper(), 100));
                cmd.Parameters.Add(ParamNVarChar("@sec", secRegNo.Trim(), 100));
                cmd.Parameters.Add(ParamNVarChar("@dti", dtiNumber.Trim(), 100));
                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (SqlException ex) when (ex.Number == 2627)
            { return (false, "A vehicle with this plate number already exists."); }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static (bool Success, string? ErrorMessage) UpdateVehiclePermit(
            string vin, string companyName, string driverName,
            string plateNo, string secRegNo, string dtiNumber)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    UPDATE VehiclePermits
                    SET CompanyName = @company,
                        DriverName  = @driver,
                        PlateNo     = @plate,
                        SECRegNo    = @sec,
                        DTINumber   = @dti
                    WHERE VIN = @vin", con);
                cmd.Parameters.Add(ParamNVarChar("@company", companyName.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@driver", driverName.Trim(), 255));
                cmd.Parameters.Add(ParamNVarChar("@plate", plateNo.Trim().ToUpper(), 100));
                cmd.Parameters.Add(ParamNVarChar("@sec", secRegNo.Trim(), 100));
                cmd.Parameters.Add(ParamNVarChar("@dti", dtiNumber.Trim(), 100));
                cmd.Parameters.Add(ParamNVarChar("@vin", vin, 100));
                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (SqlException ex) when (ex.Number == 2627)
            { return (false, "Another vehicle with that plate number already exists."); }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static (bool Success, string? ErrorMessage) ArchiveVehiclePermit(string vin)
            => SetVehiclePermitArchiveStatus(vin, true);

        public static (bool Success, string? ErrorMessage) RestoreVehiclePermit(string vin)
            => SetVehiclePermitArchiveStatus(vin, false);

        private static (bool Success, string? ErrorMessage) SetVehiclePermitArchiveStatus(
            string vin, bool archived)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(@"
                    UPDATE VehiclePermits
                    SET IsArchived = @archived
                    WHERE VIN = @vin", con);
                cmd.Parameters.Add(ParamInt("@archived", archived ? 1 : 0));
                cmd.Parameters.Add(ParamNVarChar("@vin", vin, 100));
                cmd.ExecuteNonQuery();
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        // ===================== VEHICLE PERMIT PAYMENT ===================== //

        public static (bool Success, string? ErrorMessage) PayVehiclePermit(
            string vin, string orNumber, decimal amountPaid,
            int permitYear, long recordedBy)
        {
            if (string.IsNullOrWhiteSpace(orNumber))
                return (false, "OR Number cannot be empty.");
            try
            {
                using var con = OpenConnection();
                using var tran = con.BeginTransaction();
                try
                {
                    using var cmdUpdate = new SqlCommand(@"
                        UPDATE VehiclePermits
                        SET PermitStatus = 'Paid',
                            PermitYear   = @year
                        WHERE VIN = @vin", con, tran);
                    cmdUpdate.Parameters.Add(ParamInt("@year", permitYear));
                    cmdUpdate.Parameters.Add(ParamNVarChar("@vin", vin, 100));
                    cmdUpdate.ExecuteNonQuery();

                    using var cmdHistory = new SqlCommand(@"
                        INSERT INTO VehiclePermitHistory
                            (VIN, ORNumber, AmountPaid, PermitYear, DatePaid, RecordedBy)
                        VALUES
                            (@vin, @or, @amount, @year, @date, @recordedBy)", con, tran);
                    cmdHistory.Parameters.Add(ParamNVarChar("@vin", vin, 100));
                    cmdHistory.Parameters.Add(ParamNVarChar("@or", orNumber.Trim(), 100));
                    cmdHistory.Parameters.Add(ParamDecimal("@amount", amountPaid));
                    cmdHistory.Parameters.Add(ParamInt("@year", permitYear));
                    cmdHistory.Parameters.Add(ParamDateTime("@date", DateTime.Now));
                    cmdHistory.Parameters.Add(ParamInt("@recordedBy", (int)recordedBy));
                    cmdHistory.ExecuteNonQuery();

                    tran.Commit();
                    return (true, null);
                }
                catch (SqlException ex) when (ex.Number == 2627)
                {
                    tran.Rollback();
                    return (false, "OR Number already exists.");
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return (false, ex.Message);
                }
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static DataTable GetVehiclePermitHistory(string vin)
        {
            using var con = OpenConnection();

            // Older databases used VehicleId -> Vehicles.Id; newer databases
            // store the special permit VIN directly in this history table.
            using var schemaCmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM sys.columns
                WHERE object_id = OBJECT_ID('VehiclePermitHistory')
                  AND name = 'VIN'", con);
            bool historyHasVin = (int)schemaCmd.ExecuteScalar()! > 0;

            string relation = historyHasVin
                ? "WHERE ph.VIN = @vin"
                : "INNER JOIN Vehicles v ON v.Id = ph.VehicleId WHERE v.VIN = @vin";

            using var cmd = new SqlCommand($@"
                SELECT
                    ph.ORNumber                              AS [OR Number],
                    ph.AmountPaid                            AS [Amount Paid],
                    ph.PermitYear                            AS [Permit Year],
                    FORMAT(ph.DatePaid, 'MM/dd/yyyy HH:mm') AS [Date Paid],
                    u.FullName                               AS [Recorded By]
                FROM VehiclePermitHistory ph
                LEFT JOIN Users u ON ph.RecordedBy = u.Id
                {relation}
                ORDER BY ph.DatePaid DESC", con);
            cmd.Parameters.Add(ParamNVarChar("@vin", vin, 100));
            return FillDataTable(cmd);
        }

        public static string? GetVehiclePermitFeeDraft(string vin, int permitYear)
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT DetailsJson
                FROM dbo.VehiclePermitFeeDrafts
                WHERE VIN = @vin AND PermitYear = @year", con);
            cmd.Parameters.Add(ParamNVarChar("@vin", vin, 100));
            cmd.Parameters.Add(ParamInt("@year", permitYear));
            return cmd.ExecuteScalar() as string;
        }

        public static (bool Success, string? ErrorMessage) SaveVehiclePermitFeeDraft(
            string vin, int permitYear, string detailsJson, decimal grandTotal)
        {
            if (string.IsNullOrWhiteSpace(vin) || permitYear < 1900 || grandTotal < 0)
                return (false, "Invalid vehicle, permit year, or total.");

            try
            {
                using var con = OpenConnection();
                using var tran = con.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    using var cmd = new SqlCommand(@"
                        UPDATE dbo.VehiclePermitFeeDrafts
                        SET DetailsJson = @json,
                            GrandTotal = @total,
                            UpdatedAt = SYSUTCDATETIME()
                        WHERE VIN = @vin AND PermitYear = @year;
                        IF @@ROWCOUNT = 0
                            INSERT INTO dbo.VehiclePermitFeeDrafts
                                (VIN, PermitYear, DetailsJson, GrandTotal)
                            VALUES (@vin, @year, @json, @total);", con, tran);
                    cmd.Parameters.Add(ParamNVarChar("@vin", vin, 100));
                    cmd.Parameters.Add(ParamInt("@year", permitYear));
                    cmd.Parameters.Add(ParamNVarChar("@json", detailsJson, -1));
                    cmd.Parameters.Add(ParamDecimal("@total", grandTotal));
                    cmd.ExecuteNonQuery();
                    tran.Commit();
                    return (true, null);
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static bool VehicleORNumberExists(string orNumber)
        {
            try
            {
                using var con = OpenConnection();
                using var cmd = new SqlCommand(
                    "SELECT 1 FROM VehiclePermitHistory WHERE ORNumber = @or", con);
                cmd.Parameters.Add(ParamNVarChar("@or", orNumber.Trim(), 100));
                return cmd.ExecuteScalar() != null;
            }
            catch { return false; }
        }

        public static (int Total, int Paid, int Unpaid) GetVehiclePermitSummary()
        {
            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                SELECT
                    COUNT(*)                                                              AS Total,
                    ISNULL(SUM(CASE WHEN PermitStatus = 'Paid'   THEN 1 ELSE 0 END), 0) AS Paid,
                    ISNULL(SUM(CASE WHEN PermitStatus = 'Unpaid' THEN 1 ELSE 0 END), 0) AS Unpaid
                FROM VehiclePermits
                WHERE IsArchived = 0", con);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (
                    Convert.ToInt32(reader["Total"]),
                    Convert.ToInt32(reader["Paid"]),
                    Convert.ToInt32(reader["Unpaid"]));
            return (0, 0, 0);
        }

        public static int ResetAnnualVehiclePermitStatus()
        {
            if (DateTime.Today.Month != 1 || DateTime.Today.Day != 1) return 0;

            using var con = OpenConnection();
            using var cmd = new SqlCommand(@"
                UPDATE VehiclePermits
                SET    PermitStatus = 'Unpaid'
                WHERE  PermitStatus = 'Paid'
                AND    IsArchived   = 0", con);
            return cmd.ExecuteNonQuery();
        }

        // ===================== PASSWORD HASHING ===================== //

        private const int Pbkdf2Iterations = 260_000;
        private const int Pbkdf2SaltBytes = 16;
        private const int Pbkdf2HashBytes = 32;

        private static string HashPasswordPbkdf2(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(Pbkdf2SaltBytes);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Pbkdf2Iterations,
                HashAlgorithmName.SHA256, Pbkdf2HashBytes);

            return $"pbkdf2:{Pbkdf2Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPasswordPbkdf2(string password, string stored)
        {
            string[] parts = stored.Split(':');
            if (parts.Length != 4 || parts[0] != "pbkdf2") return false;

            if (!int.TryParse(parts[1], out int iterations)) return false;
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedHash = Convert.FromBase64String(parts[3]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iterations,
                HashAlgorithmName.SHA256, expectedHash.Length);

            return CryptographicEquals(actualHash, expectedHash);
        }

        private static bool IsPbkdf2Hash(string stored)
            => stored.StartsWith("pbkdf2:", StringComparison.Ordinal);


        private static string HashPasswordSha256(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
        private static bool CryptographicEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static bool CryptographicEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}