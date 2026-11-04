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
| 2026-10-30 | PB22 admin dashboard v1 (Sprint 4) | See the detailed entry below |
| 2026-10-30 | BUG6 receipt → My orders, UX1 add-to-cart dialog + toast | See the detailed entry below |
| 2026-10-31 | PB20 order and payment notifications | See the detailed entry below |
| 2026-11-01 | Back office redesign (admin + staff UI), BUG1, Dish sales report, dashboard refinements | See the detailed entry below |
| 2026-11-01 | Staff side: Floor, The Pass, Today + order-to-table link (BUG8) | See the detailed entry below |
| 2026-11-04 | Login and register restyle (`feature/UI-auth-pages`) | See the detailed entry below |
| 2026-11-04 | Account settings redesign (`feature/UI-account-settings`) | See the detailed entry below |
| 2026-11-04 | Staff pages made consistent (`feature/UI-staff-consistency`) | See the detailed entry below |
| 2026-11-04 | Staff home "Tonight": to-do list + table panel | See the detailed entry below |

### 2026-11-04 — Staff home "Tonight": to-do list + table panel

**Ch.5 screenshots to capture**
1. The four design mockups side by side (ports 5101–5104) to show the options considered, then the chosen one.
2. `/Staff/Floor` (Tonight) with a "ready" job and a "bill due" job, map on the right.
3. The table panel open on a bill-due table: dishes, total, Take payment.
4. Tonight at phone width.

**Ch.6 test cases**

| ID | Steps | Expected | Actual (4/11) |
|---|---|---|---|
| TC-U15 | Customer orders via table QR (G1); staff start and finish it on The Pass; open Tonight | "#7 ready · G1" under Now with **Mark served** | As expected |
| TC-U16 | Press Mark served | Banner "Order #7 is now served"; the job becomes "G1 bill due · #7, RM 34.00" with **Take payment**; G1 turns red | As expected |
| TC-U17 | Tap table G1 | Panel slides in: order #7, both dishes with prices, RM 34.00, **Take payment** | As expected |
| TC-U18 | Press Take payment (list or panel) | Opens the Bill page for order #7 | As expected |

**Ch.7 reflection notes**
- *Designing by comparing mockups.* Four throwaway HTML mockups on separate ports let me feel each option before touching the app. The chosen design mixed two of them.
- *Reuse over rewrite.* The to-do buttons post to the handlers the table panel already had, and payment still goes through the existing Bill page, so no business rule exists twice.

**AI use declaration note:** Claude (Anthropic) was used to draft the mockups, markup, CSS and the FloorService changes. [Write here your own part, only true statements.]

### 2026-11-04 — Staff pages made consistent (`feature/UI-staff-consistency`)

**Ch.5 screenshots to capture**
1. `/Staff/Reservations` before (cyan "No reservations found" box, "Staff Reservation Management") and after (header with Pending / Confirmed / Special requests tiles, empty state).
2. `/Staff/Billing` after: "Unpaid orders" and "Still to collect" tiles.
3. `/Staff/Orders` (The Pass) with empty columns: dashed slots with "No new orders", "Nothing cooking".
4. Any staff page at phone width.

**Ch.6 test cases**

| ID | Steps | Expected | Actual (4/11) |
|---|---|---|---|
| TC-U13 | Open Reservations, Billing and Reviews with no data | Each shows the same header style and a friendly empty state with an icon and a hint | As expected |
| TC-U14 | Reservations: filter a date with bookings | Tiles count Pending / Confirmed / Special requests for that date; table shows the bookings | To run with demo data |

**Ch.7 reflection notes**
- *Two design languages in one back office.* Each member styled their own pages, so staff saw two products. A small set of shared CSS pieces (header, filter bar, empty state) fixed it without touching anyone's C#.
- *Developer labels leaked into the UI.* "(PB12)" and "(PB13)" were in visible labels; moved to comments only.

**AI use declaration note:** Claude (Anthropic) was used to draft the CSS and markup. [Write here your own part, only true statements.]

### 2026-11-04 — Account settings redesign (`feature/UI-account-settings`)

