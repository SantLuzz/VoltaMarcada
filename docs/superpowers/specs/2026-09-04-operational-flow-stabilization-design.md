# Operational Flow Stabilization Design

## Goal

Correct the Work Order, scheduling, quote, dashboard, and public-request flows so that operational actions retain local UI state, respect professional permissions, and present consistent financial and status information.

## Scope

This specification covers the reported defects in four connected areas:

1. Work order editing, pricing, quote conversion, and listing.
2. Public appointment requests, commercial resolution, and scheduling.
3. Dashboard appointment actions and status presentation.
4. Brazilian monetary formatting and public-request submission state.

It does not introduce a new work-order numbering system. The existing `Guid` remains the sole persisted identifier. The work-order grid will display the first eight uppercase hexadecimal characters as a short code for recognition only.

## Global Constraints

- Keep source code and technical identifiers in English; Brazilian Portuguese is permitted only for user-facing UI text.
- Preserve company isolation for every query and mutation.
- Do not trust client-provided company identifiers or access scopes.
- Keep the existing .NET, Blazor, EF Core, and PostgreSQL stack; add no dependencies.
- All new and changed behavior must receive deterministic xUnit coverage.
- Use `IBrazilianDisplayFormatter` for Web UI monetary output.
- Do not reload the browser document to synchronize interactive Blazor state.

## Work Orders

### Services available to a professional

`WorkOrderRegistration` currently loads every active company service even when the authenticated user is a professional. It will obtain the current professional profile through `GetCurrentEmployeeProfessionalUseCase` and, for a non-owner, retain only active services whose identifiers are present in that profile's `ServiceIds`. Owners continue to see every active company service.

The existing application use cases remain authoritative: create and update operations will continue validating that an assigned professional is active and qualified for the selected service.

### Local registration state and photos

The current photo JavaScript stages a browser file and calls `window.location.assign(window.location.href)` after the endpoint succeeds. This causes a full navigation and reconstructs the work-order component tree, losing tab and unsaved execution state.

The photo module will instead notify the Blazor execution component when an upload succeeds. `WorkOrderExecutionPanel` will refresh only the work-order detail via an explicit callback after that notification. The parent page will preserve `activePanelIndex`; it will not change the route or recreate the panel for a tab selection. Reload methods will be separated into:

- initial/detail hydration, which is allowed to initialize form state;
- post-operation detail refresh, which updates the immutable `detail` snapshot without resetting active tab, transient dialogs, or execution state unnecessarily;
- option-list refreshes, limited to customer/equipment/professional changes.

The legacy, unused Blazor `UploadAsync`, `ReadPhotoAsync`, and `StagePhotoAsync` paths will be removed or completed so only one upload ownership path exists. The chosen path will retain staged browser files for recovery and will never use a document navigation as its success signal.

### Quick creation affordances

The `Novo cliente` and `Novo equipamento` controls beside their selects will become accessible icon buttons. They will use the add icon, retain an `AriaLabel` and tooltip, and preserve the existing dialog behavior.

### Discount and quote-derived work orders

`WorkOrder` already stores `DiscountAmount`, and quote approval currently creates a work order from the quote items and discount. The UI and work-order command contracts do not expose that value.

The create and update commands, validation, detail DTO, and pending-detail domain behavior will include a non-negative discount. `WorkOrderItemsEditor` will receive the discount as a bound value and show subtotal, discount, and final total using the existing Brazilian formatter.

For quote approval (internal and public), the domain creation path will continue copying the customer, equipment, primary service, all quote items with their item types, discount, and source-quote relationship in one transaction. Regression tests will assert that the resulting work order has the same item definitions, discount, and final total as the approved quote.

Direct editing of a quote-derived work order will preserve its stored discount. Additional work-order items remain separately editable according to existing capabilities, while final total is always calculated by the entity from persisted items minus discount.

### Work-order grid

The list DTO and repository projection already carry `CreatedAtUtc`. The grid will add:

- `Código da OS`: `Id.ToString("N")[..8].ToUpperInvariant()`;
- `Abertura`: the creation timestamp formatted in the current display timezone.

No schema migration is required for this change.

## Scheduling and Commercial Requests

### Root cause and repository behavior

