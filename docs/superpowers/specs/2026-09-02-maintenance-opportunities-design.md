# Maintenance opportunities as return-customer opportunities

## Purpose

Refactor the Maintenance screen and lifecycle so a maintenance record tracks a
return-customer opportunity rather than the execution of a future service.

An opportunity starts when a completed work order recommends a later
maintenance. It ends when the company converts it to an appointment or quote,
or intentionally closes it without conversion. Appointments, quotes, and work
orders remain independent workflows.

## Scope

The change covers:

- the `MaintenanceOpportunity` lifecycle and persistence;
- automatic conversion during the existing appointment and quote creation
  use cases;
- the existing WhatsApp/contact and contextual booking flows;
- the Maintenance list, details, filters, and discard action;
- a migration that keeps existing maintenance history usable.

It does not create a maintenance-specific scheduling or quote workflow.

## Lifecycle

The supported states are:

```text
Pending
  -> Contacted / AwaitingCustomer
  -> Converted

Pending or Contacted / AwaitingCustomer
  -> ClosedWithoutConversion
```

`AwaitingCustomer` represents an active contextual booking link and replaces
the scheduling-oriented `BookingRequested` label. `CustomerContact` continues
to hold each prepared/sent contact attempt. Preparing a WhatsApp message with a
contextual link keeps the current secure-link behavior and moves an open
opportunity into the customer-waiting state.

`Converted` is terminal. Every newly converted opportunity records:

- `ConversionType` (`Appointment` or `Quote`);
- `ConvertedEntityId`;
- `ConvertedAtUtc`.

`ClosedWithoutConversion` is terminal. It records:

- `ClosedAtUtc`;
- `ClosedByUserId`;
- mandatory `ClosureReason`.

The opportunity never copies appointment or quote data. It keeps only the
reference needed for traceability.

## Existing-flow integration

### Appointment

The Maintenance action navigates to the existing Agenda editor with an
opportunity identifier. The editor resolves the tenant-scoped context on the
server and pre-fills customer, equipment, and service; browser input is never
trusted for that context.

The operator can retain any existing appointment type: `Service`, `Visit`, or
`Other`. A maintenance conversion must not silently rewrite the chosen type.
The existing domain restriction that requires a work order for a service-type
appointment will be relaxed because an appointment may exist independently of
a work order.

`CreateAppointmentUseCase` re-fetches the open opportunity for the current
company immediately before saving. In the same persistence transaction in
which the appointment is saved, it calls the opportunity conversion behavior
with the generated appointment ID. A collision or validation failure creates
neither appointment nor conversion.

### Quote

The Maintenance action opens the existing quote-registration route with an
opportunity identifier. A tenant-scoped use case resolves and pre-fills the
customer, equipment, and primary service. The normal quote editor and its
validation remain the sole quote-creation flow.

`CreateQuoteUseCase` validates that the source opportunity is open and forces
the customer, equipment, and service to the server-resolved opportunity data.
It saves the quote and marks the opportunity converted to that quote atomically.
The quote then follows its normal draft, sent, approval, rejection, and expiry
states without changing the completed opportunity.

## Legacy data compatibility

The status migration preserves historical records rather than deleting or
recreating them:

- `Pending` and `Contacted` retain their meaning;
- `BookingRequested` becomes `AwaitingCustomer`;
- historical `Booked` and `Completed` records become `Converted`;
- `Dismissed` becomes `ClosedWithoutConversion`.

Historical converted rows retain their existing timestamps. The old model did
not persist the created entity reference, so these are shown as legacy
conversions without a link instead of inventing data. New conversions always
have type and ID.

The migration adds the conversion and closure columns and an index suitable
for tenant/status/due-date operational queries. It updates the EF model
snapshot. Existing booking token behavior remains valid for rows migrated to
`AwaitingCustomer`.

## UI

The `/manutencoes` page becomes a compact opportunity list.

Each card shows customer, equipment, recommended service, previous maintenance
date, recommended date, urgency, and opportunity status. The main actions are
`Preparar contato`, `Agendar atendimento`, and `Gerar orçamento`. `Concluir`
is removed. Closing without conversion is a secondary action named
`Descartar oportunidade` and requires a reason in a MudBlazor dialog.

The existing contact history, notes, closing metadata, and conversion
reference move into a MudBlazor expansion/detail surface. A conversion is
rendered only as a concise reference, for example an appointment ID and its
scheduled time, or a quote ID and its current status. The related module owns
all full details.

Summary/filter controls prioritize `Vencidas`, `Próximos 7 dias`, `Próximos
30 dias`, and `Aguardando cliente`. Cards use responsive MudBlazor layout so
mobile displays scan quickly without the contact history forcing tall rows.

## Error handling and authorization

All lookups and mutations remain scoped to the current company. Only owners
retain Maintenance access. Expected conditions--closed opportunity, invalid
state, unavailable appointment, or stale browser context--return existing
operation-result errors and do not create partial records.

## Verification

Automated coverage will verify at least:

- an appointment conversion preserves its selected type and closes the
  opportunity only after creation succeeds;
- quote creation from an opportunity pre-fills/validates the server context
  and records the quote conversion;
- discard requires a reason and records the acting user;
- closed opportunities cannot be converted or contacted;
- legacy status migration maps each previous status as specified;
- list projections expose compact-card and detail data without cross-tenant
  access.

