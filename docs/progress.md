# Progress

Keep this file short. Newest log entries on top. Claude reads it at the start of every session.

## Current position
- **Today's sprint:** Sprint 1 (8–15 Oct) — foundation
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
- **2026-10-10** — Lab survey done: conventions recorded in `coding-conventions.md`. Labs use one
  project, `ApplicationDbContext`, `<Entity>Id` keys, PageModels querying the DbContext directly
  (we add a Services layer instead). UI direction set: modern minimal. Repo initialised locally.
- **2026-10-10** — Project scaffolded in Visual Studio 2026 (Razor Pages, .NET 10, no auth) at
  `C:\SoftwareEngineering\FoodOrderingSystem`. Project docs and Claude Code setup added.
  Not yet pushed to GitHub. Chapter 3 (project management) draft done.
- **2026-10-07** — Domains assigned: A Carynn, B Wallace, C Jordan.
- **Late Sep** — Product backlog (PB01–PB28) agreed and mapped to the brief's requirements.
