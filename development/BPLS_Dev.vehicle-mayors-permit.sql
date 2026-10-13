SET XACT_ABORT ON;
IF DB_NAME() <> 'BPLS_Dev' THROW 51000, 'Development database required.', 1;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.VehicleMayorPermitCounters','U') IS NULL
 CREATE TABLE dbo.VehicleMayorPermitCounters(IssueYear int NOT NULL PRIMARY KEY,LastNumber int NOT NULL CHECK(LastNumber>0));
IF OBJECT_ID('dbo.VehicleMayorPermits','U') IS NULL
 CREATE TABLE dbo.VehicleMayorPermits(PaymentHistoryId int NOT NULL PRIMARY KEY REFERENCES dbo.VehiclePermitHistory(Id),IssueYear int NOT NULL,SequenceNumber int NOT NULL CHECK(SequenceNumber>0),PreparedAt datetime2 NOT NULL,PreparedBy int NOT NULL REFERENCES dbo.Users(Id),CONSTRAINT UQ_VehicleMayorPermitNumber UNIQUE(IssueYear,SequenceNumber));
COMMIT;
