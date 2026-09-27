# Phase 11 functional audit — 26 September 2026

Scope: the Razor Pages application and `BPLS_Dev` only. WinForms is the behavioral reference and was not edited. This is a development audit, not approval for operational use.

## Rental rates and audit trail

WinForms `RentalRatesForm` lists sections with `PerSqm` or `Flat`, adds sections, and updates existing rates; it has no delete. Active rates must be positive. The desktop writes `AddRate` or `UpdateRate` audit rows after a successful rate change. The web now lists current rates and supports the same add/update operations with server validation, parameterized SQL, and acting-user audit rows in the same transaction as the change. It does not delete rates.

`ProfileService` calculates rent using the current rate when a profile is created or when an edit changes the section, stall size, or additional charge. Changing a rate alone does not rewrite existing `Profiling.MonthlyRental` or any `MonthlyBilling` snapshot. Future billing of an unchanged existing profile uses its stored rent. The new admin regression test proves old profile rent and bill values survive a rate update while a newly created profile uses the new rate.

WinForms audit viewing separates login/logout from other actions and displays date, user, action, SIN/reference, and details. The web page `/AuditTrail` keeps those categories, adds simple text search and 50-row pagination, and has no mutation handler.

## WinForms → web feature inventory

| Original area | Web status | Evidence or difference |
|---|---|---|
| Login/logout | Migrated | Cookie per browser; PBKDF2 and legacy SHA-256 accepted. |
| Account creation | Deferred intentionally | No public web registration or user administration pending an authorized account-creation policy. |
| Dashboard/navigation | Replaced intentionally | Counts and links only; no page-load billing, penalty, or annual reset. |
| Stall-owner list, search, detail, create, edit | Migrated | SIN generation and stored rent calculation retained. |
| Rental rates | Migrated | Add/update, no delete. |
| Monthly billing and penalties | Migrated with approved correction | First month after occupancy, missing months, base rent plus additional charge plus 25% base-rent penalty after the 20th. |
| Stall payments and history | Migrated | Exact bill links, authoritative amount, acting user, audit. |
| Vehicle list/detail | Migrated | Permit history and fee drafts visible. |
| Single-vehicle create/edit | Migrated in Phase 11 audit | Desktop fields and VIN identity retained; web allocates VIN on save and keeps history unchanged on edit. |
| Vehicle fee draft and payment | Migrated with policy pending | Draft total is authoritative; BPLO must confirm this rule. |
| Archive/restore | Migrated | Both record types; web adds a vehicle-restore audit row intentionally. |
| Audit Trail | Migrated | Read only. |
| Profile and vehicle CSV/XLSX import, export | Migrated | Row validation and active/search export. |
| Rental and monthly collection reports | Migrated | Print pages and monthly Excel. Desktop vehicle report button has no active handler, so no web vehicle report. |

Account administration is intentionally deferred, so office account provisioning needs an approved path. Full functional completion still requires end-to-end validation and BPLO policy decisions.

## Regression and manual checks

The full solution builds. The focused console suite passed with `--database --payments --vehicles --archive --transfer --reports --admin` against `BPLS_Dev`; the admin subset was rerun after adding vehicle entry/edit. It checks monthly boundaries, concurrent/idempotent billing, Unverified and archived exclusions, corrected rent/penalty math, old billing snapshots, exact payment links, duplicate OR and concurrent payments, rollback, vehicle VIN/year and OR safeguards, annual eligibility, archive/restore and history retention, import mixed rows and formula-safe exports, report totals/read-only behavior, rate/audit behavior, and individual vehicle add/edit with duplicate-plate and audit checks. Synthetic test data is retained in the development database.

Manual HTTP checks on the Phase 11 local preview verified PBKDF2 and legacy SHA-256 logins, user identity on the home page, rental and audit pages, report protection, logout, and two independent sessions (logging out one leaves the other authenticated). The final preview also verified vehicle form navigation, antiforgery token, invalid-field response, and successful create through the page. No public registration route exists. The web uses request claims for payment/audit ownership, not user IDs supplied by a form. Authentication is required by the Razor Pages folder convention. State-changing page handlers are POST and Razor forms receive antiforgery tokens. No shared static login state was found.

