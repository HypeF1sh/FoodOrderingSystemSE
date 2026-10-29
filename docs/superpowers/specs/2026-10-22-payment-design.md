# Payment — PB16 (pay online + track status + receipt) — Design

- **Owner:** Jordan (Domain C) · **Sprint:** 3 (22–29 Oct 2026) · **Status:** Draft 2026-10-22 — awaiting Jordan's review
- **Branch:** `feature/PB16-payment` off `dev`, created **after** PB14 (ordering) is merged
- **Builds on:** `2026-10-22-ordering-design.md` (Order + Confirmation page), `2026-10-22-staff-orders-design.md`
- **Decisions:** payment is a **simulated gateway** (Option A, chosen 22/10). Receipt folded into PB16 (**D4**).

## 1. Goal
| Item | Outcome |
|---|---|
| PB16 | A customer pays for their own order online (card or e-wallet) from the confirmation page, sees whether the payment went through (Paid / Failed), can retry after a failure, and gets a printable receipt. |
| D4 (receipt) | A professional receipt page per successful payment — screen and print — for online **and** cash payments. |
| PB17 hook | `IPaymentService` exposes the methods Wallace's staff billing (PB17) calls to record **cash** payments. Wallace never writes to `Payments` directly (`team-and-domains.md`). |

**Primary flow (phone, at the table):** `/Orders/Confirmation/{id}` → **Pay now** → `/Payments/Pay/{orderId}`
(choose Card or E-wallet, enter details) → success → `/Payments/Receipt/{paymentId}` · failure → back on the Pay page
with the reason and the failed attempt listed → retry.

**Out of scope:** real payment provider, promotions/discounts (PB07 — see §10 Q3), payment history list (PB18),
admin payment monitoring (PB21, Carynn — reads through `IPaymentService`, see §5), notifications (PB20), email receipts (PB26).

## 2. Constraints
- Strict 3-tier: pages → `IPaymentService` → `ApplicationDbContext`. Pages never touch the DbContext.
- **No schema change.** Uses existing `Payment` (OrderId, RecordedByUserId?, Amount, Method, Status, TransactionRef, PaidAt)
  and the `Order.Payments` collection. Enums `PaymentMethod` (Card, EWallet, Cash) and `PaymentStatus` (Unpaid, Paid, Failed, Refunded) already exist.
- No new NuGet packages, no API keys, no user-secrets.
- Forms bind to input models in `ViewModels/`. **The amount is never taken from the form** — always `Order.TotalAmount` from the database.
- **Card number and CVV are never stored, logged, or echoed back** to the page (see §6 security rules).

## 3. Simulated gateway — how "online payment" works
| Part | Decision |
|---|---|
| Seam | `IPaymentGateway` with one method `ChargeAsync(amount, details)` → `(bool Approved, string Reference, string? DeclineReason)`. `SimulatedPaymentGateway` is the only implementation. `PaymentService` depends on the interface, so a real provider (Stripe, etc.) could replace it later **without touching pages or `PaymentService`** — a clear viva point for the 3-tier / dependency-injection marks. |
| Approved | Generates `TransactionRef = "SIM-yyyyMMdd-XXXXXXXX"` (8 random uppercase hex chars). |
| Declined (test values) | Card `4000 0000 0000 0002` → "Card declined by issuer." · E-wallet phone ending `0000` → "E-wallet has insufficient balance." Every other valid input is approved. |
| Delay | None (no fake spinner). Keeps tests fast; mention in Ch.7 that a real gateway is asynchronous / uses webhooks. |

Test values are listed in a small "Demo mode" note on the Pay page so the examiner can try both paths.

