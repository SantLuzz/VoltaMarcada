# Appointment, Request, Quote, and Work Order Flow Remodeling

> Status: Approved on 2026-08-28.

## Goal

Prepare the current quote implementation for Phase 8 by restoring clear and durable boundaries between a service request, an appointment, a quote, and a work order. An appointment records one concrete calendar commitment. It must never be repurposed to represent a later operational stage.

## Existing Foundations

- `Appointment` already owns its schedule, status transitions, reschedule history, and company-scoped conflict protection.
- `CreateWorkOrderUseCase` already supports an optional appointment and revalidates availability before saving it.
- Quote approval already creates exactly one work order in a transaction and retains quote-to-work-order idempotency.
- Public booking creates `AppointmentRequest` plus `ServiceRequest`, so the customer intention is already persisted independently from a confirmed appointment.

## Problems in the Current Flow

`ResolveServiceRequestUseCase` currently creates an appointment in both outcomes. Preparing a quote simultaneously creates a technical visit; creating a direct work order simultaneously creates a service-execution appointment. This incorrectly turns commercial triage into a calendar commitment.

The current adjustment flow can associate an arbitrary scheduled appointment with a work order. A completed visit could therefore be reused as service execution in a later step. The manual appointment API also does not expose the existing type and description contract.

## Domain Model

### Appointment

`AppointmentType` will contain the persisted values `Visit`, `Service`, and `Other`. The numeric values of the existing `TechnicalVisit`, `ServiceExecution`, and `Other` values will be retained so existing records preserve their meaning.

- `Service` requires a work order and represents execution of that work order.
- `Visit` and `Other` may exist without a quote or work order, and have the existing `Scheduled -> InProgress -> Completed` lifecycle.
- Type is immutable after creation.
- A `Visit` or `Other` can never gain a work-order association through the adjustment flow. A new `Service` appointment is required for a later execution commitment.
- A visit created while resolving a public service request has an optional `ServiceRequestId`. This gives the request an ordered, queryable appointment history without requiring an appointment to be a work-order detail.

### Service Request

`ServiceRequest` remains the commercial record created from a public `AppointmentRequest`. Its state will distinguish initial review, scheduled visit, completed visit, quote preparation/delivery, conversion to a work order, closure, and cancellation.

The redundant `ServiceRequest.QuoteId` will be removed. `Quote.ServiceRequestId` is the authoritative relationship. `ServiceRequest.WorkOrderId` remains the summary link for direct and quote-based conversion.

### Quote and Work Order Origin

`Quote` and `WorkOrder` will each gain an optional `SourceAppointmentId`.

- A quote created from a completed `Visit` or `Other` records that appointment as its source. If the visit itself came from a public request, the quote also retains the existing `ServiceRequestId`.
- A work order created directly from a completed `Visit` or `Other` records that appointment as its source.
- A work order generated from a quote continues to use `SourceQuoteId`; the quote source preserves the preceding visit where applicable.
- Direct/manual quotes and work orders remain valid without a source appointment or request.

These links are one-directional provenance fields. They avoid a separate case-flow aggregate and avoid circular foreign keys while allowing the request, visit, quote, work order, and any later service appointment to be reconstructed.

## Operational Flows

### Request to Quote

```text
ServiceRequest -> Quote -> customer approval -> WorkOrder
```

Creating the quote does not create or confirm an appointment. Quote approval creates only the work order. If execution is scheduled, the user creates a new `Service` appointment linked to that work order.

### Request to Visit

```text
ServiceRequest -> Visit -> completed Visit -> Quote or WorkOrder or close
```

Resolving the request as a visit validates real availability, creates a `Visit` appointment, and records the appointment request acceptance. Completing that appointment makes the source available for explicit quote creation, direct work-order creation, or closing the service request.

### Visit or Other to Quote or Work Order

```text
completed Visit/Other -> Quote -> WorkOrder -> optional new Service
completed Visit/Other -> WorkOrder -> optional new Service
```

The originating appointment remains completed with its original type. Creating a work order never creates an appointment unless an explicit schedule input is supplied. If supplied, it creates a second entity of type `Service` after availability is revalidated.

### Manual Scheduling

Every internally created appointment explicitly selects `Service`, `Visit`, or `Other`. `Service` requires a work order; the other types do not. All types continue to block the professional interval and are subject to the existing availability and concurrency checks.

## Application Boundaries

- `ResolveServiceRequestUseCase` will separate quote preparation, visit scheduling, and retained direct-work-order resolution rather than infer an appointment from the commercial decision.
- Quote creation will accept a completed `Visit` or `Other` source only after tenant and state validation.
- Work-order creation will accept a completed `Visit` or `Other` source and an independent optional service-appointment request.
- Appointment adjustment will preserve type semantics and reject work-order attachment for non-service appointments.
- Existing start, complete, reschedule, and conflict logic remains centralized in scheduling use cases.

## Persistence

`RemodelAppointmentDocumentFlow` will:

- add nullable, company-scoped `ServiceRequestId` to `Appointments`;
- add nullable, company-scoped `SourceAppointmentId` to `Quotes` and `WorkOrders`;
- add foreign keys and query indexes for all three provenance relationships;
- remove `ServiceRequests.QuoteId`;
- preserve appointment-type numeric values without data conversion.

## Presentation

The request queue will present separate actions to prepare a quote, schedule a visit, or create an unscheduled work order. The agenda and shared appointment editor will require a type and expose source-aware actions only when a `Visit` or `Other` is completed. Quote and work-order registration will retain and display origin context, while creation from a completed appointment pre-fills the customer, equipment, and service data.

The existing schedule, request queue, quote list/editor, work-order registration, appointment details, and appointment HTTP contract are affected. All edited UI will continue to use MudBlazor controls and localized Brazilian Portuguese copy.

## Security and Reliability

Authenticated use cases resolve company access from the current context and load all provenance records with both ID and company predicates. Source appointments must be completed and typed `Visit` or `Other`. New service appointments always revalidate availability and use the existing scheduling transaction and database conflict handling.

## Verification

The test suite will cover:

1. request -> quote -> approval -> work order;
2. request -> visit -> completed visit -> quote -> approval -> work order -> new service;
3. request -> visit -> completed visit -> direct work order;
4. visit -> work order without a new appointment;
5. visit -> work order with a new service appointment;
6. equivalent `Other` flows;
7. manual `Service`, `Visit`, and `Other` creation;
8. preserved completed appointment type and identity;
9. scheduling conflicts for every appointment type.

The repository currently has only a small integration-test project. A focused xUnit domain/application test project is justified by the explicit test request and will be added to the solution.
