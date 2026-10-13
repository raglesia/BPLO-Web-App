# BPLO Host PC Deployment — Step-by-Step Guide

Municipality of Masinloc · Business Permit and Licensing Office

Prepared October 5, 2026. This guide describes deployment; it does not confirm that production deployment has occurred.

## Deployment prerequisite

QA-01 application target support and the schema-v1 installation path are now locally validated. Use `operations/database/README.md` to configure the approved server, SQL identity, and database and to create/validate the schema. Official host/IIS/HTTPS, permissions, data transfer, and any incompatible existing-database migration still require acceptance. Do not run development reset, sample, cleanup, or regression scripts against official data.

## System arrangement

One Windows host runs IIS, the BPLO web app, and SQL Server. Three employees connect to that same website through their browsers over the office LAN. The host can also be one employee's workstation. It must stay powered on and awake while employees use BPLO.

```text
User 1 browser ─┐
User 2 browser ─┼── Office LAN ── Host PC
User 3 browser ─┘                  ├─ IIS + BPLO Web App
                                  └─ SQL Server database
```

## 1. Prepare the host PC

1. Use patched, supported x64 Windows with IIS support, such as Windows 11 Pro or Windows Server.
2. Set the correct Philippine date, time, and UTC+08:00 time zone.
3. Connect to the office network, preferably through Ethernet.
4. Ask municipal IT to assign a stable IP address or router DHCP reservation and approved hostname.
5. Prevent sleep during office hours. Provide reliable power and sufficient storage.
6. Create the following folders:

| Folder | Purpose |
|---|---|
| `C:\BPLO\Web` | Published application |
| `C:\BPLO_Operations\Backup` | Protected backup scripts and configuration |
| `C:\BPLO_DB_Backup` | Database backups and logs |

## 2. Install SQL Server

Install the SQL Server Database Engine. SSMS alone does not install the database engine.

1. Obtain the official Microsoft installer.
2. Choose a standalone installation and Database Engine Services.
3. Record the actual instance name, for example `SQLEXPRESS`.
4. Prefer Windows Authentication and assign approved database administrators.
5. Configure the SQL Server service to start automatically.
6. Record its service account for backup-folder permissions.

Use an edition licensed for production. Express may be suitable; Developer Edition is for development/testing. Our documented development engine is SQL Server 2025 Express. A backup cannot be restored to an older SQL Server version than the engine that created it.

## 3. Install SSMS

SSMS is an optional administration tool, recommended on the host for setup and maintenance. Employees do not need it.

1. Install SQL Server Management Studio.
2. Connect to the actual instance, such as `HOST-PC-NAME\SQLEXPRESS`.
3. Use Windows Authentication under the approved administrator account.

## 4. Create or restore the approved database

Choose one reviewed installation path:

| Situation | Approach |
|---|---|
| New database | Create it, then run the approved production schema migration |
| Approved existing database | Restore its verified backup, then apply required migrations |

### New database

1. In SSMS, right-click Databases and choose New Database.
2. Enter the approved database name and create it.
3. Select that database before opening/running the approved schema script.
4. Verify the script's database target and preflight requirements.
5. Run the reviewed migration and verify its results.

Creating an empty database does not create BPLO tables. The application currently does not create its database or tables automatically. Use the schema-v1 package through its guarded PowerShell entry point. Its installation path passed local staging validation; an incompatible existing database requires a separately reviewed incremental migration.

The schema must include all application requirements: users, profiles, rental rates, monthly bills, rental payment histories and links, vehicle records/history and fee drafts, arrears, audit records, and the global OR registry. Verify indexes, relationships, constraints, and receipt safeguards as well as tables.

### Transfer official data

Load only the approved records through the agreed migration process. Verify:

- Exactly 538 official Stall Owner profiles.
- Correct identifiers, rental rates, and monthly rental values.
- Approved billing baseline and outstanding balances.
- No synthetic profiles, rates, users, or payments.
- No duplicate billing periods or OR numbers.

Decide which other existing records transfer before migration. Do not copy the entire development database without reviewing its contents. If an official database already exists, inspect and back it up before any authorized change; do not overwrite it blindly.

## 5. Install IIS and .NET

1. Enable Internet Information Services and its Management Console through Windows features.
2. Install the x64 .NET 10 Hosting Bundle after enabling IIS.
3. Restart IIS or the host as required.

If the bundle was installed before IIS, repair/reinstall it afterward. The host receiving a published application does not require Visual Studio or the .NET SDK.

## 6. Publish and copy BPLO

On the development/build PC, from the solution repository root:

```powershell
dotnet restore .\BusinessPermitLicensingSystem.Web\BusinessPermitLicensingSystem.Web.csproj
dotnet build .\BusinessPermitLicensingSystem.slnx -c Release
dotnet publish .\BusinessPermitLicensingSystem.Web\BusinessPermitLicensingSystem.Web.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\BPLO-Web
```

Copy the approved published output to `C:\BPLO\Web`, including `web.config` and `wwwroot`. Exclude development secrets, regression runners, databases, experimental build folders, and the separate SQLite demo. Record the deployed commit and package version.

