# Phase 5 Maintenance Opportunities Design

## Goal

Deliver the strategic recurrence cycle for VoltaMarcada: when a completed work order recommends future maintenance, the system creates a company-scoped commercial opportunity and surfaces it in an actionable maintenance center.

## Scope

The phase includes:

- Optional next-maintenance recommendation while completing an in-progress work order.
- The `MaintenanceOpportunity` aggregate, persistence, tenant-scoped queries, and state transitions.
- A maintenance center with operational urgency counts and actions.
- Contact tracking, WhatsApp share-link generation, internal scheduling handoff, dismissal, and completion.
- Context handoff from a maintenance opportunity to the existing internal schedule flow.
- A dashboard summary and authenticated navigation entry.
- A global unexpected-exception boundary that always returns a safe internal-error response to the frontend.

The phase does not include background jobs, automatic status changes, official WhatsApp Business API integration, public booking, contextual public tokens, automatic new work-order creation, or automatic work-order association for maintenance returns.

## Existing Foundations

`WorkOrder` already represents the completed service record and has required company, customer, equipment, and service references. `CompleteWorkOrderUseCase` currently performs the final execution validation and completes an in-progress work order.

The scheduling module creates internal appointments with validated professional availability and supports optional customer, equipment, service, and work-order context. The authenticated agenda is therefore reused instead of creating a second scheduling surface.

The Web project already has an `UnexpectedExceptionHandler` registered through `AddExceptionHandler`. The pipeline currently enables `UseExceptionHandler` only outside development; this phase makes that global boundary active in every environment.

## Domain Model

### MaintenanceOpportunityStatus

```csharp
public enum MaintenanceOpportunityStatus
{
    Pending,
    Contacted,
    BookingRequested,
    Booked,
    Completed,
    Dismissed
}
```

`DueSoon` is deliberately not persisted. It is an urgency classification calculated at query time from `DueDate` and the current business date.

### MaintenanceOpportunity

`MaintenanceOpportunity` is a company-scoped aggregate root. It has the following mandatory business references and fields:

- `Id` and `CompanyId`.
- `CustomerId`, `EquipmentId`, and `PreviousWorkOrderId`.
- `RecommendedServiceId`.
- `DueDate` as `DateOnly`.
- `Notes` containing the recommendation context.
- `Status`, `CreatedAtUtc`, and `UpdatedAtUtc`.

It additionally records nullable `ContactedAtUtc`, `BookingRequestedAtUtc`, `BookedAtUtc`, `CompletedAtUtc`, and `DismissedAtUtc` values. All timestamp fields are stored as UTC `DateTimeOffset`; due dates stay as pure dates.

The aggregate exposes behavior methods rather than public setters:

- `MarkAsContacted(DateTimeOffset now)`.
- `MarkAsBookingRequested(DateTimeOffset now)`.
- `MarkAsBooked(DateTimeOffset now)`.
- `Complete(DateTimeOffset now)`.
- `Dismiss(DateTimeOffset now)`.

It rejects invalid identifiers, a missing due date or notes, and invalid state transitions. A dismissed opportunity is terminal. A booked opportunity may be completed. `MarkAsContacted` is available from `Pending` and remains idempotent for `Contacted`; `MarkAsBookingRequested` follows contact; `MarkAsBooked` follows booking request. Application use cases establish expected preconditions and return explicit business failures before invoking a domain method.

## Work-Order Completion Recommendation

The completion UI adds an optional expandable section named “Recomendar próxima manutenção?”. When disabled, existing completion behavior is unchanged.

When enabled, all recommendation inputs are mandatory:

- One of 30, 60, 90, or 180 days; a custom positive interval; or an explicit due date.
- Recommended service.
- Notes.

The recommended service starts as the current work-order service but remains selectable from active company services. The due date is calculated from the work order completion business date plus the selected interval, or is the explicit date. The application validates that the selected service belongs to the current company and that the supplied data is complete and valid.

The work order completion and opportunity creation execute in one database transaction. If either operation cannot persist, neither is committed. Completion keeps all existing authorization, status, checklist, photo, and open-appointment validation.

## Persistence

`AppDbContext` gains `DbSet<MaintenanceOpportunity>`. A dedicated Fluent configuration maps the aggregate with required properties, company-scoped foreign keys to customer, equipment, work order, and service, and restrictive deletion behavior that preserves commercial history.

The configuration adds the query-focused indexes:

- `CompanyId`, `Status`, and `DueDate` for operational lists and counts.
- `CompanyId` and `CustomerId` for customer-context lookups.

Infrastructure adds an `IMaintenanceOpportunityRepository` implementation. Its reads apply both the entity identifier and `CompanyId` where applicable; no authenticated operation takes a company identifier from the browser. A descriptive migration, `AddMaintenanceOpportunities`, contains the schema changes and model snapshot update.

## Application Use Cases

Application owns small, focused use cases and contracts:

- `GetMaintenanceOpportunitiesUseCase`.
- `GetMaintenanceOpportunitySummaryUseCase`.
- `MarkMaintenanceOpportunityAsContactedUseCase`.
- `MarkMaintenanceOpportunityAsBookingRequestedUseCase`.
- `MarkMaintenanceOpportunityAsBookedUseCase`.
- `CompleteMaintenanceOpportunityUseCase`.
- `DismissMaintenanceOpportunityUseCase`.

Opportunity creation is an internal concern of `CompleteWorkOrderUseCase`, not a separately browser-invoked create flow. A focused factory or repository method may be used to preserve this boundary.

