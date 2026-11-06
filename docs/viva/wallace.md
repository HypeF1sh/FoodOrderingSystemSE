# Viva guide: Wallace (Domain B: Reservations and Staff Operations)

**Find any file fast:** every code file starts with a header line, `// PBxx (Owner) - what it does`.
In Visual Studio press **Ctrl+Shift+F** and search for `PB10` (for example) to list every file for that item.

## The 3 tiers in one picture (same for every PB)

```
Browser -> Page (Pages/.../X.cshtml + X.cshtml.cs)        Presentation: shows data, reads the form
        -> Service interface (Services/IXService.cs)       Business: rules, status changes, checks
        -> Service class (Services/XService.cs)
        -> ApplicationDbContext (Data/) -> SQL Server      Data: EF Core only, no SQL strings
```
- A page model **never** uses the DbContext. It calls a service through its interface (injected in the constructor).
- Services are registered in `Program.cs` as `AddScoped` (one per web request).
- Every POST: `ModelState.IsValid` (annotations) -> service (business rules) -> error on the form, or success message + redirect (Post/Redirect/Get).
- Your data layer work: `Data/ApplicationDbContext.cs` (DbSets, enums stored as text, delete rules) and `Data/DbSeeder.cs` (areas, tables, hours, menu).

**Booking rules** are constants at the top of `ReservationService` (no magic numbers):
`DiningDurationMinutes = 90`, `TurnoverBufferMinutes = 15`, `MinAdvanceBookingMinutes = 30`, `MaxAdvanceBookingDays = 60`, `CutoffHoursBeforeStart = 2`, `MaxOnlinePartySize = 8`.

---

## PB09: Check table availability

| | |
|---|---|
| Page | `Pages/Reservations/Create.cshtml(.cs)` (step 1: search), picker `Pages/Shared/_BookingPicker.cshtml` + `wwwroot/js/booking-picker.js` |
| Service | `ReservationService.ValidateOperatingHoursAsync`, `GetAvailableTablesAsync` |

**Open at that time?** (`ValidateOperatingHoursAsync`)
1. Not in the past, at most 60 days ahead, at least 30 minutes from now.
2. The day's opening hours come from Carynn's `IOperatingHoursService`.
3. Last seating = closing time minus 90 minutes, so the meal ends before closing.

**Which tables are free?** (`GetAvailableTablesAsync`) The overlap rule:
```csharp
// a table is taken if an active booking overlaps the requested slot, widened by the 15 min buffer
&& r.StartTime < windowEnd      // windowEnd   = requested end   + 15 min
&& r.EndTime > windowStart)     // windowStart = requested start - 15 min
```
- Cancelled and NoShow bookings do not block a table.
- Table must fit the party, and have at most 3 spare seats (big tables stay free for big groups).
- Smallest fitting table first.

## PB10: Make a reservation with automatic validation

| | |
|---|---|
| Pages | `Pages/Reservations/Create` (step 2: book), `Confirmation` |
| Service | `ReservationService.CreateReservationAsync` |
| Model | `Models/Reservation.cs`, `Models/Enums/ReservationStatus.cs` |

`CreateReservationAsync` re-checks **everything** on the server (never trusts the form):
1. Party size 1-8 and opening hours.
2. The customer has no other booking at the same time.
3. The chosen table is **still** free (someone may have booked it since the page loaded).
4. Saved as **Pending**; staff confirm it (PB12). End time = start + 90 min.
5. The customer gets a notification (PB20).

**Examiner questions**
- *Why check again in the service if the page already showed free tables?* Two customers can pick the same table at the same moment; only the server check is reliable.
- *Why the 15-minute buffer?* Time to clear and reset the table between parties.

## PB11: Modify or cancel my reservation

| | |
|---|---|
| Pages | `Pages/Reservations/Index` (list + Cancel button), `Edit` |
| Service | `UpdateReservationAsync`, `CancelReservationAsync`, `EvaluateModificationPolicy` |

- `EvaluateModificationPolicy`: cannot change a booking that is Cancelled, Completed or NoShow, already started, or **less than 2 hours away**.
- Editing runs the same checks as a new booking, but ignores the booking itself (`excludeReservationId`) so it does not clash with its own old slot.
- Only the owner can see or change a booking: every query filters by `CustomerId`.

## PB12: Staff view daily reservations and confirm / modify / cancel

