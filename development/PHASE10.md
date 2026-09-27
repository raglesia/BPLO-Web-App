# Phase 10: reports, printing, and downloads

## Desktop report inventory

| Workflow | Launch and parameters | Source and calculation | Output/use |
| --- | --- | --- | --- |
| Stall-owner rental payment sheet | `ProfilingLists` checked rows, one to three active profiles; Paid rows skipped | Grid `BillingReportModel`, not the bill ledger. `BillingReport.rdlc`/`BillingReportDataSet.xsd` display municipality, province, BPLO, owner, business, section, stall, SIN, status, current rental, penalty, additional charge, total, processing date and operator. RDLC total adds all three fields. The profile's rental may already contain the additional charge, so this can double-count it. | `ReportViewerForm` print layout. No separate file-save code; ReportViewer can print/export. Used by a button. |
| Monthly collection summary | `ProfilingLists` Collection Report button; from/to month and year, most recent six years in selector | `Database.GetMonthlyReport` selects active profiles whose **occupancy date** falls in range. Excel rows show SIN, name, business, section, current rental, current penalty, current additional charge, profile payment status. Summary counts Paid/Unpaid and calls Paid current rent “collected,” Unpaid current rent “uncollected,” and Unpaid penalty “penalty collected.” These are not payment-ledger totals. | ClosedXML `.xlsx` saved via file dialog. Used by a button; not a print action in source. |
| List exports | Profile and vehicle grid exports | Current active/filtered grid columns | Migrated in Phase 9; not recreated as reports. |
| Profile list statistics | `ProfilingLists` labels | `GetPaymentSummary` and `GetCollectionSummary` use current active profile fields | On-screen only; no report/print/download action. |
| Vehicle “Collection Report” | `VehiclePermitLists` designer button | No click handler or vehicle report/print source found | Inert desktop button; no vehicle report migrated. |

`Database.GetProfiles()` has no caller and is not a report workflow. Typed DataSet designer code remains a WinForms artifact. No `PrintDocument`, vehicle permit print, or vehicle payment report caller was found.

## Web implementation and intentional corrections

- `/Reports` links to a printable stall-owner rental report and monthly collection summary. The billing report is server-rendered HTML with browser print and A4 print CSS. It supports one SIN, including archived profiles, and lists each stored billing period and linked payment history. The web report includes paid records because historical payment verification is required; desktop sheet skipped them. It does not use WinForms ReportViewer or RDLC at runtime.
- `/Reports/Monthly` validates month/year range, shows print-friendly HTML, and downloads ClosedXML `.xlsx` with Summary, Billing periods, and Payments sheets. The workbook uses numeric money cells and text string cells; a leading `=` business name was reopened and verified to have no formula. Dates in payment cells remain dates.
- The web monthly period applies to `MonthlyBilling.BillingYear/BillingMonth`. Recorded collections apply separately to `PaymentHistory.DatePaid`. This intentionally replaces the desktop occupancy-date filter and current-profile-field totals, which cannot accurately answer a monthly billing/collection question. The page explains that a payment recorded in the range may cover bills outside it. `PaymentHistoryBilling` supplies exact covered periods; each payment is counted once. Historical payments remain included when an owner is now archived. Penalty collected is summed from `PaymentHistory.Penalty`, never from unpaid bills.
- Bill amounts come from stored `MonthlyBilling` snapshots. `BaseOnly` rows use stored base rent; zero-additional legacy rows are unambiguous. A legacy row with an additional charge and no rent-basis marker shows stored rent/additional/penalty, but its corrected total and any aggregate containing it show `Review needed`. No historical bill is rewritten. Stored penalty is displayed; report reads do not run billing generation or penalty updates. Corrected total is base rent + additional charge + stored penalty.
- Profile/rental-rate changes after billing do not affect reported snapshots. OR, amount, payment date, and linked periods come from the payment ledger. Recorder names resolve from the recorded `UserId` through the current Users table because no historical name snapshot exists; the current viewer appears only as report preparer.
- All routes require authentication through the existing Razor Pages convention. The report service checks connection-string Initial Catalog and the connected database name for `BPLS_Dev`. Report generation uses only SELECT statements and in-memory workbook creation. No PDF library or new schema was added.

## Verification

- Full solution build passed with one pre-existing WinForms `NU1510` package warning. WinForms source was unchanged.
- Synthetic `BPLS_Dev` tests passed: 100 base + 10 additional + 25 penalty = 135, three periods total 460, paid OR/date/amount/recorder/exact linked period, unchanged historic bill after profile rental changes, archived profile payment inclusion, ambiguous legacy review marker, collection and penalty totals equal direct PaymentHistory sums, workbook opens, formula safety, invalid period rejection, and repeated report reads/downloads leaving billing/payment/audit counts and archive state unchanged.
- Existing stall-payment regression tests passed against the Phase 10 build. HTTP verification: anonymous `/Reports` redirects to login; authenticated monthly view, Excel download, and printable billing page return 200. Invalid month order displays a validation message. Printable billing page shows 135.00, archived state, OR, and print control.
- Only `BPLS_Dev` was used; no production/live BPLO database connection occurred. Synthetic records use `SIN-REPTEST` and `SIN-LEGREP` prefixes and remain in the development database.

## Remaining ambiguity / next phase

Desktop monthly “collection” semantics conflict with ledger truth. This phase uses authoritative bills and payments and labels their different date bases. Historical recorder names may change if a Users name changes because the schema stores only `RecordedBy` ID. Ambiguous legacy rent-basis rows still require BPLO review before authoritative totals or payment.

Proposed next phase: a focused pre-release audit of migrated workflows, business-rule decisions, accessibility, and deployment readiness using `BPLS_Dev`; production migration or deployment remains outside scope until separately authorized. No next-phase work began here.
