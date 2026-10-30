# Admin dashboard — PB22 (charts + date-filtered reports) — Design

- **Owner:** Jordan (Domain C) · **Sprint:** 4 (29 Oct–3 Nov 2026) · **Status:** Draft 2026-10-30
- **Branch:** `feature/PB22-dashboard` (off `dev` after PR #49)

## 1. Goal
| Item | Outcome |
|---|---|
| PB22 | The administrator opens one page and, for any date range, sees sales, reservations and the most popular dishes as headline numbers, interactive charts and tables. |

Covers requirement 1f (reports: reservation statistics, sales, popular items) at **Intermediate**
(filtered, date-filtered reports by day / week / month) and **Advanced** (interactive dashboard with charts).
Customer activity (PB23) is Carynn's; CSV/PDF export is PB24 (later).

## 2. Constraints and decisions
| Decision | Who |
|---|---|
| Chart.js 4.4.1, one file in `wwwroot/lib/chart.js/` (with its MIT licence), no NuGet | Jordan agreed 30/10 |
| One `AddScoped<IReportService, ReportService>()` line in `Program.cs` | Jordan, group agreed (same as earlier lines) |
| "Dashboard" card on the Configurations hub (`Pages/Admin/Index`, Carynn's page) | Jordan agreed 30/10, **Carynn to OK in PR** |
| No schema change. Profit is not shown (no cost price; see D5) | — |
| Pages → `IReportService` → DbContext (own tables) + `IReservationService` (Wallace's, cross-domain rule) | architecture |
| `/Admin` folder is `AdminOnly`; the PageModel also gets `[Authorize(Roles = Administrator)]` | — |

## 3. Components
| Layer | File | Responsibility |
|---|---|---|
| Business | `IReportService` / `ReportService` | `GetDashboardAsync(from, to, grouping)`: headline numbers, sales per period, bookings per period, booking status counts, top 5 dishes |
| ViewModels | `DashboardViewModel`, `ReportPointViewModel`, `PopularItemViewModel`, `ReportGrouping` | Data shown on the page; grouping is a screen choice (Day / Week / Month), never stored |
| Pages | `Pages/Admin/Dashboard/Index` | Filter bar (quick ranges + from/to + group by), 4 stat tiles, 3 charts, a table under each chart |
| Script | `wwwroot/js/admin-dashboard.js` | Reads the page's JSON block and draws the charts with Chart.js |

## 4. Report rules (all in `ReportService`)
| Number | Rule |
|---|---|
| Revenue | Sum of `Payment.Amount` where `Status = Paid` and `PaidAt` is in the range. Refunded payments change to `Refunded`, so they drop out by themselves. Grouped by **payment** date. |
| Paid orders | Distinct orders with a Paid payment in the range. **Average order value** = revenue ÷ paid orders (0 when none). |
| Orders | Orders created in the range, **not Cancelled**. |
| Bookings | Reservations whose `ReservationDate` is in the range, **not Cancelled**; covers = sum of party sizes. Status counts show every status, Cancelled and No-show too. |
| Popular dishes | `OrderItems` of non-cancelled orders created in the range, grouped by dish: quantity sold and item sales (qty × unit price). Top 5 by quantity, then sales. |
| Range | Both ends included. `from` after `to` or a range over **366 days** is refused with a message. |
| Periods | Day = each date; Week = Monday-start weeks; Month = calendar months. Every period in the range is listed, **zeros included**, so gaps show on the chart. Default grouping: Day up to 31 days, Week up to 92, else Month. |
| Default range | Last 7 days ending today (restaurant local time). |

## 5. Charts (dataviz rules)
- Three **single-series bar charts** (sales per period, bookings per period, top dishes as horizontal bars): one accent colour (`--rr-accent`), so no legend, no categorical palette. No pie charts, no dual axes.
- Hover tooltips (Chart.js default), recessive grid, rounded bar ends, RM formatting on money.
- Each chart has a plain table beneath it (accessibility, and a fallback if the script fails).

## 6. Tests (manual)
| ID | Steps | Expected |
|---|---|---|
| TC-D01 | Admin opens `/Admin/Dashboard` | Last 7 days, 4 tiles, 3 charts, tables |
| TC-D02 | Pick "This month", Group by Week | Bars per Monday-start week, totals unchanged |
| TC-D03 | Pay an order, reload | Revenue +amount on today's bar |
| TC-D04 | Staff refunds that payment, reload | Revenue drops back |
| TC-D05 | Cancel an order | Not counted in Orders or Popular dishes |
| TC-D06 | From after To; range > 366 days | Error message, no charts |
| TC-D07 | Staff or customer opens the URL | Access denied |
| TC-D08 | Range with no data | Zeros, "No orders in this range" in the dishes table |
