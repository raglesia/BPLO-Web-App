:ON ERROR EXIT
IF DB_NAME() <> 'BPLS_Dev' THROW 50000, 'Run this script only in BPLS_Dev', 1;
GO
-- Existing NULL rows retain their original financial values and unknown/legacy basis.
-- Only newly generated web bills are marked BaseOnly.
IF COL_LENGTH('dbo.MonthlyBilling', 'WebRentBasis') IS NULL
    ALTER TABLE dbo.MonthlyBilling ADD WebRentBasis NVARCHAR(20) NULL;
GO
