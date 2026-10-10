# FoodOrderingSystem — RROMS

Web-based **Restaurant Reservation and Order Management System**, built for
5026CMD Software Engineering (Coventry University · INTI International College Penang).

**Team:** Carynn (Accounts & Administration) · Wallace Chan (Reservations & Staff Operations) · Jordan (Menu, Orders & Payment)

## Stack
ASP.NET Core Razor Pages · C# · .NET 10 · SQL Server + EF Core · Bootstrap 5

## Run it locally
1. Install Visual Studio 2026 with the **ASP.NET and web development** workload.
2. Clone the repo and open `FoodOrderingSystem.slnx`.
3. Apply database migrations (once the database layer exists):
   Tools → NuGet Package Manager → Package Manager Console → `Update-Database`
4. Press **F5**. The site opens in your browser.

## Repository layout
```
FoodOrderingSystem.slnx       solution file
FoodOrderingSystem/           ASP.NET Core Razor Pages project
docs/                         requirements, backlog, architecture, workflow, progress
.claude/                      shared Claude Code settings and rules
CLAUDE.md                     project instructions for Claude Code
```

## Working on the project
Read `docs/git-workflow.md` before your first commit. Short version:
branch from `dev` as `feature/PBxx-short-name`, commit with `feat(PBxx): ...`, open a PR into `dev`,
get one teammate's review, merge.
