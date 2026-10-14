# Team and Domains

Work is split by **domain**, not by sprint: everyone works in every sprint, each on their own area.
This keeps Git contribution even and gives each member a clear ownership story for the viva.

| Domain | Owner | Core items | Extras | Sprint 1 setup task |
|---|---|---|---|---|
| A — Accounts & Administration | Carynn | PB01–PB05, PB07, PB21, PB23 | PB27 | Auth, roles, table/area admin |
| B — Reservations & Staff Operations | Wallace Chan | PB09–PB13, PB17, PB19, PB20 | PB26 | Database schema, ER model, seed data |
| C — Menu, Orders & Payment | Jordan | PB06, PB08, PB14–PB16, PB18, PB22 | PB24, PB25, PB28 | Layout shell, navigation, styling |

## Scrum roles
| Role | Who |
|---|---|
| Scrum Master | **Jordan** — runs sprint planning and the twice-weekly standups, chases blockers, keeps the GitHub Projects board current |
| Product Owner | Represented by the assignment brief; priorities agreed by the team |
| Development Team | Carynn, Wallace Chan, Jordan |

## Sprint plan
| Sprint | Dates | A — Carynn | B — Wallace | C — Jordan |
|---|---|---|---|---|
| 1 | 8–15 Oct | PB01–PB05 | Schema, ER, seed data | Layout, nav, styling |
| 2 | 15–22 Oct | PB07 | PB09, PB10, PB11 | PB06, PB08 |
| 3 | 22–29 Oct | PB21 | PB12, PB13, PB17 | PB14, PB15, PB16 |
| 4 | 29 Oct–3 Nov | PB23 | PB19, PB20 | PB18, PB22 |
| Freeze | 3–5 Nov | Testing, fixes, report | | |

## Code ownership
| | A — Carynn | B — Wallace | C — Jordan |
|---|---|---|---|
| Pages | `Account/`, `Admin/` (users, tables, areas, hours, promotions, settings, monitoring) | `Reservations/`, `Staff/Reservations/`, `Feedback/` | `Menu/`, `Admin/Menu/`, `Orders/`, `Staff/Orders/`, `Payments/`, `Admin/Dashboard/` |
| Services | `UserService`, `TableService`, `PromotionService`, `SettingsService` | `ReservationService`, `FeedbackService`, `NotificationService` | `MenuService`, `OrderService`, `PaymentService`, `ReportService` |
| Tables | Users/Roles, DiningAreas, RestaurantTables, OperatingHours, Promotions, Announcements, RestaurantSettings | Reservations, Feedback, Notifications | MenuCategories, MenuItems, Orders, OrderItems, Payments |

## Cross-domain rules
| Situation | Rule |
|---|---|
| B's staff billing (PB17) records payments, which live in C's Payments table | B calls `IPaymentService`. B never writes to Payments directly. |
| A's monitoring (PB21) and reports read everyone's data | A reads through B's and C's service interfaces only. |
| C's orders may link to a reservation (B) | `Order.ReservationId` is nullable; C reads reservations via `IReservationService`. |
| B's notifications (PB20) fire on order/payment events (C) | C's services call `INotificationService`; B owns how notifications are stored and shown. |
| Shared files: `Program.cs`, `_Layout.cshtml`, `ApplicationDbContext`, migrations, `appsettings.json` | Announce in Discord before editing. Small changes, merge the same day. |
| Database schema | Locked after Sprint 1. Any change is agreed by the group first. |

## Agree interfaces early
When one domain needs another's service before it exists, agree the interface signature first
and stub the implementation. Example: C needs `INotificationService.SendAsync(userId, type, message)`
in Sprint 3 even if B builds the real one in Sprint 4.
