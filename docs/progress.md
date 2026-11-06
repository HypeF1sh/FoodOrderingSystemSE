# Progress

Keep this file short. Newest log entries on top. Claude reads it at the start of every session.

## Current position
- **Today's sprint:** Sprint 4 (29 Oct–3 Nov) — closing gaps (PB20 promotions, BUG1, UX3), then extras and UI refining
- **Internal target:** 4 Nov · **Formal deadline:** Fri 6 Nov 2026
- **Jordan, 3 Nov:** #57 (staff floor) and #58 (pre-orders) merged. **Table QR ordering** built on
  `feature/PB14-table-qr` (off `dev`, not pushed): migration + QRCoder package, announce in the group first.
  Read **`docs/handoff-jordan.md`** first when continuing on another PC.

| Sprint | Dates | Goal |
|---|---|---|
| 1 | 8–15 Oct | All three roles can log in; layout and DB schema in place |
| 2 | 15–22 Oct | Customer can browse menu and book a table end to end |
| 3 | 22–29 Oct | Full order → status → payment flow |
| 4 | 29 Oct–3 Nov | History, feedback, notifications, analytics dashboard |
| Freeze | ~~3–5 Nov~~ postponed (3 Nov) | Testing, fixes, report finalisation. Work is not finished, so Sprint 5 continues; freeze date to be agreed |

## Decisions (status checked 5/11)
| # | Decision | Owner | Blocks |
|---|---|---|---|
| D1 | **(Done: Identity is in use with roles Administrator / Staff / Customer.)** **ASP.NET Core Identity** chosen by Jordan — Carynn to confirm, then scaffold it (project was created with no auth) | Carynn | PB01–PB04, every `[Authorize]` page |
| D3 | **(Done: LocalDB `DefaultConnection` per developer.)** Dev database: SQL Server LocalDB per developer, `DefaultConnection` (proposed, as in labs) | Group | First migration |
| D4 | **(Done: receipt page `/Payments/Receipt`, booking confirmation on PB10.)** Fold receipt/confirmation (Intermediate #3) into PB10 and PB16 | Group | PB10, PB16 scope |
| D5 | **(Done 1–4 Nov: staff land on Tonight with no money totals; admins on the Dashboard; staff get their own top bar.)** **One dashboard per role** (raised by Jordan 30/10). Today Staff and Admin land on the customer home page and reach their pages by URL. Questions: (1) **Staff home** `/Staff`: today's orders board, today's reservations, unpaid bills, today's order count and takings? (2) Should **staff** see money totals at all (today's takings only) or is **revenue/sales reporting admin-only** (PB22)? (3) **Profit** cannot be shown: there is no cost price on `MenuItem` — revenue only, unless the group agrees a schema change. (4) **Admin home**: PB22 dashboard (sales, reservations, popular dishes, date filter) + PB21 monitor (all reservations, orders, payments, read-only) + link to Configurations. (5) What admin must **not** do: card details are never stored, so nothing to hide there; should admin be able to change order/payment status, or only staff (audit trail)? (6) Can staff see customer phone/email (BUG2) — yes for bookings? | Group (Jordan: PB22 + order data; Carynn: PB21 + admin hub; Wallace: staff reservations) | BUG7, BUG8, PB21, PB22, BUG1 |

## Decided
- One project with layered folders (Pages / ViewModels / Services / Models / Data / Migrations), matching the labs.
- Scrum Master: **Jordan**.
- Auth: ASP.NET Core Identity (confirmed, in use).
- UI direction: modern minimal.

