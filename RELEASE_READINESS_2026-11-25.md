# BPLO Current release readiness

25 November 2026

**Disposition.** The web release passed the applicable automated gates on isolated SQL Server. Production readiness remains conditional on validating the intended host and database and completing staff acceptance. This pass did not deploy.

## Scope and Git status

- Application baseline and rollback candidate from this pass is `56f75084c02ca993a7abde28db1eb824e245a750`. The app, Current UI, financial rules, audit service and schema files match that revision.
- HTTP harness commit `f99b353af598f63bc7a7551acf7766522682daa2` was pushed to `origin/main` and verified against the remote-tracking ref. This report follows in a documentation-only commit.
- The test diff changes only the HTTP harness. No credentials, database files, QA data files, screenshots or build artifacts were staged. The pre-existing web `AGENTS.md` edit remains unstaged.

## Test results

| Gate | Result |
|---|---|
| Full solution Release build | Passed. The first offline restore lacked three legacy WinForms packages. Restore from NuGet.org resolved those packages. A clean Release build had zero errors and 64 existing WinForms warnings. The final incremental Release build had zero errors and one existing WinForms package warning. |
| Web dependency audit | NuGet.org reported no vulnerable direct or transitive packages for the web project at the time of the check. |
| HTTP regression | **231 passed, zero failed.** Ran through logout and second-session isolation. Coverage includes compatibility redirects and POSTs, antiforgery, account and record workflows, report selection, settled payment snapshots, stale and concurrent submissions, duplicate receipts, archive and restore, transfers, and read-only GET behavior. |
| Release SQL regression | **237 passed, zero failed.** Includes 60 database-free checks and 177 release assertions, covering payment and audit atomicity, rollback and retry, cross-ledger OR conflicts, snapshot integrity, and permit numbering. |
| Additional SQL integration | Three profile checks and 106 legacy billing, administration, transfer and database-target checks passed. Legacy suites with a fixed development target ran from a disposable, guarded copy against synthetic staging. Counts exclude repeated database-free assertions. |
| Schema and financial integrity | Schema validation confirmed 115 columns and required billing and receipt safeguards. SQL found zero duplicate or unmatched receipts, duplicate annual payments, or wrong audit counts on snapshotted vehicle payments. The read-only physical integrity check passed. |
| Historical cutover suite | Skipped. It requires the original migrated 538-record state and account fixture. A synthetic substitute would not validate that migration. |

The harness now creates in-memory synthetic account credentials, requires an explicitly named QA database, and uses the Philippine business clock. Stable form handlers and parsed route parameters replace obsolete button and query-string expectations. It exercises legacy compatibility alongside canonical payment pages. Valid numeric ORs reach the intended payment branches. A separate test confirms rejection of alphabetic ORs.

## Known issues and manual acceptance

No new application defect was reproduced in the applicable automated gates. Existing WinForms compiler and package warnings remain. Earlier A4 browser print comparisons remain applicable because report and print sources match the approved baseline. Physical printing, staff acceptance on office devices, full assistive-technology testing and production-host acceptance remain open.

Before release, have staff verify one stall rental and one vehicle permit workflow, including audit visibility, report preparation and print alignment on the actual printer. Use a dedicated QA environment for write and concurrency tests. Production smoke checks should use authorized accounts and read-only actions.

## Deployment and rollback precautions

1. Publish the approved web revision with the supported .NET 10 runtime. Verify IIS or the approved host, HTTPS, application identity, and protected database configuration. Confirm the configured server endpoint, physical SQL identity and database name agree.
2. Verify a current database backup and rehearse restoration separately. Run the repository’s guarded schema validator on an isolated copy of the intended target. Confirm it includes the approved release schema, including receipt safeguards, vehicle payment snapshots and Mayor’s Permit sequencing. This release requires no schema migration or historical audit backfill.
3. Stage and smoke-test the exact publish artifact before cutover. Schedule an office-approved window, drain in-flight requests and keep the preceding verified application package available. If a receipt request was interrupted, reconcile the OR and payment history before retrying.
4. Roll back application files and configuration to a verified compatible package if needed. Preserve committed payments, receipts, audits and permit numbers. Treat database recovery as a separate incident procedure requiring reconciliation and authorization.

See [database setup and validation](operations/database/README.md) and [vehicle payment audit verification](VEHICLE_PAYMENT_AUDIT_QA_2026-11-25.md).
