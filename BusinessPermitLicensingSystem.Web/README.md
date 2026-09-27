# BPLO web foundation

This ASP.NET Core Razor Pages project is separate from the existing WinForms application. The home and database-check pages require a user account from the existing `Users` table. The database check runs only `SELECT 1`. The web application does not initialize or change the database schema.

## Configure SQL Server

Keep credentials on the server. For local development, set the `BPLS` connection string with .NET user secrets:

```powershell
dotnet user-secrets init --project .\BusinessPermitLicensingSystem.Web.csproj
dotnet user-secrets set "ConnectionStrings:BPLS" "Server=YOUR_SERVER;Database=YOUR_DATABASE;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True" --project .\BusinessPermitLicensingSystem.Web.csproj
```

Prefer a development copy of the BPLO database. On a deployed host, set the `ConnectionStrings__BPLS` environment variable or another ASP.NET Core server-side configuration source. Use a SQL account with read-only permission for this phase. Never place credentials in `appsettings.json` or web assets.

## Run locally

```powershell
dotnet run --project .\BusinessPermitLicensingSystem.Web.csproj
```

Open the shown local URL. The application redirects to `/Account/Login`. After login, `/DatabaseHealth` checks connectivity. SQL exceptions are logged on the server and are not displayed on the page.

The web application uses cookie authentication. It accepts existing PBKDF2 passwords and legacy SHA-256 passwords. A verified legacy password is upgraded to the existing PBKDF2 format. The cookie contains user ID, username, full name, and position; it contains no password or hash. Position is displayed but is not an authorization role. Public account creation is deliberately unavailable until BPLO decides who may create accounts.

The authenticated home page reads only active and archived profile counts and active vehicle-permit count. Its links open the migrated workflows. Viewing the dashboard does not run billing, penalty, or annual-reset operations. The application is restricted to `BPLS_Dev`; it is not configured for production use. Local development database details are in `../development/README.md`; functional audit and browser-test evidence are in `../development/PHASE11.md` and `../development/PHASE12.md`.

## Current boundary

Phase 12 remains local development and functional verification. See `../development/PHASE12.md` for tested workflows, remaining limits, and policy decisions.
