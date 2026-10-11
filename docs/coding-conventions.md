# Coding Conventions

## Observed lab conventions
_Surveyed 2026-10-10 from `C:\SoftwareEngineering\C-LabSE`: `LabLessons` (console basics),
`ProjectManagementSystem` (first EF Core CRUD lab), `StaffAccountSystem` (latest and richest —
Staff, Menu, RestaurantTable, Order, OrderItem). Where these differ from the baseline below,
these win, except where a conflict is flagged._

### Project layout
- **One** web project inside a folder of the same name, `*.slnx` solution beside it (matches our scaffold).
- Folders at project root: `Data/`, `Models/`, `Migrations/`, `Pages/`, `Properties/`, `wwwroot/`.
  No `Services/`, no `ViewModels/`, no separate class libraries in any lab.
- `Models/` holds entities directly — one class per file, file name = class name.
- `Pages/` is grouped by feature folder (`Pages/Menu/`, `Pages/Admin/Orders/`,
  `Pages/Admin/RestaurantTables/`, `Pages/Staff/`).

### EF Core setup
- `Data/ApplicationDbContext.cs`, class `ApplicationDbContext : DbContext`, constructor takes
  `DbContextOptions<ApplicationDbContext>` and calls `: base(options)`.
- One `public DbSet<T> Ts { get; set; }` per entity, plural property name (`Menus`, `Orders`, `OrderItems`).
- No `OnModelCreating` override in any lab — all mapping is done with data annotations on the entity.
- Registered in `Program.cs` with `AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(
  builder.Configuration.GetConnectionString("DefaultConnection")))`.
- Connection string name is **`DefaultConnection`** in `appsettings.json`, LocalDB:
  `Server=(localdb)\MSSQLLocalDB;Database=<Name>Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true`
- Packages: `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Tools` (10.0.12).
- Migrations are named by what they add, PascalCase, one per feature:
  `InitialCreate`, `AddMenu`, `AddRestaurantTable`, `AddOrder`.

### Entities
- Key is either plain `Id` (`Menu`, `Staff`, `Product`) or `[Key] public int <Entity>Id`
  (`Order.OrderId`, `OrderItem.OrderItemId`, `RestaurantTable.RestaurantTableId`). Inconsistent
  across labs — pick one and keep it: we use `<Entity>Id`.
- `[Required(ErrorMessage = "...")]` with a written-out message on every user-entered field.
- `[Display(Name = "...")]` when the label should differ from the property name.
- `[StringLength(n)]`, `[Range(min, max)]`, `[EmailAddress]`, `[DataType(DataType.Password)]`,
  `[Compare("Password")]`, `[NotMapped]` (for `ConfirmPassword`) all used as in the baseline.
- `[Index(nameof(X), IsUnique = true)]` on the class for unique columns (`RestaurantTable.TableNumber`).
- Money: `decimal` + `[Column(TypeName = "decimal(18,2)")]` — labs use **18,2**, not 10,2.
- Strings initialised `= string.Empty;`, collections `= new List<T>();`, optional strings are `string?`.
- Status fields are **`string` with a default** (`Status = "Pending"`, `"Available"`), not enums.
- Navigation properties grouped at the bottom under a `// Navigation properties` comment.

### PageModels
- `public class <Name>Model : PageModel` in namespace `<Project>.Pages.<Folder>`.
- Constructor injection into `private readonly` fields: `_context`, `_environment`.
- Form-bound data: `[BindProperty] public <Entity> <Entity> { get; set; } = new <Entity>();`
  — labs bind the entity itself, no separate input/view model.
- Read-only display data: plain public `IList<T>` / `List<T>` properties initialised to an empty list.
- Handlers: `OnGet` / `OnGetAsync(int id)` / `OnPostAsync()`, plus named handlers
  (`OnPostLogout`, `OnPostMakeAvailableAsync(int id)`) wired from the page with `asp-page-handler`.
- Return type is `IActionResult` / `Task<IActionResult>` whenever the handler can redirect or 404.
- Standard POST shape, in this order:
  1. auth/session check then `RedirectToPage`
  2. `if (!ModelState.IsValid) return Page();`
  3. extra business checks, `ModelState.AddModelError("Entity.Field", "message")` then `return Page();`
  4. mutate, `await _context.SaveChangesAsync();`
  5. `TempData["SuccessMessage"] = "... successfully!";`
  6. `RedirectToPage("./Index")` or an absolute `"/Admin/Dashboard"`.
- Edit pages re-fetch the row and copy fields onto `existingEntity` rather than attaching the bound entity.
- Queries: `FirstOrDefaultAsync`, `AnyAsync` (duplicate checks), `ToListAsync`, always with
  `OrderBy`/`OrderByDescending`; `.Include(...).ThenInclude(...)` for order to items to menu.
- Step-by-step `//` comments above each block, written for a reader learning the framework.
  Keep this density — it is the house style and it helps in the viva.

### Validation
- Data annotations on the entity plus `asp-validation-for` spans, plus
  `<div asp-validation-summary="ModelOnly" class="text-danger mb-3">` at the top of the form.