All use cases resolve the authenticated current company. Expected failures return explicit `OperationResult` values with feature-owned English error codes such as `MaintenanceOpportunity.NotFound`, `MaintenanceOpportunity.InvalidInput`, and `MaintenanceOpportunity.InvalidStatus`. The Web layer maps these codes to safe Brazilian Portuguese text.

## Operational Urgency

Urgency is calculated from the company’s local current date. The calculation never writes a new status or requires a background process:

- Overdue: `DueDate` is before today and the opportunity is open.
- Next seven days: `DueDate` is from today through today plus seven days and the opportunity is open.
- Next thirty days: `DueDate` is from today through today plus thirty days and the opportunity is open.
- Future: any later open opportunity.

Terminal `Completed` and `Dismissed` records are excluded from actionable counts. Summary results contain overdue, next-seven-days, and next-thirty-days counts. The list returns the related customer, equipment, prior work-order completion date, recommended service, current status, and calculated urgency required by the Web layer.

## State and Actions

The commercial flow is:

```text
Pending -> Contacted -> BookingRequested -> Booked -> Completed
                         \
                          -> Dismissed
```

`Dismissed` is terminal. The application returns a conflict result when an action is not permitted in the current persisted state.

The maintenance center supports these actions:

- **WhatsApp:** mark the opportunity as contacted and open a pre-filled WhatsApp link. The message includes safe, relevant customer, equipment, service, and recommendation context. URL/message construction is isolated in one Web or application-facing helper, not duplicated across components.
- **Mark as contacted:** covers phone, in-person, or external messaging contact.
- **Schedule:** mark the opportunity as `BookingRequested` and navigate to the internal schedule with only the opportunity ID in the route/query. The schedule loads all customer, equipment, and service context server-side after verifying company ownership.
- **Book:** successful appointment creation from that contextual schedule marks the opportunity as `Booked` in the same transaction as the appointment persistence. A failed appointment creation leaves the opportunity unbooked.
- **Complete:** available only from `Booked`; records the completion timestamp.
- **Dismiss:** asks for confirmation, then marks the opportunity dismissed and records the timestamp.

No action exposes storage keys, non-tenant context, raw exception information, or a public token.

## Web Experience

### Work-Order Completion

`WorkOrderExecutionPanel` uses MudBlazor controls to show recommendation fields only after the user chooses to recommend a future maintenance. The completion action shows field-level validation and remains disabled while the recommendation is incomplete or an operation is in progress. It preserves existing autosave and completion behavior when no recommendation is selected.

### Maintenance Center

The authenticated `/manutencoes` page follows the existing two-screen management/list visual conventions where appropriate. It contains a `PageHeader`, three urgency summary cards, filters for status and period, loading and empty states, and a responsive list or grid. Each row/card displays customer, equipment, previous maintenance date, recommended date, status, and only the actions valid for the current state.

The page uses MudBlazor controls and the established VoltaMarcada theme, spacing, feedback, accessibility, and responsive rules. Dismissal uses a MudBlazor confirmation dialog. Action submissions display loading state, prevent duplicates, show centralized Portuguese errors, and refresh the affected summary/list data after success.

### Schedule Handoff

Selecting schedule navigates to the existing `/agenda` route with the opportunity identifier. The page obtains all prefill values by calling a tenant-filtered application query. It does not trust client-provided customer, equipment, or service IDs. The normal agenda form remains responsible for professional selection and slot calculation. Appointment creation receives the validated opportunity context only after it is loaded server-side, then updates the opportunity to `Booked` atomically.

### Dashboard and Navigation

The dashboard contains a compact maintenance summary using the same overdue, next-seven-days, and next-thirty-days values, with a direct link to `/manutencoes`. The authenticated main navigation gains a “Manutenções” entry.

## Error Boundary

Unexpected exceptions remain exceptional. `UnexpectedExceptionHandler` logs the exception with a trace identifier and returns HTTP 500 `ProblemDetails` with only a generic Portuguese title and `traceId`. It does not expose exception messages, stack traces, database data, or sensitive request values.

`Program.cs` calls `UseExceptionHandler()` in all environments, before request-processing middleware that can throw. Development diagnostics remain available through server logs and the trace identifier; the frontend receives the same safe response shape in development and production. `OperationCanceledException` is never converted to an internal-error response and continues to propagate.

Expected validation, not-found, authorization, and state failures remain `OperationResult` values, mapped by the established Web helpers rather than being handled by the unexpected-exception boundary.

## Authorization and Tenant Isolation

Every maintenance use case resolves `ICurrentCompany` or the existing authenticated access scope and queries with `CompanyId`. Related customer, equipment, prior work order, recommended service, appointment context, and opportunity must all belong to the same company. Route and query identifiers never select the tenant.

Employee access follows the existing work-order and scheduling authorization model. If the current product rules require tighter ownership visibility for employees, the use case must apply it in the database query; the UI is not treated as an authorization boundary.

## Verification

No automated tests, builds, application launches, or migration execution are run unless the user explicitly requests them, in accordance with repository instructions. Static verification covers:

- Domain aggregate and state transition invariants.
- Application validation and tenant-scoped queries.
- Dependency-injection registrations and endpoint contracts.
- Entity configuration, migration, and model-snapshot consistency.
- Razor routes, component references, loading/error/confirmation states, and responsive MudBlazor usage.
- Safe exception response handling and the absence of raw exception text in responses.

