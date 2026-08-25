# Services Module Design

## Goal

Provide each company with a tenant-isolated catalog of services, including price presentation, approximate duration, online-request eligibility, operational photo requirements, and item-level checklist rules.

## Scope

The module introduces an authenticated service catalog with list, create, edit, and delete operations. It does not create work orders, public booking pages, or upload/validate service-execution photos; those future flows will consume the configuration introduced here.

## Domain Model

`Service` is a company-scoped entity with server-generated `Guid` identity and the following state:

- `CompanyId`
- `Name` (required)
- `Description` (optional)
- `DurationMinutes` (required positive integer)
- `PriceType` (`ServicePriceType` enum)
- `BasePrice` (nullable monetary decimal)
- `AllowOnlineBooking`
- `RequireBeforePhotos`
- `RequireAfterPhotos`
- `IsActive` (created active; retained for the existing model requirement even though activation controls are outside this first screen)
- ordered checklist items

`ServiceChecklistItem` is a dependent entity owned by a service. It contains an item description, an `IsRequired` flag, and a zero-based display order. Checklist obligation is deliberately stored per item; there is no service-level "mandatory checklist" setting.

`ServicePriceType` lives in `VoltaMarcada.Domain.Enums` and uses these values:

- `Fixed`
- `StartingAt`
- `QuoteRequired`
- `Hidden`

Domain construction and updates enforce a nonblank name and a positive duration. Application validation enforces the cross-field pricing rule: `BasePrice` is required and nonnegative for `Fixed` and `StartingAt`, and must be absent for `QuoteRequired` and `Hidden`. Checklist item text must be nonblank after trimming.

## Application and Persistence

The application layer will follow the existing Equipment module pattern with explicit `CreateServiceUseCase`, `GetServicesUseCase`, `GetServiceForEditUseCase`, `UpdateServiceUseCase`, and `DeleteServiceUseCase`, request/response records, a result type for expected missing-record outcomes, and FluentValidation validators.

`IServiceRepository` will expose the required tenant-safe operations. Its EF Core implementation will always query by both the entity identifier and the current company identifier. It will persist service checklist items as a one-to-many relationship and preserve their submitted order.

`AppDbContext` will expose `Services` and `ServiceChecklistItems`. Fluent configurations will map the tables, required limits, decimal precision, foreign key and uniqueness/indexes appropriate to company-scoped service lookup. A descriptive EF migration will add both tables and the foreign key cascade relationship.

## Web Experience

Add a `Serviços` link to the authenticated main menu and implement these routes:

- `/servicos`: searchable management list with service name, duration, price presentation, online request status, edit, and delete actions.
- `/servicos/registro`: separate create/edit form using the established two-screen management pattern.

The registration form uses MudBlazor components and Portuguese UI copy. It groups fields into service details, price, operational rules, and checklist. Duration is entered with a numeric field labelled `Duração aproximada (min)` and only accepts whole positive minutes.

The price type is selected from the enum. When the selected type is `Fixed` or `StartingAt`, the currency value input is displayed and required. When it is `QuoteRequired` or `Hidden`, the value input is hidden and its value is cleared in the submitted model.

The operational-rules section provides independent switches for `Permitir solicitação online`, `Exigir fotos antes do atendimento`, and `Exigir fotos depois do atendimento`.

The checklist section provides a text field, an `Obrigatório` checkbox, and an `Adicionar item` button. Clicking the button adds an in-memory item only when its text is valid, resets the entry controls, and renders the item with its required marker and a remove action. Saving transfers the resulting collection to the application command.

After successful create or update the UI returns to the list and shows a success notification. Failures show server validation feedback without losing entries. The list and registration pages follow the existing loading, empty, error, responsive layout, and confirmation-dialog conventions.

## Error Handling and Security

Authenticated company-member authorization protects both service pages and service endpoints. The current company is resolved server-side; neither browser fields nor route data select the company. Requests targeting a service from another company return the same not-found/expected operation result used by the existing tenant-scoped modules.

Client-side conditional visibility improves usability only. The backend remains authoritative for all duration, pricing, and checklist validation.

## Verification

Automated tests are outside this request and are not added during MVP validation. The implementation will receive static review of its domain invariants, FluentValidation rules, tenant-scoped repository predicates, Fluent API mapping, migration structure, endpoint authorization, and UI command mapping. No build, test run, or application launch is performed unless separately requested.

## Assumptions

- `BasePrice` represents Brazilian real and is stored as a decimal; localization/formatting occurs only at the web boundary.
- Photo requirements are configuration for future work-order execution; this scope does not upload or inspect photos.
- Service activation remains enabled by default but no active/inactive management control is required unless separately requested.
