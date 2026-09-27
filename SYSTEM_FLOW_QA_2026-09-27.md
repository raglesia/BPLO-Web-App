# BPLO web system-flow QA — 27 September 2026

Scope: source review of the original WinForms application and the current web app; local testing against `BPLS_Dev` only. WinForms was not edited. No deployment or production database work occurred.

## WinForms → web workflow map

| Desktop source and sequence | Web sequence and assessment |
| --- | --- |
| `LoginForm` → `AccountCreationForm` → `DashboardForm` | `/Account/Login` → always-available `/Account/Create` → dashboard; authenticated `/UserAccounts` provides edit and reset. Permanent signup is the user's confirmed policy, so the QA brief's first-run-only closure does not apply. Position is descriptive, not an access role. |
| `ProfilingLists` → `ProfilingForm` → billing/payment in `Database.cs` → `PaymentHistory` → `ReportViewerForm` | Profiles search/create/details/edit → Billing and Payments → payment history → billing statement or Stall Owner Rental Payment preview. The web keeps corrected base-rent snapshots, one-time additional charge, base-only penalty, and Review Needed safeguards rather than desktop billing defects. |
| `VehiclePermitLists` → `VehicleProfiling` and permit history/detail → `Database.PayVehiclePermit` | Vehicles search/create/details/edit → fee assessment draft → annual payment → history → exact recorded-payment print view. Saving a draft does not pay. The web uses per-VIN/year history rather than a dashboard-triggered annual status reset. |
| `RentalRatesForm`, `ArchivedForm`, list import/export, `AuditTrail`, monthly/report forms | Rental Rates, separate Profile/Vehicle Archives, module-specific import/export, Audit Trail, and Reports. Context links and sidebar make the daily path shorter than reopening desktop dialogs. |

## Findings and corrections

- **Fixed:** the Billing and Payments link labeled “Printable Rental Report” led to a billing statement. It now says **Print Billing Statement**. Profile Details and Billing and Payments now also link directly to **Print Rental Payment Report**. No report or payment data changes were made.
- **Fixed:** the development server sent an empty body when browsers requested gzip-compressed static assets. CSS and JavaScript were consequently missing, producing an unstyled, horizontally overflowing page. Build-time static-asset compression is disabled in the web project; a gzip-requesting client now receives the full CSS. The existing visual design remains.
- No dead button or duplicate active report entry was confirmed. The Reports landing page distinguishes Stall Owner Rental Payment, Special Vehicle Permit Payment, and Monthly collection summary. The separate billing statement remains a contextual profile report.
- Business-policy clarification: the user confirmed **Create Account remains available on the login page even after accounts exist**. This takes precedence over the brief's first-run-only closure. Public signup is therefore intentional; office deployment should account for that access policy.
- A paid owner’s Stall Owner Rental Payment preview shows the *current amount due*, which can be ₱0 after settlement. This matches the desktop-style assessment report, but it is not a historical payment receipt. Whether the office also needs an OR-specific rental receipt is a separate business decision; no financial-report semantics were changed during QA.

## Verification

| Area | Result and evidence |
| --- | --- |
| Authentication/accounts | Browser suite passed invalid login, legacy SHA256 upgrade, PBKDF2 login, two sessions, public creation, duplicate username, edit, reset, anonymous restrictions, logout, and audit redaction. Existing populated dev DB was tested; a newly empty database was **not** created for this pass. |
| Profiles/rates/billing | Database and browser suites passed creation, search, edit, concurrent SIN allocation, duplicate submission, updated rental rates, historical snapshots, billing generation, penalty, and unverified/Review Needed safeguards. |
| Stall payments | Both suites passed OR validation, exact bill links, history, stale/double/concurrent submissions, rollback, and read-only rental previews with one to three selections; a fourth is rejected. |
| Vehicles/special permit | Both suites passed create/edit/search, duplicate plate, VIN allocation, draft save/reload, named Other Fees, total, authoritative amount, one VIN/year payment, next-year renewal, stale/concurrent submissions, OR conflicts, archive protection, history, and print report. |
| Reports | Rental and vehicle previews passed one-to-three selection and fourth-item rejection, stored values, logos, archive history where applicable, and read-only behavior. Monthly, billing, and Excel reports passed data and formula-injection checks. |
| Archive/import/audit | Both suites passed profile/vehicle archive and restore, retained history, active-list exclusion, CSV/XLSX mixed/invalid/duplicate handling, export filtering, audit actor/action/search, and no password leakage. |
| Dashboard/navigation | Browser suite reached all sidebar destinations and quick actions. Read-only SQL counts and monthly sums matched the rendered dashboard during QA. Final SQL snapshot: 881 active profiles, 230 unverified, 2,030 unpaid bill rows, 476 active vehicles, ₱424,205.00 rent and ₱18,709.65 vehicle collections for September 2026. These values change as synthetic fixtures are added. |
| Keyboard | Browser inspection found a visible keyboard focus and working skip link on Profile create, Vehicle create, Vehicle fee assessment, Reports, and User Accounts. Full keyboard-only completion of every requested form was **not** performed. |
| Responsive | After the asset fix, 8 representative pages were checked at 1440, 1280, 1024, 768, and 390 px (40 checks). None had page-level horizontal overflow. The narrow Profiles table scrolls internally. The viewport override was reset. |
| Print | Both preview pages and print CSS were inspected; A4 portrait, hidden app chrome, repeating logo and break avoidance rules are present. Chrome/Edge print preview was unavailable through the connected browser, so physical A4 pagination of 2–3 stacked reports is **unverified**. |
| Build/regression | Full solution build: 0 errors, 1 existing NU1510 warning in the WinForms project. Complete database suite and complete browser/HTTP suite passed sequentially after the final changes. |

## Data integrity and limits

All SQL checks explicitly targeted `BPLS_Dev`. After regression: 0 orphan billing rows, 0 orphan stall payments, 0 orphan vehicle payments, and 0 duplicate VIN/year groups. Transaction and read-only report assertions passed. The development database retains many synthetic QA records by design; 27 `SIN-ARCHTEST-*` paid bill fixtures have no OR/date fields. They are test setup rows, not new payment results. No blanket claim of unchanged row counts is possible because the suites intentionally create persistent fixtures.

**Assessment:** The tested web paths follow the practical desktop office workflow with the approved billing and permit safety corrections. Remaining proof gaps are fresh-empty-database bootstrap, complete keyboard-only form runs, and physical A4 print pagination in Chrome/Edge.
