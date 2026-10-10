# FoodOrderingSystem — Restaurant Reservation & Order Management System (RROMS)

Group coursework for 5026CMD Software Engineering (Coventry University, delivered at INTI Penang).
Team of 3: Carynn, Wallace Chan, Jordan. Formal deadline **Fri 6 Nov 2026** — late submission scores zero.

@docs/progress.md

## What the system is
A web application where customers book tables and order food online, staff run daily service,
and the administrator manages the restaurant and views analytics.
Three roles: **Administrator, Staff, Customer**.

| Need | Read |
|---|---|
| Full functional requirements, performance levels, marking rubric | `docs/requirements.md` |
| Backlog items PB01–PB28, owners, sprints, status | `docs/product-backlog.md` |
| Who owns which files and tables, cross-domain rules | `docs/team-and-domains.md` |
| Solution layout, entities, enums, reservation overlap rule | `docs/architecture.md` |
| Naming and style conventions (from the lecturer's lab code) | `docs/coding-conventions.md` |
| Branching, commit format, PR flow | `docs/git-workflow.md` |

## Tech stack — fixed by the brief, do not substitute
- ASP.NET Core **Razor Pages**, C#, **.NET 10** (not MVC, not Blazor, not Web API)
- SQL Server (LocalDB in development) via **Entity Framework Core**, code-first migrations
- Bootstrap 5, jQuery Validation (already in `wwwroot/lib`), vanilla JavaScript
- Visual Studio 2026 — solution file is `FoodOrderingSystem.slnx`

## Architecture — strict 3-tier (graded, 5 marks)
Presentation (Razor Pages) → Business (Services) → Data (DbContext, entities). Never skip a layer.
- Page models call **services only**. A page model never injects or touches the DbContext.
- Services own business rules, validation beyond simple field checks, and status transitions.
- Only the Data layer talks to the database. No string-concatenated SQL, ever.
- Services are interfaces + implementations, registered in `Program.cs` as scoped.
- Pages receive and return view models / input models, not raw EF entities, where a form posts data.

## Domain ownership
| Domain | Owner | Backlog items |
|---|---|---|
| A — Accounts & Administration | Carynn | PB01–PB05, PB07, PB21, PB23 |
| B — Reservations & Staff Operations | Wallace Chan | PB09–PB13, PB17, PB19, PB20 |
| C — Menu, Orders & Payment | Jordan | PB06, PB08, PB14–PB16, PB18, PB22 |

Work only inside the current user's domain unless they say otherwise. Shared files
(`Program.cs`, `_Layout.cshtml`, `AppDbContext`, migrations, `appsettings.json`) need group
agreement before editing — say so and stop rather than editing silently.

## Coding conventions (summary — full list in docs/coding-conventions.md)
- Match the lecturer's lab code style where it differs from generic ASP.NET defaults.
- `async`/`await` for every database call; async methods end in `Async`.
- PascalCase types and members, camelCase locals and parameters, `_camelCase` private fields.
- Nullable reference types are on — handle nulls explicitly; no `!` without a comment saying why.
- One public class per file, file name matches class name.
- Money is `decimal` with `[Column(TypeName = "decimal(10,2)")]`. Never `double` for prices.
- Data annotations for validation on input models; show errors with the tag helpers.
- No secrets or connection strings in code. Connection string lives in `appsettings.json`;
  anything sensitive (API keys) goes in user-secrets.

## Working agreements
- Build each backlog item as a vertical slice: entity → migration → service → page → manual test.
- Ask before: adding a NuGet package, changing the database schema, editing a shared file,
  or touching another member's domain.
- Keep changes scoped to the backlog item in progress. No drive-by refactors.
- After finishing an item: `dotnet build` must pass with no new warnings, then list
  (1) what to test manually, (2) screenshots worth taking for the report, and
  (3) update the item's status in `docs/product-backlog.md` and the log in `docs/progress.md`.
- Every member must be able to explain their code in an individual viva. Prefer clear,
  conventional code over clever code, and explain non-obvious decisions in a short comment.
