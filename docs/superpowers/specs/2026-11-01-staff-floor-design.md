# Staff side: Floor, The Pass, Today — design

Date: 2026-11-01 · Owner: Jordan · Branch: `feature/UI-staff-floor` (off `feature/UI-back-office`)

## Problem
Staff land on the order board, a list of cards, with nothing that shows the restaurant as a whole.
Orders, reservations and bills are three separate pages, so staff have to put together "what does table 4
need?" in their heads. There is no overview (BUG8). The design should stand out from the usual Bootstrap
table layouts.

## Research (1 Nov)
Malaysian F&B systems never put staff on a stats dashboard first. StoreHub, FeedMe and Qashier open the
front of house on a **table layout** with live status (free, occupied, reserved, waiting for payment).
GrabMerchant and FeedMe KDS open the kitchen on an **order queue** (New / Preparing / Ready, with
prep timers). Sales figures stay in the owner's back office. This supports D5: money totals are admin only.

## Decisions (agreed with Jordan, 1 Nov)
- Staff land on **Floor**, which has a **Needs-you bar** at the top.
- **The Pass** (a ticket rail) replaces the Orders board view, at the same URL. **Today** is a read-only run sheet.
- Each table drawer has **one next-step button**. Changing a status happens in the drawer. Payment
  opens Wallace's existing Bill page, so the cash logic is not built a second time.
- Devices: **iPad first** (landscape is the main target, portrait must also work), phone second, no
  PC-specific layout (it must still not break on a PC).
- **Approach 1:** one new `FloorService` that combines data from the existing services. No schema change.
- Build order: Floor, then The Pass, then Today. **Today is cut first** if time runs out.
- Look: the existing "Peranakan Tile" back office (`backoffice.css`, scoped under `body.bo`).

## Staff menu (`_BackOfficeNav.cshtml`)
The single Operations group becomes four groups. Admin sees the same groups as well, plus the admin-only groups.

| Group | Items |
|---|---|
| Front of house | Floor `/Staff/Floor` · Today `/Staff/Today` · Reservations |
| Kitchen | The Pass `/Staff/Orders` |
| Counter | Billing |
| Guests | Reviews |

