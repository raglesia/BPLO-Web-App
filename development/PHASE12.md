# Phase 12 — functional hardening and end-to-end evidence

Date: 27 September 2026. Scope: local Razor Pages application and `BPLS_Dev` only. WinForms files were not edited. No deployment, production connection, schema change, or UI redesign was performed.

## Approach and reproducibility

`BusinessPermitLicensingSystem.BillingTests/BrowserScenarios.cs` drives the running ASP.NET Core app over loopback HTTP. Each simulated browser has a separate cookie jar. Tests submit real Razor forms with antiforgery tokens, follow redirects, upload multipart CSV/XLSX files, download Excel, and compare selected results with SQL queries guarded to `BPLS_Dev`. The test reads development credentials from `%TEMP%/BPLS_Dev.test-accounts.json`; no password is stored in source or output. Each run creates records marked `PHASE12-<random>` plus fresh `SIN-P12-*`, `VIN-*`, and `P12-*` identifiers. These records remain in the development database as evidence. The harness verifies server-rendered HTTP behavior; it does not exercise JavaScript, visual layout, keyboard navigation, or browser history UI.

Run the local app with the `BPLS_Dev` connection string, then run the test executable with `--browser=http://127.0.0.1:<port>`. The final verified preview ran at `http://127.0.0.1:5146`. The earlier focused scenarios run with `--database --payments --vehicles --archive --transfer --reports --admin`.

## Scenario evidence

| Scenario | Expected | Actual | Result |
|---|---|---|---|
| Anonymous, invalid cookie, invalid/unknown credentials | Protected pages return to login; generic failure | All behaved as expected | Pass |
| PBKDF2 and fresh SHA-256 login | Both log in; verified legacy hash upgrades | Login succeeded; legacy row changed to PBKDF2 | Pass |
| Sessions, logout, unsafe return URL | Cookies independent; logout blocks access; external URL rejected | All behaved as expected | Pass |
| Navigation | Every migrated module opens from normal routes | Dashboard, profiles, vehicles, rates, archive, audit, import/export, reports opened | Pass |
| Profile create/search/detail/edit | SIN assigned; record found; edit retained | HTTP forms and resulting pages matched | Pass |
| Profile concurrency and repeat POST | Distinct SINs; one duplicate record rejected | Two audited SINs; repeat identical profile created one row | Pass |
| Flat/per-square-meter and occupancy | Correct rent; Unverified excluded; missing occupancy rejected | All matched expected values and messages | Pass |
| Rate change | New profile uses new rate; old rent/bills remain | 100 to 150 per sqm changed new rent; old rent and snapshot stayed | Pass |
| Billing generate and concurrency | First month after occupancy; one row per period; repeat adds zero | All matched; no duplicate periods | Pass |
| Stall payment and stale/double submit | One payment, linked bills, current state rechecked | One payment; stale and repeat POST made no second row | Pass |
| Concurrent stall payment and duplicate OR | One winner; duplicate OR refused | Exactly one history row; other payment and duplicate OR refused | Pass |
| Vehicle create/edit and VIN concurrency | Valid entry; stable VIN on edit; unique concurrent VINs | All matched; duplicate plate rejected | Pass |
| Vehicle draft/payment | Draft survives reload; amount comes from stored draft | 123.45 paid despite submitted 0.01 amount | Pass |
| Concurrent/stale/double vehicle payment | One VIN/year payment | Exactly one history row for each scenario | Pass |
| OR scope | Same OR accepted separately in stall and vehicle ledgers | Both payments recorded; duplicate within either ledger rejected | Pass, policy pending |
| Ambiguous legacy bill | Review needed; no payment; report remains viewable | UI and forced POST both blocked payment | Pass |
| Archive/restore, repeat action, stale edit | Identity and history preserved; repeats have no extra audit; stale edits rejected | Both record types behaved as expected | Pass |
| CSV/XLSX imports | Mixed rows counted; duplicate/invalid rows reported; no financial side effects | Both file types for both record types; counts and financial tables verified | Pass |
| Upload errors | Bad header, empty/unsupported/oversize file handled without raw error | Friendly errors; path-like client filename did not escape storage | Pass |
| Excel exports | Files open; search and empty result work | Profile, vehicle, and monthly report workbooks opened | Pass |
| Reports | Payment and archived history visible; invalid/empty period safe | Printable/monthly reports and download worked | Pass |
| Audit Trail | Acting username/reference shown; search, empty state, pagination | All matched; viewer has no mutation route | Pass |
| Read-only GET | Viewing cannot change bills, payments, drafts, archive state | Counts equal before and after repeated GETs | Pass |
| Error pages | No SQL internals, credentials, paths, or hashes | Generic page and expected validation responses passed scan | Pass |

The existing focused tests additionally prove forced rollback after intermediate stall and vehicle payment failures, historical billing snapshots, penalty boundaries, vehicle renewal with a reference year, import formula safety, and reporting with archived owners. Date additions cover December-to-January, leap February, leap-day occupancy, and small/large decimal values. The approved rule remains `Total Due = Base Rent + Additional Charge + Penalty`, with penalty equal to 25% of base rent after day 20.

The dashboard's rendered links were collected from its HTML and followed over HTTP. Every local dashboard link, including database health, returned a valid page. Profile and vehicle list links expose transfer pages; reports and record detail pages expose their related workflows.

## Bugs found and fixed

