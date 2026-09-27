# Special Vehicle Permit Payment report — completion record

1. Inspected vehicle details, annual payment service, `VehiclePermitHistory`, `VehiclePermitFeeDrafts`, `VehiclePermits`, Phase 15 notes, Reports, and current print styles.
2. No confirmed complete WinForms vehicle payment report was used as a reference.
3. Reused Stall Owner report document classes, search/selection pattern, A4 print rules, and the current app shell.
4. Reused `wwwroot/images/masinloc-logo.jpg`; no second logo copy was created. The original `Masinloc Logo.jpg` remains untouched.
5. Each recorded annual payment renders as one formal, bordered municipal report block.
6. Fields include company/operator, driver, plate, VIN, permit year, paid status, OR number, and payment date.
7. SEC and DTI registration fields appear only when nonempty.
8. Payment ID, year, OR, date, paid amount, and recorder come from `VehiclePermitHistory`.
9. Fee rows come from `VehiclePermitFeeDrafts` for the same VIN and permit year. All 19 established fee items appear when a valid draft exists.
10. `VehiclePermitHistory.AmountPaid` always controls **Total Amount Paid**. Current vehicle status and draft total never replace it.
11. Each block represents exactly one payment ID and permit year.
12. Paid status follows the existence of that specific history row, not current `VehiclePermits.PermitStatus`.
13. Transaction fields use history values. Vehicle identity and fee assessment use currently stored records because the history table does not snapshot them.
14. Archived vehicles remain searchable and their recorded payments remain printable.
15. `Processed By` resolves the history `RecordedBy` user to current full name and position. User names and positions are not snapshotted in history, so later user edits can change displayed labels.
16. `Date Printed` uses local preview generation time, separate from payment date.
17. Every completed payment row on Vehicle Details has a `Print Payment Report` link with its exact history ID.
18. Reports contains `Special Vehicle Permit Payment` selection. Staff choose recorded payments without typing IDs.
19. Server-side paged search matches company, driver, plate, VIN, and OR. Permit year filters results. History search includes archived vehicles.
20. Browser code blocks a fourth selection and displays the required limit message.
21. Server rejects more than three IDs; it never truncates the request.
22. UI marks selected payments; server rejects duplicate and invalid IDs.
23. Blocks stack vertically without forced per-owner page breaks. Multiple blocks may share an A4 page when their contents fit.
24. Each block uses `break-inside: avoid` and `page-break-inside: avoid`. Physical browser pagination remains unverified because the in-app browser did not expose its print dialog.
25. Each block repeats the Masinloc seal and municipality/BPLO header.
26. Shared print CSS uses A4 portrait, 10 mm margins, hidden app chrome, and print-safe borders. Fee assessment uses a compact two-pair table.
27. A later synthetic draft change from ₱123.45 to ₱124.45 produced a clear mismatch note. Report retained recorded ₱123.45 payment amount; neither record was rewritten by report generation.
28. Keyboard search, selection, removal, preview, and Print button activation were exercised. Native links/buttons cover direct history print.
29. Selection and preview had no document-level horizontal overflow at 1440, 1280, 1024, 768, and 390 px.
30. Browser checks found no history, draft, or audit row changes after viewing reports.
31. Stall Owner report browser checks passed.
32. Existing detailed and monthly report checks, including Excel, passed.
33. Full database and browser regression suites passed against `BPLS_Dev`.
34. Web and test projects built with zero warnings and errors using restored dependencies.
35. Only `BPLS_Dev` was used for SQL-backed work and tests.
36. Fee drafts remain editable after payment; their current values may differ from the original assessment. User names, positions, and vehicle identity are likewise not historical snapshots. The report states these limits and flags paid-amount mismatch. Physical print pagination remains unverified.
37. No deployment, IIS, LAN, firewall, certificate, workstation, or production SQL work occurred.
