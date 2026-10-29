# Staff orders — PB15 (receive orders + update status) — Design

- **Owner:** Jordan (Domain C) · **Sprint:** 3 (22–29 Oct 2026) · **Status:** Draft 2026-10-22 — awaiting Jordan's review
- **Branch:** `feature/PB15-staff-orders` (after PB14) · **Builds on:** `2026-10-22-ordering-design.md`

## 1. Goal
| Item | Outcome |
|---|---|
| PB15 | Staff see incoming orders on an iPad/PC, move each one through Pending → Preparing → Ready to serve → Served → Completed, and can cancel an order while it is Pending or Preparing. |

Covers requirements 2d (receive, update, process orders) and 2e (the five statuses). Customer-side live
tracking is PB18.

## 2. Constraints
- Pages → `IOrderService` → `ApplicationDbContext`. No schema change, no new packages.
- `/Staff` already has the `StaffOrAdmin` folder policy; each PageModel also gets
  `[Authorize(Roles = "Staff,Administrator")]`.
- Status transitions are enforced **in the service** (architecture rule), never by the page.

## 3. Components
| Layer | File | Responsibility |
|---|---|---|
| Business | `IOrderService` / `OrderService` (extended) | `GetOrdersForStaffAsync(date, filter)`, `GetOrderForStaffAsync(id)`, `AdvanceStatusAsync(id, currentStatus)`, `CancelOrderAsync(id, currentStatus)` |
| ViewModels | `StaffOrderViewModel` | Order no., time, customer name, reservation no., status, lines, notes, total, next status, can-cancel flag |
| ViewModels | `StaffOrderFilter` | Active · Completed · Cancelled · All (UI filter, not stored) |
| ViewModels | `OrderStatusDisplay` | One place for status labels ("Ready to serve") and button text ("Mark served") |
| Pages | `Pages/Staff/Orders/Index` | Order board: date picker, filter pills, order cards, one "next step" button per card |
| Pages | `Pages/Staff/Orders/Cancel` | Confirmation page for cancelling (destructive action rule) |
| Pages | `Pages/Orders/_OrderStatusBadge` | Status badge partial, shared by the staff board and the customer confirmation page |

## 4. Business rules
| Rule | Where |
|---|---|
| Next status: Pending → Preparing → ReadyToServe → Served → Completed. No skipping, no going back | `OrderService.GetNextStatus` |
| Cancel only from Pending or Preparing | `OrderService` |
| **Stale-click guard:** the button posts the status the staff member *saw*. If the order has moved on (another staff member clicked first), nothing changes and the message says so — prevents double-advancing an order | `OrderService` |
| Active board = Pending, Preparing, ReadyToServe, Served, **oldest first** (kitchen works first-in, first-out). Completed/Cancelled/All = newest first | `OrderService` |
| Board shows one day at a time, default today (restaurant local time) | Page + service |
| Customer name is read through the order's own `Customer` navigation (display only, no writes to Domain A tables) | `OrderService` |

## 5. UI
- Tablet/PC first but works on a phone: cards in 1 / 2 / 3 columns (`< 768`, `≥ 768`, `≥ 1200`).
- Card: header `#12 · 07:32 PM` + status badge; body customer, reservation no. if any, item list with quantities,
  notes highlighted (allergies!), total; footer **next-step button** (`btn-primary`) + **Cancel** (`btn-outline-danger`, only when allowed).
- Filter pills with counts are left out of v1; plain pills: Active · Completed · Cancelled · All.
- **Auto-refresh:** the Active view for today reloads every 30 s (a few lines of vanilla JS) so new orders appear
  without pressing F5. SignalR would be "true" real time but adds complexity to explain — noted for Ch.7.

## 6. Open questions
| # | Question | Recommendation |
|---|---|---|
| Q1 | Staff navbar link to `/Staff/Orders` | `_Layout.cshtml` is shared — add with the Menu link request |
| Q2 | Show the table number on each card | Depends on D5 (`Order.RestaurantTableId`) |
| Q3 | Notify the customer on each status change (PB20) | Call `INotificationService` once agreed with Wallace |

## 7. Testing
| ID | Steps | Expected | Actual |
|---|---|---|---|
| TC-S01 | Staff opens `/Staff/Orders` | Today's active orders, oldest first | Pass (22/10) — notes highlighted, 30 s refresh on |
| TC-S02 | Press the next-step button 4 times on one order | Pending → Preparing → Ready to serve → Served → Completed; leaves the Active view at the end | Pass — then a 5th press: "is completed and cannot move on" |
| TC-S03 | Two tabs show the same Pending order; press "Start preparing" in both | Second press: "already updated" message, order is Preparing (not Ready) | Pass — "already updated by someone else — it is now preparing", DB Preparing |
| TC-S04 | Cancel a Pending order via the confirmation page | Cancelled; appears under Cancelled | Pass |
| TC-S05 | Try to cancel a Served order (crafted POST) | Refused with message | Pass — button disabled, POST refused, DB still Served |
| TC-S06 | Customer opens `/Staff/Orders` | Access denied | Pass — customer AccessDenied, anonymous to Login |
| TC-S07 | Pick yesterday's date | Yesterday's orders only | Pass — no auto-refresh on a past day |

Also checked: missing order id → 404 / "could not be found"; the customer confirmation page shows the
same "Ready to serve" badge. HTTP-level tests on the scratch copy (session lines added there only); test
orders removed afterwards. **Still to do in a browser:** card layout at 768px / 1280px.
