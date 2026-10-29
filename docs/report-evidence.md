# Report Evidence Log

Each member's report is **individual**. Use this file to collect evidence as you build, so
Chapters 5–7 aren't written from memory at the end. Add entries under your own name.

## Chapter checklist
| Chapter | Evidence to collect while building |
|---|---|
| 3 Project management | Branch list, own commit history, merged PR, contributors graph, sprint board |
| 4 Design | Use case, class, ER diagrams (shared) · own sequence diagram |
| 5 Development | Screenshot of each page you built · short code excerpts of key logic |
| 6 Quality assurance | Test cases (ID, steps, expected, actual, pass/fail) · screenshots of validation and error handling |
| 7 Reflection | Problems hit and how they were solved · what you'd improve |

## AI use declaration (required by the brief)
Each report must state which AI tools were used and for what. Keep a note of how you used them.

---

## Jordan — Domain C

| Date | Item | Evidence / note |
|---|---|---|
| 2026-10-11 | Layout shell (Sprint 1) | See the detailed entry below |
| 2026-10-20 | PB06 + PB08 menu v1 (Sprint 2) | See the detailed entry below — TC-M01–M10 passed 21/10 |
| 2026-10-22 | PB16 payment + receipt v1 (Sprint 3) | See the detailed entry below — manual run-through done 28/10, merged PR #15 |
| 2026-10-28 | BUG4 home hours + BUG5 palette | See the detailed entry below |
| 2026-10-29 | Admin list methods for Carynn's PB21/PB23 (`feature/admin-list-methods`) | No UI. Ch.7: agreeing a service interface across domains (A reads C's data through `IPaymentService`/`IOrderService` only); inclusive date range done as `>= from 00:00` and `< to + 1 day` so the last day's evening rows are not lost |

### 2026-10-28 — BUG4 home opening hours + BUG5 palette (`fix/BUG4-BUG5-home-hours-palette`)

**Ch.5 screenshots:** home page hours before (hard-coded) and after; set a day to Closed in `/Admin/OperatingHours`
and show "Closed" on the home page; Pay page tiles and Checkout header before (blue) / after (palette).

**Ch.6 test cases**
| ID | Steps | Expected |
|---|---|---|
| TC-H01 | Admin changes Monday to 09:00–21:00, save, open `/` | Home shows Monday 09:00 AM - 09:00 PM |
| TC-H02 | Admin ticks Sunday Closed, save, open `/` | Home shows Sunday: Closed |

**Ch.7 reflection:** the home page was a layout-shell placeholder that was never wired to real data. It went unnoticed
because the page looked finished. Fixed by reading Carynn's `IOperatingHoursService` through its interface, not her table
(the 3-tier and domain rules). BUG5: a theme gap (Bootstrap "primary" not re-themed) that the proper fix would solve in the
shared `site.css`. I fixed only my own pages so I wouldn't edit a shared file without group agreement.

### 2026-10-22 — PB16 payment + receipt (`feature/PB16-payment`)

**Ch.5 screenshots to capture**
1. Confirmation page with **Unpaid** badge + **Pay now**; the same page after paying (**Paid** + View receipt).
2. `/Payments/Pay` at 375px — Card / E-wallet tiles, card form, "Pay RM x" button, Demo mode note.
3. Pay page after a declined card — error message, Failed row in "Payment attempts", card number and CVV boxes empty.
4. Receipt on screen, then the browser **print preview** (no navbar, footer or buttons).
5. Staff order board with Paid / Unpaid badges on the cards.
6. Code excerpts: `PaymentService.PayOnlineAsync` (transaction + row lock + amount from DB),
   `PassesLuhnCheck`, and `IPaymentGateway` (swappable provider — dependency injection point).

