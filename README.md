# BPLO Web App — Official Host-PC Deployment Guide

Municipality of Masinloc, Zambales · Business Permit and Licensing Office

ASP.NET Core Razor Pages for Stall Owners, rental billing/payments, Special Vehicle Permits, arrears, archives, imports/exports, audit records, and printable reports. Install on a Windows host serving municipal employees over an approved LAN. Employee PCs need only a browser.

## Release gate — resolve before official use

**Current source is development-validated, not production-approved.** Authentication and several services explicitly require `BPLS_Dev`; changing the connection string alone will fail. Development SQL scripts also enforce that database. Authorize and review production configuration/schema support, prepare an approved migration, and validate it on an isolated staging copy before official cutover. Do not remove safeguards blindly or run reset/sample scripts on official data.

This is the deployment runbook, not evidence that production installation has occurred. No production database, credentials, or backup is included in this repository.

## 1. Prepare the Windows host

Use patched, supported x64 Windows with IIS support (Windows 11 Pro or supported Windows Server), reliable power, sufficient database/backup storage, and a stable LAN address or municipal DNS name. Set Windows to Philippine time (`Singapore Standard Time`, UTC+08:00); Task Scheduler uses host local time.

Record the approved SQL instance, database, HTTPS hostname/certificate, IIS identity, and unattended backup identity. Separate folders:

- `C:\BPLO\Web`: published application.
- `C:\BPLO_Operations\Backup`: administrator-controlled scripts/configuration.
- `C:\BPLO_DB_Backup`: protected database backups/logs.

## 2. Install SQL Server

1. Obtain Microsoft's official SQL Server installer. Development backup tests used SQL Server 2025 Express, version 17.x. Use the same or newer compatible engine when restoring an authorized backup; a backup cannot be restored to an older SQL version.
2. Choose a new standalone installation and **Database Engine Services**. Record the actual named instance, for example `SQLEXPRESS`.
3. Prefer Windows Authentication. Assign approved database administrators and start SQL Server automatically. Developer Edition is for development/testing, not official production use.
4. Record the SQL service Log On account in SQL Server Configuration Manager or Windows Services.
5. When SQL and IIS share the host, keep SQL local; employee browser PCs do not need SQL ports. If SQL is on another machine, run backup scripts on that SQL host and separately review network/authentication requirements.

