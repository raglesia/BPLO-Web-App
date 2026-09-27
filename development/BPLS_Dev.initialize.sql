:ON ERROR EXIT
-- Snapshot generated from Database.Initialize() for disposable BPLS_Dev only.
IF DB_NAME() <> 'BPLS_Dev' THROW 50000, 'Run this script only in BPLS_Dev', 1;
GO
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                CREATE TABLE Users (
                    Id       INT           IDENTITY(1,1) PRIMARY KEY,
                    FullName NVARCHAR(255) NOT NULL,
                    Username NVARCHAR(255) NOT NULL UNIQUE,
                    Position NVARCHAR(255) NOT NULL DEFAULT '',
                    Password NVARCHAR(512) NOT NULL,
                    Created  DATETIME      NOT NULL DEFAULT GETDATE()
                );
GO
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
                );
GO
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditTrail' AND xtype='U')
                CREATE TABLE AuditTrail (
                    Id        INT           IDENTITY(1,1) PRIMARY KEY,
                    Action    NVARCHAR(255) NOT NULL,
                    SIN       NVARCHAR(100),
                    UserId    INT           NOT NULL,
                    Timestamp DATETIME      NOT NULL DEFAULT GETDATE(),
                    Details   NVARCHAR(MAX)
                );
GO
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
                );
GO
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RentalRates' AND xtype='U')
                CREATE TABLE RentalRates (
                    Section    NVARCHAR(255) PRIMARY KEY,
                    RatePerSqm DECIMAL(18,2) NOT NULL DEFAULT 0,
                    FlatRate   DECIMAL(18,2) NOT NULL DEFAULT 0,
                    RateType   NVARCHAR(50)  NOT NULL DEFAULT 'PerSqm'
                );
GO
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
                END
GO
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
                );
GO
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
                );
GO
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppSettings' AND xtype='U')
                CREATE TABLE AppSettings (
                    [Key]   NVARCHAR(100) PRIMARY KEY,
                    [Value] NVARCHAR(255) NOT NULL
                );
GO
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = 'LastMonthlyReset')
                INSERT INTO AppSettings ([Key], [Value]) VALUES ('LastMonthlyReset', '2000-01');
GO
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
                );
GO
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
                );
GO
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
                );
GO
-- Profiling.StartDate
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'StartDate')        ALTER TABLE Profiling ADD StartDate        NVARCHAR(50)  DEFAULT '';
GO
-- Profiling.Penalty
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'Penalty')          ALTER TABLE Profiling ADD Penalty          DECIMAL(18,2) DEFAULT 0;
GO
-- Profiling.AdditionalCharge
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'AdditionalCharge') ALTER TABLE Profiling ADD AdditionalCharge DECIMAL(18,2) DEFAULT 0;
GO
-- Users.Position
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users')     AND name = 'Position')         ALTER TABLE Users     ADD Position         NVARCHAR(255) NOT NULL DEFAULT '';
GO
-- Profiling.IsArchived
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Profiling') AND name = 'IsArchived')       ALTER TABLE Profiling ADD IsArchived        INT           DEFAULT 0;
GO
-- VehiclePermits.PermitStatus
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('VehiclePermits') AND name = 'PermitStatus') ALTER TABLE VehiclePermits ADD PermitStatus NVARCHAR(50) NOT NULL DEFAULT 'Unpaid';
GO
-- VehiclePermits.PermitYear
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('VehiclePermits') AND name = 'PermitYear')   ALTER TABLE VehiclePermits ADD PermitYear   INT          NOT NULL DEFAULT 0;
GO
