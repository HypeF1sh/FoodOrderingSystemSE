# Staff Floor, The Pass and Today: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give staff a table-first landing page (Floor + Needs-you bar), a ticket-rail kitchen view (The Pass), and a read-only shift run sheet (Today), all in the existing back-office look.

**Architecture:** A new `FloorService` (business layer) combines data from `IDiningAreaService`, `IReservationService` and `IOrderService`, and works out each table's state and the Needs-you items. It never touches the DbContext. The Razor pages under `Pages/Staff` call services only. The live parts of the page are Razor partials, re-fetched every 20 s by one small vanilla-JS file.

**Tech Stack:** ASP.NET Core Razor Pages (.NET 10), EF Core (unchanged), Bootstrap 5.3 (grid + offcanvas, already in `wwwroot/lib`), vanilla JS.

**Spec:** `docs/superpowers/specs/2026-11-01-staff-floor-design.md`

---

## Ground rules for whoever runs this plan

- **Jordan commits and pushes himself** (`CLAUDE.local.md`). Every "Commit" step means: *stop, list the files to stage
  and the suggested message, and let Jordan commit.* Never run `git commit` or `git push`.
- **There is no test project** in the solution, and adding one needs a NuGet package plus group OK. Verification in
  each task is therefore `dotnet build` (0 errors, **no new warnings**) followed by an exact manual check in the browser.
  The manual test IDs (TC-…) come from the spec.
- Run everything from the repo root: `C:\Software Engineering\HypeF1sh\FoodOrderingSystemSE`.
- Build: `dotnet build FoodOrderingSystem.slnx`. Expected last lines: `Build succeeded.` and `0 Warning(s)`. If
  warnings already existed before your change, the count must not go up.
- Run: `dotnet run --project FoodOrderingSystem --launch-profile http`, then open `http://localhost:5068`.
  Staff login is `staff@rroms.com` / `Staff@123`. Admin login is `admin@rroms.com` / `Admin@123`.
- Never add a `using` for `ApplicationDbContext` to a service or page in this plan.
- One public type per file. File name = type name.

## File map

