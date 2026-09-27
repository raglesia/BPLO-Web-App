# Phase 15 — Special Vehicle Permit annual payment completion

## Scope

Only the Razor Pages web application, its regression tests, and `BPLS_Dev` were used. WinForms, production databases, schema, and deployment were not changed. User Acceptance Testing was not started.

## Completion report

1. **Missing functionality:** The annual transaction already existed. The practical gap was presentation: the vehicle list showed raw stored status/year, and details buried the current-year payment behind a long fee table without a clear paid-year summary.
2. **Reused code:** `VehicleService`, `VehicleFeeDraft`, `VehiclePermitHistory`, the existing VIN/year unique index, and Razor Page handlers remain the payment implementation. The WinForms source was inspected for annual reset and draft behavior but not edited.
3. **Annual rule:** One payment per VIN per permit year; later years retain earlier history. The normal web handler uses the current server year.
4. **Eligibility:** Detail reads current-year history plus current stored state. A prior-year Paid state does not make the new year Paid. GET pages do not reset status or write to the database.
5. **Duplicate safeguard:** Payment rechecks VIN/year inside a serializable transaction with update/hold locks. `UX_VehiclePermitHistory_VIN_PermitYear` is unique in `BPLS_Dev` and is verified by the test suite.
6. **Fee draft:** The current-year saved fee assessment remains required. A missing or zero draft produces guidance and a direct link to the assessment section.
7. **Amount authority:** The transaction reloads and validates `VehiclePermitFeeDrafts.GrandTotal` and its JSON. Posted amount is ignored.
8. **Payment UI:** The detail page now leads with a year-specific Paid/Unpaid panel, plate, and saved amount. The payment section identifies company, plate, year, amount, OR field, explicit confirmation, and a year-specific primary button. Save Draft remains a separate secondary action.
9. **History UI:** Read-only annual history shows permit year, OR, amount, payment date, and recorder in descending year/date order. A paid current year shows its OR, amount, and date above the draft.
10. **OR validation:** A nonblank vehicle OR of at most 100 characters is required. Duplicate vehicle ORs are rejected cleanly within the transaction; stall and vehicle ledgers remain separate.
11. **Transaction:** Vehicle lock, eligibility, VIN/year check, saved draft, OR check, history insert, and current status/year update occur in one serializable SQL transaction. A forced intermediate failure rolls back both history and status.
12. **Server authority:** Authenticated user comes from claims. VIN is bound explicitly from the route for payment; forged form VIN/year/amount/user fields do not redirect or alter the payment target.
13. **Stale page:** A second browser submitting an old unpaid form receives an already-paid response and creates no second row.
14. **Concurrency:** Two near-simultaneous payments for one VIN/year have exactly one winner and one history row.
15. **Duplicate year:** A repeat payment for the same VIN/year fails; the Paid state and original annual history remain.
16. **Duplicate OR:** Reuse of a vehicle OR fails without creating history or changing status.
17. **Rollback:** A synthetic trigger failure after history insertion was exercised in `BPLS_Dev`; history and vehicle status remained unchanged. The test trigger was removed.
18. **Tampering:** Browser tests submit forged amount, next year, acting-user ID, and form VIN. Recorded amount, year, user, and VIN come from the server-side route/claims/database. A POST without an anti-forgery token returns HTTP 400.
19. **Renewal:** The same VIN paid for 2026 and then 2027 in the service test. The 2026 row remained, 2027 got a separate draft/payment, and 2027 became the current stored year. Eligibility for 2027 was available before any reset or page mutation.
20. **Archive:** Archived vehicles cannot be paid; historical rows remain viewable. A direct service payment attempt against an archived vehicle was rejected.
21. **Acting user/audit:** History records the authenticated user ID and displays the associated full name. The existing vehicle-payment workflow writes no separate `AuditTrail` row, matching the inspected WinForms behavior and current tests.
22. **Keyboard:** Created synthetic `VIN-2026-0160`, saved a ₱165.50 draft, used the payment jump link, entered OR `P15-SVP-0927-0160`, confirmed with Space, and recorded payment with Enter. The page then showed Paid, OR, amount, date, and history; the active payment form disappeared.
23. **Responsive:** The unpaid draft/payment page was inspected at 1440, 1280, 1024, 768, and 390 px. OR and payment controls stayed within the viewport. At 390 px, the fee table scrolled internally; there was no page-level horizontal overflow.
24. **Regression:** Full billing, database, payment, vehicle, archive, transfer, report, admin, and browser suite passed after final edits (exit code 0). This includes concurrent/stale payments, duplicate OR/year, antiforgery, tampering, and rollback.
25. **Build:** Solution build passed with zero errors and the pre-existing WinForms `NU1510` package warning.
26. **Database:** All database reads, writes, manual payment, and automated tests targeted only `BPLS_Dev`; the service enforces the database name.
27. **Open policy:** Whether the saved fee draft is the official BPLO assessment basis remains a business-policy decision. Global OR uniqueness across stall and vehicle payments is also intentionally unresolved. Neither rule was changed.
28. **Deployment:** No production connection, deployment, LAN/IIS, or office workstation work occurred.

The local preview is `http://127.0.0.1:5153/` while its development process remains running. Synthetic test records remain in `BPLS_Dev`.
