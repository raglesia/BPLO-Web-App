# Phase 5 billing behavior and test record

Phase 5 was the desktop-compatible baseline. BPLO subsequently approved a corrected web billing rule. See `PHASE6.md` for current behavior. The original Phase 5 synthetic billing amounts remain unchanged.

This phase uses only `BPLS_Dev`. It adds no payment operation or automatic billing action on page load.

## Desktop source behavior

- `ProfilingForm.ComputeMonthlyRental` computes base rent from `RentalRates`: flat rate, or stall size multiplied by the per-square-meter rate. It adds enabled additional charge to the displayed monthly rental. `Database.AddProfile` and `UpdateProfile` store that result in `Profiling.MonthlyRental` and store `Profiling.AdditionalCharge` separately.
- `Database.EnsureBillingPeriodsForProfile` starts with the month after occupancy and inserts every missing month through the current month. Each inserted `MonthlyBilling` row copies both `Profiling.MonthlyRental` and `Profiling.AdditionalCharge`. The unique `(SIN, BillingYear, BillingMonth)` constraint guards duplicates. The copied values are snapshots: later profile or rate changes do not alter existing bills.
- `Database.EnsureMonthlyBillingForAll` inserts only the current month for active, verified profiles with valid occupancy dates, subject to `AppSettings.LastMonthlyReset`. `DashboardForm` invokes this pass on load. `Database.ApplyPenaltiesToAll` separately backfills all missing periods for active, non-Unverified profiles. The web version uses explicit POST actions and backfills all periods.
- `Database.ApplyPenaltiesToAll` excludes archived and Unverified profiles, calls `CalculateOutstandingPenalty`, and overwrites `Profiling.Penalty`. It does not update unpaid `MonthlyBilling.Penalty`. `CalculateOutstandingPenalty` sums 25% of each unpaid row's `MonthlyRental` after that period's 20th, using `Math.Round(..., 2)` (midpoint to even). Repeated processing replaces the aggregate and does not compound it. `UpdatePaymentStatus` writes a calculated penalty into each billing row when paid; Phase 5 does not call it.
- `Database.GetOutstandingBalance` sums unpaid billing rows through the current month for non-Unverified profiles, including `MonthlyRental`, `AdditionalCharge`, and calculated penalty. It does not filter archived profiles. `UpdatePaymentStatus` uses the same three components to calculate a payment. Thus additional charge is counted twice when `MonthlyRental` already contains it.
- Example: base rent 100.00 plus additional charge 10.00 stores `MonthlyRental=110.00` and `AdditionalCharge=10.00`. Before penalty, desktop outstanding balance is 120.00. After the 20th, the penalty is 27.50 and total is 147.50. The likely intended alternative is 110.00 before penalty, with a 25.00 penalty on base rent, but BPLO must confirm its rule before any correction.
- Billing-generation and penalty-update methods do not call `LogAudit`. The web billing actions preserve that behavior; page views are read only.

## Web behavior

- At Phase 5 completion, `/Profiles/{sin}/Billing` showed stored bill snapshots and had no payment action. Phase 6 added payment entry and history.
- Authenticated, antiforgery-protected POST actions generate missing bills or recalculate the profile penalty. Both recalculate values from server data. Generation uses a serializable transaction and update locks. The database unique constraint remains the final safeguard. `BillingService` refuses any database other than `BPLS_Dev`.
- Archived and Unverified profiles have no generation controls. Existing history remains visible. No job or page GET writes billing data.

## Verification

Run `dotnet run --project BusinessPermitLicensingSystem.BillingTests -- --database` from the solution directory to test rules and synthetic `BPLS_Dev` data. The test leaves marked synthetic profiles for inspection. It checks occupancy month exclusion, backfill, repeat and concurrent generation, archived and Unverified exclusion, historical bill snapshots, penalty boundaries and repetition, the double-counted additional charge, outstanding totals, and unchanged payment-history counts. The authenticated billing page was checked with a local development account.

The business decision was made after Phase 5: charge additional amount once and calculate the 25% penalty from base rent only. See `PHASE6.md`.
