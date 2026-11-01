# Back office (admin + staff UI) — design

Date: 2026-11-01 · Owner: Jordan · Branch: `feature/UI-back-office`

## Problem
Admins and staff are lost. After login they land on the customer home page. The navbar mixes customer
links (Book a table, My orders) with staff links and "Admin". Every admin page hangs off one flat hub of
11 cards, and the staff Orders board has no link at all (BUG1).

## Decisions (agreed with Jordan, 1 Nov)
- **Structure, not a rebuild.** A back-office shell around the existing pages; page contents stay as they are.
- **Admin and Staff share one shell.** Menu items are filtered by role. This also fixes BUG1.
- **Look: "Peranakan Tile".** Continues the customer site: deep teal ink side menu with a tile strip,
  asam-plum accent, jade for positive numbers, warm paper background, Georgia headings, system UI text.
  No web fonts (the demo must work offline). Rejected: Kopitiam, Batik Indigo, Pasar Malam (dark).

## Layout
`Pages/Shared/_BackOfficeLayout.cshtml`, applied by a `_ViewStart.cshtml` in `Pages/Admin` and `Pages/Staff`.

- **Side menu** (248 px, sticky; on screens below `lg` it slides in from the left as a Bootstrap offcanvas,
  opened from a menu button in the top bar). One `<nav>`, no duplicated markup.
- **Top bar:** today's date, the user's name, "View customer site", Logout.
- **Main area:** banners (`_Banners`), then the page. The page's own `.container` is widened and
  left-aligned inside the shell.
- No footer.

## Menu (role-filtered, `Pages/Shared/_BackOfficeNav.cshtml`)
| Group | Items | Who |
|---|---|---|
| Insights | Dashboard, Monitoring, Customer activity | Admin |
| Operations | Orders board, Reservations, Billing, Reviews | Staff + Admin |
| Restaurant | Menu, Dining areas, Tables, Operating hours, Settings, All configurations (the existing hub) | Admin |
| Marketing | Promotions, Announcements | Admin |
| People | Users | Admin |

The active item is matched by page-path prefix (e.g. `/Admin/Menu/` keeps "Menu" active on Edit Item).

## Customer navbar (`_Layout.cshtml`, shared file)
- Customers and visitors: unchanged.
- Staff and Admin: Home, Menu, and one **Back office** link. "Book a table", "My orders" and the separate
  staff/admin links are hidden for them. This supersedes the UX4 one-liner on `fix/UX4-configurations-label`.

## Landing after login (`Areas/Identity/.../Login.cshtml.cs`, Carynn's file)
Only when no explicit `returnUrl` was given: Admin → `/Admin/Dashboard`, Staff → `/Staff/Orders`,
everyone else → home as before. Roles are read with `SignInManager.UserManager` (no new injection).

## Styling (`wwwroot/css/backoffice.css`, new)
Everything is scoped under `body.bo`, so the customer site cannot change.
- Bootstrap's blue (`btn-outline-primary`, `bg-primary`) is re-mapped to the palette inside the back office.
- Status badges become soft tinted pills; colour meanings stay (warning = pending, info = in progress,
  success = done, danger = cancelled, secondary = other).
- Headings slightly smaller than on the customer site; tables and cards keep the hairline style.

## Out of scope
Re-designing the inside of each page, dark mode, live counters in the menu, schema or service changes.

## Shared files touched — group OK needed in the PR
`_Layout.cshtml` (navbar for staff/admin), `Login.cshtml.cs` (Carynn: landing page),
and the frame of Wallace's and Carynn's pages (no change to their markup).

## Manual test
1. Customer: navbar unchanged; `/Admin` → Access denied.
2. Staff login → Orders board; menu shows Operations only; `/Admin/Dashboard` → Access denied.
3. Admin login → Dashboard; every menu link opens and stays highlighted on its sub-pages.
4. Login with `?ReturnUrl=/Admin/Menu` → goes to Menu, not Dashboard.
5. Phone width (375 px): menu button opens the slide-in menu; no horizontal scroll.
6. Banners still show after a save; badges and buttons have no Bootstrap blue.