**Ch.6 test cases** (full list in the spec §11)

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-P02 | Pay with `4242 4242 4242 4242`, future expiry, CVV 123 | Receipt shown; Payments row Card / Paid / amount = order total / `SIM-…` ref | |
| TC-P03 | Pay with `4000 0000 0000 0002` | "Card declined by issuer."; Failed attempt listed; card fields empty | |
| TC-P05 | `4242 4242 4242 4241` (bad Luhn); expiry `01/24` | "not valid" / "has expired"; nothing saved | |
| TC-P07 | Paid order: open Pay page; resubmit the old form | "already been paid", no form; still one Paid row | |
| TC-P10 | Staff: cancel a paid order | "has been paid. Refund it first." | |

**Ch.7 reflection notes**
- *Double payment race.* Checking "is it paid?" then saving leaves a gap where two tabs both pass the check.
  Fixed by doing check + save in one transaction after an UPDATE that locks the order row, so the second request waits.
- *Never echo card data.* `ModelState.Remove` would also delete the field's error message;
  `ModelState.SetModelValue(key, null, "")` blanks the box but keeps the error.
- *Simulated gateway behind an interface.* A real provider would replace one class in `Program.cs`;
  real systems never see raw card numbers at all (hosted payment form, PCI DSS).
- *Amount never from the form.* A crafted POST with `Amount=1.00` is ignored — `PaymentInput` has no amount field.
- *TempData cannot hold decimals*, so cash tendered/change are passed to the receipt as strings.

**AI use declaration note:** Claude Code drafted the payment spec, the service and page code, and ran an
automated smoke test on a throwaway database. The simulated-gateway choice, spec decisions and review were mine.

### 2026-10-20 — PB06 + PB08 menu (`feature/PB06-PB08-menu`)

**Ch.5 screenshots to capture**
1. `/Menu` at phone width (375px) — one-column rows, sticky category bar, a greyed "Sold out" dish.
2. `/Menu` at 1280px — 3-column card grid. Put 1 and 2 side by side for the responsive-design point.
3. `/Menu?search=zzz` — the empty state with "Clear search".
4. `/Admin/Menu` Items tab on an iPad-width window (768px) — table with thumbnails and badges.
5. Edit Item with the current-photo preview, then the same dish on `/Menu` showing the new photo.
6. Delete Item page for an ordered dish — warning shown, Delete button disabled.
7. Code excerpt: `MenuService.GetMenuForBrowseAsync` (filtering in the Business layer) and
   `ImageStorageService.ValidateImage` (upload security).

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-M01 | Logged out, open `/Menu` at 375px | Menu loads without login, one column, sold-out item greyed with badge, no horizontal scroll | Pass (21/10/2026) |
| TC-M02 | Tap "Desserts"; then search "zzz" | Only desserts shown; then empty state + Clear search | Pass (21/10/2026) |
| TC-M03 | Log in as customer@rroms.com, open `/Admin/Menu` | Access denied — page not shown | Pass (21/10/2026) |
| TC-M04 | Admin: Add item with a PNG photo | Success banner; dish appears on `/Menu` with the photo | Pass (21/10/2026) |
| TC-M05 | Admin: Add item with a `.txt` renamed to `.png`; then a 5 MB JPG | "Only JPG, PNG or WebP…" / "2 MB or smaller" error; nothing saved | Pass (21/10/2026) |
| TC-M06 | Insert an OrderItem for a dish (SQL), then try to delete that dish | Warning shown, Delete disabled; crafted POST also refused | Pass (21/10/2026) |
| TC-M07 | Delete a category that still has dishes | Refused with message | Pass (21/10/2026) |
| TC-M08 | Add category "desserts" (lower case) | Duplicate-name error under Name | Pass (21/10/2026) |
| TC-M09 | Click "Mark sold out" on a dish | Badge flips to Sold out; dish greyed on `/Menu`; click again restores it | Pass (21/10/2026) |
| TC-M10 | Price 0, price 10000, empty name | Field-level validation errors, form not submitted | Pass (21/10/2026) |

**Ch.7 reflection notes**
- *Responsive design without screen detection.* My first idea was to read the screen size in
  JavaScript to pick a phone or PC layout. CSS media queries (Bootstrap breakpoints) do this in the
  browser automatically, survive rotation and iPad split-screen, and need no script — so the menu
  is written phone-first with `@media (min-width: 768px)` / `992px` for larger screens.
