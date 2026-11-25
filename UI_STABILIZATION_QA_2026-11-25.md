# BPLO Current stabilization and verification

25 November 2026

## Scope and test target

The approved Current UI was verified against the original ASP.NET Core application on local SQL Server. The fresh staging database was created from the guarded schema package, which validated 115 columns and the required receipt and billing safeguards before fixtures were inserted. The database contains synthetic records only. Both web builds bound to loopback. The print comparison used the same settled records in pre-refinement commit `7bdb1ec` and the stabilized Current build.

## Results

| Check | Result |
|---|---|
| Current and pre-refinement web builds | Passed with zero errors or warnings. |
| Release QA | 233 assertions passed, including 60 database-free checks. Covered paid, unpaid, unverified, archived and Treasury-verified states, settlement totals, ledger history, linked bill periods, payment snapshots, receipt reservation, rollback and audit attribution for stall payments. |
| Duplicate payment and OR protection | Passed same-record concurrent attempts, shared OR races within each ledger and across ledgers, and rollback and retry. SQL checks found zero duplicate ORs, duplicate stall periods, duplicate VIN/year payments, unlinked reservations or snapshot amount mismatches. The read-only physical integrity check passed. |
| Additional integration suites | Three profile checks passed. Legacy billing, administration, transfer and database-target suites passed 106 further assertions in a disposable test copy pointed at the new QA database. |
| Live Edge workflows | 16 checks passed. Created a stall owner and vehicle, settled ₱750 and ₱726 respectively, opened receipt reports and prepared and reprinted a numbered Mayor’s Permit. SQL confirmed both paid states and the permit number. |
| Print comparison | Settled rental payment report, special vehicle permit payment report and issued Mayor’s Permit were each one A4 page before and after. All three print rasters matched pixel for pixel at 90 DPI. |
| Legacy HTTP harness | Reached 73 HTTP assertions before stopping at an outdated billing-page assertion. It expects “Billing history” at the old route. The existing compatibility route redirects to the canonical Billing & Payments workflow. The targeted Edge run verified the canonical route and payment actions. |
| Historical cutover fixture suite | Skipped. It requires the particular historical 538-record cutover state and its original account fixture. That dataset was outside synthetic-only testing. |

## Stabilization and source review

A delayed search response previously removed a focused result link and left focus on the document body. The shared live-search script now returns focus to the search input when it replaces the focused result. Edge verified this with an intentionally delayed response. It also verified that an older response cannot overwrite a newer query and that a failed request preserves the previous results and URL.

Review covered the Current source diff, the stylesheet cascade and the profile and payment submission handlers. Older component styles remain where their conditional states still depend on them. The final stabilization changes only focus handling. It preserves the design, server handlers, financial rules, print layout and database configuration.

## Server-side defect for separate approval

**Vehicle payment has no matching Audit Trail entry.** The completed vehicle payment has a paid ledger record, registered OR, immutable assessment snapshot and permit number. The audit query found the stall payment entry and no vehicle entry for the corresponding OR. The stall transaction explicitly inserts an audit row. The vehicle transaction writes the ledger, receipt link, snapshot and status without an equivalent insert. Adding a transactional vehicle audit event requires separate approval. No audit behavior was changed.

## Remaining acceptance

Physical printer output, production-host integration, the historical cutover fixture, and full assistive-technology coverage remain outside this verification. The disposable QA database was retained for inspection. No deployment was performed.
