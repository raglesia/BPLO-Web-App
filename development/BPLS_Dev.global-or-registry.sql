:ON ERROR EXIT
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME()<>'BPLS_Dev' THROW 50000,'BPLS_Dev only.',1;
BEGIN TRANSACTION;
BEGIN TRY
    -- Freeze both writers during conflict inspection, backfill and trigger installation.
    SELECT 'StallRental' PaymentType,Id PaymentReferenceId,ORNumber,RecordedBy,DatePaid
    INTO #Payments FROM dbo.PaymentHistory WITH(TABLOCKX,HOLDLOCK)
    UNION ALL SELECT 'VehiclePermit',Id,ORNumber,RecordedBy,DatePaid
    FROM dbo.VehiclePermitHistory WITH(TABLOCKX,HOLDLOCK);
    -- The same SQL normalization is used by all future registry writes.
    IF OBJECT_ID('dbo.NormalizeOR','FN') IS NULL
    EXEC(N'CREATE FUNCTION dbo.NormalizeOR(@value nvarchar(100)) RETURNS nvarchar(100)
    WITH SCHEMABINDING AS BEGIN
        RETURN TRIM(NCHAR(9)+NCHAR(10)+NCHAR(11)+NCHAR(12)+NCHAR(13)+NCHAR(32) FROM @value);
    END');
    SELECT PaymentType,COUNT(*) PaymentRows,COUNT(NULLIF(dbo.NormalizeOR(ORNumber),N'')) ORCount
    FROM #Payments GROUP BY PaymentType;
    SELECT PaymentType,COUNT(*) DuplicateGroups FROM(
        SELECT PaymentType,dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS ORNumber
        FROM #Payments GROUP BY PaymentType,dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1
    )d GROUP BY PaymentType;
    SELECT COUNT(*) GlobalDuplicateGroups FROM(
        SELECT dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS ORNumber
        FROM #Payments GROUP BY dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1
    )d;
    IF EXISTS(SELECT 1 FROM #Payments WHERE NULLIF(dbo.NormalizeOR(ORNumber),N'') IS NULL)
        THROW 50001,'Blank existing OR: inspect payment rows; no migration applied.',1;
    IF EXISTS(SELECT 1 FROM #Payments GROUP BY dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1)
    BEGIN
        SELECT * FROM #Payments WHERE dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS IN(
            SELECT dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS FROM #Payments
            GROUP BY dbo.NormalizeOR(ORNumber) COLLATE Latin1_General_100_CI_AS HAVING COUNT(*)>1);
        THROW 50002,'Conflicting normalized ORs: no migration applied.',1;
    END;
    IF OBJECT_ID('dbo.ReceiptRegistry','U') IS NULL
    EXEC(N'CREATE TABLE dbo.ReceiptRegistry(
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceiptRegistry PRIMARY KEY,
        ORNumber nvarchar(100) COLLATE Latin1_General_100_CI_AS NOT NULL,
        PaymentType varchar(20) NOT NULL,
        PaymentReferenceId int NULL,
        RecordedByUserId int NOT NULL,
        RecordedAt datetime NOT NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_ReceiptRegistry_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_ReceiptRegistry_ORNumber UNIQUE(ORNumber),
        CONSTRAINT CK_ReceiptRegistry_Type CHECK(PaymentType IN(''StallRental'',''VehiclePermit'')),
        CONSTRAINT CK_ReceiptRegistry_OR CHECK(DATALENGTH(ORNumber)>0 AND DATALENGTH(ORNumber)=DATALENGTH(dbo.NormalizeOR(ORNumber)))
    ); CREATE UNIQUE INDEX UX_ReceiptRegistry_Payment ON dbo.ReceiptRegistry(PaymentType,PaymentReferenceId) WHERE PaymentReferenceId IS NOT NULL;');
    EXEC(N'INSERT INTO dbo.ReceiptRegistry(ORNumber,PaymentType,PaymentReferenceId,RecordedByUserId,RecordedAt)
        SELECT dbo.NormalizeOR(p.ORNumber),p.PaymentType,p.PaymentReferenceId,p.RecordedBy,p.DatePaid
        FROM #Payments p WHERE NOT EXISTS(SELECT 1 FROM dbo.ReceiptRegistry r
            WHERE r.PaymentType=p.PaymentType AND r.PaymentReferenceId=p.PaymentReferenceId);');
    -- Cover direct SQL/older writers too. Existing history OR fields are never rewritten.
    DECLARE @table sysname,@type varchar(20),@sql nvarchar(max);
    DECLARE ledgers CURSOR LOCAL FAST_FORWARD FOR
        SELECT 'PaymentHistory','StallRental' UNION ALL SELECT 'VehiclePermitHistory','VehiclePermit';
    OPEN ledgers; FETCH NEXT FROM ledgers INTO @table,@type;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @sql=N'CREATE OR ALTER TRIGGER dbo.TR_'+@table+N'_ReceiptRegistry ON dbo.'+QUOTENAME(@table)+N'
        AFTER INSERT,UPDATE,DELETE AS
        BEGIN
          SET NOCOUNT ON;
          IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
             WHERE i.ORNumber COLLATE Latin1_General_100_BIN2<>d.ORNumber COLLATE Latin1_General_100_BIN2
             OR DATALENGTH(i.ORNumber)<>DATALENGTH(d.ORNumber))
             THROW 51002,''Recorded OR numbers cannot be changed.'',1;
          DELETE r FROM dbo.ReceiptRegistry r JOIN deleted d ON r.PaymentReferenceId=d.Id
             AND r.PaymentType='''+@type+N''' WHERE NOT EXISTS(SELECT 1 FROM inserted i WHERE i.Id=d.Id);
          UPDATE r SET PaymentReferenceId=i.Id FROM dbo.ReceiptRegistry r JOIN inserted i
             ON r.ORNumber=dbo.NormalizeOR(i.ORNumber) COLLATE Latin1_General_100_CI_AS
             WHERE r.PaymentType='''+@type+N''' AND r.PaymentReferenceId IS NULL;
          INSERT INTO dbo.ReceiptRegistry(ORNumber,PaymentType,PaymentReferenceId,RecordedByUserId,RecordedAt)
             SELECT dbo.NormalizeOR(i.ORNumber),'''+@type+N''',i.Id,i.RecordedBy,i.DatePaid FROM inserted i
             WHERE NOT EXISTS(SELECT 1 FROM dbo.ReceiptRegistry r
                WHERE r.PaymentType='''+@type+N''' AND r.PaymentReferenceId=i.Id);
        END';
        EXEC(@sql);
        FETCH NEXT FROM ledgers INTO @table,@type;
    END;
    CLOSE ledgers; DEALLOCATE ledgers;
    EXEC(N'IF EXISTS(SELECT 1 FROM #Payments p LEFT JOIN dbo.ReceiptRegistry r
            ON r.PaymentType=p.PaymentType AND r.PaymentReferenceId=p.PaymentReferenceId
            WHERE r.Id IS NULL OR r.ORNumber<>dbo.NormalizeOR(p.ORNumber) COLLATE Latin1_General_100_CI_AS
            OR r.RecordedByUserId<>p.RecordedBy OR r.RecordedAt<>p.DatePaid)
        OR EXISTS(SELECT 1 FROM dbo.ReceiptRegistry r LEFT JOIN #Payments p
            ON r.PaymentType=p.PaymentType AND r.PaymentReferenceId=p.PaymentReferenceId WHERE p.PaymentReferenceId IS NULL)
        THROW 50003,''Receipt backfill consistency failed.'',1;
        SELECT COUNT(*) RegistryRows FROM dbo.ReceiptRegistry;');
    COMMIT;
    PRINT 'PASS: global OR registry/backfill committed in BPLS_Dev.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