1. Optional vehicle fields were bound as non-nullable strings. A blank driver/SEC/DTI field could fail model validation before duplicate-plate handling. These fields are now nullable at the page boundary and become empty strings in the service. A browser test covers the empty-field duplicate case.
2. Successful imports returned the result directly from POST, allowing browser refresh to offer resubmission. Successful imports now redirect to GET and retain summary counts plus the first 20 row issues in TempData. The browser test confirms a refresh uses GET and leaves one imported row.
3. A profile edit submitted after another browser archived the profile reached a 404. It now returns to archived profiles with a clear message; the archived record remains unchanged.
4. A repeated profile archive now returns to the archived list with an explicit already-archived message. Repeat archive/restore tests confirm no extra audit rows.
5. The default error template contained misleading development-mode text. The error page now gives a simple request-ID instruction without internal details.

## Integrity, validation, and remaining limits

Profile create/edit, vehicle create/edit, rental-rate writes, and archive/restore write their audit entry inside the same SQL transaction as the record change. Stall and vehicle payments also use transactions; existing forced-failure tests verify rollback of financial rows. Browser tests verify acting usernames, exact stall-bill links, VIN/year protection, duplicate OR mapping, and rejection of stale writes. All normal GET page handlers were reviewed; the browser test confirms no changes to billing, payments, drafts, or archive counts. Report/export GETs only read data.

Server validation is consistent with the established field limits: profile and vehicle names up to 255 characters, stall/plate identifiers up to 100, decimal money, required occupancy for payable profiles, and current-year vehicle payment. Different field rules reflect the desktop workflows. The web does not expose raw SQL messages for expected duplicate or invalid submissions. Public account creation remains unavailable.

Limits of this pass: tests do not simulate JavaScript, visual browser rendering, keyboard accessibility, an expired cookie after eight hours, a completely empty rental-rate table, or a true calendar rollover in a live browser. Those conditions were covered by code review or reference-date/service tests where possible. Forced rollback was exercised by focused service tests, not by inducing a production-like infrastructure failure through HTTP. No known critical data-integrity defect remains in the tested workflows, but this is development evidence, not operational approval.

The unresolved BPLO policy questions remain unchanged: whether a saved vehicle fee draft is authoritative, whether stall and vehicle ORs share one series, and how to assess historical bills with unknown rent basis. Office account provisioning also needs an approved process.

## Phase decision

The application is stable enough to begin **Phase 13 UI/UX Design** in the development workspace. Keep the policy questions open and retain functional regression coverage during design work. This is not a deployment recommendation.

## Completion report index

1. **Approach:** Real ASP.NET HTTP requests with separate cookie jars, antiforgery, multipart uploads, workbook parsing, and SQL assertions.
2. **Authentication:** Protected routes, valid/invalid login, fresh SHA-256 upgrade, logout, independent sessions, invalid cookie, and unsafe return URL passed.
3. **Navigation:** Rendered dashboard links and completed module routes returned working pages.
4. **Profiles:** Create, search, detail, edit, SIN, duplicate, occupancy, Unverified, flat/per-square-meter, archive/restore passed.
5. **Rates:** Browser update changed only new calculations; old profile rent and bill snapshot remained.
6. **Billing:** First month, repeat/concurrent generation, exclusions, corrected totals, and legacy review passed.
7. **Stall payment:** Exact links, authoritative amount, audit owner, duplicate/stale/concurrent/double-submit safeguards passed.
8. **Vehicle create/edit:** VIN allocation, duplicate plate, validation, search, edit, concurrent VIN, archive interaction passed.
9. **Vehicle draft/payment:** Persisted draft, tamper resistance, VIN/year, acting user, stale/concurrent/double-submit safeguards passed.
10. **Archive/restore:** Both record types, repeat handling, history retention, and audit counts passed.
11. **Import/export:** CSV and XLSX multipart uploads, mixed rows, bad files, path-like filename, Excel opening, search, empty export passed.
12. **Reports:** Printable rental, monthly collection, workbook, empty/invalid period, archived history, and legacy review passed.
13. **Audit Trail:** User/reference search, empty search, pagination, and immutable UI passed.
14. **Duplicate submission:** Payments reject repeats; identical profile submit rejected; imports now redirect after success.
15. **Stale pages:** Stall/vehicle payment and archived-record edit submissions revalidate current server state.
16. **Concurrency:** HTTP SIN, VIN, billing, and both payment races passed; focused tests also cover OR and VIN/year uniqueness.
17. **Date boundaries:** Day 19/20/21, month end, December/January, leap February, leap-day occupancy, and renewal reference year passed.
18. **Money:** Decimal base/additional/penalty math, 0.00, small and large values, and 123.45 draft authority passed.
19. **Validation:** Fixed optional vehicle field binding; required occupancy and duplicate plates produce useful messages.
20. **Error handling:** Generic error page and expected failures showed no SQL internals, paths, connection details, or hashes.
21. **Transactions:** Multi-table writes use SQL transactions; existing forced payment-failure tests verify rollback.
22. **GET audit:** Repeated normal GETs left financial, draft, and archive counts unchanged.
23. **Bugs found:** Blank optional vehicle fields, import resubmission, stale profile edit 404, repeat archive response, misleading error template.
24. **Bugs fixed:** All five findings above, with browser regressions for their behavior.
25. **Remaining defects:** No known critical integrity defect in tested flows; rendered-browser accessibility and entirely empty rate-table behavior remain untested.
26. **Policy questions:** Saved vehicle draft authority, shared OR series, ambiguous legacy bills, and account provisioning remain open.
27. **Coverage added:** Repeatable `--browser` HTTP suite plus date/decimal boundary assertions.
28. **Build/tests:** Full solution build and all focused tests passed; final browser suite passed.
29. **Database boundary:** Every test connection used `BPLS_Dev`; no production database connection occurred.
30. **Phase 13:** Stable enough for development UI/UX design, with these regressions retained. No deployment work is recommended.
