# QA-01 Acceptance Report

**Project:** Municipality of Masinloc BPLO Web App  
**Date:** October 5, 2026  
**Scope:** configurable web database targets, schema-v1 installation/validation, isolated staging acceptance  
**Result:** QA-01 hardcoded-target blocker resolved for the validated schema-v1 path. Official deployment has not occurred.

## 1. Original defect

Authentication, billing, profiles, vehicles, reports, administration, archives, transfers, dashboard and database health checks explicitly required BPLS_Dev. A production connection-string change alone could not work.

## 2. Implemented correction

A shared `DatabaseConnection` guard now owns all web SQL connection creation. Thirteen service/page entry points use it. Existing queries, calculations, transaction boundaries, routes, cookie names/lifetimes, access rules, UI and login workflows remain in place. Non-development cookie protection scopes are isolated by environment, SQL identity and database; Development keeps its existing protection scope.

Development still requires BPLS_Dev. Staging/Production require explicitly approved server endpoint, database and SQL Server identity. Actual host environment selection overrides attempts to change `Database:Environment` through settings. System databases, unexpected targets, attached files and user-instance connections are rejected. Each connection verifies its actual database; non-development connections also verify actual SQL Server identity.

The app does not initialize or migrate a database on startup. Deployment settings are documented in this folder and the repository README.

A local cross-instance check reproduced shared-key cookie acceptance between the QA runtime and Development. The non-development protection scope corrected it. Four bidirectional checks passed: Production QA to Development, Development to Production QA, Production QA to Staging, and Staging to Production QA. Valid logins and the 19 Production routes passed afterward.

## 3. Schema package

The guarded Create/Validate entry point is `Initialize-BPLODatabase.ps1`.

Verified schema v1 includes:

- 14 tables with 97 required columns.
- Required types, lengths, precision, scale, nullability, identity, defaults and text collation.
- 43 index, primary-key, foreign-key, check and SQL module definitions.
- Monthly SIN/year/month uniqueness and annual vehicle/year uniqueness.
- Global OR normalization, unique receipt registry and both ledger triggers.
- Historical OR conflict, blank OR, missing/mismatched receipt linkage and orphan-reservation checks.

The package exports schema only. It does not seed users, rates, profiles, bills, payments or audit records. Fresh schema creation and validation are transactional. CREATE DATABASE itself is outside that transaction; a failed installation can leave an empty database for review. No automatic overwrite/drop/restore is performed.

Existing databases are validated read-only. Exact required object definitions/names are deliberate. An older or different schema requires a separately reviewed incremental migration; no universal automatic upgrade of an uninspected database is claimed.

## 4. Test results

| Gate | Result |
|---|---|
| Database target/staging suite | 138 PASS assertions: 60 shared pure checks, 47 QA01 checks, 31 global receipt checks |
| Existing development regression suites | 461 PASS assertions; no failures |
| Official baseline suite | 74 PASS assertions, including shared pure checks |
| Staging HTTP acceptance | 19 authenticated module routes passed, plus login/logout and rendered audit checks |
| Actual Production runtime against synthetic QA database | Same 19 routes and account/audit checks passed |
| Cookie isolation | Four bidirectional cross-environment checks passed; valid login preserved |
| Fresh schema installation | Passed on two separately created empty QA databases |
| Full schema compatibility | Passed required column/default/collation and structural checks |
| Existing target protection | Create refused an existing database |
| SQL server target protection | Wrong server identity refused before creating a database |
| Raw schema protection | Direct execution without approved session context refused |
| Transaction rollback | Injected failure rolled back all schema tables; empty owned rollback database removed afterward |
| Production environment protection | Production startup refused BPLS_Dev despite an attempted Development configuration override |
| Backup independence | Backup succeeded after stopping the staging app |
| Backup verification | RESTORE VERIFYONLY WITH CHECKSUM passed |
| Recovery | Restore into separate QA databases matched every stored field across all 14 source tables |
| Full Release build | Passed, 0 errors; final incremental build had 1 existing package warning (earlier full compilation had 63 existing WinForms/package warnings) |
| Release publish | Passed; local package created outside the Git repository |
| Main localhost | Available at http://127.0.0.1:5199; login returned HTTP 200 |

### Functional coverage