| | |
|---|---|
| Pages | `Pages/Staff/Reservations/Index` (filters + status buttons), `Edit` |
| Service | `GetStaffDailyReservationsAsync`, `UpdateStatusByStaffAsync`, `UpdateReservationByStaffAsync` |

- Filters: date (or all dates), status, dining area, search (customer name, email, table, request), only-with-requests.
- Status buttons: Confirm, Complete, Cancel, No-show. Reactivating a cancelled/no-show booking is allowed only if its table is still free.
- Staff edit can skip the advance-notice rules (phone bookings) but **not** the overlap and capacity rules.
- Confirm / Cancel / Complete notify the customer; Complete also invites them to leave feedback.

## PB13: Customer requests, preferences and history for staff

| | |
|---|---|
| Page | `Pages/Staff/Reservations/CustomerHistory` |
| Service | `GetCustomerReservationProfileAsync`, `GetReservationsWithRequestsAsync` |

- Totals: bookings, completed, cancelled, no-shows.
- Preferred dining area = the area they booked most; typical party = average party size; up to 8 past special requests.

## PB17: Staff record payments and manage billing

| | |
|---|---|
| Pages | `Pages/Staff/Billing/Index` (unpaid bills, look up by order number), `Bill` |
| Service | Jordan's `IPaymentService`: `GetUnpaidOrdersAsync`, `GetBillAsync`, `RecordCashPaymentAsync`, `RefundPaymentAsync` |

- Cash: the amount tendered must be at least the total; the change is shown on the receipt. The payment amount is always the order total from the database.
- The staff member who took the cash is saved on the payment (`RecordedByUserId`).
- Refund: a Paid payment becomes Refunded.

## PB19: Ratings and feedback after a completed meal

| | |
|---|---|
| Pages | `Pages/Feedback/Index` (customer), `Pages/Staff/Feedback/Index` (staff, star filter) |
| Service | `FeedbackService.cs` |
| Model | `Feedback` (for a booking **or** an order) |

- `GetEligibleVisitsAsync`: only **Completed** bookings and orders that are not rated yet.
- `SubmitFeedbackAsync`: 1-5 stars; the visit must be the customer's own, Completed, and not rated before. All checked on the server.
- The dropdown value is `R:12` (booking) or `O:34` (order), so one list holds both kinds.

## PB20: Notifications (reservation, order, payment)

| | |
|---|---|
| Page | `Pages/Notifications/Index` (list, mark read, mark all read), bell count in the navbar |
| Service | `NotificationService.cs` (`INotificationService.SendAsync`) |
| Model | `Notification` (`IsRead`) |

- Every domain calls `SendAsync(userId, type, message)`: bookings (yours), orders and payments (Jordan's).
- Saves an unread notification, then emails a copy (PB26).
- Mark read only works on the user's own notifications.
- **Gap:** "promotional updates" are not sent as notifications (promotions are shown on the home and menu pages). Needs Carynn's `PromotionService` to call `SendAsync` for customers.

## PB25: Table occupancy analytics

| | |
|---|---|
| Page | `Pages/Admin/Occupancy/Index` |
| Service | `OccupancyService.cs` (reads `IReservationService`, `IDiningAreaService`, `IOperatingHoursService` only) |

- Peak hours (bar chart) and a day x hour heatmap of bookings.
- **Utilisation %** per table = minutes booked (bookings x 90) / minutes the table was open in the range.
- Trend per day (up to 31 days) or per week (longer). Max range 366 days. Cancelled and NoShow are left out.

## PB26: Email confirmations

| | |
|---|---|
| Services | `NotificationService` (builds the email), `SmtpEmailService` (`IEmailService`), options in `appsettings.json` "Email" |

- Every notification also sends an HTML email with category, reference number and amount (found in the message text). Everything put in the email is **HTML-encoded**.
- Demo mode (`UsePickupDirectory: true`): emails are written as `.eml` files in `App_Data/MailPickup` instead of being sent, so the demo works offline. Real SMTP is one setting away (password in user-secrets).
- Email errors are logged and swallowed, so a mail problem never breaks a booking, order or payment.

---

## Open items to mention if asked
- **UX3:** online booking reaches 14 days in the picker and 60 days in the service, not the 1 week the team discussed; the guest limit is the fixed 8, not the largest table.
- **BUG5:** Bootstrap blue is still on Notifications, My bookings and Edit booking.
- Staff/Admin can open the customer `/Reservations` pages (the folder only requires login, not the Customer role).
