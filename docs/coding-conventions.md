# Coding Conventions

## Observed lab conventions
_To be filled in from the lecturer's lab projects (C-LabSE) and confirmed by the team.
Where these differ from the baseline below, these win._

- TODO

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