Verified staging account creation/authentication, invalid-password rejection, rental-rate writes/reads, profile creation and rate calculation, bill generation and duplicate-period prevention, 25% penalty, rental payment, legacy December baseline, Treasury verified arrears and settlement, vehicle creation/assessment/payment, cross-ledger OR rejection, rental/monthly/vehicle reports, archive/restore, export and session/business audits.

Global receipt checks covered same-ledger and cross-ledger concurrent payments, one clean winner, friendly duplicate errors, controlled downstream failures, rollback/retry, direct-ledger insert enforcement, normalization, linkage integrity and fixture cleanup.

The existing legacy workflow prepares the December baseline obligation before its start date. Tests verified that behavior and that no historical unpaid regular obligation was generated. No billing rule was changed to satisfy a test.

## 5. Development data preservation

A fresh snapshot was taken before running development fixture suites. Only QA-created rows were removed afterward. All stored rows across 14 tables matched their original contents, not merely their counts.

| Table | Before / after |
|---|---:|
| AppSettings | 1 / 1 |
| Users | 123 / 123 |
| RentalRates | 14 / 14 |
| Profiling | 538 / 538 |
| MonthlyBilling | 538 / 538 |
| StallOwnerArrears | 0 / 0 |
| PaymentHistory | 0 / 0 |
| PaymentHistoryBilling | 0 / 0 |
| PaymentHistoryArrears | 0 / 0 |
| VehiclePermits | 116 / 116 |
| VehiclePermitHistory | 42 / 42 |
| VehiclePermitFeeDrafts | 60 / 60 |
| AuditTrail | 1160 / 1160 |
| ReceiptRegistry | 42 / 42 |

The official 538 profiles, billing baseline, existing user records, audit rows and financial amounts were preserved. SQL identity counters can advance during insert/delete fixture tests; the stored-row comparison does not claim identity counters were rewound. The next safe official SIN allocation was separately tested.

## 6. Isolated QA resources

Only synthetic QA data was used outside BPLS_Dev:

- BPLS_Staging_QA01 — functional staging fixtures.
- BPLS_Staging_QA01_InstallCheck — empty schema installation proof.
- BPLS_Staging_QA01_Recovery — isolated restored synthetic staging backup.
- BPLS_QA01_ProductionAcceptance — isolated synthetic restore used with the actual Production runtime.

These are QA databases, not official deployment databases or data packages. The temporary rollback-test database was removed after proving it was empty.

Backup created:

`C:\BPLO_DB_Backup\BPLS_Staging_QA01_2026-10-05_093552_350_fd3ab842.bak`

VERIFYONLY passed; backup size was 6,775,296 bytes. Backup ran manually with the staging app stopped. No scheduled task was registered, enabled or changed. Current BPLO/BPLS task inventory returned no matching task; this report does not assert a currently installed development task is disabled. Existing backup-folder permissions were not changed by this test.

The temporary QA web instance is stopped after checks; the main Development localhost instance remains running.

## 7. Remaining official-host acceptance

QA-01 is resolved for the tested configuration/schema-v1 path. This does not certify the actual host PC, an unknown existing database, or the full deployment:

1. Approve the actual server endpoint, SQL identity and database name.
2. Validate an isolated copy if using an existing database; review any incompatible schema migration.
3. Approve and reconcile official data transfer.
4. Install and verify IIS, trusted HTTPS/SQL certificates, restricted identities/permissions and LAN access.
5. Configure persistent protected authentication keys.
6. Prepare/register the separately approved production backup task and verify recovery on the host.
7. Complete three-user host acceptance before official opening.

No connection to or modification of Masinloc_BPLS occurred. No official deployment, production task registration, SQLite demo change, or GitHub push was performed in this task.

## 8. Evidence location

Local raw evidence is outside the repository under `outputs/qa01/`: staging/development test output, official baseline output, HTTP results, Release build/publish output, installer rejection checks, restore verification and development preservation snapshots. QA credentials and database snapshots are not included in the publish package or Git source.

## 9. Rollback

Revert this application refactor to restore development-only target behavior. BPLS_Dev required no schema change. Schema Create rolls back on failure; successful QA databases remain isolated for review. Any official database restore, deletion or incremental migration requires separate review and authorization. Previous business records are not reset by application rollback.


