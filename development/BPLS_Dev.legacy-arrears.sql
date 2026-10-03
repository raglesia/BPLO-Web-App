:ON ERROR EXIT
IF DB_NAME() <> 'BPLS_Dev' THROW 50000, 'Run only in BPLS_Dev', 1;
GO
IF COL_LENGTH('dbo.Profiling', 'IsLegacyBaseline') IS NULL
    ALTER TABLE dbo.Profiling ADD IsLegacyBaseline BIT NOT NULL
        CONSTRAINT DF_Profiling_IsLegacyBaseline DEFAULT (0);
GO
IF OBJECT_ID('dbo.StallOwnerArrears', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StallOwnerArrears (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StallOwnerArrears PRIMARY KEY,
        SIN NVARCHAR(100) NOT NULL,
        BillingYear INT NOT NULL,
        BillingMonth INT NOT NULL,
        BaseRent DECIMAL(18,2) NOT NULL,
        AdditionalCharge DECIMAL(18,2) NOT NULL CONSTRAINT DF_StallOwnerArrears_Additional DEFAULT (0),
        PenaltyAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_StallOwnerArrears_Penalty DEFAULT (0),
        IsPaid BIT NOT NULL CONSTRAINT DF_StallOwnerArrears_IsPaid DEFAULT (0),
        PaidAt DATETIME NULL,
        VerifiedByUserId INT NOT NULL,
        VerifiedAt DATETIME2 NOT NULL CONSTRAINT DF_StallOwnerArrears_VerifiedAt DEFAULT (SYSUTCDATETIME()),
        TreasuryReference NVARCHAR(500) NOT NULL CONSTRAINT DF_StallOwnerArrears_Reference DEFAULT (''),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_StallOwnerArrears_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_StallOwnerArrears_Profile FOREIGN KEY (SIN) REFERENCES dbo.Profiling(SIN),
        CONSTRAINT FK_StallOwnerArrears_Verifier FOREIGN KEY (VerifiedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_StallOwnerArrears_Month CHECK (BillingMonth BETWEEN 1 AND 12),
        CONSTRAINT CK_StallOwnerArrears_Amounts CHECK (BaseRent >= 0 AND AdditionalCharge >= 0 AND PenaltyAmount >= 0),
        CONSTRAINT UQ_StallOwnerArrears_Period UNIQUE (SIN, BillingYear, BillingMonth)
    );
END;
GO
IF OBJECT_ID('dbo.PaymentHistoryArrears', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentHistoryArrears (
        PaymentHistoryId INT NOT NULL,
        StallOwnerArrearsId INT NOT NULL,
        CONSTRAINT PK_PaymentHistoryArrears PRIMARY KEY (PaymentHistoryId, StallOwnerArrearsId),
        CONSTRAINT UQ_PaymentHistoryArrears_Arrears UNIQUE (StallOwnerArrearsId),
        CONSTRAINT FK_PaymentHistoryArrears_Payment FOREIGN KEY (PaymentHistoryId) REFERENCES dbo.PaymentHistory(Id),
        CONSTRAINT FK_PaymentHistoryArrears_Arrears FOREIGN KEY (StallOwnerArrearsId) REFERENCES dbo.StallOwnerArrears(Id)
    );
END;
GO
-- Controlled baseline: execute separately after reviewing the candidate list.
-- This schema script never changes occupancy dates or existing financial rows.
