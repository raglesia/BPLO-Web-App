# Vehicle payment audit and compatibility verification

25 November 2026

## Change

Vehicle payment writes the established **Update Vehicle Permit** action to Audit Trail. The existing SIN column holds the VIN, as it does for vehicle record edits. The audit uses the authenticated recorder and the same timestamp as the payment ledger. Details include the Paid status, normalized OR, saved assessment amount and permit year.

The insert runs after the ledger, receipt, assessment snapshot and paid-status writes and before the transaction commits. An audit error rolls back the payment. Existing validation, eligibility, assessment and financial calculations, receipt and permit numbering, and stall payment auditing remain unchanged. There is no historical backfill or schema change.

The legacy HTTP harness now verifies that the old stall billing GET and billing-generation POST both reach the canonical Billing & Payments page for the same SIN. It still checks generated periods, dates and idempotence.

## Verification

- Built the web and regression projects with zero warnings or errors.
- Passed 237 release regression assertions, including 60 database-free assertions, on a fresh isolated SQL Server database. Three additional profile checks passed.
- Verified audit attribution and payment context for two recorders, one audit for a winning concurrent VIN/year payment, one audit in a shared-OR vehicle race, and an audit only when the vehicle wins a cross-ledger race. Rejected, archived and duplicate attempts did not add success audits.
- Forced the audit insert to fail after the other payment writes. Payment history, paid state and the audit rolled back. Retrying with the same OR committed exactly one payment and audit.
- Passed seven live Edge checks, including actual synthetic stall and vehicle settlements. SQL readback found one stall audit and one vehicle audit for the authenticated recorder, with matching ledger timestamps. Every vehicle payment with an assessment snapshot in that fresh database had exactly one matching audit.
- The retargeted legacy HTTP suite passed 98 HTTP checks, including both updated compatibility assertions. It then stopped at a separate pre-existing assertion for the old **Record full payment** label. The current browser flow used the visible **Record payment** action and verified settlement.

All mutating tests used a fresh synthetic-only QA database. Prior production and historical records were outside scope. No UI or deployment changes were made.