## 7. Configure IIS and SQL access

1. Create a dedicated x64 application pool, for example BPLO, with No Managed Code.
2. Create a website pointing to `C:\BPLO\Web`.
3. Configure the approved hostname and trusted HTTPS certificate.
4. Set `ASPNETCORE_ENVIRONMENT=Production`.
5. Configure `ConnectionStrings__BPLS` through protected host configuration, including the approved target settings from the database guide.
6. Grant the application pool identity the necessary SQL login/database permissions. Integrated authentication uses this identity, not the interactive SSMS administrator.
7. Grant Read & Execute on published files and only necessary writable permissions.
8. Configure persistent, protected Data Protection key storage for authentication cookies.
9. Configure trusted SQL encryption/certificates and approved host allowlisting.
10. Start/recycle the application pool and verify local access.

Do not grant the web application SQL sysadmin permissions or commit credentials. Exact database name, identities, certificates, and permissions must be reviewed for the host.

## 8. Connect the employee PCs

Ask municipal IT to:

1. Verify all employee PCs can reach the host over the office LAN.
2. Allow the website's HTTPS port through Windows Firewall for the approved LAN.
3. Configure hostname resolution and certificate trust on employee PCs.
4. Create a browser shortcut to the approved URL on each PC.

An illustrative URL is `https://bplo.office.example`; IT must supply the actual address.

Do not use `127.0.0.1` on other PCs: it refers to each employee's own computer. Employee browsers connect to the web app, not directly to SQL Server. When IIS and SQL share the host, employee PCs do not need SQL ports exposed or SQL client installations.

## 9. Create three employee accounts

Use the approved BPLO account-management process. Give each employee a separate account with the correct full name, position, and access under existing permissions. Confirm names/positions appear correctly on reports.

All three accounts share one database. Saved changes become available to other employees when their pages load or refresh.

## 10. Configure independent weekly backups

Prepare the reviewed production registration/configuration. The included development registration script refuses production and must not be treated as a production installer.

Required settings:

- Weekly Friday at 4:30 PM, host local Philippine time.
- Destination: `C:\BPLO_DB_Backup`.
- Timestamped unique `.bak` filenames.
- Backup with checksum and `RESTORE VERIFYONLY` verification.
- Success/failure logging and nonzero exit on failure.
- Run whether the task account is logged on or not.
- Run as soon as possible after a missed scheduled start.
- Prevent overlapping runs.
- No automatic deletion of old backups.

SQL Server writes backups using its service account. Grant that exact identity the required destination-folder permissions. The task account needs approved SQL backup/verification permissions, script execution, log writing, and required file access. Do not grant Everyone Full Control or automatically grant sysadmin.

Test a manual run under the task account. Then, during an approved maintenance window, stop BPLO, trigger the task, verify backup/VERIFYONLY/log success, and restart BPLO. Leave the production schedule enabled only after acceptance and authorization. The previous development task remains disabled.

A powered-off host cannot run a backup. Keep a protected copy on another device, monitor disk space/logs, and periodically restore into an isolated database. VERIFYONLY does not replace a full recovery test. Protect application files, IIS configuration, certificates, and authentication keys separately.

## 11. Complete three-user acceptance testing

Use isolated staging for synthetic financial tests. Never run destructive development fixtures against the official database.

| Area | Acceptance check |
|---|---|
| Network | All three PCs open BPLO through trusted HTTPS |
| Accounts | Separate logins work; login/logout audits appear |
| Official data | 538 profiles and approved rates/balances verified |
| Billing | Baseline, arrears, penalties, and totals follow approved rules |
| Payments | Concurrent activity does not duplicate payments or OR numbers |
| Vehicles | Assessments, employees × PHP 300 occupational fee, and payments work |
| Reports | Printed layouts, names, positions, and dates are correct |
| Restart | Database and app return after a host restart |
| Backup | Independent scheduled backup, verification, and isolated restore succeed |

Record test evidence and approval before official opening. Take a verified backup before upgrades and preserve the previous application package. Review schema compatibility before rollback; database restoration is separately authorized and can discard newer transactions.

## Installation summary

| Component | Host PC | Other employee PCs |
|---|---|---|
| SQL Server Database Engine | Required | Not required |
| SSMS | Administrator tool | Not required |
| IIS | Required | Not required |
| .NET 10 Hosting Bundle | Required | Not required |
| Published BPLO application | Required | Not required |
| Current browser | Required | Required |

**Order:** resolve QA-01 → pass staging validation → prepare host → install SQL/IIS/.NET → create or restore approved database → install/configure BPLO → connect LAN users → create accounts → configure/test backups → complete acceptance.

## References

- [Microsoft SQL Server installation](https://learn.microsoft.com/sql/database-engine/install-windows/install-sql-server-from-the-installation-wizard-setup)
- [Microsoft .NET 10 Hosting Bundle](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0)
- [Microsoft IIS hosting guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0)
- [Microsoft RESTORE VERIFYONLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver17)
- Repository README and `operations/backup/README.md` for technical configuration details.
