# Phase 8: stall and vehicle archive / restore

## Desktop reference

- `ArchiveProfiling` and `RestoreProfiling` set only `Profiling.IsArchived` to 1 or 0. Normal profiling list selects 0; `ArchivedForm` selects 1 and can open payment history. Its archive and restore buttons ask for confirmation, then log `Archive` or `Restore` in `AuditTrail` with SIN and current user. The desktop audit call is separate from the update.
- `ArchiveVehiclePermit` and `RestoreVehiclePermit` set only `VehiclePermits.IsArchived`. Normal vehicle list selects 0; `GetArchivedVehiclePermits` selects 1. The vehicle-list archive button confirms and logs `Archive Vehicle Permit`, placing VIN in the audit table's SIN column. No desktop caller of `RestoreVehiclePermit` was found, so desktop vehicle restore has no observed UI or audit behavior.
- Neither desktop archive method checks paid/unpaid status, outstanding billing, or permit state. Neither method deletes history or changes an identifier. The desktop operations do not check affected-row count; the web operations do.

## Web behavior

- Active profile and vehicle detail pages offer a confirmed POST archive action. `/Archive/Profiles` and `/Archive/Vehicles` show archived records, basic search, identifying fields, history links, and confirmed POST restore actions. All pages require authentication; Razor Pages supplies anti-forgery validation.
- Each transition reads the current state with SQL Server `UPDLOCK, HOLDLOCK` in a serializable transaction, updates only `IsArchived`, inserts an audit row, and commits. A repeated request returns a clean already-active or already-archived message. A failed audit rolls back the state update.
- Profile audit actions and detail text match desktop behavior. Vehicle archive matches desktop behavior. Web vehicle restore adds `Restore Vehicle Permit` with VIN and company name for traceability; this is an explicit web extension because no desktop restore caller was found. The existing SIN-oriented audit schema is reused without redesign.
- The SQL service checks both connection-string Initial Catalog and connected database name for `BPLS_Dev`. No schema change is needed. `Masinloc_BPLS` is not used.
- Archive/restore does not change SIN, VIN, billing, payment history, payment-to-bill links, permit history, fee drafts, stored permit state, or audit history. Restore does not generate billing. Existing billing and payment services reject archived records, while their history views remain available.

## Verification

- Full solution build passes with `-p:BaseOutputPath=phase8-bin\\`. The default output was locked by an existing web preview process; the isolated build had one pre-existing `NU1510` warning from the WinForms project.
- `ArchiveScenarios.RunAsync()` passed against synthetic `BPLS_Dev` profile and vehicle rows: active/archive lists, repeat archive, billing generation and both payment rejections while archived, preserved monthly bill, payment, payment-bill link, permit history and draft, restore, unchanged bill count, and four audit transitions.
- HTTP check on the Phase 8 preview: anonymous restore POST redirects to login; authenticated restore POST without anti-forgery token returns 400; authenticated archive list GET returns 200.
- No WinForms source was edited. The development database contains the synthetic test records used in this verification; they are restored and clearly named.

## Pending policy / next phase

Phase 7 vehicle fee-draft prerequisite and separate stall/vehicle OR-number namespaces remain unchanged pending BPLO policy. Proposed Phase 9 scope: read-only reports and search over the migrated profile, billing, payment, vehicle, and archive data, with exact report definitions confirmed before implementation. No Phase 9 work began here.
