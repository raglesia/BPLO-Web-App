# BPLO Current UI refinement and QA

25 November 2026

## Changes

- Kept the approved Office Overview composition, Masinloc seal and Midnight + Ice palette. Shared primary actions use the existing dark action blue. Field borders are stronger, selects retain their chevrons, and focus is visible across light and dark navigation.
- Removed unused Current dashboard selectors and retired overlapping screen declarations for buttons, focus, fields, tables and badges where Current already owns the component. Kept the existing stylesheet includes and print rules.
- Compacted results tables, linked names directly to their records or billing workspace, aligned comparable amounts, and standardized pagination and status cues. Search announces pending, empty and error states. Failed requests leave the last successful results and URL in place. Native search and pagination remain available.
- Grouped the saved payment context next to the OR fields. Accepted POSTs announce progress and reject a repeated submission while navigating. Validation or an open confirmation does not lock the form. The browser preserves the original submitter and leaves authoritative validation, payment processing and receipt controls on the server.
- Added a leave-page warning to editable profiles, drafts and OR forms. Stall-owner forms show an estimated base rent. Vehicle assessment continues to show its live draft total and identifies the saved assessment as the payment basis.
- Office Overview month buttons expose the month, stall rent, vehicle permits and total from the existing chart data. Pointer hover, keyboard focus and activation show the same breakdown beside the chart.
- Condensed report choices and grouped monthly date fields. The narrow rental preview has a keyboard-focusable scroll region and a screen-only instruction. The formal document markup inside the preview and all print layout rules are preserved.

## Validation

- Main web project compiled through the existing billing regression executable. All 60 database-free assertions passed. The isolated SQLite presentation build also passed.
- The before and after suites each cover 13 rendered screens at 1440, 820 and 390 CSS pixels. Synthetic fixtures include populated directories, unpaid stall billing and a recorded vehicle payment for the chart. Screenshots and test records are local QA artifacts.
- Live Edge checks cover search and context, empty results, field indicators, rent and fee previews, unsaved changes, chart focus, drawer keyboard behavior, reduced motion, invalid payment submission, the duplicate-submit guard, confirmation cancellation and preview keyboard access. The payment test intercepted the browser request before it reached a handler and checked the exact OR and request count.
- Compared the synthetic Rental Report before and after in Edge print mode. Both were one A4 page. Raster differences were confined to the generated timestamps.
- SQL Server integration scenarios, payment settlement, official permit preparation, full assistive-technology coverage and physical printer output were outside this verification. Some dense tables still scroll locally at narrow widths. Older module styles remain in the cascade pending coverage of their conditional states.

No deployment was performed.
