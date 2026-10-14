---
paths:
  - "**/Pages/**/*.cshtml"
  - "**/Pages/**/*.cshtml.cs"
  - "**/ViewModels/**/*.cs"
---

# Razor Pages rules

Source of truth: `docs/architecture.md` and `docs/coding-conventions.md`. If this file and those
disagree, the docs win — fix this file.

## PageModel
- A PageModel injects **services** through its constructor, into `private readonly` fields
  (`_menuService`). Never inject `ApplicationDbContext` — that is the whole point of the
  Services layer and it is worth 5 marks.
- Namespace is `FoodOrderingSystem.Pages.<Folder>`, class is `<Name>Model : PageModel`,
  one public class per file.
- Handlers are `OnGetAsync` / `OnPostAsync`, plus named handlers (`OnPostDeleteAsync`,
  `OnPostCancelAsync`) wired from the page with `asp-page-handler`. Always async.
- Return `IActionResult` / `Task<IActionResult>` whenever the handler can redirect or 404.
- Form data binds to a dedicated **input model** in `ViewModels/`, not to an EF entity:
  `[BindProperty] public CreateMenuItemInput Input { get; set; } = new();`
  (The labs bind the entity directly; we differ to avoid over-posting.)
- Read-only display data can be the entity itself, as a plain public `IList<T>` / `List<T>`
  property initialised to an empty list.
- Step-by-step `//` comments above each block, as in the labs. Keep that density — it is the
  house style and it is what you will be asked to walk through in the viva.

## POST shape — always this order
1. `if (!ModelState.IsValid) return Page();`
2. call the service; it returns success or an error message
3. on a business failure, `ModelState.AddModelError("Input.Field", "message")` then `return Page();`
4. on success, `TempData["SuccessMessage"] = "... successfully!";`
5. redirect — `RedirectToPage("./Index")` or an absolute `"/Admin/Dashboard"`.
   Post/Redirect/Get, never re-render after a successful POST.

## Validation
- Data annotations on the input model, errors shown with the tag helpers.
- `<div asp-validation-summary="ModelOnly" class="text-danger mb-3">` at the top of every form,
  plus `<span asp-validation-for="..." class="text-danger">` under each field.
- Cross-field and database checks (duplicate name, table already booked) belong in the **service**;
  the page turns the returned error into `AddModelError`.
- Pages with client-side validation render `_ValidationScriptsPartial` in the `Scripts` section.
- Forms always carry the anti-forgery token — the tag helpers add it, so never hand-write a
  `<form>` without `asp-page` / `asp-page-handler`. Do not use `[IgnoreAntiforgeryToken]`
  (the lab's login page does; that is a teaching shortcut, not for us).

## Authorisation
- Restrict by role with `[Authorize(Roles = "Administrator")]` on the PageModel class.
  Roles are exactly `Administrator`, `Staff`, `Customer`.
- Do **not** copy the lab's `HttpContext.Session.GetString("AdminLoggedIn")` guards.
- Get the current user's id with the injected `UserManager<ApplicationUser>` /
  `User.FindFirstValue(ClaimTypes.NameIdentifier)` — it is a `string`.

## Folders
`Pages/Account/` (Identity, scaffolded) · `Pages/Admin/` · `Pages/Staff/` · `Pages/Customer/` ·
`Pages/Menu/` · `Pages/Orders/` · `Pages/Reservations/` · `Pages/Shared/`

## Markup
- `@page`, then `@model FoodOrderingSystem.Pages.<Folder>.<Name>Model`, then
  `@{ ViewData["Title"] = "..."; }`. Every page sets a title.
- Bootstrap 5 from the local `wwwroot/lib` copy — **not** a CDN, so the demo works offline.
  (The newest lab uses the jsDelivr CDN; we differ deliberately.)
- Page content sits in `<div class="container mt-4">`. Forms live inside `card` -> `card-body`.
  `card shadow` with a coloured `card-header` for confirmations and summaries.
- Fields: `<div class="mb-3">` + `<label asp-for class="form-label">` + `class="form-control"` +
  the validation span.
- Prices use an `input-group` with `<span class="input-group-text">RM</span>`.
- Button meaning is consistent: `btn btn-primary` main action, `btn btn-secondary` cancel/back,
  `btn btn-warning` edit, `btn btn-danger` delete, `btn btn-outline-*` for secondary actions.
- Lists use `table table-hover table-bordered` inside `table-responsive`.
- Statuses render as badges in an `@if` chain: `bg-warning text-dark` pending, `bg-info` in
  progress, `bg-success` done, `bg-danger` cancelled/failed, `bg-secondary` otherwise.
- Every destructive action asks for confirmation on its own page or in a modal.
- Tag helpers for all links and forms (`asp-page`, `asp-route-id`, `asp-for`). No raw `href` to
  an internal page.
- No inline `style=""` except a genuine one-off tweak. Page-specific CSS goes in the page's
  `.cshtml.css` (CSS isolation); shared tokens and components go in `wwwroot/css/site.css`.
- Mobile-first — check every page at phone width, 16px side gutter, no horizontal scroll.

## Formatting
| Thing | Format | Example |
|---|---|---|
| Money | `RM @value.ToString("0.00")` | `RM 12.50` |
| Date | `ToString("dd/MM/yyyy")` | `15/10/2026` |
| Time | `ToString("hh:mm tt")` | `07:30 PM` |
| Date + time | `ToString("dd/MM/yyyy hh:mm tt")` | |

Use the shared helper rather than repeating the format string once one exists.

## Banners
`TempData["SuccessMessage"]` and `TempData["ErrorMessage"]` are rendered **once, by the layout**,
as `alert alert-success` / `alert alert-danger`. Pages set them and never render them themselves.

## Shared files — stop and ask
`Pages/Shared/_Layout.cshtml`, `_ViewImports.cshtml`, `_ViewStart.cshtml` and `wwwroot/css/site.css`
are shared. They need group agreement before editing — say so and stop rather than editing silently.
