:ON ERROR EXIT
SET NOCOUNT ON;
IF DB_NAME()<>'BPLS_Dev' THROW 50000,'BPLS_Dev only.',1;
SELECT 'StallRental' PaymentType,Id PaymentReferenceId,ORNumber,RecordedBy,DatePaid
INTO #Payments FROM PaymentHistory
UNION ALL SELECT 'VehiclePermit',Id,ORNumber,RecordedBy,DatePaid FROM VehiclePermitHistory;
SELECT t.PaymentType,COUNT(p.PaymentReferenceId) ORCount FROM(VALUES('StallRental'),('VehiclePermit'))t(PaymentType)
LEFT JOIN #Payments p ON p.PaymentType=t.PaymentType GROUP BY t.PaymentType;
SELECT t.PaymentType,COUNT(d.ORNumber) DuplicateGroups FROM(VALUES('StallRental'),('VehiclePermit'))t(PaymentType)
LEFT JOIN(SELECT PaymentType,dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS ORNumber
FROM #Payments GROUP BY PaymentType,dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1)d
ON d.PaymentType=t.PaymentType GROUP BY t.PaymentType;
SELECT COUNT(*) GlobalDuplicateGroups FROM(SELECT dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS ORNumber
FROM #Payments GROUP BY dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1)d;
SELECT COUNT(*) RegistryRows FROM ReceiptRegistry;
IF EXISTS(SELECT 1 FROM #Payments p LEFT JOIN ReceiptRegistry r ON r.PaymentType=p.PaymentType AND r.PaymentReferenceId=p.PaymentReferenceId
WHERE r.Id IS NULL OR r.ORNumber<>dbo.NormalizeOR(p.ORNumber) COLLATE Latin1_General_100_CI_AS
OR r.RecordedByUserId<>p.RecordedBy OR r.RecordedAt<>p.DatePaid)
OR EXISTS(SELECT 1 FROM ReceiptRegistry r LEFT JOIN #Payments p ON p.PaymentType=r.PaymentType AND p.PaymentReferenceId=r.PaymentReferenceId
WHERE p.PaymentReferenceId IS NULL)
THROW 50001,'Missing, orphan or mismatched receipt registry row.',1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('ReceiptRegistry') AND name='UQ_ReceiptRegistry_ORNumber' AND is_unique=1 AND is_disabled=0)
THROW 50002,'Global unique index missing or disabled.',1;
IF (SELECT COUNT(*) FROM sys.triggers WHERE name IN('TR_PaymentHistory_ReceiptRegistry','TR_VehiclePermitHistory_ReceiptRegistry') AND is_disabled=0)<>2
THROW 50003,'Receipt synchronization triggers missing or disabled.',1;
PRINT 'PASS: every payment has one matching receipt; no orphan registry rows; global unique index and triggers enabled.';
