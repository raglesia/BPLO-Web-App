:ON ERROR EXIT
IF DB_NAME() <> 'BPLS_Dev' THROW 50000, 'Run this script only in BPLS_Dev', 1;
GO
IF EXISTS (
    SELECT VIN, PermitYear FROM dbo.VehiclePermitHistory
    GROUP BY VIN, PermitYear HAVING COUNT(*) > 1
) THROW 50001, 'Duplicate vehicle/year history must be reviewed before adding uniqueness', 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.VehiclePermitHistory')
      AND name = 'UX_VehiclePermitHistory_VIN_PermitYear'
)
    CREATE UNIQUE INDEX UX_VehiclePermitHistory_VIN_PermitYear
        ON dbo.VehiclePermitHistory(VIN, PermitYear);
GO
