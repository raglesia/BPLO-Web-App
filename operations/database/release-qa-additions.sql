-- Additive release schema. No historical snapshots are inferred or backfilled.
:ON ERROR EXIT
IF SESSION_CONTEXT(N'BPLO_ApprovedSchemaTarget') IS NULL OR CONVERT(nvarchar(128),SESSION_CONTEXT(N'BPLO_ApprovedSchemaTarget'))<>DB_NAME()
    THROW 50000, 'Approved schema target context is missing or mismatched.', 1;
GO
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