## 4. Components
| Layer | File | Responsibility |
|---|---|---|
| Business | `Services/IPaymentService.cs`, `PaymentService.cs` | Pay-online, cash recording (PB17), payment status, receipt data, all payment rules (§6) |
| Business | `Services/IPaymentGateway.cs`, `SimulatedPaymentGateway.cs` | Approve / decline a charge and issue a reference. No database access |
| ViewModels | `PaymentInput` | Form: `Method` (Card / EWallet), `CardHolderName`, `CardNumber`, `Expiry` (MM/YY), `Cvv`, `WalletPhone` |
| ViewModels | `PaymentPageViewModel` | Order no., lines, total, `IsPaid`, paid payment id (for the receipt link), `CanPay` + reason, list of attempts |
| ViewModels | `PaymentAttemptViewModel` | Time, method, status, reference — one row per `Payment` |
| ViewModels | `OrderPaymentSummary` | `Status` (Unpaid / Paid / Failed / Refunded) + paid payment id — used by the Confirmation page, the staff board and PB18 |
| ViewModels | `ReceiptViewModel` | Receipt no., restaurant name, order no., date/time, customer name, lines, total, method, reference, recorded-by (cash), amount tendered/change (cash, display only) |
| ViewModels | `BillViewModel`, `UnpaidOrderRowViewModel` | For PB17 (Wallace): one order's bill; unpaid orders list |
| Pages | `Pages/Payments/Pay` | `[Authorize(Roles = "Customer")]`, `{orderId:int}`. Owner only, otherwise 404 |
| Pages | `Pages/Payments/Receipt` | `[Authorize]`, `{paymentId:int}`. Customer: own order only; Staff/Administrator: any. Otherwise 404 |
| Pages | `Pages/Payments/_PaymentStatusBadge` | Payment badge partial (Unpaid `bg-warning text-dark`, Paid `bg-success`, Failed `bg-danger`, Refunded `bg-secondary`) |
| Pages | `Pages/Orders/Confirmation` (edit, own file) | Payment badge + **Pay now** (unpaid) or **View receipt** (paid) — replaces the PB16 placeholder comment |
| Pages | `Pages/Staff/Orders/Index` (edit, own file) | Payment badge on each order card so staff can see who still owes |

## 5. Service contract — `IPaymentService`
All mutating methods return a tuple, same shape as `IOrderService` / `IUserAdminService`. On failure the page turns
`Message` into `ModelState.AddModelError`; on success it goes into `TempData["SuccessMessage"]`.

**Customer (PB16)**
| Method | Notes |
|---|---|
| `GetPaymentPageAsync(int orderId, string customerId)` → `PaymentPageViewModel?` | `null` if missing or someone else's order → 404 |
| `PayOnlineAsync(int orderId, string customerId, PaymentInput input)` → `(bool Success, string Message, int? PaymentId)` | Runs §6 checks, calls the gateway, saves **one** `Payment` row either way (Paid or Failed). `PaymentId` is set only when Paid |
| `GetPaymentSummaryAsync(int orderId)` → `OrderPaymentSummary` | Badge on Confirmation / staff board / PB18 history |

**Receipt (D4) — customer and staff**
| Method | Notes |
|---|---|
| `GetReceiptAsync(int paymentId, string userId, bool isStaffOrAdmin)` → `ReceiptViewModel?` | Only **Paid** (or later Refunded) payments have a receipt; Failed → `null`. Customer can only see their own; staff/admin any. `null` → 404 |

**Staff billing — for Wallace's PB17**
| Method | Notes |
|---|---|
| `GetUnpaidOrdersAsync(DateOnly date)` → `List<UnpaidOrderRowViewModel>` | Orders placed that day, not Cancelled, with no Paid payment. Oldest first |
| `GetBillAsync(int orderId)` → `BillViewModel?` | Lines, total, payment summary, attempts. `null` if missing |
| `RecordCashPaymentAsync(int orderId, decimal amountTendered, string staffUserId)` → `(bool Success, string Message, int? PaymentId, decimal Change)` | Saves `Method = Cash`, `Status = Paid`, `Amount = Order.TotalAmount`, `RecordedByUserId = staffUserId`, `TransactionRef = "CASH-yyyyMMdd-XXXXXXXX"`. `Change = amountTendered − total`. Tendered/change are **returned, not stored** (no column) — the receipt page shows them only right after recording (passed via `TempData`) |
| `RefundPaymentAsync(int paymentId, string staffUserId)` → `(bool Success, string Message)` | **Optional — see §10 Q1.** Paid → Refunded; order may then be cancelled |

**Admin monitoring (Carynn, PB21)** can reuse `GetBillAsync` / `GetPaymentSummaryAsync`, or ask for a
`GetPaymentsAsync(DateOnly from, DateOnly to)` list — agree in standup, not built in v1.

