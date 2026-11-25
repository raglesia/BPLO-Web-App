# BPLO web design system

The approved reference is **Office Overview**, the authenticated Administrative Dashboard for the Business Permit and Licensing Office of Masinloc, Zambales. Staff see pending reviews and month-to-date collections before historical reporting. The municipal seal identifies the office, blue links identify destinations, and peso amounts line up for comparison. This document records the shared screen styles and identifies the arrangements specific to that page. [Dashboard markup](Pages/Index.cshtml) · [Shared shell](Pages/Shared/_Layout.cshtml) · [Midnight + Ice screen theme](wwwroot/css/midnight-current.css)

## Color and type

The screen palette is declared on `body.bplo-system.midnight-current.midnight-current`. Older aliases still resolve through the cascade. In particular, the token named “teal” now carries the office action blue.

| Role | Screen token | Value | Used for |
|---|---|---|---|
| Institutional navy | `--bplo-navy` | `#162e58` | BPLO wordmark and prominent financial totals. The top bar itself uses `#152e58`. |
| Action | `--bplo-teal`, `--primary` | `#396fc7` | Selected controls and active navigation text |
| Dark action | `--bplo-teal-hover`, `--primary-hover` | `#28559b` | Text links, outlined buttons and hover treatment |
| Page and panel | `--bplo-canvas`, `--bplo-surface` | `#f4f7fd`, `#fff` | Ice canvas surrounding white work areas |
| Text | `--bplo-text`, `--bplo-secondary`, `--bplo-muted` | `#243b5c`, `#5c7191`, `#607695` | Primary text, descriptions and quieter metadata |
| Dividers and controls | `--bplo-border`, `--border-strong` | `#dce5f2`, `#bdcde3` | Panel edges, field and button borders |
| Tinted surfaces | `--bplo-tint`, `--surface-alt` | `#eaf1fc`, `#f8faff` | Selected or informational surfaces and table headers |

The body uses **Segoe UI, Arial, sans-serif**, 14.98px with 1.5 line height and tabular numerals. The sizes below are CSS declarations. Native font availability and more specific page rules affect the render.

| Role | Selector | Size and emphasis |
|---|---|---|
| Page title | `.page-header h1`, `.desk-header h1` | 29.96px, weight 700, line height 1.25, tracking −0.7px. At 650px and below, 26.75px. |
| Section title | `.section-panel > h2`, `.form-panel > h2` | 18.19px, weight 650, line height 1.4 |
| Labels and buttons | `.form-label`, `.btn` | 13.91px, weight 600 |
| Supporting text | `.form-text`, `.table thead th` | 12.84px. Help text uses 1.6 line height. |
| Shell metadata | `.topbar-clock`, `.sidebar-office-brand > small` | 11.77px clock on desktop, 10.7px locality |
| Dashboard figures | `.current-amount`, `.record-strip strong` | 28px current collections, 22px record counts |

A named application-wide type or spacing scale is **undefined**. The decimal sizes above belong to specific roles. [Screen declarations](wwwroot/css/midnight-current.css)

## Shell, alignment and responsive navigation

The desktop top bar carries the office name, Philippine clock and sign-out action. Beneath it, the white `.app-sidebar` runs horizontally with a 40px seal, BPLO wordmark, locality, navigation and staff identity. The current route has `aria-current="page"`. The compact header keeps the 28px circular seal beside **Masinloc BPLO**, visible even with the menu closed.

| Viewport width | Implemented behavior |
|---|---|
| Above 1250px | Full navigation. Top bar minimum height 42px. Main content is centered, capped at 1600px, with 30px top, 34px side and 25px bottom padding. |
| 1250px and below | Menu toggle and drawer replace the horizontal navigation. Drawer width is `min(310px,86vw)`, below the 54px header. Sign out moves into the drawer. The JS media query uses the same breakpoint. |
| 991px and below | Main padding becomes 25px. This is a content breakpoint independent of drawer activation. |
| 900px and below | Dashboard chart and register stack. |
| 650px and below | Main padding is 23px vertically and 16px horizontally. Page headers stack with wrapping actions beneath the title. The clock is hidden. Dashboard operational panels stack and the four record columns become two. |

The approved renders cover 1440, 1366, 1024, 992, 820, 768, 390 and 320px. At 1024px the drawer avoids collision with the account area. At 390px pending reviews and the monthly total appear together in the first screen. [Shell markup](Pages/Shared/_Layout.cshtml) · [Responsive rules](wwwroot/css/midnight-current.css) · [Drawer behavior](wwwroot/js/site.js)

## Shared page and component patterns

