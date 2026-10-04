# BPLO host database backups

Standalone Windows PowerShell 5.1 script and Windows Task Scheduler; no web process dependency. Development configuration targets only BPLS_Dev, weekly Friday 16:30 in the host's local time.

## Files and operation

- `backup.config.json`: instance, database, folder, weekly day/time, task name, and SQL service identity. No secrets.
- `Backup-BPLODatabase.ps1`: creates the configured folder if absent, tests writing, checks disk space, creates a uniquely timestamped full native backup, checks nonempty output, and runs RESTORE VERIFYONLY WITH CHECKSUM.
- `Register-BPLOBackupTask.ps1`: administrator registration, current Windows account with S4U (no stored password), weekly trigger, StartWhenAvailable, no idle requirement, no automatic wake, no overlapping runs, two-hour task limit. Registration refuses to overwrite an existing task and permits development only.

Run from this folder:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Backup-BPLODatabase.ps1
# In an administrator terminal, after confirming the task account has SQL access:
.\Register-BPLOBackupTask.ps1
Start-ScheduledTask -TaskName 'BPLO Development Weekly Database Backup'
Get-ScheduledTaskInfo -TaskName 'BPLO Development Weekly Database Backup'
Disable-ScheduledTask -TaskName 'BPLO Development Weekly Database Backup'
# Optional removal of the task definition only, when authorized:
Unregister-ScheduledTask -TaskName 'BPLO Development Weekly Database Backup' -Confirm
```

Backups: `C:\BPLO_DB_Backup\BPLS_Dev_YYYY-MM-DD_HHmmss_fff_unique.bak`. Logs: `C:\BPLO_DB_Backup\logs\backup.log`. No old backup deletion or retention limit. Disk use grows; retention must be separately approved.

## Permissions and identity

SQL Server writes the backup as `NT Service\MSSQL$SQLEXPRESS`, not as the logged-in user. When the folder is first created, the script adds Modify for that exact service account on this folder and children. No Everyone grant. Existing folders are not automatically repermissioned; inspect their ACL and, if needed, grant only the configured SQL service identity Modify on the dedicated backup folder. The scheduled identity also needs folder creation/log/file-read access and SQL backup/verification permissions. The approved task identity JOHN\jmere successfully backed up BPLS_Dev using S4U and existing local SQL access. No SQL permissions were added. The development task is disabled after testing. Do not grant broad sysadmin rights just to fix backup execution.

Express Edition 17.0.1000.7 was inspected. Compression is omitted. SQL uses COPY_ONLY, CHECKSUM, NOINIT, STATS=10 with a fresh filename; it never intentionally overwrites a prior backup. VERIFYONLY verifies readability/integrity checks, not a full test restore or recovery guarantee.

Failures return nonzero; successful verified backup returns zero. Connection, folder, SQL, disk and verification errors are reported. If the folder itself is inaccessible, the error goes to PowerShell stderr and Task Scheduler's result because no folder log can be written. No passwords are configured or logged.

## Schedule changes and future handoff

Change BackupDay/BackupTime in configuration and update the registered trigger in an administrator terminal; changing JSON alone does not change an existing trigger. A missed run starts when Task Scheduler permits after Windows starts; it cannot back up while the machine is powered off. WakeToRun is disabled. Enabling wake would need host hardware/power-policy support and separate approval.

For future production deployment: copy reviewed scripts to an administrator-controlled host folder, configure the actual local instance/database/service identity, authorize an unattended account with appropriate SQL and folder access, then register a separately named production task after explicit deployment approval. The development registration script intentionally refuses production; it must not be used unchanged. The backup script has an explicit AllowConfiguredDatabase switch for an administrator-approved future database. Do not pass that switch during development. Prefer Windows integrated authentication; if SQL authentication is required, use an account-bound protected credential store and never passwords in JSON, scripts, arguments, source control or logs.

Inspect the newest backup with `Get-ChildItem C:\BPLO_DB_Backup -Filter *.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1`, inspect the log, and have an authorized SQL account run RESTORE VERIFYONLY FROM DISK against that file. Never run RESTORE DATABASE against a live database as a backup check.

No BPLO workflows, schema, or business records are modified by these scripts. SQL backup history metadata is naturally recorded by SQL Server.