- *`@section` is a Razor keyword.* A loop variable called `section` broke the build
  (`RZ2005: The 'section' directive must appear at the start of the line`). Renamed it.
- *CSS isolation and tag helpers again.* Same lesson as the layout shell: the category tabs are
  `<a asp-page>` links, so their scoped styles had to go through a plain parent with `::deep`.
- *Testing a rule before its feature exists.* TC-M06 needs an ordered dish, but ordering (PB14) is
  not built yet, so a test Order + OrderItem row was inserted with SQL, tested, then removed.
- *Delete rules protect history.* Deleting a dish that appears on past orders would break those
  orders and receipts, so the service refuses and suggests "Mark sold out" instead. The rule is
  checked again on POST, not only when the page loads.

**AI use declaration note:** Claude Code was used to draft the menu design spec, the service and
page code, and the test cases. The QR / phone-first direction, image-upload choice, delete rules
and all review of the output were mine.

### 2026-10-11 — Layout shell (`feature/layout-shell`)

**Ch.5 screenshots to capture**
1. Home page at desktop width — navbar, hero, opening hours, footer in one frame.
2. Home page at phone width (F12, then Ctrl+Shift+M for the device toolbar) — collapsed
   hamburger menu, then the same with the menu open.
3. The navbar with the active-page underline visible on Home, for the "user always knows where
   they are" point.
4. Keyboard focus ring — press Tab until the Privacy link is focused, screenshot the plum outline.
   Good evidence for the accessibility paragraph.
5. `site.css` design-token block (`:root`) — a short code excerpt showing one place controls the
   whole palette.

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-UI-01 | Open the site at 1440px, then drag the window down to 375px | Navbar collapses to a hamburger below 768px; menu opens and closes on click; no horizontal scrollbar at any width | |
| TC-UI-02 | Open any page and press Tab repeatedly | Every link and button shows a visible plum focus ring in reading order; nothing is reachable but invisible | |

**Ch.7 reflection notes**
- *CSS isolation does not reach tag-helper output.* The navbar styles were first written in
  `_Layout.cshtml.css`. The build succeeded and the scoped stylesheet was served, but the wordmark
  and the Home link were unstyled: CSS isolation works by stamping a `b-xxxxx` attribute on each
  element and rewriting selectors to match, and elements rendered by a tag helper
  (`<a asp-page="...">`) come out without that attribute. Found it by reading the served HTML
  rather than by trusting the clean build. Fixed by moving the shell styles into the global
  `site.css`, nested under `.rr-header` / `.rr-footer` so they cannot leak. Worth a paragraph:
  a successful build is not evidence that the page is correct.
- *Linking to pages that do not exist.* An `asp-page` tag helper pointing at a missing page throws
  at render time, so the Menu / Book a table / My orders links are inert `<span>`s with the
  replacement line written in a comment beside each.
- *Working around a shared file.* `_Layout.cshtml` belongs to the whole group, so the restaurant
  name is a single `const` and the `_LoginPartial` slot is pre-cut — Carynn's Identity work drops
  into it without touching the rest of the file.

**AI use declaration note:** Claude Code was used to survey the lecturer's lab projects for
conventions, to draft the layout markup and stylesheet, and to diagnose the CSS isolation problem.
Design direction, the decision to cap the content width and all review of the output were mine.

## Wallace Chan — Domain B

| Date | Item | Evidence / note |
|---|---|---|
| 2026-10-14 | ER diagram (`docs/diagrams/ERD.drawio`, `ERD.png`) | 15-table normalized schema; updated `User` to `ApplicationUser` (`Id` string PK) and exported on white background |
| 2026-10-14 | Core database schema & seed (`feature/schema`) | 4 enums, 14 entity models, `AddCoreSchema` migration, `DbSeeder` (2 areas, 8 tables, 7 operating hours, 4 categories, 15 items) |
| 2026-10-22 | PB09 + PB10 + PB11 customer reservations (`feature/PB09-11-reservations` & `fix/sprint2-tidyup-wallace`) | See detailed entry below — TC-R01–R09 passed |

