# Requirements — RROMS (5026CMD CW1)

Summary of the assignment brief. The brief is the authority if anything here disagrees.

## The task
Build a web-based Restaurant Reservation and Order Management System in C#, with a
user-friendly GUI, deployable as a web application. Three roles: Administrator, Restaurant Staff, Customer.
Group-built system, **individually assessed**: each student is marked on their own source code,
their own report, and an individual viva.

## Functional requirements (by module)
### 1. Administrator
| Ref | Requirement | Backlog |
|---|---|---|
| 1a | Log in securely | PB04 |
| 1b | Manage dining areas, tables, operating hours, menu categories | PB05, PB06 |
| 1c | Manage customer and staff accounts, registration, role assignment | PB04 |
| 1d | Manage menu items, pricing, availability, promotional offers | PB06, PB07 |
| 1e | Monitor reservations, orders, payment transactions | PB21 |
| 1f | Reports: reservation statistics, sales, popular items, customer activity | PB22, PB23 |
| 1g | Manage announcements, promotional campaigns, restaurant settings | PB07 |

### 2. Restaurant Staff
| Ref | Requirement | Backlog |
|---|---|---|
| 2a | Log in and manage personal profile | PB03 |
| 2b | View and manage daily table reservations | PB12 |
| 2c | Confirm, modify, cancel reservations | PB12 |
| 2d | Receive, update, process customer orders | PB15 |
| 2e | Update order status: Preparing, Ready to Serve, Served, Completed, Cancelled | PB15 |
| 2f | Record payment status and manage billing | PB17 |
| 2g | View customer requests, dining preferences, reservation history | PB13 |

### 3. Customer
| Ref | Requirement | Backlog |
|---|---|---|
| 3a | Register and log in securely | PB01, PB02 |
| 3b | Manage personal profile | PB02 |
| 3c | Browse digital menu with descriptions, pricing, availability | PB08 |
| 3d | Make, modify, cancel reservations based on table availability | PB09–PB11 |
| 3e | Place food and beverage orders online | PB14 |
| 3f | View reservation, order and payment history | PB18 |
| 3g | Track reservation and order status in real time | PB18 |
| 3h | Submit ratings and feedback for completed visits | PB19 |
| 3i | Notifications: reservation confirmations, order updates, promotions, payments | PB20 |

## Performance levels
| Level | Items |
|---|---|
| **Basic** | Registration + secure login · reservation + cancellation · profile management · menu browsing + ordering · basic reservation/order/payment records |
| **Intermediate** | Automated availability checking + validation · real-time order status · **professional reservation confirmation and payment receipt** · online payment + status tracking · filtered reports · date-filtered reports (daily/weekly/monthly) |
| **Advanced** | Role-based access control · occupancy and reservation analytics · interactive dashboard with charts · export to CSV/PDF/Excel · email or system notifications · AI chatbot · recommendation system · multi-user auth with role permissions |

## Mandatory technology
HTML, CSS, Bootstrap, JavaScript · ASP.NET Core Web App (Razor Pages) · C# · Visual Studio 2026 · SQL Server (SSMS)

## System development marking (40 marks)
| Criterion | Marks | Top band looks like |
|---|---|---|
| Functional requirements | 10 | All modules working |
| System architecture | 5 | 3-tier correctly implemented, maintainable, scalability considered |
| Database design & integration | 5 | Normalised, proper FKs and constraints, efficient queries |
| UI & usability | 5 | Professional, consistent, responsive, clear workflow, usability principles applied |
| Features & innovation | 5 | Additional features, automation, analytics |
| Code quality & structure | 5 | Clean, modular, well-named, documented, reusable |
| Team contribution evidence | 5 | Strong Git history, clear individual ownership of features |

Overall: system 40% · individual report 50% · viva 10%.

## Submission
- Individual report as one PDF: `FullName_INTIID_5026CMD_Report.pdf`
- GitHub link. **Branches, commits, pull requests and merges are checked.** A single bulk upload scores less.
  The repo must be accessible to the module leader at marking time.
- Any AI tool use must be declared in the report (which tools, for what purpose).
- Deadline Fri 6 Nov 2026 (Week 12). Late = zero. Viva in Weeks 13–15.
