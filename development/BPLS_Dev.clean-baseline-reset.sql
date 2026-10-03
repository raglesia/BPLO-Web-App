:ON ERROR EXIT
-- One-time, development-only reset of the exact 538 official imported profiles.
-- The copy-only recovery backup and read-only preflight must precede this script.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
IF DB_NAME()<>'BPLS_Dev' THROW 50000,'Run only in BPLS_Dev',1;
BEGIN TRY
BEGIN TRANSACTION;
-- Serialize profile allocation, financial writes, and migration checks.
SELECT * INTO #ProfilesBefore FROM dbo.Profiling WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #BillsBefore FROM dbo.MonthlyBilling WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #PaymentsBefore FROM dbo.PaymentHistory WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #ArrearsBefore FROM dbo.StallOwnerArrears WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #BillingLinksBefore FROM dbo.PaymentHistoryBilling WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #ArrearsLinksBefore FROM dbo.PaymentHistoryArrears WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #AuditBefore FROM dbo.AuditTrail WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #UsersBefore FROM dbo.Users WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #RatesBefore FROM dbo.RentalRates WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #VehiclesBefore FROM dbo.VehiclePermits WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #VehicleHistoryBefore FROM dbo.VehiclePermitHistory WITH(TABLOCKX,HOLDLOCK);
SELECT * INTO #VehicleDraftsBefore FROM dbo.VehiclePermitFeeDrafts WITH(TABLOCKX,HOLDLOCK);

SELECT ROW_NUMBER() OVER(ORDER BY FullName,SIN) SortNumber,SIN OldSIN,
    CAST('SIN-2026-'+RIGHT('0000'+CAST(ROW_NUMBER() OVER(ORDER BY FullName,SIN) AS varchar(4)),4) AS nvarchar(100)) NewSIN,
    CAST('SIN-RESET-TEMP-'+RIGHT('0000'+CAST(ROW_NUMBER() OVER(ORDER BY FullName,SIN) AS varchar(4)),4) AS nvarchar(100)) TempSIN,
    FullName INTO #Map