| Element | Implementation |
|---|---|
| Page header | `.page-header` and `.desk-header` align title and actions on opposite edges with a 24px gap and 26px bottom margin. `.page-actions` and `.desk-actions` wrap with a 10px gap. The dashboard uses **Add Stall Owner** as its primary action and **Add Vehicle** as its secondary action. The monthly report link sits beside the amount it explains. |
| Buttons and links | `.btn` has a 40px minimum height, 9px × 14px padding and 6px radius. `.btn-sm` uses 32px, 5px × 10px. Primary actions use solid dark action blue. Outline variants have a white surface and blue text. Dashboard content links are underlined with a 2px offset. Navigation uses selection and focus treatments of its own. |
| Panels | `.form-panel`, `.section-panel`, `.search-panel`, `.surface` and `.card` share white fill, a 1px border, 10px radius (`--bplo-radius`) and no shadow. Standard padding is 23px. Search panels use 19px × 22px. Section panels normally have 22px bottom margin. |
| Forms | `.form-control` and `.form-select` use a 41px minimum height, 5px radius, 1px border and 9px × 11px padding. At 650px and below the minimum is 43px with 17.12px text. Fields use the stronger `--bplo-control-border` token. Focus adds a dark blue outline. Selects retain the Bootstrap chevron. Disabled fields and readonly inputs use `#f1f5fc`. |
| Field grouping | Profile fields use `fieldset.field-group`, a legend, Bootstrap `.row.g-3` and `.col-md-6`. Razor pairs `asp-for` labels and controls with `asp-validation-for`. `.form-actions` has a top rule, 20px top padding, 25px top margin and wrapping buttons. [Profile fields](Pages/Profiles/_Fields.cshtml) |
| Tables | `.table-responsive` provides local horizontal scrolling, a 1px edge and 8px radius. The theme declares 14px × 16px cell padding and 12.84px header text. `.financial-table td.text-end` aligns tabular figures right. Record-result regions can receive keyboard focus and carry an accessible name. [Vehicle results](Pages/Vehicles/_Results.cshtml) |
| Status and feedback | `.status-badge` uses 4px × 8px padding, 4px radius and 12.84px semibold text. Text identifies paid/active, unpaid, unverified/review or archived states. Archived uses `#5c7191` on `#edf2f9`. The base screen CSS declares success `#247a4b`, warning `#a96712` and danger `#b53a3a`. Existing system colors apply to flat badges across record and payment lists. A unified set of `--bplo-*` status tokens is **undefined**. [Base states](wwwroot/css/site.css) · [Surface overrides](wwwroot/css/bplo-surfaces.css) |
| Elevation | Routine panels sit flat. The desktop navigation dropdown uses `0 8px 24px #162e5817` and 8px radius. Confirmation dialogs use `0 16px 48px #162e5833` and 12px radius. A named elevation scale is **undefined**. |

### Form and search feedback

Forms show processing feedback after client validation and confirmation. The guard locks repeated posts while the browser navigates, preserving the submitter and its form data. Use `data-processing` for an action-specific label and `data-warn-unsaved` for forms where leaving would lose meaningful edits. Server-side validation and financial calculations are authoritative.

Stall-owner and vehicle searches announce loading, results, empty results and request failures. A failed request keeps the previous results and URL. Record links and pagination retain the query through existing route values.

### Confirmations and keyboard behavior

The layout owns one native `dialog#bplo-confirm` with a labelled title, description and initially focused **Cancel** button. A submitting form supplies `data-confirm-message` and may supply `data-confirm-title` and `data-confirm-action`. The handler opens the modal and resumes the original form submission only after confirmation. Width is `min(30rem, calc(100% - 2rem))`, with 1.5rem padding, a 4px blue top border and a `#162e5866` backdrop. [Dialog markup](Pages/Shared/_Layout.cshtml) · [Submit handling](wwwroot/js/site.js) · [Dialog styles](wwwroot/css/site.css)

The skip link targets `main#main-content[tabindex="-1"]`. Closed compact navigation is inert. Tab and Shift+Tab cycle between the open drawer and its toggle. Escape inside an open disclosure closes it and focuses its summary. Escape on the open drawer returns focus to the toggle. The light navigation surface uses a 3px dark blue focus outline inset by 3px. top-bar controls use white with a 2px offset. Validation code focuses the first invalid field, validation summary, page error or success message in that order. The theme includes a reduced-motion media query. [Keyboard and validation handling](wwwroot/js/site.js) · [Focus and motion rules](wwwroot/css/midnight-current.css)

## Dashboard composition and financial data

These measurements belong to the dashboard. Shared application pages use their own task order and shared panel styles. [Office Overview](Pages/Index.cshtml) · [Selectors under `.office-overview` and `.office-history`](wwwroot/css/midnight-current.css)

