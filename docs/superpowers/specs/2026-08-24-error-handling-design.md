# Error Handling Design

## Objective

VoltaMarcada will stop using exceptions as the normal representation of validation failures, missing tenant-owned records, authorization failures, state conflicts, scheduling conflicts, and other expected business outcomes. These conditions will be returned explicitly by application use cases and presented consistently by HTTP endpoints and Blazor components.

Exceptions remain appropriate for programming errors, corrupted or impossible domain state, unavailable infrastructure, and other unexpected failures. They are logged at an application boundary and are never exposed directly to users.

## Result Model

Application use cases that can fail for expected reasons will return `OperationResult` or `OperationResult<TValue>`. A result contains either a success value or one or more `OperationError` values. An error contains:

- a stable English `Code`, scoped by feature, such as `Customer.DuplicateDocument` or `Scheduling.AppointmentConflict`;
- an `OperationErrorType` describing the broad transport behavior: `Validation`, `NotFound`, `Forbidden`, or `Conflict`;
- an optional field name for input validation;
- an internal English description suitable for diagnostics, but not direct display to the user.

The common result types belong in `VoltaMarcada.Application`. They do not depend on ASP.NET Core, MudBlazor, EF Core, or localization concerns. Feature-specific factory methods or static error definitions keep use cases readable and prevent string literals from spreading through the application.

The design intentionally avoids a global enum containing every business failure. Stable feature error codes provide a shared contract while each feature continues to own its vocabulary.

## Domain Boundary

The Domain layer continues to protect entity invariants. `DomainException` is reserved for conditions that application code should have made impossible before invoking an entity, such as constructing an entity with an empty tenant identifier or calling a state transition after its preconditions were already checked.

Expected failures caused by user input or current persisted state are checked in the Application layer before domain mutation and returned as operation errors. This includes invalid commands, records not found within the current company, invalid status transitions, duplicate data, professional unavailability, and scheduling collisions.

Domain APIs will not be mechanically converted to `Try*` methods or made dependent on Application result types. During migration, each use case must establish the required preconditions before calling a throwing domain method. Remaining `DomainException` sites will be reviewed and reclassified: predictable user-driven conditions move to explicit application results; genuine invariant guards remain exceptions.

## Application Flow

Each use case follows the same flow:

1. Validate command shape and field values.
2. Resolve the current company from the authenticated context.
3. Load tenant-owned records using both record ID and `CompanyId`.
4. Return an explicit result for expected validation, ownership, not-found, state, or conflict outcomes.
5. Invoke domain behavior only after its preconditions are satisfied.
6. Persist changes and return success.

Expected persistence conflicts, such as duplicate documents or concurrent appointment collisions, are translated by Infrastructure into a narrowly defined persistence outcome. Application converts that outcome into the corresponding operation error. Other storage failures remain exceptions.

Cancellation is never converted into an operation error. `OperationCanceledException` continues to propagate so request and component cancellation semantics remain correct.

## Web and HTTP Mapping

The Web project will contain one result-to-HTTP mapper. It maps broad error types consistently:

- `Validation` to HTTP 400;
- `NotFound` to HTTP 404;
- `Forbidden` to HTTP 403;
- `Conflict` to HTTP 409.

Error responses use ASP.NET Core `ProblemDetails` with a stable error code and optional field errors in extensions. Endpoint files retain responsibility only for their success response, such as `Created`, `Ok`, or `NoContent`; repeated error switches and anonymous `{ error = ... }` payloads are removed.

Brazilian Portuguese user messages belong to the Web layer. A centralized feature-aware message catalog maps stable application error codes to safe user-facing text. Unknown codes fall back to a generic message and are not exposed verbatim.

An `IExceptionHandler` handles unexpected endpoint failures, logs structured context, and returns generic HTTP 500 `ProblemDetails` containing a trace identifier. It does not translate `DomainException` into a business response because an uncaught domain exception indicates a missed application precondition or invalid internal state.

## Blazor Behavior

Blazor components consume operation results instead of using exceptions to detect expected outcomes. Presentation follows the existing UI rules:

- field errors appear beside the corresponding MudBlazor input;
- form or section errors appear contextually;
- transient operation errors use a snackbar;
- retryable load failures include a retry action when practical;
- raw exception messages and internal error codes are never displayed.

Components may still catch unexpected exceptions at an asynchronous UI boundary to keep the circuit usable, log the failure, and show a generic fallback. These catches are not used for normal business branching. Repeated component exception-handling code may be extracted into a small Web-only helper after at least two equivalent call sites are migrated; the change will not introduce a new framework or cross-layer abstraction.

## Migration Strategy

The migration is performed feature by feature to keep changes reviewable and preserve the existing behavior:

1. Add the common result, error, HTTP mapping, message catalog, and unexpected exception handler.
2. Migrate scheduling and work orders, which already use result enums but have the richest business outcomes.
3. Migrate customers, equipment, services, professionals, onboarding, and employee invitations.
4. Review remaining `throw`, `catch (Exception)`, feature result enums, anonymous error responses, and endpoint error switches.
5. Remove obsolete feature result types only after every caller has moved to the common contract.

The migration does not change tenant-isolation rules, authorization policies, successful HTTP response shapes, or business behavior. Existing uncommitted scheduling changes are preserved and treated as the current source of truth.

## Testing and Verification

Automated tests are not introduced because the repository instructions say they are added only when explicitly requested. Static verification will cover:

- every migrated expected failure returns an operation error rather than throwing;
- domain mutation is invoked only after application preconditions are checked;
- tenant-owned lookups remain filtered by `CompanyId`;
- HTTP mappings use 400, 403, 404, and 409 consistently;
- unexpected failures remain HTTP 500 and include a trace identifier;
- Portuguese UI messages do not originate from Domain or Application;
- cancellation is not swallowed;
- no raw exception message is rendered in Blazor or returned by an endpoint.

Per repository policy, no build, test run, application launch, or migration execution will be performed unless the user explicitly requests it.
