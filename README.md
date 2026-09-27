# BPLO Web App

ASP.NET Core Razor Pages application for BPLO stall profiles, rental billing and payments, vehicle permits and annual payments, archives, imports and exports, administration, and printable reports. The solution also includes the source WinForms application and a regression console project.

## Development setup

Install the .NET 10 SDK and SQL Server. Create the dedicated `BPLS_Dev` database, then review and run the guarded scripts in `development/` against that database only. `development/BPLS_Dev.samples.sql` contains synthetic sample records. See [development notes](development/README.md) for setup details.

Supply `ConnectionStrings:BPLS` through .NET user secrets or a server environment variable. Do not place credentials in source or `appsettings.json`.

```powershell
dotnet user-secrets set "ConnectionStrings:BPLS" "Server=YOUR_SERVER;Database=BPLS_Dev;Integrated Security=True;Encrypt=True;TrustServerCertificate=True" --project .\BusinessPermitLicensingSystem.Web
dotnet build .\BusinessPermitLicensingSystem.slnx
dotnet run --project .\BusinessPermitLicensingSystem.Web
```

Run the regression console against the dedicated development database:

```powershell
dotnet run --project .\BusinessPermitLicensingSystem.BillingTests -- --database --payments --vehicles --archive --transfer --reports --admin
```

Browser scenarios additionally require the web app running locally and `--browser=http://127.0.0.1:5153` with matching port. Tests contain only synthetic records and check for `BPLS_Dev`.

## Status and data boundary

Current source includes the Stall Owner Rental Payment and Special Vehicle Permit Payment printable reports. Phase records and known report limits are in `development/`. `Masinloc_BPLS` is a production database; it is not a development database and is not included here. No production data, database backup, production connection string, or deployment setup is supplied.
