# Ordering — PB14 (cart + place order) — Design

- **Owner:** Jordan (Domain C) · **Sprint:** 3 (22–29 Oct 2026) · **Status:** Draft 2026-10-22 — awaiting Jordan's review
- **Branch:** `feature/PB14-ordering` off `dev`, created **after** `feature/PB06-PB08-menu` is merged
- **Builds on:** menu spec `2026-10-18-menu-design.md`

## 1. Goal
| Item | Outcome |
|---|---|
| PB14 | A customer adds dishes to a cart from `/Menu`, reviews it, logs in if needed, and places an order. The order is saved as **Pending** with a price snapshot per line, ready for staff (PB15) and payment (PB16). |

**Primary flow (phone, at the table):** scan QR → `/Menu` → tap **Add** on dishes → sticky **View cart** bar →
`/Orders/Cart` (change quantities) → **Checkout** → log in / register if not signed in (cart is kept) →
`/Orders/Checkout` (notes, place order) → `/Orders/Confirmation/{id}` (order number, items, total, **Pay now** → PB16).

**Out of scope:** staff order screen + status changes (PB15), payment + receipt (PB16), order history (PB18),
promotions/discounts (see §9 Q3), notifications (PB20, see §9 Q4).

## 2. Constraints
- Strict 3-tier: pages → `ICartService` / `IOrderService` → `ApplicationDbContext`. Pages never touch the DbContext.
- **No schema change in v1.** Uses existing `Order` (CustomerId, ReservationId?, Status, TotalAmount, Notes, CreatedAt)
  and `OrderItem` (OrderId, MenuItemId, Quantity, UnitPrice). A table link needs a schema change → decision D5 (§9).
- No new NuGet packages (session is built into ASP.NET Core).
- `Order.CustomerId` is required, so **placing** an order needs a logged-in customer. Browsing and the cart do not.
- Forms bind to input models in `ViewModels/`. Prices and totals are **never** taken from the form.

## 3. Cart storage — server session
| Option | Verdict |
|---|---|
| **Session (chosen)** | Server-side, works before login, survives the login redirect, same mechanism as the labs (`HttpContext.Session`). Lost if idle 60 min — acceptable for a meal. |
| Database cart table | Needs a schema change. Overkill for a cart that lives for one meal. |
| Browser localStorage + JS | Prices would live in the browser; every value must be re-checked anyway. More JavaScript to explain in the viva. |

The session stores **only** `MenuItemId` + `Quantity` as JSON under key `"Cart"`. Names, prices, photos and
availability are read fresh from the database every time the cart is shown, so a price change or a
dish going sold out is picked up immediately.

## 4. Components
| Layer | File | Responsibility |
|---|---|---|
| Business | `Services/ICartService.cs`, `CartService.cs` | Read/write the session cart; build the cart view with live prices; add / change quantity / remove / clear |
| Business | `Services/IOrderService.cs`, `OrderService.cs` | Place an order from cart lines (re-validate, snapshot prices, compute total, save Pending); load one order for its owner |
| ViewModels | `CartSessionItem` | `MenuItemId`, `Quantity` — the only thing stored in session |
| ViewModels | `CartLineViewModel`, `CartViewModel` | Cart page rows with live name/price/photo/availability, line totals, grand total, item count |
| ViewModels | `CheckoutInput` | `Notes` (≤ 500) — the only field the customer types at checkout |
| ViewModels | `OrderConfirmationViewModel`, `OrderLineViewModel` | Confirmation page: order no., time, status, lines, total, notes |
| Pages | `Pages/Orders/Cart` | Anonymous. Shows the cart; POST handlers `Add`, `Increase`, `Decrease`, `Remove` |
| Pages | `Pages/Orders/Checkout` | `[Authorize(Roles = Customer)]`. Read-only summary + notes + **Place order** |
| Pages | `Pages/Orders/Confirmation` | `[Authorize(Roles = Customer)]`, `{id:int}`. Owner only, otherwise 404 |
| Pages | `Pages/Menu/Index` (edit, **after the menu PR is merged**) | **Add** button per available dish (POST to `/Orders/Cart?handler=Add`), sticky **View cart · n items · RM x** bar |

