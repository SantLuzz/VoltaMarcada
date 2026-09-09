# Manual Maintenance Opportunities Design

## Purpose

Allow a company owner to create a maintenance opportunity without a prior
work order. This supports newly onboarded companies that already know a
customer's upcoming maintenance need but have not yet recorded service
history in VoltaMarcada.

## Scope

`MaintenanceOpportunity` remains the single aggregate for maintenance return
work. A prior work order becomes optional only for manually created
opportunities. Customer, equipment, recommended service, due-date strategy,
and notes remain mandatory for every opportunity.

The existing work-order completion flow keeps requiring a prior work order and
continues creating opportunities in the same transaction as the work-order
completion.

## Creation flow

The maintenance opportunities page exposes a **New opportunity** action for
company owners with an operational license. The form collects:

- customer;
- equipment belonging to that customer;
- active recommended service;
- either an interval in days or an explicit due date, matching the work-order
  completion recommendation pattern;
- required notes.

The browser sends only selected identifiers and recommendation data. The
application resolves the authenticated company, validates every selected
customer, equipment, and service within that company, and verifies that the
equipment belongs to the customer. It then persists an open, pending
opportunity with no previous work order.

## Data model and display

`PreviousWorkOrderId` becomes nullable in the domain entity, EF configuration,
and database foreign key. Existing opportunities and work-order-originated
opportunities retain their reference.

Read projections expose an optional previous-work-order completion timestamp.
The opportunities list and detail dialog display **Sem OS anterior** when no
previous work order exists. No public screen exposes whether a specific work
order exists.

## Downstream flows

Manual and work-order-originated opportunities use the same existing flows:

- customer-contact history and WhatsApp preparation;
- contextual booking link generation and revocation;
- appointment scheduling and conversion;
- quote creation and conversion;
- discard and lifecycle status transitions.

Each affected use case and repository projection must tolerate the missing
previous work order without changing tenant filtering or authorization.

## Validation and authorization

Creation uses the existing owner authorization plus operational-license
requirement. Expected failures use `OperationResult`: missing or cross-company
customer, equipment, or service; equipment not owned by the selected customer;
invalid due-date strategy; inactive service; and invalid notes.

## Testing

Tests cover a valid manual creation, company isolation, customer-equipment
ownership, invalid recommendations, nullable persistence and list rendering,
the continued mandatory reference from work-order completion, and regression
coverage for contact, booking, scheduling, quote, and discard paths.
