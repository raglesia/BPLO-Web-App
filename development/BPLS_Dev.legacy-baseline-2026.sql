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
COMMIT TRANSACTION;
SELECT COUNT(*) AS LegacyProfiles FROM dbo.Profiling WHERE IsLegacyBaseline=1;
SELECT COUNT(*) AS UnpaidDecemberBills FROM dbo.MonthlyBilling
WHERE BillingYear=2026 AND BillingMonth=12 AND PaymentStatus='Unpaid';