Public appointment requests are intentionally created without a professional. `ServiceRequestRepository.GetOpenForCompanyAsync` currently performs inner joins from the appointment request to professional/company user/application user. This removes the very public requests it must return, which makes the UI show the incorrect "A solicitação comercial não está mais pendente" message.

The repository projection will use a left join for optional professional membership and return an empty professional name when none is assigned. It will continue filtering every joined business entity by company.

### Available professional selection

Every public request creates a pending `ServiceRequest`, which makes `RequiresCommercialResolution` true and hides the existing available-professional selector. The scheduling card will prioritize the availability result:

- If one or more professionals are available at the requested time, show the eligible-professional select and `Confirmar horário`, regardless of the pending commercial context.
- If none are available but alternatives exist, offer chronological alternatives.
- The commercial decision action remains available where a quote or a manually described technical visit is the appropriate next step, and it must load the associated request through the corrected repository.

The server-side acceptance use case remains responsible for final availability validation and tenant/role authorization immediately before creating an appointment.

### Agenda layout

The action controls in the professional agenda will use a responsive grid of two equal columns. Controls that need the full width, such as a professional selector, will explicitly span both columns. This avoids clipped actions without turning the entire action group into a single vertical list.

## Dashboard Appointment Experience

`AppointmentDetailsDialog` will become an action dialog driven by `AppointmentStatus` and existing access capabilities.

| Status | Actions |
| --- | --- |
| Scheduled | Start attendance; view in schedule; cancel appointment; open linked work order when present |
| In progress | Complete attendance; view in schedule; cancel appointment when authorized; open linked work order when present |
| Completed | Open linked work order and only historical/navigation actions |
| Cancelled | Only historical/navigation actions |

The dialog will expose callbacks to its hosts. Start, complete, and cancel operations will use the established scheduling use cases, then refresh the originating dashboard or schedule list. `Ver na agenda` will navigate with the appointment identifier in the query string. `Schedule` will read that value, select/open the matching appointment, and use the normal date range loading path to make it visible.

Dashboard appointment cards will use a status-specific left border:

- cancelled: red;
- completed: green;
- scheduled or pending: default primary color;
- in progress: informational color.

Their label will be simplified to `Pendente`, `Iniciou`, `Cancelado`, or `Concluído` so the text and color agree.

## Quotes and Currency

Quote registration and quote-list screens currently call `decimal.ToString("C")`, which uses the server's default culture and can emit an incorrect currency glyph. They will inject and use `IBrazilianDisplayFormatter` for every displayed quote amount. The same shared formatter will be used by the public quote page where applicable; PDF generation remains on its already-correct `pt-BR` formatter.

The existing `BrazilianCurrencyInput` remains the sole input formatter for quote and work-order monetary editing.

## Public Request Submission

The global `form-submission-loading.js` listens to every valid HTML form submission and permanently disables submit controls. It is not aware of Blazor's asynchronous operation outcome. It will be scoped to explicitly marked traditional form posts only, leaving interactive Blazor forms under their component state.

The public company and contextual maintenance pages will keep an explicit single-submission guard and will reset `isSubmitting` in `finally` on success, failure, and exception. Their success state will replace the form with a success alert stating that the request was sent and the company will analyze and confirm the time. On failure, controls become enabled again and the error remains visible.

## Error Handling

Business failures continue to return `OperationResult` and display existing localized error mappings. The scheduling and public-request UI must not claim success until the use case succeeds. Any post-action refresh failure will show a non-sensitive warning and leave the action's server outcome intact rather than retrying the mutation.

## Test Strategy

Add or extend xUnit integration/component-source tests for:

- service filtering for a logged-in professional;
- discount creation/update and quote-to-work-order item/discount/total copying;
- short-code and local-time opening-date list projections;
- service-request retrieval when the appointment request has no professional;
- selector visibility/acceptance path for an available professional;
- action availability for each appointment status and schedule focus query;
- status-card class/label mapping and responsive two-column action CSS;
- Brazilian amount rendering in quote registration, listing, and public quote;
- no document navigation in the photo upload JavaScript and callback-based refresh;
- public submission controls leaving loading state and rendering success after a successful result.

## Out of Scope

- A persisted or sequential work-order number.
- Changes to appointment availability rules, time-zone rules, or public slot generation.
- Payment, inventory, accounting, or new messaging integrations.
- A broad replacement of the scheduling or commercial-request architecture.
