# Service Requests and Quotes Design

> Status: Proposed on 2026-08-26.

## Goal

Create a coherent commercial flow from a public service request through evaluation, quote delivery, customer decision, and work order creation. The flow must not prevent internal users from creating a work order directly for an existing customer.

The public flow must never end as an unclassified calendar event. A confirmed appointment has a purpose that makes its operational outcome clear to both the company and the customer.

## Scope

This phase includes:

- Public service requests with a requested appointment and problem description.
- Internal request triage and confirmation.
- Appointment types and customer-facing appointment descriptions.
- Quotes with service, parts, materials, travel, custom items, discount, notes, validity, and total.
- Secure public quote links at `/q/{token}`.
- Public quote viewing, approval, rejection, and WhatsApp contact action.
- WhatsApp share links with a prepared quote message and public link.
- Automatic, idempotent work-order creation when a quote is approved.
- Copying approved quote line items and discount into the generated work order.

The phase excludes PDF generation. A PDF can be introduced later as a complement to the public quote link without changing the quote lifecycle.

## Existing Foundations

The current public company page already creates an `AppointmentRequest` after a visitor chooses a service, professional, date, and time. Internal acceptance revalidates availability and creates an `Appointment`. The scheduling module already protects tenant boundaries and concurrent appointment overlap.

Work orders currently support a primary service, financial items, and a direct internal creation flow. That direct flow remains valid and unchanged as a supported path.

The existing WhatsApp link builder and secure-token patterns used by public booking provide the appropriate foundations for quote sharing.

## Domain Model

### Service Request

`ServiceRequest` is the commercial origin of a public request. It records the company, customer, optional equipment, originating `AppointmentRequest`, submitted problem description, optional resulting quote, optional resulting work order, timestamps, and its operational state.

Public submission creates the service request together with the existing appointment request. It does not create an appointment, quote, or work order immediately.

The request lifecycle distinguishes at least pending internal review, quote preparation or delivery, direct work-order conversion, completion, cancellation, and closure. The exact state transitions stay inside behavior methods so a request cannot be linked to unrelated tenant records or converted twice.

An internally created quote may omit a service request. An internally created work order may omit both a quote and a service request.

### Appointment Type

`Appointment` gains an `AppointmentType` enum:

- `TechnicalVisit`: diagnostic, inspection, survey, or evaluation visit that may lead to a quote and intentionally may not have a work order yet.
- `ServiceExecution`: execution commitment that must be associated with a work order.
- `Other`: an internal commitment that does not fit either operational purpose.

Every appointment has a type and an optional bounded description. The description is suitable for communicating the purpose to the customer, such as "Technical visit for evaluation and quote preparation." It is not a substitute for internal execution notes.

During public-request confirmation, the internal user chooses the path. The quote path creates a `TechnicalVisit` and starts a quote in `Draft`. The direct-work-order path creates a `ServiceExecution` appointment and a work order in one consistent operation. Existing appointments migrate to `Other` to avoid asserting a purpose that was never captured.

### Quote

`Quote` is a company-scoped aggregate. It includes its customer, optional equipment, optional service request, primary service, status, item snapshot, discount amount, notes, validity date, public-link state, sent/decision timestamps, and optional generated-work-order identifier.

The primary service is required because every approved quote must be convertible into an operational work order. Quote items contain a description, quantity, unit price, display order, and a type:

- `Service`
- `Part`
- `Material`
- `Travel`
- `Custom`

The quote total is the sum of its line items less its explicit non-negative discount amount. The public page shows the service and all item values, the discount when present, notes, validity, and final total.

Quote states are exactly:

```text
Draft -> Sent -> Approved
              -> Rejected
              -> Expired
```

Only a `Sent` and still-valid quote can be approved or rejected through the public link. Approval is terminal. An expired, rejected, or approved quote cannot be edited or sent again. The application may mark a sent quote expired when it is read or acted upon after its validity date; all checks use `TimeProvider`.

## Work-Order Generation

Approving a quote creates exactly one work order in the same transaction that changes the quote to `Approved`. The quote stores the generated work-order identifier and the work order stores its source quote reference. This creates a database-enforced one-to-one relationship and makes the action idempotent for retries and repeated public submissions.

