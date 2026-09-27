:ON ERROR EXIT
IF DB_NAME() <> 'BPLS_Dev' THROW 50000, 'Run this script only in BPLS_Dev', 1;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Profiling WHERE SIN = 'SIN-2099-0001')
    INSERT INTO dbo.Profiling (SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize, MonthlyRental, PaymentStatus, StartDate, IsArchived)
    VALUES ('SIN-2099-0001', 'Dev Sample Owner One', 'Dev Sample Business One', 'Public Market Stalls', '9001', '2', 420, 'Unverified', '', 0);
IF NOT EXISTS (SELECT 1 FROM dbo.Profiling WHERE SIN = 'SIN-2099-0002')
    INSERT INTO dbo.Profiling (SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize, MonthlyRental, PaymentStatus, StartDate, IsArchived)
    VALUES ('SIN-2099-0002', 'Dev Sample Owner Two', 'Dev Sample Business Two', 'Public Market Stalls', '9002', '2', 420, 'Unverified', '', 0);
IF NOT EXISTS (SELECT 1 FROM dbo.Profiling WHERE SIN = 'SIN-2099-0003')
    INSERT INTO dbo.Profiling (SIN, FullName, BusinessName, BusinessSection, StallNumber, StallSize, MonthlyRental, PaymentStatus, StartDate, IsArchived)
    VALUES ('SIN-2099-0003', 'Dev Sample Archived Owner', 'Dev Sample Archived Business', 'Public Market Stalls', '9003', '2', 420, 'Unverified', '', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.VehiclePermits WHERE VIN = 'VIN-2099-0001')
    INSERT INTO dbo.VehiclePermits (VIN, CompanyName, DriverName, PlateNo, IsArchived)
    VALUES ('VIN-2099-0001', 'Dev Sample Company One', 'Dev Driver One', 'DEV-PLATE-001', 0);
IF NOT EXISTS (SELECT 1 FROM dbo.VehiclePermits WHERE VIN = 'VIN-2099-0002')
    INSERT INTO dbo.VehiclePermits (VIN, CompanyName, DriverName, PlateNo, IsArchived)
    VALUES ('VIN-2099-0002', 'Dev Sample Company Two', 'Dev Driver Two', 'DEV-PLATE-002', 0);
IF NOT EXISTS (SELECT 1 FROM dbo.VehiclePermits WHERE VIN = 'VIN-2099-0003')
    INSERT INTO dbo.VehiclePermits (VIN, CompanyName, DriverName, PlateNo, IsArchived)
    VALUES ('VIN-2099-0003', 'Dev Sample Archived Company', 'Dev Archived Driver', 'DEV-PLATE-003', 1);
GO