Sources: [SQL Server installation](https://learn.microsoft.com/sql/database-engine/install-windows/install-sql-server-from-the-installation-wizard-setup), [SQL Server 2025 requirements](https://learn.microsoft.com/en-us/sql/sql-server/install/hardware-and-software-requirements-for-installing-sql-server-2025?view=sql-server-ver17).

## 3. Necessary tools and add-ons

| Component | Installation location / purpose |
|---|---|
| SQL Server Database Engine | SQL host; required by main BPLO |
| SSMS | Optional administrator tool; not required to run BPLO/backups |
| IIS and Web Management Console | Web host |
| .NET 10 Hosting Bundle, x64 | Web host, installed **after IIS** |
| .NET 10 SDK | Build PC only |
| Windows PowerShell 5.1 / Task Scheduler | SQL host; built into supported Windows |
| Current browser | Employee PCs |

The Hosting Bundle supplies the ASP.NET Core runtime and IIS module. If installed before IIS, repair/reinstall afterward. Restart IIS during approved maintenance. [Microsoft Hosting Bundle guide](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0).

Office/Excel, SQLite, SQL Server Agent, third-party schedulers, and separate ClosedXML/CsvHelper installations are not required. NuGet dependencies accompany published output. ASP.NET 4.x Windows features do not replace the .NET 10 Hosting Bundle.

## 4. Prepare the approved database

A DBA must supply an approved official schema/data migration or authorized backup to restore. **The development initializer is not a production migration package.** Preserve profiles, rates, bills, payment links, OR references, vehicle drafts/history, arrears, users, and audits; compare identifiers, counts, balances, and constraints before cutover.

Never transfer disposable test fixtures into official operation. Do not run `BPLS_Dev.samples.sql`, clean-baseline resets, or the regression console on an official database. Establish the IIS identity's necessary SQL permissions without granting application sysadmin. Review account creation/access policy before exposing the site.

## 5. Publish on the build PC

From the repository root:

```powershell
dotnet restore .\BusinessPermitLicensingSystem.Web\BusinessPermitLicensingSystem.Web.csproj
dotnet build .\BusinessPermitLicensingSystem.slnx -c Release
dotnet publish .\BusinessPermitLicensingSystem.Web\BusinessPermitLicensingSystem.Web.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\BPLO-Web
```

Copy the reviewed **published output**, including `web.config` and `wwwroot`, to `C:\BPLO\Web`. Exclude WinForms, regression runners, developer secrets, databases, test records, and build experiment folders. Install a supported .NET 10 runtime patch on the host.

## 6. Configure IIS and application access

1. Create a dedicated x64 application pool with **No Managed Code** and reviewed identity. Grant Read & Execute on published files and only necessary writable locations.
2. Create a site pointing to `C:\BPLO\Web`; configure a trusted HTTPS binding for its approved hostname. The app uses HTTPS redirection/HSTS; resolve certificate setup rather than disabling protections.
3. Set `ASPNETCORE_ENVIRONMENT=Production`. Supply `ConnectionStrings__BPLS` through protected host configuration. Do not commit credentials or store SQL passwords in scripts. Integrated authentication uses the IIS identity, not the interactive installer.
4. After the release gate is resolved, connection-string shape: `Server=HOST\INSTANCE;Database=APPROVED_DATABASE;Integrated Security=True;Encrypt=True;TrustServerCertificate=False`. Configure a trusted SQL certificate where required; do not copy development trust overrides without review.
5. Configure approved host allowlisting, stable Data Protection key storage with appropriate ACLs for authentication cookies, and firewall access restricted to the approved LAN. Do not expose SQL/BPLO publicly as part of installation.
6. Recycle the pool; verify locally and from an employee PC. Clients use the host name/address, not `127.0.0.1`, which refers to their own PC.

[Microsoft IIS hosting guide](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0). Actual IIS/identity/certificate acceptance is required; localhost Kestrel checks are not proof of production hosting.

## 7. Install the independent weekly backup

Copy `operations/backup/` to `C:\BPLO_Operations\Backup`. Restrict script/config modification to approved administrators. Configure actual local SQL instance, official database, SQL service identity, destination `C:\BPLO_DB_Backup`, Friday, `16:30`, and an approved production task name.

**The included registration script is development-only and refuses production.** Prepare and review a production registration version after explicit authorization. Its action must pass `-AllowConfiguredDatabase` to `Backup-BPLODatabase.ps1` with the reviewed host config. Do not repurpose the installed development task accidentally.

### Identities and permissions

SQL Server writes backup files as its service account. The script creates a missing destination and adds Modify for that exact configured identity. For an existing folder, inspect/remediate ACLs as administrator. Inspect inherited rights and restrict backup access to approved identities; no Everyone Full Control.

The task identity needs SQL backup **and VERIFYONLY** permissions, script execution, log writing, and backup-file reading. Do not automatically grant sysadmin. [VERIFYONLY permissions/reference](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver17).

Development passed under `JOHN\jmere` with S4U, no stored password, and existing local SQL access. This account is not portable. Test the actual host account; S4U must not be assumed to support remote SQL/network storage. Where necessary use an approved service account or Task Scheduler's protected credential entry, never passwords in JSON, scripts, or command arguments.

### Schedule and verification

Required settings: weekly **Friday 4:30 PM**, run whether logged on or not, **StartWhenAvailable**, no idle requirement, allow battery operation, no automatic wake, ignore overlapping runs. Use Windows PowerShell 5.1 and absolute script/config paths.

Each run creates `DATABASE_YYYY-MM-DD_HHmmss_fff_unique.bak`, using COPY_ONLY, CHECKSUM, NOINIT, STATS=10, then checks nonempty output and runs RESTORE VERIFYONLY WITH CHECKSUM. Compression is omitted for the inspected Express setup. Logs: `C:\BPLO_DB_Backup\logs\backup.log`. Failures return nonzero; verified success returns 0. A free-space estimate is checked before backup. No old backups are automatically deleted.

After explicit production backup authorization:

1. Run the backup script manually under the approved identity with reviewed config and `-AllowConfiguredDatabase`.
2. Confirm file size, VERIFYONLY success, and success log.
3. Register the reviewed production task in an administrator terminal; stop BPLO during a maintenance window, trigger it, confirm result 0, then restart BPLO.
4. Confirm the trigger and missed-run settings; leave production scheduling enabled only after approval.
5. Run again to confirm a new filename. Keep a protected copy on another device and periodically test recovery into an isolated database. VERIFYONLY is not a full recovery test.

```powershell
# Actual approved production task name:
Start-ScheduledTask -TaskName 'BPLO Weekly Database Backup'
Get-ScheduledTaskInfo -TaskName 'BPLO Weekly Database Backup'
Get-Content C:\BPLO_DB_Backup\logs\backup.log -Tail 30
Disable-ScheduledTask -TaskName 'BPLO Weekly Database Backup'
```

Powered-off machines cannot back up; missed jobs run when Windows/Task Scheduler permits after startup. Changing JSON day/time does not update an existing task trigger. Monitor disk growth and failures. Backups cover the whole database, not binaries, IIS configuration, certificates, or cookie keys; protect those separately. See [operations guide](operations/backup/README.md).

## 8. Acceptance and maintenance

Verify login/permissions, all module reads, authorized staging transactions, OR validation, printing, baseline/arrears rules, record counts, LAN HTTPS, app restart, and independent backup/recovery. Never run destructive fixture tests against production.

Record deployed commit/package, approved schema, host configuration, backup evidence, and responsible administrator. Before upgrades take a verified backup and preserve previous binaries/config. Binary rollback requires schema compatibility; database restore is separately authorized and may lose newer transactions. Never run RESTORE DATABASE merely to check a backup.

## Repository map

- `BusinessPermitLicensingSystem.Web/`: main web source; `Program.cs` startup and `Pages/Index.cshtml` Dashboard.
- `BusinessPermitLicensingSystem/`: original WinForms source.
- `BusinessPermitLicensingSystem.BillingTests/`: synthetic development regression runner.
- `development/`: historical development scripts/notes, not official migrations.
- `operations/backup/`: host backup scripts and guide.
- `BusinessPermitLicensingSystem.slnx`: full solution.

Development uses BPLS_Dev and .NET user secrets for `ConnectionStrings:BPLS`. SQLite demo is separate. Development backup tests passed and its Friday 16:30 task is **disabled**. No production connection, task registration, or deployment occurred during implementation.
