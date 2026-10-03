:ON ERROR EXIT
IF DB_NAME() <> 'BPLS_Dev' THROW 50000, 'Run only in BPLS_Dev', 1;
IF COL_LENGTH('dbo.Profiling', 'IsLegacyBaseline') IS NULL OR
   COL_LENGTH('dbo.MonthlyBilling', 'WebRentBasis') IS NULL OR
   OBJECT_ID('dbo.StallOwnerArrears', 'U') IS NULL
    THROW 50001, 'Apply the BPLS_Dev legacy-arrears schema first', 1;
GO
-- Official 538-row imported deployment cohort. Do not widen this range for future profiles.
-- Four previously prepared regular bills are preserved; no older bills are generated here.
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;
SELECT * INTO #ExistingBills FROM dbo.MonthlyBilling WITH (UPDLOCK,HOLDLOCK);
SELECT * INTO #ExistingPayments FROM dbo.PaymentHistory WITH (UPDLOCK,HOLDLOCK);
SELECT * INTO #ExistingArrears FROM dbo.StallOwnerArrears WITH (UPDLOCK,HOLDLOCK);
IF (SELECT COUNT(*) FROM dbo.Profiling WITH (UPDLOCK, HOLDLOCK)
    WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200') <> 538
    THROW 50002, 'Legacy cohort changed; review exact SIN list', 1;
IF EXISTS (SELECT 1 FROM dbo.PaymentHistory p
    WHERE p.SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200')
    THROW 50003, 'Legacy cohort has payments; do not reset paid history', 1;
IF EXISTS (SELECT 1 FROM dbo.MonthlyBilling b
    WHERE b.SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200' AND b.PaymentStatus <> 'Unpaid')
    THROW 50004, 'Legacy cohort has paid or unknown bill status', 1;
UPDATE dbo.Profiling SET StartDate='2026-12-01', IsLegacyBaseline=1,
    PaymentStatus='Unpaid'
WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200';
INSERT INTO dbo.MonthlyBilling
    (SIN, BillingYear, BillingMonth, MonthlyRental, AdditionalCharge,
     Penalty, PaymentStatus, WebRentBasis)
SELECT p.SIN, 2026, 12, p.MonthlyRental-p.AdditionalCharge,
       p.AdditionalCharge, 0, 'Unpaid', 'BaseOnly'
FROM dbo.Profiling p
WHERE p.SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200'
  AND p.IsLegacyBaseline=1 AND p.IsArchived=0
  AND p.MonthlyRental>=p.AdditionalCharge
  AND NOT EXISTS (SELECT 1 FROM dbo.MonthlyBilling b WITH (UPDLOCK, HOLDLOCK)
      WHERE b.SIN=p.SIN AND b.BillingYear=2026 AND b.BillingMonth=12);
IF (SELECT COUNT(*) FROM dbo.MonthlyBilling b
    WHERE b.SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200'
      AND b.BillingYear=2026 AND b.BillingMonth=12 AND b.PaymentStatus='Unpaid') <> 538
    THROW 50005, 'December baseline bill count is not 538', 1;
IF (SELECT COUNT(*) FROM dbo.Profiling WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200' AND StartDate='2026-12-01' AND IsLegacyBaseline=1 AND PaymentStatus='Unpaid')<>538 THROW 50010,'Profile initialization verification failed',1;
IF EXISTS(SELECT SIN FROM dbo.MonthlyBilling WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200' AND BillingYear=2026 AND BillingMonth=12 GROUP BY SIN HAVING COUNT(*)<>1) THROW 50011,'December duplicate verification failed',1;
IF EXISTS(SELECT SIN,BillingYear,BillingMonth FROM dbo.MonthlyBilling GROUP BY SIN,BillingYear,BillingMonth HAVING COUNT(*)>1) THROW 50012,'Duplicate billing periods exist',1;
IF EXISTS(SELECT * FROM #ExistingBills EXCEPT SELECT * FROM dbo.MonthlyBilling) THROW 50013,'An existing bill changed',1;
IF EXISTS(SELECT * FROM #ExistingPayments EXCEPT SELECT * FROM dbo.PaymentHistory) OR EXISTS(SELECT * FROM dbo.PaymentHistory EXCEPT SELECT * FROM #ExistingPayments) THROW 50014,'Payments or collection source changed',1;
IF EXISTS(SELECT * FROM #ExistingArrears EXCEPT SELECT * FROM dbo.StallOwnerArrears) OR EXISTS(SELECT * FROM dbo.StallOwnerArrears EXCEPT SELECT * FROM #ExistingArrears) THROW 50015,'Arrears changed',1;
IF (SELECT COUNT(*) FROM dbo.MonthlyBilling b WHERE NOT EXISTS(SELECT 1 FROM #ExistingBills old WHERE old.Id=b.Id))<>538 THROW 50016,'Unexpected new bill count',1;
IF EXISTS(SELECT 1 FROM dbo.MonthlyBilling b WHERE NOT EXISTS(SELECT 1 FROM #ExistingBills old WHERE old.Id=b.Id) AND (b.SIN NOT BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200' OR b.BillingYear<>2026 OR b.BillingMonth<>12 OR b.PaymentStatus<>'Unpaid' OR b.ORNumber IS NOT NULL OR b.DatePaid IS NOT NULL)) THROW 50017,'Unexpected generated period or payment fields',1;
SELECT COUNT(*) VerifiedLegacyProfiles FROM dbo.Profiling WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200' AND StartDate='2026-12-01' AND IsLegacyBaseline=1 AND PaymentStatus='Unpaid';
SELECT COUNT(*) VerifiedUnpaidDecemberBills FROM dbo.MonthlyBilling WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200' AND BillingYear=2026 AND BillingMonth=12 AND PaymentStatus='Unpaid';
SELECT COUNT(*) PreservedEarlierUnpaidBills FROM dbo.MonthlyBilling WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200' AND NOT(BillingYear=2026 AND BillingMonth=12) AND PaymentStatus='Unpaid';
SELECT (SELECT COUNT(*) FROM #ExistingPayments) BeforePaymentCount,COUNT(*) AfterPaymentCount,(SELECT COALESCE(SUM(AmountPaid),0) FROM #ExistingPayments) BeforeCollectionSource,COALESCE(SUM(AmountPaid),0) AfterCollectionSource FROM dbo.PaymentHistory;
COMMIT TRANSACTION;
PRINT 'COMMITTED: all preservation and initialization assertions passed.';
SELECT COUNT(*) AS LegacyProfiles FROM dbo.Profiling WHERE IsLegacyBaseline=1;
SELECT COUNT(*) AS UnpaidDecemberBills FROM dbo.MonthlyBilling
WHERE BillingYear=2026 AND BillingMonth=12 AND PaymentStatus='Unpaid';
