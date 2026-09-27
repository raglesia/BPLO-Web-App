# Development database

The web migration uses `BPLS_Dev` on the local `SQLEXPRESS` instance. It is separate from `Masinloc_BPLS`, which this setup does not access.

`BPLS_Dev.initialize.sql` is a snapshot of the table creation, rental-rate seed, and column migration SQL in `BusinessPermitLicensingSystem/Database/Database.cs`. It refuses to run unless the current database is `BPLS_Dev`. If the desktop initializer changes, regenerate or review this snapshot before using it again.

The development database contains three synthetic authentication accounts:

- `dev_pbkdf2_a` — Dev Test One
- `dev_pbkdf2_b` — Dev Test Two
- `dev_legacy_sha256` — Legacy Test User; its hash was upgraded to PBKDF2 during verification

Random test passwords are stored only in the local uncommitted file `%TEMP%\BPLS_Dev.test-accounts.json`. The web project's `ConnectionStrings:BPLS` setting is stored in .NET user secrets and points to `BPLS_Dev` with Windows authentication. No SQL password is in the repository.

`BPLS_Dev.samples.sql` adds three fake profiles and three fake vehicles for read-only count checks. It refuses to run in any other database. Profile occupancy dates are blank and statuses are `Unverified`, so these samples do not create billing history.

Do not point migration development at a real BPLO database without identifying the environment and reviewing the operation first. The web dashboard only reads profile and vehicle counts; it does not run the desktop dashboard's billing, penalty, or vehicle-reset operations.

## Phase 4 profile work

The web profile list, details, create, and edit pages use `BPLS_Dev` in Development mode. The profile service checks the connected database name before reading or writing. Test submissions added synthetic `SIN-2026-0001` through `SIN-2026-0004`; these can be discarded with the development database.

The desktop form stores `MonthlyRental` as the selected flat rate or size times rate, plus an enabled additional charge. It also stores `AdditionalCharge` separately. This may double count charges in later billing code. Phase 4 preserves the stored values and leaves billing rules for review in their own phase.

The desktop form offers `Paid` during profile entry, but that choice calls payment processing. Phase 4 allows new profiles as `Unverified` or `Unpaid` and keeps an existing `Paid` status unchanged during nonpayment edits. The web form does not record payments or create billing rows.

The schema has no rowversion column. Concurrent profile edits currently use last-write-wins behavior. New SIN allocation uses a serializable SQL transaction with update/range locks around the existing yearly `MAX(...)+1` format.

## Phase 6 payment work

See `PHASE6.md` for the approved base-rent billing rule, the `BPLS_Dev` rent-basis marker, preserved legacy rows, payment transaction, tests, and the unresolved cross-module OR-number policy.

## Phase 7 vehicle payment work

See `PHASE7.md` for vehicle fee drafts, VIN/year eligibility, transactional payment, the separate OR namespace, tests, and the remaining production-policy question.
