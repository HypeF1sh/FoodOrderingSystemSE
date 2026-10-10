---
paths:
  - "**/Services/**/*.cs"
  - "**/Data/**/*.cs"
  - "**/Models/**/*.cs"
  - "**/Entities/**/*.cs"
  - "**/Migrations/**/*.cs"
---

# Business and data layer rules

## Services
- Every service has an interface (`IMenuService`) and an implementation (`MenuService`), registered scoped.
- Services return results the page can act on (entity, view model, bool, or a small result type with an error message).
  Don't throw exceptions for normal validation failures.
- Status changes (reservation, order, payment) happen only inside a service method that checks the
  current status allows the transition.
- Services may call other domains' services through their interfaces; they never write to another domain's tables directly.

## Entities and EF Core
- Entity classes live in the Data layer. Key property is `Id` (int, identity).
- Every relationship has an explicit foreign key property (`MenuCategoryId`) plus navigation property.
- Prices and totals: `decimal` with `[Column(TypeName = "decimal(10,2)")]`.
- `OrderItem.UnitPrice` stores the price **at the time of ordering** — never read the current menu price for a past order.
- Statuses are C# enums stored as strings (`HasConversion<string>()`), so the database is readable in SSMS.
- Timestamps: `DateTime` in local restaurant time, named `CreatedAt`, `UpdatedAt`, `PaidAt` etc.
- Use `AsNoTracking()` for read-only queries. Use `Include()` deliberately — no lazy loading.

## Migrations
- Schema changes only through migrations, never by editing the database in SSMS.
- One migration per logical change, named for the change: `AddMenuTables`, `AddOrderStatus`.
- Announce a migration to the team before pushing it — two people adding migrations in parallel conflict.
- Never edit a migration that has already been pushed. Add a new one.
