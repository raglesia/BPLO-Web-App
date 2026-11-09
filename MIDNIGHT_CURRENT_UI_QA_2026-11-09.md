# Midnight + Ice / Current presentation rollout

The approved Current dashboard composition and Midnight + Ice palette now define the shared web presentation: a navy office strip, horizontal desktop navigation, white work surfaces, blue actions, aligned financial tables and a gradient municipal collection summary. The existing mobile drawer retains its controls and keyboard behavior.

## Scope

- Updated `Pages/Shared/_Layout.cshtml`, `Pages/Index.cshtml` and the new `wwwroot/css/midnight-current.css`.
- Shared styling reaches profile, vehicle, billing/payment, rental-rate, report, archive, administration, transfer, authentication and error pages.
- The dashboard uses its existing model values and queries. Its lower table displays the existing monthly collection data, rather than adding a recent-receipts query or copying fictional mockup figures.
- An explicit zero-collection state replaces an unexplained empty chart.
- The main SQL Server app and the separate SQLite demo receive the same presentation. Their existing routes, storage providers, authentication, handlers, fees and transactions are retained. The demo's Billing & Payments navigation reuses its existing module destination.
- The SQLite demo's older glass-theme include is superseded by the new screen theme. Its data, cookie settings and key location are retained.

Both requested taste skills informed hierarchy, palette, spacing, component consistency, restrained pointer feedback and responsive review. Marketing heroes, randomized layouts, testimonials and scroll effects were not applied because the user explicitly selected an operational dashboard design.

## Validation

- Main ASP.NET Core application build: successful, zero errors.
- SQLite Windows self-contained publish: successful.
- Existing database-free billing and permit regression checks: 60 passed.
- Isolated authenticated SQLite page sweep: 34 existing page requests passed, including valid route parameters and expected redirects. The login capture uses a separate unauthenticated request.
- Actual rendered page captures cover the dashboard, profile details, profile creation, vehicle assessment and login. Dashboard layouts were inspected at 1440, 1024, 820, 768 and 390 pixels.
- Mobile drawer checks passed: closed drawer is inert; open state exposes navigation; Tab enters the drawer; Escape closes it and restores focus; backdrop closes it and restores focus. Removing an inherited visibility transition resolved immediate focus timing. The existing JavaScript handlers were retained.
- No page overflow was reported by the final five-width/focused layout checks. The taste layout checker still reports marketing-specific findings about full-screen sections, hero placement and card text inset from page edges. These are documented exceptions for this operational application, not a clean marketing-layout checker pass.
- Formal report source files and print styles were not changed. The new theme is enclosed in `@media screen`; physical printing remains a manual check.

## Local demo preservation

Before replacing the local application build, a consistent SQLite backup and logical record fingerprints were recorded outside the repository. The previous application directory is retained separately. The database is not replaced during the application-directory switch. A comparison after startup verifies the same records across all 17 tables.

The isolated QA copy uses its own synthetic account and vehicle fixture. Those fixtures are not installed into the running demo database. QA screenshots, snapshots, credentials, keys, database copies, compiled packages and logs remain outside this repository.

## Existing build warnings

The main application's unchanged dependency graph reports NU1902 and NU1903 for `System.Security.Cryptography.Pkcs` 9.0.13: GHSA-2fmv-rv2c-59x7 and GHSA-846f-f764-76mx. No dependency update or security remediation was included in this presentation task.

The local .NET launcher was missing, so an official .NET SDK was installed into the workspace tool directory to build the application. No project package versions were changed.
