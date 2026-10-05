# Global OR receipt integrity — development migration

An OR can be used once across StallRental and VehiclePermit payments. Existing history OR columns and report readers remain in place.

## Normalization

`ReceiptRegistry.NormalizeOR` and `dbo.NormalizeOR` remove only leading/trailing ASCII whitespace: space, tab, LF, CR, vertical tab and form feed. Internal spaces, hyphens, digits, and case are preserved. Registry uniqueness uses `Latin1_General_100_CI_AS`: case insensitive, accent sensitive. For example ` OR-001 ` and `or-001` conflict, while `OR-001` and `OR001` remain different. Existing history values are not rewritten during backfill.

## Forward path (BPLS_Dev only)

1. Stop payment writers for the maintenance window and retain a database recovery backup.
2. Run `BPLS_Dev.global-or-registry.sql` against BPLS_Dev. Its transaction locks both histories, reports normalized conflicts, aborts on blanks/conflicts without changing receipts, creates the registry and backfills every existing history row with its recorder/date.
3. Build/run the updated application. Both services reserve an OR inside their existing payment transaction before history insertion. The registry unique constraint serializes competing receipt reservations. A failed payment rolls back the reservation.
4. Ledger triggers associate reservations with history IDs, enforce global uniqueness for direct SQL history inserts, reject changes to recorded OR values, and remove registry entries when their history is deleted. No extra audit event is created.
5. Run `BPLS_Dev.global-or-check.sql`, existing regression suites and `--global-receipts`. Clean synthetic fixtures before reopening payment entry.

The registry is `Id`, `ORNumber` (nvarchar100, unique/non-null), `PaymentType` (two-value check), nullable `PaymentReferenceId` while a transaction is in progress, `RecordedByUserId`, `RecordedAt`, and UTC `CreatedAt`. A filtered unique index on payment type/reference prevents multiple receipts pointing to one completed payment. Payment references are polymorphic; their relationship is maintained by triggers and checked by the integrity script, rather than a misleading foreign key to one ledger.

Pending reservations are never committed by either payment service. Table writes should remain restricted to approved application/administrative identities; arbitrary administrative SQL can always damage data or disable constraints. Run the consistency check after authorized maintenance.

SQL Server history tables with triggers require `OUTPUT ... INTO` when returning inserted IDs. Both the Web rental writer and retained WinForms source use this compatible form. Do not run an old binary using bare `OUTPUT INSERTED.Id` against the migrated schema; rebuild first. No WinForms application or production connection was executed for this task.

## Retry and recovery

The migration is idempotent for its intended schema and existing matching rows. A rerun must not change CreatedAt, amounts, OR history, or collection totals. Conflicting data must be investigated, never renamed/deleted to force migration success.

If validation fails during migration, the transaction rolls back DDL/backfill automatically. If an application rollback is later authorized, stop all writers, verify there are no pending/missing/orphan receipts, restore the previous application source, and remove the two receipt triggers, then registry table and normalization function in a reviewed transaction. Preserve both history tables and their original OR columns. This removes the new global guarantee and therefore requires an explicit business decision; no rollback was executed in this implementation.

## Production boundary

Both SQL scripts reject any database other than BPLS_Dev. Official deployment requires a separately authorized production migration, conflict preflight on an approved staging copy, backup/restore validation, application configuration work and production acceptance. Do not edit the database guard and apply this development script to Masinloc_BPLS. No production migration or deployment was performed.
