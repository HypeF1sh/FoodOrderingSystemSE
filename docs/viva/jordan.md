# Viva guide: Jordan (Domain C: Menu, Orders and Payment)

**Find any file fast:** every code file starts with a header line, `// PBxx (Owner) - what it does`.
In Visual Studio press **Ctrl+Shift+F** and search for `PB14` (for example) to list every file for that item.

## The 3 tiers in one picture (same for every PB)

```
Browser -> Page (Pages/.../X.cshtml + X.cshtml.cs)        Presentation: shows data, reads the form
        -> Service interface (Services/IXService.cs)       Business: rules, status changes, checks
        -> Service class (Services/XService.cs)
        -> ApplicationDbContext (Data/) -> SQL Server      Data: EF Core only, no SQL strings
```
- A page model **never** uses the DbContext. It calls a service through its interface (injected in the constructor).
- Services are registered in `Program.cs` as `AddScoped` (one instance per web request).
- Forms bind to **input models** in `ViewModels/`, not to entities (stops over-posting).
- Every POST: `ModelState.IsValid` -> service -> error on the form, or `TempData` message + redirect (Post/Redirect/Get).

---

## PB06: Admin manages the menu

| | |
|---|---|
| Pages | `Pages/Admin/Menu/` (Index, CreateItem, EditItem, DeleteItem, CreateCategory, EditCategory, DeleteCategory) |
| Service | `Services/MenuService.cs` (`IMenuService`), `Services/ImageStorageService.cs` |
| Models | `Models/MenuItem.cs`, `Models/MenuCategory.cs`, input models `ViewModels/MenuItemInput.cs`, `MenuCategoryInput.cs` |
| Seed photos | `wwwroot/images/menu/`, `Data/DbSeeder.cs` step 6 |

**How it works**
- `CreateItemAsync` / `UpdateItemAsync`: check the name is unique in its category, price is valid, then save.
- Photo upload: `ImageStorageService.ValidateImage` (JPG/PNG/WebP, max 2 MB, checks extension **and** content type) then `SaveMenuImageAsync` saves it with a **GUID file name** in `wwwroot/uploads/menu/`.
- `ToggleItemAvailabilityAsync`: sold out / available, without deleting.
- Delete rules: `GetItemDeleteBlockReasonAsync` blocks deleting a dish that was ever ordered (old orders and receipts must stay correct). A category with dishes cannot be deleted.

```csharp
// MenuService: a dish that appears on any order is kept
var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.MenuItemId == menuItemId);
```

**Examiner questions**
- *Why a GUID file name?* The user's file name never reaches the disk, so it cannot overwrite another file or contain `../`.
- *Why not delete an ordered dish?* `OrderItem` points to it; receipts and reports would break.
- *Where do seed photos come from on a new PC?* They are committed in `wwwroot/images/menu`; `DbSeeder` links them by dish name, only when the dish has no photo, so an admin upload is never replaced.

## PB08: Customer browses the menu

| | |
|---|---|
| Page | `Pages/Menu/Index.cshtml(.cs)`, script `wwwroot/js/menu-cart.js` |
| Service | `MenuService.GetMenuForBrowseAsync(categoryId, search)` |

- Shows only **active categories**; sold-out dishes appear greyed out and cannot be added.
- Tabs filter by category, the search box filters by name/description; both are query-string values, so the page can be bookmarked.
- `menu-cart.js`: "Add" opens a quantity dialog and adds by `fetch` (no page reload), then shows a toast.

## PB14: Customer places an order (cart, checkout, pre-orders, table QR)

| | |
|---|---|
| Pages | `Pages/Orders/Cart`, `Checkout`, `Confirmation`, `Table` (QR landing), `Pages/Staff/TableQr` (print codes) |
| Services | `CartService`, `OrderService.PlaceOrderAsync` / `DescribeOrderTargetAsync`, `TableQrService` |
| Models | `Order` (`ReservationId`, `RestaurantTableId`), `OrderItem` (`UnitPrice`) |

**Cart** (`CartService`): the session stores **only dish ids and quantities**. Prices are re-read from the database every time, so a customer cannot change a price in the browser.

**Where does an order go?** `OrderService.FindOrderTargetAsync`, first match wins:
1. The phone scanned a table QR (valid 3 h) -> order goes to that table.
2. The customer has a booking happening now -> order goes to that booking's table.
3. A **Confirmed** booking later -> saved as a **Held pre-order**; staff send it to the kitchen when the guest arrives.
4. Otherwise **refused**: "scan the QR on your table, or book a table". A Pending booking gets its own message.

**PlaceOrderAsync** re-checks every dish (still available, category active), saves each line with `UnitPrice` = today's price, applies a valid promo code, and empties the cart only after the save succeeds.

**Table QR** (`TableQrService`): the QR link holds a **signed code**, not the table number.
```csharp
public string CreateCode(int restaurantTableId) => _protector.Protect(restaurantTableId.ToString());
```
ASP.NET Data Protection signs it, so typing `?code=3` or editing the link fails.