Carynn's Identity Manage pages (Domain A), restyled with one small C# addition (editable full name).

**Ch.5 screenshots to capture**
1. `/Identity/Account/Manage` at desktop width: profile card on the left, Profile card on the right.
2. Same page at phone width: compact profile card and the section chips.
3. Password page while typing a new password, showing the strength bar.
4. Profile after saving a new name: green "Your profile has been updated" and the new initials in the header avatar.

**Ch.6 test cases**

| ID | Steps | Expected | Actual (4/11) |
|---|---|---|---|
| TC-U09 | Profile: change Full name, Save changes | Success message; side card and header avatar show the new name and initials | As expected |
| TC-U10 | Profile: clear Full name, Save changes | "The Full name field is required."; nothing saved | As expected |
| TC-U11 | Password: type `abc`, then `abcDEF12!xy` | Meter shows Weak, then Strong | As expected |
| TC-U12 | Open the account pages at phone width | One column, menu as chips, no sideways scroll | As expected |

**Ch.7 reflection notes**
- *Template pages left as-is.* The scaffolded Identity pages were never restyled, and the profile had no way to change the full name, so the avatar kept showing whatever was typed at sign-up. Found while reviewing the new account menu.
- *Strength meter is only a hint.* It counts the same five rules as `Program.cs`, but the server still checks them on submit, so turning off JavaScript cannot weaken a password.

**AI use declaration note:** Claude (Anthropic) was used to draft the markup, CSS and the full-name change. [Write here your own part, only true statements.]

### 2026-11-04 — Login and register restyle (`feature/UI-auth-pages`)

Carynn's pages (Domain A), restyled with her OK. Markup and page CSS only; her C# is unchanged.

**Ch.5 screenshots to capture**
1. `/Identity/Account/Login` before (template, from an old commit) and after, at desktop width.
2. `/Identity/Account/Register` at desktop width (paired fields, password rules hint).
3. Both pages at phone width (DevTools, 375 px): brand panel hidden, form full width.
4. Login with a wrong password: the red "Invalid login attempt." box.

**Ch.6 test cases**

| ID | Steps | Expected | Actual (4/11) |
|---|---|---|---|
| TC-U01 | Log in with a wrong password, then the right one | Red error box; then logged in and sent home | As expected |
| TC-U02 | Register with password `abcdef` | Error box lists every missing password rule; no account created | As expected |
| TC-U03 | Register with Password and Confirm password different | "do not match" under Confirm password; no account created | As expected |
| TC-U04 | Open both pages at 375 px | Single column, 16 px gutter, no sideways scroll | As expected |

**Ch.7 reflection notes**
- *Styles did not change after editing.* Scoped CSS (`.cshtml.css`) is bundled at build time, so the running app kept the old bundle until it was restarted.
- *Links stayed blue.* Bootstrap 5.3 reads `--bs-link-color-rgb`, not `--bs-link-color`, so overriding the second did nothing. Fixed on these pages with a scoped rule; the site-wide fix belongs in the shared `site.css`.
- *Tag-helper elements and scoped CSS.* `<a asp-page>` and `asp-validation-summary` are rendered without the scope attribute, so they are styled with `::deep` from a plain parent element.
- *Hiding the navbar without touching the shared layout.* After review I wanted a focused sign-in page (no nav links, full-page green background). Editing `_Layout.cshtml` would need group agreement, so Login and Register got their own `_AuthLayout` instead: same head, header bar and footer, minus the links.

- *Layout that fits any screen.* The page sat in a narrow 1120 px column with empty sides, and the footer stacked into three lines on phones. Fixed with responsive CSS rather than JavaScript: full-width header/footer bars, a wider content cap, and a gutter set with `clamp()` so spacing grows with the screen.
- *Following familiar patterns.* Compared with Grab / foodpanda / OpenTable: logo top-left goes home, one "Log in" button for guests, an avatar menu top-right once signed in. I kept the account top-right (not under the logo) because that is where users look for it.