## Known issues (status checked against the code 5/11)
| # | Issue | Owner | Notes |
|---|---|---|---|
| UX1 | **(Fixed 30/10, `fix/BUG6-UX1-orders-nav-cart`.)** **Add to cart is not friendly (PB14, `/Menu`).** (a) Every "Add" reloads the page and jumps back to the top of the menu, so the customer loses their place. (b) The "added" message is a banner at the top, not a floating notification. (c) There is no confirmation step, so one mis-tap adds a dish. | Jordan | Wanted: stay at the same scroll position (no full reload), a floating toast notification, and a quick confirm before adding (e.g. a small dialog with the quantity). Found 22/10 while testing the cart. A floating toast for every page would mean changing the shared `_Banners` / `_Layout.cshtml`, which needs group agreement; a toast on the menu page only does not. |
| UX3 | **(Mostly fixed 4/11, booking picker: day strip, 30-min slots inside opening hours, guest chips 1–8. Still open: limit is 14-day strip + 60 days via "Another date", not 1 week; max party is the constant 8, not the largest table.)** **Table booking form is confusing (PB09/PB10, `/Reservations`).** (a) **Date:** customers can pick any date. Limit booking to **1 week ahead**, and refine the date picker's look using Grab Malaysia's date picker as the reference. (b) **Time, hours:** the hour picker scrolls endlessly (wraps around). It should stop at the end of the day, ideally showing only opening hours. (c) **Time, minutes:** too fine-grained and also scrolls endlessly. Use **10-minute steps** with no wrap-around. (d) **Party size:** replace the up/down number spinner with a **dropdown**, with the maximum set by the largest table the restaurant has. | Wallace (raised by Jordan) | Still to agree: does "1 week ahead" mean 7 days from today or to the end of next week? The 7-day limit must also be checked in `ReservationService`, not only in the UI. The dropdown maximum should come from the table data (Carynn's `RestaurantTable` via her service), not a hard-coded number. **Wallace's domain:** raise it with him. |
| BUG1 | **(Fixed: customer links hidden for staff/admin; one Back office link; staff now have their own top bar.)** **Navbar is not role-aware.** A Staff account sees the customer links **"Book a table"** and **"My orders"**. Staff should see **"Reservations"** (the staff reservation list) and **"Orders"** (the staff order board, `/Staff/Orders`). Customers keep "Book a table" and "My orders". | Jordan (layout) + Wallace (staff reservations page) | Cause: `_Layout.cshtml` lines 66–72 show both links to every user, and the `User.IsInRole("Staff")` block (line 86) is still a comment. Fix needs an edit to the **shared `_Layout.cshtml` → group agreement**. The staff "Reservations" link needs Wallace's PB12 page, which doesn't exist yet, so link it once his page is merged. "My orders" is still a placeholder until PB18. Also decide what Administrators see. |
| BUG2 | **(Fixed: phone number on Register and Manage profile.)** **Register has no phone number.** Customers sign up without a phone number, so staff cannot call them to verify a booking or reach them about urgent changes. | Carynn (PB01) | Add a required Malaysian mobile field to `Register` (and to Manage profile, PB02). **No schema change:** `ApplicationUser` inherits `PhoneNumber` from `IdentityUser`. Existing accounts have none, so decide whether to prompt them for it at booking time. Wallace's staff reservation views (PB12/PB13) should then show the number. |
| BUG3 | **(Fixed: avatar menu shows FullName and initials.)** **Navbar greeting shows the email**: top right reads "Hello customer@rroms.com!" instead of the person's name. | Carynn (Identity, `_LoginPartial`) | Cause: `_LoginPartial.cshtml` line 9 uses `User.Identity?.Name`, which in Identity is the username (= email). Should show `ApplicationUser.FullName`, falling back to the email if it is empty. |
| UX4 | **(Fixed: hub page is "Configurations"; the navbar Admin link was replaced by Back office.)** **Rename the "Admin" navbar tab to "Configurations"** so it's clear that's where everything editable lives (menu, tables, hours, promotions, settings, users). | Carynn (admin hub) | Rename the link text in `_Layout.cshtml` line 79 (**shared file → group agreement**). Also rename the hub page's title and heading (`Pages/Admin/Index.cshtml` lines 4 and 8) to match. The `/Admin` URL and folder can stay as they are. Pairs with UX2 (adding a Menu card to the same hub). |
| BUG5 | **(Fixed in the back office (backoffice.css re-themes primary/info). Still blue on customer pages `Reservations/Index`, `Reservations/Edit`, `Notifications` — site.css does not set `--bs-primary`.)** **(Fixed on Jordan's pages 28/10; still open on `Admin/Users`, `Reservations/Index`.)** **Bootstrap blue breaks the colour palette.** The Checkout "Your order" header is bright blue, and after **Pay now** the Pay page header, Card/E-wallet tiles and buttons are blue too. None of it matches the site palette (ink `#0F2E2B` + burgundy accent `#A3334A`). | Jordan (Checkout, Confirmation, Pay pages) + group (theme) | Cause: `site.css` maps links to `--rr-accent` but **never re-themes Bootstrap's "primary"**, so `bg-primary`, `btn-primary` and `btn-outline-primary` stay Bootstrap's default blue `#0d6efd`. The green `bg-success` headers (Confirmation) clash too. Same issue on `Admin/Users`, `Reservations/Index`. **Best fix:** map Bootstrap's primary to `--rr-accent` once in `site.css` (**shared file → group agreement**), which fixes every page. **Fallback:** on my own pages, swap the coloured headers for the neutral card style. |
| UX2 | **(Fixed: "Manage menu" card on Configurations.)** Admin cannot reach `/Admin/Menu` from the UI: no Menu card on the Admin hub | Jordan / Carynn | See log 22/10. Needs Carynn's OK |
| BUG6 | **(Fixed 30/10, `fix/BUG6-UX1-orders-nav-cart`.)** **Receipt has no way back to order history (PB18).** Customer: My orders → View receipt → "Back to my order" (Confirmation) → only "Back to menu". Misleading: the customer came from **My orders** and can't get back except via the navbar. | Jordan | `Payments/Receipt.cshtml` line 87 links to Confirmation; `Orders/Confirmation.cshtml` line 53 only offers "Back to menu". Fix: add a "Back to my orders" link (`/Orders`) on both, or pass a `returnTo` so the button returns where the customer came from. My pages only, no shared file. |
| BUG7 | **(Fixed: PB21 Monitoring has Orders and Payments tabs.)** **Admin has no order view in the UI.** The `/Staff` folder allows `StaffOrAdmin`, so an admin *can* open `/Staff/Orders` by typing the URL, but there is no link and no admin-specific read-only list. | Jordan + Carynn (PB21) | `GetOrdersAsync` / `GetPaymentsAsync` for admin monitoring are on `feature/admin-list-methods` (not merged yet). PB21 admin monitor page is Carynn's. Depends on D5 and BUG1. |
| BUG8 | **(Fixed 01/11, `feature/UI-staff-floor`: Floor + Today; no money for staff, per D5.)** **Staff have no overview.** Staff can only see one day's order board; no totals (orders today, takings today, unpaid count) and no landing page of their own. Staff UI reuses the customer navbar/home. | Jordan (order/payment numbers) + Wallace (staff reservations) + group (staff home) | Needs D5 decided first: what staff may see (money or not). Profit is not possible without a cost price (schema change). Staff home would be a new `Pages/Staff/Index`; navbar link needs `_Layout.cshtml` (group OK). |

## Ideas (not scheduled yet)
| # | Idea | Owner | Notes |
|---|---|---|---|
| I1 | **(Not started; overlaps PB28 (Carynn), also not started.)** **"Hot picks" row on `/Menu` (PB08 extension).** A row or category at the top of the menu showing the dishes customers have ordered most recently, so a new customer can spot the signature dishes. | Jordan | Data-driven: count `OrderItems` over a recent window (e.g. last 30 days), excluding Cancelled orders. Show **min 2, max 4** dishes; hide the row if fewer than 2 qualify. Only available dishes in active categories, so nothing sold out. No schema change. The "most ordered" query overlaps with PB22's popular-items report, and this could later grow into PB28 (personal recommendations). |
| I3 | **(Not started.)** **Admin "View as" switcher.** An admin can switch the screen between **Customer view**, **Staff view** and **Admin view**, to see exactly what each role sees, spot issues, and test directly without logging out and in with other accounts. | Carynn (roles / admin, PB04) — idea from Jordan | Needs BUG1's role-aware navbar first. Simplest safe version: a session flag that only changes what the UI shows (navbar, menus, buttons), while `[Authorize]` keeps checking the admin's real role. Showing a pretend role on top of the real one, instead of really changing roles, avoids a security hole. To decide: can an admin *act* in customer view (place orders under their own account) or only look? Show a clear "Viewing as Staff — back to Admin" banner. Touches the shared `_Layout.cshtml`, so group agreement needed. Good Ch.7 / usability talking point. |
| I4 | **(Now part of the landing page redesign, 5/11.)** **Home page background photo.** Put a photo of the restaurant or a dining table behind the home page hero, so the page looks less plain. | Jordan (home page, layout shell) | Do later. Keep the text readable (dark overlay), use a compressed image (WebP, ~200 KB) in `wwwroot/images/`, and use a royalty-free or own photo, credited in the report. Home page only, so `site.css` / `_Layout.cshtml` probably don't need to change. |
| I5 | **(Partly: staff Tables map 4/11; no customer floor plan.)** **Visual floor plan for dining areas and tables.** **Customer (booking):** after pressing **Check tables**, show a floor plan of the dining area(s) so the customer can pick exactly where they want to sit. Only tables that are free for that slot **and** fit the party size (same rule as I2) can be selected; the rest are greyed out. **Admin/Staff:** the same floor plan, where staff can see which table is which, and the admin can place tables and set each one's **name/number and seats**. | Wallace (customer booking, PB09/PB10) + Carynn (dining areas/tables, PB05) — idea from Jordan | `RestaurantTable` already has `TableNumber`, `Capacity` and `DiningAreaId`, but **no position**. A drawn layout needs `PositionX`/`PositionY` (and maybe shape) columns, i.e. a **schema change → group agreement + migration**. Cheaper version with no schema change: one grid of table "tiles" per dining area, in table-number order. Keep the availability rule in `ReservationService` (a crafted POST picking a full or too-small table must be refused). Must work on a phone: tap to select, not drag. Strong Ch.5 UI screenshot. |
| I6 | **(Not started.)** **Admin-configurable tax and service charge.** Admin (and maybe staff) can set how much tax/charge customers pay, e.g. SST % and service charge %, shown as separate lines at checkout, on the Pay page and on the receipt. | Carynn (settings page, PB07 `RestaurantSettings`) + Jordan (applying it to orders/payments) | Store the rates as restaurant settings (Carynn's service, no schema change for the settings). Apply them **at checkout (PB14)**, so the order total, payment amount and receipt always agree (same reasoning as spec Q3 for discounts). The rate in use must be **saved on the order** so changing the rate later doesn't change old orders and receipts. That means new `Order` columns (e.g. `Subtotal`, `TaxAmount`, `ServiceCharge`) → **schema change + migration, group agreement**. Decide who may change it (admin only is safer). |
| I2 | **(Partly: available tables are listed smallest first; bigger tables are still offered to small parties.)** **Smallest-table-first booking (PB09/PB10 extension).** A party of 1 is only offered 2-seat tables, so bigger tables stay free for bigger groups and seats are not wasted. | Wallace (idea from Jordan) | Rule: offer the smallest free table that fits the party (1–2 people → 2-seaters). Larger tables only when no smaller one is free for that slot, or not at all for solo diners (to agree with Wallace). Lives in `ReservationService`'s availability check; no schema change. **Wallace's domain**, so raise it with him before anyone builds it. |

## Log
- **2026-11-06** — Jordan: **menu photos** on `feature/menu-photos` (off the `dev` hotfix #70). 15 dish photos
  supplied by Jordan, resized to 900 px (911 KB total, from 3 MB), committed in `wwwroot/images/menu/` so every PC
  has them. `DbSeeder` step 6 links each seeded dish to its photo **by name**, on every start, but **only when the
  dish has no photo**, so teammates' existing databases get them on the next run and an admin upload is never
  replaced. Admin upload (PB06) is unchanged: it still saves to the git-ignored `wwwroot/uploads/menu/`.
  `DbSeeder.cs` is Wallace's/Carynn's file: group OK needed. Tested: all 15 linked and served (HTTP 200), Menu and
  landing "From our kitchen" show them, an admin-uploaded photo survives a restart, an emptied one is refilled.
- **2026-11-05** — Jordan: **status review + landing page.** Checked every Known issue / Idea / Decision against the
  code and marked the cleared ones above (D1, D3–D5, BUG1–3, BUG6–8, UX1, UX2, UX4). Still open: BUG5 on 3 customer
  pages, UX3 booking reach (14/60 days vs 1 week), staff/admin can still open customer pages, PB27 (Jordan), PB28
  (Carynn). GitHub: PB17–23, PB25, PB26 issues are done but still open (no "Closes #N"); PR #66 (PB24, Carynn)
  reviewed: 3-tier, CSV quoting, BOM and CSV-injection guard all fine, merges cleanly with #65; `main` not updated since
  29 Oct. **Landing page** (`Pages/Index`, I4): full-width hero slideshow (crossfade every 5 s + slow zoom, respects
  reduced motion), headline and Book / Menu buttons, "open today" line, signature dishes from `IMenuService` (first
  available dish per category), story, visit us (hours with today bold; address/phone from optional `Address` /
  `Phone` settings), what's on (active promotions + announcements), closing call to action. Photos go in
  `wwwroot/images/landing/hero-1..5.jpg` (README there); with none, soft coloured slides. Table QR still goes straight
  to the Menu. Home page model now reads 5 existing services; no schema change.
  **Then, after a mockup on :5105 that Jordan approved:** 5 hero photos from Unsplash (Unsplash License; credits in
  `wwwroot/images/landing/README.md`, and `credits.json` drives the "Photo: name / Unsplash" line on each slide).
  "From our kitchen" shows 8 dishes (taken in turn from each category) with **no prices**, as an endless row drifting
  left (track copied once by script, CSS slides it by one copy, hover pauses, edges fade; reduced motion = manual
  scroll). Dish cards still show the placeholder plate: the seed menu has no photos (see dish photo question).
- **2026-11-04** — Jordan: **modern date pickers** (committed `3dfa114`). Compared with OpenTable / Grab /
  Chope (booking) and calendar apps (day lists). **A · booking picker** (`_BookingPicker` partial +
  `BookingPickerViewModel` + `wwwroot/js/booking-picker.js`): day strip for the next 14 days (closed days crossed
  out, "Another date" for up to 60 days), 30-minute time chips inside opening hours up to the last seating, grouped
  Lunch / Dinner (today hides slots inside the 30-min notice), guest chips 1–8; area chips on Book a table. Used on
  Book a table, customer Change booking and staff Edit booking (Wallace's pages, markup only). **B · day stepper**
  (`_DayStepper` + `DayStepperViewModel`): ‹ day › + Today on staff Bookings, Bills and Kitchen history; links change
  only the page's own date field. Both post the **same field names as before**, so no PageModel/service change; the
  server still checks hours, notice and overlaps. Covers most of **UX3**. Admin range picker (C) left as future work
  (Dashboard already has quick ranges). Tested: today 7:00 PM for 4 found 4 tables; stepper on all 3 staff pages;
  375 px. Full slots are not greyed out yet (would need an availability lookup in `ReservationService`).
- **2026-11-04** — Jordan: **staff home redesigned as "Tonight"** (same branch). Picked from 4 static mockups
  (dark console, action inbox, three-pane cockpit, dark inbox): Jordan chose the **action inbox** plus the cockpit's
  table panel. The Floor page (still where staff land) is now a to-do list grouped Now / Next up, each row with its one
  button (Take payment → Bill page, Mark served, Send to kitchen, Confirm booking, Check kitchen, Open table), beside
  the table map and stat tiles. Tapping a table slides in its panel (guests, every order with dishes and prices, table
  total, Take payment, plus "Open bill" on other unpaid orders). Buttons reuse the existing Floor handlers, so no rule
  is written twice. `FloorService` now fills `Detail`, `Action`, `ReservationId` and `IsNow` on each Needs-you item;
  `TableAction` gained `SendToKitchen` and `OpenTable`. Menu item Floor → Tonight. No schema change. Tested end to
  end with order #7 at G1 (QR order → Pass → ready → Mark served → bill due → Take payment opens Bill/7), desktop and
  375 px. Order #7 left unpaid on the laptop DB for the demo.
  **Then matched to mockup B's look** (Jordan: "same as what I chose"): staff now get a **top bar instead of the side
  menu** (`_StaffTopBar.cshtml`: wordmark, shift, pill tabs Tonight / Kitchen / Bookings / Bills / More, avatar menu);
  `_BackOfficeLayout` picks it when the user is Staff and not Administrator, so admins keep the side menu. Tonight has
  B's greeting, icon squares, waiting times, Now / Soon / Later groups, and side cards with a mini table map that opens
  the table panel. Jobs come first on phones. Markup/CSS only on top of the earlier FloorService change.
  **Account settings for staff/admin:** Manage `_Layout` now uses `_BackOfficeAccountLayout` for Staff and
  Administrator: a plain bar with the wordmark and "← Back to Tonight" (staff) or "← Back to Dashboard" (admin), no
  customer links. Customers keep the normal site header.
  **Tables tab** (from mockup C, without live tickets): new `/Staff/Tables` page, the full table map with the chosen
  table's panel beside it (guests, orders, total, Take payment, Complete table). The panel markup moved into a shared
  `Pages/Staff/_TablePanel.cshtml` (+ `TablePanelViewModel`) used by both Tonight's slide-in and Tables; its forms post
  to the current page, and both pages have the same thin handlers calling the same services. Tested: pay order #7 in
  cash → G1 "Paid · ready to complete" → Complete table → "Table completed.", G1 free. **Table colours changed**
  (Jordan): free = plain, booked = amber dashed, in use = red, bill due = deeper red.
- **2026-11-04** — Jordan: **staff pages made consistent** on `feature/UI-staff-consistency` (stacked on `feature/UI-account-settings`, PR #64). Wallace's
  Reservations, Billing and Reviews used a second style (long titles, Bootstrap badges as stats, cyan
  `alert-info` empty messages, "(PB12)/(PB13)" in labels). Added shared pieces to `backoffice.css` (`bo-head`
  header with stat tiles, `bo-filters` bar, `bo-empty` empty state) and a `_EmptyState` partial +
  `EmptyStateViewModel` (inline SVG icons, no icon font). Applied to all staff pages; The Pass columns now say what
  an empty column means. **Markup and CSS only** in Wallace's pages, no C#/service change. Build 0 warnings;
  checked every staff page at 1440 px and 375 px.
- **2026-11-04** — Jordan: **account settings redesign** on `feature/UI-account-settings` (off
  `feature/UI-shell-account-menu`, so it needs PR #63 merged first). Carynn's Identity Manage pages (PB02/PB03):
  new Manage `_Layout` with a profile card (initials, name, email, role) and a section menu (chips on phones);
  Profile, Email, Password and "Privacy and data" each in one card. **One C# change in Carynn's code:**
  `Manage/Index.cshtml.cs` can now edit **Full name** (`[Required]`, max 100, saved with `UserManager.UpdateAsync`);
  before, a name could only be set at sign-up. Password page shows the rules and a strength bar (vanilla JS, a hint
  only; the server still enforces the rules). Two-factor and external logins left out of the menu (pages still
  exist). No schema change. Build 0 warnings; tested name change (header avatar and card update), empty name refused,
  strength bar, at 557 px and 1440 px.
- **2026-11-04** — Jordan: **site shell and account menu** on `feature/UI-shell-account-menu` (off `dev`, after PR #62).
  Login and Register now use their own `_AuthLayout` (in `Areas/Identity/Pages/Account/`): white header with only the
  wordmark and one **Sign up** / **Log in** button (no nav links), the green tile pattern fills the page, the form is a
  white card at 93% opacity (same width on both pages), and the footer is one slim line. **Shared files** (Jordan owns
  layout/styling; group heads-up needed): `_Layout.cshtml`, `_LoginPartial.cshtml` (Carynn's partial), `site.css`.
  Header and footer bars are full width (`container-fluid`); page content cap 1120 → 1320 px; side gutter is
  `clamp(1rem, 3vw, 2.5rem)` so it scales with the screen; footer stays one line on phones. Guests see one **Log in**
  button; signed-in users see an initials avatar + first name that opens a menu (My account, Password and security,
  Back office for staff/admin, Log out). The avatar sits outside the collapsing menu so it stays visible on phones.
  Back office layout unchanged. Build 0 warnings; checked as guest, customer and staff at 557 px and 1440 px.
- **2026-11-04** — Jordan (Carynn's OK via Jordan): **login and register restyled** on `feature/UI-auth-pages` (off `dev`).
  Split screen like the Uber Eats / OpenTable / Grab sign-in pages: a teal brand panel with a Peranakan tile pattern
  (inline SVG, no image file) on the left and the form on the right; on phones the form takes the whole screen.
  Removed the template's "no external services" note and the unused resend-confirmation link
  (`RequireConfirmedAccount` is off). Register pairs name/phone and password/confirm on wide screens and shows the
  password rules from `Program.cs` up front. **Markup and page CSS only** — `Login.cshtml.cs`, `Register.cshtml.cs`,
  `site.css` and `_Layout` untouched. Found: Bootstrap 5.3 colours links from `--bs-link-color-rgb`, which `site.css`
  does not set, so links site-wide are Bootstrap blue; fixed locally on these two pages only (site-wide fix needs a
  `site.css` change, group OK). Build 0 warnings; tested over HTTP (wrong password, good login, weak password,
  mismatched confirm — no account created) and at 375 px and 1280 px.
- **2026-11-03** — Jordan: **table QR ordering** on `feature/PB14-table-qr`. New nullable `Order.RestaurantTableId`
  (migration `AddOrderRestaurantTable`) and the **QRCoder** NuGet package (MIT, draws PNGs without System.Drawing).
  Staff print `/Staff/TableQr` (Front of house menu); each QR holds a link signed with ASP.NET Data Protection, so
  typing another table's id does not work. Scanning remembers the table in the session for 3 h (session idle limit
  60 min still applies). Checkout order: scanned table → booking on now → Held pre-order → **refused** (no more
  "No table" orders; Pending bookings get their own message). Floor shows walk-in orders on their table, and Complete
  table works without a booking. TC-P05 run and passed. Shared files touched: `Program.cs` (1 `AddScoped`),
  `Migrations/`, `.csproj`, `_BackOfficeNav.cshtml` (1 link). Known limits: QR links are tied to the PC that printed
  them (Data Protection keys are per machine); a phone must reach the PC's network address, not `localhost`; booking
  availability (Wallace) does not know about walk-ins; Carynn's table delete would fail for a table with walk-in
  orders but no bookings (FK, no cascade).
- **2026-11-03** — Jordan: **freeze postponed**: features are not finished, so Sprint 5 carries on. Agreed Carynn's
  Sprint 5 asks (PB24 export on the Dashboard, PB28 `GetTopItemIdsAsync` in `IOrderService` + recommended row on
  `/Menu`, 2 `AddScoped` lines, time-zone fix, lockout fix). Table QR ordering back on Jordan's list.
- **2026-11-02** — Jordan: **pre-orders** on `feature/PB14-pre-orders` (off `feature/UI-staff-floor`). Ordering rule
  agreed with Jordan: at the restaurant the order goes to the table's booking as before; from outside, a customer
  with a **Confirmed** booking still to come places a **pre-order**, saved with the new status **`Held`** and kept off
  The Pass. 5 min before the booking a Needs-you chip appears on Floor; staff check the customer has arrived (in
  person or by phone) and press **Send to kitchen** (Held → Pending), which they can also do earlier if the customer
  arrives early. Checkout says where the order will go before it is placed. Release resets `CreatedAt`, so kitchen
  timers start then. A held pre-order blocks Complete table and can be cancelled. **No schema change**: `OrderStatus`
  is stored as a string, so adding `Held` needs no migration. Not done yet: orders with no booking still go through
  as "No table" until **table QR ordering** (needs a new `Order.RestaurantTableId` column, group OK) is built; then
  they will be blocked. Build 0 warnings; tested over HTTP (pre-order placed → no chip at T-60 → chip at T-4 → send
  → on The Pass; second send refused; unconfirmed booking gives a No-table order).
- **2026-11-01** — Jordan: **staff side** on `feature/UI-staff-floor` (off `feature/UI-back-office`; spec
  `docs/superpowers/specs/2026-11-01-staff-floor-design.md`, plan `docs/superpowers/plans/2026-11-01-staff-floor.md`).
  Research: StoreHub, FeedMe and Qashier open front of house on a table layout, GrabMerchant and FeedMe KDS open
  the kitchen on an order queue, and none show staff money, so **D5 (2) is answered "no money totals for staff"**.
  New **Floor** (staff landing page): one tile per table, state worked out by the new `FloorService` (Bill due,
  In use, Booked, Free), a **Needs-you** bar (late orders, bills due, ready to serve, unconfirmed or late bookings)
  and a drawer with one next-step button (Confirm booking, Mark served, Take payment → Wallace's Bill page, Complete
  table). **The Pass** replaces the Orders board view (same URL): ticket rail New → Preparing → Ready → Served, age
  chips, allergy strip; the old board is under History. **Today**: read-only run sheet. Staff menu regrouped (Front
  of house, Kitchen, Counter, Guests). Live parts refresh every 20 s (`staff-live.js`, Razor partials).
  **Found while testing: no code ever set `Order.ReservationId`**, so every order was "No table". `PlaceOrderAsync`
  now links an order to the customer's booking whose slot covers now (15 min early allowed), read through
  `IReservationService`; the checkout banner says "placed for your table T4". Also: `Order.CreatedAt` is local
  time (only the seed uses UTC), so timers use `DateTime.Now`. **No schema change.** **Group OK in the PR:**
  `Program.cs` (1 line), `Login.cshtml.cs` (Carynn, staff land on Floor), Floor calling Wallace's
  `UpdateStatusByStaffAsync`, `OrderService` now reading Wallace's `GetCustomerReservationsAsync`. Build clean,
  0 warnings. Verified over HTTP as customer@ and staff@: Booked → Confirm → Seated → order linked to M1 → Mark served
  → Bill due → crafted Complete refused → cash → Complete table → Free. **Still to do:** TC-F06/TC-F07 in a real
  browser (iPad landscape/portrait, 390 px). Test bookings #2–#4 and orders #3–#6 were left in the local dev DB
  (table M4 left on Bill due, order #3 "No table" left Ready) so the states can be seen.
- **2026-11-01** — Jordan: **summary of `feature/UI-back-office`, ready for PR** (branched from
  `feature/PB20-order-payment-notifications`, so merge PB20 first). Every change, in order:
  1. `407a0de` Design spec `docs/superpowers/specs/2026-11-01-back-office-design.md` (look "Peranakan Tile", chosen
     over Kopitiam, Batik Indigo and Pasar Malam).
  2. `f51e0ed` Back office shell for `/Admin` and `/Staff`: `_BackOfficeLayout`, role-filtered `_BackOfficeNav`
     (Insights, Operations, Restaurant, Marketing, People; staff see Operations only), folder `_ViewStart`s,
     `backoffice.css` scoped to `body.bo`. Slide-in menu below `lg`.
  3. `137df1d` **BUG1:** customer navbar shows staff/admin one "Back office" link instead of customer links.
  4. `4dbad49` Login lands admins on `/Admin/Dashboard`, staff on `/Staff/Orders` (only when no `ReturnUrl`).
  5. `8314205` Dashboard: full width, filter refreshes on change (no "Show report"), tables replaced by "View all"
     links, "Bookings by status" removed, stat notes only where they add a number.
  6. `403566e` **Monitoring + Customer activity: 62 → 366 days**, one range query instead of one per day.
  7. `365a6a2` New **Dish sales** page `/Admin/Dashboard/Dishes` (every dish incl. 0 sales), linked from Popular dishes;
     quick ranges moved to shared `_QuickRanges` partial.
  8. `86f4993` Back office fixes found in review: Bootstrap-blue links, invisible ☰ icon, clipped dish names on phones.
  9. `1ecf19a` Sales as a line chart; "This month" replaced by a 12-month picker; only one range ever highlighted.
  **Needs group OK in the PR:** `_Layout.cshtml` (shared), `Login.cshtml.cs` + `AdminMonitoringService` +
  `CustomerActivityService` (Carynn), the new frame around Wallace's staff pages.
  **Still open from this work:** same blue-link bug on the customer site (`site.css`, shared); `fix/UX4-configurations-label`
  is superseded and can be deleted. **Suggested next:** "vs previous period" on the stat tiles, an Unpaid tile,
  hourly grouping for "Today", same filter (chips + month picker + auto-refresh) on Monitoring and Customer activity.
- **2026-11-01** — Jordan: dashboard filter and chart. **Sales is now a line chart** (filled, monotone curve) to show
  the trend; Bookings and Popular dishes stay bars (counts). **"This month" chip replaced by a month picker** with the
  last 12 months (current month "so far"), on Dashboard and Dish sales (`_QuickRanges`), after the e-wallet history
  pattern. Fixed: on the 1st, "Today" and "This month" were both highlighted; now only one option is ever active
  (chips first, then the picker). Month names fixed to English regardless of the PC's locale.
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