FROM #ProfilesBefore WHERE SIN BETWEEN 'SIN-2026-0663' AND 'SIN-2026-1200';
IF (SELECT COUNT(*) FROM #Map)<>538 THROW 50001,'Expected exact 538 official profiles; reset not repeatable',1;
IF EXISTS(SELECT 1 FROM #ProfilesBefore p JOIN #Map m ON p.SIN=m.NewSIN OR p.SIN=m.TempSIN)
    THROW 50002,'Destination or temporary SIN collision',1;
IF EXISTS(SELECT 1 FROM #ProfilesBefore WHERE SIN IN(SELECT OldSIN FROM #Map) AND
    (COALESCE(IsArchived,0)<>0 OR MonthlyRental<COALESCE(AdditionalCharge,0)))
    THROW 50003,'Archived profile or invalid rent; review required',1;
IF EXISTS(SELECT 1 FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id
    WHERE c.name='SIN' AND (s.name<>'dbo' OR t.name NOT IN('Profiling','MonthlyBilling','PaymentHistory','StallOwnerArrears','AuditTrail')))
    THROW 50004,'Unexpected structured SIN dependency',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
    THROW 50005,'Untrusted or disabled constraints; review required',1;
IF EXISTS(SELECT 1 FROM #BillsBefore WHERE SIN IN(SELECT OldSIN FROM #Map) AND
    (BillingYear>2026 OR BillingYear=2026 AND BillingMonth>12))
    THROW 50006,'Future target bills present; scope review required',1;
IF EXISTS(SELECT 1 FROM #BillingLinksBefore l JOIN #PaymentsBefore p ON p.Id=l.PaymentHistoryId JOIN #BillsBefore b ON b.Id=l.MonthlyBillingId
    WHERE p.SIN<>b.SIN AND (p.SIN IN(SELECT OldSIN FROM #Map) OR b.SIN IN(SELECT OldSIN FROM #Map)))
    THROW 50007,'Cross-profile payment link; review required',1;
IF EXISTS(SELECT 1 FROM #ArrearsLinksBefore l JOIN #PaymentsBefore p ON p.Id=l.PaymentHistoryId JOIN #ArrearsBefore a ON a.Id=l.StallOwnerArrearsId
    WHERE p.SIN<>a.SIN AND (p.SIN IN(SELECT OldSIN FROM #Map) OR a.SIN IN(SELECT OldSIN FROM #Map)))
    THROW 50008,'Cross-profile arrears link; review required',1;

SELECT b.* INTO #December FROM #BillsBefore b WHERE b.SIN IN(SELECT OldSIN FROM #Map)
    AND b.BillingYear=2026 AND b.BillingMonth=12
    AND b.Id=(SELECT MIN(b2.Id) FROM #BillsBefore b2 WHERE b2.SIN=b.SIN AND b2.BillingYear=2026 AND b2.BillingMonth=12);
DECLARE @removedArrearsLinks int,@removedBillingLinks int,@removedPayments int,@removedArrears int,@stagedBills int,
    @insertedDecember int,@retainedDecember int=(SELECT COUNT(*) FROM #December),
    @removedOldBills int=(SELECT COUNT(*) FROM #BillsBefore WHERE SIN IN(SELECT OldSIN FROM #Map) AND (BillingYear<2026 OR BillingYear=2026 AND BillingMonth<12)),
    @duplicateDecember int=(SELECT COUNT(*) FROM #BillsBefore WHERE SIN IN(SELECT OldSIN FROM #Map) AND BillingYear=2026 AND BillingMonth=12)-(SELECT COUNT(*) FROM #December),
    @collectionBefore decimal(38,2)=(SELECT COALESCE(SUM(AmountPaid),0) FROM #PaymentsBefore),
    @removedCollection decimal(38,2)=(SELECT COALESCE(SUM(AmountPaid),0) FROM #PaymentsBefore WHERE SIN IN(SELECT OldSIN FROM #Map));

DELETE l FROM dbo.PaymentHistoryArrears l WHERE EXISTS(SELECT 1 FROM #PaymentsBefore p WHERE p.Id=l.PaymentHistoryId AND p.SIN IN(SELECT OldSIN FROM #Map))
    OR EXISTS(SELECT 1 FROM #ArrearsBefore a WHERE a.Id=l.StallOwnerArrearsId AND a.SIN IN(SELECT OldSIN FROM #Map));
SET @removedArrearsLinks=@@ROWCOUNT;
DELETE l FROM dbo.PaymentHistoryBilling l WHERE EXISTS(SELECT 1 FROM #PaymentsBefore p WHERE p.Id=l.PaymentHistoryId AND p.SIN IN(SELECT OldSIN FROM #Map))
    OR EXISTS(SELECT 1 FROM #BillsBefore b WHERE b.Id=l.MonthlyBillingId AND b.SIN IN(SELECT OldSIN FROM #Map));
SET @removedBillingLinks=@@ROWCOUNT;
DELETE FROM dbo.PaymentHistory WHERE SIN IN(SELECT OldSIN FROM #Map);
SET @removedPayments=@@ROWCOUNT;
DELETE FROM dbo.StallOwnerArrears WHERE SIN IN(SELECT OldSIN FROM #Map);
SET @removedArrears=@@ROWCOUNT;
-- Stage and restore December rows with identical IDs. No FK is disabled and no
-- unrelated row changes. Older rows and duplicate December rows are discarded.
DELETE FROM dbo.MonthlyBilling WHERE SIN IN(SELECT OldSIN FROM #Map);
SET @stagedBills=@@ROWCOUNT;
UPDATE p SET SIN=m.TempSIN,StartDate='2026-12-01',IsLegacyBaseline=1,
    PaymentStatus='Unpaid',Penalty=0,DatePaid=NULL
FROM dbo.Profiling p JOIN #Map m ON m.OldSIN=p.SIN;
UPDATE a SET SIN=m.TempSIN FROM dbo.AuditTrail a JOIN #Map m ON m.OldSIN=a.SIN;
UPDATE p SET SIN=m.NewSIN FROM dbo.Profiling p JOIN #Map m ON m.TempSIN=p.SIN;
UPDATE a SET SIN=m.NewSIN FROM dbo.AuditTrail a JOIN #Map m ON m.TempSIN=a.SIN;

SET IDENTITY_INSERT dbo.MonthlyBilling ON;
INSERT INTO dbo.MonthlyBilling(Id,SIN,BillingYear,BillingMonth,MonthlyRental,AdditionalCharge,Penalty,PaymentStatus,ORNumber,DatePaid,RecordedBy,WebRentBasis)
SELECT d.Id,m.NewSIN,2026,12,p.MonthlyRental-COALESCE(p.AdditionalCharge,0),COALESCE(p.AdditionalCharge,0),0,'Unpaid',NULL,NULL,NULL,'BaseOnly'
FROM #December d JOIN #Map m ON m.OldSIN=d.SIN JOIN dbo.Profiling p ON p.SIN=m.NewSIN;
SET IDENTITY_INSERT dbo.MonthlyBilling OFF;
INSERT INTO dbo.MonthlyBilling(SIN,BillingYear,BillingMonth,MonthlyRental,AdditionalCharge,Penalty,PaymentStatus,WebRentBasis)
SELECT p.SIN,2026,12,p.MonthlyRental-COALESCE(p.AdditionalCharge,0),COALESCE(p.AdditionalCharge,0),0,'Unpaid','BaseOnly'
FROM dbo.Profiling p JOIN #Map m ON m.NewSIN=p.SIN WHERE NOT EXISTS(SELECT 1 FROM dbo.MonthlyBilling b WHERE b.SIN=p.SIN AND b.BillingYear=2026 AND b.BillingMonth=12);
SET @insertedDecember=@@ROWCOUNT;

IF (SELECT COUNT(*) FROM dbo.Profiling p JOIN #Map m ON m.NewSIN=p.SIN WHERE p.StartDate='2026-12-01' AND p.IsLegacyBaseline=1 AND p.PaymentStatus='Unpaid' AND COALESCE(p.Penalty,0)=0 AND NULLIF(p.DatePaid,'') IS NULL)<>538
    THROW 50010,'Final profile baseline failed',1;
IF EXISTS(SELECT 1 FROM dbo.Profiling WHERE SIN IN(SELECT OldSIN FROM #Map) OR SIN IN(SELECT TempSIN FROM #Map)) THROW 50011,'Old or temporary profile identifiers remain',1;
IF (SELECT COUNT(*) FROM dbo.MonthlyBilling WHERE SIN IN(SELECT NewSIN FROM #Map))<>538
    OR EXISTS(SELECT 1 FROM dbo.MonthlyBilling WHERE SIN IN(SELECT NewSIN FROM #Map) AND (BillingYear<>2026 OR BillingMonth<>12 OR PaymentStatus<>'Unpaid' OR Penalty<>0 OR NULLIF(ORNumber,'') IS NOT NULL OR DatePaid IS NOT NULL OR RecordedBy IS NOT NULL OR WebRentBasis<>'BaseOnly'))
    THROW 50012,'Clean December obligation verification failed',1;
IF EXISTS(SELECT SIN FROM dbo.MonthlyBilling WHERE SIN IN(SELECT NewSIN FROM #Map) GROUP BY SIN HAVING COUNT(*)<>1) THROW 50013,'December obligation not unique per profile',1;
IF EXISTS(SELECT 1 FROM dbo.PaymentHistory WHERE SIN IN(SELECT NewSIN FROM #Map)) OR EXISTS(SELECT 1 FROM dbo.StallOwnerArrears WHERE SIN IN(SELECT NewSIN FROM #Map)) THROW 50014,'Target payment or arrears remnants',1;
IF EXISTS(SELECT 1 FROM dbo.MonthlyBilling b LEFT JOIN dbo.Profiling p ON p.SIN=b.SIN WHERE p.SIN IS NULL)
    OR EXISTS(SELECT 1 FROM dbo.StallOwnerArrears a LEFT JOIN dbo.Profiling p ON p.SIN=a.SIN WHERE p.SIN IS NULL)
    OR EXISTS(SELECT 1 FROM dbo.PaymentHistory p LEFT JOIN dbo.Profiling owner ON owner.SIN=p.SIN WHERE owner.SIN IS NULL)
    OR EXISTS(SELECT 1 FROM dbo.PaymentHistoryBilling l LEFT JOIN dbo.PaymentHistory p ON p.Id=l.PaymentHistoryId LEFT JOIN dbo.MonthlyBilling b ON b.Id=l.MonthlyBillingId WHERE p.Id IS NULL OR b.Id IS NULL)
    OR EXISTS(SELECT 1 FROM dbo.PaymentHistoryArrears l LEFT JOIN dbo.PaymentHistory p ON p.Id=l.PaymentHistoryId LEFT JOIN dbo.StallOwnerArrears a ON a.Id=l.StallOwnerArrearsId WHERE p.Id IS NULL OR a.Id IS NULL)
    THROW 50015,'Referential integrity failed',1;
IF EXISTS(SELECT * FROM #ProfilesBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map) EXCEPT SELECT * FROM dbo.Profiling WHERE SIN NOT IN(SELECT NewSIN FROM #Map))
    OR EXISTS(SELECT * FROM dbo.Profiling WHERE SIN NOT IN(SELECT NewSIN FROM #Map) EXCEPT SELECT * FROM #ProfilesBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map)) THROW 50016,'Unrelated profiles changed',1;
IF EXISTS(SELECT * FROM #BillsBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map) EXCEPT SELECT * FROM dbo.MonthlyBilling WHERE SIN NOT IN(SELECT NewSIN FROM #Map))
    OR EXISTS(SELECT * FROM dbo.MonthlyBilling WHERE SIN NOT IN(SELECT NewSIN FROM #Map) EXCEPT SELECT * FROM #BillsBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map)) THROW 50017,'Unrelated bills changed',1;
IF EXISTS(SELECT * FROM #PaymentsBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map) EXCEPT SELECT * FROM dbo.PaymentHistory)
    OR EXISTS(SELECT * FROM dbo.PaymentHistory EXCEPT SELECT * FROM #PaymentsBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map)) THROW 50018,'Unrelated payments changed',1;
IF EXISTS(SELECT * FROM #ArrearsBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map) EXCEPT SELECT * FROM dbo.StallOwnerArrears)
    OR EXISTS(SELECT * FROM dbo.StallOwnerArrears EXCEPT SELECT * FROM #ArrearsBefore WHERE SIN NOT IN(SELECT OldSIN FROM #Map)) THROW 50019,'Unrelated arrears changed',1;
IF EXISTS(SELECT * FROM #UsersBefore EXCEPT SELECT * FROM dbo.Users) OR EXISTS(SELECT * FROM dbo.Users EXCEPT SELECT * FROM #UsersBefore)
    OR EXISTS(SELECT * FROM #RatesBefore EXCEPT SELECT * FROM dbo.RentalRates) OR EXISTS(SELECT * FROM dbo.RentalRates EXCEPT SELECT * FROM #RatesBefore)
    OR EXISTS(SELECT * FROM #VehiclesBefore EXCEPT SELECT * FROM dbo.VehiclePermits) OR EXISTS(SELECT * FROM dbo.VehiclePermits EXCEPT SELECT * FROM #VehiclesBefore)
    OR EXISTS(SELECT * FROM #VehicleHistoryBefore EXCEPT SELECT * FROM dbo.VehiclePermitHistory) OR EXISTS(SELECT * FROM dbo.VehiclePermitHistory EXCEPT SELECT * FROM #VehicleHistoryBefore)
    OR EXISTS(SELECT * FROM #VehicleDraftsBefore EXCEPT SELECT * FROM dbo.VehiclePermitFeeDrafts) OR EXISTS(SELECT * FROM dbo.VehiclePermitFeeDrafts EXCEPT SELECT * FROM #VehicleDraftsBefore) THROW 50020,'Protected users, rates or vehicle data changed',1;
-- Historical audit text is immutable; only its structured SIN reference changes.
IF EXISTS(SELECT a.Id,a.Action,COALESCE(m.NewSIN,a.SIN) SIN,a.UserId,a.Timestamp,a.Details FROM #AuditBefore a LEFT JOIN #Map m ON m.OldSIN=a.SIN EXCEPT SELECT Id,Action,SIN,UserId,Timestamp,Details FROM dbo.AuditTrail)
    OR (SELECT COUNT(*) FROM #AuditBefore)<>(SELECT COUNT(*) FROM dbo.AuditTrail) THROW 50021,'Audit text changed or audit events created',1;
IF (SELECT COALESCE(SUM(AmountPaid),0) FROM dbo.PaymentHistory)<>@collectionBefore-@removedCollection THROW 50022,'Collection sources mismatch',1;
IF EXISTS(SELECT SIN FROM dbo.Profiling GROUP BY SIN HAVING COUNT(*)>1) THROW 50023,'Duplicate SIN',1;
IF EXISTS(SELECT 1 FROM #Map m JOIN dbo.Profiling p ON p.SIN=m.NewSIN WHERE p.FullName<>m.FullName) THROW 50024,'Name-to-SIN mapping changed',1;
IF (SELECT COUNT(*) FROM dbo.PaymentHistoryBilling)<>(SELECT COUNT(*) FROM #BillingLinksBefore)-@removedBillingLinks
    OR (SELECT COUNT(*) FROM dbo.PaymentHistoryArrears)<>(SELECT COUNT(*) FROM #ArrearsLinksBefore)-@removedArrearsLinks THROW 50025,'Payment link count mismatch',1;
IF EXISTS(SELECT * FROM #BillingLinksBefore WHERE PaymentHistoryId NOT IN(SELECT Id FROM #PaymentsBefore WHERE SIN IN(SELECT OldSIN FROM #Map)) AND MonthlyBillingId NOT IN(SELECT Id FROM #BillsBefore WHERE SIN IN(SELECT OldSIN FROM #Map)) EXCEPT SELECT * FROM dbo.PaymentHistoryBilling)
    OR EXISTS(SELECT * FROM #ArrearsLinksBefore WHERE PaymentHistoryId NOT IN(SELECT Id FROM #PaymentsBefore WHERE SIN IN(SELECT OldSIN FROM #Map)) AND StallOwnerArrearsId NOT IN(SELECT Id FROM #ArrearsBefore WHERE SIN IN(SELECT OldSIN FROM #Map)) EXCEPT SELECT * FROM dbo.PaymentHistoryArrears) THROW 50026,'Unrelated payment links changed',1;

SELECT @removedOldBills OldBillsRemoved,@duplicateDecember DuplicateDecemberRemoved,@retainedDecember DecemberIDsRetained,@insertedDecember DecemberNewRows,
    @removedPayments PaymentsRemoved,@removedBillingLinks BillingLinksRemoved,@removedArrearsLinks ArrearsLinksRemoved,@removedArrears ArrearsRemoved,
    @collectionBefore CollectionsBefore,(SELECT COALESCE(SUM(AmountPaid),0) FROM dbo.PaymentHistory) CollectionsAfter;
SELECT Id,SIN,BillingYear,BillingMonth,MonthlyRental,AdditionalCharge,Penalty,PaymentStatus FROM #BillsBefore
    WHERE SIN IN(SELECT OldSIN FROM #Map) AND (BillingYear<2026 OR BillingYear=2026 AND BillingMonth<12) ORDER BY Id;
SELECT SortNumber,OldSIN,NewSIN,FullName FROM #Map ORDER BY SortNumber;
IF $(ApplyReset)=1
BEGIN
    COMMIT TRANSACTION;
    PRINT 'COMMITTED: clean December baseline and alphabetical SIN reassignment; all integrity assertions passed.';
END
ELSE
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'DRY RUN ROLLED BACK: all integrity assertions passed; database unchanged.';
END;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