**Ch.5 screenshots to add:** Login at desktop width after the follow-up (white header with only the wordmark, green page, see-through card, slim footer); home page as a guest (single Log in button); the open avatar menu at desktop and at phone width.

| ID | Steps | Expected | Actual (4/11) |
|---|---|---|---|
| TC-U05 | As a guest, open any customer page | Header shows only one "Log in" button on the right | As expected |
| TC-U06 | Log in as customer, click the avatar | Menu shows name, email, My account, Password and security, Log out; Log out signs out | As expected |
| TC-U07 | Log in as staff, open /Menu, click the avatar | Menu also shows Back office | As expected |
| TC-U08 | Open the home page at phone width | Avatar stays visible next to the menu button; footer is one line | As expected |

**AI use declaration note:** Claude (Anthropic) was used to draft the markup and CSS. [Write here your own part, only true statements.]
| 2026-11-03 | Table QR ordering (PB14) | See the detailed entry below — TC-Q01–Q07 passed 3/11 |

### 2026-11-03 — Table QR ordering (`feature/PB14-table-qr`)

**What and why:** walk-ins have no booking, so an order needs its own table link. Each table gets a printed QR;
scanning it opens the menu and orders go straight to that table. Orders with no table and no confirmed booking are
now refused, so nobody at home can send food to the kitchen.

**Ch.5 screenshots:** `/Staff/TableQr` (grid of codes, and the print preview); phone camera scanning a code; the menu
with the green "You're at table G3" banner; Checkout "For table G3, sent to the kitchen now"; Checkout refused (yellow
box + disabled Place order + Book a table link); Floor G3 tile In use with the walk-in order, then "Paid · ready to
complete". Code excerpts: `TableQrService.CreateCode`/`ScanAsync` (Data Protection) and `OrderService.FindOrderTargetAsync`
(the four rules in order).

**Ch.6 test cases**
| ID | Steps | Expected | Actual (3/11) |
|---|---|---|---|
| TC-Q01 | Customer, no booking, no scan → Checkout, then POST anyway | Yellow "scan the QR" box, button disabled, POST refused, nothing saved | Pass |
| TC-Q02 | Staff opens Table QR codes | One card + QR per active table (8) | Pass |
| TC-Q03 | Open a QR link with the code edited, and `/Orders/Table?code=3` | "That QR code is not valid", no table remembered | Pass |
| TC-Q04 | Scan G3 → order | "Order #… placed for table G3"; row has RestaurantTableId, no ReservationId; on The Pass as "G3 · #…" | Pass |
| TC-Q05 | Walk-in order served → cash payment → Complete table on Floor | Tile Bill due → "Paid · ready to complete" → Free; order Completed | Pass |
| TC-Q06 | Pending booking tomorrow → Checkout; then booking Confirmed → Checkout | "Not confirmed yet" refusal; then Held pre-order for that table | Pass |
| TC-Q07 | Booking on now at G3 + scan G3 → order | Order linked to both the booking and G3 | Pass |
| TC-P05 | Held pre-order for the G3 booking → Floor drawer, then a crafted Complete POST | No Complete button; POST refused "Order #… is not finished or not paid yet" | Pass |

**Ch.7 reflection:** signed link vs a random code column: Data Protection avoided a second schema change, but codes
only work on the PC that made them (keys are per machine) — fine for a demo, a shared key store in production. The
first test run failed to pay because the cash form needs an amount; test scripts must send every bound field. A
refused order showed its message twice (warning box + validation summary), fixed by skipping the summary when the box
already explains it. The migration and package are announced to the group before pushing, because two migrations made in parallel conflict.


### 2026-11-02 — Pre-orders for confirmed bookings (`feature/PB14-pre-orders`)

**What and why:** customers use the system from their phone, often before they leave home. A customer with a
confirmed booking can now order in advance. The order is saved as **Held** and stays off The Pass, so the kitchen
does not cook for someone who may be late. 5 min before the booking, Floor shows a Needs-you chip; staff check the
customer is here (in person or by phone) and press **Send to kitchen**.

