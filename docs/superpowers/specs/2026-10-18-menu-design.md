# Menu — PB08 (browse) + PB06 (admin management) — Design

- **Owner:** Jordan (Domain C) · **Sprint:** 2 (15–22 Oct 2026) · **Status:** Approved 2026-10-20 — v1 implemented
- **Branch:** `feature/PB06-PB08-menu` off `dev`

## 1. Goal
| Item | Outcome |
|---|---|
| PB08 | Anyone (no login) can browse the digital menu on a phone — descriptions, prices, availability — filter by category and search by name. |
| PB06 | An administrator can create, edit and delete menu categories and items, set prices, upload an image, and toggle availability. |

**Primary use case:** a customer scans a QR code at the table and lands on `/Menu` on their phone.
Admin/staff manage the menu on an iPad or PC.

**Out of scope:** cart / ordering (PB14), per-table QR codes (PB14 + Domain A — see §8), promotions (PB07).

## 2. Constraints
- Strict 3-tier: pages → `IMenuService` / `IImageStorageService` → `ApplicationDbContext`. Pages never touch the DbContext.
- **No schema change.** Uses existing `MenuCategory` (Name, Description, DisplayOrder, IsActive) and
  `MenuItem` (MenuCategoryId, Name, Description, Price `decimal(10,2)`, ImageUrl, IsAvailable).
- No new NuGet packages.
- Forms bind to input models in `ViewModels/`, never EF entities.
- Shared files (`Program.cs`, `_Layout.cshtml`) are edited only after group agreement (§7).

## 3. Components
| Layer | File | Responsibility |
|---|---|---|
| Business | `Services/IMenuService.cs`, `Services/MenuService.cs` | Browse query (category + search filter), category CRUD, item CRUD, availability toggle, delete rules, duplicate-name checks |
| Business | `Services/IImageStorageService.cs`, `Services/ImageStorageService.cs` | Validate and save uploaded images to `wwwroot/uploads/menu/{guid}{ext}` (own folder so it can be git-ignored), delete old files. Uses `IWebHostEnvironment.WebRootPath` |
| ViewModels | `MenuBrowseViewModel` | Tabs, sections, applied category id, search term |
| ViewModels | `MenuCategoryOptionViewModel` | Id + name + active flag — browse tabs and the admin dropdown |
| ViewModels | `MenuCategorySectionViewModel` | One category heading + its `MenuItemCardViewModel` list |
| ViewModels | `MenuItemCardViewModel` | Id, name, description, price, image URL (or placeholder), IsAvailable |
| ViewModels | `MenuItemInput` | Form model for create/edit item, incl. `IFormFile? ImageFile` |
| ViewModels | `MenuCategoryInput` | Form model for create/edit category |
| ViewModels | `AdminMenuItemRowViewModel`, `AdminMenuCategoryRowViewModel` | Rows for the admin tables (category item count, etc.) |
| Pages | `Pages/Menu/Index` | Public browse (PB08) |
| Pages | `Pages/Admin/Menu/Index` | Admin hub with Items / Categories tabs + availability toggle handler |
| Pages | `Pages/Admin/Menu/CreateItem`, `EditItem`, `DeleteItem` | Item CRUD |
| Pages | `Pages/Admin/Menu/CreateCategory`, `EditCategory`, `DeleteCategory` | Category CRUD |
| Static | `wwwroot/images/menu-placeholder.svg` | Neutral image for items without an upload (all seeded items) |

Admin pages are protected by the existing `AuthorizeFolder("/Admin", "AdminOnly")` convention, plus
`[Authorize(Roles = "Administrator")]` on each PageModel as the rules file requires.

## 4. Service contract (summary)
Mutating methods return `(bool Success, string Message)` — same shape as `IUserAdminService`. On failure the page turns `Message` into `ModelState.AddModelError`; on success it goes into `TempData["SuccessMessage"]`. Delete pages also call `Get…DeleteBlockReasonAsync` to disable the button up front.

| Method | Notes |
|---|---|
| `GetMenuForBrowseAsync(int? categoryId, string? search)` | Active categories only, by `DisplayOrder`; items by name; unavailable items included, flagged. Search is a case-insensitive `Contains` on item name, trimmed; blank = no filter |
| `GetCategoryOptionsAsync()` | All categories for the item form dropdown (tabs come with the browse result) |
| `GetAllItemsForAdminAsync()` / `GetAllCategoriesForAdminAsync()` | Include inactive / unavailable |
| `GetItemInputAsync(id)` / `GetCategoryInputAsync(id)` | `null` → page returns `NotFound()` |
| `CreateItemAsync`, `UpdateItemAsync`, `DeleteItemAsync` | |
| `CreateCategoryAsync`, `UpdateCategoryAsync`, `DeleteCategoryAsync` | |
| `ToggleItemAvailabilityAsync(id)` | Flips `IsAvailable` |

