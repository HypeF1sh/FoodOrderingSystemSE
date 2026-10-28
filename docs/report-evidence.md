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
| 2026-10-22 | PB16 payment + receipt v1 (Sprint 3) | See the detailed entry below — manual run of TC-P01–P13 still to do |

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
| | | |

## Carynn — Domain A

| Date | Item | Evidence / note |
|---|---|---|
| | | |
