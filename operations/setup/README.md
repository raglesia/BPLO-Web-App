# Guided Windows host setup

`Setup-BPLO.bat` launches the main SQL Server web app setup. It does not install the SQLite demo. SSMS is optional and is not required by this tool.

## Build PC

With .NET 10 SDK installed, run `operations\setup\Build-SetupPackage.ps1`. It publishes the current web source and creates `artifacts\BPLO-Setup`. Copy that entire folder to the host. It excludes databases, backup files, local host settings and development settings. Keep `package-manifest.json` with the release. Use a new output folder for each build.

## Host preparation

Use supported x64 Windows Pro/Server, a stable LAN/DNS hostname, Philippine time, and an approved setup administrator. SQL Server and IIS must be on this same host for this installer.

Install SQL Server Database Engine (Express or appropriately licensed edition), IIS, then .NET 10 Hosting Bundle. Alternatively set `InstallPrerequisites=true` and supply paths to official Microsoft-signed **full local installers**. SQL installation opens Microsoft's wizard: select Database Engine, Windows Authentication and the approved instance. The Hosting Bundle runs quietly after IIS. A required reboot stops setup; reboot and rerun. No software is downloaded, SQL passwords stored, or existing SQL instances replaced automatically. Windows Server administrators may need to enable IIS through Server Manager first.

Provision a trusted HTTPS certificate in Local Computer / Personal with a private key and the exact DNS hostname. Provision trusted SQL encryption. The app/database setup does not bypass certificate validation. Arrange DNS and an approved LAN subnet; a certificate alone does not create DNS.

## Configure and review

1. Copy `operations\setup\setup.example.json` to `operations\setup\setup.json`.
2. Replace every REPLACE value. ServerIdentity is SQL's `SERVERPROPERTY('ServerName')`, not merely a guessed hostname. Choose `Create` for a new empty approved database or `Validate` for a previously restored compatible database. Neither overwrites data. Do not use BPLS_Dev or synthetic staging data.
3. Select a new application folder, site and pool. Set the exact hostname/certificate thumbprint, HTTPS port and approved IPv4 LAN range.
4. Leave prerequisite installation false if components are already installed.
5. Optionally set `PrepareBackupTask=true`, confirm the SQL service Log On identity, and choose the task name. The current setup administrator is the local S4U task identity; no password is written. It must already have authorized SQL backup/VERIFYONLY access. The installer does not grant sysadmin or unattended backup SQL rights.
6. Run `Setup-BPLO.bat` for a configuration plan. This performs no installation.
7. Review the plan. From an administrator terminal run `Setup-BPLO.bat -Apply`.

## What Apply does

- Validates prerequisites, exact certificate dates/name/private key, free port, and absence of conflicting destination/site/pool/firewall/task resources.
- Uses the existing guarded schema Create/Validate tool. Fresh creation has no official data or employee accounts.
- Copies published files, writes approved Production configuration, creates a dedicated No Managed Code IIS pool with its Windows user profile loaded, and grants Read & Execute to its identity.
- Creates its Windows SQL login/user if absent and grants dbo data CRUD/execute only. No application server role or DDL grant is made.
- Creates the HTTPS site with SNI and an inbound firewall rule scoped to Domain/Private and the configured LAN.
- Optionally prepares `C:\BPLO_DB_Backup`, grants Modify to the exact SQL service and task accounts, runs a timestamped full COPY_ONLY/CHECKSUM backup plus VERIFYONLY, and registers a **disabled** Friday 4:30 PM task with StartWhenAvailable. No backups are deleted.

IIS's loaded application-pool profile supplies the normal persistent Windows Data Protection key location. Verify profile/key persistence through recycling/restarting before accepting the installation. Keep the same pool identity for upgrades.

## Required acceptance after setup

An installation message is not official acceptance. A DBA must transfer the approved profiles/rates and related records and validate the restored schema. Never copy the development database blindly. Create three individual users through the existing account workflow and verify access policy. Check trusted HTTPS from all three PCs, login/logout/audit, authorized billing/payment/report operations, and app restart/session behavior.

For the backup, review ACL inheritance (no broad write grants), SQL access and protected scripts. Enable and manually trigger the disabled task during a maintenance window, including while IIS BPLO is stopped. Verify task result 0 and the backup log, then restart the site. Enable recurrence only after authorized acceptance. S4U is for local resources; this installer does not support remote SQL/backups. Keep a second protected backup copy and periodically test isolated recovery.

## Failure and upgrades

Stop and inspect the displayed error. Database creation/schema application has its own transaction, but Windows/IIS/filesystem operations are not one transaction. Partial resources may remain. Nothing is automatically deleted, restored or overwritten on retry. Preserve logs and review resources before manual remediation. This is a fresh-host installer, not an in-place upgrade/data-migration or disaster-recovery tool. No installation was performed on the official host during creation of this package.