**Examiner questions**
- *Why save UnitPrice on OrderItem?* A later price change must not change old orders or receipts.
- *Why a signed QR code?* With a plain id anyone could order to any table from home.
- *Why is the scan kept in the session for 3 hours?* Long enough for a meal; after that an old screenshot of the link stops working.

## PB15: Staff receive orders and update status (The Pass)

| | |
|---|---|
| Pages | `Pages/Staff/Orders/Index` (The Pass), `Cancel`, `Pages/Staff/Floor/Index`, script `wwwroot/js/staff-live.js` |
| Service | `OrderService.AdvanceStatusAsync`, `CancelOrderAsync`, `FloorService` |

```csharp
private static OrderStatus? GetNextStatus(OrderStatus status) => status switch
{
    OrderStatus.Held => OrderStatus.Pending,   // staff send a pre-order to the kitchen
    OrderStatus.Pending => OrderStatus.Preparing,
    OrderStatus.Preparing => OrderStatus.ReadyToServe,
    OrderStatus.ReadyToServe => OrderStatus.Served,
    OrderStatus.Served => OrderStatus.Completed,
    _ => null   // Completed and Cancelled are final
};
```
- Status only moves **one step forward**; the page sends the status it saw, and if someone else already moved it, the service refuses ("already updated by someone else"). That stops two iPads double-advancing.
- Cancel only from Held, Pending or Preparing (`CanBeCancelled`).
- The customer gets a notification at each step (PB20).
- Floor (`FloorService`): one tile per table with its state (free, booked, seated, bill due...), a "Needs you" list, and "Complete table".
- `staff-live.js` refreshes the boards every 20 s.

## PB16: Customer pays online and tracks payment

| | |
|---|---|
| Pages | `Pages/Payments/Pay`, `Receipt` |
| Services | `PaymentService.PayOnlineAsync`, `SimulatedPaymentGateway` (`IPaymentGateway`) |
| Model | `Payment` (one order can have several attempts) |

- Card checks before the gateway: **Luhn** check on the number, expiry not in the past, CVV format.
- `LockOrderAsync` takes a row lock on the order inside a transaction, so two clicks cannot pay twice.
- A failed attempt is saved as **Failed** and never overwritten; a later success adds a new **Paid** row.
- The amount always comes from `Order.TotalAmount` in the database, never from the form.
- The gateway is simulated (no real money): `IPaymentGateway` lets a real one be swapped in without touching the service.

## PB17 (shared with Wallace): staff bill and cash
- `PaymentService.RecordCashPaymentAsync`: tendered must be >= total (and not absurdly more); the change is shown on the receipt.
- `RefundPaymentAsync`: Paid -> Refunded.

## PB18: Order history and live status

| | |
|---|---|
| Page | `Pages/Orders/Index`, script `wwwroot/js/my-orders.js` |
| Service | `OrderService.GetCustomerOrdersAsync`, `PaymentService.GetPaymentSummariesAsync` |

- Every 15 s the script calls `/Orders?handler=Status` (JSON) and updates the badges, so the status is near real time without reloading.

## PB22: Admin dashboard and reports (+ PB24 export button)

| | |
|---|---|
| Pages | `Pages/Admin/Dashboard/Index`, `Dishes`, script `wwwroot/js/admin-dashboard.js` (Chart.js) |
| Service | `ReportService.GetDashboardAsync`, `GetDishSalesAsync` |

- Date filter (max 366 days). Groups by day, week or month depending on the range.
- Sales use **Paid** payments only; refunds are excluded. Popular dishes count non-cancelled order lines.
- Inclusive date range: `>= from 00:00` and `< to + 1 day`, so the last day's evening is included.

## PB27: AI chatbot (Gemini)

| | |
|---|---|
| Files | `Pages/Shared/_ChatWidget.cshtml`, `wwwroot/js/chatbot.js`, `Pages/Chatbot/Index.cshtml.cs`, `Services/ChatbotService.cs`, `Services/GeminiClient.cs` |

- **Progress in real time:** the endpoint streams **Server-Sent Events**: status lines ("Checking the menu...") while tools run, then the answer piece by piece.
- **Stop:** `AbortController` in the browser; the server sees `HttpContext.RequestAborted` and cancels the Gemini call.
- **Tools:** Gemini cannot see the database. It asks for a tool (menu, hours, offers, info, my bookings, my orders); `ChatbotService` runs it through the normal services and sends back the result. So it cannot invent prices and only sees the customer's own data.
- **Safety:** answers are inserted with `textContent` (never `innerHTML`); only 4 known page paths become links; 1000 chars per message, 40 questions per session; anti-forgery token checked.
- **Key:** in user-secrets (`Gemini:ApiKey`), never in git. Free tier = 15 requests/min, so one automatic retry with a countdown.

## UI work (not a PB, but you may be asked)
- Landing page `Pages/Index`: photo slideshow, dishes, hours, offers.
- Back office layout, staff top bar, Floor / Tables / Today pages, sign-in pages, logo.

## Known limits (be honest if asked)
- Payment gateway is simulated.
- Table QR codes are signed with this PC's keys: codes printed on one PC do not work on another.
- Live status uses polling (15-20 s), not SignalR.
