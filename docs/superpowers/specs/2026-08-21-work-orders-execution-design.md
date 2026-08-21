# Work Orders and Service Execution Design

## Goal

Deliver the Phase 3 MVP for operational work orders: a company can create a work order for a customer equipment item, assign and execute it, record checklist progress and photographic evidence, and complete it only when the configured execution rules are satisfied.

## Scope

The implementation includes:

- Work order creation, listing, details, assignment, start, completion, and cancellation.
- Financial work order items.
- A per-work-order snapshot of the existing service checklist template.
- Before, after, and other photographic evidence with immediate storage upload.
- Mobile-browser image capture.
- Batch checklist progress saving and mandatory persisted validation before completion.
- Owner and employee access rules for work orders.

The implementation does not include appointments, quotes, maintenance reminders, inventory, image resizing, image compression, or a custom camera application.

## Existing Foundations

The current `Service` aggregate already owns company-scoped `ServiceChecklistItem` template items and photo-requirement flags (`RequireBeforePhotos` and `RequireAfterPhotos`). The new work order flow consumes these settings without altering service behavior.

`Professional` is an active company-scoped profile linked to `CompanyUser` and to the services that the professional can perform. This relationship is the source of truth for employee self-assignment and execution eligibility.

Cloudinary is already available through `IFileStorage`. The work order feature will store its returned key, not a public URL or image bytes.

## Domain Model

Source identifiers remain in English. The Portuguese UI uses "Ordem de Serviço" while the domain type is `WorkOrder`.

### Enums

```csharp
public enum WorkOrderStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public enum WorkOrderPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum WorkOrderPhotoType
{
    Before,
    After,
    Other
}
```

### WorkOrder

`WorkOrder` is the aggregate root. It has `Id`, `CompanyId`, `CustomerId`, `EquipmentId`, `ServiceId`, nullable `ProfessionalId`, `CreatedByUserId`, priority, reported problem, execution notes, status, and created/updated/started/completed UTC timestamps.

Customer, equipment, and service are required at creation. The equipment must belong to the selected customer and the current company. The service must be active and belong to the current company. A direct work order begins in `Pending`.

The aggregate owns collections of `WorkOrderItem`, `WorkOrderChecklistItem`, and `WorkOrderPhoto`. It exposes behavior methods instead of public mutation for assignment, header updates, starting, checklist progress updates, adding/removing evidence, and completion.

The owner can create a pending work order with no assigned professional and can assign or reassign an active professional qualified for the selected service. An employee can create a work order only when the employee has an active professional profile qualified for that service. In that case the application resolves the professional server-side, forces self-assignment, and forbids employee reassignment.

Starting requires the `Pending` status and an assigned, active professional qualified for the selected service. It changes the state to `InProgress` and records `StartedAtUtc`. Completing requires `InProgress`, runs the final checklist and photo validation, changes the state to `Completed`, and records `CompletedAtUtc`. Cancelling is allowed only before completion.

Header information may be updated while pending. Changing the selected service while pending replaces the work order checklist snapshot before execution begins. Assignment can be changed by an owner while pending. Financial items may be changed while pending or in progress, but never after completion. Checklist state, photos, and execution notes can be changed only while in progress.

### WorkOrderItem

`WorkOrderItem` holds `Id`, `CompanyId`, `WorkOrderId`, description, quantity, unit price, display order, and UTC timestamps. Description is required, quantity must be greater than zero, and unit price cannot be negative. The line total is calculated as quantity multiplied by unit price; the work order total is the sum of its line totals and is not trusted from the browser.

### WorkOrderChecklistItem

`WorkOrderChecklistItem` is the immutable checklist-template snapshot with mutable completion state. It holds `Id`, `CompanyId`, `WorkOrderId`, description, required flag, display order, `IsCompleted`, and nullable `CompletedAtUtc`.

When a work order is created, the active ordered `Service.ChecklistItems` collection is copied into these entities. If the service has no template items, the work order has no checklist and works normally. Later edits to a service template never affect existing work orders.

### WorkOrderPhoto

`WorkOrderPhoto` holds `Id`, `CompanyId`, `WorkOrderId`, `Type`, storage `FileKey`, optional description, `UploadedByUserId`, and `CreatedAtUtc`. It has no checklist-item reference in this MVP.

The service photo rules are evaluated from the work order service when completion is attempted. Required before or after evidence means at least one persisted photo of the corresponding type exists.

## Persistence

`AppDbContext` gains DbSets for work orders, items, checklist snapshots, and photos. Fluent configurations in Infrastructure use the established table naming convention and configure required properties, foreign keys, company-scoped lookup indexes, and ordered child collections.

The work order listing needs an index beginning with `CompanyId` and including status and creation date. Employee listing and access checks need an index beginning with `CompanyId` and including `ProfessionalId`. Child tables have indexes on `CompanyId`, `WorkOrderId`, and their display order or type where relevant.

The change is delivered through one descriptive migration named `AddWorkOrdersAndExecutionRecords`.

## Application Use Cases

The Application project introduces small, explicit work-order use cases and contracts. They resolve `ICurrentCompany` and `ICurrentUser`; they never accept an authenticated tenant identity from a browser request.

