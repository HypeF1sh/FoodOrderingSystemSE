# Progress

Keep this file short. Newest log entries on top. Claude reads it at the start of every session.

## Current position
- **Today's sprint:** Sprint 4 (29 Oct–3 Nov) — closing gaps (PB20 promotions, BUG1, UX3), then extras and UI refining
- **Internal target:** 4 Nov · **Formal deadline:** Fri 6 Nov 2026

| Sprint | Dates | Goal |
|---|---|---|
| 1 | 8–15 Oct | All three roles can log in; layout and DB schema in place |
| 2 | 15–22 Oct | Customer can browse menu and book a table end to end |
| 3 | 22–29 Oct | Full order → status → payment flow |
| 4 | 29 Oct–3 Nov | History, feedback, notifications, analytics dashboard |
| Freeze | 3–5 Nov | Testing, fixes, report finalisation |

## Open decisions (blocking)
| # | Decision | Owner | Blocks |
|---|---|---|---|
| D1 | **ASP.NET Core Identity** chosen by Jordan — Carynn to confirm, then scaffold it (project was created with no auth) | Carynn | PB01–PB04, every `[Authorize]` page |
| D3 | Dev database: SQL Server LocalDB per developer, `DefaultConnection` (proposed, as in labs) | Group | First migration |
| D4 | Fold receipt/confirmation (Intermediate #3) into PB10 and PB16 | Group | PB10, PB16 scope |
| D5 | **One dashboard per role** (raised by Jordan 30/10). Today Staff and Admin land on the customer home page and reach their pages by URL. Questions: (1) **Staff home** `/Staff`: today's orders board, today's reservations, unpaid bills, today's order count and takings? (2) Should **staff** see money totals at all (today's takings only) or is **revenue/sales reporting admin-only** (PB22)? (3) **Profit** cannot be shown: there is no cost price on `MenuItem` — revenue only, unless the group agrees a schema change. (4) **Admin home**: PB22 dashboard (sales, reservations, popular dishes, date filter) + PB21 monitor (all reservations, orders, payments, read-only) + link to Configurations. (5) What admin must **not** do: card details are never stored, so nothing to hide there; should admin be able to change order/payment status, or only staff (audit trail)? (6) Can staff see customer phone/email (BUG2) — yes for bookings? | Group (Jordan: PB22 + order data; Carynn: PB21 + admin hub; Wallace: staff reservations) | BUG7, BUG8, PB21, PB22, BUG1 |