Profile list/search/pagination, detail, create/edit, SIN allocation, duplicate handling, occupancy and Unverified behavior were reviewed in the page/service code; the rate test creates profiles and validates calculation, while archive and transfer tests exercise profile boundaries. **Concurrent SIN creation and full browser-driven profile form validation are not covered by an automated end-to-end test.** This remains a focused hardening item.

## Navigation, forms, errors, empty states, security

The home page now links directly to live modules and the rental-rates nav link resolves to its real page. Old `/Modules/{module}` links redirect to the corresponding migrated page. No migrated page requires a manually typed URL. The unused template privacy page and placeholder content were removed. Forms use model validation and POST/redirect/GET after successful writes; write services recheck business rules. Invalid input and duplicate actions produce user-facing errors while retaining form values. Search/list/report pages use empty collections or explicit no-results messages; the audit page handles zero rows. These paths should receive a browser automation pass for exact wording and keyboard behavior.

SQL calls use parameters. Financial totals and record ownership are calculated or derived on the server. Import handlers process uploaded streams without using client filenames as filesystem paths. No plaintext password or connection credential is in tracked web configuration. Cookie is HttpOnly, SameSite Lax, and Secure when HTTPS is used. Exception handling now routes unexpected errors to the generic error page even in local development; technical detail goes to logs. The app remains HTTP on the local preview and is **not** approved for office network access.

## Database mutation and schema inventory

Writes occur only for login legacy-hash upgrade and login/logout audit, profile create/edit/archive/restore, billing generation/penalties, stall payment, vehicle create/edit, fee draft/payment/archive/restore, imports, and rental-rate add/update. Report and export GET requests, dashboard, detail/list, and archive views only read. There is no permanent-delete business handler and no GET handler that generates billing, penalties, or payments. The database-health page only executes `SELECT 1`.

The development initialization script creates baseline tables when absent. Migration-relevant additions and constraints are:

| Item | Reason/dependent feature | Desktop compatibility and semantics |
|---|---|---|
| `PaymentHistoryBilling` with composite PK and FKs | Exact paid-bill links for stall payments/reports | Additive; desktop can continue, though old payments may lack links. |
| `VehiclePermitFeeDrafts`, PK `(VIN, PermitYear)`, FK to vehicle | Persist confirmed draft for each year | Additive; desktop does not use it. |
| `Profiling.StartDate`, `Penalty`, `AdditionalCharge`, `IsArchived` | Occupancy, billing, archive | Existing desktop-compatible fields; defaults do not infer unknown historical occupancy. |
| `Users.Position` | Existing user identity display | Desktop-compatible; not a role. |
| `VehiclePermits.PermitStatus`, `PermitYear` | Current permit state | Desktop-compatible fields. |
| `MonthlyBilling.WebRentBasis` nullable | Distinguishes base-only web bills from ambiguous older rows | Additive; NULL remains review-needed, no historical rewrite. |
| `UX_VehiclePermitHistory_VIN_PermitYear` | One payment per vehicle/year | Additive constraint; can reject a desktop duplicate attempt, without rewriting existing rows. |
| `UQ_MonthlyBilling(SIN, year, month)` and existing OR unique keys | Duplicate period/receipt protection | Part of development baseline, not newly introduced by Phase 11. |

No schema change was applied in Phase 11. All scripts explicitly target `BPLS_Dev`; no other database was connected to or changed during this phase.

## Business decisions and remaining risk

1. **Vehicle payment source:** the saved fee draft is the web's authoritative payment amount. BPLO confirmation is needed before operational use.
2. **OR-number scope:** stall and vehicle OR numbers are currently unique within their separate ledgers. BPLO must confirm whether both share one receipt series.
3. **Ambiguous historical billing:** older rows with unknown rent basis display `Review needed`. Assess real data later; do not infer or silently recalculate.
4. **Account provisioning:** no public account creation is intentional; BPLO needs an approved office account-admin workflow.

The focused suite does not replace an end-to-end browser pass, especially for profile and vehicle form errors, empty states, concurrency of SIN/VIN creation, and full import/export navigation. Next phase: **Functional Hardening / End-to-End Testing**, including explicit BPLO decisions. Deployment is outside this recommendation.
