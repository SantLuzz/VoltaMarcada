# Appointment, Quote, and Work Order Flow Refactor Plan

> **For implementation:** Follow the tasks in order and keep the appointment
> calendar independent from commercial-document decisions.

## Context

The quote phase previously resolved an external service request by always
creating an appointment. That coupled commercial resolution to a calendar
commitment: preparing a quote created a technical visit and creating a direct
work order created a service-execution appointment.

The corrected model keeps a specific appointment immutable as a historical
calendar commitment. A later commercial or operational stage creates a new
record when it needs another commitment.

## Decisions

- `AppointmentType` keeps its persisted numeric values while using the product
  names `Visit` (`0`), `Service` (`1`), and `Other` (`2`).
- A `Service` appointment requires a work order. `Visit` and `Other` can be
  created and completed independently.
- The existing public trace remains `ServiceRequest -> AppointmentRequest ->
  Appointment`; no duplicate service-request foreign key is added to
  `Appointment`.
- `Quote.SourceAppointmentId` records a quote produced after a completed
  `Visit` or `Other` appointment.
- `WorkOrder.SourceAppointmentId` records a direct work order produced after a
  completed `Visit` or `Other` appointment. A quote-created work order keeps
  its existing `SourceQuoteId` and reaches the prior appointment through the
  quote.
- Neither a quote nor a direct work order changes the source appointment type
  or lifecycle. A service execution is represented by a newly created
  `Service` appointment and is always optional.
- A direct quote from a service request does not create an appointment. The
  scheduling preference is closed for scheduling while the commercial request
  continues through its quote state.

## Implementation Tasks

1. Add characterization tests for appointment conversion, manual types,
   optional scheduling, and preserved history.
2. Evolve domain entities, commands, configurations, and repositories for the
   two source-appointment links and the renamed appointment types.
3. Separate service-request resolution into `PrepareQuote` (no appointment)
   and `ScheduleVisit` (one external `Visit` appointment).
4. Allow a completed `Visit` or `Other` to open a prefilled quote or work-order
   flow. Validate its completed state and keep any optional new appointment as
   a distinct `Service` record.
5. Require the type in manual scheduling UI and use `Service` for the optional
   appointment created along with a new work order.
6. Add the PostgreSQL migration only after the model and configuration match,
   then verify all migrations and test flows.

## Verification Matrix

- Service request -> quote -> approval -> work order, with no appointment.
- Service request -> visit -> completed -> quote -> approved work order ->
  separate service appointment.
- Completed visit/other -> direct work order, with and without a separate
  service appointment.
- Manual `Service`, `Visit`, and `Other` appointments.
- Rejection of a service appointment without a work order and a conversion from
  a non-completed appointment.
- Availability collision behavior remains governed by the shared scheduling
  engine.
