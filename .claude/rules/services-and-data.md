---
paths:
  - "**/Services/**/*.cs"
  - "**/Data/**/*.cs"
  - "**/Models/**/*.cs"
  - "**/Migrations/**/*.cs"
---

# Business and data layer rules

Source of truth: `docs/architecture.md` (layering, entity list, enums, rules) and
`docs/coding-conventions.md` (naming and style). If this file and those disagree, the docs win —
fix this file.

## Layering
- Dependency direction is one-way: Presentation -> Business -> Data. Nothing points upward.
- Only the Data layer touches the database. A PageModel never injects `ApplicationDbContext`.
- No string-concatenated SQL, ever.

## Services
- Every service is an interface (`IMenuService`) plus an implementation (`MenuService`),
  registered in `Program.cs` as **scoped**.
- Services live in `Services/`. One public class or interface per file, file name matches it.
- Every database call is `async`/`await`; async methods end in `Async`
  (`GetAvailableItemsAsync`, `CreateOrderAsync`).
- Services own business rules, validation beyond simple field checks, and status transitions.
- Services return something the page can act on — an entity for read-only display, a view model,
  a `bool`, or a small result type carrying an error message. Don't throw exceptions for normal
  validation failures.
- XML doc comments (`///`) on public service interface methods.
- Services may call another domain's service through its interface; they never read or write
  another domain's tables directly.
- Guard clauses and early returns over deep nesting. No magic numbers or strings — use the enums,
  constants or settings (e.g. the 90-minute slot length).

## Status transitions
Enforce these inside the service, checking the current status allows the move
(from `docs/architecture.md`):
- `OrderStatus`: Pending -> Preparing -> ReadyToServe -> Served -> Completed.
  Cancelled is allowed from **Pending or Preparing only**.
- `ReservationStatus`: Pending, Confirmed, Completed, Cancelled, NoShow.
- `PaymentStatus`: Unpaid, Paid, Failed, Refunded. An Order may have several Payment rows
  (a failed attempt followed by a successful one) — never overwrite a failed attempt.
- A table is unavailable for a requested slot when any non-cancelled reservation on it satisfies
  `existing.Start < requested.End + buffer && existing.End + buffer > requested.Start`
  (90-minute slot, 15-minute buffer). Overlap, not exact-time matching.

## Entities
- Entity classes live in **`Models/`** (as in the labs), not in a `Data/` or `Entities/` folder.
  Enums live in `Models/Enums/`.
- Key is `[Key] public int <Entity>Id` — `MenuItemId`, `OrderId`, `OrderItemId`. **Not** plain `Id`.
  The one exception is `ApplicationUser`, whose `Id` is a **string** supplied by Identity.
- Any link to a user is therefore a `string`: `CustomerId`, `UserId`, `RecordedByUserId`.
- Every relationship has an explicit foreign key property (`MenuCategoryId`) plus a navigation
  property. Navigation properties go at the **bottom** of the class under a
  `// Navigation properties` comment.
- Money and totals: `decimal` with `[Column(TypeName = "decimal(10,2)")]`. Never `double`.
- `OrderItem.UnitPrice` stores the price **at the time of ordering** — never read the current
  menu price for a past order.
- Statuses are C# **enums**, not magic strings. (The labs use `string` literals such as
  `"Pending"`; we deliberately differ — enums avoid typo bugs and the rubric favours them.)
- Timestamps are `DateTime` in local restaurant time, named `CreatedAt`, `UpdatedAt`, `PaidAt`.
- Nullable reference types are on: strings default to `= string.Empty;`, collections to
  `= new List<T>();`, optional strings are `string?`. No `!` without a comment saying why.
- Validation annotations on the entity as in the labs: `[Required(ErrorMessage = "...")]` with a
  written-out message, `[Display(Name = "...")]`, `[StringLength(n)]`, `[Range(min, max)]`,
  `[Index(nameof(X), IsUnique = true)]` on the class for unique columns.

## DbContext
- `Data/ApplicationDbContext.cs`, class **`ApplicationDbContext`** — this exact name, as in the
  labs and in Carynn's Sprint 1 task. (`docs/architecture.md` writes `AppDbContext` in the layering
  diagram; that is a typo in the doc, the real name is `ApplicationDbContext`.)
- It inherits `IdentityDbContext<ApplicationUser>` because the project uses ASP.NET Core Identity.
- One `public DbSet<T> Ts { get; set; }` per entity, plural property name (`MenuItems`, `Orders`).
- `OnModelCreating` is used only for what annotations cannot express — mainly
  `.HasConversion<string>()` on every status enum so the database reads clearly in SSMS, plus
  composite indexes and delete behaviour. Call `base.OnModelCreating(builder)` first (Identity
  needs it). The labs have no `OnModelCreating`; this is a deliberate, explainable difference.
- Read-only queries use `AsNoTracking()`. Load related data with explicit `Include()` /
  `ThenInclude()` — no lazy loading.
- Queries are `FirstOrDefaultAsync`, `AnyAsync` (duplicate checks), `ToListAsync`, always with an
  `OrderBy` / `OrderByDescending`.

## Migrations
- Schema changes only through migrations, never by editing the database in SSMS.
- Run them from the Package Manager Console: `Add-Migration <Name>` then `Update-Database`.
- One migration per logical change, named PascalCase for the change: `InitialCreate`,
  `AddCoreSchema`, `AddOrderStatus`.
- Never edit a migration that has already been pushed. Add a new one.
- Connection string is named `DefaultConnection` in `appsettings.json`, LocalDB per developer.
  No secrets in code — API keys go in user-secrets.

## Shared files — stop and ask
`Program.cs`, `Data/ApplicationDbContext.cs`, anything in `Migrations/`, `appsettings.json` and
`Pages/Shared/_Layout.cshtml` are shared. They need group agreement before editing — say so and
stop rather than editing silently. Two migrations added in parallel will conflict: announce one
in the group chat before pushing it.

## Table ownership
| Owner | Tables |
|---|---|
| Carynn (A) | ApplicationUser, DiningArea, RestaurantTable, OperatingHours, Promotion, Announcement, RestaurantSetting |
| Wallace (B) | Reservation, Feedback, Notification |
| Jordan (C) | MenuCategory, MenuItem, Order, OrderItem, Payment |
