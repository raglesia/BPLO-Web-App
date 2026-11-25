# BPLO Current UAT release candidate

25 November 2026 · UAT approved · Production deployment pending separate approval

| Baseline | Revision |
|---|---|
| UAT release candidate tag | `bplo-current-uat-rc1` |
| Verified source and test baseline | `e29338fdfcf9e8e071524797b454438928b6e90f` |
| Last application change included | `56f75084c02ca993a7abde28db1eb824e245a750` |

Build the web package from the tagged revision in a clean checkout. Record the resolved commit, package manifest and SHA-256 digest with the UAT evidence. Advance the same accepted package to production review. The feature freeze covers the Current UI, payment workflows, financial rules and schema. Any verified defect needs a reproducible report and separate approval before application changes. If a fix is approved, validate and issue a new candidate tag. Keep this baseline available.

## Staff acceptance checklist

Use the isolated UAT site, individual test accounts and synthetic stall, vehicle and receipt records. Use separate synthetic records for each tester or coordinate who owns shared scenarios. Record the date, workstation, browser, tester and candidate revision. Mark each item Pass, Fail or Blocked. For failures, capture steps, expected behavior, actual behavior and synthetic record references. Keep sign-in details and municipal records out of evidence.

| Done | Staff task | Expected result |
|---|---|---|
| [ ] | Sign in, open Office Overview, and sign out. Try reopening a protected page after sign-out. | The office seal and name appear, navigation opens the correct workspace, and protected pages require sign-in again. |
| [ ] | Open the collection chart and monthly register. Tab to a month and select it. | Stall rent, vehicle permits and the total are available without a mouse and match the register. An empty month shows a clear state. |
| [ ] | Find a stall owner and a vehicle by name and identifier. Change the search, clear it, paginate and return from a record. | Results update to the active query, no matches are explained, and the expected record and list context are available on return. |
| [ ] | Edit a synthetic profile or fee draft. Try leaving with edits before saving, cancel the warning, and save. | The page warns about edits. Cancel preserves input. Validation identifies invalid fields and a successful save shows confirmation. |
| [ ] | Open stall Billing & Payments for an unpaid profile. Verify the occupancy date, first billable period, rent, applicable charges and penalties against a QA worksheet. | Confirm the exact due periods and total before recording a synthetic OR. After settlement, only the intended periods are paid and history shows the exact OR, amount and recorder. |
| [ ] | Check unverified, archived and Treasury-verified fixtures. Enter authorized synthetic arrears, review the resulting totals and check a duplicate attempt. | The UI respects eligibility, archived history stays available, and recording verified arrears alone does not record payment. A duplicate arrears entry is rejected. |
| [ ] | Open the vehicle workspace. Enter fees, compare the running total with the worksheet, save the draft, then record a synthetic OR. | Draft saving does not settle payment. Payment uses the saved assessment for the correct year. A paid assessment is locked and history shows the recorded OR and amount. |
| [ ] | Review Audit Trail after both settlements. Have the UAT lead check a duplicate OR or repeat submission with separate fixtures. | The correct user is attributed. The vehicle audit shows VIN, OR, amount, year and payment time. A rejected duplicate creates no second payment or success audit. |
| [ ] | Prepare and reprint a Mayor’s Permit from an eligible paid vehicle. | The permit has its assigned number, the expected vehicle details and year, and the same number on reprint. |
| [ ] | Preview the rental assessment, settled rental report, vehicle payment report and Mayor’s Permit. Run a monthly report and check Excel output. | Before-payment and recorded-payment documents are distinguishable. Dates, totals and selected records match the fixtures. Excel opens and agrees with the on-screen report. |
| [ ] | Print representative documents on the office printer using the approved A4 settings. Include a multi-record report. | Seal, headings, rows, totals, permit number and signature areas are legible and unobstructed. Page breaks and margins are acceptable. |
| [ ] | Repeat key tasks using keyboard navigation and any assistive technology used by staff. Check compact navigation on a narrower window. | Focus stays visible, dialogs open and dismiss with predictable focus, alerts are perceivable, and essential actions remain reachable. |

UAT lead: ____________________ · Date: __________ · Package SHA-256: ____________________

Accepted by licensing: ____________________ · Treasury: ____________________ · Reports: ____________________

Open defects or blocked checks: ______________________________________________________

## Production deployment checklist

Perform these steps only after a separate deployment authorization and an approved maintenance window. Use the existing [host setup guide](operations/setup/README.md) and [database validation guide](operations/database/README.md). The guided installer is for a fresh local IIS/SQL host. Use the approved host-specific change plan for an upgrade.

### Before cutover

- [ ] Record staff acceptance and unresolved items. Obtain owner approval for the tested package, deployment window, smoke checks and rollback lead.
- [ ] Verify the package manifest, commit and digest against UAT. Confirm the intended host supports .NET 10, IIS or the approved hosting configuration, trusted HTTPS, DNS, LAN access, and persistent Data Protection keys. Confirm the application identity and database access.
- [ ] Confirm the approved connection endpoint, physical SQL Server identity and database name. Use protected Production configuration and trusted SQL encryption. Validate the release schema on an isolated authorized copy. Confirm receipt constraints, assessment snapshots and permit sequencing. This candidate needs no schema migration or audit backfill.
- [ ] Verify the authorized backup schedule, pre-cutover backup, isolated restore rehearsal and retention. Confirm the rollback package and compatible configuration are available.
- [ ] Notify staff, freeze writes and drain in-flight requests. The Treasury lead should note any unsettled or interrupted OR requests for reconciliation.

