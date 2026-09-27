# Stall Owner Rental Payment web report — completion record

1. Inspected `ReportViewerForm`, `BillingReport.rdlc`, `BillingReportModel`, and the WinForms three-profile selection caller. The RDLC repeats a municipal header, profile grid, four charge rows, staff identity, and printed date per owner.
2. Reused `ProfileService.ListAsync` and `GetAsync` for active search/selection and `ReportService.ProfileAsync` for read-only stored billing snapshots. Kept the old detailed billing history route and monthly collection report.
3. Used `BusinessPermitLicensingSystem/Masinloc Logo.jpg` as the logo source.
4. Copied it to `BusinessPermitLicensingSystem.Web/wwwroot/images/masinloc-logo.jpg`.
5. Source and copy SHA-256 hashes match: `8655955356E63DE1296A1961F3C7D114FA15A9FFC87FD480E7F527F7AB9E3654`. The source was not edited.
6. Each report is a compact bordered municipal document that stacks vertically in selection order.
7. Each includes owner, business, stall, SIN, section, status, processed date, rent, penalty, additional charge, total due, staff name/position, and printed date.
8. Charges come from stored `MonthlyBilling` snapshots through `ReportService.ProfileAsync`.
9. The document totals all **stored unpaid** periods for that owner. It does not create missing periods. The older detailed billing history still displays all stored periods, paid and unpaid. The WinForms RDLC used profile-level values rather than period-linked due amounts; its billing scope was not explicit.
10. Base rent and additional charge are separate; `ReportBill.Total` uses the existing `BillingRules.Total` calculation. The report displays stored penalty without changing it.
11. Any ambiguous historical bill marks the report `Review Needed` and suppresses the rent and total figures. Staff must review it in the billing workflow.
12. `Processed By` reads authenticated `FullName` and `Position` claims on the server.
13. `Date Processed` is the local preview generation time in a readable format.
14. `Date Printed` uses the same preview timestamp so all blocks in a print job agree.
15. Search uses the existing paged server-side active-profile query; it matches owner, business, stall number, and SIN.
16. Selected owners remain visible with their name, business, stall, and SIN; removal and preview are ordinary forms.
17. The browser blocks a fourth Select submission and displays the required limit message.
18. The server rejects more than three submitted SINs with the same message.
19. The UI marks an already selected owner; the server rejects repeated SINs case-insensitively.
20. The submitted SIN order is preserved by sequential server loading and rendering.
21. One to three complete reports appear in a combined preview with Back and Print controls.
22. Blocks have no forced page break between them, allowing two on one A4 page when space permits.
23. Each block has both `break-inside: avoid` and `page-break-inside: avoid`.
24. Print pagination flows naturally; a block that does not fit should start on the next page. Browser-specific physical print pagination was not visually confirmed because the in-app browser did not expose its print dialog.
25. Each block repeats the copied seal and full municipality/BPLO heading.
26. Print CSS requests A4 portrait with 10 mm margins, hides application chrome and controls, and keeps black borders and legible text.
27. Screen preview was inspected with two owners. Measured blocks were approximately 474 and 491 screen pixels high at 1440 px, which suggests both fit within A4's printable height, but a physical print/PDF preview remains unverified.
28. Search, Select, Remove, Preview, and Print use labeled native inputs, forms, links, and buttons; automated keyboard-only traversal was not performed.
29. At 1440, 1280, 1024, 768, and 390 px, the selection and preview pages had no document-level horizontal overflow. The narrow report itself can scroll inside its paper preview.
30. Preview calls only read methods. Browser checks compared billing, payment, and audit row counts before and after preview and found no change.
31. Existing detailed rental and monthly report browser checks passed; the monthly Excel test passed.
32. Database, payment, vehicle, archive, transfer, report, administration, and browser regression suites passed against `BPLS_Dev`.
33. Web and test projects built with zero warnings and errors using existing restored dependencies.
34. All SQL-backed tests and the local web app used `BPLS_Dev`; no production database was accessed.
35. The main scope ambiguity is that the WinForms RDLC used profile-level totals while the old web detailed report listed all stored bills. This document uses stored unpaid bills to express amount due. It shows stored penalties and does not perform billing preparation. Physical A4 pagination and keyboard-only traversal still need a human print/browser pass.
36. No deployment, LAN, IIS, firewall, certificate, or production migration work occurred.