| File | Responsibility |
|---|---|
| `ViewModels/TableState.cs` | enum: Free, Booked, InUse, BillDue |
| `ViewModels/TableAction.cs` | enum: the one next-step button in a table drawer |
| `ViewModels/TableStateDisplay.cs` | labels and CSS names for `TableState` (like `OrderStatusDisplay`) |
| `ViewModels/OrderAgeDisplay.cs` | "placed N min ago" and its colour, shared by Floor and The Pass |
| `ViewModels/NeedsYouItemViewModel.cs` | one Needs-you chip or row |
| `ViewModels/FloorTableViewModel.cs` | one table tile plus its drawer contents |
| `ViewModels/FloorViewModel.cs` | the whole Floor page |
| `ViewModels/StaffTodayViewModel.cs` | the whole Today page |
| `Services/IFloorService.cs`, `Services/FloorService.cs` | table-state rules, Needs-you rules, Complete table |
| `Pages/Staff/Floor/Index.cshtml(.cs)` | Floor page: area tabs, drawer, POST handlers, `?handler=Grid` |
| `Pages/Staff/Floor/_FloorGrid.cshtml` | live part of Floor: Needs-you bar + tile grid + footer |
| `Pages/Staff/Orders/_PassRail.cshtml` | live part of The Pass: four columns of tickets |
| `Pages/Staff/Orders/Index.cshtml(.cs)` | The Pass: rail (default) or History |
| `Pages/Staff/Today/Index.cshtml(.cs)` | Today run sheet |
| `wwwroot/js/staff-live.js` | 20 s partial refresh for any element with `data-live-url` |
| `wwwroot/css/backoffice.css` | new sections 9 (Floor), 10 (The Pass), 11 (Today) |
| `ViewModels/StaffOrderViewModel.cs`, `Services/OrderService.cs` | add `TableNumber` |
| `Pages/Shared/_BackOfficeNav.cshtml` | new staff menu groups |
| `Program.cs` | register `IFloorService` (shared file, group OK) |
| `Areas/Identity/Pages/Account/Login.cshtml.cs` | staff land on Floor (Carynn's file, group OK) |

All paths below are relative to `FoodOrderingSystem/` unless they start with `docs/` or `.gitignore`.

---

### Task 0: Branch and housekeeping

**Files:**
- Modify: `.gitignore` (repo root, append at the end)

- [ ] **Step 1: Create the branch off the back-office branch**

```bash
git checkout feature/UI-back-office
git checkout -b feature/UI-staff-floor
```
Expected: `Switched to a new branch 'feature/UI-staff-floor'`

- [ ] **Step 2: Keep brainstorm mockups out of git.** Append to `.gitignore`:

```gitignore

## ---------- Brainstorming companion mockups (local only) ----------
.superpowers/
```

- [ ] **Step 3: Verify**

Run: `git status --short`
Expected: only ` M .gitignore`, plus the two new docs (`docs/superpowers/specs/2026-11-01-staff-floor-design.md` and
`docs/superpowers/plans/2026-11-01-staff-floor.md`). `.superpowers/` is no longer listed.

- [ ] **Step 4: Commit (Jordan)**

Stage: `.gitignore`, `docs/superpowers/specs/2026-11-01-staff-floor-design.md`, `docs/superpowers/plans/2026-11-01-staff-floor.md`
Message: `docs(UI): add staff floor design spec and plan`

---

### Task 1: Table number on staff orders

The Pass and the Needs-you bar show "T4 · #58", but `StaffOrderViewModel` only has `ReservationId`. Fill the table
number in the existing projection, through Order → Reservation → RestaurantTable. No schema change.

**Files:**
- Modify: `ViewModels/StaffOrderViewModel.cs` (after the `ReservationId` line)
- Modify: `Services/OrderService.cs`, the `ProjectToStaffView` method (around line 315)

- [ ] **Step 1: Add the property.** In `ViewModels/StaffOrderViewModel.cs`, directly under `public int? ReservationId { get; set; }`, add:

```csharp
    // Table of the order's reservation, e.g. "T4"; null when the order has no reservation ("No table" on The Pass)
    public string? TableNumber { get; set; }
```

- [ ] **Step 2: Fill it in the projection.** In `Services/OrderService.cs`, inside `ProjectToStaffView`, directly under `ReservationId = o.ReservationId,`, add:

```csharp
            TableNumber = o.Reservation != null && o.Reservation.RestaurantTable != null
                ? o.Reservation.RestaurantTable.TableNumber
                : null,
```

- [ ] **Step 3: Build**

Run: `dotnet build FoodOrderingSystem.slnx`
Expected: `Build succeeded.`, with no new warnings.

- [ ] **Step 4: Check that nothing else broke.** Run the app, log in as staff, and open `/Staff/Orders`. The board looks exactly as before (the new field isn't shown yet).

- [ ] **Step 5: Commit (Jordan)**

Stage: `FoodOrderingSystem/ViewModels/StaffOrderViewModel.cs`, `FoodOrderingSystem/Services/OrderService.cs`
Message: `feat(UI): add table number to staff order view`

---

### Task 2: View models and display helpers

**Files:**
- Create: `ViewModels/TableState.cs`, `ViewModels/TableAction.cs`, `ViewModels/TableStateDisplay.cs`,
  `ViewModels/OrderAgeDisplay.cs`, `ViewModels/NeedsYouItemViewModel.cs`, `ViewModels/FloorTableViewModel.cs`,
  `ViewModels/FloorViewModel.cs`, `ViewModels/StaffTodayViewModel.cs`

- [ ] **Step 1: `ViewModels/TableState.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// The one state a table is in on the staff Floor. FloorService checks them from BillDue down to Free
// and the first rule that matches wins (spec: docs/superpowers/specs/2026-11-01-staff-floor-design.md).
public enum TableState
{
    Free,
    Booked,
    InUse,
    BillDue
}
```

- [ ] **Step 2: `ViewModels/TableAction.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// The single next-step button in a table's drawer on the Floor
public enum TableAction
{
    None,
    ConfirmBooking,   // POST: reservation Pending -> Confirmed (Wallace's service)
    MarkServed,       // POST: order ReadyToServe -> Served
    ViewInPass,       // link only: Pending and Preparing belong to the kitchen
    TakePayment,      // link to Wallace's Bill page, so cash handling is not built twice
    CompleteTable     // POST: FloorService.CompleteTableAsync
}
```

- [ ] **Step 3: `ViewModels/TableStateDisplay.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// Words and CSS names for each table state, kept in one place like OrderStatusDisplay
public static class TableStateDisplay
{
    public static readonly TableState[] InLegendOrder =
        { TableState.Free, TableState.Booked, TableState.InUse, TableState.BillDue };

    public static string Label(TableState state) => state switch
    {
        TableState.Booked => "Booked",
        TableState.InUse => "In use",
        TableState.BillDue => "Bill due",
        _ => "Free"
    };

    // Used as the CSS class "is-<name>" in backoffice.css section 9
    public static string CssName(TableState state) => state switch
    {
        TableState.Booked => "booked",
        TableState.InUse => "inuse",
        TableState.BillDue => "billdue",
        _ => "free"
    };
}
```

- [ ] **Step 4: `ViewModels/OrderAgeDisplay.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// How long ago an order was placed, and its colour on The Pass.
// Order.CreatedAt is saved with DateTime.UtcNow, so it is compared with UtcNow, never with local time.
// There is no "cooking started" or "served at" time on Order, so this is always time since it was placed.
public static class OrderAgeDisplay
{
    public const int AmberFromMinutes = 10;
    public const int PlumFromMinutes = 20;

    public static int MinutesSincePlaced(DateTime createdAtUtc) =>
        Math.Max(0, (int)(DateTime.UtcNow - createdAtUtc).TotalMinutes);

    public static string ToneClass(int minutes) =>
        minutes >= PlumFromMinutes ? "is-plum"
        : minutes >= AmberFromMinutes ? "is-amber"
        : "is-jade";
}
```

- [ ] **Step 5: `ViewModels/NeedsYouItemViewModel.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// One chip on the Floor's Needs-you bar, or one row in Today's Needs-you list
public class NeedsYouItemViewModel
{
    public int Priority { get; set; }            // 1 = most urgent, as in the spec's table
    public string Text { get; set; } = string.Empty;
    public string Tone { get; set; } = "plum";   // plum, jade or amber -> CSS class "is-<tone>"
    public bool HasAllergyNote { get; set; }
    public DateTime SortTime { get; set; }       // oldest first within the same priority

    // Where tapping it goes: the table on the Floor, or The Pass when the order has no table
    public int? DiningAreaId { get; set; }
    public int? RestaurantTableId { get; set; }
    public int? OrderId { get; set; }
}
```

- [ ] **Step 6: `ViewModels/FloorTableViewModel.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// One table tile on the staff Floor, plus what its drawer shows. Filled by FloorService.
public class FloorTableViewModel
{
    public int RestaurantTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int DiningAreaId { get; set; }

    public TableState State { get; set; } = TableState.Free;
    public string Subtitle { get; set; } = string.Empty;
    public bool IsPendingBooking { get; set; }   // amber dot on a Booked tile

    // The booking that decides the state; all null when the table is free
    public int? ReservationId { get; set; }
    public string? CustomerName { get; set; }
    public int? PartySize { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    public List<StaffOrderViewModel> Orders { get; set; } = new();   // today's orders on this table, not yet Completed
    public decimal UnpaidTotal { get; set; }
    public List<string> LaterToday { get; set; } = new();            // other bookings for this table later today

    // The one next-step button in the drawer
    public TableAction Action { get; set; } = TableAction.None;
    public int? ActionOrderId { get; set; }
}
```

- [ ] **Step 7: `ViewModels/FloorViewModel.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// Everything the staff Floor page shows for one dining area
public class FloorViewModel
{
    // How many chips fit on the Needs-you bar; the rest are counted in "+N more"
    public const int BarLimit = 5;

    public List<DiningAreaOptionViewModel> Areas { get; set; } = new();
    public int? SelectedAreaId { get; set; }
    public string SelectedAreaName { get; set; } = string.Empty;
    public List<FloorTableViewModel> Tables { get; set; } = new();
    public List<NeedsYouItemViewModel> NeedsYou { get; set; } = new();   // every area, most urgent first
    public DateTime UpdatedAt { get; set; }

    // Set by the page from ?table=, so the grid can outline the open table and the drawer can show it
    public int? SelectedTableId { get; set; }
    public FloorTableViewModel? SelectedTable => Tables.FirstOrDefault(t => t.RestaurantTableId == SelectedTableId);
}
```
(`DiningAreaOptionViewModel` already exists in `ViewModels/ReservationViewModels.cs`. It's reused, not copied.)

- [ ] **Step 8: `ViewModels/StaffTodayViewModel.cs`**

```csharp
namespace FoodOrderingSystem.ViewModels;

// The staff Today page: counts (never money), today's bookings in time order, and the full Needs-you list
public class StaffTodayViewModel
{
    public DateTime Now { get; set; }

    public int BookingsToday { get; set; }
    public int ArrivingSoon { get; set; }
    public int ActiveOrders { get; set; }
    public int BillsDue { get; set; }

    public List<ReservationDetailViewModel> Timeline { get; set; } = new();
    public List<NeedsYouItemViewModel> NeedsYou { get; set; } = new();
}
```

- [ ] **Step 9: Build**

Run: `dotnet build FoodOrderingSystem.slnx`
Expected: `Build succeeded.`, no new warnings.

- [ ] **Step 10: Commit (Jordan)**

Stage: the 8 new files in `FoodOrderingSystem/ViewModels/`
Message: `feat(UI): add floor, needs-you and today view models`

---

### Task 3: FloorService

**Files:**
- Create: `Services/IFloorService.cs`, `Services/FloorService.cs`
- Modify: `Program.cs`, directly after the `IReportService` line (around line 78). **Shared file → group OK in the PR.**

- [ ] **Step 1: `Services/IFloorService.cs`**

```csharp
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Staff Floor, Needs-you and Today (spec: docs/superpowers/specs/2026-11-01-staff-floor-design.md)
public interface IFloorService
{
    // Tiles for one dining area (the first area when diningAreaId is null or unknown) plus the Needs-you list
    Task<FloorViewModel> GetFloorAsync(int? diningAreaId);

    Task<StaffTodayViewModel> GetTodayAsync();

    // Refuses while an order is still cooking or unpaid; otherwise completes paid, served orders, then the booking
    Task<(bool Success, string Message)> CompleteTableAsync(int reservationId);
}
```

- [ ] **Step 2: `Services/FloorService.cs`**

```csharp
using FoodOrderingSystem.Models;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.ViewModels;

namespace FoodOrderingSystem.Services;

// Works out what every table is doing right now, and what needs a staff member's attention.
// It only combines what other services return, so it never touches the DbContext:
// tables from IDiningAreaService (Carynn), bookings from IReservationService (Wallace),
// orders with their payment status from IOrderService (Jordan).
public class FloorService : IFloorService
{
    // Thresholds in minutes, kept together so they are easy to find and change
    public const int BookedWindowMinutes = 60;        // a table shows Booked this long before the booking starts
    public const int OrderWaitMinutes = 10;           // a Pending order older than this goes on Needs you
    public const int UnconfirmedWindowMinutes = 120;  // a Pending booking starting within this goes on Needs you
    public const int LateMinutes = 15;                // a booking this late with no order goes on Needs you
    public const int ArrivingWindowMinutes = 120;     // "arriving in the next 2 h" on Today

    // The kitchen is still working on these
    private static readonly OrderStatus[] KitchenStatuses =
        { OrderStatus.Pending, OrderStatus.Preparing, OrderStatus.ReadyToServe };

    private readonly IDiningAreaService _diningAreaService;
    private readonly IReservationService _reservationService;
    private readonly IOrderService _orderService;

    public FloorService(
        IDiningAreaService diningAreaService,
        IReservationService reservationService,
        IOrderService orderService)
    {
        _diningAreaService = diningAreaService;
        _reservationService = reservationService;
        _orderService = orderService;
    }

    public async Task<FloorViewModel> GetFloorAsync(int? diningAreaId)
    {
        var today = await LoadTodayAsync();

        // 1. The chosen area, or the first one when none (or an unknown one) was chosen
        var selected = today.Areas.FirstOrDefault(a => a.DiningAreaId == diningAreaId) ?? today.Areas.FirstOrDefault();

        var model = new FloorViewModel
        {
            Areas = today.Areas
                .Select(a => new DiningAreaOptionViewModel
                {
                    DiningAreaId = a.DiningAreaId,
                    Name = a.Name,
                    Description = a.Description
                })
                .ToList(),
            SelectedAreaId = selected?.DiningAreaId,
            SelectedAreaName = selected?.Name ?? string.Empty,
            NeedsYou = BuildNeedsYou(today),
            UpdatedAt = today.Now
        };

        // 2. One tile per active table in that area, in table-number order (T2 before T10)
        if (selected != null)
        {
            model.Tables = selected.RestaurantTables
                .Where(t => t.IsActive)
                .OrderBy(t => t.TableNumber.Length)
                .ThenBy(t => t.TableNumber)
                .Select(t => BuildTable(t, today))
                .ToList();
        }

        return model;
    }

    public async Task<StaffTodayViewModel> GetTodayAsync()
    {
        var today = await LoadTodayAsync();
        var arrivingBy = today.Now.AddMinutes(ArrivingWindowMinutes);

        return new StaffTodayViewModel
        {
            Now = today.Now,
            BookingsToday = today.Reservations.Count,
            ArrivingSoon = today.Reservations.Count(r =>
                IsOpenBooking(r) && Start(r) >= today.Now && Start(r) <= arrivingBy),
            ActiveOrders = today.Orders.Count(o => KitchenStatuses.Contains(o.Status)),
            BillsDue = today.Orders.Count(IsBillDue),
            Timeline = today.Reservations.OrderBy(r => r.StartTime).ToList(),
            NeedsYou = BuildNeedsYou(today)
        };
    }

    public async Task<(bool Success, string Message)> CompleteTableAsync(int reservationId)
    {
        var today = await LoadTodayAsync();
        var orders = today.Orders.Where(o => o.ReservationId == reservationId).ToList();

        // 1. A table can only be closed once the kitchen is done and everything served is paid for.
        //    The button is hidden in that case, but a crafted POST must still be refused here.
        var blocking = orders.FirstOrDefault(o => KitchenStatuses.Contains(o.Status) || IsBillDue(o));
        if (blocking != null)
        {
            return (false, $"Order #{blocking.OrderId} is not finished or not paid yet, so the table can't be completed.");
        }

        // 2. Paying does not change an order's status, so paid orders are still Served: complete them first
        foreach (var order in orders.Where(o => o.Status == OrderStatus.Served))
        {
            var (success, message) = await _orderService.AdvanceStatusAsync(order.OrderId, OrderStatus.Served);
            if (!success)
            {
                return (false, message);
            }
        }

        // 3. Then the booking itself (Wallace's service also sends the customer the "leave a rating" notification)
        return await _reservationService.UpdateStatusByStaffAsync(reservationId, ReservationStatus.Completed);
    }

    // ---------------------------------------------------------------------
    // Loading
    // ---------------------------------------------------------------------

    // Everything the floor needs for today, read once per request.
    // The three calls are awaited one after another on purpose: the services share one scoped DbContext,
    // and a DbContext cannot run two queries at the same time (so no Task.WhenAll here).
    private async Task<TodaySnapshot> LoadTodayAsync()
    {
        var now = DateTime.Now;
        var date = DateOnly.FromDateTime(now);

        var areas = await _diningAreaService.GetAreasAsync();

        var reservations = (await _reservationService.GetStaffDailyReservationsAsync(date))
            .Where(r => r.Status != nameof(ReservationStatus.Cancelled) && r.Status != nameof(ReservationStatus.NoShow))
            .ToList();

        var orders = (await _orderService.GetOrdersForStaffAsync(date, StaffOrderFilter.All))
            .Where(o => o.Status != OrderStatus.Cancelled)
            .ToList();

        return new TodaySnapshot(now, areas, reservations, orders);
    }

    private sealed record TodaySnapshot(
        DateTime Now,
        List<DiningArea> Areas,
        List<ReservationDetailViewModel> Reservations,
        List<StaffOrderViewModel> Orders);

    // ---------------------------------------------------------------------
    // Table state: the four rules from the spec
    // ---------------------------------------------------------------------

    private static FloorTableViewModel BuildTable(RestaurantTable table, TodaySnapshot today)
    {
        var bookings = today.Reservations.Where(r => r.RestaurantTableId == table.RestaurantTableId).ToList();
        var openBookings = bookings.Where(IsOpenBooking).ToList();
        var bookingIds = bookings.Select(r => r.ReservationId).ToHashSet();
        var orders = today.Orders
            .Where(o => o.ReservationId.HasValue && bookingIds.Contains(o.ReservationId.Value))
            .OrderBy(o => o.CreatedAt)
            .ToList();

        var tile = new FloorTableViewModel
        {
            RestaurantTableId = table.RestaurantTableId,
            TableNumber = table.TableNumber,
            Capacity = table.Capacity,
            DiningAreaId = table.DiningAreaId
        };

        ResolveState(tile, bookings, openBookings, orders, today.Now);

        tile.LaterToday = openBookings
            .Where(r => r.ReservationId != tile.ReservationId && Start(r) > today.Now)
            .Select(r => $"{r.StartTime:h:mm tt} · {r.CustomerName} · {r.PartySize} pax")
            .ToList();

        return tile;
    }

    // Checked in order; the first rule that matches decides the state, the subtitle and the drawer button
    private static void ResolveState(
        FloorTableViewModel tile,
        List<ReservationDetailViewModel> bookings,
        List<ReservationDetailViewModel> openBookings,
        List<StaffOrderViewModel> orders,
        DateTime now)
    {
        // 1. Bill due: something was served and nobody has paid for it yet
        var unpaid = orders.Where(IsBillDue).ToList();
        if (unpaid.Count > 0)
        {
            tile.State = TableState.BillDue;
            tile.UnpaidTotal = unpaid.Sum(o => o.TotalAmount);
            tile.Subtitle = $"Bill due · RM {tile.UnpaidTotal:0.00}";
            tile.Action = TableAction.TakePayment;
            tile.ActionOrderId = unpaid[0].OrderId;
            SetBooking(tile, FindBooking(bookings, unpaid[0]), orders);
            return;
        }

        // 2. In use: the kitchen is working on an order, or a confirmed party is at the table.
        //    A party past its end time stays In use while it has orders, until staff press Complete table.
        var cooking = orders.FirstOrDefault(o => KitchenStatuses.Contains(o.Status));
        var seated = openBookings.FirstOrDefault(r =>
            r.Status == nameof(ReservationStatus.Confirmed)
            && Start(r) <= now
            && (now < End(r) || orders.Any(o => o.ReservationId == r.ReservationId)));

        if (cooking != null || seated != null)
        {
            tile.State = TableState.InUse;
            SetBooking(tile, cooking != null ? FindBooking(bookings, cooking) : seated, orders);

            var ready = orders.FirstOrDefault(o => o.Status == OrderStatus.ReadyToServe);
            if (ready != null)
            {
                tile.Subtitle = $"#{ready.OrderId} ready to serve";
                tile.Action = TableAction.MarkServed;
                tile.ActionOrderId = ready.OrderId;
            }
            else if (cooking != null)
            {
                var minutes = OrderAgeDisplay.MinutesSincePlaced(cooking.CreatedAt);
                tile.Subtitle = $"{OrderStatusDisplay.Label(cooking.Status)} · placed {minutes}m ago";
                tile.Action = TableAction.ViewInPass;
                tile.ActionOrderId = cooking.OrderId;
            }
            else
            {
                // seated! is safe: this branch only runs when cooking is null, so seated must be the match
                tile.Subtitle = orders.Count > 0 ? "Paid · ready to complete" : $"Seated · {seated!.PartySize} pax";
                tile.Action = TableAction.CompleteTable;
            }
            return;
        }

        // 3. Booked: a party is due within the hour, or a Pending booking's slot has already started
        var soon = openBookings.FirstOrDefault(r =>
            now < End(r) && Start(r) <= now.AddMinutes(BookedWindowMinutes));
        if (soon != null)
        {
            tile.State = TableState.Booked;
            tile.Subtitle = $"{soon.StartTime:h:mm tt} · {soon.CustomerName} · {soon.PartySize} pax";
            tile.IsPendingBooking = soon.Status == nameof(ReservationStatus.Pending);
            tile.Action = tile.IsPendingBooking ? TableAction.ConfirmBooking : TableAction.None;
            SetBooking(tile, soon, orders);
            return;
        }

        // 4. Free, mentioning the next booking later today if there is one
        var next = openBookings.FirstOrDefault(r => Start(r) > now);
        tile.State = TableState.Free;
        tile.Subtitle = next == null ? $"{tile.Capacity} seats" : $"{tile.Capacity} seats · next {next.StartTime:h:mm tt}";
    }

    private static void SetBooking(
        FloorTableViewModel tile,
        ReservationDetailViewModel? booking,
        List<StaffOrderViewModel> orders)
    {
        tile.Orders = orders.Where(o => o.Status != OrderStatus.Completed).ToList();
        if (booking == null)
        {
            return;
        }

        tile.ReservationId = booking.ReservationId;
        tile.CustomerName = booking.CustomerName;
        tile.PartySize = booking.PartySize;
        tile.StartTime = booking.StartTime;
        tile.EndTime = booking.EndTime;
    }

    // ---------------------------------------------------------------------
    // Needs you: the five triggers from the spec, most urgent first
    // ---------------------------------------------------------------------

    private static List<NeedsYouItemViewModel> BuildNeedsYou(TodaySnapshot today)
    {
        var items = new List<NeedsYouItemViewModel>();

        // Orders: waiting too long (1), bill due (2), ready to serve (3)
        foreach (var order in today.Orders)
        {
            var booking = today.Reservations.FirstOrDefault(r => r.ReservationId == order.ReservationId);
            var where = booking?.TableNumber ?? "no table";
            var minutes = OrderAgeDisplay.MinutesSincePlaced(order.CreatedAt);

            if (order.Status == OrderStatus.Pending && minutes > OrderWaitMinutes)
            {
                var item = OrderItem(1, "plum", $"#{order.OrderId} waiting {minutes}m · {where}", order, booking);
                item.HasAllergyNote = !string.IsNullOrWhiteSpace(order.Notes);
                items.Add(item);
            }
            else if (IsBillDue(order))
            {
                var label = booking != null ? $"{booking.TableNumber} bill due · #{order.OrderId}" : $"#{order.OrderId} bill due";
                items.Add(OrderItem(2, "plum", label, order, booking));
            }
            else if (order.Status == OrderStatus.ReadyToServe)
            {
                items.Add(OrderItem(3, "jade", $"#{order.OrderId} ready · {where}", order, booking));
            }
        }

        // Bookings: late with no order (5), or still not confirmed and starting soon (4)
        foreach (var booking in today.Reservations.Where(IsOpenBooking))
        {
            var start = Start(booking);
            var who = $"{booking.CustomerName} {booking.StartTime:h:mm tt}";
            var hasOrder = today.Orders.Any(o => o.ReservationId == booking.ReservationId);

            if (!hasOrder && today.Now > start.AddMinutes(LateMinutes) && today.Now < End(booking))
            {
                items.Add(BookingItem(5, "amber", $"{who} late, no-show?", booking, start));
            }
            else if (booking.Status == nameof(ReservationStatus.Pending)
                     && start <= today.Now.AddMinutes(UnconfirmedWindowMinutes)
                     && today.Now < End(booking))
            {
                items.Add(BookingItem(4, "amber", $"{who} not confirmed", booking, start));
            }
        }

        return items.OrderBy(i => i.Priority).ThenBy(i => i.SortTime).ToList();
    }

    private static NeedsYouItemViewModel OrderItem(
        int priority, string tone, string text, StaffOrderViewModel order, ReservationDetailViewModel? booking) =>
        new()
        {
            Priority = priority,
            Tone = tone,
            Text = text,
            SortTime = order.CreatedAt,
            OrderId = order.OrderId,
            DiningAreaId = booking?.DiningAreaId,
            RestaurantTableId = booking?.RestaurantTableId
        };

    private static NeedsYouItemViewModel BookingItem(
        int priority, string tone, string text, ReservationDetailViewModel booking, DateTime start) =>
        new()
        {
            Priority = priority,
            Tone = tone,
            Text = text,
            SortTime = start,
            DiningAreaId = booking.DiningAreaId,
            RestaurantTableId = booking.RestaurantTableId
        };

    // ---------------------------------------------------------------------
    // Small helpers
    // ---------------------------------------------------------------------

    // Reservation times are local DateOnly + TimeOnly. Comparing as DateTime avoids TimeOnly wrapping at midnight.
    private static DateTime Start(ReservationDetailViewModel r) => r.ReservationDate.ToDateTime(r.StartTime);
    private static DateTime End(ReservationDetailViewModel r) => r.ReservationDate.ToDateTime(r.EndTime);

    // Pending or Confirmed (Cancelled and NoShow are already filtered out when loading)
    private static bool IsOpenBooking(ReservationDetailViewModel r) =>
        r.Status == nameof(ReservationStatus.Pending) || r.Status == nameof(ReservationStatus.Confirmed);

    private static bool IsBillDue(StaffOrderViewModel o) => o.Status == OrderStatus.Served && !o.IsPaid;

    private static ReservationDetailViewModel? FindBooking(
        List<ReservationDetailViewModel> bookings, StaffOrderViewModel order) =>
        bookings.FirstOrDefault(r => r.ReservationId == order.ReservationId);
}
```

- [ ] **Step 3: Register it.** In `Program.cs`, directly after `builder.Services.AddScoped<IReportService, ReportService>();`, add:

```csharp

// Staff Floor, Needs you and Today (spec 2026-11-01-staff-floor-design.md)
builder.Services.AddScoped<IFloorService, FloorService>();
```

- [ ] **Step 4: Build**

Run: `dotnet build FoodOrderingSystem.slnx`
Expected: `Build succeeded.`, no new warnings. (The one `seated!` has a comment saying why it is safe, as the
conventions require.)

- [ ] **Step 5: Commit (Jordan)**

Stage: `FoodOrderingSystem/Services/IFloorService.cs`, `FoodOrderingSystem/Services/FloorService.cs`, `FoodOrderingSystem/Program.cs`
Message: `feat(UI): add FloorService for table states and needs-you list`

---

### Task 4: Live refresh script

**Files:**
- Create: `wwwroot/js/staff-live.js`

- [ ] **Step 1: Write the file**

```javascript
// Staff Floor and The Pass: every 20 seconds, fetch just the live part of the page and swap it in.
// New orders and bookings appear without a full reload, so an open table drawer or the phone tabs stay as they are.
// Any element with data-live-url is refreshed; the server returns a Razor partial (e.g. ?handler=Grid).
(function () {
    const pollMs = 20000;

    document.querySelectorAll('[data-live-url]').forEach(function (root) {
        const url = root.dataset.liveUrl;

        async function refresh() {
            try {
                const response = await fetch(url, { headers: { 'X-Requested-With': 'fetch' } });
                // redirected = the login page came back because the session ended; treat it as a failure
                if (!response.ok || response.redirected) {
                    throw new Error('Refresh failed');
                }
                root.innerHTML = await response.text();
            } catch {
                const status = root.querySelector('[data-live-status]');
                if (status) {
                    status.textContent = "Couldn't refresh, last updated " + status.dataset.updated + '. Trying again…';
                    status.classList.add('is-offline');
                }
            }
        }

        setInterval(refresh, pollMs);
    });
})();
```

- [ ] **Step 2: Commit (Jordan).** Nothing uses the script yet, so commit it together with Task 5.

---

### Task 5: Floor page

**Files:**
- Create: `Pages/Staff/Floor/Index.cshtml.cs`, `Pages/Staff/Floor/Index.cshtml`, `Pages/Staff/Floor/_FloorGrid.cshtml`

- [ ] **Step 1: `Pages/Staff/Floor/Index.cshtml.cs`**

```csharp
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Floor;

// Staff landing page: every table in one dining area, a Needs-you bar on top, and a drawer for the chosen table
// (spec: docs/superpowers/specs/2026-11-01-staff-floor-design.md)
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly IFloorService _floorService;
    private readonly IOrderService _orderService;
    private readonly IReservationService _reservationService;

    public IndexModel(IFloorService floorService, IOrderService orderService, IReservationService reservationService)
    {
        _floorService = floorService;
        _orderService = orderService;
        _reservationService = reservationService;
    }

    // Not called "Area": that route name is reserved for ASP.NET Core areas and would break every link on the page
    [BindProperty(SupportsGet = true)]
    public int? AreaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Table { get; set; }

    public FloorViewModel Floor { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    // Every 20 seconds staff-live.js asks for just the Needs-you bar and the grid
    public async Task<PartialViewResult> OnGetGridAsync()
    {
        await LoadAsync();
        return Partial("_FloorGrid", Floor);
    }

    public async Task<IActionResult> OnPostConfirmAsync(int reservationId)
    {
        var (success, message) = await _reservationService.UpdateStatusByStaffAsync(reservationId, ReservationStatus.Confirmed);
        return BackToTable(success, message);
    }

    // ReadyToServe is the status the button was shown for, so a second iPad pressing it too gets a clear message
    public async Task<IActionResult> OnPostServeAsync(int orderId)
    {
        var (success, message) = await _orderService.AdvanceStatusAsync(orderId, OrderStatus.ReadyToServe);
        return BackToTable(success, message);
    }

    public async Task<IActionResult> OnPostCompleteAsync(int reservationId)
    {
        var (success, message) = await _floorService.CompleteTableAsync(reservationId);
        return BackToTable(success, message);
    }

    private async Task LoadAsync()
    {
        Floor = await _floorService.GetFloorAsync(AreaId);
        Floor.SelectedTableId = Table;
    }

    // Back to the same area with the same drawer open, showing the result in the usual banner
    private IActionResult BackToTable(bool success, string message)
    {
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { areaId = AreaId, table = Table });
    }
}
```

- [ ] **Step 2: `Pages/Staff/Floor/_FloorGrid.cshtml`**

```cshtml
@using FoodOrderingSystem.ViewModels
@model FloorViewModel
@*
    Live part of the Floor: the Needs-you bar, the table tiles and the footer.
    Rendered inside Index.cshtml, and again every 20 s by staff-live.js through ?handler=Grid.
    Page paths are absolute because this partial is also rendered by the Grid handler.
*@
@{
    var chips = Model.NeedsYou.Take(FloorViewModel.BarLimit).ToList();
    var more = Model.NeedsYou.Count - chips.Count;
}

<div class="fl-needs" role="region" aria-label="Needs you">
    <span class="fl-needs-label">Needs you</span>
    @if (chips.Count == 0)
    {
        <span class="fl-needs-none">All good right now</span>
    }
    @foreach (var item in chips)
    {
        var href = item.RestaurantTableId != null
            ? Url.Page("/Staff/Floor/Index", new { areaId = item.DiningAreaId, table = item.RestaurantTableId })
            : Url.Page("/Staff/Orders/Index");
        <a class="fl-chip is-@item.Tone" href="@href">
            @item.Text
            @if (item.HasAllergyNote)
            {
                <span class="fl-chip-flag">⚠ allergy</span>
            }
        </a>
    }
    @if (more > 0)
    {
        <a class="fl-chip is-more" asp-page="/Staff/Today/Index">+@more more</a>
    }
</div>

@if (Model.Tables.Count == 0)
{
    <div class="alert alert-light border">No active tables in @Model.SelectedAreaName.</div>
}
else
{
    <div class="fl-grid">
        @foreach (var tile in Model.Tables)
        {
            var isOpen = tile.RestaurantTableId == Model.SelectedTableId;
            <a asp-page="/Staff/Floor/Index" asp-route-areaId="@tile.DiningAreaId" asp-route-table="@tile.RestaurantTableId"
               class="fl-tile is-@TableStateDisplay.CssName(tile.State) @(isOpen ? "selected" : "")"
               aria-current="@(isOpen ? "true" : null)">
                @if (tile.IsPendingBooking)
                {
                    <span class="fl-dot" title="Booking not confirmed yet"></span>
                }
                <span class="fl-tile-num">@tile.TableNumber</span>
                <span class="fl-tile-state">@TableStateDisplay.Label(tile.State)</span>
                <span class="fl-tile-sub">@tile.Subtitle</span>
            </a>
        }
    </div>
}

<div class="fl-foot">
    <span class="fl-legend">
        @foreach (var state in TableStateDisplay.InLegendOrder)
        {
            <span class="is-@TableStateDisplay.CssName(state)">@TableStateDisplay.Label(state)</span>
        }
    </span>
    <span data-live-status data-updated="@Model.UpdatedAt.ToString("h:mm tt")">
        Updated @Model.UpdatedAt.ToString("h:mm tt") · refreshes every 20 s
    </span>
</div>
```

- [ ] **Step 3: `Pages/Staff/Floor/Index.cshtml`**

```cshtml
@page
@using FoodOrderingSystem.ViewModels
@model FoodOrderingSystem.Pages.Staff.Floor.IndexModel
@{
    ViewData["Title"] = "Floor";
    var floor = Model.Floor;
    var selected = floor.SelectedTable;
}

<div class="container mt-4 fl">
    <div class="fl-head">
        <h1 class="mb-0">Floor</h1>
        @if (floor.Areas.Count > 1)
        {
            <nav class="fl-areas" aria-label="Dining areas">
                @foreach (var area in floor.Areas)
                {
                    var isOn = area.DiningAreaId == floor.SelectedAreaId;
                    <a asp-page="./Index" asp-route-areaId="@area.DiningAreaId"
                       class="fl-area @(isOn ? "active" : "")" aria-current="@(isOn ? "page" : null)">@area.Name</a>
                }
            </nav>
        }
    </div>

    @if (floor.Areas.Count == 0)
    {
        <div class="alert alert-light border">No tables set up yet. Ask an admin to add dining areas and tables.</div>
    }
    else
    {
        <div class="fl-layout">
            <div class="fl-live"
                 data-live-url="@Url.Page("./Index", "Grid", new { areaId = floor.SelectedAreaId, table = floor.SelectedTableId })">
                <partial name="_FloorGrid" model="floor" />
            </div>

            @* A fixed panel from lg up (iPad landscape); a slide-in panel below lg, and a bottom sheet on phones *@
            <aside id="tableDrawer" class="fl-drawer offcanvas-lg offcanvas-end" tabindex="-1"
                   aria-labelledby="tableDrawerTitle" data-open="@(selected != null ? "true" : "false")">
                @if (selected == null)
                {
                    <div class="fl-drawer-body">
                    <h2 id="tableDrawerTitle" class="fl-drawer-title">Tonight at a glance</h2>
                    <p class="fl-muted">@floor.SelectedAreaName · tap a table to see its booking, order and bill.</p>
                    <dl class="fl-glance">
                        @foreach (var state in TableStateDisplay.InLegendOrder)
                        {
                            <div class="is-@TableStateDisplay.CssName(state)">
                                <dt>@TableStateDisplay.Label(state)</dt>
                                <dd>@(floor.Tables.Count(t => t.State == state))</dd>
                            </div>
                        }
                    </dl>
                    </div>
                }
                else
                {
                    @* One wrapping element keeps everything below in markup mode, so the nested @if/@switch are valid *@
                    <div class="fl-drawer-body">
                    <div class="fl-drawer-head">
                        <h2 id="tableDrawerTitle" class="fl-drawer-title">Table @selected.TableNumber</h2>
                        <a asp-page="./Index" asp-route-areaId="@floor.SelectedAreaId" class="btn-close" aria-label="Close table"></a>
                    </div>
                    <span class="fl-state is-@TableStateDisplay.CssName(selected.State)">@TableStateDisplay.Label(selected.State)</span>

                    @if (selected.ReservationId != null)
                    {
                        <p class="fl-who">
                            @selected.CustomerName · party of @selected.PartySize<br />
                            <span class="fl-muted">
                                @(selected.StartTime?.ToString("h:mm tt"))–@(selected.EndTime?.ToString("h:mm tt")) · Ref #@selected.ReservationId
                            </span>
                        </p>
                    }
                    else
                    {
                        <p class="fl-muted">@selected.Capacity seats · no booking right now</p>
                    }

                    @foreach (var order in selected.Orders)
                    {
                        <div class="fl-order">
                            <div class="fl-row">
                                <span class="fw-semibold">Order #@order.OrderId</span>
                                <partial name="/Pages/Orders/_OrderStatusBadge.cshtml" model="order.Status" />
                            </div>
                            @if (!string.IsNullOrEmpty(order.Notes))
                            {
                                <div class="fl-note">⚠ @order.Notes</div>
                            }
                            <ul class="fl-lines">
                                @foreach (var line in order.Lines)
                                {
                                    <li><span>@line.Quantity× @line.Name</span><span>@((line.Quantity * line.UnitPrice).ToString("0.00"))</span></li>
                                }
                            </ul>
                            <div class="fl-row">
                                <span class="fw-semibold">RM @order.TotalAmount.ToString("0.00")</span>
                                <partial name="/Pages/Payments/_PaymentStatusBadge.cshtml" model="order.PaymentStatus" />
                            </div>
                        </div>
                    }

                    @switch (selected.Action)
                    {
                        case TableAction.ConfirmBooking:
                            <form method="post" asp-page-handler="Confirm" asp-route-reservationId="@selected.ReservationId"
                                  asp-route-areaId="@floor.SelectedAreaId" asp-route-table="@selected.RestaurantTableId">
                                <button type="submit" class="btn btn-primary fl-action">Confirm booking</button>
                            </form>
                            break;
                        case TableAction.MarkServed:
                            <form method="post" asp-page-handler="Serve" asp-route-orderId="@selected.ActionOrderId"
                                  asp-route-areaId="@floor.SelectedAreaId" asp-route-table="@selected.RestaurantTableId">
                                <button type="submit" class="btn btn-primary fl-action">Mark #@selected.ActionOrderId served</button>
                            </form>
                            break;
                        case TableAction.CompleteTable:
                            <form method="post" asp-page-handler="Complete" asp-route-reservationId="@selected.ReservationId"
                                  asp-route-areaId="@floor.SelectedAreaId" asp-route-table="@selected.RestaurantTableId">
                                <button type="submit" class="btn btn-primary fl-action">Complete table</button>
                            </form>
                            break;
                        case TableAction.TakePayment:
                            <a asp-page="/Staff/Billing/Bill" asp-route-id="@selected.ActionOrderId" class="btn btn-primary fl-action">
                                Take payment · RM @selected.UnpaidTotal.ToString("0.00") →
                            </a>
                            break;
                        case TableAction.ViewInPass:
                            <a asp-page="/Staff/Orders/Index" class="btn btn-outline-primary fl-action">View in The Pass</a>
                            break;
                    }

                    @if (selected.LaterToday.Count > 0)
                    {
                        <h3 class="fl-sub">Later today</h3>
                        <ul class="fl-later">
                            @foreach (var booking in selected.LaterToday)
                            {
                                <li>@booking</li>
                            }
                        </ul>
                    }
                    </div>
                }
            </aside>
        </div>
    }
</div>

@section Scripts {
    <script src="~/js/staff-live.js" asp-append-version="true"></script>
    <script>
        // Below the lg breakpoint the drawer is a slide-in panel, so open it when a table was chosen
        (function () {
            const drawer = document.getElementById('tableDrawer');
            if (drawer && drawer.dataset.open === 'true' && window.matchMedia('(max-width: 991.98px)').matches) {
                bootstrap.Offcanvas.getOrCreateInstance(drawer).show();
            }
        })();
    </script>
}
```

- [ ] **Step 4: Build**

Run: `dotnet build FoodOrderingSystem.slnx`
Expected: `Build succeeded.`, no new warnings.

- [ ] **Step 5: Check that the page works (it's unstyled until Task 6).** Run the app, log in as staff, and open `http://localhost:5068/Staff/Floor`.
Expected: the heading "Floor", area links, a Needs-you line, one link per active table showing its number, state word
and subtitle, the footer "Updated … · refreshes every 20 s", and on the right "Tonight at a glance" with four counts.
Click a table: the URL becomes `/Staff/Floor?areaId=…&table=…` and the drawer shows "Table T…". Wait 20 s with the
browser dev tools Network tab open: a request to `/Staff/Floor?handler=Grid&areaId=…` returns 200 every 20 s.

- [ ] **Step 6: Commit (Jordan)**

Stage: `FoodOrderingSystem/wwwroot/js/staff-live.js`, the three files in `FoodOrderingSystem/Pages/Staff/Floor/`
Message: `feat(UI): add staff Floor page with needs-you bar and table drawer`

---

### Task 6: Floor styling

**Files:**
- Modify: `wwwroot/css/backoffice.css`. Insert a new section **before** the existing section `8. Phone and tablet`.

- [ ] **Step 1: Add section 9 for the Floor.** Paste above the `8. Phone and tablet` comment block:

```css
/* ---------------------------------------------------------------------------
   9. Staff Floor (spec 2026-11-01-staff-floor-design.md)
   Tiles: free = plain, booked = dashed jade, in use = jade fill, bill due = plum fill.
   Every state also has a text label, so colour is never the only signal.
   --------------------------------------------------------------------------- */
.bo .fl-head {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    margin-bottom: 1rem;
}

.bo .fl-areas {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
}

.bo .fl-area {
    display: inline-flex;
    align-items: center;
    min-height: 44px;
    padding: 0 1rem;
    border: 1px solid var(--rr-line);
    border-radius: 999px;
    background-color: var(--rr-raised);
    color: var(--rr-ink);
    text-decoration: none;
    font-weight: 500;
}

.bo .fl-area.active {
    background-color: var(--rr-ink);
    border-color: var(--rr-ink);
    color: #FFFFFF;
}

/* Needs-you bar: one row of chips; scrolls sideways on narrow screens instead of wrapping into a wall */
.bo .fl-needs {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    overflow-x: auto;
    padding: 0.5rem 0.75rem;
    margin-bottom: 1rem;
    background-color: var(--rr-raised);
    border: 1px solid var(--rr-line);
    border-radius: var(--bo-radius);
    scrollbar-width: thin;
}

.bo .fl-needs-label {
    flex: none;
    font-size: 0.6875rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--rr-accent);
}

.bo .fl-needs-none {
    color: var(--rr-muted);
    font-size: var(--rr-text-sm);
}

.bo .fl-chip {
    flex: none;
    display: inline-flex;
    align-items: center;
    gap: 0.375rem;
    min-height: 44px;
    padding: 0 0.875rem;
    border: 1px solid;
    border-radius: 999px;
    font-size: var(--rr-text-sm);
    font-weight: 500;
    text-decoration: none;
    white-space: nowrap;
}

.bo .fl-chip.is-plum  { background-color: #F6E3E7; border-color: #E7C2CA; color: var(--rr-accent); }
.bo .fl-chip.is-jade  { background-color: #E3F1EC; border-color: #BFE0D6; color: var(--bo-jade); }
.bo .fl-chip.is-amber { background-color: #FBEFD9; border-color: #EED9B5; color: #94600B; }
.bo .fl-chip.is-more  { background-color: var(--rr-raised); border-color: var(--rr-line); color: var(--rr-muted); }

.bo .fl-chip-flag {
    font-size: 0.75rem;
    font-weight: 700;
}

/* Grid: 2 tiles per row on phones, 3 on iPad portrait, 4 from lg (iPad landscape) */
.bo .fl-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.75rem;
}

.bo .fl-tile {
    position: relative;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.125rem;
    min-height: 112px;
    padding: 0.75rem 0.5rem;
    border: 2px solid var(--rr-line);
    border-radius: 14px;
    background-color: var(--rr-raised);
    color: var(--rr-ink);
    text-align: center;
    text-decoration: none;
}

.bo .fl-tile:focus-visible {
    outline: 3px solid var(--rr-ink);
    outline-offset: 2px;
}

.bo .fl-tile.is-booked  { border-color: var(--bo-jade); border-style: dashed; }
.bo .fl-tile.is-inuse   { border-color: var(--bo-jade); background-color: #E3F1EC; }
.bo .fl-tile.is-billdue { border-color: var(--rr-accent); background-color: #F6E3E7; }

.bo .fl-tile.selected {
    outline: 3px solid var(--rr-ink);
    outline-offset: 3px;
}

.bo .fl-tile-num {
    font-family: var(--rr-serif);
    font-size: 1.5rem;
    line-height: 1.1;
}

.bo .fl-tile-state {
    font-size: 0.6875rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-transform: uppercase;
}

.bo .fl-tile-sub {
    font-size: 0.8125rem;
    color: var(--rr-muted);
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 100%;
}

/* Amber dot: the booking is still Pending */
.bo .fl-dot {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    width: 10px;
    height: 10px;
    border-radius: 50%;
    background-color: #C88A1E;
}

.bo .fl-foot {
    display: flex;
    flex-wrap: wrap;
    justify-content: space-between;
    gap: 0.5rem;
    margin-top: 0.875rem;
    font-size: 0.8125rem;
    color: var(--rr-muted);
}

.bo .fl-legend {
    display: flex;
    flex-wrap: wrap;
    gap: 0.875rem;
}

.bo .fl-legend span::before {
    content: "";
    display: inline-block;
    width: 12px;
    height: 12px;
    margin-right: 0.375rem;
    vertical-align: -1px;
    border: 2px solid var(--rr-line);
    border-radius: 4px;
    background-color: var(--rr-raised);
}

.bo .fl-legend .is-booked::before  { border-color: var(--bo-jade); border-style: dashed; }
.bo .fl-legend .is-inuse::before   { border-color: var(--bo-jade); background-color: #E3F1EC; }
.bo .fl-legend .is-billdue::before { border-color: var(--rr-accent); background-color: #F6E3E7; }

.bo [data-live-status].is-offline {
    color: var(--rr-accent);
    font-weight: 600;
}

/* Drawer (also used for "Tonight at a glance") */
.bo .fl-drawer {
    --bs-offcanvas-width: 360px;
    background-color: var(--rr-raised) !important;   /* offcanvas-lg forces transparent from lg up */
    padding: 1.25rem;
    overflow-y: auto;
}

.bo .fl-drawer-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.bo .fl-drawer-title {
    font-family: var(--rr-serif);
    font-size: 1.375rem;
    margin: 0 0 0.25rem;
}

.bo .fl-drawer .btn-close {
    width: 44px;
    height: 44px;
}

.bo .fl-state {
    display: inline-block;
    margin: 0.25rem 0 0.75rem;
    padding: 0.2em 0.75em;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 700;
    background-color: #ECEAE4;
    color: var(--rr-muted);
}

.bo .fl-state.is-booked  { background-color: #E3F1EC; color: var(--bo-jade); }
.bo .fl-state.is-inuse   { background-color: #E3F1EC; color: var(--bo-jade); }
.bo .fl-state.is-billdue { background-color: #F6E3E7; color: var(--rr-accent); }

.bo .fl-muted {
    color: var(--rr-muted);
    font-size: var(--rr-text-sm);
}

.bo .fl-who {
    margin-bottom: 0.75rem;
}

.bo .fl-order {
    padding: 0.75rem 0;
    border-top: 1px solid var(--rr-line);
}

.bo .fl-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
}

.bo .fl-lines {
    list-style: none;
    padding: 0;
    margin: 0.375rem 0;
    font-size: var(--rr-text-sm);
}

.bo .fl-lines li {
    display: flex;
    justify-content: space-between;
}

.bo .fl-note {
    margin: 0.375rem 0;
    padding: 0.25rem 0.5rem;
    border-left: 3px solid #C88A1E;
    background-color: #FBEFD9;
    font-size: var(--rr-text-sm);
}

.bo .fl-action {
    display: block;
    width: 100%;
    min-height: 48px;
    margin-top: 0.75rem;
    font-weight: 600;
}

.bo .fl-sub {
    margin: 1.25rem 0 0.375rem;
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--rr-muted);
}

.bo .fl-later {
    padding-left: 1rem;
    font-size: var(--rr-text-sm);
}

.bo .fl-glance {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.5rem;
    margin: 0.75rem 0 0;
}

.bo .fl-glance div {
    padding: 0.625rem 0.75rem;
    border: 1px solid var(--rr-line);
    border-radius: var(--bo-radius);
}

.bo .fl-glance dt {
    font-size: 0.75rem;
    font-weight: 600;
    color: var(--rr-muted);
}

.bo .fl-glance dd {
    margin: 0;
    font-family: var(--rr-serif);
    font-size: 1.5rem;
}

/* iPad portrait and up: 3 tiles per row */
@media (min-width: 768px) {
    .bo .fl-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
    }
}

/* iPad landscape and up: side menu visible, 4 tiles per row, drawer becomes a fixed panel on the right */
@media (min-width: 992px) {
    .bo .fl-layout {
        display: grid;
        grid-template-columns: minmax(0, 1fr) 320px;
        gap: 1.25rem;
        align-items: start;
    }

    .bo .fl-grid {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }

    .bo .fl-drawer {
        position: sticky;
        top: 4.5rem;   /* below the sticky top bar */
        max-height: calc(100vh - 5.5rem);
        border: 1px solid var(--rr-line);
        border-radius: var(--bo-radius);
    }
}

/* Phones: the drawer comes up from the bottom (easy to reach with a thumb) instead of from the right */
@media (max-width: 767.98px) {
    .bo .fl-drawer.offcanvas-lg.offcanvas-end {
        top: auto;
        left: 0;
        right: 0;
        width: 100%;
        max-height: 80vh;
        border-left: 0;
        border-top: 1px solid var(--rr-line);
        border-radius: 16px 16px 0 0;
        transform: translateY(100%);
    }

    .bo .fl-drawer.offcanvas-lg.offcanvas-end.showing,
    .bo .fl-drawer.offcanvas-lg.offcanvas-end.show:not(.hiding) {
        transform: none;
    }
}
```

- [ ] **Step 2: Build and run.** Hard-refresh the browser (Ctrl+F5) so it loads the new CSS.

- [ ] **Step 3: TC-F07, check each screen size.** In Edge, press F12, then the device toolbar (Ctrl+Shift+M):
  - **iPad Air landscape (1180×820):** side menu visible, 4 tiles per row, drawer panel on the right. Clicking a table changes the panel to that table.
  - **iPad Air portrait (820×1180):** ☰ menu, 3 tiles per row. Clicking a table slides the drawer in from the right. ✕ closes it.
  - **iPhone 12 Pro (390×844):** 2 tiles per row, the Needs-you bar scrolls sideways, and clicking a table brings the drawer up from the bottom.
  - On all three: no sideways scroll on the page itself, and every chip, tile and button is at least 44 px tall (check in dev tools).

- [ ] **Step 4: Commit (Jordan)**

Stage: `FoodOrderingSystem/wwwroot/css/backoffice.css`
Message: `feat(UI): style staff Floor for iPad and phone`

---

### Task 7: Menu groups and staff landing page

**Files:**
- Modify: `Pages/Shared/_BackOfficeNav.cshtml`, the comment block and the `Operations` entry in `groups`
- Modify: `Pages/Staff/_ViewStart.cshtml`, comment only
- Modify: `Areas/Identity/Pages/Account/Login.cshtml.cs:138` (**Carynn's file, 1 line → group OK in the PR**)

- [ ] **Step 1: Replace the nav's comment block.** In `_BackOfficeNav.cshtml`, replace the opening `@* … *@` comment with:

```cshtml
@*
    Side menu for the back office. Groups are filtered by role: staff see the four service groups
    (Front of house, Kitchen, Counter, Guests); administrators see those plus the admin groups.
    A link stays highlighted on its sub-pages because it is matched by path prefix (e.g. "/Admin/Menu/" covers
    Edit Item and Delete Category). Hiding a link is only for tidiness: [Authorize] and the folder policies
    still guard every page.
*@
```

- [ ] **Step 2: Replace the Operations group.** In the same file, replace this whole entry:

```csharp
        ("Operations", false, new[]
        {
            ("Orders board", "/Staff/Orders/Index", "/Staff/Orders/"),
            ("Reservations", "/Staff/Reservations/Index", "/Staff/Reservations/"),
            ("Billing", "/Staff/Billing/Index", "/Staff/Billing/"),
            ("Reviews", "/Staff/Feedback/Index", "/Staff/Feedback/")
        }),
```

with:

```csharp
        // Staff groups follow how a shift is run: the room, the kitchen, the counter, then guest feedback
        ("Front of house", false, new[]
        {
            ("Floor", "/Staff/Floor/Index", "/Staff/Floor/"),
            ("Today", "/Staff/Today/Index", "/Staff/Today/"),
            ("Reservations", "/Staff/Reservations/Index", "/Staff/Reservations/")
        }),
        ("Kitchen", false, new[]
        {
            ("The Pass", "/Staff/Orders/Index", "/Staff/Orders/")
        }),
        ("Counter", false, new[]
        {
            ("Billing", "/Staff/Billing/Index", "/Staff/Billing/")
        }),
        ("Guests", false, new[]
        {
            ("Reviews", "/Staff/Feedback/Index", "/Staff/Feedback/")
        }),
```

- [ ] **Step 3: Update the staff `_ViewStart` comment.** In `Pages/Staff/_ViewStart.cshtml`, change the first line to:

```cshtml
@* Staff pages share the back office shell with admin; the side menu shows staff the Front of house, Kitchen, Counter and Guests groups *@
```

- [ ] **Step 4: Land staff on the Floor.** In `Areas/Identity/Pages/Account/Login.cshtml.cs`, line 138, change

```csharp
                        return RedirectToPage("/Staff/Orders/Index", new { area = "" });
```
to
```csharp
                        return RedirectToPage("/Staff/Floor/Index", new { area = "" });
```

- [ ] **Step 5: Build**

Run: `dotnet build FoodOrderingSystem.slnx`
Expected: `Build succeeded.`, no new warnings.

- [ ] **Step 6: TC-F01 and a role check**
  - Log out, then log in as staff with no ReturnUrl. You land on `/Staff/Floor`, and "Floor" is highlighted under **Front of house**. The menu shows only Front of house, Kitchen, Counter and Guests.
  - Log out, then log in as admin. You land on `/Admin/Dashboard`. The menu shows Insights, the four staff groups, then Restaurant, Marketing and People.
  - Open `/Identity/Account/Login?ReturnUrl=%2FStaff%2FReservations` and log in as staff. You land on Reservations, not Floor.

- [ ] **Step 7: Commit (Jordan)**

Stage: `FoodOrderingSystem/Pages/Shared/_BackOfficeNav.cshtml`, `FoodOrderingSystem/Pages/Staff/_ViewStart.cshtml`, `FoodOrderingSystem/Areas/Identity/Pages/Account/Login.cshtml.cs`
Message: `feat(UI): group staff menu by service area and land staff on Floor`

---

### Task 8: The Pass (ticket rail)

**Files:**
- Create: `Pages/Staff/Orders/_PassRail.cshtml`
- Modify: `Pages/Staff/Orders/Index.cshtml.cs` (add `Mode`, the rail loading and `OnGetRailAsync`; keep everything else)
- Modify: `Pages/Staff/Orders/Index.cshtml` (full replacement below; the History part is today's markup, unchanged)

- [ ] **Step 1: Replace `Pages/Staff/Orders/Index.cshtml.cs` with:**

```csharp
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Models.Enums;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Orders;

// The Pass: the kitchen's ticket rail for today's open orders (PB15, spec 2026-11-01-staff-floor-design.md).
// History keeps the original board: any day, with the Active / Completed / Cancelled / All filters.
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService;

    public IndexModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public List<StaffOrderViewModel> Orders { get; set; } = new();

    // "rail" (the default) or "history"
    [BindProperty(SupportsGet = true)]
    public string Mode { get; set; } = "rail";

    public bool IsHistory => Mode == "history";

    // Bound from the query string so the chosen day and filter show in the URL, e.g. ?mode=history&date=2026-10-22&filter=All
    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public StaffOrderFilter Filter { get; set; } = StaffOrderFilter.Active;

    public DateOnly SelectedDate => Date ?? DateOnly.FromDateTime(DateTime.Now);

    // In History, only today's active list refreshes itself; an old day or a finished list does not change.
    // The rail refreshes through staff-live.js instead.
    public bool AutoRefresh => IsHistory && Filter == StaffOrderFilter.Active && SelectedDate == DateOnly.FromDateTime(DateTime.Now);

    public async Task OnGetAsync()
    {
        Orders = IsHistory
            ? await _orderService.GetOrdersForStaffAsync(SelectedDate, Filter)
            : await GetRailOrdersAsync();
    }

    // Every 20 seconds staff-live.js asks for just the rail
    public async Task<PartialViewResult> OnGetRailAsync()
    {
        return Partial("_PassRail", await GetRailOrdersAsync());
    }

    // currentStatus is the status shown on the card when the button was pressed (stale-click guard)
    public async Task<IActionResult> OnPostAdvanceAsync(int id, OrderStatus currentStatus)
    {
        var (success, message) = await _orderService.AdvanceStatusAsync(id, currentStatus);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = message;

        // Back to the same view, day and filter
        return RedirectToPage(new { mode = Mode, date = Date?.ToString("yyyy-MM-dd"), filter = Filter });
    }

    // Today's open orders (Pending, Preparing, ReadyToServe, Served), oldest first, as the kitchen works them
    private Task<List<StaffOrderViewModel>> GetRailOrdersAsync() =>
        _orderService.GetOrdersForStaffAsync(DateOnly.FromDateTime(DateTime.Now), StaffOrderFilter.Active);
}
```

- [ ] **Step 2: Create `Pages/Staff/Orders/_PassRail.cshtml`**

```cshtml
@using FoodOrderingSystem.Models.Enums
@using FoodOrderingSystem.ViewModels
@model List<StaffOrderViewModel>
@*
    The Pass rail: one column per kitchen step. Rendered inside Index.cshtml, and again every 20 s by
    staff-live.js through ?handler=Rail, so page paths are absolute.
    Paying does not change an order's status, so a Served order stays here until it is Completed.
*@
@{
    var columns = new (string Key, string Title, OrderStatus Status)[]
    {
        ("new", "New", OrderStatus.Pending),
        ("preparing", "Preparing", OrderStatus.Preparing),
        ("ready", "Ready", OrderStatus.ReadyToServe),
        ("served", "Served", OrderStatus.Served)
    };
}

<div class="ps-cols">
    @foreach (var column in columns)
    {
        var orders = Model.Where(o => o.Status == column.Status).ToList();
        <section class="ps-col" data-col="@column.Key" aria-label="@column.Title">
            <h2 class="ps-col-head"><span>@column.Title</span><span class="ps-count">@orders.Count</span></h2>

            @if (orders.Count == 0)
            {
                <p class="ps-empty">Nothing here</p>
            }

            @foreach (var order in orders)
            {
                var minutes = OrderAgeDisplay.MinutesSincePlaced(order.CreatedAt);
                <article class="ps-ticket">
                    <header class="ps-ticket-head">
                        <span>@(order.TableNumber ?? "No table") · #@order.OrderId</span>
                        @if (order.Status == OrderStatus.Served)
                        {
                            <partial name="/Pages/Payments/_PaymentStatusBadge.cshtml" model="order.PaymentStatus" />
                        }
                        else
                        {
                            <span class="ps-age @OrderAgeDisplay.ToneClass(minutes)">placed @(minutes)m ago</span>
                        }
                    </header>

                    @* Notes often carry allergies, so they sit on top of the ticket *@
                    @if (!string.IsNullOrEmpty(order.Notes))
                    {
                        <div class="ps-note">⚠ @order.Notes</div>
                    }

                    <ul class="ps-lines">
                        @foreach (var line in order.Lines)
                        {
                            <li><strong>@line.Quantity×</strong> @line.Name</li>
                        }
                    </ul>

                    <p class="ps-meta">@order.CustomerName · @order.CreatedAt.ToLocalTime().ToString("h:mm tt")</p>

                    @if (order.Status == OrderStatus.Served && !order.IsPaid)
                    {
                        <a asp-page="/Staff/Billing/Bill" asp-route-id="@order.OrderId" class="btn btn-primary ps-action">
                            Take payment · RM @order.TotalAmount.ToString("0.00") →
                        </a>
                    }
                    else if (order.NextStatus != null)
                    {
                        <form method="post" asp-page="/Staff/Orders/Index" asp-page-handler="Advance"
                              asp-route-id="@order.OrderId" asp-route-currentStatus="@order.Status" asp-route-mode="rail">
                            <button type="submit" class="btn btn-primary ps-action">@OrderStatusDisplay.ActionLabel(order.NextStatus.Value)</button>
                        </form>
                    }

                    @if (order.CanCancel)
                    {
                        <a asp-page="/Staff/Orders/Cancel" asp-route-id="@order.OrderId" class="ps-cancel">Cancel order</a>
                    }
                </article>
            }
        </section>
    }
</div>

<p class="ps-foot" data-live-status data-updated="@DateTime.Now.ToString("h:mm tt")">
    Updated @DateTime.Now.ToString("h:mm tt") · refreshes every 20 s
</p>
```

- [ ] **Step 3: Replace `Pages/Staff/Orders/Index.cshtml` with:**

```cshtml
@page
@using FoodOrderingSystem.ViewModels
@model FoodOrderingSystem.Pages.Staff.Orders.IndexModel
@{
    ViewData["Title"] = "The Pass";
    var date = Model.SelectedDate.ToString("yyyy-MM-dd");
}

<div class="container mt-4">
    <div class="d-flex flex-wrap justify-content-between align-items-center gap-3 mb-3">
        <h1 class="mb-0">The Pass</h1>

        <nav class="ps-mode" aria-label="View">
            <a asp-page="./Index" asp-route-mode="rail" class="@(Model.IsHistory ? "" : "active")"
               aria-current="@(Model.IsHistory ? null : "page")">Live rail</a>
            <a asp-page="./Index" asp-route-mode="history" class="@(Model.IsHistory ? "active" : "")"
               aria-current="@(Model.IsHistory ? "page" : null)">History</a>
        </nav>
    </div>

    @if (!Model.IsHistory)
    {
        @* Phones show one column at a time; these tabs pick which. From md up all four columns show. *@
        <div class="ps-tabs" role="tablist" aria-label="Kitchen step">
            <button type="button" class="active" data-col="new" aria-selected="true">New</button>
            <button type="button" data-col="preparing" aria-selected="false">Preparing</button>
            <button type="button" data-col="ready" aria-selected="false">Ready</button>
            <button type="button" data-col="served" aria-selected="false">Served</button>
        </div>

        <div id="passLive" class="ps-rail" data-active-col="new" data-live-url="@Url.Page("./Index", "Rail")">
            <partial name="_PassRail" model="Model.Orders" />
        </div>
    }
    else
    {
        <div class="ps-history">
        <!-- Day picker: a GET form, so the chosen day is in the URL -->
        <form method="get" asp-page="./Index" class="d-flex gap-2 mb-3">
            <input type="hidden" name="mode" value="history" />
            <input type="hidden" name="filter" value="@Model.Filter" />
            <label for="orderDate" class="visually-hidden">Day</label>
            <input id="orderDate" type="date" name="date" value="@date" class="form-control" style="max-width: 12rem" />
            <button type="submit" class="btn btn-outline-primary">Show</button>
        </form>

        <!-- Filter pills -->
        <ul class="nav nav-pills mb-3">
            @foreach (var filter in Enum.GetValues<StaffOrderFilter>())
            {
                <li class="nav-item">
                    <a class="nav-link @(filter == Model.Filter ? "active" : "")"
                       asp-page="./Index" asp-route-mode="history" asp-route-date="@date" asp-route-filter="@filter">@filter</a>
                </li>
            }
        </ul>

        @if (Model.AutoRefresh)
        {
            <p class="text-muted small">This list refreshes every 30 seconds.</p>
        }

        @if (Model.Orders.Count == 0)
        {
            <div class="alert alert-light border">No @Model.Filter.ToString().ToLower() orders on @Model.SelectedDate.ToString("dd/MM/yyyy").</div>
        }

        <div class="row g-3">
            @foreach (var order in Model.Orders)
            {
                <div class="col-12 col-md-6 col-xl-4">
                    <div class="card shadow-sm h-100">
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <span class="fw-semibold">#@order.OrderId · @order.CreatedAt.ToString("hh:mm tt")</span>
                            <partial name="/Pages/Orders/_OrderStatusBadge.cshtml" model="order.Status" />
                        </div>

                        <div class="card-body">
                            <p class="mb-2">
                                @order.CustomerName
                                @if (order.ReservationId != null)
                                {
                                    <span class="text-muted">· Reservation #@order.ReservationId</span>
                                }
                            </p>

                            <ul class="list-unstyled mb-2">
                                @foreach (var line in order.Lines)
                                {
                                    <li><strong>@line.Quantity ×</strong> @line.Name</li>
                                }
                            </ul>

                            @* Notes often carry allergies, so they stand out *@
                            @if (!string.IsNullOrEmpty(order.Notes))
                            {
                                <div class="alert alert-warning py-2 mb-2">@order.Notes</div>
                            }

                            <div class="d-flex justify-content-between align-items-center">
                                <span class="text-muted">Total RM @order.TotalAmount.ToString("0.00")</span>
                                <partial name="/Pages/Payments/_PaymentStatusBadge.cshtml" model="order.PaymentStatus" />
                            </div>
                        </div>

                        @if (order.NextStatus != null || order.CanCancel)
                        {
                            <div class="card-footer d-flex flex-wrap gap-2">
                                @if (order.NextStatus != null)
                                {
                                    <form method="post" asp-page-handler="Advance" asp-route-id="@order.OrderId"
                                          asp-route-currentStatus="@order.Status" asp-route-mode="history"
                                          asp-route-date="@Model.Date?.ToString("yyyy-MM-dd")" asp-route-filter="@Model.Filter">
                                        <button type="submit" class="btn btn-primary">@OrderStatusDisplay.ActionLabel(order.NextStatus.Value)</button>
                                    </form>
                                }
                                @if (order.CanCancel)
                                {
                                    <a asp-page="./Cancel" asp-route-id="@order.OrderId" class="btn btn-outline-danger">Cancel</a>
                                }
                            </div>
                        }
                    </div>
                </div>
            }
        </div>
        </div>
    }
</div>

@section Scripts {
    @* AutoRefresh is only ever true in History; the rail refreshes through staff-live.js *@
    @if (Model.AutoRefresh)
    {
        <script>
            // New orders appear without pressing F5. A plain reload keeps this simple.
            setTimeout(function () { window.location.reload(); }, 30000);
        </script>
    }
    else if (!Model.IsHistory)
    {
        <script src="~/js/staff-live.js" asp-append-version="true"></script>
        <script>
            // Phone tabs: remember the chosen column on #passLive, which staff-live.js never replaces,
            // so the same column stays visible after every refresh
            (function () {
                const rail = document.getElementById('passLive');
                document.querySelectorAll('.ps-tabs button').forEach(function (button) {
                    button.addEventListener('click', function () {
                        rail.dataset.activeCol = button.dataset.col;
                        document.querySelectorAll('.ps-tabs button').forEach(function (b) {
                            b.classList.toggle('active', b === button);
                            b.setAttribute('aria-selected', b === button ? 'true' : 'false');
                        });
                    });
                });
            })();
        </script>
    }
}
```

- [ ] **Step 4: Check whether `Cancel` sends you back to the rail.** `Pages/Staff/Orders/Cancel.cshtml.cs:46` returns
  `RedirectToPage("./Index")`, which lands on the rail because `rail` is the default `Mode`. No change is needed.

- [ ] **Step 5: Build**

Run: `dotnet build FoodOrderingSystem.slnx`
Expected: `Build succeeded.`, no new warnings.

- [ ] **Step 6: Commit (Jordan).** Commit together with Task 9, because the rail is unstyled until then.

---

### Task 9: The Pass styling

**Files:**
- Modify: `wwwroot/css/backoffice.css`, appending section 10 directly after section 9 (still before section 8's phone rules)

- [ ] **Step 1: Add section 10**

```css
/* ---------------------------------------------------------------------------
   10. The Pass (ticket rail)
   Four columns on tablets; on phones one column at a time, picked by .ps-tabs
   (the choice is kept in #passLive[data-active-col] so it survives the 20 s refresh).
   --------------------------------------------------------------------------- */
.bo .ps-mode {
    display: inline-flex;
    border: 1px solid var(--rr-line);
    border-radius: 999px;
    overflow: hidden;
    background-color: var(--rr-raised);
}

.bo .ps-mode a {
    display: inline-flex;
    align-items: center;
    min-height: 44px;
    padding: 0 1.125rem;
    color: var(--rr-muted);
    text-decoration: none;
    font-weight: 500;
}

.bo .ps-mode a.active {
    background-color: var(--rr-ink);
    color: #FFFFFF;
}

.bo .ps-tabs {
    display: flex;
    gap: 0.375rem;
    margin-bottom: 0.75rem;
    overflow-x: auto;
}

.bo .ps-tabs button {
    flex: 1 0 auto;
    min-height: 44px;
    padding: 0 0.875rem;
    border: 1px solid var(--rr-line);
    border-radius: 999px;
    background-color: var(--rr-raised);
    color: var(--rr-ink);
    font-weight: 500;
}

.bo .ps-tabs button.active {
    background-color: var(--rr-ink);
    border-color: var(--rr-ink);
    color: #FFFFFF;
}

.bo .ps-cols {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    gap: 0.75rem;
}

/* Phones: only the chosen column */
.bo .ps-rail[data-active-col="new"] .ps-col:not([data-col="new"]),
.bo .ps-rail[data-active-col="preparing"] .ps-col:not([data-col="preparing"]),
.bo .ps-rail[data-active-col="ready"] .ps-col:not([data-col="ready"]),
.bo .ps-rail[data-active-col="served"] .ps-col:not([data-col="served"]) {
    display: none;
}

.bo .ps-col {
    display: flex;
    flex-direction: column;
    gap: 0.875rem;
    padding: 0.625rem;
    border-radius: var(--bo-radius);
    background-color: #F2F0EA;
    min-height: 200px;
}

.bo .ps-col-head {
    display: flex;
    justify-content: space-between;
    margin: 0;
    font-family: var(--rr-sans);
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--rr-muted);
}

.bo .ps-empty {
    margin: 0;
    color: var(--rr-muted);
    font-size: var(--rr-text-sm);
}

/* A paper docket with a torn bottom edge */
.bo .ps-ticket {
    position: relative;
    display: flex;
    flex-direction: column;
    gap: 0.375rem;
    padding: 0.75rem 0.75rem 1rem;
    border-radius: 6px 6px 0 0;
    background-color: #FFFFFF;
    box-shadow: 0 1px 2px rgba(15, 46, 43, 0.14);
}

.bo .ps-ticket::after {
    content: "";
    position: absolute;
    left: 0;
    right: 0;
    bottom: -6px;
    height: 7px;
    background:
        linear-gradient(-45deg, transparent 4px, #FFFFFF 0) 0 0 / 8px 7px,
        linear-gradient(45deg, transparent 4px, #FFFFFF 0) 0 0 / 8px 7px;
}

.bo .ps-ticket-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    font-weight: 700;
}

.bo .ps-age {
    flex: none;
    padding: 0.125rem 0.5rem;
    border-radius: 999px;
    font-size: 0.75rem;
    font-weight: 700;
}

.bo .ps-age.is-jade  { background-color: #E3F1EC; color: var(--bo-jade); }
.bo .ps-age.is-amber { background-color: #FBEFD9; color: #94600B; }
.bo .ps-age.is-plum  { background-color: #F6E3E7; color: var(--rr-accent); }

.bo .ps-note {
    padding: 0.25rem 0.5rem;
    border-left: 3px solid #C88A1E;
    background-color: #FBEFD9;
    font-size: var(--rr-text-sm);
    font-weight: 600;
}

.bo .ps-lines {
    list-style: none;
    padding: 0;
    margin: 0;
}

.bo .ps-meta {
    margin: 0;
    font-size: 0.8125rem;
    color: var(--rr-muted);
}

.bo .ps-action {
    display: block;
    width: 100%;
    min-height: 48px;
    font-weight: 600;
}

.bo .ps-ticket form {
    margin: 0;
}

/* Cancel is a quiet link so it is not hit by mistake next to the big button */
.bo .ps-cancel {
    align-self: flex-start;
    display: inline-flex;
    align-items: center;
    min-height: 44px;
    font-size: var(--rr-text-sm);
    color: var(--rr-muted);
}

.bo .ps-foot {
    margin-top: 0.875rem;
    font-size: 0.8125rem;
    color: var(--rr-muted);
}

/* iPad portrait and up: all four columns side by side, tabs hidden */
@media (min-width: 768px) {
    .bo .ps-tabs {
        display: none;
    }

    .bo .ps-cols {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }

    .bo .ps-rail .ps-col {
        display: flex !important;   /* undo the phone "one column" rule */
    }
}
```

- [ ] **Step 2: TC-P01 and the full order flow.** Run the app. As a **customer** (register one, or use the seeded customer if there is one), book a table for now and place an order with the note "No peanuts". As **staff**, open The Pass.
  - The ticket is under **New**, reads "T… · #id" (or "No table · #id" if it has no reservation), has an amber "⚠ No peanuts" strip, a "placed Nm ago" chip, and a **Start preparing** button.
  - Press the buttons in turn: the ticket moves New → Preparing → Ready → Served. In Served it shows **Unpaid** and **Take payment · RM … →**.
  - Pay by cash on the Bill page and go back to The Pass. The ticket shows **Paid** and a **Complete** button. Press it and the ticket leaves the rail.
  - **History** shows the original board with the day picker and filters. Choosing a day or filter keeps `mode=history` in the URL.
  - Age colour: for a quick check, temporarily change `AmberFromMinutes` to 0, rebuild, see amber, then change it back.

- [ ] **Step 3: Phone check (390 px).** One column at a time, and the tabs switch columns. After 20 s (watch the Network tab for `?handler=Rail`), the chosen tab is still the one showing.

- [ ] **Step 4: Commit (Jordan)**

Stage: `FoodOrderingSystem/Pages/Staff/Orders/Index.cshtml`, `FoodOrderingSystem/Pages/Staff/Orders/Index.cshtml.cs`, `FoodOrderingSystem/Pages/Staff/Orders/_PassRail.cshtml`, `FoodOrderingSystem/wwwroot/css/backoffice.css`
Message: `feat(PB15): turn the staff order board into The Pass ticket rail`

---

### Task 10: Floor end-to-end checks

No new code. These are the spec's Floor test cases, run against the real app. Fix anything that fails before you move on.

- [ ] **Step 1: TC-F02, one table in each state.** As admin, make sure one dining area has at least 4 active tables. As a customer, book:
  table A starting **30 min from now** (it stays Pending), table B starting **now** with an order placed, and table C starting now with an order that staff take all the way to Served.
  Expected on Floor: table A is **Booked** (dashed, with an amber dot, "h:mm PM · name · N pax"), table B is **In use** ("Pending · placed Nm ago"), table C is **Bill due** ("Bill due · RM …"), and an unbooked table is **Free** ("N seats").

- [ ] **Step 2: TC-F03.** Tap table A, then **Confirm booking**. Expected: a green banner "Reservation #… status updated to Confirmed.", the same drawer reopens, the dot is gone, and the "… not confirmed" chip is gone. (If table A starts more than 2 h away, the chip doesn't appear at all, which is also correct.)

- [ ] **Step 3: TC-F04.** On The Pass, move table B's order to Ready, then go to Floor. Expected: a jade chip "#id ready · B", and the tile reads "#id ready to serve". Tap it, then **Mark #id served**. Expected: the tile becomes **Bill due**, and on The Pass the ticket is under Served.

- [ ] **Step 4: TC-F05.** On table C's drawer, tap **Take payment**. Wallace's Bill page opens. Pay cash, then go back to Floor. Expected: table C shows **In use · "Paid · ready to complete"**. Tap **Complete table**. Expected: a success banner, the table becomes **Free**, and the order no longer appears on The Pass.

- [ ] **Step 5: The crafted-POST check.** On a Bill-due table, open dev tools and change the drawer's form action to `?handler=Complete&reservationId=<that reservation>`, or use any Complete form on an unpaid table. Expected: a red banner "Order #… is not finished or not paid yet, so the table can't be completed." and the table is unchanged.

- [ ] **Step 6: TC-F06.** Keep Floor open with a table drawer open on iPad landscape. In another browser, place a new order as a customer. Within 20 s a new tile state or chip appears and the open drawer stays open.

- [ ] **Step 7: Offline check.** With Floor open, stop the app (Ctrl+C in the terminal). Within 20 s the footer turns plum and reads "Couldn't refresh, last updated h:mm PM. Trying again…". The grid stays on screen.

- [ ] **Step 8: Admin check.** Log in as admin and open Floor from the menu. Expected: the page works the same, and the admin's landing page is still the Dashboard.

---

### Task 11: Today page

**Files:**
- Create: `Pages/Staff/Today/Index.cshtml.cs`, `Pages/Staff/Today/Index.cshtml`
- Modify: `wwwroot/css/backoffice.css`, appending section 11 after section 10

- [ ] **Step 1: `Pages/Staff/Today/Index.cshtml.cs`**

```csharp
using FoodOrderingSystem.Data;
using FoodOrderingSystem.Services;
using FoodOrderingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FoodOrderingSystem.Pages.Staff.Today;

// Read-only run sheet for the shift: counts (never money), today's bookings on a timeline, and everything
// that needs attention. Every action happens on Floor or The Pass, so each rule is enforced in one place.
[Authorize(Roles = $"{DbSeeder.StaffRole},{DbSeeder.AdminRole}")]
public class IndexModel : PageModel
{
    private readonly IFloorService _floorService;

    public IndexModel(IFloorService floorService)
    {
        _floorService = floorService;
    }

    public StaffTodayViewModel Today { get; set; } = new();

    public async Task OnGetAsync()
    {
        Today = await _floorService.GetTodayAsync();
    }
}
```

- [ ] **Step 2: `Pages/Staff/Today/Index.cshtml`**

```cshtml
@page
@using Microsoft.AspNetCore.Identity
@using FoodOrderingSystem.Models
@using FoodOrderingSystem.ViewModels
@inject UserManager<ApplicationUser> UserManager
@model FoodOrderingSystem.Pages.Staff.Today.IndexModel
@{
    ViewData["Title"] = "Today";
    var today = Model.Today;

    // First name for the greeting, read the same way _BackOfficeLayout reads the display name
    var user = await UserManager.GetUserAsync(User);
    var firstName = string.IsNullOrWhiteSpace(user?.FullName) ? null : user.FullName.Split(' ')[0];
    var greeting = today.Now.Hour < 12 ? "Good morning" : today.Now.Hour < 18 ? "Good afternoon" : "Good evening";

    var nowTime = TimeOnly.FromDateTime(today.Now);
    var nowLineShown = false;
}

<div class="container mt-4 td">
    <div class="d-flex flex-wrap justify-content-between align-items-baseline gap-2 mb-3">
        <h1 class="mb-0">@greeting@(firstName == null ? "" : ", " + firstName)</h1>
        <span class="text-muted">@today.Now.ToString("dddd, d MMMM · h:mm tt")</span>
    </div>

    <div class="td-counts">
        <div><b>@today.BookingsToday</b><span>bookings today</span></div>
        <div><b>@today.ArrivingSoon</b><span>arriving in the next 2 h</span></div>
        <div><b>@today.ActiveOrders</b><span>orders in the kitchen</span></div>
        <div><b>@today.BillsDue</b><span>bills due</span></div>
    </div>

    <div class="td-layout">
        <section class="td-needs" aria-labelledby="needsTitle">
            <h2 id="needsTitle" class="td-title">Needs you · @today.NeedsYou.Count</h2>
            @if (today.NeedsYou.Count == 0)
            {
                <p class="text-muted">Nothing needs you right now.</p>
            }
            @foreach (var item in today.NeedsYou)
            {
                var href = item.RestaurantTableId != null
                    ? Url.Page("/Staff/Floor/Index", new { areaId = item.DiningAreaId, table = item.RestaurantTableId })
                    : Url.Page("/Staff/Orders/Index");
                <a class="td-item is-@item.Tone" href="@href">
                    <span>
                        @item.Text
                        @if (item.HasAllergyNote)
                        {
                            <span class="fl-chip-flag">⚠ allergy</span>
                        }
                    </span>
                    <span aria-hidden="true">›</span>
                </a>
            }
        </section>

        <section class="td-timeline" aria-labelledby="timelineTitle">
            <h2 id="timelineTitle" class="td-title">Bookings today</h2>
            @if (today.Timeline.Count == 0)
            {
                <p class="text-muted">No bookings today.</p>
            }
            <ol class="td-line">
                @foreach (var booking in today.Timeline)
                {
                    // Inside the loop body we are in C#, so this "if" has no @
                    if (!nowLineShown && booking.StartTime > nowTime)
                    {
                        nowLineShown = true;
                        <li class="td-now" aria-label="Now"><span>@today.Now.ToString("h:mm")</span></li>
                    }
                    var isPast = booking.EndTime <= nowTime || booking.Status == "Completed";
                    var badge = booking.Status switch
                    {
                        "Confirmed" => "bg-success",
                        "Pending" => "bg-warning text-dark",
                        _ => "bg-secondary"
                    };
                    <li class="td-row @(isPast ? "is-past" : "")">
                        <span class="td-time">@booking.StartTime.ToString("h:mm tt")</span>
                        <a asp-page="/Staff/Floor/Index" asp-route-areaId="@booking.DiningAreaId" asp-route-table="@booking.RestaurantTableId" class="td-card">
                            <span>@booking.CustomerName · @booking.PartySize pax · @booking.TableNumber, @booking.DiningAreaName</span>
                            <span class="badge @badge">@booking.Status</span>
                        </a>
                    </li>
                }
                @if (!nowLineShown && today.Timeline.Count > 0)
                {
                    <li class="td-now" aria-label="Now"><span>@today.Now.ToString("h:mm")</span></li>
                }
            </ol>
        </section>
    </div>
</div>
```

- [ ] **Step 3: Add section 11 to `backoffice.css`**

```css
/* ---------------------------------------------------------------------------
   11. Staff Today (read-only run sheet)
   Phones: Needs you first, then the timeline. From lg: timeline left, Needs you right.
   --------------------------------------------------------------------------- */
.bo .td-counts {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.75rem;
    margin-bottom: 1.25rem;
}

.bo .td-counts div {
    padding: 0.75rem 1rem;
    border: 1px solid var(--rr-line);
    border-radius: var(--bo-radius);
    background-color: var(--rr-raised);
}

.bo .td-counts b {
    display: block;
    font-family: var(--rr-serif);
    font-size: 1.75rem;
    font-weight: 400;
    line-height: 1.1;
}

.bo .td-counts span {
    font-size: 0.8125rem;
    color: var(--rr-muted);
}

.bo .td-layout {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.bo .td-title {
    margin: 0 0 0.75rem;
    font-family: var(--rr-sans);
    font-size: 0.75rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--rr-muted);
}

.bo .td-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    min-height: 48px;
    margin-bottom: 0.5rem;
    padding: 0.5rem 0.875rem;
    border: 1px solid var(--rr-line);
    border-left: 4px solid var(--rr-accent);
    border-radius: 8px;
    background-color: var(--rr-raised);
    color: var(--rr-ink);
    text-decoration: none;
}

.bo .td-item.is-jade  { border-left-color: var(--bo-jade); }
.bo .td-item.is-amber { border-left-color: #C88A1E; }

.bo .td-line {
    position: relative;
    list-style: none;
    margin: 0;
    padding: 0 0 0 4.75rem;
}

.bo .td-line::before {
    content: "";
    position: absolute;
    left: 4rem;
    top: 0;
    bottom: 0;
    width: 2px;
    background-color: var(--rr-line);
}

.bo .td-row {
    position: relative;
    margin-bottom: 0.5rem;
}

.bo .td-row.is-past {
    opacity: 0.55;
}

.bo .td-time {
    position: absolute;
    left: -4.75rem;
    top: 0.875rem;
    width: 3.75rem;
    text-align: right;
    font-size: 0.8125rem;
    color: var(--rr-muted);
}

.bo .td-card {
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 0.5rem;
    min-height: 48px;
    padding: 0.5rem 0.875rem;
    border: 1px solid var(--rr-line);
    border-radius: 8px;
    background-color: var(--rr-raised);
    color: var(--rr-ink);
    text-decoration: none;
}

.bo .td-now {
    position: relative;
    height: 0;
    margin: 0.75rem 0 0.875rem -0.75rem;
    border-top: 2px solid var(--rr-accent);
}

.bo .td-now span {
    position: absolute;
    left: -4rem;
    top: -0.7rem;
    padding: 0 0.375rem;
    border-radius: 4px;
    background-color: var(--rr-accent);
    color: #FFFFFF;
    font-size: 0.75rem;
    font-weight: 700;
}

@media (min-width: 768px) {
    .bo .td-counts {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }
}

@media (min-width: 992px) {
    .bo .td-layout {
        display: grid;
        grid-template-columns: minmax(0, 1.4fr) minmax(0, 1fr);
        align-items: start;
    }

    /* Timeline on the left, Needs you on the right */
    .bo .td-timeline { grid-column: 1; grid-row: 1; }
    .bo .td-needs    { grid-column: 2; grid-row: 1; }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build FoodOrderingSystem.slnx`
Expected: `Build succeeded.`, no new warnings.

- [ ] **Step 5: TC-T01.** Make two bookings for today, one that has already ended or started earlier, and one later. Open **Today** from the menu.
  Expected: the greeting with the staff member's first name ("Good evening, Staff" for the seeded account), four count tiles with **no RM amounts anywhere**, the earlier booking faded, a plum NOW line between the two bookings, and the full Needs-you list with no cap of 5. Tapping a booking opens Floor on that table's area with its drawer open. Tapping a "no table" item opens The Pass. At 390 px, Needs you comes first and the timeline is below it.

- [ ] **Step 6: Commit (Jordan)**

Stage: `FoodOrderingSystem/Pages/Staff/Today/Index.cshtml`, `FoodOrderingSystem/Pages/Staff/Today/Index.cshtml.cs`, `FoodOrderingSystem/wwwroot/css/backoffice.css`
Message: `feat(UI): add staff Today run sheet`

---

### Task 12: Docs, progress log and report evidence

**Files:**
- Modify: `docs/progress.md` (new entry at the top of `## Log`; update the BUG8 row in Known issues)
- Modify: `docs/report-evidence.md` (under Jordan's section)

- [ ] **Step 1: Progress log entry.** Add at the top of `## Log` in `docs/progress.md`:

```markdown
- **2026-11-0X** — Jordan: **staff side** on `feature/UI-staff-floor` (off `feature/UI-back-office`; spec
  `docs/superpowers/specs/2026-11-01-staff-floor-design.md`). Research: StoreHub, FeedMe and Qashier open front of
  house on a table layout, GrabMerchant and FeedMe KDS open the kitchen on an order queue, and none show staff money,
  so D5 (2) is answered "no money totals for staff". New **Floor** (staff landing page): one tile per table, with
  state worked out by the new `FloorService` (Bill due, In use, Booked, Free), a **Needs-you** bar (late orders,
  bills due, ready to serve, unconfirmed or late bookings), and a drawer with one next-step button (Confirm booking,
  Mark served, Take payment → Wallace's Bill page, Complete table). **The Pass** replaces the Orders board view
  (same URL): a ticket rail New → Preparing → Ready → Served, with age chips and an allergy strip; the old board is
  under History. **Today**: read-only run sheet. Staff menu regrouped (Front of house, Kitchen, Counter, Guests).
  Partial refresh every 20 s (`staff-live.js`). **No schema change.** `FloorService` reads only through the other
  services. **Group OK in the PR:** `Program.cs` (1 line), `Login.cshtml.cs` (Carynn, staff land on Floor), Floor
  calling Wallace's `UpdateStatusByStaffAsync` and linking to his Bill page. Timers are "since placed": `Order` has
  no cooking or served time. Build clean, 0 new warnings. Manual TC-F01–F07, TC-P01, TC-T01 run on iPad
  landscape and portrait and 390 px (Edge device mode).
```
(Replace `0X` with the real day.)

- [ ] **Step 2: BUG8 row.** In the Known issues table, put this at the start of BUG8's description cell:
  `**(Fixed 0X/11, feature/UI-staff-floor: Floor + Today; no money for staff, per D5.)**`

- [ ] **Step 3: Report evidence.** Under Jordan's section in `docs/report-evidence.md`, add:

```markdown
### Staff side: Floor, The Pass, Today (UI, BUG8)
**Screenshots (Ch.5):** Floor on iPad landscape with a drawer open (Bill due table); Floor at 390 px with the bottom
sheet open; The Pass rail with one ticket per column and an allergy strip; Today with the NOW line; the Needs-you bar.
**Screenshots (Ch.6):** TC-F03 banner after Confirm booking; TC-F05 table back to Free; crafted-POST refusal banner;
offline footer "Couldn't refresh…".
**Test cases:** TC-F02 (each table state), TC-F05 (pay, then complete table), crafted Complete POST is refused.
**Ch.7 reflection:** researched Malaysian F&B POS apps before designing (table-first front of house, queue-first
kitchen, money kept from staff). One business service combines three teammates' services without touching the
database, which keeps the 3-tier rule. Found that paying doesn't move an order on, so "Complete table" also
closes paid orders. Order has only CreatedAt (UTC), so timers are "since placed" and are honest about it. Partial
refresh instead of a full reload, so an open drawer survives.
```

- [ ] **Step 4: Commit (Jordan)**

Stage: `docs/progress.md`, `docs/report-evidence.md`
Message: `docs: log staff floor, the pass and today`

---

## Changes made while executing (1 Nov)

- **Order times are local, not UTC.** `PlaceOrderAsync` saves `CreatedAt = DateTime.Now` (coding conventions); only the
  seed uses UTC. `OrderAgeDisplay` compares with `DateTime.Now`, and The Pass prints `CreatedAt` without
  `ToLocalTime()`. The "History shows UTC" issue this plan used to list was wrong: History is correct.
- **Orders were never linked to a booking.** Added to `OrderService.PlaceOrderAsync` (option A, agreed with Jordan):
  link to the customer's Pending/Confirmed booking today whose slot covers now (15 min early allowed), read through
  `IReservationService`. `OrderService` now also takes `IReservationService` in its constructor (no cycle:
  `ReservationService` does not use `IOrderService`).
- **Area tab order:** `FloorService` sorts areas by `DiningAreaId`, so Main Dining Hall is the default tab.
- **Verified end to end over HTTP** on a second copy of the app (port 5099), as customer@ and staff@: Booked
  (Pending dot, "not confirmed" chip) → Confirm → In use "Seated" → order linked to the table ("#4 ready · M1") →
  Mark served → Bill due RM 31.00 → crafted Complete refused ("Order #6 is not finished or not paid yet…") →
  cash on the Bill page → "Paid · ready to complete" → Complete table → Free, order gone from the rail. Today shows
  counts only, the past booking faded and the NOW line. **Not yet checked in a real browser:** TC-F06 (open drawer
  survives a refresh) and TC-F07 (iPad/phone layouts).