### 2026-10-22 — PB09 + PB10 + PB11 customer reservations (`feature/PB09-11-reservations`)

**Ch.5 screenshots to capture**
1. `/Reservations/Create` after searching for 2 guests — showing available tables (`G1`, `M1`, `M2`, `G2`, `M3`, `M4`) with 6- and 8-seater tables hidden to prevent capacity waste.
2. `/Reservations/Confirmation/{id}` — showing a newly created reservation in `Pending` status with `dd/MM/yyyy` and `hh:mm tt` formatting.
3. `/Reservations` (My Reservations) at desktop width (1280px) vs phone width (375px) — showing the responsive table-to-card switch (`d-none d-md-block` vs `d-md-none`).
4. `/Reservations/Edit/{id}` — modifying a reservation slot and re-checking available tables while excluding the current reservation ID from self-conflict.
5. Code excerpt: `ReservationService.GetAvailableTablesAsync` (90-minute dining slot + 15-minute turnover buffer overlap formula and cross-domain calls to `IDiningAreaService` and `IOperatingHoursService`).

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-R01 | Logged out, navigate to `/Reservations/Create` | Redirected to `/Identity/Account/Login` via folder authorization | Pass |
| TC-R02 | Log in as `customer@rroms.com`, search valid future date at 06:00 PM for 2 guests | Available 2- and 4-seater tables displayed; 6- and 8-seater tables excluded | Pass |
| TC-R03 | Select Table G1, enter special request, submit booking | Saved with `Pending` status; redirected to Confirmation page with green `TempData` banner | Pass |
| TC-R04 | Search the same date at 06:30 PM (or 07:35 PM inside the 15-min buffer) | Table G1 is excluded from available tables due to the 90m + 15m overlap window | Pass |
| TC-R05 | Attempt to book a second table with the same customer account at 06:30 PM | Refused with "You already have an active reservation during this time window." | Pass |
| TC-R06 | Enter party size > 8 or time outside opening hours (e.g., 10:00 PM when closing is 11:00 PM) | Friendly validation error shown; last seating enforces 90m before closing | Pass |
| TC-R07 | Open `/Reservations` at 375px phone width | Table collapses into stacked mobile cards with no horizontal scrollbar | Pass |
| TC-R08 | Click Modify on an upcoming booking (> 2h away), change table/time, save | Reservation updated; self-conflict excluded via `excludeReservationId` | Pass |
| TC-R09 | Click Cancel on an upcoming booking (> 2h away) | Status changes to `Cancelled`; Modify/Cancel buttons hidden; table freed for other bookings | Pass |

**Ch.7 reflection notes**
- *Why exact-time matching fails for reservations.* Checking `StartTime == requestedStartTime` would allow a 6:30 PM booking to double-book a table already reserved from 6:00 PM to 7:30 PM. Implemented interval overlap logic (`r.StartTime < windowEnd && r.EndTime > windowStart`) with a 90-minute dining slot plus a 15-minute turnover buffer on both ends.
- *Cross-domain service boundaries.* Initially `ReservationService` queried `_context.DiningAreas`, `_context.RestaurantTables`, and `_context.OperatingHours` directly. Refactored to inject `IDiningAreaService` and `IOperatingHoursService` so Domain B respects Domain A's service boundary.
- *SQL Server multiple cascade paths.* During `AddCoreSchema`, having `Reservation`, `Order`, and `Feedback` all cascade-delete from `ApplicationUser` caused SQL Server cycle errors. Solved by configuring `DeleteBehavior.Restrict` / `NoAction` in `OnModelCreating`.
- *Midnight wrap-around with `TimeOnly`.* Calling `StartTime.AddMinutes(90)` on a late time like `23:00` wraps past midnight to `00:30`, making `endTime > CloseTime` evaluate falsely. Fixed by comparing `startTime > hours.CloseTime.AddMinutes(-90)` (`lastSeatingTime`) directly.

