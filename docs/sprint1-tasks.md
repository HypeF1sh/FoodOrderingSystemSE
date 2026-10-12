# Sprint 1 — Urgent Tasks (due Tue 13 / Wed 15 Oct)

Order matters: **Carynn's Identity setup → Wallace's tables.** Jordan's layout runs in parallel.

---

## Carynn — Identity setup (login + roles) · due **Tue 13 Oct**

**Why this is first:** it creates the login system *and* the database connection (`ApplicationDbContext`).
Wallace's tables go inside this DbContext, and every page that needs a logged-in user depends on it.
Counts toward PB01–PB04.

1. **Branch:** `git checkout dev` → `git pull` → `git checkout -b feature/PB04-identity-setup`
2. **NuGet packages** (same 10.0.x version as the lab): `Microsoft.AspNetCore.Identity.EntityFrameworkCore`,
   `Microsoft.AspNetCore.Identity.UI`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`
3. **`Models/ApplicationUser.cs`** — inherits `IdentityUser`; add `FullName` (string), `IsActive` (bool, default true), `CreatedAt` (DateTime)
4. **`Data/ApplicationDbContext.cs`** — `ApplicationDbContext : IdentityDbContext<ApplicationUser>` (exact name, same as the lab)
5. **`appsettings.json`** — connection string named `DefaultConnection`:
   `Server=(localdb)\MSSQLLocalDB;Database=FoodOrderingSystemDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`
6. **`Program.cs`**
   - `AddDbContext<ApplicationDbContext>(...UseSqlServer(GetConnectionString("DefaultConnection")))`
   - `AddDefaultIdentity<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = false).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>()`
   - `app.UseAuthentication();` **before** `app.UseAuthorization();`
   - ⚠️ `RequireConfirmedAccount` must be **false** — otherwise new users can't log in (no email sending is set up)
7. **Scaffold login pages:** right-click project → Add → New Scaffolded Item → **Identity**.
   Pick Register, Login, Logout, Manage/Index, AccessDenied. Context = `ApplicationDbContext`, user = `ApplicationUser`.
   This also adds `_LoginPartial` for the navbar.
8. **`Data/DbSeeder.cs`** — on startup create roles **Administrator, Staff, Customer** and three dev accounts
   (one per role). New registrations get the Customer role automatically. Post the dev logins in the group.
9. **Migration** (Package Manager Console): `Add-Migration InitialCreate` → `Update-Database`
10. **Test:** register a customer and log in · seeded admin logs in · logout works · tables visible in SSMS
11. **Commit small, push, PR into `dev`**, post in group. Example commits:
    `feat(PB04): add ApplicationUser and ApplicationDbContext` · `feat(PB04): scaffold Identity pages` · `feat(PB04): seed roles and dev accounts`

**Don't** create any other tables — Wallace does those next.

---

## Wallace — ER diagram + database tables

### Part 1 — ER diagram · due **Tue 13 Oct**
**Why:** it's the blueprint all three of us build against — an `Order` links to a customer (Carynn),
a reservation (Wallace) and menu items (Jordan), so names and links must be agreed before coding.
It's also required in Chapter 4 of **everyone's** report and backs the 5-mark database design criterion.

- Tool: draw.io (app.diagrams.net) or Lucidchart
- Source: the entity list in `docs/architecture.md` — 15 tables, fields already listed
- Each table shows: name, primary key (`<Entity>Id`), foreign keys, main fields
- Crow's foot lines for relationships. Key links:
  - ApplicationUser 1–many Reservation, Order, Feedback, Notification
  - DiningArea 1–many RestaurantTable · RestaurantTable 1–many Reservation
  - Reservation 1–many Order (`Order.ReservationId` optional)
  - MenuCategory 1–many MenuItem · MenuItem 1–many OrderItem · Order 1–many OrderItem
  - Order 1–many Payment (allows a failed attempt then a successful one)
- Post the image in the group by Tue; Carynn and Jordan check it
- Save the `.drawio` file and a PNG export in `docs/diagrams/`

### Part 2 — tables in code · **Wed 15 Oct, only after Carynn's Identity is merged**
**Why:** turns the diagram into real database tables. Everyone's Sprint 2 work needs them.

- Branch: `feature/schema`
- One C# class per table in `Models/`, enums in `Models/Enums/`
- Add each as a `DbSet` in Carynn's `ApplicationDbContext`
- **One** migration: `Add-Migration AddCoreSchema` → `Update-Database`
- Seed data in `Data/DbSeeder.cs`: dining areas, ~8 tables, operating hours, menu categories, ~15 menu items
- Check in SSMS, PR into `dev`

**Conventions** (full list in `docs/coding-conventions.md`):
- Keys: `[Key] public int MenuItemId` — not plain `Id`
- Customer links (`CustomerId`, `UserId`) are **string** — Identity's user Id is a string
- Money: `decimal` with `[Column(TypeName = "decimal(10,2)")]`
- Statuses are C# enums, stored as text
- Strings default to `string.Empty`; navigation properties at the bottom

⚠️ **Don't create a DbContext or a migration before Carynn's is merged** — two of them will clash.

---

## Jordan — layout shell · due Wed 15 Oct
Modern minimal navbar, footer and base styles. Leave a slot for `_LoginPartial`. No database work.