## 6. Business rules
| Rule | Where |
|---|---|
| An order can be paid only if it exists, is **not Cancelled**, and has **no Paid payment** yet | `PaymentService` (both online and cash) |
| At most one Paid payment per order. Checked again on POST, not only when the page loads (double-click / second tab) | `PaymentService` |
| `Amount` = `Order.TotalAmount` read from the database — never from the form | `PaymentService` |
| Every attempt is saved — a declined card leaves a `Failed` row. This is what "track payment status" shows | `PaymentService` |
| A customer can retry after `Failed`; the order's overall status is `Paid` as soon as any attempt is Paid | `PaymentService.GetPaymentSummaryAsync` |
| `PaidAt` = attempt time, set explicitly with `DateTime.Now` (restaurant local time, same as `Order.CreatedAt` in PB14). For Failed rows it is the attempt time — noted because the column name says "Paid" | `PaymentService` |
| Payment is independent of kitchen status — a customer may pay at any point from Pending to Completed | `PaymentService` |
| **Card:** holder name 2–100 chars; number 13–19 digits (spaces allowed, stripped) **and passes the Luhn check**; expiry `MM/YY`, valid month, not in the past; CVV 3–4 digits | Format: data annotations on `PaymentInput`. Luhn + expiry: `PaymentService` (business rule, testable, explainable) |
| **E-wallet:** Malaysian mobile number `01` + 8–9 digits | `[RegularExpression]` on `PaymentInput` |
| Fields for the method **not** chosen are ignored (and their ModelState errors removed) | Pay page |
| **Cash (PB17):** `amountTendered` ≥ total and ≤ total + RM 1,000 (typo guard); `staffUserId` required | `PaymentService` |
| **Cancelling a paid order is refused** — "This order has been paid. Refund it first." | `OrderService.CancelOrderAsync` (own file, small change) |

**Security rules (card data)**
- Card number and CVV are passed to the gateway in memory and then discarded. They are **not** saved to `Payments`,
  not written to logs, not put in `TempData`/session.
- On any validation error or decline, the page clears `CardNumber` and `Cvv` before re-rendering (`ModelState.Remove` + empty value), so they are never sent back in HTML.
- Inputs use `autocomplete="cc-name" / "cc-number" / "cc-exp" / "cc-csc"`, `inputmode="numeric"`; CVV is `type="password"`.
- Anti-forgery token is on by default for Razor Pages POSTs; the site already uses HTTPS redirection.
- Viva point: a real system never touches raw card numbers — the provider's hosted form does (PCI DSS). The simulation keeps the same "don't store it" discipline.

## 7. UI / responsive design
Phone-first like `/Menu` and `/Orders/Cart`; Bootstrap breakpoints only.
- **`/Orders/Confirmation/{id}` additions:** payment badge next to the order status badge; **Pay now** (`btn-primary`) when unpaid;
  **View receipt** when paid; a small line "Prefer cash? Pay at the counter." The text "You can pay at the counter or online." stays.
- **`/Payments/Pay/{orderId}`:** card `shadow` with order summary (lines collapsed to "3 items" on phones, expandable) and **Total RM x**.
  Method chooser: two large radio "tiles" — Card · E-wallet. A few lines of vanilla JS show the matching fieldset;
  without JS both fieldsets show and the radio still decides (progressive enhancement).
  Card: name, number (groups of 4 as typed), `MM/YY` + CVV side by side. E-wallet: phone number.
  Button **Pay RM 45.00** (`btn-success`, full width on phones, disabled after first click to stop double submits).
  "Demo mode" note with the test values (§3). Below: **Payment attempts** table (time · method · status badge · reference).
  If already paid: no form — "This order is paid" + **View receipt**. If cancelled: "This order was cancelled" and no form.
- **`/Payments/Receipt/{paymentId}`:** narrow centred card (max ~420px, like a till slip). Restaurant name, "Receipt R-000123",
  date/time, order no., customer name, lines with quantities, total, method (`Card` / `E-wallet` / `Cash`), reference,
  **PAID** stamp badge, staff name for cash, tendered/change for cash (only right after recording), "Thank you for dining with us."
  Buttons **Print** (`window.print()`) and **Back to my order** / **Back to billing** (staff).
- **Print:** buttons use Bootstrap's `d-print-none`; the navbar and footer are hidden with a small `<style>` block inside
  the receipt page (`@media print { header, footer { display: none; } }`) — **no change to the shared `_Layout.cshtml`**.
- Money `RM 0.00`; time `hh:mm tt`; date `dd/MM/yyyy`. Receipt no. = `R-` + `PaymentId` padded to 6 digits.
- Restaurant name on the receipt: the same placeholder constant as the layout in v1 (`Rumah Rasa`); read from Carynn's
  settings once PB07 is merged (§10 Q4).

## 8. Shared-file changes — need group agreement
| File | Change |
|---|---|
| `Program.cs` | `AddScoped<IPaymentService, PaymentService>()` and `AddScoped<IPaymentGateway, SimulatedPaymentGateway>()` — ask together with the PB14 session lines |
| `_Layout.cshtml` | **None** |