**AI use declaration note:** Gemini was used to review EF Core `OnModelCreating` cascade configurations, verify edge cases in the time-interval overlap math, and assist with scaffolding Razor Page markup and test case tables. Schema design, business rules, and testing were verified by me.

## Caryn — Domain A

| Date | Item | Evidence / note |
|---|---|---|
| 2026-10-23 | Sprint 2 tidy-up | PR #12 — backup file removed, Menu hub card, CS8618 fix, this evidence entry |
| 2026-10-22 | PB07 promotions, announcements, settings, admin hub (Sprint 2) | PR #10 — see the detailed entry below |
| 2026-10-22 | PB05 dining areas, tables, operating hours (Sprint 2) | PR #9 — see the detailed entry below |
| 2026-10-15 | PB04 role locks + admin user management (Sprint 1) | PR #6 — see the detailed entry below |
| 2026-10-12 | PB01–PB04 Identity setup (Sprint 1) | PR #3 — see the detailed entry below |

### 2026-10-22 — PB07 promotions, announcements, settings, admin hub (`feature/PB07-promotions-settings`)

**Ch.5 screenshots to capture**
1. `/Admin` hub — all cards, then the same at phone width.
2. `/Admin/Promotions` showing an Active, an Inactive and an Expired badge.
3. `/Admin/Promotions/Create` with a validation error (discount 101, or end date before start date).
4. `/Admin/Announcements` list and the Create form.
5. `/Admin/Settings` with the "Add a setting" form.
6. Code excerpt: `PromotionService.Validate` (discount 1–100, end date on or after start date).

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-A10 | Promotion: discount 0, then 101; end date before start date | Error each time; nothing saved | |
| TC-A11 | Promotion and announcement: click Deactivate / Activate | Badge flips; one success message shown | |
| TC-A12 | Settings: change a value and save; then add a key that already exists | Value saved; duplicate key refused with a message | |
| TC-A13 | Log in as customer, open `/Admin` and `/Admin/Promotions` | Access Denied page | |

**Ch.7 reflection notes**
- *Starting before the dependency was merged.* PB07 began while PB05 was still in review. The hub links to PB05 pages, and a link to a page that does not exist in the branch only fails when the page renders, not when the project builds, so a clean build was not proof that the hub worked. The cards were left out until PB05 reached `dev`.
- *Shared file conflicts.* Every member registers services in the same part of `Program.cs`, so merges conflict there. The rule is to keep all lines.
- *Settings design.* The page edits values and adds new keys but does not rename or delete existing keys, which keeps other code that reads a key from breaking.

**AI use declaration note:** Claude (Anthropic) was used to draft the service and page code and the test cases. [Write here what you did yourself: running and testing every page, fixing errors, deciding what to keep. Only true statements.]

### 2026-10-22 — PB05 dining areas, tables, operating hours (`feature/PB05-dining-setup`)

**Ch.5 screenshots to capture**
1. `/Admin/DiningAreas` with the table-count badge, and the message when deleting an area that has tables.
2. `/Admin/Tables` grouped by area, with one Inactive table.
3. `/Admin/Tables/Create` showing the duplicate-number error.
4. `/Admin/OperatingHours` with seven days and one closed day.
5. Code excerpt: `DiningAreaService.ValidateTableAsync` (unique number across all tables, capacity 1–20).

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-A06 | Delete a dining area that still has tables | Refused with a message; area stays | |
| TC-A07 | Create a table with a number already in use; then capacity 0 and 21 | Duplicate error; capacity must be 1–20; nothing saved | |
| TC-A08 | Delete a table that has reservations | Table becomes Inactive instead of being deleted, with a message | |
| TC-A09 | Operating hours: Close earlier than Open on one day; then tick Closed on that day | Error naming the day; with Closed ticked it saves | |