- **Operational information.** `.office-operations` has two equal columns for Needs Attention and Municipal Collections. `.office-overview` uses a 16px grid gap. Its panels use 18px × 22px padding and zero outer margin. At 650px the overall gap becomes 12px and panel padding becomes 16px. Archived owners sit with directory counts. The Treasury link identifies the account it opens.
- **Records.** `.record-strip` has four equal columns, a 12px gap and a leading divider with 15px inset per item. “Unpaid billing periods” labels the count. “Find unpaid profiles” names the separate directory action.
- **History.** `.office-history` allocates .85fr to the chart and 1.15fr to the register. The six-month chart ends at the current Philippine business date. The chart area is 151px high with 123px bar groups and 12px scale, month and legend labels. Rent uses solid `#28559b`. Vehicle permits use a 135° stripe in `#52798b` and `#d2e4ec`. Each month is a button with an accessible name that lists both sources and the total. Hover, keyboard focus and activation update the on-screen breakdown. The adjacent register supplies the same exact values.
- **Register.** Month, total recorded, stall rent and vehicle permits appear in that order, newest month first. Peso values use `₱#,##0.00`, right alignment and a bold total. History cells use 9px × 10px padding. The month and total remain first in the horizontally scrollable table. The table includes a hidden caption and scoped row and column headers.
- **Zero, unavailable and loading.** A confirmed empty queue displays “No pending occupancy or Treasury reviews.” Six zero months retain the register and replace bars with a plain message. Missing counts display an em dash and explanatory text. Missing collections suppress the historical sections. **Retry dashboard** uses a live status for “Refreshing dashboard…” while navigating. The initial dashboard is server-rendered. A shared skeleton, global loading overlay and standard loading delay are **undefined**. Directory partials show load errors with `role="alert"`. shared `.note` and `.empty-state` provide informational and dashed-border treatments elsewhere.

## Razor and stylesheet ownership

| Source | Responsibility |
|---|---|
| [Shared layout](Pages/Shared/_Layout.cshtml) | Authenticated shell, skip link, seal, clock, navigation, identity, main landmark, footer and confirmation dialog. |
| [Workflow guide](Pages/Shared/_WorkflowGuide.cshtml) | Route-specific native details/summary guidance. The layout inserts it before other page bodies. Office Overview places it after the dashboard content. |
| [Validation scripts](Pages/Shared/_ValidationScriptsPartial.cshtml) | Loads jQuery Validation and unobtrusive validation for Razor forms. |
| [Profile fields](Pages/Profiles/_Fields.cshtml) · [Profile results](Pages/Profiles/_Results.cshtml) · [Vehicle results](Pages/Vehicles/_Results.cshtml) | Feature-specific partials for bound fields and searchable record tables. General-purpose Razor button, panel and chart components are **undefined**. |
| [Base CSS](wwwroot/css/site.css) | Foundation, accumulated module rules, print layouts, confirmation dialog and workflow guide. |
| [System CSS](wwwroot/css/bplo-system.css) | Earlier shared theme and module rules, many scoped outside `.dashboard-workspace`. |
| [Dashboard CSS](wwwroot/css/dashboard.css) | Home-only predecessor styles, including older shell treatments and high-specificity focus rules. |
| [Surfaces CSS](wwwroot/css/bplo-surfaces.css) | Remaining surface and module rules. Current owns shared button, table and form presentation. |
| [Midnight + Ice](wwwroot/css/midnight-current.css) | Current screen palette, shared theme, responsive shell and final dashboard-specific rules. |

**Cascade.** The layout links Bootstrap, `site.css`, the generated `BusinessPermitLicensingSystem.Web.styles.css` bundle reference, `bplo-system.css`, home-only `dashboard.css`, `bplo-surfaces.css`, then `midnight-current.css`. Specificity and legacy `!important` declarations can override load order. For example, the approved navigation ring explicitly overrides the old white dashboard ring. The unused Current predecessor dashboard selectors have been removed. Older module and layout rules still require coverage checks before removal. The current Razor view renders `.office-operations`, `.record-strip` and `.office-history`. Approval applies to that rendered composition.

## Print

Screen panels and navigation are separate from formal document layouts. The print rules hide application chrome and the workflow guide. Rental payment documents use the named `rental-payment-clean` page, A4 portrait with zero page margin and 10mm padding on `.rental-signature-paper`. The vehicle mayor’s permit has its own stylesheet and named `mayor-permit` A4 portrait page with 8mm margins. Those dimensions belong to the respective forms. A dedicated dashboard print composition and a universal municipal form margin are **undefined**. [Rental and shared print rules](wwwroot/css/site.css) · [Mayor’s permit rules](wwwroot/css/vehicle-mayors-permit.css)
