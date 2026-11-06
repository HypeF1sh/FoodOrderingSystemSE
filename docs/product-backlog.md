# Product Backlog

Status values: `Todo` · `In progress` · `Done` · `Blocked`
Update the Status column when an item changes. Owner domain: A = Carynn, B = Wallace Chan, C = Jordan.

## Core items (PB01–PB23)
| ID | User story | Priority | Level | Owner | Sprint | Status |
|---|---|---|---|---|---|---|
| PB01 | As a customer, I want to register an account. | High | Basic | A | 1 | Done |
| PB02 | As a customer, I want to log in securely and manage my profile. | High | Basic | A | 1 | Done |
| PB03 | As a staff member, I want to log in and manage my profile. | Medium | Basic | A | 1 | Done |
| PB04 | As an administrator, I want to log in securely, manage user accounts, assign roles, and restrict access by role. | High | Basic + Advanced | A | 1 | Done |
| PB05 | As an administrator, I want to manage dining areas, tables and operating hours. | High | Basic | A | 1 | Done |
| PB06 | As an administrator, I want to manage menu categories, menu items, pricing and availability. | High | Basic | C | 2 | Done |
| PB07 | As an administrator, I want to manage promotional offers, system announcements and restaurant settings. | Medium | Basic | A | 2 | Done |
| PB08 | As a customer, I want to browse the digital menu. | High | Basic | C | 2 | Done |
| PB09 | As a customer, I want to check table availability by date, time and party size. | High | Basic | B | 2 | Done |
| PB10 | As a customer, I want to make a table reservation with automatic availability validation. | High | Basic + Intermediate | B | 2 | Done |
| PB11 | As a customer, I want to modify or cancel my reservation. | Medium | Basic | B | 2 | Done |
| PB12 | As a staff member, I want to view daily reservations and confirm, modify or cancel them. | High | Basic | B | 3 | Done |
| PB13 | As a staff member, I want to view customer requests, dining preferences and reservation history. | Medium | Basic | B | 3 | Done |
| PB14 | As a customer, I want to place a food and beverage order. | High | Basic | C | 3 | Done (pre-orders 2/11, table QR ordering 3/11, in review) |
| PB15 | As a staff member, I want to receive orders and update their status. | High | Intermediate | C | 3 | Done |
| PB16 | As a customer, I want to pay online and track my payment status. | High | Intermediate | C | 3 | Done |
| PB17 | As a staff member, I want to record payment status and manage billing. | Medium | Basic | B | 3 | Done |
| PB18 | As a customer, I want to view my reservation, order and payment history and track status in real time. | Medium | Basic + Intermediate | C | 4 | Done |
| PB19 | As a customer, I want to submit ratings and feedback after a completed meal. | Medium | Basic | B | 4 | Done |
| PB20 | As a customer, I want to receive notifications for reservation, order, payment and promotional updates. | Medium | Advanced | B | 4 | In progress |
| PB21 | As an administrator, I want to monitor all reservations, orders and payment transactions. | High | Basic | A | 3 | Done |
| PB22 | As an administrator, I want a dashboard with charts and date-filtered reports for sales, reservations and popular menu items. | High | Intermediate + Advanced | C | 4 | Done |
| PB23 | As an administrator, I want to view customer activity reports. | Medium | Advanced | A | 4 | Done |

Sprint 1 also includes non-PB setup work: database schema + ER model + seed data (B) and the
Bootstrap layout shell, navigation and styling (C).

## Gap to resolve
Intermediate requirement 3 — *professional reservation confirmation and payment receipt
generation* — is not covered by any PB item. Proposed fix (needs team agreement):
- Reservation confirmation page/printout → fold into **PB10** (B)
- Payment receipt page/printout → fold into **PB16** (C)

## Optional extras (only if core is on schedule)
| ID | User story | Priority | Owner | Status |
|---|---|---|---|---|
| PB24 | As an administrator, I want to export reports to CSV, PDF or Excel. | Medium | C | Todo |
| PB25 | As an administrator, I want table occupancy analytics showing peak hours, trends and utilisation. | Medium | C | Todo |
| PB26 | As a customer, I want email confirmations for reservations, orders and payments. | Medium | B | Todo |
| PB27 | As a customer, I want an AI chatbot that shows its progress in real time and lets me stop it. | Medium | C (taken over by Jordan from A) | Done (6/11, `feature/PB27-chatbot`, in review) |
| PB28 | As a customer, I want personalised dish recommendations based on my order history. | Low | C | Todo |

Build order for extras: PB24 → PB26 → PB25 → PB27 → PB28.
