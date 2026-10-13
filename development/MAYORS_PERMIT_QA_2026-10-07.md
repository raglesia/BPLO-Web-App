# Special Vehicle Mayor’s Permit — Development QA

Date: October 7, 2026
Scope: Main BPLO web application, BPLS_Dev only. No deployment or demo synchronization.

## Implemented

- New route: /Reports/VehicleMayorPermit?id=<VehiclePermitHistory.Id>.
- Access: Vehicle Details → Previous Annual Permit Payments → Print Mayor’s Permit.
- Prepare assigns a permanent number through an authenticated antiforgery-protected POST. Merely viewing a report does not allocate a number.
- Numbers use the current Philippine issuance year and an ascending sequence, starting at YYYY-0001. A new year has its own counter. Reprints keep the original number.
- Number allocation uses a serializable transaction, a transaction-owned exclusive SQL application lock, and unique constraints.
- Active vehicle, recorded OR payment, matching saved assessment, Company, Driver Name, Location, Line of Business, and Description are required before preparation.
- Incomplete details may be reviewed in an explicitly unissued screen preview. The preview has no preparation/printing button and is hidden by print CSS.
- Municipal seal, blue wave header, municipal hall watermark, approval block, six terms, Serbisyon Potog logo, and dynamic validity year follow the supplied references.

## Field mapping

| Document field | Source |
|---|---|
| Business Name | Vehicle CompanyName |
| Tax Payer’s Name | Vehicle DriverName |
| Location / Line of Business / Description | Saved annual fee assessment |
| Mayor’s Permit amount | Saved Mayor’s Permit Fee |
| OR | Existing VehiclePermitHistory.ORNumber |
| Payment Date | Existing DatePaid |
| Date Issued / Date Printed | Same current Philippine date at page load |
| Validity | December 31 of the payment’s permit year |
| Place Issued | MASINLOC, ZAMBALES |

Business Tax, TIN, PAG-IBIG, SSS and PhilHealth are omitted as requested. CTC is omitted because no corresponding field is available.

## Files for this feature

- BusinessPermitLicensingSystem.Web/Pages/Reports/VehicleMayorPermit.cshtml
- BusinessPermitLicensingSystem.Web/Pages/Reports/VehicleMayorPermit.cshtml.cs
- BusinessPermitLicensingSystem.Web/Reports/VehicleMayorPermitService.cs
- BusinessPermitLicensingSystem.Web/wwwroot/css/vehicle-mayors-permit.css
- BusinessPermitLicensingSystem.Web/wwwroot/images/mayors-permit/municipal-hall.png
- BusinessPermitLicensingSystem.Web/wwwroot/images/mayors-permit/serbisyon-potog.png
- BusinessPermitLicensingSystem.Web/Program.cs: scoped service registration
- BusinessPermitLicensingSystem.Web/Pages/Vehicles/Details.cshtml: payment-history action
- development/BPLS_Dev.vehicle-mayors-permit.sql: development-only schema

## Executed checks

| Check | Result / evidence |
|---|---|
| Full solution build | PASS, zero errors; NU1900 vulnerability-feed connectivity warning and pre-existing NU1510 package warning |
| Rebuilt web project | PASS, zero errors; NU1900 warning |
| Existing database-free regression suite | PASS, exit code 0 |
| Number allocation SQL | PASS: first 2099-0001, repeated payment remains 2099-0001, another payment becomes 2099-0002 |
| Test allocation cleanup | PASS: transaction rolled back; issued permit count 0, counter count 0 afterward |
| Payment-history action | PASS, observed in authenticated vehicle details |
| Missing/nonexistent payment | PASS, friendly error; no permit or issuance control |
| Assessment/payment mismatch | PASS, friendly review message; no permit or issuance control |
| Missing required details | PASS, friendly completeness message, explicitly unissued preview only |
| Driver mapping | PASS, existing stored DriverName shown under Tax Payer’s Name |
| Date mapping | PASS, both displayed October 07, 2026 |
| Assets | PASS, all three permit images loaded with nonzero natural width |
| Desktop visual inspection | PASS for unissued preview at 1920 × 1080; header, fields, receipt, approval, watermark, terms, logo and validity visible |
| Narrow browser inspection | Preview readable; wraps vertically. Official print output remains A4 portrait |
| Browser session after restart | PASS, session remained signed in |
| Production | No production connection or deployment performed |

## Remaining acceptance limits

No existing active paid record currently has all required permit details and a matching saved assessment. Therefore preparation through the browser and the final numbered official browser print preview were NOT exercised. No profile/assessment/payment data was altered to manufacture eligibility. The visual specimen uses an existing synthetic development record and is visibly marked unissued.

Print CSS hides the application shell and unissued previews, sets A4 portrait margins, and preserves branding colors. Physical printer output and final official print pagination remain unverified until a complete eligible paid record is available. Long descriptions may need additional pagination review.

The existing report service reads the current same-year assessment rather than an immutable payment-time snapshot. Amount mismatches are blocked; same-total detail edits remain visible on reprints. No fee, payment, OR or collection calculation was changed.

The new schema has been applied only to BPLS_Dev. Production setup/schema integration is outside this task and has not been performed.

Desktop screenshot: outputs/mayors-permit-desktop-preview.jpg (outside repository root).

## Approved fee-only refinement

The user clarified that the Mayor’s Permit includes only the Mayor’s Permit Fee, excluding all other assessment fees. The Mayor’s Permit page therefore no longer blocks on a total-assessment/payment mismatch. This supersedes the total-match eligibility statements above; payment reports and financial workflows retain their existing behavior.

Rebuilt web project: PASS, zero errors, NU1900 vulnerability-feed warning only. Restarted main localhost. Authenticated browser verified payment 357 / VIN-2026-0001: NVIDIA, Driver John, Location Zambales, PC Components, Graphics Cards, Mayor’s Permit Fee ₱300.00, OR 123456789, and enabled Prepare Mayor’s Permit. No Occupational Permit or other fee is displayed. No permit number was allocated during this read-only verification. Final official numbered print preview still requires preparation.

## A4 pagination correction

User reported validity footer spilling onto page two after document headers were restored. Removed the forced 287 mm minimum print height, assigned a named A4 page with 8 mm margins, and kept the validity block with the permit. Added a beforeprint fit pass measured at the actual 194 mm print width; the full document scales only when its measured height exceeds 279 mm (281 mm printable area minus 2 mm clearance). No content is clipped or removed. Screen sizing is restored after printing.

Web build passed, zero errors, NU1900 feed warning only. Main app restarted. Browser Print was invoked for payment 447 / permit 2026-0003; the native modal blocked browser automation. Actual preview sheet count is pending user confirmation; do not treat this as final print acceptance yet.