Core use cases include:

- `CreateWorkOrderUseCase`
- `GetWorkOrdersUseCase`
- `GetWorkOrderDetailsUseCase`
- `UpdateWorkOrderUseCase`
- `StartWorkOrderUseCase`
- `SaveWorkOrderChecklistProgressUseCase`
- `AddWorkOrderPhotoUseCase`
- `RemoveWorkOrderPhotoUseCase`
- `CompleteWorkOrderUseCase`
- `CancelWorkOrderUseCase`

The work-order application persistence interface stays focused on the queries and writes required by these use cases. Infrastructure implements it with EF Core and performs tenant ownership filters in the query.

Every use case returns an explicit operation result for expected business failures: missing record, forbidden access, inactive or unqualified professional, invalid state transition, invalid selection, image validation failure, photo limit reached, or incomplete completion requirements. The Web layer maps these outcomes to user-facing Portuguese messages without exposing internal exceptions.

## Authorization and Tenant Isolation

All authenticated work-order operations filter by `CompanyId`.

Owners may list and access every work order in their company. Employees may list and access only work orders whose `ProfessionalId` is their active professional profile. This restriction applies equally to list results, direct detail routes, checklist changes, photo uploads/downloads/removals, start, completion, and cancellation.

The server determines whether the user is an owner or employee and resolves the current employee professional profile. An employee cannot select or submit another professional identifier to bypass self-assignment. An employee without a qualified active professional profile receives a business validation result instead of an automatically created professional profile.

## Web Experience

The Web project adds a work-order list and a work-order registration/detail page. The page uses two tabs.

### Data and Items Tab

This tab contains customer, equipment, service, professional, priority, reported problem, and financial items.

Customer selection is required and searchable. The page provides a modal to create a customer without leaving the flow. Equipment selection is required, filtered to the selected customer, and has its own inline creation modal. Changing the customer clears a selected equipment from a different customer.

Service selection is required and only exposes active company services. Owner users can leave the professional empty while the work order is pending or assign an eligible professional. Employees see themselves as the fixed assignment when permitted.

The items editor adds, edits, removes, and orders description/quantity/unit-price rows and displays calculated line and work-order totals.

### Execution Tab

The execution tab becomes available after the work order is created. In the pending state it presents the action to start the service. In progress it presents, in operational order, before photos, the checklist, after photos, other photos, observations, a progress-save action, and completion.

The image input accepts only images and requests the environment camera on supported mobile browsers through `accept="image/*"` and `capture="environment"`. The browser remains responsible for its native capture experience; the MVP does not include a custom camera.

Checklist checkbox changes update local UI state and are submitted as a debounced batch. The user can explicitly select "Save progress". Tab change and any completion attempt flush pending changes. Completion calls the server only after this persistence operation; the server independently loads and validates the current persisted snapshot.

## Image Upload and Serving

Selecting a photo immediately starts a streaming upload. The server validates that the work order is in progress, the caller has work-order access, the file is an accepted image content type, the declared/streamed content does not exceed 10 MiB, and the work order has fewer than 30 photos and fewer than 10 photos of the requested type.

The Application use case uploads to the existing `IFileStorage` under a company/work-order folder and then persists a `WorkOrderPhoto` record. If record persistence fails after storage succeeds, it deletes the uploaded storage key as compensation. A remove action deletes the record and storage object while the work order is still in progress.

Image display uses an authenticated, tenant-checked endpoint that obtains the content from `IFileStorage`. Storage keys are never exposed to the browser and no large image collection is retained in Blazor Server component state.

## Completion Validation

`CompleteWorkOrderUseCase` is the final authority. It requires:

1. The work order is in progress.
2. An assigned, active, service-qualified professional exists.
3. Every persisted required checklist item is completed.
4. At least one persisted before photo exists when the service requires before photos.
5. At least one persisted after photo exists when the service requires after photos.

On any failure, the use case returns the corresponding business result and keeps the work order in progress. Only successful validation performs the `InProgress` to `Completed` transition.

## Routes and Endpoints

Authenticated routes follow the existing company-member policy, with owner or employee authorization enforced again by the use case. The Web layer adds routes for work-order list/details and endpoints for CRUD-like header/item changes, transitions, checklist batch persistence, photo upload/removal/content, and quick option queries.

The employee work-order list is server-filtered; the UI does not receive other professionals' rows and does not merely hide them after loading.

## Validation and Verification

The implementation uses server-side command validation and domain invariants. Browser validation is only a usability layer.

No automated tests are added in this phase because the project guidance explicitly defers tests unless requested. Validation consists of static inspection of domain invariants, use-case ownership queries, DI registrations, endpoint contracts, EF configurations, migration consistency, and Razor component references. The repository instruction prohibits build, test, and application execution unless the user explicitly requests it, so those commands are not part of this work.

## Out of Scope

- A stock or product catalog.
- Inventory deduction.
- Quote conversion.
- Appointment scheduling.
- Maintenance reminder generation.
- Customer-visible work-order pages.
- Image compression, resizing, annotation, or custom camera controls.
- Per-checklist-item photo evidence.
