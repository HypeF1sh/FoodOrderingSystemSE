# Progress

Keep this file short. Newest log entries on top. Claude reads it at the start of every session.

## Current position
- **Today's sprint:** Sprint 2 (15–22 Oct) — menu browsing + table booking
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

## Decided
- One project with layered folders (Pages / ViewModels / Services / Models / Data / Migrations), matching the labs.
- Scrum Master: **Jordan**.
- Auth: ASP.NET Core Identity (pending Carynn's confirmation — see D1).
- UI direction: modern minimal.

## Known issues (logged, not fixed yet)
| # | Issue | Owner | Notes |
|---|---|---|---|
| UX1 | **Add to cart is not friendly (PB14, `/Menu`).** (a) Every "Add" reloads the page and jumps back to the top of the menu, so the customer loses their place. (b) The "added" message is a banner at the top, not a floating notification. (c) There is no confirmation step, so one mis-tap adds a dish. | Jordan | Wanted: stay at the same scroll position (no full reload), a floating toast notification, and a quick confirm before adding (e.g. a small dialog with the quantity). Found 22/10 while testing the cart. A floating toast for every page would mean changing the shared `_Banners` / `_Layout.cshtml`, which needs group agreement; a toast on the menu page only does not. |
| UX3 | **Table booking form is confusing (PB09/PB10, `/Reservations`).** (a) **Date:** customers can pick any date. Limit booking to **1 week ahead**, and refine the date picker's look using Grab Malaysia's date picker as the reference. (b) **Time, hours:** the hour picker scrolls endlessly (wraps around). It should stop at the end of the day, ideally showing only opening hours. (c) **Time, minutes:** too fine-grained and also scrolls endlessly. Use **10-minute steps** with no wrap-around. (d) **Party size:** replace the up/down number spinner with a **dropdown**, with the maximum set by the largest table the restaurant has. | Wallace (raised by Jordan) | Still to agree: does "1 week ahead" mean 7 days from today or to the end of next week? The 7-day limit must also be checked in `ReservationService`, not only in the UI. The dropdown maximum should come from the table data (Carynn's `RestaurantTable` via her service), not a hard-coded number. **Wallace's domain:** raise it with him. |
| BUG1 | **Navbar is not role-aware.** A Staff account sees the customer links **"Book a table"** and **"My orders"**. Staff should see **"Reservations"** (the staff reservation list) and **"Orders"** (the staff order board, `/Staff/Orders`). Customers keep "Book a table" and "My orders". | Jordan (layout) + Wallace (staff reservations page) | Cause: `_Layout.cshtml` lines 66–72 show both links to every user, and the `User.IsInRole("Staff")` block (line 86) is still a comment. Fix needs an edit to the **shared `_Layout.cshtml` → group agreement**. The staff "Reservations" link needs Wallace's PB12 page, which doesn't exist yet, so link it once his page is merged. "My orders" is still a placeholder until PB18. Also decide what Administrators see. |
| BUG2 | **Register has no phone number.** Customers sign up without a phone number, so staff cannot call them to verify a booking or reach them about urgent changes. | Carynn (PB01) | Add a required Malaysian mobile field to `Register` (and to Manage profile, PB02). **No schema change:** `ApplicationUser` inherits `PhoneNumber` from `IdentityUser`. Existing accounts have none, so decide whether to prompt them for it at booking time. Wallace's staff reservation views (PB12/PB13) should then show the number. |
| BUG3 | **Navbar greeting shows the email**: top right reads "Hello customer@rroms.com!" instead of the person's name. | Carynn (Identity, `_LoginPartial`) | Cause: `_LoginPartial.cshtml` line 9 uses `User.Identity?.Name`, which in Identity is the username (= email). Should show `ApplicationUser.FullName`, falling back to the email if it is empty. |
| BUG4 | **Home page opening hours are wrong**: they don't match the times the admin set, and a day marked **Closed** is not shown as closed. | Jordan (home page) — reads Carynn's data | Cause: `Pages/Index.cshtml` lines 28–46 are still the **hard-coded placeholder** from the layout shell ("Mon–Thu 11:30 am – 10:00 pm…"), never wired to the `OperatingHours` table. Fix: `IndexModel` reads the 7 days through Carynn's `IOperatingHoursService` (no direct table access). Show each day's open–close in `hh:mm tt`, and **"Closed"** when `IsClosed` is true. Optionally group identical days ("Mon – Thu"). No schema change. |
| UX4 | **Rename the "Admin" navbar tab to "Configurations"** so it's clear that's where everything editable lives (menu, tables, hours, promotions, settings, users). | Carynn (admin hub) | Rename the link text in `_Layout.cshtml` line 79 (**shared file → group agreement**). Also rename the hub page's title and heading (`Pages/Admin/Index.cshtml` lines 4 and 8) to match. The `/Admin` URL and folder can stay as they are. Pairs with UX2 (adding a Menu card to the same hub). |
| BUG5 | **Bootstrap blue breaks the colour palette.** The Checkout "Your order" header is bright blue, and after **Pay now** the Pay page header, Card/E-wallet tiles and buttons are blue too. None of it matches the site palette (ink `#0F2E2B` + burgundy accent `#A3334A`). | Jordan (Checkout, Confirmation, Pay pages) + group (theme) | Cause: `site.css` maps links to `--rr-accent` but **never re-themes Bootstrap's "primary"**, so `bg-primary`, `btn-primary` and `btn-outline-primary` stay Bootstrap's default blue `#0d6efd`. The green `bg-success` headers (Confirmation) clash too. Same issue on `Admin/Users`, `Reservations/Index`. **Best fix:** map Bootstrap's primary to `--rr-accent` once in `site.css` (**shared file → group agreement**), which fixes every page. **Fallback:** on my own pages, swap the coloured headers for the neutral card style. |
| UX2 | Admin cannot reach `/Admin/Menu` from the UI: no Menu card on the Admin hub | Jordan / Carynn | See log 22/10. Needs Carynn's OK |

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
- **2026-10-29** — Jordan: Sprint 4 started. `feature/admin-list-methods` (unblocks Carynn's PB21/PB23): read-only
  `IPaymentService.GetPaymentsAsync(from, to, status?)` + `PaymentListItemViewModel`, and
  `IOrderService.GetOrdersAsync(from, to, status?)` reusing the staff projection (no staff actions set).
  `CustomerId` added to `StaffOrderViewModel` for grouping by customer. Both date ends inclusive, newest first,
  `AsNoTracking`. No schema change, no pages, no `Program.cs` edit. Build clean, 0 warnings. Smoke-tested
  read-only against the dev DB. Checked: `INotificationService` does not exist yet (Wallace, PB20);
  `IReservationService` has no date-range method (needed for PB22 reservations chart, ask Wallace).
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
