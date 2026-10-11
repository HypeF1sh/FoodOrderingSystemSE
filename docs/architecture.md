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

## Solution layout — one project, layered folders (decided 2026-10-10)
Same structure as the lecturer's labs (`StaffAccountSystem` etc.). The 3-tier separation comes from
the **Services layer** and the rule that pages never touch the DbContext.

```
FoodOrderingSystem/                 web project
├── Pages/          Presentation — Razor Pages grouped by feature folder
├── ViewModels/     Presentation — input/view models that forms bind to
├── Services/       Business — I<Name>Service interfaces + <Name>Service implementations
├── Models/         Data — entity classes (as in the labs)
├── Data/           Data — ApplicationDbContext
├── Migrations/     Data — EF Core migrations (as in the labs)
└── wwwroot/        static files, Bootstrap in lib/
```

Three separate projects (Web / Business / Data) were considered and rejected: no lab uses that
structure, ASP.NET Core Identity scaffolding assumes a single project, and every EF command would
need `--project` / `--startup-project`. This trade-off is worth a paragraph in Chapter 2.

The labs themselves are **not** 3-tier: their PageModels query `ApplicationDbContext` directly.
Adding the Services layer is the deliberate difference — explain this in Chapter 2 and the viva.

## Entities
| Entity | Key fields | Owner |
|---|---|---|
| ApplicationUser (extends IdentityUser) | FullName, Phone, IsActive, CreatedAt (+ Identity's Id, Email, PasswordHash) | A |
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

Keys follow the newest lab: `[Key] public int <Entity>Id` (`MenuItemId`, `OrderId`), except
`ApplicationUser`, whose string `Id` comes from Identity.

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
| 2 | One project with layered folders, matching the labs | Agreed |
| 3 | Authentication: **ASP.NET Core Identity** with roles Administrator / Staff / Customer | Chosen by Jordan — confirm with Carynn |
| 4 | Dev database: SQL Server LocalDB per developer, connection string `DefaultConnection` (as in the labs) | Proposed |
| 5 | UI direction: modern minimal — see `coding-conventions.md` | Agreed |
