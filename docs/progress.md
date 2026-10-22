# Progress

Keep this file short. Newest log entries on top. Claude reads it at the start of every session.

## Current position
- **Today's sprint:** Sprint 2 (15–22 Oct) — menu browsing + table booking
- **Internal target:** 4 Nov · **Formal deadline:** Fri 6 Nov 2026

| Sprint | Dates | Goal |
|---|---|---|
| 1 | 8–15 Oct | All three roles can log in; layout and DB schema in place |
| 2 | 15–22 Oct | Customer can browse menu and book a table end to end |
| 3 | 22–29 Oct | Full order → status → payment flow |
| 4 | 29 Oct–3 Nov | History, feedback, notifications, analytics dashboard |
| Freeze | 3–5 Nov | Testing, fixes, report finalisation |

## Open decisions (blocking)
| # | Decision | Owner | Blocks |
|---|---|---|---|
| D1 | **ASP.NET Core Identity** chosen by Jordan — Carynn to confirm, then scaffold it (project was created with no auth) | Carynn | PB01–PB04, every `[Authorize]` page |
| D3 | Dev database: SQL Server LocalDB per developer, `DefaultConnection` (proposed, as in labs) | Group | First migration |
| D4 | Fold receipt/confirmation (Intermediate #3) into PB10 and PB16 | Group | PB10, PB16 scope |

## Decided
- One project with layered folders (Pages / ViewModels / Services / Models / Data / Migrations), matching the labs.
- Scrum Master: **Jordan**.
- Auth: ASP.NET Core Identity (pending Carynn's confirmation — see D1).
- UI direction: modern minimal.

## Log
- **2026-10-20** — Jordan: PB06 + PB08 v1 on `feature/PB06-PB08-menu` (spec:
  `docs/superpowers/specs/2026-10-18-menu-design.md`). `IMenuService`/`MenuService`,
  `IImageStorageService` (JPG/PNG/WebP ≤ 2 MB, GUID names, `wwwroot/uploads/menu/`), public
  phone-first `/Menu` (sticky category tabs, search, sold-out greyed), `/Admin/Menu` CRUD for items
  and categories with availability toggle; delete blocked when an item has orders / a category has
  items. No schema change. Build clean, no new warnings. Group agreed the 2 `AddScoped` lines in
  `Program.cs` (22/10). TC-M01–M10 passed 21/10. Still pending: Menu nav link in `_Layout.cshtml`.
- **2026-10-11** — Jordan: layout shell on `feature/layout-shell` — modern minimal navbar, footer,
  design tokens in `site.css`, `_Banners` partial for TempData messages, placeholder home page.
  Restaurant name is a placeholder (`Rumah Rasa`), set in one `const` in `_Layout.cshtml`.
  `_LoginPartial` slot left commented out for Carynn's Identity work. `.claude/rules/*.md` brought
  in line with `architecture.md` and `coding-conventions.md`. Build clean, 0 warnings.
- **2026-10-10** — Lab survey done: conventions recorded in `coding-conventions.md`. Labs use one
  project, `ApplicationDbContext`, `<Entity>Id` keys, PageModels querying the DbContext directly
  (we add a Services layer instead). UI direction set: modern minimal. Repo initialised locally.
- **2026-10-10** — Project scaffolded in Visual Studio 2026 (Razor Pages, .NET 10, no auth) at
  `C:\SoftwareEngineering\FoodOrderingSystem`. Project docs and Claude Code setup added.
  Not yet pushed to GitHub. Chapter 3 (project management) draft done.
- **2026-10-07** — Domains assigned: A Carynn, B Wallace, C Jordan.
- **Late Sep** — Product backlog (PB01–PB28) agreed and mapped to the brief's requirements.
