# Setup-BPLO creation QA — October 5, 2026

## Delivered

Root batch launcher, configuration example, guided PowerShell fresh-host installer, package builder, setup instructions, and non-mutating guard tests. Transfer package: `outputs\BPLO-Setup-20261005.zip` in the workspace. Published framework-dependent Windows x64 app included; .NET 10 Hosting Bundle remains a host prerequisite.

## Executed checks

- All three setup PowerShell files parsed successfully.
- Eleven configuration-plan assertions passed: incomplete template, valid plan, four invalid/protected database targets, remote SQL, system destination, unrestricted firewall scope, invalid IPv4 and unsupported database mode.
- Release win-x64 publish succeeded. NU1900 warning: package vulnerability service unavailable through this environment's network; no successful online vulnerability scan is claimed.
- Package builder rejected pre-existing output destinations by design and generated SHA256 file manifest. Package excludes database/backup data, local setup configuration and Development settings.
- Git diff whitespace check passed; line-ending notices only.

## Preservation and limits

No Apply run, SQL connection, database/data changes, IIS change, Windows component installation, scheduled-task registration, firewall change, or official-host deployment was performed. No application workflow/UI changes were made. Backup configuration gained optional certificate-trust and actual-server-identity fields; existing configurations retain prior behavior, while installer-generated host configuration requires trusted SQL encryption.

The tests establish configuration guards and package/build validity. They do not establish an IIS installation, actual host SQL login privileges, certificate trust, data import, employee login, three-PC LAN use, or unattended backup acceptance. Those must be verified on the approved host. Partial Apply failures leave resources for review; no automated destructive rollback is implemented. Fresh schema contains no employee accounts or official profiles. Backup task is deliberately registered disabled until independent unattended execution passes.

Changes are local and have not been committed or pushed by this task.
