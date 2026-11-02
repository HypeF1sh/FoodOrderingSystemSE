# Handoff: Jordan, picking up on another PC

Last updated: **2026-11-02**. Written so the next session (on the home desktop, with or without Claude) can carry on
without the chat history. The detailed history is in `docs/progress.md`; this file is the short "where am I".

## 1. Where the work stands

| Branch | State | Next step |
|---|---|---|
| `feature/UI-back-office` | **PR #56 merged** into `dev` (1 Nov, by speedup9) | Nothing |
| `feature/UI-staff-floor` | Pushed. **PR #57 open → `dev`** (8 commits) | Get the group's OK on the shared files (below), then merge |
| `fix/UX4-configurations-label` | 1 old commit, never merged | **Do not PR.** Superseded by the back office ("Back office" link replaced the "Admin" link). Safe to delete |
| `backup/*-before-trailer-removal` | Local only, on the laptop | Safety copies. Never push |

**PR #57 needs group OK for:** `Program.cs` (1 `AddScoped` line), `Pages/Shared/_Layout.cshtml` (staff "Back office" link →
Floor), Carynn's `Login.cshtml.cs` (staff land on `/Staff/Floor`), Floor calling Wallace's `UpdateStatusByStaffAsync` and
linking to his Bill page, and `OrderService` reading Wallace's `GetCustomerReservationsAsync`.

### What PR #57 contains (staff side redesign, BUG8)
- **Floor** `/Staff/Floor`: staff landing page. One tile per table (Free / Booked / In use / Bill due), a Needs-you bar, and
  a drawer with one next-step button (Confirm booking, Mark served, Take payment → Bill page, Complete table).
- **The Pass** `/Staff/Orders`: ticket rail New → Preparing → Ready → Served; old board under **History**.
- **Today** `/Staff/Today`: read-only run sheet (counts, no money).
- Staff menu regrouped: Front of house / Kitchen / Counter / Guests.
- **Orders are now linked to a table** at checkout (`OrderService.PlaceOrderAsync` → the customer's booking that covers now,
  15 min early allowed). Before this, `Order.ReservationId` was never set.
- No schema change. Spec: `docs/superpowers/specs/2026-11-01-staff-floor-design.md`.
  Plan: `docs/superpowers/plans/2026-11-01-staff-floor.md`.
- Verified: build 0 warnings; end-to-end over HTTP (book → confirm → order → serve → bill due → crafted Complete refused →
  pay → complete → free); Jordan's manual browser check on 1 Nov ("all looks good").

## 2. What I was in the middle of: customer side (not started in code)

Brainstorming only. **No code, no branch yet.** Ideas from Jordan (1–2 Nov):
- Customer side should be **phone-first**: customers use the system from their phone.
- **Table QR ordering**: scan the QR on the table → order goes straight to that table.
- Guests / people not at the restaurant should not be able to send orders.
- Extra features come **after** the customer panel is done.

Proposed split into three sub-projects, built in this order (agreed in principle, not yet approved in detail):
1. **Phone-first customer shell**: app-style bottom tab bar (Home · Menu · Orders · Bookings · Account), compact header,
   sticky cart bar. Biggest visible change for the least code; restyles every page (incl. Wallace's and Carynn's)
   without touching their markup. `_Layout.cshtml` is shared → group OK.
2. **Table QR ordering**: a QR per table opens the menu with that table attached; builds on the order-to-table link in PR #57.
3. **Phone polish of Jordan's own pages** (Menu, Cart, Checkout, Pay, My orders) if time allows.

**Open question to answer first (it shapes the QR design): who may place an order?**
- **A.** Logged-in customers anywhere (as now); QR only adds the table. From home = "No table" (takeaway).
- **B. (recommended)** Logged-in customers **only at the restaurant**: sending an order needs a table, from a QR scan or a
  booking that is on now. From home the menu is browse-only ("Scan the QR on your table to order").
- **C.** Anyone who scans the QR, including guests without an account. Much bigger: orders, payments and receipts are tied
  to a customer account today, and it would touch Carynn's Identity setup.

Note: guests already cannot order today (`PlaceOrderAsync` refuses an empty customer id).

**To resume with Claude:** say "continue the customer side brainstorm from `docs/handoff-jordan.md`, the answer to the
ordering question is A/B/C". The flow is: brainstorm → spec in `docs/superpowers/specs/` → plan → code, as for the staff side.

## 3. Setting up the home desktop

1. `git clone https://github.com/HypeF1sh/FoodOrderingSystemSE` (or `git fetch` if it is already cloned).
2. `git checkout feature/UI-staff-floor` (until PR #57 is merged; after that, `git checkout dev && git pull`).
3. Open `FoodOrderingSystem.slnx` in Visual Studio 2026.
4. Database: Tools → NuGet Package Manager → Package Manager Console → `Update-Database`. The seeder creates the dev
   accounts on first run: `admin@rroms.com` / `Admin@123`, `staff@rroms.com` / `Staff@123`, `customer@rroms.com` / `Customer@123`.
   The home database will **not** have the laptop's test bookings and orders, so Floor shows every table as Free until you
   book (as the customer) and order.
5. F5. App runs at `http://localhost:5068`.

**Not in git, copy by hand if you want them on the home desktop:**
- `CLAUDE.local.md` (personal Claude instructions, gitignored): concise answers, viva summaries, exact Visual Studio clicks,
  "Jordan commits and pushes himself", report evidence after each item.
- Claude's memory lives in `~/.claude/` on each PC and does not travel with the repo.
- `.superpowers/` (brainstorm mockups) is gitignored and stays on the laptop.

## 4. Deadlines
- Internal target **4 Nov**, formal deadline **Fri 6 Nov 2026** (late = zero). Freeze from 3–5 Nov is for testing, fixes
  and the report, so keep the customer-side work small and in the order above.
