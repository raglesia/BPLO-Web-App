# Phase 6: corrected billing and stall payments

This phase is limited to `BPLS_Dev`. The WinForms source and existing Phase 5 bill amounts were not changed. `BPLS_Dev.phase6-rent-basis.sql` adds nullable `MonthlyBilling.WebRentBasis`; it refuses to run outside `BPLS_Dev`. Existing rows remain `NULL`, and new web bills are marked `BaseOnly`.

## Approved billing rule

The web application continues to store a combined rent (`base + additional`) in `Profiling.MonthlyRental`, matching both its Phase 4 writes and the desktop form. For a new web bill only, `BillingService` derives base rent as `Profiling.MonthlyRental - Profiling.AdditionalCharge`, copies that base into `MonthlyBilling.MonthlyRental`, copies the charge separately, and marks `WebRentBasis='BaseOnly'`. This derivation is valid for the known `BPLS_Dev` profile records and is not assumed valid for an unexamined production database. The billing service refuses any connection outside `BPLS_Dev`.

Outstanding due and payment both use bill snapshots: base rent plus additional charge plus a 25% penalty on base rent after the period's 20th. `decimal` calculations use midpoint-to-even rounding, matching desktop `Math.Round`. Example: base 100.00, additional 10.00, and overdue penalty 25.00 total 135.00. No penalty gives 110.00; no additional charge gives 125.00 when overdue.

Existing Phase 5 rows with an additional charge and `WebRentBasis=NULL` have an unverified historical rent basis. Their amounts and statuses are not changed. The page shows those rows as needing review, omits a potentially misleading total, and blocks payment across such rows. Existing rows without an additional charge are unambiguous. Paid historical rows remain untouched. Review real BPLO data before a later production migration; do not mass-derive base rent from unknown records.

## Desktop payment path re-inspected

`Database.UpdatePaymentStatus` generates missing periods before its transaction. It settles every unpaid bill through the current month under one OR number; partial periods are not supported. It recalculates each overdue penalty immediately before payment, adds rent and additional charge separately, inserts `PaymentHistory`, links rows through `PaymentHistoryBilling`, marks billing rows Paid with OR/date/recorder/penalty, then sets `Profiling.PaymentStatus='Paid'` and `Profiling.Penalty=0`. The desktop implementation reselects rows for linking/updating and does not check exact affected counts. `ORNumberDialog` checks only `PaymentHistory`; the table has a unique OR constraint. `ProfilingForm.SaveEdit` calls `LogAudit("Update", ...)` after `UpdatePaymentStatus`, outside that transaction, and does not check its result. `GetPaymentHistory` lists OR, periods, rent, charges, penalty, amount, date, and recorder.

The web payment path performs missing-period generation, exact unpaid-row locking, OR validation, amount calculation, history insert, exact row links, exact bill updates, profile update, and audit insert in one serializable transaction. A failed step rolls back all of them. A duplicate OR returns a friendly error. The web audit uses the authenticated claim's user ID and is transactional; this strengthens the desktop audit timing. `DatePaid` and bill penalties are refreshed inside payment, so a missed maintenance action cannot avoid a penalty. The form submits only OR number and confirmation. No partial payment, vehicle payment, refund, edit, or deletion is available.

Cross-module OR uniqueness remains unresolved. `PaymentHistory.ORNumber` and `VehiclePermitHistory.ORNumber` are separately unique; neither schema nor desktop code enforces one shared series. Phase 6 enforces stall OR uniqueness only. Decide the BPLO receipt policy before vehicle payments.

## Verification and rollback

`dotnet run --project BusinessPermitLicensingSystem.BillingTests -- --database` tests corrected bills and confirms the old Phase 5 snapshots remain unchanged. `--payments` tests a three-period payment, the 135.00 example, exact links, duplicate OR, already paid, concurrent attempts, user/audit IDs, and rollback. The rollback test creates a temporary trigger limited to its synthetic SIN, forces failure after payment-history insertion, verifies no payment, link, paid bill, or audit remains, then removes the trigger in `finally`. Two different development accounts also submitted authenticated web payments; their distinct recorder and audit IDs were verified in SQL.

Rollback of the application change is to restore the earlier web build while preserving `BPLS_Dev` and its added nullable marker. Do not drop the marker or rewrite bill amounts as part of rollback. The migration script is idempotent. Re-run tests after any code rollback because older web code does not interpret the marker.