## Decided
- One project with layered folders (Pages / ViewModels / Services / Models / Data / Migrations), matching the labs.
- Scrum Master: **Jordan**.
- Auth: ASP.NET Core Identity (pending Carynn's confirmation — see D1).
- UI direction: modern minimal.

## Known issues (logged, not fixed yet)
| # | Issue | Owner | Notes |
|---|---|---|---|
| UX1 | **(Fixed 30/10, `fix/BUG6-UX1-orders-nav-cart`.)** **Add to cart is not friendly (PB14, `/Menu`).** (a) Every "Add" reloads the page and jumps back to the top of the menu, so the customer loses their place. (b) The "added" message is a banner at the top, not a floating notification. (c) There is no confirmation step, so one mis-tap adds a dish. | Jordan | Wanted: stay at the same scroll position (no full reload), a floating toast notification, and a quick confirm before adding (e.g. a small dialog with the quantity). Found 22/10 while testing the cart. A floating toast for every page would mean changing the shared `_Banners` / `_Layout.cshtml`, which needs group agreement; a toast on the menu page only does not. |
| UX3 | **Table booking form is confusing (PB09/PB10, `/Reservations`).** (a) **Date:** customers can pick any date. Limit booking to **1 week ahead**, and refine the date picker's look using Grab Malaysia's date picker as the reference. (b) **Time, hours:** the hour picker scrolls endlessly (wraps around). It should stop at the end of the day, ideally showing only opening hours. (c) **Time, minutes:** too fine-grained and also scrolls endlessly. Use **10-minute steps** with no wrap-around. (d) **Party size:** replace the up/down number spinner with a **dropdown**, with the maximum set by the largest table the restaurant has. | Wallace (raised by Jordan) | Still to agree: does "1 week ahead" mean 7 days from today or to the end of next week? The 7-day limit must also be checked in `ReservationService`, not only in the UI. The dropdown maximum should come from the table data (Carynn's `RestaurantTable` via her service), not a hard-coded number. **Wallace's domain:** raise it with him. |
| BUG1 | **Navbar is not role-aware.** A Staff account sees the customer links **"Book a table"** and **"My orders"**. Staff should see **"Reservations"** (the staff reservation list) and **"Orders"** (the staff order board, `/Staff/Orders`). Customers keep "Book a table" and "My orders". | Jordan (layout) + Wallace (staff reservations page) | Cause: `_Layout.cshtml` lines 66–72 show both links to every user, and the `User.IsInRole("Staff")` block (line 86) is still a comment. Fix needs an edit to the **shared `_Layout.cshtml` → group agreement**. The staff "Reservations" link needs Wallace's PB12 page, which doesn't exist yet, so link it once his page is merged. "My orders" is still a placeholder until PB18. Also decide what Administrators see. |
| BUG2 | **Register has no phone number.** Customers sign up without a phone number, so staff cannot call them to verify a booking or reach them about urgent changes. | Carynn (PB01) | Add a required Malaysian mobile field to `Register` (and to Manage profile, PB02). **No schema change:** `ApplicationUser` inherits `PhoneNumber` from `IdentityUser`. Existing accounts have none, so decide whether to prompt them for it at booking time. Wallace's staff reservation views (PB12/PB13) should then show the number. |
| BUG3 | **Navbar greeting shows the email**: top right reads "Hello customer@rroms.com!" instead of the person's name. | Carynn (Identity, `_LoginPartial`) | Cause: `_LoginPartial.cshtml` line 9 uses `User.Identity?.Name`, which in Identity is the username (= email). Should show `ApplicationUser.FullName`, falling back to the email if it is empty. |
| UX4 | **Rename the "Admin" navbar tab to "Configurations"** so it's clear that's where everything editable lives (menu, tables, hours, promotions, settings, users). | Carynn (admin hub) | Rename the link text in `_Layout.cshtml` line 79 (**shared file → group agreement**). Also rename the hub page's title and heading (`Pages/Admin/Index.cshtml` lines 4 and 8) to match. The `/Admin` URL and folder can stay as they are. Pairs with UX2 (adding a Menu card to the same hub). |
| BUG5 | **(Fixed on Jordan's pages 28/10; still open on `Admin/Users`, `Reservations/Index`.)** **Bootstrap blue breaks the colour palette.** The Checkout "Your order" header is bright blue, and after **Pay now** the Pay page header, Card/E-wallet tiles and buttons are blue too. None of it matches the site palette (ink `#0F2E2B` + burgundy accent `#A3334A`). | Jordan (Checkout, Confirmation, Pay pages) + group (theme) | Cause: `site.css` maps links to `--rr-accent` but **never re-themes Bootstrap's "primary"**, so `bg-primary`, `btn-primary` and `btn-outline-primary` stay Bootstrap's default blue `#0d6efd`. The green `bg-success` headers (Confirmation) clash too. Same issue on `Admin/Users`, `Reservations/Index`. **Best fix:** map Bootstrap's primary to `--rr-accent` once in `site.css` (**shared file → group agreement**), which fixes every page. **Fallback:** on my own pages, swap the coloured headers for the neutral card style. |
| UX2 | Admin cannot reach `/Admin/Menu` from the UI: no Menu card on the Admin hub | Jordan / Carynn | See log 22/10. Needs Carynn's OK |
| BUG6 | **(Fixed 30/10, `fix/BUG6-UX1-orders-nav-cart`.)** **Receipt has no way back to order history (PB18).** Customer: My orders → View receipt → "Back to my order" (Confirmation) → only "Back to menu". Misleading: the customer came from **My orders** and can't get back except via the navbar. | Jordan | `Payments/Receipt.cshtml` line 87 links to Confirmation; `Orders/Confirmation.cshtml` line 53 only offers "Back to menu". Fix: add a "Back to my orders" link (`/Orders`) on both, or pass a `returnTo` so the button returns where the customer came from. My pages only, no shared file. |
| BUG7 | **Admin has no order view in the UI.** The `/Staff` folder allows `StaffOrAdmin`, so an admin *can* open `/Staff/Orders` by typing the URL, but there is no link and no admin-specific read-only list. | Jordan + Carynn (PB21) | `GetOrdersAsync` / `GetPaymentsAsync` for admin monitoring are on `feature/admin-list-methods` (not merged yet). PB21 admin monitor page is Carynn's. Depends on D5 and BUG1. |
| BUG8 | **Staff have no overview.** Staff can only see one day's order board; no totals (orders today, takings today, unpaid count) and no landing page of their own. Staff UI reuses the customer navbar/home. | Jordan (order/payment numbers) + Wallace (staff reservations) + group (staff home) | Needs D5 decided first: what staff may see (money or not). Profit is not possible without a cost price (schema change). Staff home would be a new `Pages/Staff/Index`; navbar link needs `_Layout.cshtml` (group OK). |

## Ideas (not scheduled yet)
| # | Idea | Owner | Notes |
|---|---|---|---|
| I1 | **"Hot picks" row on `/Menu` (PB08 extension).** A row or category at the top of the menu showing the dishes customers have ordered most recently, so a new customer can spot the signature dishes. | Jordan | Data-driven: count `OrderItems` over a recent window (e.g. last 30 days), excluding Cancelled orders. Show **min 2, max 4** dishes; hide the row if fewer than 2 qualify. Only available dishes in active categories, so nothing sold out. No schema change. The "most ordered" query overlaps with PB22's popular-items report, and this could later grow into PB28 (personal recommendations). |
| I3 | **Admin "View as" switcher.** An admin can switch the screen between **Customer view**, **Staff view** and **Admin view**, to see exactly what each role sees, spot issues, and test directly without logging out and in with other accounts. | Carynn (roles / admin, PB04) — idea from Jordan | Needs BUG1's role-aware navbar first. Simplest safe version: a session flag that only changes what the UI shows (navbar, menus, buttons), while `[Authorize]` keeps checking the admin's real role. Showing a pretend role on top of the real one, instead of really changing roles, avoids a security hole. To decide: can an admin *act* in customer view (place orders under their own account) or only look? Show a clear "Viewing as Staff — back to Admin" banner. Touches the shared `_Layout.cshtml`, so group agreement needed. Good Ch.7 / usability talking point. |
| I4 | **Home page background photo.** Put a photo of the restaurant or a dining table behind the home page hero, so the page looks less plain. | Jordan (home page, layout shell) | Do later. Keep the text readable (dark overlay), use a compressed image (WebP, ~200 KB) in `wwwroot/images/`, and use a royalty-free or own photo, credited in the report. Home page only, so `site.css` / `_Layout.cshtml` probably don't need to change. |
| I5 | **Visual floor plan for dining areas and tables.** **Customer (booking):** after pressing **Check tables**, show a floor plan of the dining area(s) so the customer can pick exactly where they want to sit. Only tables that are free for that slot **and** fit the party size (same rule as I2) can be selected; the rest are greyed out. **Admin/Staff:** the same floor plan, where staff can see which table is which, and the admin can place tables and set each one's **name/number and seats**. | Wallace (customer booking, PB09/PB10) + Carynn (dining areas/tables, PB05) — idea from Jordan | `RestaurantTable` already has `TableNumber`, `Capacity` and `DiningAreaId`, but **no position**. A drawn layout needs `PositionX`/`PositionY` (and maybe shape) columns, i.e. a **schema change → group agreement + migration**. Cheaper version with no schema change: one grid of table "tiles" per dining area, in table-number order. Keep the availability rule in `ReservationService` (a crafted POST picking a full or too-small table must be refused). Must work on a phone: tap to select, not drag. Strong Ch.5 UI screenshot. |
| I6 | **Admin-configurable tax and service charge.** Admin (and maybe staff) can set how much tax/charge customers pay, e.g. SST % and service charge %, shown as separate lines at checkout, on the Pay page and on the receipt. | Carynn (settings page, PB07 `RestaurantSettings`) + Jordan (applying it to orders/payments) | Store the rates as restaurant settings (Carynn's service, no schema change for the settings). Apply them **at checkout (PB14)**, so the order total, payment amount and receipt always agree (same reasoning as spec Q3 for discounts). The rate in use must be **saved on the order** so changing the rate later doesn't change old orders and receipts. That means new `Order` columns (e.g. `Subtotal`, `TaxAmount`, `ServiceCharge`) → **schema change + migration, group agreement**. Decide who may change it (admin only is safer). |
| I2 | **Smallest-table-first booking (PB09/PB10 extension).** A party of 1 is only offered 2-seat tables, so bigger tables stay free for bigger groups and seats are not wasted. | Wallace (idea from Jordan) | Rule: offer the smallest free table that fits the party (1–2 people → 2-seaters). Larger tables only when no smaller one is free for that slot, or not at all for solo diners (to agree with Wallace). Lives in `ReservationService`'s availability check; no schema change. **Wallace's domain**, so raise it with him before anyone builds it. |

## Log
- **2026-11-01** — Jordan: back office refinements (same branch). **Dashboard:** full width, filter refreshes on
  change, tables replaced by "View all" links (sales → Monitoring payments/Paid, bookings → Monitoring
  reservations, dishes → new **Dish sales** page `/Admin/Dashboard/Dishes`: every dish incl. 0 sales, category,
  share of sales, unavailable badge), "Bookings by status" removed. **Monitoring + Customer activity range
  62 → 366 days (Carynn's files, needs her OK):** the cap existed because reservations were loaded one day at a
  time; her own comment said to switch once a range method existed, and Wallace's `GetReservationsAsync(from,to)`
  now does, so it is one query. Fixed in the back office: links were Bootstrap blue (Bootstrap 5.3 reads
  `--bs-link-color-rgb`, `site.css` only sets `--bs-link-color` → **same bug on the customer site, fix in
  `site.css` needs group OK**), invisible ☰ icon on phones, long dish names clipped on the phone chart.
  Build clean, 0 warnings. Checked over HTTP and with headless Edge screenshots (desktop + 390 px).
- **2026-11-01** — Jordan: **back office v1** on `feature/UI-back-office` (spec:
  `docs/superpowers/specs/2026-11-01-back-office-design.md`, look "Peranakan Tile"). New `_BackOfficeLayout` +
  `_BackOfficeNav` applied to `Pages/Admin` and `Pages/Staff` by folder `_ViewStart`s: grouped side menu filtered
  by role (staff see Operations only), top bar, slide-in menu below `lg`. `wwwroot/css/backoffice.css` scoped to
  `body.bo` (no Bootstrap blue, tinted status pills). **BUG1 fixed:** staff/admin navbar shows Home, Menu,
  "Back office" (supersedes `fix/UX4-configurations-label`, which can be deleted). Login lands admins on
  `/Admin/Dashboard`, staff on `/Staff/Orders` when no `ReturnUrl`. Page contents unchanged. **Shared/other
  domains → group OK in the PR:** `_Layout.cshtml`, `Login.cshtml.cs` (Carynn), the frame of Carynn's and
  Wallace's pages. Build clean, 0 warnings. Smoke-tested per role over HTTP: all 17 admin/staff pages 200 with
  the right active item; staff → `/Admin` denied; customer navbar unchanged. Browser look + phone width still to check.
- **2026-11-01** — Jordan: **Sprint 4 status review** (checked against code on `dev`, not the backlog column).
  **Built and merged:** PB01–PB19, PB21–PB23. Carynn: PB01–05, PB07, PB21, PB23. Wallace: PB09–13, PB17, PB19.
  Jordan: PB06, PB08, PB14–16, PB18, PB22; BUG6 + UX1 merged as PR #54. Also fixed since logging: BUG2, BUG3
  (Carynn 29/10), UX2 (Menu card, Carynn 23/10); BUG7 mostly covered by PB21's orders/payments tabs.
  **Still open:** (1) **PB20** — reservations notify (Wallace); orders and payments notify on this branch, not merged;
  **promotions send nothing yet** (Carynn's promotions code). (2) **BUG1** — Staff/Admin still see "Book a table" and
  "My orders"; staff have **no navbar link to `/Staff/Orders`**. (3) **UX3** booking form (Wallace; only the party-size
  cap of 8 is done). (4) **UX4 regressed:** the PR #53 merge resolution put the navbar link back to "Admin"; one-line
  fix committed on `fix/UX4-configurations-label` (`ad2a021`), **not pushed yet**. (5) `docs/product-backlog.md`
  statuses were stale; updated in the same commit as this entry. (6) No end-to-end run of merged `dev` per role yet.
  **Proposed split for the rest of Sprint 4** (to agree with the group; extras time-boxed, done by **2 Nov**):
  Jordan — BUG1 role-aware navbar, then **UI refining, starting with an admin-side redesign**; Wallace — UX3, then
  PB26 email confirmations; Carynn — PB20 promotion notifications, then PB24 export (moved from C: fits her
  report pages). Skip PB25, PB27, PB28 (too big before the 3 Nov freeze).
- **2026-10-31** — Jordan: on `feature/PB20-order-payment-notifications`. Orders and payments now call Wallace's
  `INotificationService.SendAsync` after the save succeeds (type `Order`: place, advance, cancel; type `Payment`: online
  paid/declined, cash, refund), always to the order's `CustomerId`. Refused actions send nothing. No business rule, page
  or schema change; `Program.cs` already registers the service. `.gitignore` now ignores `wwwroot/uploads/`. Build clean,
  0 warnings. Smoke-tested all six calls against a throwaway DB; manual run-through on `/Notifications` still to do.
- **2026-10-30** — Jordan: on `fix/BUG6-UX1-orders-nav-cart`. **BUG6 fixed:** receipt's customer button is now
  "Back to my orders" (`/Orders`); Confirmation gains a "My orders" button. **UX1 fixed (menu page only):** Add opens
  a dialog with a 1–20 quantity stepper and total; confirming posts in the background (`wwwroot/js/menu-cart.js`,
  `Cart.OnPostAddAsync` returns JSON when `X-Requested-With: fetch`), so no reload, a floating toast, and the cart bar
  count updates. Works without JS as before. No shared files, no schema change. Build clean, 0 warnings.
  Smoke-tested: JSON add, quantity clamp, no-JS redirect, missing token → 400. Browser click-through still to do.
- **2026-10-30** — Jordan: PB22 v1 on `feature/PB22-dashboard` (spec: `docs/superpowers/specs/2026-10-30-dashboard-design.md`).
  `/Admin/Dashboard`: quick ranges + from/to + group by Day/Week/Month, tiles (revenue, average order, orders,
  bookings + guests), Chart.js bar charts for sales, bookings and top 5 dishes, a table under each chart.
  `IReportService`/`ReportService`: revenue = Paid payments by payment date (refunds drop out), cancelled orders and
  bookings excluded, reservations read through Wallace's `IReservationService`, max range 366 days. No schema change.
  **Chart.js 4.4.1** added to `wwwroot/lib/chart.js` (Jordan OK'd). **`Program.cs`**: 1 `AddScoped` line. **Dashboard
  card added to Carynn's Configurations hub (`Pages/Admin/Index`) → Carynn to OK in the PR.** Build clean, 0 warnings.
  Smoke-tested against the dev DB (tiles, day/week grouping, bad-range messages, staff → Access denied); manual
  run-through and phone-width check still to do.
- **2026-10-30** — Jordan: PB18 v1 on `feature/PB18-history`. `/Orders` "My orders": in-progress and past orders
  (status + payment badges, receipt / pay links) and the latest 5 reservations through Wallace's `IReservationService`.
  In-progress badges refresh every 15 s (`wwwroot/js/my-orders.js` polls `?handler=Status`). Added the 3 files that
  were missing from the first push (`CustomerOrderRowViewModel`, `_CustomerOrderRow`, `my-orders.js`). Build clean,
  0 warnings. No schema change. **`_Layout.cshtml` "My orders" link enabled → needs group OK**; Staff/Admin still see it (BUG1).
  Logged **BUG6** (receipt → no way back to My orders), **BUG7** (admin can't see orders from the UI), **BUG8** (staff
  have no overview) and decision **D5** (one dashboard per role: what staff and admin can and can't see). Next after push.
- **2026-10-29** — Jordan: Sprint 4 started. `feature/admin-list-methods` (unblocks Carynn's PB21/PB23): read-only
  `IPaymentService.GetPaymentsAsync(from, to, status?)` + `PaymentListItemViewModel`, and
  `IOrderService.GetOrdersAsync(from, to, status?)` reusing the staff projection (no staff actions set).
  `CustomerId` added to `StaffOrderViewModel` for grouping by customer. Both date ends inclusive, newest first,
  `AsNoTracking`. No schema change, no pages, no `Program.cs` edit. Build clean, 0 warnings. Smoke-tested
  read-only against the dev DB. Checked: `INotificationService` does not exist yet (Wallace, PB20);
  `IReservationService` has no date-range method (needed for PB22 reservations chart, ask Wallace).
- **2026-10-28** — Jordan: on `fix/BUG4-BUG5-home-hours-palette`. **BUG4 fixed:** home page reads the 7 days through
  Carynn's `IOperatingHoursService` (Monday first, `hh:mm tt`, "Closed" when `IsClosed`). **BUG5 fixed on my pages (fallback):**
  Checkout / Confirmation / Pay headers now neutral, Card/E-wallet tiles use the palette (`Pay.cshtml.css`). No shared files,
  no schema change. Build clean, 0 warnings. `Admin/Users` and `Reservations/Index` still use `bg-primary` (Carynn/Wallace).
- **2026-10-28** — Jordan: PB14, PB15, PB16 manually tested end to end (order → staff status → pay → receipt;
  cancel shows only for unpaid Pending/Preparing orders). Marked **Done**; merged to `dev` as PR #15, with Carynn's and Wallace's tidy-ups (#12, #13) and PB12-13 (#14).
  Staff still reach `/Staff/Orders` by URL only (BUG1, needs `_Layout.cshtml` group OK).
- **2026-10-22** — Jordan: logged **BUG5** (Bootstrap default blue on Checkout and Pay pages, off-palette) and
  idea **I6** (admin-configurable tax / service charge on orders, payments and receipts).
- **2026-10-22** — Jordan: logged idea **I5**: a visual floor plan to pick a table after "Check tables"
  (only free tables that fit the party can be picked), plus an admin/staff floor plan to name tables and set seats.
  Wallace + Carynn; a true layout needs a schema change.
- **2026-10-22** — Jordan: logged **BUG4** (home page opening hours are hard-coded placeholders, so they're wrong
  and never show "Closed") and **UX4** (rename the "Admin" tab to "Configurations").
- **2026-10-22** — Jordan: logged **BUG2** (no phone number at registration, so staff can't call about bookings),
  **BUG3** (navbar says "Hello <email>" instead of the name), both Carynn's domain; and idea **I4** (home page
  background photo, later).
- **2026-10-22** — Jordan: logged idea **I3**: an admin "View as" switcher (Customer / Staff / Admin) for
  checking and testing each role's screens. Carynn's domain; depends on BUG1.
- **2026-10-22** — Jordan: logged bug **BUG1**. Staff see the customer navbar links ("Book a table",
  "My orders") instead of "Reservations" and "Orders". Fixing it needs a `_Layout.cshtml` edit, so group agreement first.
- **2026-10-22** — Jordan: logged **UX3** (booking form: 1-week booking limit, Grab-style date picker,
  hours/minutes that don't scroll endlessly, 10-min steps, party size dropdown). Wallace's domain; to raise with him.
- **2026-10-22** — Jordan: logged idea **I2** (solo diners only offered 2-seat tables, to save bigger
  tables for groups). Wallace's domain; to raise with him.
- **2026-10-22** — Jordan: logged idea **I1** (hot picks row on the menu, 2–4 most-ordered dishes from
  recent orders). Not scheduled yet.
- **2026-10-22** — Jordan: logged **UX1** (add-to-cart reloads to the top, no floating notification, no
  confirmation). Not fixed yet.
- **2026-10-22** — Jordan: PB16 v1 on `feature/PB16-payment` (branched off `feature/PB14-PB15-ordering`,
  spec: `docs/superpowers/specs/2026-10-22-payment-design.md`, defaults Q1–Q4 accepted). `IPaymentGateway` +
  `SimulatedPaymentGateway`, `IPaymentService`/`PaymentService` (online pay with Luhn + expiry checks, every
  attempt saved, order row locked so it cannot be paid twice, receipt, cash/bill/unpaid/refund methods for
  Wallace's PB17). `/Payments/Pay/{orderId}`, printable `/Payments/Receipt/{paymentId}`, payment badges on
  Confirmation and the staff board, paid orders cannot be cancelled. No schema change. 2 `AddScoped` lines in
  `Program.cs` (group agreed 22/10). Build clean, no new warnings. Smoke-tested TC-P01–P10, P13 against a
  throwaway DB; manual run-through still to do. **Wallace:** `IPaymentService` is ready to code PB17 against.
- **2026-10-22** — Jordan: Sprint 2 tested on `dev` (all PRs #8–#11 merged). **Issue found (not fixed yet):**
  admin cannot reach add/edit/delete for dishes from the UI — the Admin hub (`/Admin/Index`, Carynn)
  has no Menu card, so `/Admin/Menu` is only reachable by typing the URL. Fix: add a Menu card to the hub
  (needs Carynn's OK). Sprint 3 work (PB14, PB15) moved to `feature/PB14-PB15-ordering`.
- **2026-10-20** — Jordan: PB06 + PB08 v1 on `feature/PB06-PB08-menu` (spec:
  `docs/superpowers/specs/2026-10-18-menu-design.md`). `IMenuService`/`MenuService`,
  `IImageStorageService` (JPG/PNG/WebP ≤ 2 MB, GUID names, `wwwroot/uploads/menu/`), public
  phone-first `/Menu` (sticky category tabs, search, sold-out greyed), `/Admin/Menu` CRUD for items
  and categories with availability toggle; delete blocked when an item has orders / a category has
  items. No schema change. Build clean, no new warnings. Group agreed the 2 `AddScoped` lines in
  `Program.cs` (22/10). TC-M01–M10 passed 21/10. Still pending: Menu nav link in `_Layout.cshtml`.
- **2026-10-11** — Jordan: layout shell on `feature/layout-shell` — modern minimal navbar, footer,
  design tokens in `site.css`, `_Banners` partial for TempData messages, placeholder home page.
  Restaurant name is a placeholder (`Rumah Rasa`), set in one `const` in `_Layout.cshtml`.
  `_LoginPartial` slot left commented out for Carynn's Identity work. `.claude/rules/*.md` brought
  in line with `architecture.md` and `coding-conventions.md`. Build clean, 0 warnings.
- **2026-10-10** — Lab survey done: conventions recorded in `coding-conventions.md`. Labs use one
  project, `ApplicationDbContext`, `<Entity>Id` keys, PageModels querying the DbContext directly
  (we add a Services layer instead). UI direction set: modern minimal. Repo initialised locally.
- **2026-10-10** — Project scaffolded in Visual Studio 2026 (Razor Pages, .NET 10, no auth) at
  `C:\SoftwareEngineering\FoodOrderingSystem`. Project docs and Claude Code setup added.
  Not yet pushed to GitHub. Chapter 3 (project management) draft done.
- **2026-10-07** — Domains assigned: A Carynn, B Wallace, C Jordan.
- **Late Sep** — Product backlog (PB01–PB28) agreed and mapped to the brief's requirements.
