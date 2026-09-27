# Phase 7: Vehicle permit drafts and payments

Phase 7 uses only `BPLS_Dev`. WinForms source and historical vehicle payment rows were not changed. `BPLS_Dev.phase7-vin-year.sql` first checks for duplicate `(VIN, PermitYear)` history, then adds an idempotent unique index. The existing vehicle OR unique constraint remains separate from the stall OR constraint. No cross-module receipt registry or constraint was added.

## Desktop source findings

- `VehiclePermitLists` opens `VehicleProfiling` for add/edit. The annual tabs in `VehicleProfiling.PermitHistory.cs` display the current year and older `VehiclePermitHistory` rows. The displayed year is `Max(DateTime.Today.Year, VehiclePermits.PermitYear)`.
- `VehiclePermitDetailDialog` holds 19 fee amounts, four other-fee descriptions, and other permit details. It starts with `0.00` values and recalculates their sum live. `Update_Click` validates non-negative amounts with at most two decimals, serializes the existing `FeeDraft` JSON shape, and calls `SaveVehiclePermitFeeDraft(VIN, year, JSON, total)`. Saving the draft creates no payment history and makes no status change. The database draft key is `(VIN, PermitYear)`.
- `Database.PayVehiclePermit` accepts VIN, OR, **caller-supplied amount**, permit year, and user ID. In one transaction it updates `VehiclePermits.PermitStatus='Paid'` and `PermitYear`, then inserts `VehiclePermitHistory`. It checks neither prior VIN/year payment nor affected row counts. The source has no call to this method from the vehicle forms. Thus the desktop UI supplies no verified rule connecting a draft to a payment amount. Phase 7 uses a saved, internally checked draft as the sole web payment amount source; this should be confirmed with BPLO before production rollout.
- `VehicleORNumberExists` checks only `VehiclePermitHistory`. That table already has a unique OR constraint. `GetVehiclePermitHistory` shows stored OR, amount, year, payment date, and recorder. The desktop vehicle payment and draft methods do not call `LogAudit`. Vehicle profile add/edit uses audit separately.
- `ResetAnnualVehiclePermitStatus` changes active Paid snapshots to Unpaid only on January 1. `DashboardForm.CheckPenalties` calls it when the dashboard loads. It does not check the paid year and has no scheduler. The web page performs no reset on GET. Instead, current-year eligibility uses the vehicle's stored year/status and immutable history; a vehicle paid last year is eligible for the new year without changing the old snapshot on page load.

## Web workflow

`/Vehicles/Index` lists active vehicles. `/Vehicles/Details/{vin}` displays vehicle details, the current permit year, the saved fee draft, and stored payment history. The draft form uses the same 19 fee keys and four description slots. The server validates and sums submitted draft items, stores the desktop JSON shape and `GrandTotal`, and does not create payment history. Existing non-fee draft details are preserved on edit.

Payment requires an active vehicle, the current server year, no existing history for VIN/year, no current-year Paid snapshot, a saved valid draft with a positive total, and a vehicle-only unique OR. The server re-reads the draft and verifies its JSON item sum equals stored `GrandTotal`; it ignores browser amount, year, status, and user fields. It uses the authenticated claim's user ID. A serializable transaction locks the vehicle and year/OR history ranges, inserts one history row, updates the vehicle status/year, and checks affected counts. Unique `(VIN, PermitYear)` and OR indexes are final safeguards. Any failure rolls back. Drafts remain stored after payment; editing one cannot alter historical `VehiclePermitHistory.AmountPaid`. No payment audit is inserted because the desktop workflow has none.

The separate OR namespaces are intentional for Phase 7. A stall OR and a vehicle OR can still have the same text. BPLO must decide whether production receipts share one series before any global uniqueness change.

## Verification

`dotnet run --project BusinessPermitLicensingSystem.BillingTests -- --vehicles` tests draft arithmetic and JSON, draft-only saves, server-authoritative amount, historical amount preservation, duplicate OR, same-year replay, wrong year, concurrent attempts, rollback, and next-year eligibility. The rollback test installs a temporary trigger limited to its synthetic VIN, forces failure after history insertion, verifies history and status rollback, then drops the trigger in `finally`. It confirms stall payment tables stay unchanged.

Two separate authenticated development accounts saved drafts and paid synthetic vehicles through the web page. They submitted false amount/year/user fields; each history row still stored 110.00, permit year 2026, and the correct signed-in user. Payment history rendered on the vehicle page. The web test used `VIN-2099-0001` and `VIN-2099-0002`, which are synthetic sample vehicles in `BPLS_Dev`.

Rollback of the web change should preserve the unique VIN/year index and existing history. Do not remove the safeguard or rewrite paid records without a separate review. The migration script can be rerun safely.
