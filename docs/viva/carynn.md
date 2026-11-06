# Viva guide: Carynn (Domain A: Accounts and Administration)

**Find any file fast:** every code file starts with a header line, `// PBxx (Owner) - what it does`.
In Visual Studio press **Ctrl+Shift+F** and search for `PB05` (for example) to list every file for that item.

## The 3 tiers in one picture (same for every PB)

```
Browser -> Page (Pages/.../X.cshtml + X.cshtml.cs)        Presentation: shows data, reads the form
        -> Service interface (Services/IXService.cs)       Business: rules, checks
        -> Service class (Services/XService.cs)
        -> ApplicationDbContext (Data/) -> SQL Server      Data: EF Core only, no SQL strings
```
- A page model **never** uses the DbContext. It calls a service through its interface (injected in the constructor).
- Services are registered in `Program.cs` as `AddScoped` (one per web request).
- Every POST: `ModelState.IsValid` (annotations) -> service (business rules) -> error on the form, or success message + redirect (Post/Redirect/Get).
- Access by role: `Program.cs` puts `/Admin` behind the `AdminOnly` policy, `/Staff` behind `StaffOrAdmin`.

---

## PB01: Customer registers

| | |
|---|---|
| Page | `Areas/Identity/Pages/Account/Register.cshtml(.cs)` |
| Model | `Models/ApplicationUser.cs` (`FullName`, `IsActive`), phone uses Identity's `PhoneNumber` |

- Input model needs full name, phone, email, password + confirm.
- Password rules (in `Program.cs`): at least 8 chars, upper, lower, digit, symbol. Identity checks them.
- New users get the **Customer** role: `await _userManager.AddToRoleAsync(user, "Customer");`
- Passwords are stored as a salted hash by Identity, never as plain text.

## PB02 / PB03: Log in securely and manage my profile (customer and staff)

| | |
|---|---|
| Pages | `Areas/Identity/Pages/Account/Login.cshtml.cs`, `Manage/Index.cshtml.cs` (name, phone), `Manage/ChangePassword.cshtml.cs` |

- **Lockout:** `PasswordSignInAsync(..., lockoutOnFailure: true)`; 5 wrong passwords lock the account for 5 minutes (`Program.cs`).
- **Deactivated users** are refused before sign-in: `if (existingUser != null && !existingUser.IsActive)`.
- After login each role goes to its own home: Admin -> `/Admin/Dashboard`, Staff -> `/Staff/Floor`, Customer -> home.
- Profile page edits full name and phone; Change password uses Identity's `ChangePasswordAsync` (needs the old password).

## PB04: Admin manages users and roles, access restricted by role

| | |
|---|---|
| Pages | `Pages/Admin/Users/Index`, `Edit` |
| Service | `Services/UserAdminService.cs` (`IUserAdminService`) |
| Seed | `Data/DbSeeder.cs`: the 3 roles and one dev account per role |

- `UpdateUserAsync(userId, newRole, isActive, currentAdminId)`:
  - role must be one of Administrator / Staff / Customer;
  - an admin **cannot demote or deactivate themselves** (checked in the service, so a crafted form cannot bypass it);
  - role change = remove old roles, add the new one;
  - deactivating calls `UpdateSecurityStampAsync`, which **logs the user out** of any open session.
- Uses `UserManager` only, never the DbContext.

**Examiner questions**
- *How is access restricted?* `[Authorize]` policies per folder in `Program.cs` (`AdminOnly`, `StaffOrAdmin`, `CustomerOnly`), plus role checks in the services.
- *Why the security stamp?* Without it a deactivated user stays logged in until their cookie expires.

## PB05: Dining areas, tables and operating hours

| | |
|---|---|
| Pages | `Pages/Admin/DiningAreas/`, `Pages/Admin/Tables/`, `Pages/Admin/OperatingHours/Index` |
| Services | `DiningAreaService.cs`, `OperatingHoursService.cs` |
| Models | `DiningArea`, `RestaurantTable`, `OperatingHours` |

- Area name required and unique. An area with tables cannot be deleted.
- Table: number required and **unique**, 1-20 seats, area must exist (`ValidateTableAsync`).
- Deleting a table that has bookings **deactivates** it instead, so booking history stays.
- Hours: `GetWeekAsync` always returns 7 days; `UpdateWeekAsync` saves all 7 at once and checks closing time is after opening time (unless the day is closed).
- Other domains read tables and hours **only through these services** (Wallace's bookings, Jordan's floor and chatbot).

## PB07: Promotions, announcements and restaurant settings

| | |
|---|---|
| Pages | `Pages/Admin/Promotions/`, `Pages/Admin/Announcements/`, `Pages/Admin/Settings/Index` |
| Services | `PromotionService.cs`, `AnnouncementService.cs`, `RestaurantSettingService.cs` |
| Shown to customers | `ViewComponents/OffersStripViewComponent.cs` (strip on the Menu page), "What's on" on the home page |

- Promotion rules: title required, **1-90 % off**, end date not before start date. Active today = switched on and today is inside the date range.
- Promo code = `PROMO` + promotion id (e.g. `PROMO3`), so no extra column was needed. Checkout uses `GetActiveByCodeAsync`; the discount maths is in one place, `PromotionService.ApplyDiscount`.
- Announcements: title and body required, can be switched on/off without deleting.
- Settings are key-value pairs (e.g. `Address`, `Phone`), shown on the home page and used by the chatbot.

## PB21: Admin monitors reservations, orders and payments

| | |
|---|---|
| Page | `Pages/Admin/Monitoring/Index` (3 tabs) |
| Service | `AdminMonitoringService.cs` |

- Reads through the other domains' services only: `IReservationService`, `IOrderService`, `IPaymentService`.
- Each tab: list in the date range, count per status, optional status filter; orders show total value, payments show paid and refunded totals.
- Range is checked: end not before start, at most 366 days. Filters are in the URL (bookmarkable).

## PB23: Customer activity reports (+ PB24 CSV export)

| | |
|---|---|
| Page | `Pages/Admin/Reports/CustomerActivity` |
| Services | `CustomerActivityService.cs`, `ExportService.cs` |

- One row per customer: bookings (not cancelled), orders (not cancelled), money spent (**Paid** payments only, so refunds are excluded), last visit.
- Grouped by `CustomerId`, not by name (names are not unique). Sort by activity or by spend.
- **Export (PB24):** `ExportService` writes CSV with a UTF-8 **BOM** (Excel shows accents correctly) and a **CSV-injection guard**: a cell starting with `=`, `+`, `-` or `@` gets a leading apostrophe, so Excel treats it as text, not a formula. Print button for PDF.

## PB28: Personalised dish recommendations

| | |
|---|---|
| Service | `RecommendationService.cs` (uses Jordan's `IOrderService.GetTopItemIdsAsync`) |
| Shown on | Menu page, "Recommended for you" row |

1. The customer's own most-ordered dishes (only ones available now).
2. Topped up with the restaurant's most popular dishes in the **same categories**.
3. New customer (no history): "Popular right now" = best sellers of the last 30 days.
- Shows 1 to 3 dishes.

---

## Gap to mention if asked
- **PB20 "promotional updates":** creating a promotion does not send a notification to customers; promotions are shown on the home and menu pages instead. (Would need `PromotionService` to call Wallace's `INotificationService` for each customer.)