**Ch.5 screenshots:** Checkout showing "Pre-order for your booking on … (table …)"; the confirmation banner; My orders
with the grey "Pre-order" badge; Floor tile with "· pre-order" and the drawer with **Send #… to kitchen**; the Needs-you
chip "M3 pre-order #… · Customer 5:27 PM"; The Pass with the order in New after sending. Code excerpts:
`OrderService.FindOrderBookingAsync` (current booking vs next confirmed booking) and the Held branch of
`AdvanceStatusAsync`.

**Ch.6 test cases**
| ID | Steps | Expected | Actual (2/11) |
|---|---|---|---|
| TC-P01 | Customer with a Confirmed booking at 6:30 PM orders at 5:23 PM | Checkout says pre-order; order saved as Held, linked to the booking; customer notified | As expected (#2005) |
| TC-P02 | Staff open Floor at T-60 min, then at T-4 min | No chip at T-60 (but Send button already in the drawer); chip at T-4 | As expected |
| TC-P03 | Staff press Send to kitchen; a second iPad presses it again | First: "sent to the kitchen", order Pending and on The Pass; second: "already updated by someone else" | As expected |
| TC-P04 | Customer with only a Pending (unconfirmed) booking orders | Normal "No table" order, not a pre-order | As expected (#2006) |
| TC-P05 | Table has a Held pre-order; staff try Complete table | Button hidden; a crafted POST is refused | Not run yet |

**Ch.7 reflection:** (1) Adding a status looked like a schema change, but `OrderStatus` is stored as a string
(`HasConversion<string>()`), so `Held` needed no migration. Checking the DbContext first saved asking the group for one.
(2) The staff board only loads orders *placed* today, so a pre-order placed yesterday for tonight would never have
reached the Floor or The Pass. The day filter now also takes Held orders whose booking is today. (3) On release,
`CreatedAt` is reset: otherwise a pre-order placed at lunch would show "waiting 300m" on The Pass the moment it was
sent. Trade-off: the original placing time is not kept. (4) The early-arrival window (15 min) and the notice window
(5 min) interact: an order placed within 15 min of the booking counts as "at the table", so it is never held.
(5) Scope was cut on purpose: blocking orders with no booking would stop walk-ins ordering at all until table QR
exists, so that rule waits for QR. **AI use:** Claude Code explored the code, wrote the change and ran the HTTP tests;
Jordan set the ordering rule and the three arrival scenarios and reviewed the code.

### 2026-11-01 — Staff side: Floor, The Pass, Today (`feature/UI-staff-floor`, BUG8)

**Ch.5 screenshots**
1. Floor on iPad landscape with a drawer open on a Bill due table (Edge device mode, iPad Air landscape).
2. Floor at 390 px with the bottom sheet open.
3. The Pass rail with tickets in several columns and an allergy strip.
4. Today with the NOW line and the Needs-you list.
5. The Needs-you bar with plum, jade and amber chips.

**Ch.6 test cases**

| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-F02 | One table in each state (book within the hour; book and order; serve without paying) | Free / Booked / In use / Bill due tiles with the right colour and subtitle | |
| TC-F03 | Pending booking → tap table → Confirm booking | Green banner, amber dot and "not confirmed" chip gone | |
| TC-F05 | Bill due → Take payment → pay cash → back → Complete table | "Paid · ready to complete", then Free; order Completed | |
| TC-F08 | Crafted POST `?handler=Complete` on a Bill due table | Red banner "Order #… is not finished or not paid yet…"; table unchanged | |
| TC-O01 | Customer with a booking for now places an order | Banner "placed for your table T…"; ticket on The Pass shows the table | |

**Ch.7 reflection notes**
- *Researched before designing.* Malaysian F&B POS apps open front of house on a table layout and the kitchen on an
  order queue, and keep money from staff. That answered D5 and set the staff landing page.
- *3-tier across three domains.* `FloorService` combines Carynn's, Wallace's and my services and never touches the
  database, so each domain's rules stay in one place.
- *Testing found a gap the design assumed away.* `Order.ReservationId` existed but nothing ever set it, so no order
  had a table. Fixed at checkout by matching the customer's current booking.
- *Paying does not move an order on.* A paid order stays Served, so "Complete table" also completes paid orders,
  and The Pass keeps Served orders until they are Completed.
- *Time zones.* I assumed `CreatedAt` was UTC because the seed uses `UtcNow`; the real code saves local time. Every
  timer would have read about 480 minutes. Checked the code that writes the value, not just the seed.
- *Honest timers.* There is no cooking or served time on `Order`, so timers say "placed N min ago".

### 2026-11-01 — Back office redesign (`feature/UI-back-office`)

**What and why:** admins and staff were lost: they landed on the customer home page, the navbar mixed customer and
staff links (BUG1), and every admin page hung off one flat hub of 11 cards. Built a shared back office for Admin and
Staff (side menu grouped Insights / Operations / Restaurant / Marketing / People, filtered by role), then refined the
dashboard from a user's point of view. Page contents of other members were not changed, only framed.

**Ch.4 design evidence:** the four style options compared before choosing (Peranakan Tile, Kopitiam, Batik Indigo,
Pasar Malam), with the reason: Peranakan Tile continues the customer site's palette, so the system feels like one
product. Spec: `docs/superpowers/specs/2026-11-01-back-office-design.md`.

**Ch.5 screenshots:** back office as admin (full menu) and as staff (Operations only); the same page at phone width
with the slide-in menu open; customer navbar vs staff navbar ("Back office" link); dashboard on a month from the
month picker (sales line chart); Dish sales page showing dishes with 0 sales greyed out; Monitoring opened from
"View all sales" with the same dates. Code excerpts: `_BackOfficeNav` role filter and prefix-matched active link;
`_QuickRanges` single-highlight rule; `ReportService.GetDishSalesAsync` (menu left-joined to sales).

**Ch.6 test cases**
| ID | Steps | Expected |
|---|---|---|
| TC-B01 | Log in as admin with no return page | Lands on Dashboard; all 17 admin/staff pages open with the right menu item highlighted |
| TC-B02 | Log in as staff; open `/Admin/Dashboard` by URL | Lands on Orders board; menu shows Operations only; admin page gives Access denied |
| TC-B03 | Log in with `?ReturnUrl=/Admin/Menu` | Goes to Menu, not Dashboard |
| TC-B04 | Customer opens `/Admin`; checks navbar | Access denied; navbar unchanged (Book a table, My orders) |
| TC-B05 | Change From, To or Group by on the dashboard | Report reloads without pressing a button |
| TC-B06 | Pick "October 2026" in the month picker | Dates become 01/10–31/10; only the picker is highlighted |
| TC-B07 | On the 1st, pick Today | Only "Today" is highlighted, not the current month too |
| TC-B08 | Monitoring for 01/01–01/11 (10 months); then a 2-year range | Loads; 2 years shows "366 days or fewer" |
| TC-B09 | Open Dish sales for this year | Every dish listed, best seller first, unsold dishes at 0 |
| TC-B10 | Dashboard at 390 px wide | ☰ opens the menu, no sideways scrolling, dish names shortened with "…" |

**Ch.7 reflection:** (1) The 62-day limit on Monitoring looked like a business rule but was a workaround: reservations
were loaded one day at a time, and the original comment said to switch once a range query existed. Wallace had since
added one, so reading the code history turned a "limit" into a one-line query change for two pages. (2) Reviewing
screenshots, not just HTTP status codes, found bugs tests didn't: links were Bootstrap blue because Bootstrap 5.3 reads
`--bs-link-color-rgb` and the site only set `--bs-link-color` (the customer site has the same bug); the menu icon was
invisible outside a `.navbar`. (3) Dish sales counts orders placed (paid or not) while revenue counts paid payments,
so the page says so; otherwise the two totals look like a bug. (4) "Today" and "This month" were both highlighted on
the 1st because both matched the same dates; the fix was a rule that only one option can be active.
Shared files (`_Layout.cshtml`) and other members' files (Login, Monitoring, Customer activity) went through the PR for
group OK. **AI use:** Claude Code was used to explore the codebase, draft the design options, write and test the code
and these notes; every change was reviewed and accepted by Jordan.

### 2026-10-31 — PB20 order and payment notifications (`feature/PB20-order-payment-notifications`)

**Ch.5 screenshots:** `/Notifications` as the customer showing Order and Payment badges together; the navbar unread badge
after a staff status change.

**Test cases (Ch.6):**
| ID | Steps | Expected |
|---|---|---|
| TC-N01 | Staff moves an order from Preparing to Ready to serve; log in as its customer, open `/Notifications` | "Your order #N is now ready to serve." on the customer's list; nothing on the staff account's list |
| TC-N02 | Customer pays with test card 4000 0000 0000 0002 | "Payment for order #N was declined: Card declined by issuer."; the order can still be paid |
| TC-N03 | Pay an order, then try to cancel it from the staff board | Cancel refused, no "cancelled" notification |

**Ch.7:** services calling another domain's service through its interface (C uses B's `INotificationService`, never the
Notifications table). Notifications are sent after the save or commit, so a customer is never told about something that
was rolled back. Staff actions must look up the order's `CustomerId` rather than use the logged-in user.

### 2026-10-30 — BUG6 + UX1 (`fix/BUG6-UX1-orders-nav-cart`)

**Ch.5 screenshots:** the "Add to cart" dialog with the quantity stepper and running total; the dark toast
"Added … to your cart." floating above the "View cart" bar; the same at phone width; the receipt's
"Back to my orders" button; the Confirmation page's "My orders" button.

**Test cases (Ch.6):**
| ID | Steps | Expected |
|---|---|---|
| TC-UX1-01 | Scroll halfway down `/Menu`, tap Add, pick 3, confirm | No reload, same scroll position, toast shows, cart bar reads 3 |
| TC-UX1-02 | Tap Add, then Cancel | Nothing added, cart count unchanged |
| TC-UX1-03 | In the dialog, press + past 20 | + stops at 20 (cart limit), no wrap-around; − stops at 1 |
| TC-UX1-04 | Disable JavaScript, tap Add | Form posts normally, returns to the same tab with the banner (fallback) |
| TC-BUG6-01 | My orders → View receipt → Back to my orders | Lands on `/Orders` |

**Ch.7:** progressive enhancement — the same form works with and without JavaScript; the handler only answers
JSON when the request says `X-Requested-With: fetch`. The fetch posts the dish's own form, so the anti-forgery
token is sent and a request without it is refused (400). The toast lives on the menu page only, so the shared
`_Banners` / `_Layout.cshtml` did not need changing.

### 2026-10-30 — PB22 admin dashboard (`feature/PB22-dashboard`)

**Ch.5 screenshots:** full dashboard on "Last 30 days" (tiles + 3 charts); hover tooltip on a sales bar; Group by
Week vs Day for the same range; "Show as table" opened under a chart; the Dashboard card on the Configurations hub;
the dashboard at phone width; code excerpt of `ReportService.GroupIntoPeriods` (zero-filled periods).

**Ch.6 test cases** (full list TC-D01–D08 in `docs/superpowers/specs/2026-10-30-dashboard-design.md`)
| ID | Steps | Expected |
|---|---|---|
| TC-D03/04 | Pay an order online, reload dashboard; staff refunds it, reload | Revenue rises by the amount, then drops back |
| TC-D06 | Set From after To; then a range over 366 days | Error message under the filter, no charts |

**Ch.7 reflection:** revenue is counted from *payments* (by payment date), not order totals, so unpaid and refunded
orders don't inflate sales; a refund flips the payment to Refunded, so no extra rule was needed. Reservations are
Wallace's table, so they are read through `IReservationService.GetReservationsAsync` (cross-domain rule), not
queried directly. The database adds up each day (`GROUP BY` date); C# then folds days into weeks/months and fills empty
periods with 0 so gaps show on the chart. Profit can't be shown: there is no cost price (schema change, D5). Problem
hit: CSS isolation didn't style the quick-range links built inside a `@foreach`, so they're scoped through their
wrapper with `::deep`. Chart.js 4.4.1 (MIT) is a third-party library and should be credited.

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