Landing after login (Carynn's `Login.cshtml.cs`): staff go to `/Staff/Floor` instead of `/Staff/Orders`. Admin
still goes to `/Admin/Dashboard`. An explicit `ReturnUrl` still takes priority.

## Table states (`FloorService`)
Only today's reservations count, and Cancelled and NoShow are ignored. Each active table gets exactly one state,
checked in this order (first match wins). "Now" means local time.

| # | State | Rule | Tile |
|---|---|---|---|
| 1 | BillDue | an order on one of today's reservations for this table is `Served` and not `Paid` | plum fill, "Bill due · RM x" |
| 2 | InUse | a `Confirmed` reservation with `StartTime ≤ now` that is either still in its slot (`now < EndTime`) or has at least one linked order (so a party that stays past its end time stays InUse until staff press Complete table), **or** any linked order that is `Pending`, `Preparing` or `ReadyToServe` | jade fill, order stage + "placed Nm ago" |
| 3 | Booked | a `Pending`/`Confirmed` reservation with `now < EndTime` and `StartTime ≤ now + BookedWindowMinutes` (60). This includes a Pending booking whose slot has already started | dashed jade, "7:30 PM · Tan · 4 pax", amber dot if Pending |
| 4 | Free | none of the above | plain, "N seats", plus "Next 9:00 PM" if a booking comes later today |

Inactive tables are hidden. When a table has several bookings today, the booking that covers now (or else the next
one) decides its state. The drawer lists the rest under "Later today".

### Next-step button (one per table)
| Situation | Button | Calls |
|---|---|---|
| Booked, reservation Pending | Confirm booking | `IReservationService.UpdateStatusByStaffAsync(id, Confirmed)` |
| InUse, an order is ReadyToServe | Mark served | `IOrderService.AdvanceStatusAsync(id, ReadyToServe)` |
| InUse, an order is Pending/Preparing | *(link)* View in The Pass | — |
| BillDue | Take payment → | link to `/Staff/Billing/Bill/{orderId}` |
| InUse, no order cooking or unpaid | Complete table | `FloorService.CompleteTableAsync`: refuses while an order is cooking or unpaid, moves paid `Served` orders to `Completed` (`AdvanceStatusAsync`), then `UpdateStatusByStaffAsync(id, Completed)` |
| Free | none | — |

### Needs-you items (sorted by priority, then oldest first)
| # | Trigger | Chip colour |
|---|---|---|
| 1 | order `Pending` for more than `OrderWaitMinutes` (10) since it was placed; adds an allergy tag if `Notes` is not empty | plum |
| 2 | a table in BillDue | plum |
| 3 | order `ReadyToServe` | jade |
| 4 | reservation `Pending` that starts within `UnconfirmedWindowMinutes` (120) | amber |
| 5 | reservation start passed by more than `LateMinutes` (15), no order, not completed | amber |

The Floor bar shows the first 5 items plus "+N more". Today shows every item. Tapping an item opens that table's
drawer on Floor. An order with no table opens The Pass.

**Limit:** `Order` stores only `CreatedAt`, so every timer is *time since the order was placed*, and the labels
say "placed Nm ago". `PlaceOrderAsync` saves `CreatedAt` in restaurant **local** time (`DateTime.Now`, per the
coding conventions), so timers compare it with `DateTime.Now`. Only the seed data uses UTC.

## Linking orders to tables (added 1 Nov, found while testing)
Nothing set `Order.ReservationId`, so every order was "No table". `OrderService.PlaceOrderAsync` now links the
order to the customer's Pending or Confirmed booking **today** whose slot covers now, from
`EarlyArrivalMinutes` (15) before the start until the end, read through `IReservationService.GetCustomerReservationsAsync`.
Without such a booking, the order stays "No table" (e.g. ordered from home). The checkout banner then reads
"Order #N has been placed for your table T4." No schema change; the column already existed.

The Floor's area tabs follow `DiningAreaId` (set-up order), not the name order `GetAreasAsync` uses, so the main
hall is the default tab.

## Floor page `/Staff/Floor`
- Dining-area tabs, one per area. The selected area is in `?area=`, and the open table is in `&table=`.
- Layout: iPad landscape (≥ 992 px) has the side menu showing, 4 tiles per row, and a fixed right-hand panel for the drawer.
  When no table is selected, the panel shows *Tonight at a glance*: counts per state, with no money. iPad portrait
  (768–991 px) has 3 per row and the drawer as `offcanvas-end`. Phone (< 768 px) has 2 per row, the drawer as
  `offcanvas-bottom`, and a Needs-you bar that scrolls sideways.
- Every 20 s, `staff-live.js` fetches `?handler=Grid`, a partial containing the bar and the grid, and swaps it in.
  An open drawer stays open. If the fetch fails, the last grid stays on screen and the footer reads "Couldn't
  refresh, last updated hh:mm".
- Buttons are normal POST forms that redirect back to the same area and table, with `_Banners` messages.
- Tap targets are at least 44 px, nothing depends on hover, and every coloured state also has a text label.
- Empty states: no dining areas shows "No tables set up yet, ask an admin". An area with no active tables shows
  "No active tables in X".

## The Pass `/Staff/Orders` (same URL, same page model)
- A segmented control switches between **Live rail** (the default) and **History**, which is the current card list
  with the day picker and the Completed / Cancelled / All filters, unchanged.
- The rail has 4 columns: New (`Pending`), Preparing, Ready (`ReadyToServe`), and Served. Paying does not change an
  order's status, so a Served order stays on the rail until it is Completed: unpaid shows "Take payment →", paid
  shows the existing "Complete" button. On a phone the columns become tabs.
- Ticket: "T4 · #58", or "No table · #59", plus an age chip (jade under 10 min, amber 10–20, plum over 20), an amber
  allergy strip when `Notes` is set, the lines, one big button (existing `OnPostAdvanceAsync` + `ActionLabel`),
  and Cancel as a small link to the existing Cancel page. The Served column shows "Take payment →".
- `StaffOrderViewModel` gets `TableNumber` (`string?`), filled in `OrderService` through Reservation → RestaurantTable.
- The rail refreshes every 20 s through `?handler=Rail`. This replaces the full-page reload every 30 s.

## Today `/Staff/Today` (read-only)
- A greeting with the first name, plus the date and time.
- Count tiles: bookings today, arriving in the next 2 h, active orders, bills due. **No money.**
- A timeline of today's reservations across all areas, by start time, with a NOW line and earlier bookings faded.
  A row links to Floor with that area and table selected.
- The full Needs-you list next to the timeline (tablet), or above it (phone).
- No buttons that change data.

## Architecture (3-tier)
- `IFloorService` / `FloorService` (new, Jordan) uses `IDiningAreaService.GetAreasAsync`,
  `IReservationService.GetStaffDailyReservationsAsync(today)`, and `IOrderService.GetOrdersForStaffAsync(today, All)`
  (which already carries `PaymentStatus`). It does not inject `ApplicationDbContext`.
- Methods: `GetFloorAsync(int? diningAreaId)`, `GetTodayAsync()`, `CompleteTableAsync(int reservationId)`. The
  Needs-you list is built inside both getters (private `BuildNeedsYou`).
- `FloorService` awaits the three services one after another, never with `Task.WhenAll`, because they share one
  scoped DbContext.
- The state rules live in one private `ResolveState(...)`, with the threshold constants at the top of the class.
- No migration and no schema change.

## Files
| | File |
|---|---|
| new | `Services/IFloorService.cs`, `Services/FloorService.cs` |
| new | `ViewModels/TableState.cs`, `TableAction.cs`, `TableStateDisplay.cs`, `OrderAgeDisplay.cs`, `FloorViewModel.cs`, `FloorTableViewModel.cs`, `NeedsYouItemViewModel.cs`, `StaffTodayViewModel.cs` |
| new | `Pages/Staff/Floor/Index.cshtml(.cs)`, `Pages/Staff/Floor/_FloorGrid.cshtml` |
| new | `Pages/Staff/Today/Index.cshtml(.cs)` |
| new | `Pages/Staff/Orders/_PassRail.cshtml`, `wwwroot/js/staff-live.js` |
| change | `Pages/Staff/Orders/Index.cshtml(.cs)`, `ViewModels/StaffOrderViewModel.cs`, `Services/OrderService.cs` (table number + order-to-booking link), `wwwroot/css/backoffice.css` |
| shared | `Pages/Shared/_BackOfficeNav.cshtml`, `Program.cs` (1 `AddScoped` line), `Login.cshtml.cs` (Carynn, 1 line) |

## Group OK needed in the PR
`Program.cs`, Carynn's `Login.cshtml.cs`, and Floor calling Wallace's `UpdateStatusByStaffAsync` and linking to his
Bill page (none of his files change).

## Out of scope
Walk-in orders (every order needs a customer account), a drawn floor map (needs positions, a schema change),
live counters in the menu, money totals for staff, cooking or served timestamps (schema change), and
automated tests (would need a new test project and NuGet packages).

## Manual tests
| ID | Steps | Expected |
|---|---|---|
| TC-F01 | Staff logs in with no ReturnUrl | Lands on Floor, with "Floor" active under Front of house |
| TC-F02 | Prepare one table in each state | Each tile shows the right colour and subtitle |
| TC-F03 | Pending booking, tap the table, Confirm booking | Success banner, dot gone, "not confirmed" chip gone |
| TC-F04 | Ready order, Mark served on Floor | Table becomes Bill due, and the ticket is in Served on The Pass |
| TC-F05 | Bill due, Take payment, pay cash, back to Floor, Complete table | Table shows "Paid · ready to complete", then becomes Free; the order is Completed on The Pass |
| TC-F06 | Place an order with Floor open, wait 20 s | New chip appears, open drawer stays open |
| TC-F07 | iPad landscape / portrait / 390 px | 4 / 3 / 2 per row, panel / right drawer / bottom sheet, no sideways scroll |
| TC-P01 | Order with notes, placed 25 min ago | Allergy strip, plum age chip, one big button |
| TC-T01 | Today at 7 PM with bookings at 6 PM and 8 PM | NOW line between them, 6 PM faded, no money shown |