The generated work order receives the quote's customer, equipment when present, primary service, reported problem or request description, and the approved financial snapshot. The primary service becomes the work order's required service item. Every remaining service, part, material, travel, and custom item is copied as a detailed work-order financial item. The quote discount is copied as a work-order discount.

The existing work-order financial-item model will be expanded so it can represent the approved item categories and discount instead of collapsing them into an undifferentiated value. Prices, quantities, descriptions, and discount are copied; later edits to service catalog prices or the quote cannot alter the created work order.

Internal approval uses the same quote-approval use case as the public action, after tenant authorization. Therefore, every approved quote, regardless of origin, creates a work order automatically.

## Public and Internal Flow

```text
Public request with service, requested slot, and problem description
    -> ServiceRequest + AppointmentRequest
    -> internal review and availability revalidation
    -> choose TechnicalVisit + Draft quote
       or ServiceExecution + direct WorkOrder
    -> prepare and send quote
    -> customer opens /q/{token}
    -> customer approves
    -> Quote Approved + WorkOrder created atomically
```

The internal request queue is the place to review public requests. When confirming one, the user must choose between preparing a quote and creating a direct work order. The selection controls the appointment type and its mandatory association. The existing agenda continues to show the confirmed appointment, now including its type and description.

The quote area supports creating internal quotes directly, editing draft quotes, adding categorized line items, setting a discount, notes and validity, sending the public link, and viewing its decision state. The work-order area shows the source quote when a work order was generated from one.

## Public Quote Link and WhatsApp

Sending a quote generates a 32-byte cryptographically random token. Only its SHA-256 hash, UTC expiry state, and optional revocation timestamp are persisted. The raw token is neither logged nor stored. Re-sending with a replacement token revokes the old token.

`/q/{token}` validates the token before loading quote context. It displays only the company identity, customer-safe quote details, validity, total, and actions needed to approve, reject, or contact the company. It never exposes database identifiers, internal notes, request history, or unrelated customer data.

The WhatsApp action uses the existing application abstraction to produce a pre-filled message containing the quote link. The message construction remains outside Razor components. Sending a link moves a draft quote to `Sent`; message-delivery or browser-opening failures never roll back the quote state.

## Security, Validation, and Reliability

All authenticated request, quote, and work-order queries filter by the current company. Browser-provided company, customer, quote, work-order, equipment, and request identifiers are never trusted for tenant selection.

Public quote commands validate the token, quote state, expiry, and input server-side. They return safe Brazilian Portuguese messages for invalid or expired links, invalid transitions, and unavailable public actions. They must not disclose whether a token existed or reveal internal exception information.

Availability is revalidated immediately before confirming the appointment. Quote approval and automatic work-order creation run in a transaction, with the one-to-one quote/work-order constraint providing a final guard against duplicate work orders.

## Persistence and Application Boundaries

The Domain project owns the `ServiceRequest`, `Quote`, `QuoteItem`, appointment-type behavior, quote state transitions, totals, and quote-to-work-order invariant. It stays independent of EF Core and presentation concerns.

The Application project adds explicit use cases for public-request triage, quote creation and editing, quote sending, internal approval or rejection, public quote loading, public quote approval or rejection, and generated-work-order retrieval. It owns token orchestration, tenant authorization, and operation-result errors.

Infrastructure adds EF Core configurations, tenant-filtered repository methods, indexes, secure-token persistence, a descriptive migration, and one-to-one constraints. The Web project adds internal request and quote pages, public `/q/{token}` UI, endpoints, navigation, and WhatsApp share actions without embedding business rules in Razor components.

## Verification

In accordance with repository instructions, implementation validation will use static inspection unless a build, test run, or application launch is explicitly requested. The implementation review must verify:

- protected domain transitions and financial calculations;
- tenant-filtered reads and writes;
- secure hashing, expiry, revocation, and non-disclosure of public tokens;
- public quote approval idempotency and the quote/work-order uniqueness constraint;
- transactional copying of every approved financial line and discount;
- appointment-type invariants and public-request confirmation choices;
- use-case registrations, endpoint contracts, Razor loading/error states, and localized safe messages;
- migration and model-snapshot consistency.
