# QA-01 — Configurable Database Target and Schema v1

## Status

QA-01's hardcoded BPLS_Dev application restriction is resolved for the verified schema-v1 installation path. Local staging and an actual Production runtime using synthetic QA data passed. This is not evidence of official IIS/LAN deployment or compatibility with an uninspected existing production database. Masinloc_BPLS was not connected to or changed.

## Application configuration

All web database access goes through `BusinessPermitLicensingSystem.Web/Data/DatabaseConnection.cs`.

- Development remains restricted to BPLS_Dev. Its default approved connection endpoint is `.\SQLEXPRESS`; explicitly configure `Database:ApprovedServer` if using another development endpoint.
- Staging and Production require an explicit approved database, connection endpoint, and SQL Server identity.
- The actual ASP.NET host environment controls selection. `Database:Environment` cannot override it in the running application.
- Missing settings, unexpected connection targets, system databases, attached database files, and user-instance connections fail closed.
- Production refuses BPLS_Dev and databases named with the BPLS_Staging prefix.
- Every opened connection verifies DB_NAME(). Non-development connections also verify SERVERPROPERTY('ServerName') against the configured identity.
- Staging/Production cookie protection is scoped to environment, actual approved SQL identity and database. Sessions cannot authenticate across those targets; Development retains its existing scope.
- The application never creates databases or applies schema changes automatically.

Set these through protected host configuration, using the actual approved values:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__ApprovedServer=HOST\INSTANCE
Database__ApprovedServerIdentity=ACTUAL_SQL_SERVER_NAME\INSTANCE
Database__ApprovedDatabase=APPROVED_DATABASE
ConnectionStrings__BPLS=Server=HOST\INSTANCE;Database=APPROVED_DATABASE;Integrated Security=True;Encrypt=True;TrustServerCertificate=False
```

`ApprovedServer` must match the connection string's Data Source. `ApprovedServerIdentity` must match the SQL value returned by `SELECT CONVERT(nvarchar(256),SERVERPROPERTY('ServerName'));`. Aliases and listener endpoints can differ from the physical identity; approve both deliberately. Use trusted certificates on the official host. The local QA trust override is not an official deployment setting.

Existing .NET user secrets for Development continue to work. Do not copy development user secrets or QA credentials to the host.

## Schema installation package

Files in this folder:

- `Initialize-BPLODatabase.ps1`: guarded Create/Validate entry point using Windows integrated authentication.
- `schema-v1.sql`: schema-only definition for 14 tables, receipt normalization function, indexes, checks, foreign keys, and receipt triggers.
- `schema-v1.columns.json`: required 97 column definitions, defaults, and text collations.
- `schema-v1.database.json`: verified database collation and package version.
- `schema-v1.structure.json` and `schema-structure-query.sql`: required 43 structural safeguards/module definitions.

The schema has no profile, rental-rate, user, bill, payment, OR, or audit seed data. Official data transfer is a separately reviewed cutover step.

### New approved database

Run under the approved setup administrator, after installing SQL Server. Replace placeholders; these are not runnable host-specific settings:

```powershell
.\Initialize-BPLODatabase.ps1 `
  -Server 'HOST\INSTANCE' `
  -Database 'APPROVED_DATABASE' `
  -ExpectedServerIdentity 'ACTUAL_SQL_SERVER_NAME\INSTANCE' `
  -Mode Create `
  -AllowConfiguredDatabase
```

The script checks the connected server identity before creating the named database. Create refuses any existing database. Schema creation and compatibility validation run in one transaction. Failure rolls back schema changes; because CREATE DATABASE is outside that transaction, an empty database can remain for administrator inspection. The script never drops it automatically.

Direct execution of `schema-v1.sql` without the approved session context fails closed. Use the entry point, not a manual blanket search/replace of development scripts.

### Approved restored/existing database

Take a verified backup and validate an isolated copy first:

```powershell
.\Initialize-BPLODatabase.ps1 `
  -Server 'HOST\INSTANCE' `
  -Database 'APPROVED_DATABASE' `
  -ExpectedServerIdentity 'ACTUAL_SQL_SERVER_NAME\INSTANCE' `
  -Mode Validate `
  -AllowConfiguredDatabase
```

Validate is read-only. It verifies required column types/nullability/identity, exact required indexes, checks, foreign keys and receipt module definitions, enabled/trusted constraints, period uniqueness, and normalized OR conflicts. Exact object definitions/names are intentional: an unfamiliar schema requires review. The validator does not silently repair, rename, reset, or overwrite objects or data.

An older/incompatible existing database needs a separately reviewed incremental migration based on its actual schema and data. No universal in-place migration is claimed. Do not run development initializers, sample loaders, baseline resets, or fixture suites against an official database.

### Retry and rollback

- Rerun Validate safely after successful Create.
- Create never overwrites an existing database.
- On failed Create, inspect the empty/rolled-back target and authorize its disposition before retrying.
- Reverting the application change restores the earlier development-only behavior; it does not modify database contents.
- No automatic database restore or deletion is part of rollback.
- Official installation, data transfer, IIS identities, certificates, and production backup-task registration remain host-specific acceptance steps.

## Verification evidence

See `QA01_ACCEPTANCE_REPORT.md`. QA databases contain only synthetic fixtures and must never be used as official deployment data. The historical development SQL scripts retain their BPLS_Dev safeguards. The SQLite demo was not modified.