### Cutover and acceptance

- [ ] Deploy the exact accepted package through the approved procedure. Keep the approved database and persistent key location. Record artifact digest, host, window and operator.
- [ ] Verify trusted HTTPS from office workstations, sign-in and sign-out with authorized staff, service restart and session behavior, read-only navigation, report previews and absence of startup or SQL errors. Keep payment-writing tests in UAT.
- [ ] Check the backup task and logs on the target host. Confirm staff and technical sign-off before reopening work. If a smoke check fails, keep writes paused and follow rollback.

## Database backup and restoration

The DBA owns execution. See [backup operations](operations/backup/README.md). The development registration and schedule are development-only. Production needs an approved backup identity, protected location, schedule, retention policy, recovery-point target and recovery-time target.

### Backup

1. Confirm the approved SQL Server identity and database name using the DBA connection. Confirm free space and that the SQL Server service account can write to the dedicated backup directory. Confirm a protected second-copy destination and access controls.
2. Run the approved production backup job. If using the reviewed repository script, supply the protected host-specific `-ConfigPath` and use `-AllowConfiguredDatabase` only for the authorized target. Ensure the configuration includes the expected physical server identity. Use a unique backup filename and preserve earlier sets.
3. Verify the job exit code, nonempty file and successful `RESTORE VERIFYONLY ... WITH CHECKSUM`. Record source identity, backup type, file identifier, checksum or digest, time, operator and verification result. Copy the set to the protected second location and verify that copy. Keep production backups and logs out of Git and UAT.
4. Keep the backup chain and any required encryption certificates or keys under the approved recovery plan. A file verification is only one check. Record the date of the last successful isolated restore.

### Isolated restoration rehearsal

1. Choose the recovery point and required sets from the approved plan. Confirm a compatible isolated SQL Server instance, a new database name, unique destination files and restricted access. No live application should point at the rehearsal database. If the backup contains real data, use the approved restricted environment.
2. Verify the selected media with checksum. Inspect backup headers and logical files. Restore to the **new** database with explicit file mappings. If the recovery plan includes differential or log sets, apply the approved chain in order and recover only at the intended point. Stop on an identity, chain, file or validation error.
3. Run database integrity and guarded schema checks on the restored copy. Check critical billing, receipt links, audit counts, assessment snapshots and permit sequences against the chosen recovery point. Record actual restore duration and whether the recovery targets were met. Follow the approved access and disposal policy for the copy.
4. A production restore requires separate incident authorization. Freeze writes, preserve the affected database and relevant backups, and reconcile payments, ORs, audits and permit numbers against Treasury records before reopening. Do not overwrite a live database as a rehearsal or as part of application rollback.

## Application rollback

1. The release lead declares rollback if startup, trusted access, database targeting, read-only reporting or reconciliation fails. Keep write traffic paused and drain requests. Record failed revision, symptoms, time and affected ORs. Preserve logs and current configuration securely.
2. Confirm the preceding approved application package and configuration work with the **current** database schema. Restore the application package and compatible configuration through the approved host procedure. Preserve the target database, Data Protection keys, database backups and transaction evidence.
3. Restart the site and repeat trusted HTTPS, sign-in, database identity, read-only payment history, audit and report checks. Confirm the preceding revision is active. Treasury reconciles interrupted ORs against payment history and the receipt register before any retry.
4. Reopen writes only after the technical and Treasury leads sign off. If database damage is suspected, escalate to the DBA recovery procedure under separate authorization. Record the rollback outcome and any outstanding defect.

## Release notes and remaining limits

- **Current UI.** Midnight + Ice, Office Overview and the municipal seal remain the reference. Staff get clearer form controls, focus states, compact results, consistent statuses and easier reporting filters.
- **Interaction.** Forms announce processing, guard repeated posts and warn about meaningful unsaved changes. Live search handles missing and stale responses and restores keyboard focus when results replace a focused link. Fee previews and collection breakdowns use existing application data.
- **Financial verification.** The Release build passed with existing WinForms warnings. Isolated SQL Server passed 231 HTTP checks, 237 release assertions and 109 additional integration checks. Tests covered authoritative amounts, duplicate and concurrent receipts, transaction rollback, payment history, snapshots and permit numbering. Representative rental, vehicle and Mayor’s Permit A4 output matched before and after.
- **Vehicle audit.** Each new successful vehicle payment writes one Audit Trail entry in the same transaction. It records the authenticated user, VIN, OR, paid amount, timestamp and permit year. Historical audits were not backfilled.
- **Open acceptance.** Staff workflow sign-off, production host and schema validation, backup/restore rehearsal, actual A4 printer checks and assistive-technology checks remain pending. The historical 538-record cutover suite was skipped because it requires its original migration dataset. No application defects were reproduced in the applicable automated gates.

Evidence: [release readiness](RELEASE_READINESS_2026-11-25.md) · [stabilization](UI_STABILIZATION_QA_2026-11-25.md) · [vehicle payment audit](VEHICLE_PAYMENT_AUDIT_QA_2026-11-25.md)
