---
paths:
  - "**/Pages/**/*.cshtml"
  - "**/Pages/**/*.cshtml.cs"
---

# Razor Pages rules

- A PageModel injects **services** through its constructor. Never inject `AppDbContext`.
- Use `OnGetAsync` / `OnPostAsync` (and named handlers `OnPostDeleteAsync` etc.) — always async.
- Bind form data with `[BindProperty]` on a dedicated input model class, not on an EF entity.
- Validate with data annotations; on `!ModelState.IsValid` return `Page()` so errors render.
- After a successful POST, redirect (`RedirectToPage`) — Post/Redirect/Get, never re-render.
- Success/error banners go through `TempData["Success"]` / `TempData["Error"]`, shown once in the layout.
- Restrict pages by role with `[Authorize(Roles = "...")]` once authentication exists.
- Folder per area of the site: `Pages/Admin/`, `Pages/Staff/`, `Pages/Customer/`, `Pages/Menu/`, `Pages/Orders/`, `Pages/Account/`.
- Markup: Bootstrap 5 classes, tag helpers (`asp-for`, `asp-page`, `asp-validation-for`), no inline `style=""`
  except one-off layout tweaks. Page-specific CSS goes in the page's `.cshtml.css` (CSS isolation).
- Every page sets `ViewData["Title"]`.
- Forms include the anti-forgery token (tag helpers do this automatically — don't hand-write `<form>` without them).