- Cross-field and database checks are done in `OnPostAsync` with `AddModelError`, not with custom attributes.
- `_ValidationScriptsPartial.cshtml` holds only jquery.validate + jquery.validate.unobtrusive.

### Pages and Bootstrap
- `@page` then `@model <Project>.Pages.<Folder>.<Name>Model`, then `@{ ViewData["Title"] = "..."; }`.
- Everything wrapped in `<div class="container mt-4">`; forms live inside `card` then `card-body`.
- `card shadow` with a coloured `card-header` for confirmations and summaries.
- Fields: `<div class="mb-3">` + `<label asp-for class="form-label">` + `class="form-control"` +
  `<span asp-validation-for class="text-danger">`.
- `input-group` with `<span class="input-group-text">RM</span>` for prices.
- Buttons: `btn btn-primary` submit, `btn btn-secondary` cancel/back, `btn btn-warning` edit,
  `btn btn-danger` delete. Lists use `table table-hover table-bordered` inside `table-responsive`.
- Status shown as `badge bg-warning text-dark` / `bg-success` / `bg-danger` / `bg-secondary`
  in an `@if` chain.
- `TempData["SuccessMessage"]` rendered as `<div class="alert alert-success">` at the top of Index pages.
- Prices always `RM @value.ToString("0.00")`. Dates `ToString("dd/MM/yyyy")`, times `ToString("hh:mm tt")`.
- `_Layout.cshtml` in the newest lab loads Bootstrap 5.3.3 from the jsDelivr CDN, not `wwwroot/lib`.
  Our project keeps the local `wwwroot/lib` copy so it works offline during the demo.

### Things the labs do that we deliberately do NOT copy
| Lab practice | Our rule | Why |
|---|---|---|
| PageModel injects `ApplicationDbContext` and queries it directly | PageModel calls a service interface only | 3-tier architecture is worth 5 marks |
| No `Services/` folder | `Services/` with `I<Name>Service` + `<Name>Service` registered scoped | same |
| Entity bound straight to the form with `[BindProperty]` | input/view model where a form posts data | avoids over-posting; rubric mentions validation |
| Plaintext `Password` column; hardcoded `admin` / `Admin@123` login; `Session.GetString("AdminLoggedIn")` guard on every page | hashed passwords and the agreed auth approach (decision D1) | security is graded |
| `[IgnoreAntiforgeryToken]` on the login page | leave antiforgery on | security |
| `decimal(18,2)` | `decimal(10,2)` per our CLAUDE.md | plenty for RM prices; stay consistent |
| `string` status with magic literals `"Pending"`, `"Available"` | C# `enum` | no typo bugs, and the rubric likes enums |
| `Models/` holds entities only | entities in `Models/`, DTOs in `ViewModels/` | keeps layers visible |
| Non-nullable navigation props without initialiser (nullable warnings) | mark `null!` with a comment, or make nullable | nullable reference types are on |

## Baseline (Microsoft C# conventions)
### Naming
| Thing | Style | Example |
|---|---|---|
| Classes, records, enums, interfaces | PascalCase; interfaces start with `I` | `MenuService`, `IMenuService`, `OrderStatus` |
| Public methods and properties | PascalCase | `GetAvailableItemsAsync`, `TotalAmount` |
| Async methods | end in `Async` | `CreateOrderAsync` |
| Locals and parameters | camelCase | `menuItem`, `partySize` |
| Private fields | `_camelCase` | `_menuService` |
| Razor page files | PascalCase, verb or noun | `Pages/Admin/Menu/Create.cshtml` |
| CSS classes | kebab-case | `.menu-card`, `.order-status-badge` |

### Structure
- One public class per file; file name matches the class.
- Constructor injection with `readonly` fields. No service locator, no `new` for services.
- Guard clauses and early returns over deep nesting.
- Methods do one thing. If it needs a comment to explain what it does, split it.
- XML doc comments (`///`) on public service interface methods.
- No magic numbers or strings — use enums, constants or settings (e.g. slot length = 90).

### UI
- Bootstrap 5 grid and components; mobile-first, check at phone width.
- One shared layout (`_Layout.cshtml`) with role-aware navigation.
- Consistent button meaning: primary = main action, outline/secondary = cancel/back, danger = delete.
- Every destructive action asks for confirmation.
- Show success/error feedback after every form submission.
- Prices displayed as `RM 12.50` using a shared formatting helper.

### Visual direction — modern minimal (agreed 2026-10-10)
- White and very light grey surfaces, near-black text, **one** accent colour used sparingly
  (primary buttons, active nav item, key badges). Pick the accent once and define it as a CSS variable.
- Generous whitespace; thin borders or subtle shadows rather than heavy coloured card headers.
  Where the lab uses a coloured `card-header`, prefer a plain header with a bold title.
- One sans-serif typeface across the site; clear size hierarchy instead of many colours.
- Status badges are the only place several colours appear (pending / in progress / done / cancelled).
- Restaurant name: TBD — use a placeholder in the navbar until the group picks one.