## 5. Business rules
| Rule | Where |
|---|---|
| Item name 1–150 chars, description ≤ 500, price RM 0.01–9,999.99, category required | `[Required]`, `[StringLength]`, `[Range]` on `MenuItemInput` |
| Category name 1–100 chars, description ≤ 500, display order 0–999 | `MenuCategoryInput` |
| Category name unique (case-insensitive) | Service |
| Item name unique within its category | Service |
| Selected category must exist | Service |
| **Delete item blocked** if any `OrderItems` reference it → "This item has order history. Mark it unavailable instead." | Service |
| **Delete category blocked** if it has any items → "Move or delete its items first, or deactivate the category." | Service |
| Deleting an item also deletes its uploaded image file (never the placeholder) | Service → `IImageStorageService` |
| Replacing an image on edit deletes the old file after the new one saves | Service |
| Upload: `.jpg/.jpeg/.png/.webp` only, content type must be `image/*`, max 2 MB, saved under a GUID name (no user-supplied filename reaches the disk) | `ImageStorageService` |

## 6. UI / responsive design
Responsive layout uses **CSS media queries via Bootstrap 5 breakpoints**, not JavaScript screen detection —
the browser picks the layout from viewport width, which survives rotation and iPad split-screen.

**`/Menu` — phone-first**
- Header "Our Menu" + search box (GET form, `?search=`).
- Sticky, horizontally scrollable category pill bar (`All` + active categories, links with `?categoryId=`).
- Per category: heading + description, then items.
- `< 768px`: one-column compact rows — image thumbnail left, name / short description / `RM 0.00` right; large tap targets.
- `≥ 768px`: 2-column card grid; `≥ 992px`: 3 columns.
- Unavailable: card greyed (reduced opacity) + `Sold out` badge (`bg-secondary`).
- Empty result: "No dishes match '…'" + Clear search link.
- No "Add to order" button until PB14.

**`/Admin/Menu` — tablet/PC-first**
- Bootstrap nav tabs: Items | Categories, plus "Add item" / "Add category" buttons.
- `≥ 768px`: `table table-hover table-bordered` in `table-responsive`.
  Items: thumbnail · name · category · price · availability badge · toggle button · Edit · Delete.
  Categories: order · name · item count · active badge · Edit · Delete.
- `< 768px`: same data as stacked cards.
- Create/Edit Item: name, category `<select>`, description textarea, price `input-group` with `RM`,
  file input (`accept=".jpg,.jpeg,.png,.webp"`) with current-image preview, Available checkbox.
  `enctype="multipart/form-data"`.
- Delete pages: confirmation card; if the service would refuse, show the reason and disable Delete.
- Success/error via `TempData["SuccessMessage"]` / `["ErrorMessage"]`, rendered by the layout.
- Page-specific CSS in `.cshtml.css` (CSS isolation); no edits to `site.css`.

## 7. Shared-file changes — need group agreement (not done until agreed)
| File | Change |
|---|---|
| `Program.cs` | `AddScoped<IMenuService, MenuService>()` and `AddScoped<IImageStorageService, ImageStorageService>()` |
| `Pages/Shared/_Layout.cshtml` | Replace the placeholder Menu `<span>` with `<a asp-page="/Menu/Index">`; change the Admin link's active check to `StartsWith("/Admin")`; add an Admin → Menu entry |

Without the `Program.cs` lines the pages cannot run, so this agreement is needed before manual testing.

## 8. Future scope (noted, not built)
- **Per-table QR codes:** QR encodes `/Menu?table=M3`; PB14 carries the table into the order.
  Generating codes per table touches Domain A (tables) — discuss with Carynn in Sprint 3.
- For the demo, a QR to `/Menu` made with any free generator is enough.

## 9. Testing
No test project exists; adding xUnit needs a group decision, so testing is manual and written up in
`docs/report-evidence.md`.

| ID | Steps | Expected |
|---|---|---|
| TC-M01 | Logged out, open `/Menu` on phone width | Menu loads, single column, sold-out item greyed with badge |
| TC-M02 | Tap "Desserts" tab; search "zzz" | Only desserts; then empty state + Clear search |
| TC-M03 | As customer, open `/Admin/Menu` | Access denied / redirected |
| TC-M04 | Admin creates item with a PNG | Appears on `/Menu` with the image |
| TC-M05 | Upload `.txt`; upload 5 MB JPG | Validation error each time, nothing saved |
| TC-M06 | Delete an item that has an OrderItem (insert one via SSMS until PB14 exists) | Refused with message |
| TC-M07 | Delete a category that has items | Refused with message |
| TC-M08 | Create category "Desserts" again | Duplicate-name error |
| TC-M09 | Toggle availability on an item | Badge flips in admin; greyed on `/Menu` |

Plus: `dotnet build` with no new warnings; every page checked at 375px, 768px and 1280px.

## 10. Build order
1. Branch → 2. `ImageStorageService` + `MenuService` → 3. ViewModels → 4. `/Menu` (PB08) →
5. `/Admin/Menu` pages (PB06) → 6. shared-file edits (after agreement) → 7. manual tests →
8. update `product-backlog.md`, `progress.md`, `report-evidence.md`.
