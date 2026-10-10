# Architecture

## 3-tier layering
```
Browser
   │
Presentation  — Razor Pages (.cshtml + PageModel), view/input models, Bootstrap UI
   │  calls interfaces only
Business      — Services (IMenuService → MenuService …), rules, status transitions
   │  uses
Data          — AppDbContext, entity classes, EF Core migrations
   │
SQL Server (LocalDB in development)
```
Dependency direction is one-way: Presentation → Business → Data. Nothing points upward.

## Solution layout
**Decision pending (see progress.md, D2).** Two options:

**Option 1 — three projects (recommended).** The compiler enforces the layers, which is the
strongest evidence for the 5-mark architecture criterion and easy to show in the viva.
```
FoodOrderingSystem.slnx
FoodOrderingSystem/            Presentation — Razor Pages web app (startup project)
FoodOrderingSystem.Business/   Business — services, interfaces, view models
FoodOrderingSystem.Data/       Data — AppDbContext, entities, enums, migrations
```
References: Web → Business → Data. EF commands then need
`--project FoodOrderingSystem.Data --startup-project FoodOrderingSystem`.

**Option 2 — one project, layered folders.** Simpler, layering enforced by discipline only.
```
FoodOrderingSystem/
├── Pages/        Presentation
├── Models/       view and input models
├── Services/     Business (interfaces + implementations)
└── Data/         AppDbContext, Entities/, Enums/, Migrations/
```

## Entities
| Entity | Key fields | Owner |
|---|---|---|
| User (or Identity user) | Id, FullName, Email, Phone, Role, IsActive, CreatedAt | A |
| DiningArea | Id, Name, Description | A |
| RestaurantTable | Id, DiningAreaId, TableNumber, Capacity, IsActive | A |
| OperatingHours | Id, DayOfWeek, OpenTime, CloseTime, IsClosed | A |
| Promotion | Id, Title, Description, DiscountPercent, StartDate, EndDate, IsActive | A |
| Announcement | Id, Title, Body, PublishedAt, IsActive | A |
| RestaurantSetting | Id, Key, Value | A |
| Reservation | Id, CustomerId, TableId, ReservationDate, StartTime, EndTime, PartySize, Status, SpecialRequests, CreatedAt | B |
| Feedback | Id, CustomerId, ReservationId?, OrderId?, Rating (1–5), Comment, CreatedAt | B |
| Notification | Id, UserId, Type, Message, IsRead, CreatedAt | B |
| MenuCategory | Id, Name, Description, DisplayOrder, IsActive | C |
| MenuItem | Id, MenuCategoryId, Name, Description, Price, ImageUrl, IsAvailable | C |
| Order | Id, CustomerId, ReservationId?, Status, TotalAmount, Notes, CreatedAt | C |
| OrderItem | Id, OrderId, MenuItemId, Quantity, UnitPrice | C |
| Payment | Id, OrderId, Amount, Method, Status, TransactionRef, PaidAt, RecordedByUserId? | C |

`OrderItem.UnitPrice` snapshots the price at ordering time so past orders stay correct when prices change.

## Status enums
| Enum | Values |
|---|---|
| ReservationStatus | Pending, Confirmed, Completed, Cancelled, NoShow |
| OrderStatus | Pending, Preparing, ReadyToServe, Served, Completed, Cancelled |
| PaymentStatus | Unpaid, Paid, Failed, Refunded |
| PaymentMethod | Card, EWallet, Cash |
| UserRole | Administrator, Staff, Customer |

Allowed order transitions: Pending → Preparing → ReadyToServe → Served → Completed.
Cancelled is allowed from Pending or Preparing only.

## Reservation rules (agreed)
| Rule | Value |
|---|---|
| Slot length | Fixed 90 minutes |
| Turnover buffer | 15 minutes between bookings |
| Table choice | Customer picks from available tables |
| Confirmation | Created as Pending, staff confirms |
| Multi-table bookings | Not supported |

A table is **unavailable** for a requested slot if any non-cancelled reservation on it satisfies:
```
existing.Start < requested.End + buffer  AND  existing.End + buffer > requested.Start
```
Overlap, not exact-time matching. A 7:00pm booking blocks a 7:45pm request.

## Decisions log
| # | Decision | Status |
|---|---|---|
| 1 | Razor Pages on .NET 10, SQL Server via EF Core code-first | Agreed |
| 2 | One project vs three projects (see above) | **Pending** |
| 3 | Authentication: ASP.NET Core Identity vs custom cookie auth | **Pending — Carynn** |
| 4 | Dev database: SQL Server LocalDB per developer, schema shared via migrations | Proposed |
