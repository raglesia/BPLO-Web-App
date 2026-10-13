:ON ERROR EXIT
-- Run through Initialize-BPLODatabase.ps1; direct SQL execution must fail closed.
IF SESSION_CONTEXT(N'BPLO_ApprovedSchemaTarget') IS NULL OR CONVERT(nvarchar(128),SESSION_CONTEXT(N'BPLO_ApprovedSchemaTarget'))<>DB_NAME()
    THROW 50000, 'Approved schema target context is missing or mismatched.', 1;
GO
SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
GO
CREATE   FUNCTION dbo.NormalizeOR(@value nvarchar(100)) RETURNS nvarchar(100)
    WITH SCHEMABINDING AS BEGIN
        RETURN TRIM(NCHAR(9)+NCHAR(10)+NCHAR(11)+NCHAR(12)+NCHAR(13)+NCHAR(32) FROM @value);
    END
GO
CREATE TABLE dbo.[AppSettings] (
[Key] nvarchar(100) NOT NULL,
[Value] nvarchar(255) NOT NULL);
GO
CREATE TABLE dbo.[AuditTrail] (
[Id] int IDENTITY(1,1) NOT NULL,
[Action] nvarchar(255) NOT NULL,
[SIN] nvarchar(100) NULL,
[UserId] int NOT NULL,
[Timestamp] datetime NOT NULL DEFAULT (getdate()),
[Details] nvarchar(max) NULL);
GO
CREATE TABLE dbo.[MonthlyBilling] (
[Id] int IDENTITY(1,1) NOT NULL,
[SIN] nvarchar(100) NOT NULL,
[BillingYear] int NOT NULL,
[BillingMonth] int NOT NULL,
[MonthlyRental] decimal(18,2) NOT NULL,
[AdditionalCharge] decimal(18,2) NOT NULL DEFAULT ((0)),
[Penalty] decimal(18,2) NOT NULL DEFAULT ((0)),
[PaymentStatus] nvarchar(50) NOT NULL DEFAULT ('Unpaid'),
[ORNumber] nvarchar(100) NULL,
[DatePaid] datetime NULL,
[RecordedBy] int NULL,
[WebRentBasis] nvarchar(20) NULL);
GO
CREATE TABLE dbo.[PaymentHistory] (
[Id] int IDENTITY(1,1) NOT NULL,
[SIN] nvarchar(100) NOT NULL,
[ORNumber] nvarchar(100) NOT NULL,
[AmountPaid] decimal(18,2) NOT NULL,
[Penalty] decimal(18,2) NOT NULL DEFAULT ((0)),
[DatePaid] datetime NOT NULL DEFAULT (getdate()),
[RecordedBy] int NOT NULL);
GO
CREATE TABLE dbo.[PaymentHistoryArrears] (
[PaymentHistoryId] int NOT NULL,
[StallOwnerArrearsId] int NOT NULL);
GO
CREATE TABLE dbo.[PaymentHistoryBilling] (
[PaymentHistoryId] int NOT NULL,
[MonthlyBillingId] int NOT NULL);
GO
CREATE TABLE dbo.[Profiling] (
[SIN] nvarchar(100) NOT NULL,
[FullName] nvarchar(255) NOT NULL,
[BusinessName] nvarchar(255) NOT NULL,
[BusinessSection] nvarchar(255) NOT NULL,
[StallNumber] nvarchar(100) NOT NULL,
[StallSize] nvarchar(100) NOT NULL,
[MonthlyRental] decimal(18,2) NOT NULL,
[PaymentStatus] nvarchar(50) NOT NULL DEFAULT ('Unpaid'),
[StartDate] nvarchar(50) NULL DEFAULT (''),
[Penalty] decimal(18,2) NULL DEFAULT ((0)),
[AdditionalCharge] decimal(18,2) NULL DEFAULT ((0)),
[IsArchived] int NULL DEFAULT ((0)),
[DatePaid] nvarchar(50) NULL DEFAULT (''),
[IsLegacyBaseline] bit NOT NULL DEFAULT ((0)));
GO
CREATE TABLE dbo.[ReceiptRegistry] (
[Id] int IDENTITY(1,1) NOT NULL,
[ORNumber] nvarchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
[PaymentType] varchar(20) NOT NULL,
[PaymentReferenceId] int NULL,
[RecordedByUserId] int NOT NULL,
[RecordedAt] datetime NOT NULL,
[CreatedAt] datetime2(7) NOT NULL DEFAULT (sysutcdatetime()));
GO
CREATE TABLE dbo.[RentalRates] (
[Section] nvarchar(255) NOT NULL,
[RatePerSqm] decimal(18,2) NOT NULL DEFAULT ((0)),
[FlatRate] decimal(18,2) NOT NULL DEFAULT ((0)),
[RateType] nvarchar(50) NOT NULL DEFAULT ('PerSqm'));
GO
CREATE TABLE dbo.[StallOwnerArrears] (
[Id] int IDENTITY(1,1) NOT NULL,
[SIN] nvarchar(100) NOT NULL,
[BillingYear] int NOT NULL,
[BillingMonth] int NOT NULL,
[BaseRent] decimal(18,2) NOT NULL,
[AdditionalCharge] decimal(18,2) NOT NULL DEFAULT ((0)),
[PenaltyAmount] decimal(18,2) NOT NULL DEFAULT ((0)),
[IsPaid] bit NOT NULL DEFAULT ((0)),
[PaidAt] datetime NULL,
[VerifiedByUserId] int NOT NULL,
[VerifiedAt] datetime2(7) NOT NULL DEFAULT (sysutcdatetime()),
[TreasuryReference] nvarchar(500) NOT NULL DEFAULT (''),
[UpdatedAt] datetime2(7) NOT NULL DEFAULT (sysutcdatetime()));
GO
CREATE TABLE dbo.[Users] (
[Id] int IDENTITY(1,1) NOT NULL,
[FullName] nvarchar(255) NOT NULL,
[Username] nvarchar(255) NOT NULL,
[Position] nvarchar(255) NOT NULL DEFAULT (''),
[Password] nvarchar(512) NOT NULL,
[Created] datetime NOT NULL DEFAULT (getdate()));
GO
CREATE TABLE dbo.[VehiclePermitFeeDrafts] (
[VIN] nvarchar(100) NOT NULL,
[PermitYear] int NOT NULL,
[DetailsJson] nvarchar(max) NOT NULL,
[GrandTotal] decimal(18,2) NOT NULL,
[UpdatedAt] datetime2(7) NOT NULL DEFAULT (sysutcdatetime()));
GO
CREATE TABLE dbo.[VehiclePermitHistory] (
[Id] int IDENTITY(1,1) NOT NULL,
[VIN] nvarchar(100) NOT NULL,
[ORNumber] nvarchar(100) NOT NULL,
[AmountPaid] decimal(18,2) NOT NULL,
[PermitYear] int NOT NULL,
[DatePaid] datetime NOT NULL DEFAULT (getdate()),
[RecordedBy] int NOT NULL);
GO
CREATE TABLE dbo.[VehiclePermits] (
[VIN] nvarchar(100) NOT NULL,
[CompanyName] nvarchar(255) NOT NULL,
[DriverName] nvarchar(255) NOT NULL DEFAULT (''),
[PlateNo] nvarchar(100) NOT NULL,
[SECRegNo] nvarchar(100) NULL DEFAULT (''),
[DTINumber] nvarchar(100) NULL DEFAULT (''),
[IsArchived] int NULL DEFAULT ((0)),
[DateAdded] datetime NULL DEFAULT (getdate()),
[PermitStatus] nvarchar(50) NOT NULL DEFAULT ('Unpaid'),
[PermitYear] int NOT NULL DEFAULT ((0)));
GO
ALTER TABLE dbo.[AppSettings] ADD CONSTRAINT [PK__AppSetti__C41E0288F33080EE] PRIMARY KEY CLUSTERED ([Key] ASC);
GO
ALTER TABLE dbo.[AuditTrail] ADD CONSTRAINT [PK__AuditTra__3214EC07695C7BBB] PRIMARY KEY CLUSTERED ([Id] ASC);
GO
ALTER TABLE dbo.[MonthlyBilling] ADD CONSTRAINT [PK__MonthlyB__3214EC07FE9369F1] PRIMARY KEY CLUSTERED ([Id] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_MonthlyBilling] ON dbo.[MonthlyBilling] ([SIN] ASC,[BillingYear] ASC,[BillingMonth] ASC);
GO
ALTER TABLE dbo.[PaymentHistory] ADD CONSTRAINT [PK__PaymentH__3214EC079097FF49] PRIMARY KEY CLUSTERED ([Id] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ__PaymentH__E0E8FFF38BB77422] ON dbo.[PaymentHistory] ([ORNumber] ASC);
GO
ALTER TABLE dbo.[PaymentHistoryArrears] ADD CONSTRAINT [PK_PaymentHistoryArrears] PRIMARY KEY CLUSTERED ([PaymentHistoryId] ASC,[StallOwnerArrearsId] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_PaymentHistoryArrears_Arrears] ON dbo.[PaymentHistoryArrears] ([StallOwnerArrearsId] ASC);
GO
ALTER TABLE dbo.[PaymentHistoryBilling] ADD CONSTRAINT [PK__PaymentH__9CB73B042AAC7245] PRIMARY KEY CLUSTERED ([PaymentHistoryId] ASC,[MonthlyBillingId] ASC);
GO
ALTER TABLE dbo.[Profiling] ADD CONSTRAINT [PK__Profilin__CA1959466E1C3BB3] PRIMARY KEY CLUSTERED ([SIN] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ__Profilin__76B574A0E38F2FB0] ON dbo.[Profiling] ([FullName] ASC,[BusinessName] ASC,[StallNumber] ASC);
GO
ALTER TABLE dbo.[ReceiptRegistry] ADD CONSTRAINT [PK_ReceiptRegistry] PRIMARY KEY CLUSTERED ([Id] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_ReceiptRegistry_ORNumber] ON dbo.[ReceiptRegistry] ([ORNumber] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_ReceiptRegistry_Payment] ON dbo.[ReceiptRegistry] ([PaymentType] ASC,[PaymentReferenceId] ASC) WHERE ([PaymentReferenceId] IS NOT NULL);
GO
ALTER TABLE dbo.[RentalRates] ADD CONSTRAINT [PK__RentalRa__70E702BA794C88FA] PRIMARY KEY CLUSTERED ([Section] ASC);
GO
ALTER TABLE dbo.[StallOwnerArrears] ADD CONSTRAINT [PK_StallOwnerArrears] PRIMARY KEY CLUSTERED ([Id] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_StallOwnerArrears_Period] ON dbo.[StallOwnerArrears] ([SIN] ASC,[BillingYear] ASC,[BillingMonth] ASC);
GO
ALTER TABLE dbo.[Users] ADD CONSTRAINT [PK__Users__3214EC076AFF7267] PRIMARY KEY CLUSTERED ([Id] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ__Users__536C85E4A18DC92B] ON dbo.[Users] ([Username] ASC);
GO
ALTER TABLE dbo.[VehiclePermitFeeDrafts] ADD CONSTRAINT [PK_VehiclePermitFeeDrafts] PRIMARY KEY CLUSTERED ([VIN] ASC,[PermitYear] ASC);
GO
ALTER TABLE dbo.[VehiclePermitHistory] ADD CONSTRAINT [PK__VehicleP__3214EC07B2FAD9DA] PRIMARY KEY CLUSTERED ([Id] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ__VehicleP__E0E8FFF332D2CC29] ON dbo.[VehiclePermitHistory] ([ORNumber] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_VehiclePermitHistory_VIN_PermitYear] ON dbo.[VehiclePermitHistory] ([VIN] ASC,[PermitYear] ASC);
GO
ALTER TABLE dbo.[VehiclePermits] ADD CONSTRAINT [PK__VehicleP__C5DF234D7E9F87E3] PRIMARY KEY CLUSTERED ([VIN] ASC);
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ__VehicleP__48227C0CF83EA013] ON dbo.[VehiclePermits] ([PlateNo] ASC);
GO
ALTER TABLE dbo.[ReceiptRegistry] WITH CHECK ADD CONSTRAINT [CK_ReceiptRegistry_Type] CHECK ([PaymentType]='VehiclePermit' OR [PaymentType]='StallRental');
GO
ALTER TABLE dbo.[ReceiptRegistry] WITH CHECK ADD CONSTRAINT [CK_ReceiptRegistry_OR] CHECK (datalength([ORNumber])>(0) AND datalength([ORNumber])=datalength([dbo].[NormalizeOR]([ORNumber])));
GO
ALTER TABLE dbo.[StallOwnerArrears] WITH CHECK ADD CONSTRAINT [CK_StallOwnerArrears_Month] CHECK ([BillingMonth]>=(1) AND [BillingMonth]<=(12));
GO
ALTER TABLE dbo.[StallOwnerArrears] WITH CHECK ADD CONSTRAINT [CK_StallOwnerArrears_Amounts] CHECK ([BaseRent]>=(0) AND [AdditionalCharge]>=(0) AND [PenaltyAmount]>=(0));
GO
ALTER TABLE dbo.[VehiclePermitHistory] WITH CHECK ADD CONSTRAINT [FK__VehiclePerm__VIN__0D7A0286] FOREIGN KEY ([VIN]) REFERENCES dbo.[VehiclePermits] ([VIN]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[VehiclePermitHistory] WITH CHECK ADD CONSTRAINT [FK__VehiclePe__Recor__0E6E26BF] FOREIGN KEY ([RecordedBy]) REFERENCES dbo.[Users] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[VehiclePermitFeeDrafts] WITH CHECK ADD CONSTRAINT [FK_VehiclePermitFeeDrafts_VehiclePermits] FOREIGN KEY ([VIN]) REFERENCES dbo.[VehiclePermits] ([VIN]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[StallOwnerArrears] WITH CHECK ADD CONSTRAINT [FK_StallOwnerArrears_Profile] FOREIGN KEY ([SIN]) REFERENCES dbo.[Profiling] ([SIN]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[StallOwnerArrears] WITH CHECK ADD CONSTRAINT [FK_StallOwnerArrears_Verifier] FOREIGN KEY ([VerifiedByUserId]) REFERENCES dbo.[Users] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[PaymentHistoryArrears] WITH CHECK ADD CONSTRAINT [FK_PaymentHistoryArrears_Payment] FOREIGN KEY ([PaymentHistoryId]) REFERENCES dbo.[PaymentHistory] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[PaymentHistoryArrears] WITH CHECK ADD CONSTRAINT [FK_PaymentHistoryArrears_Arrears] FOREIGN KEY ([StallOwnerArrearsId]) REFERENCES dbo.[StallOwnerArrears] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[PaymentHistory] WITH CHECK ADD CONSTRAINT [FK__PaymentHist__SIN__6FE99F9F] FOREIGN KEY ([SIN]) REFERENCES dbo.[Profiling] ([SIN]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[MonthlyBilling] WITH CHECK ADD CONSTRAINT [FK__MonthlyBill__SIN__7B5B524B] FOREIGN KEY ([SIN]) REFERENCES dbo.[Profiling] ([SIN]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[PaymentHistoryBilling] WITH CHECK ADD CONSTRAINT [FK__PaymentHi__Payme__7E37BEF6] FOREIGN KEY ([PaymentHistoryId]) REFERENCES dbo.[PaymentHistory] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
ALTER TABLE dbo.[PaymentHistoryBilling] WITH CHECK ADD CONSTRAINT [FK__PaymentHi__Month__7F2BE32F] FOREIGN KEY ([MonthlyBillingId]) REFERENCES dbo.[MonthlyBilling] ([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION;
GO
CREATE   TRIGGER dbo.TR_PaymentHistory_ReceiptRegistry ON dbo.[PaymentHistory]
        AFTER INSERT,UPDATE,DELETE AS
        BEGIN
          SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
             WHERE i.ORNumber COLLATE Latin1_General_100_BIN2<>d.ORNumber COLLATE Latin1_General_100_BIN2
             OR DATALENGTH(i.ORNumber)<>DATALENGTH(d.ORNumber))
             THROW 51002,'Recorded OR numbers cannot be changed.',1;
          DELETE r FROM dbo.ReceiptRegistry r JOIN deleted d ON r.PaymentReferenceId=d.Id
             AND r.PaymentType='StallRental' WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id);
          UPDATE r SET PaymentReferenceId=i.Id FROM dbo.ReceiptRegistry r JOIN inserted i
             ON r.ORNumber=dbo.NormalizeOR(i.ORNumber) COLLATE Latin1_General_100_CI_AS
             WHERE r.PaymentType='StallRental' AND r.PaymentReferenceId IS NULL;
          INSERT INTO dbo.ReceiptRegistry(ORNumber,PaymentType,PaymentReferenceId,RecordedByUserId,RecordedAt)
             SELECT dbo.NormalizeOR(i.ORNumber),'StallRental',i.Id,i.RecordedBy,i.DatePaid FROM inserted i
             WHERE NOT EXISTS(SELECT 1 FROM dbo.ReceiptRegistry r
                WHERE r.PaymentType='StallRental' AND r.PaymentReferenceId=i.Id);
        END
GO
CREATE   TRIGGER dbo.TR_VehiclePermitHistory_ReceiptRegistry ON dbo.[VehiclePermitHistory]
        AFTER INSERT,UPDATE,DELETE AS
        BEGIN
          SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
             WHERE i.ORNumber COLLATE Latin1_General_100_BIN2<>d.ORNumber COLLATE Latin1_General_100_BIN2
             OR DATALENGTH(i.ORNumber)<>DATALENGTH(d.ORNumber))
             THROW 51002,'Recorded OR numbers cannot be changed.',1;
          DELETE r FROM dbo.ReceiptRegistry r JOIN deleted d ON r.PaymentReferenceId=d.Id
             AND r.PaymentType='VehiclePermit' WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id);
          UPDATE r SET PaymentReferenceId=i.Id FROM dbo.ReceiptRegistry r JOIN inserted i
             ON r.ORNumber=dbo.NormalizeOR(i.ORNumber) COLLATE Latin1_General_100_CI_AS
             WHERE r.PaymentType='VehiclePermit' AND r.PaymentReferenceId IS NULL;
          INSERT INTO dbo.ReceiptRegistry(ORNumber,PaymentType,PaymentReferenceId,RecordedByUserId,RecordedAt)
             SELECT dbo.NormalizeOR(i.ORNumber),'VehiclePermit',i.Id,i.RecordedBy,i.DatePaid FROM inserted i
             WHERE NOT EXISTS(SELECT 1 FROM dbo.ReceiptRegistry r
                WHERE r.PaymentType='VehiclePermit' AND r.PaymentReferenceId=i.Id);
        END



GO
-- Additive release schema. No historical snapshots are inferred or backfilled.
CREATE TABLE dbo.VehicleMayorPermitCounters (
    IssueYear int NOT NULL CONSTRAINT PK_VehicleMayorPermitCounters PRIMARY KEY,
    LastNumber int NOT NULL,
    CONSTRAINT CK_VehicleMayorPermitCounters_Year CHECK (IssueYear BETWEEN 1 AND 9999),
    CONSTRAINT CK_VehicleMayorPermitCounters_Number CHECK (LastNumber > 0));
GO
CREATE TABLE dbo.VehicleMayorPermits (
    PaymentHistoryId int NOT NULL CONSTRAINT PK_VehicleMayorPermits PRIMARY KEY,
    IssueYear int NOT NULL,
    SequenceNumber int NOT NULL,
    PreparedAt datetime2(7) NOT NULL,
    PreparedBy int NOT NULL,
    CONSTRAINT CK_VehicleMayorPermits_Number CHECK (SequenceNumber > 0),
    CONSTRAINT FK_VehicleMayorPermits_Payment FOREIGN KEY (PaymentHistoryId) REFERENCES dbo.VehiclePermitHistory(Id),
    CONSTRAINT FK_VehicleMayorPermits_Preparer FOREIGN KEY (PreparedBy) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_VehicleMayorPermits_Counter FOREIGN KEY (IssueYear) REFERENCES dbo.VehicleMayorPermitCounters(IssueYear),
    CONSTRAINT UQ_VehicleMayorPermitNumber UNIQUE (IssueYear, SequenceNumber));
GO
ALTER TABLE dbo.VehiclePermitHistory ADD CONSTRAINT UQ_VehiclePermitHistory_IdAmount UNIQUE (Id, AmountPaid);
GO
CREATE TABLE dbo.VehiclePaymentAssessmentSnapshots (
    PaymentHistoryId int NOT NULL CONSTRAINT PK_VehiclePaymentAssessmentSnapshots PRIMARY KEY,
    SnapshotVersion int NOT NULL CONSTRAINT DF_VehiclePaymentAssessmentSnapshots_Version DEFAULT (1),
    DetailsJson nvarchar(max) NOT NULL,
    GrandTotal decimal(18,2) NOT NULL,
    CompanyName nvarchar(255) NOT NULL,
    DriverName nvarchar(255) NOT NULL,
    PlateNo nvarchar(100) NOT NULL,
    SECRegNo nvarchar(100) NOT NULL,
    DTINumber nvarchar(100) NOT NULL,
    RecorderName nvarchar(255) NOT NULL,
    RecorderPosition nvarchar(255) NOT NULL,
    CONSTRAINT CK_VehiclePaymentAssessmentSnapshots_Version CHECK (SnapshotVersion = 1),
    CONSTRAINT CK_VehiclePaymentAssessmentSnapshots_Json CHECK (ISJSON(DetailsJson) = 1),
    CONSTRAINT CK_VehiclePaymentAssessmentSnapshots_Total CHECK (GrandTotal > 0),
    CONSTRAINT FK_VehiclePaymentAssessmentSnapshots_PaymentAmount FOREIGN KEY (PaymentHistoryId, GrandTotal)
        REFERENCES dbo.VehiclePermitHistory(Id, AmountPaid));
GO
CREATE TRIGGER dbo.TR_VehiclePaymentAssessmentSnapshots_Immutable ON dbo.VehiclePaymentAssessmentSnapshots
AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51003, 'Recorded payment assessment snapshots cannot be changed or deleted.', 1;
END
GO

