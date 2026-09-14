# Cash Screen and Dashboard Financial KPIs Design

## Purpose

Move payment operations out of work orders into an owner-only daily cash screen. A completed work order with a positive total creates a pending cash entry automatically. Owners can also create manual entries with an optional work-order link.

## Cash Screen

The Cash page opens on the company local current date and displays the selected date prominently. Its default daily mode filters entries by their creation business date.

The page displays three daily totals:

- Total cash entries
- Total pending confirmation
- Total confirmed

A date picker lets the owner view any single day. There is no date-range filter.

The **Pending confirmation** filter is mutually exclusive with the date filter. It removes the date restriction and shows every pending entry for the company, ordered by creation date.

Each entry displays its amount, payment method, status, creation date, and optional originating work order. Owners can create a manual entry for the selected date, optionally select a work order, edit a pending entry, confirm it, or delete it.

## Work-Order Completion

The work-order page does not display payment fields, payment dialogs, installment tables, or payment actions.

When an owner or authorized professional completes a work order with payment control enabled and a positive total, the application creates one pending entry for the full work-order amount. The entry's business date is the completion date in the company timezone. The work order completes without requiring payment data in its UI.

A work order marked as no-charge does not create a cash entry.

## Data Model

The existing work-order charge concept becomes a general cash-entry model. A cash entry belongs to a company and has an optional work-order link. Existing work-order charge records are migrated without losing their links, amounts, methods, statuses, audit data, or dates.

Cash entries retain their creation business date. Confirmation changes only the status and confirmation audit timestamp; it never moves an entry to a different cash day.

All queries and mutations enforce company isolation. Only company owners can view or manage the Cash page and its entries.

## Dashboard KPIs

The operational dashboard contains clickable financial KPIs:

- **Cash today** opens Cash in the current-day view.
- **Confirmed today** opens Cash in the current-day view.
- **Cash pending confirmation** opens Cash with the all-days pending filter active.

The dashboard's cash figures use the company local date and only authorized tenant data.

## Error Handling and Validation

Manual entries require a positive amount and a valid payment method. When linked, the work order must belong to the current company. Pending entries may be edited, confirmed, or deleted; confirmed entries remain auditable and follow the existing status rules.

The application revalidates every owner action on the server. UI filters are not authorization boundaries.

## Verification

Automated tests cover daily filtering, all-days pending filtering, totals, tenant isolation, optional work-order links, automatic entry creation on work-order completion, manual entry validation, and dashboard navigation URLs. UI tests cover that the work-order page no longer renders payment controls and that the Cash page exposes owner actions.
## Payment-method timing

An automatic charge created when a work order is completed remains pending without a payment method. The owner supplies the payment method (and Other description when applicable) when confirming it in Cash. A manual Cash payment is created already confirmed and therefore requires a valid payment method. This prevents the work-order flow from inventing a payment method.
