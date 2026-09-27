# Modern LGU UI refactor — 27 September 2026

## Scope and architecture

Reviewed the existing Razor Pages layout, dashboard, shared profile fields, sidebar script, chart CSS, fee assessment CSS, report print styles, and module pages. The app remains ASP.NET Core Razor Pages with Bootstrap and server-rendered workflows. This pass changed presentation only: `BusinessPermitLicensingSystem.Web/Pages/Shared/_Layout.cshtml`, `BusinessPermitLicensingSystem.Web/Pages/Index.cshtml`, `BusinessPermitLicensingSystem.Web/wwwroot/css/site.css`, and a smaller copy of the existing seal at `BusinessPermitLicensingSystem.Web/wwwroot/images/masinloc-logo-sidebar.png`. Earlier unrelated working-tree changes were preserved.

## Design changes

| Area | Result |
| --- | --- |
| Tokens | Centralized institutional navy/gold, interaction teal, neutral canvas and surfaces, text and border levels, semantic status colors, spacing, and radii. Removed the previous mint/pastel override layer and obsolete compact shell rules. |
| Institutional shell | Compact 48px navy top bar displays Municipality of Masinloc, Province of Zambales, and Business Permits and Licensing Office. The white 256px sidebar uses the project’s Masinloc seal, a fine right border, compact groups, and a pale teal active state. |
| User area | Kept the existing signed-in name, position, and Sign Out form; tightened its spacing and visual hierarchy. Account behavior and authorization were not changed. |
| Typography/actions | Compact administrative title and section scale; teal primary buttons, neutral bordered secondary actions, visible focus ring, white labeled inputs, and restrained danger styling for archive actions. |
| Tables/forms/status | Shared table header, row, input, alert, and status-badge rules now apply to Profiles, Vehicles, Billing, Rental Rates, User Accounts, Archive, Audit Trail, Import/Export, Reports, and Database Check. Paid/Active are green, Unverified/Review Needed amber, Unpaid red, and Archived blue. The existing fee entry, Other Fees, live total, Save Draft, and Record Payment structure remains. |
| Dashboard | Replaced the six pastel cards with four neutral Records Overview cards and a separate two-card Municipal Collections section. Kept the existing count/payment queries, six-month chart, Quick actions, and Needs attention. The chart now uses restrained teal tones and neutral grid lines. |
| Reports | Restyled report selection through shared UI rules. Formal printable rental and vehicle payment documents, municipality header, existing print CSS, selection limit, and read-only data remain unchanged. |

## Verification

- Full solution build: **passed**, 0 errors; one existing WinForms NU1510 package warning.
- Complete `BPLS_Dev` database regression suite: **passed** after the final CSS cleanup.
- Complete local browser/HTTP regression suite: **passed** after the final CSS cleanup. It covers authentication, account management, profiles, billing, payments, vehicles, reports, archive, import/export, audit, and read-only navigation.
- Responsive: 65 page/width checks during implementation and 25 after CSS cleanup at 1440, 1280, 1024, 768, and 390px. No page-level horizontal overflow in those checks; narrow tables scroll within their table regions. Desktop and mobile dashboard were visually inspected.
- Keyboard: visible focus and the skip link were checked on representative administrative forms and selection pages. A complete keyboard-only submission of every workflow was **not** performed, so full accessibility compliance is not claimed.
- Dashboard: displayed figures were checked against read-only authoritative SQL in `BPLS_Dev` during QA. The values changed as the regression suites added synthetic fixtures; the UI reads the same existing queries.
- Print: formal preview markup and print rules were preserved. Physical Chrome/Edge A4 print preview was unavailable through the connected browser, so paper pagination was **not** verified.

Only `BPLS_Dev` was used. No billing, payment, authentication, password, database schema, archive, import, report calculation, or route logic was changed in this UI pass. WinForms, `Masinloc_BPLS`, production, and deployment were untouched.