## 5. Service contract (summary)
| Method | Notes |
|---|---|
| `ICartService.GetCartAsync(ISession)` | Joins session lines with `MenuItems`; drops lines whose dish was deleted; flags sold-out lines |
| `ICartService.AddItemAsync(ISession, menuItemId, quantity)` | Refuses unknown or sold-out dishes; merges with an existing line; caps at 20 |
| `ICartService.ChangeQuantity(ISession, menuItemId, delta)` | +1 / −1; reaching 0 removes the line |
| `ICartService.RemoveItem(ISession, menuItemId)`, `Clear(ISession)`, `GetItems(ISession)`, `GetItemCount(ISession)` | |
| `IOrderService.PlaceOrderAsync(customerId, lines, notes)` | Returns `(bool Success, string Message, int? OrderId)` |
| `IOrderService.GetConfirmationAsync(orderId, customerId)` | `null` if missing **or owned by someone else** → page returns 404 |

The session is passed in by the page (`HttpContext.Session`) rather than injected, so the service has no
hidden dependency and no extra registration (`IHttpContextAccessor`) is needed.

## 6. Business rules
| Rule | Where |
|---|---|
| Quantity per line 1–20; a cart holds at most 30 different dishes | `CartService` |
| Only available dishes in an active category can be added | `CartService` |
| Empty cart cannot check out | Checkout page + `OrderService` |
| At placement every dish is re-checked: still exists, still available, category still active. If any fail, nothing is saved and the message names them | `OrderService` |
| `OrderItem.UnitPrice` = the dish's price **at placement**, read from the database | `OrderService` |
| `TotalAmount` = Σ `UnitPrice × Quantity`, computed on the server | `OrderService` |
| New order: `Status = Pending`, `CreatedAt = DateTime.Now` (restaurant local time, per conventions) | `OrderService` |
| Order + its items saved in **one** `SaveChangesAsync` → one database transaction, all or nothing | `OrderService` |
| Cart cleared only after the save succeeds | Checkout page |
| A customer can only view their own order (id from the URL is checked against the logged-in user) | `OrderService` |

## 7. UI / responsive design
Phone-first, like `/Menu`; Bootstrap breakpoints only, no screen-detection script.
- **`/Menu` additions:** small **Add** button on each available card (sold-out cards keep the badge, no button);
  after adding, return to the same category/search with a banner "Added Nasi Lemak to your cart."
  Sticky bottom bar on phones when the cart is not empty: **View cart · 3 items · RM 45.00**.
- **`/Orders/Cart`:** one row per dish — photo, name, unit price, `−  2  +` stepper (each a tiny POST form, 40px tap
  targets), line total, Remove. Sold-out lines are greyed with "No longer available — remove to continue".
  Footer: total + **Checkout** (disabled while a sold-out line remains) + "Add more dishes".
  Empty state: "Your cart is empty" + Browse menu.
- **`/Orders/Checkout`:** read-only summary, Notes textarea ("No chilli, table by the window…"), **Place order**.
  Not logged in → standard Identity login with `returnUrl=/Orders/Checkout`; the session cart is still there.
- **`/Orders/Confirmation/{id}`:** "Order #1024 placed", time, status badge **Pending**, lines, total, notes,
  **Pay now** (links to PB16, hidden until PB16 exists) and **Back to menu**.
- Money always `RM 0.00`; time `hh:mm tt`; date `dd/MM/yyyy`.

