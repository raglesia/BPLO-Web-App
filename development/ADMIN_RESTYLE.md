# BPLO admin workspace restyle

The Satnaing Shadcn Admin [template](https://www.shadcn.io/template/satnaing-shadcn-admin) and [live demo](https://shadcn-admin.netlify.app/) informed the navigation hierarchy, light shell, compact cards, restrained borders, and table density. The implementation remains ASP.NET Core Razor Pages with Bootstrap and custom CSS. No React, Tailwind, SPA, or new frontend build system was added.

## Completion report

1. **Reference:** Used the template's sidebar grouping, light top bar, moderate corner radius, low-shadow panels, and compact dashboard proportions. BPLO routes and terminology determine the content.
2. **Sidebar:** Main contains Dashboard, Profiles, Vehicles. Management contains Rental Rates, Archive (Profiles/Vehicles), Audit Trail. Tools contains Import/Export (Profiles/Vehicles), Reports, Database Check. Billing remains under Profiles and permit payment under Vehicles because there is no separate transaction index.
3. **Responsive sidebar:** Fixed desktop sidebar becomes an off-canvas drawer at 991 px and below. Closed links are removed from keyboard navigation through CSS visibility. Escape and the backdrop close the drawer; focus returns to Menu.
4. **Top bar:** A 53 px bar shows the current work area and office name. At narrow widths it adds only the Menu control, rather than duplicating navigation.
5. **Typography:** Page title is approximately 24–27 px, body 15 px, section titles approximately 18 px, and controls/table text approximately 13–14 px.
6. **Density:** Reduced page, card, form, table, button, and alert padding. The dashboard shows more work information above the fold.
7. **Color:** Retained cool slate, muted blue, and teal with neutral backgrounds and subtle borders. Existing semantic paid/unpaid/review colors remain.
8. **Cards:** Thin borders, white surfaces, no floating shadow, smaller corner radius, and compact metrics.
9. **Buttons:** Reduced height and padding while retaining clear primary, secondary, danger, and focus states.
10. **Tables:** Shared header and cell padding were reduced. Table wrappers keep horizontal scrolling within the table on narrow screens.
11. **Forms:** Shared labels, controls, fieldsets, and action rows were tightened without changing field sequence or validation.
12. **Dashboard:** Replaced the large welcome panel with a simple heading, six restrained cards, one collections chart, quick actions, and Needs Attention.
13. **Cards:** Active stall owners, Unverified profiles, Unpaid billing periods, Active vehicle permits, Rent collected this month, and Vehicle permit collections this month.
14. **Rent card:** Sums `PaymentHistory.AmountPaid` by recorded `DatePaid` for the current month.
15. **Vehicle card:** Sums `VehiclePermitHistory.AmountPaid` by recorded `DatePaid` for the current month.
16. **Trend:** A six-month grouped bar chart compares recorded stall rent and vehicle collections. Months with no payments display zero. An accessible data table provides the exact values.
17. **Chart count:** One grouped bar chart with two payment series; no estimate, draft, or outstanding amount feeds it.
18. **Restraint:** No growth percentages, trend arrows, pie charts, or extra dashboard widgets.
19. **Profiles:** Shared sidebar, page width, header, form, card, badge, and table styles apply to list, detail, create, edit, billing, and transfer pages.
20. **Billing:** Monetary totals remain emphasized; compact styling leaves payment controls and review-needed state intact.
21. **Vehicles:** The same shared visual language applies to list, details, fee draft, payment, and transfer; Save Draft and Record Payment remain separate actions.
22. **Rental rates:** Compact administrative list and edit form through shared table/form styling.
23. **Archive:** Sidebar expands the two archive routes; muted archived badges and restore actions retain their meaning.
24. **Audit Trail:** Compact log rows and filters through shared table/form styling.
25. **Import/export:** Existing native file inputs, format guidance, and result summaries remain, with reduced panel/control spacing.
26. **Reports:** Selection and parameter pages receive compact shared styling; print CSS still removes the application shell.
27. **Responsive:** Checked dashboard and ten representative routes at 1440, 1024, 768, and 390 px, plus dashboard at 1280 px. No page-level horizontal overflow was found. The narrow sidebar was verified hidden while closed.
28. **Keyboard:** At a narrow viewport, Tab reached Menu after the skip link. Enter opened navigation, Tab reached Dashboard, and Escape closed it and returned focus to Menu. Existing Phase 14 focus rules remained. Full keyboard replay of every business workflow was not repeated for this visual refactor.
29. **Regression:** Solution build passed with zero errors and the existing WinForms `NU1510` warning. The full Phase 12 billing/database/payment/vehicle/archive/transfer/report/admin/browser suite passed with exit code 0 after final changes.
30. **Remaining UI concern:** A spoken screen-reader session and staff review of the compact density would add confidence. The responsive checks covered layout/overflow, not every device/browser combination.

Only the development web application and `BPLS_Dev` were used. WinForms, database schema, financial rules, transaction handling, authentication, and deployment were unchanged. The local preview is `http://127.0.0.1:5153/` while its process runs.