**Ch.7 reflection notes**
- *Rules live in the business tier.* The pages only call `IDiningAreaService` and `IOperatingHoursService`; uniqueness, capacity and delete protection are checked in the service, so they apply however the page is called.
- *Delete protection.* A table that has reservations is deactivated rather than deleted so past bookings keep their table.
- *Limit.* Closing times after midnight (for example 01:00) are not supported, because the rule is that Close must be after Open.
- *Late delivery.* This work was late against the sprint deadline; the lead was told so that the PR order could be planned.

**AI use declaration note:** Claude (Anthropic) was used to draft the service and page code and the test cases. [Write here your own part, only true statements.]

### 2026-10-15 — PB04 role locks and admin user management (`feature/PB04-admin-users`)

**Ch.5 screenshots to capture**
1. `/Admin/Users` — table with role badges and Active / Disabled badges.
2. `/Admin/Users/Edit` for another user, then for my own account showing the "cannot demote or deactivate yourself" message.
3. The Access Denied page when a customer opens `/Admin/Users`.
4. Code excerpt: the role policies and folder rules in `Program.cs`, and the self-demotion check in `UserAdminService.UpdateUserAsync`.

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-A03 | Log in as customer, open `/Admin/Users`; repeat as staff | Access Denied page, not the user list | |
| TC-A04 | Admin edits own account: set role to Customer, or untick Active | Error message; nothing changes | |
| TC-A05 | Admin deactivates another user; that user tries to log in | "Account disabled" message; not signed in | |
| TC-A14 | Admin changes a user's role from Customer to Staff | Success message; Index shows the Staff badge | |

**Ch.7 reflection notes**
- *Pages call a service, not the database.* `UserAdminService` uses `UserManager`; the pages never touch the DbContext, which keeps the three tiers separate.
- *Deactivating also ends the session.* Updating the security stamp when a user is deactivated invalidates their existing login, not only future logins.
- *Server-side rule.* The self-demotion rule is enforced in the service, so it holds even if someone changes the form data.

**AI use declaration note:** Claude (Anthropic) was used to draft the service and page code and to explain the Git and Visual Studio steps. [Write here your own part, only true statements.]

### 2026-10-12 — PB01–PB04 Identity setup (`feature/PB04-identity-setup`)

**Ch.5 screenshots to capture**
1. `/Identity/Account/Login` and `/Identity/Account/Register` (with the Full Name field).
2. The navbar logged out (Register, Login) and logged in (Hello name, Logout).
3. SSMS: `AspNetRoles` (three roles) and `AspNetUserRoles` (new user linked to Customer).
4. Code excerpt: `AddDefaultIdentity` options in `Program.cs` (password policy, lockout) and `DbSeeder.CreateUserAsync`.

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-A01 | Log in as admin, staff and customer (seeded accounts) in turn; log out each time | All three log in; logout works | |
| TC-A02 | Register a new user with a Full Name and a valid password | Logged in straight away; Customer role assigned; FullName saved | |
| TC-A15 | Register with a weak password (no uppercase, no symbol) | Password policy error; account not created | |
| TC-A16 | Five wrong passwords in a row | Account locked out for 5 minutes | |

**Ch.7 reflection notes**
- *The scaffolder created a second `ApplicationUser`.* The build failed with an ambiguous reference between `FoodOrderingSystem.Data.ApplicationUser` and `FoodOrderingSystem.Models.ApplicationUser`. Deleting the generated copy then caused 92 errors in the scaffolded pages. They were one problem repeated, and a `global using` for the Models namespace cleared them all. Lesson: read the first error before the rest.
- *Passwords.* Identity stores a salted PBKDF2 hash, unlike the lab's plain-text admin login. The seeded development passwords are known, so they must be changed before real deployment.
- *Security warnings.* NuGet reported NU1902/NU1903 for `System.Security.Cryptography.Pkcs` 9.0.11, a dependency of the SQL Server packages. They did not come from our code, so they were raised as a separate change.
- *Order of changes.* A `using` line added to `Program.cs` before the matching files existed caused error CS0234. Add the code first, then the registration.

**AI use declaration note:** Claude (Anthropic) was used to draft the setup code and to explain the Git and Visual Studio steps. [Write here your own part, only true statements.]