## 8. Shared-file changes — need group agreement
| File | Change |
|---|---|
| `Program.cs` | `AddDistributedMemoryCache()`; `AddSession(o => { IdleTimeout = 60 min; Cookie.HttpOnly = true; Cookie.IsEssential = true; })`; `AddScoped<ICartService, CartService>()`; `AddScoped<IOrderService, OrderService>()`; `app.UseSession()` after `UseRouting()` |
| `_Layout.cshtml` | (optional) cart icon with item count in the navbar — can wait; the `/Menu` bar covers phones |

Session is also usable by teammates later (e.g. a booking wizard), so it is a reasonable shared addition.

## 9. Open questions
| # | Question | Recommendation |
|---|---|---|
| **D5** (group) | How does an order know **which table** it is for? `Order` has only `ReservationId?`. A walk-in who scans the QR has no reservation. | Add nullable `int? RestaurantTableId` (FK → `RestaurantTables`) to `Order` in one small migration; QR encodes `/Menu?table=5`, kept in session and saved on the order. Staff (PB15) then see the table number. **Fallback with no schema change:** an optional "Table number" field written into `Notes`. |
| Q2 | Link the order to today's reservation automatically? | Later: once Wallace's PB09–11 is merged, call `IReservationService` to find the customer's reservation for today and set `ReservationId`. |
| Q3 | Apply Carynn's promotions (`DiscountPercent`) at checkout? | Not in v1. If wanted, PB16 asks `IPromotionService` for the active promotion; still no schema change because `TotalAmount` stores the final figure. |
| Q4 | Notify the customer/staff when an order is placed (PB20)? | Agree `INotificationService.SendAsync(userId, type, message)` with Wallace now; call it once it exists. |
| Q5 | Can a customer cancel? | Allowed while Pending (architecture rule). Build it with PB18 order history, not PB14. |

## 10. Testing (manual, written up in `docs/report-evidence.md`)
| ID | Steps | Expected | Actual (22/10/2026, HTTP-level) |
|---|---|---|---|
| TC-O01 | Logged out, add 2 dishes, open cart | Both lines, correct totals, no login asked | Pass — 20 × RM 14.90 + RM 18.50 = RM 316.50 shown |
| TC-O02 | In the cart press + to 20, then + again | Stops at 20 with a message | Pass — "You can order up to 20 of each dish." |
| TC-O03 | Press − on a line with quantity 1 | Line removed | Pass |
| TC-O04 | Logged out, press Checkout | Login page; after login back at Checkout with the same cart | Pass — `ReturnUrl=/Orders/Checkout`, cart kept |
| TC-O05 | Place the order | Confirmation shows #id, Pending, correct total; cart now empty; DB has Order + OrderItems with UnitPrice snapshot | Pass — RM 357.00, Pending, notes saved, 2 OrderItems, cart empty |
| TC-O06 | Dish set sold out while in a cart, then checkout | Line greyed, Checkout disabled; crafted POST refused, nothing saved | Pass — GET and POST both redirect to cart; 0 orders in DB |
| TC-O07 | Price changed after the order | Old order still shows the old price | Pass — price set to 99.00, order still RM 18.50 |
| TC-O08 | Another customer's order id / missing id | 404 | Pass — 404 both |
| TC-O09 | Staff account opens `/Orders/Checkout` | Access denied | Pass — redirected to AccessDenied |
| TC-O10 | Crafted Add with unknown id / quantity 999 | Refused / capped | Pass — "That dish could not be found." / capped to 20 |

Tested on a scratch copy with the §8 `Program.cs` lines added (the real `Program.cs` is unchanged until
the group agrees). Test data removed afterwards (0 orders). **Still to do in a browser:** pages at 375px,
768px, 1280px, and the `/Menu` Add buttons once built. `dotnet build`: no new warnings.

## 11. Build order
1. Services + view models (no shared files touched) → 2. Cart / Checkout / Confirmation pages →
3. group OK for §8 + D5 → 4. `Program.cs` lines → 5. `/Menu` Add buttons + cart bar (after the menu PR merges) →
6. manual tests → 7. update `product-backlog.md`, `progress.md`, `report-evidence.md`.