## 9. Cross-domain agreement — Wallace (PB17)
| Topic | Proposal |
|---|---|
| Methods Wallace calls | `GetUnpaidOrdersAsync`, `GetBillAsync`, `RecordCashPaymentAsync`, `GetReceiptAsync` (+ `RefundPaymentAsync` if agreed) — §5 |
| Where his page lives | `Pages/Staff/Billing/` (his folder). After recording cash he redirects to `/Payments/Receipt/{paymentId}` (mine) for printing |
| Staff user id | He passes `UserManager.GetUserId(User)`; the service stores it in `RecordedByUserId` |
| Card payments at the counter | Out of scope — staff record Cash only; customers pay Card/E-wallet themselves online |
| Timing | Interface + view models pushed first (small PR) so he can code against it before PB16's pages are done |

## 10. Open questions
| # | Question | Recommendation |
|---|---|---|
| Q1 (Wallace) | Do staff need **refunds** in PB17? | Yes, but simple: Paid → Refunded only, staff/admin only, confirmation page, no partial refunds. Drop if Wallace's time is short — `Refunded` stays unused |
| Q2 (Jordan) | Store the card's last 4 digits for the receipt? | No column exists. Option: append to the reference, e.g. `SIM-20261022-AB12CD34-4242` (fits in 100 chars). Recommendation: **don't** in v1 — keeps "we store nothing from the card" simple to defend |
| Q3 (Carynn) | Apply an active promotion's `DiscountPercent` at payment? | Not in v1. Amount must equal `Order.TotalAmount`; a discount belongs at checkout (PB14) so the order total, payment and receipt all agree. Revisit after PB07 merges |
| Q4 (Carynn) | Restaurant name / address / SST no. on the receipt from `RestaurantSettings`? | Yes once PB07 merges: read keys via `IRestaurantSettingService.GetAllAsync()`; agree key names (`RestaurantName`, `Address`, `Phone`) with Carynn |
| Q5 (Wallace) | Notify the customer when a payment succeeds/fails (PB20)? | Call `INotificationService` once it exists — same as PB14 Q4 |

## 11. Testing (manual, written up in `docs/report-evidence.md`)
| ID | Steps | Expected |
|---|---|---|
| TC-P01 | Customer opens Confirmation for an unpaid order | Payment badge **Unpaid**, **Pay now** shown |
| TC-P02 | Pay with card `4242 4242 4242 4242`, future expiry, CVV 123 | Receipt shown; `Payments` row: Card, Paid, Amount = order total, `SIM-…` ref; Confirmation now **Paid** + View receipt |
| TC-P03 | Pay with card `4000 0000 0000 0002` | "Card declined by issuer."; Failed attempt listed; Failed row in DB; card number and CVV fields empty |
| TC-P04 | After TC-P03, pay again with `4242…` | Paid; attempts list shows Failed then Paid |
| TC-P05 | Card `4242 4242 4242 4241` (fails Luhn); expiry `01/24`; month `13/27`; CVV `12` | Field errors each time, nothing saved, no gateway call |
| TC-P06 | E-wallet `0123456789`; then `0123450000` | Paid; then "insufficient balance" Failed |
| TC-P07 | Paid order: open Pay page; crafted POST to pay again | "already paid", no form; POST refused, still one Paid row |
| TC-P08 | Crafted POST with `Amount=1.00` added | Ignored — saved amount = order total |
| TC-P09 | Customer B opens A's Pay page / A's receipt id | 404 both |
| TC-P10 | Cancelled order → Pay page; paid order → staff Cancel | No form / "has been paid. Refund it first." |
| TC-P11 | Receipt → Print (browser print preview) | Only the receipt; no navbar, footer or buttons |
| TC-P12 | (PB17 contract, until Wallace's page exists — temporary test page or SQL check) `RecordCashPaymentAsync` with tendered < total, then ≥ total | Refused; then Paid Cash row with `RecordedByUserId`, change correct, receipt shows staff name |
| TC-P13 | Staff opens a customer's receipt; customer opens a Failed payment's receipt id | Shown; 404 |

Plus: `dotnet build` with no new warnings; Pay and Receipt checked at 375px, 768px, 1280px; search the solution and logs
to confirm no card number is written anywhere.

## 12. Build order
1. Branch (after PB14 merges) → 2. `IPaymentGateway` + `SimulatedPaymentGateway` → 3. ViewModels →
4. `IPaymentService` + `PaymentService` → **push the interface early for Wallace (§9)** → 5. `Payments/Pay` →
6. `Payments/Receipt` + print CSS → 7. Confirmation + staff board badges, `CancelOrderAsync` paid-order guard →
8. group OK for §8 → `Program.cs` lines → 9. manual tests → 10. update `product-backlog.md`, `progress.md`, `report-evidence.md`.
