# Work Orders and Service Execution Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the Phase 3 work-order MVP: company users create, execute, evidence, and complete equipment service work orders under checklist and photo rules.

**Architecture:** `WorkOrder` is a domain aggregate that owns financial items, a copied service-checklist snapshot, and persisted photo metadata. Explicit application use cases resolve tenant/user access and orchestrate EF Core repositories and the existing file-storage abstraction; Blazor supplies the two-tab UI while the backend remains authoritative for permissions and completion validation.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, Entity Framework Core, PostgreSQL, ASP.NET Core Identity, MudBlazor, FluentValidation, Cloudinary through `IFileStorage`.

**Spec:** `docs/superpowers/specs/2026-08-21-work-orders-execution-design.md`

## Global Constraints

- All source identifiers, migration names, logs, comments, and technical documentation must be in English; UI text remains Brazilian Portuguese.
- Resolve authenticated tenant identity from `ICurrentCompany` and `ICurrentUser`; never accept `CompanyId`, ownership, or employee assignment authority from the browser.
- Every work-order query and mutation must filter by `CompanyId`; employee access additionally requires the work order's `ProfessionalId` to match the employee's active professional profile.
- Use `Guid`, `TimeProvider`, UTC timestamps, protected entity state, async I/O, and `CancellationToken` propagation; no direct wall-clock calls in domain or application code.
- Reuse the existing `Service` checklist template, service photo requirements, `Professional` service links, and `IFileStorage`; do not add packages, projects, a stock catalog, inventory, appointments, quotes, maintenance reminders, image compression, or custom camera controls.
- A work order requires a customer, equipment, and active service; it starts as `Pending`, requires a qualified active professional to enter `InProgress`, and completes only after persisted checklist/photo validation.
- Owner users can assign/reassign pending work orders; employees can create only for their own active qualified professional profile and cannot reassign work orders.
- Do not add automated tests, run builds, test runs, migration execution, or the application unless the user explicitly requests them. Use only non-executing static validation commands.
- Preserve unrelated worktree changes. Stage and commit only feature-owned files.

---

## File Structure

- Create `src/VoltaMarcada.Domain/Enums/WorkOrderStatus.cs`, `WorkOrderPriority.cs`, and `WorkOrderPhotoType.cs`: stable work-order status, priority, and evidence-type values.
- Create `src/VoltaMarcada.Domain/ValueObjects/WorkOrderItemDefinition.cs` and `WorkOrderChecklistItemDefinition.cs`: immutable inputs used to create/rebuild aggregate children.
- Create `src/VoltaMarcada.Domain/Entities/WorkOrder.cs`, `WorkOrderItem.cs`, `WorkOrderChecklistItem.cs`, and `WorkOrderPhoto.cs`: aggregate and protected child state.
- Create `src/VoltaMarcada.Application/Abstractions/Persistence/IWorkOrderRepository.cs`: company-scoped work-order persistence and read models.
- Create `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommands.cs`, `WorkOrderDtos.cs`, `WorkOrderOperationResult.cs`, `WorkOrderCommandValidators.cs`, `WorkOrderAuthorization.cs`, and one focused file per use case.
- Modify `src/VoltaMarcada.Application/Abstractions/Persistence/IProfessionalRepository.cs` and `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ProfessionalRepository.cs`: resolve active professionals qualified for a service and their company-user identity.
- Create `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs`: EF Core implementation with tenant and employee predicates in every lookup.
- Modify `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`: expose work-order DbSets.
- Create `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderConfiguration.cs`, `WorkOrderItemConfiguration.cs`, `WorkOrderChecklistItemConfiguration.cs`, and `WorkOrderPhotoConfiguration.cs`: EF mappings, tenant-safe foreign keys, and query indexes.
- Create migration files `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_AddWorkOrdersAndExecutionRecords.cs` and `.Designer.cs`; modify `AppDbContextModelSnapshot.cs`.
- Modify `src/VoltaMarcada.Application/DependencyInjection.cs` and `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`: register use cases, authorization helper, and repository.
- Create `src/VoltaMarcada.Web/Endpoints/WorkOrderEndpoints.cs`; modify `src/VoltaMarcada.Web/Program.cs`: map authenticated internal API operations and image content delivery.
- Create `src/VoltaMarcada.Web/Components/Pages/WorkOrders.razor`, `WorkOrderRegistration.razor`, `Shared/WorkOrderItemsEditor.razor`, and `Shared/WorkOrderExecutionPanel.razor`: listing, data/items tab, quick-create dialogs, and execution tab.
- Modify `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor` and `src/VoltaMarcada.Web/wwwroot/app.css`: navigation and responsive feature styling.

## Task 1: Model the Work-Order Aggregate

**Files:**

- Create: `src/VoltaMarcada.Domain/Enums/WorkOrderStatus.cs`
- Create: `src/VoltaMarcada.Domain/Enums/WorkOrderPriority.cs`
- Create: `src/VoltaMarcada.Domain/Enums/WorkOrderPhotoType.cs`
- Create: `src/VoltaMarcada.Domain/ValueObjects/WorkOrderItemDefinition.cs`
- Create: `src/VoltaMarcada.Domain/ValueObjects/WorkOrderChecklistItemDefinition.cs`
- Create: `src/VoltaMarcada.Domain/Entities/WorkOrder.cs`
- Create: `src/VoltaMarcada.Domain/Entities/WorkOrderItem.cs`
- Create: `src/VoltaMarcada.Domain/Entities/WorkOrderChecklistItem.cs`
- Create: `src/VoltaMarcada.Domain/Entities/WorkOrderPhoto.cs`

**Consumes:** Existing `ICompanyScopedEntity`, `DomainException`, active-service checklist templates, and the domain timestamp conventions.

**Produces:** A protected `WorkOrder` aggregate with status transitions, item totals, copied checklist definitions, and evidence metadata for persistence and use cases.

- [ ] **Step 1: Add the three finite domain enums**

```csharp
public enum WorkOrderStatus { Pending, InProgress, Completed, Cancelled }
public enum WorkOrderPriority { Low, Normal, High, Urgent }
public enum WorkOrderPhotoType { Before, After, Other }
```

Do not use string status/priority/photo values anywhere in the feature.

- [ ] **Step 2: Add immutable child-definition value objects**

```csharp
public sealed record WorkOrderItemDefinition(string Description, decimal Quantity, decimal UnitPrice);
public sealed record WorkOrderChecklistItemDefinition(string Description, bool IsRequired, int DisplayOrder);
```

The aggregate receives these definitions rather than mutable UI/persistence children. It creates all child IDs server-side.

- [ ] **Step 3: Implement protected child entities**

`WorkOrderItem` has company/work-order IDs, description, quantity, unit price, display order, creation/update timestamps, and a calculated `TotalAmount`. `WorkOrderChecklistItem` has company/work-order IDs, description, required flag, display order, completion state, and nullable completion timestamp. `WorkOrderPhoto` has company/work-order IDs, `WorkOrderPhotoType`, private storage key, optional description, uploader user ID, and creation timestamp.

Use private parameterless constructors, private setters, and internal aggregate-only creation/update methods. Validate non-empty descriptions, positive quantity, non-negative price, non-empty storage/user IDs, and a non-negative order.

- [ ] **Step 4: Implement aggregate creation and pending editing**

Define the creation contract as:

```csharp
public static WorkOrder Create(
    Guid companyId, Guid customerId, Guid equipmentId, Guid serviceId,
    Guid? professionalId, Guid createdByUserId, WorkOrderPriority priority,
    string reportedProblem, IReadOnlyCollection<WorkOrderItemDefinition> items,
    IReadOnlyCollection<WorkOrderChecklistItemDefinition> checklistItems,
    DateTimeOffset now);
```

Require all non-nullable IDs, a defined priority, and nonblank reported problem. Set `Status = WorkOrderStatus.Pending`, UTC audit timestamps, and create snapshot/item children. Add `UpdatePendingDetails`, `AssignProfessional`, and `ReplaceItems`; allow owner-controlled service changes only while pending and replace the snapshot atomically from supplied validated definitions.

- [ ] **Step 5: Implement execution transitions and completion validation**

Add `Start`, `SaveChecklistProgress`, `UpdateExecutionNotes`, `AddPhoto`, `RemovePhoto`, `Complete`, and `Cancel`. `Start` requires pending status and a nonempty professional ID. Checklist updates identify only aggregate-owned IDs and set/clear `CompletedAtUtc` from `now`. `Complete` requires in-progress status, an assigned professional, all required snapshot items completed, and persisted before/after evidence when the selected service rules require it. `Cancel` rejects completed work orders.

- [ ] **Step 6: Perform static domain review**

Run:

```powershell
rg -n "DateTime\.(Now|UtcNow)|DateTimeOffset\.(Now|UtcNow)|public .* set;|WorkOrderStatus.*string" src/VoltaMarcada.Domain
```

Expected: the new model has no public state setters, direct clock calls, or stringly typed status values.

## Task 2: Define Work-Order Application Contracts and Access Scope

**Files:**

- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/IWorkOrderRepository.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommands.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/WorkOrderOperationResult.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommandValidators.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/WorkOrderAuthorization.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IProfessionalRepository.cs`

**Consumes:** Task 1 aggregate types; existing `ICurrentCompany`, `ICurrentUser`, company membership, customer, equipment, service, and professional repositories.

**Produces:** Explicit input/result contracts and a reusable server-side owner/employee access scope for every work-order use case.

- [ ] **Step 1: Define commands and DTOs with no browser-supplied tenant authority**

Create these contracts:

```csharp
public sealed record WorkOrderItemInput(string Description, decimal Quantity, decimal UnitPrice);
public sealed record CreateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId,
    Guid? ProfessionalId, WorkOrderPriority Priority, string ReportedProblem,
    IReadOnlyList<WorkOrderItemInput> Items);
public sealed record UpdateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId,
    Guid? ProfessionalId, WorkOrderPriority Priority, string ReportedProblem,
    IReadOnlyList<WorkOrderItemInput> Items);
public sealed record ChecklistProgressInput(Guid ChecklistItemId, bool IsCompleted);
public sealed record SaveWorkOrderChecklistProgressCommand(
    IReadOnlyList<ChecklistProgressInput> ChecklistItems, string? ExecutionNotes);
```

Expose distinct list, detail, checklist, item, photo, and selection-option DTOs. The detail DTO must contain photo requirements copied from the selected service, computed totals, current user capabilities (`CanEdit`, `CanAssignProfessional`, `CanStart`, `CanExecute`), and only the records that the caller is authorized to see.

- [ ] **Step 2: Define expected operation outcomes and validators**

Use a `WorkOrderOperationResultCode` that covers `Success`, `InvalidInput`, `NotFound`, `Forbidden`, `CustomerNotFound`, `EquipmentNotFound`, `InvalidEquipmentCustomer`, `ServiceNotFound`, `ProfessionalNotFound`, `ProfessionalNotQualified`, `ProfessionalProfileRequired`, `InvalidStatus`, `ChecklistIncomplete`, `BeforePhotoRequired`, `AfterPhotoRequired`, `PhotoLimitReached`, and `InvalidPhoto`.

Validate required IDs, defined enums, maximum text lengths consistent with entity configurations, nonblank reported problem, at most 100 item inputs, nonblank item descriptions, positive quantities, and non-negative prices. The batch checklist command requires non-null unique checklist IDs; it does not decide ownership or completion rules.

- [ ] **Step 3: Define repository and read-scope interfaces**

Use this access record and repository surface:

```csharp
public sealed record WorkOrderAccessScope(bool IsOwner, Guid? ProfessionalId);

Task<WorkOrder?> GetForAccessAsync(Guid workOrderId, Guid companyId,
    Guid? professionalId, CancellationToken cancellationToken = default);
Task<IReadOnlyList<WorkOrderListItem>> GetAllForAccessAsync(Guid companyId,
    Guid? professionalId, GetWorkOrdersQuery query, CancellationToken cancellationToken = default);
Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
Task SaveChangesAsync(CancellationToken cancellationToken = default);
```

`professionalId` is null only for owners. The repository never treats null as an employee wildcard.

- [ ] **Step 4: Implement `WorkOrderAuthorization` and professional lookup contracts**

`WorkOrderAuthorization.GetAccessScopeAsync` resolves the current company and checks `IEmployeeInvitationRepository.IsCurrentUserOwnerAsync`. For an owner it returns `(true, null)`. For an employee it resolves the current professional through `IProfessionalRepository.GetForCurrentUserAsync`; it returns `(false, professional.Id)` only when the professional exists and is active.

Add `GetForCurrentUserAndServiceAsync(Guid userId, Guid companyId, Guid serviceId, CancellationToken)` to `IProfessionalRepository`. Its implementation must return an active professional associated with the requested active service, or null. This is the self-assignment guard for employee creation.

- [ ] **Step 5: Perform static contract review**

Run:

```powershell
rg -n "CompanyId" src/VoltaMarcada.Application/WorkOrders
rg -n "GetForCurrentUserAndServiceAsync|WorkOrderAccessScope|WorkOrderOperationResultCode" src/VoltaMarcada.Application
```

Expected: commands contain no `CompanyId`; all create/update/transition paths have an explicit access/result model.

## Task 3: Implement Work-Order Persistence and Schema

**Files:**

- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ProfessionalRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderItemConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderChecklistItemConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderPhotoConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_AddWorkOrdersAndExecutionRecords.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_AddWorkOrdersAndExecutionRecords.Designer.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`

**Consumes:** Task 1 entities and Task 2 persistence contracts.

**Produces:** EF Core persistence that loads child state, makes tenant/employee predicates part of SQL queries, and creates all Phase 3 tables through a descriptive migration.

- [ ] **Step 1: Add DbSets and mappings**

Add DbSets for `WorkOrders`, `WorkOrderItems`, `WorkOrderChecklistItems`, and `WorkOrderPhotos`. Map all entity IDs with `ValueGeneratedNever`, required company IDs, bounded English text fields, `decimal(18,2)` quantity/unit-price columns, nullable timestamps where appropriate, and enum values as integers.

Configure aggregate-owned relationships from work order to the three child collections with cascade deletion. Configure tenant-consistent relationships to customer, equipment, service, and professional using company-scoped composite alternate keys where the existing entity mappings support them; otherwise validate company ownership in the use case and use a normal FK without cross-tenant cascade behavior.

- [ ] **Step 2: Add query-driven indexes**

Create an index named `IX_WorkOrders_CompanyId_Status_CreatedAtUtc` on `(CompanyId, Status, CreatedAtUtc)` and `IX_WorkOrders_CompanyId_ProfessionalId_CreatedAtUtc` on `(CompanyId, ProfessionalId, CreatedAtUtc)`. Add ordered child indexes on `(CompanyId, WorkOrderId, DisplayOrder)` for items/checklists and `(CompanyId, WorkOrderId, Type)` for photos.

- [ ] **Step 3: Implement `WorkOrderRepository` with access filters inside queries**

`GetForAccessAsync` queries `WorkOrders.Include` for all children and applies:

```csharp
order => order.Id == workOrderId && order.CompanyId == companyId &&
    (professionalId is null || order.ProfessionalId == professionalId)
```

`GetAllForAccessAsync` applies the same company/professional condition before status/search filtering, projects only list fields, orders newest first, and never loads a different employee's records to filter in memory. `AddAsync` and `SaveChangesAsync` follow existing repository conventions.

Implement `GetForCurrentUserAndServiceAsync` in `ProfessionalRepository` using joins constrained by company, user, active professional, active service, and `ProfessionalServices.ServiceId == serviceId`.

- [ ] **Step 4: Add the migration and snapshot**

Create a descriptive migration named `AddWorkOrdersAndExecutionRecords`, its designer, and matching model snapshot changes. The `Up` method creates the four tables, foreign keys, and indexes; the `Down` method drops photo, checklist, item, then aggregate table. Do not execute or apply the migration during this task.

- [ ] **Step 5: Register the repository and perform static persistence review**

Register `IWorkOrderRepository` with `WorkOrderRepository` in Infrastructure DI. Run:

```powershell
rg -n "WorkOrders|WorkOrderItems|WorkOrderChecklistItems|WorkOrderPhotos|IX_WorkOrders_CompanyId" src/VoltaMarcada.Infrastructure
rg -n "CompanyId == companyId" src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs
```

Expected: all four models, indexes, migration artifacts, DI registration, and SQL-level tenant filters are present.

## Task 4: Implement Work-Order Creation, Reading, and Pending Updates

**Files:**

- Create: `src/VoltaMarcada.Application/WorkOrders/CreateWorkOrderUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/GetWorkOrdersUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/GetWorkOrderDetailsUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/GetWorkOrderFormOptionsUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/UpdateWorkOrderUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/CancelWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`

**Consumes:** Tasks 1-3 and existing customer/equipment/service/professional repositories.

**Produces:** Tenant-safe creation, owner/employee assignment enforcement, list/detail projections, form data, pending updates, and cancellation.

- [ ] **Step 1: Implement creation with server-resolved assignment**

`CreateWorkOrderUseCase` validates the command, gets the access scope, loads customer/equipment/service by both identifier and company, verifies equipment belongs to customer, and rejects inactive services. Map the service's ordered `ChecklistItems` to `WorkOrderChecklistItemDefinition`.

For owners, validate a supplied professional is active and has the selected service; a null assignment stays null. For employees, ignore any submitted professional ID, call `GetForCurrentUserAndServiceAsync`, return `ProfessionalProfileRequired` when absent, and force the returned professional ID. Create the aggregate with `timeProvider.GetUtcNow()`, add it, save once, and return its ID.

- [ ] **Step 2: Implement list/detail projections and options**

`GetWorkOrdersUseCase` calls `WorkOrderAuthorization`, then `GetAllForAccessAsync`; owner scope gets all company rows and employee scope gets only assigned rows. Add status/search filters to `GetWorkOrdersQuery` but no browser-supplied company/professional filters.

`GetWorkOrderDetailsUseCase` loads through `GetForAccessAsync`, maps items/checklist/photos in display order, computes totals server-side, and computes capabilities from the scope plus status. `GetWorkOrderFormOptionsUseCase` returns active customer options, equipment options for a selected customer, active service options, and active qualified professional options for owners; employee options expose only the current professional when qualified.

- [ ] **Step 3: Implement pending update and cancellation rules**

`UpdateWorkOrderUseCase` loads through access scope and rejects non-pending work orders. It repeats all customer/equipment/service/professional ownership and qualification validation from creation. An employee keeps the persisted self-assignment regardless of submitted values. When service changes, build new checklist definitions from its current template and call aggregate pending-update behavior. Rebuild items and save once.

`CancelWorkOrderUseCase` loads through scope, allows cancellation only before completion, calls `Cancel(timeProvider.GetUtcNow())`, saves once, and returns the status result.

- [ ] **Step 4: Register use cases and perform static workflow review**

Register every Task 4 use case as scoped in `AddApplication`. Run:

```powershell
rg -n "GetForAccessAsync|GetForCurrentUserAndServiceAsync|EquipmentId.*CustomerId|IsActive" src/VoltaMarcada.Application/WorkOrders
```

Expected: creation and pending updates revalidate tenant ownership, equipment/customer association, service activity, and employee self-assignment server-side.

## Task 5: Implement Execution, Checklist Persistence, and Evidence Storage

**Files:**

- Create: `src/VoltaMarcada.Application/WorkOrders/StartWorkOrderUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/SaveWorkOrderChecklistProgressUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/AddWorkOrderPhotoUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/RemoveWorkOrderPhotoUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/GetWorkOrderPhotoContentUseCase.cs`
- Create: `src/VoltaMarcada.Application/WorkOrders/CompleteWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`

**Consumes:** Aggregate behavior, access scope, persisted work-order state, `IFileStorage`, and `TimeProvider`.

**Produces:** Server-authoritative execution transitions, batched checklist saving, immediate streaming uploads, authorized image delivery, and final completion checks.

- [ ] **Step 1: Implement start and batch checklist save**

`StartWorkOrderUseCase` loads through access scope, confirms the persisted professional is active and qualified for the persisted service, calls `Start`, saves, and returns `InvalidStatus` or `ProfessionalNotQualified` without mutation on failure.

`SaveWorkOrderChecklistProgressUseCase` loads through the same access predicate, rejects any state other than `InProgress`, rejects IDs not belonging to the aggregate, calls `SaveChecklistProgress` and `UpdateExecutionNotes`, then saves once. It never relies on a prior UI availability/result.

- [ ] **Step 2: Implement immediate upload with bounded streams**

Define an application upload input containing file name, content type, length, photo type, optional description, and stream. `AddWorkOrderPhotoUseCase` accepts only JPEG, PNG, WebP, or HEIC; rejects zero or greater-than-10-MiB files; loads through access scope; requires `InProgress`; and counts persisted photos to enforce 30 total and 10 per type.

Upload with:

```csharp
await fileStorage.UploadAsync(
    new FileStorageUploadRequest(upload, $"companies/{companyId:N}/work-orders/{workOrderId:N}"),
    cancellationToken);
```

After storage succeeds, add aggregate photo metadata with current user ID and UTC timestamp, then save. If adding/saving throws, call `IFileStorage.DeleteAsync(uploadResult.Key, cancellationToken)` before rethrowing so orphan storage objects are not retained.

- [ ] **Step 3: Implement removal and authenticated content delivery**

`RemoveWorkOrderPhotoUseCase` loads through access scope, allows removal only in progress, locates the aggregate-owned photo, removes it, saves the database change, then deletes the storage key. When storage deletion fails after database success, log the photo/work-order IDs and surface an unexpected failure rather than restoring deleted metadata.

`GetWorkOrderPhotoContentUseCase` loads the work order through access scope, finds its owned photo, calls `IFileStorage.GetAsync`, and returns bytes/content type. No storage key is returned in a DTO or route.

- [ ] **Step 4: Implement final completion validation**

`CompleteWorkOrderUseCase` loads through access scope, reloads/validates the assigned professional qualification, calls aggregate `Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, now)`, and maps invariant/business failures to `ChecklistIncomplete`, `BeforePhotoRequired`, `AfterPhotoRequired`, `ProfessionalNotQualified`, or `InvalidStatus`. It saves only after successful validation.

- [ ] **Step 5: Register and statically review execution security**

Register the six use cases. Run:

```powershell
rg -n "10 \* 1024 \* 1024|PhotoLimitReached|RequireBeforePhotos|RequireAfterPhotos|GetForAccessAsync" src/VoltaMarcada.Application/WorkOrders
rg -n "FileKey|Key" src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs
```

Expected: size/count/completion rules are explicit and no photo DTO exposes an internal storage key.

## Task 6: Expose Authenticated Internal Endpoints

**Files:**

- Create: `src/VoltaMarcada.Web/Endpoints/WorkOrderEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`

**Consumes:** Tasks 4-5 use cases and operation-result codes.

**Produces:** An authenticated endpoint group that maps dedicated contracts without exposing persistence entities or bypassing use-case authorization.

- [ ] **Step 1: Map typed internal endpoints**

Create `/work-orders` under `AuthorizationPolicies.CompanyMember`. Add list, detail, form-options, create, pending update, cancel, start, checklist-progress, photo upload, photo removal, photo content, and complete routes. Map every mutating result code to explicit `BadRequest`, `NotFound`, `Forbid`, or success responses; messages remain Portuguese in the Web mapping.

Use `IFormFile` only in the photo endpoint. Reject `file.Length > 10 * 1024 * 1024` before passing `file.OpenReadStream()` and its metadata to the upload use case; the use case repeats the length/content-type validation. The route includes work-order and photo GUIDs but never includes company/professional/storage identifiers.

- [ ] **Step 2: Serve photo bytes without public storage URLs**

Map `GET /work-orders/{workOrderId:guid}/photos/{photoId:guid}/content` to the content use case and return `Results.File(content.Bytes, content.ContentType)`. Unauthorized access must resolve as not found/forbidden from the use case rather than leaking another tenant's file metadata.

- [ ] **Step 3: Register the endpoint group and perform static route review**

Call `app.MapWorkOrderEndpoints()` beside the other authenticated internal endpoint mappings in `Program.cs`. Run:

```powershell
rg -n "MapWorkOrderEndpoints|MapGroup\(\"/work-orders\"|RequireAuthorization|OpenReadStream" src/VoltaMarcada.Web
```

Expected: all routes require company membership and photo routes use bounded request streams.

## Task 7: Build the Work-Order List and Data/Items Tab

**Files:**

- Create: `src/VoltaMarcada.Web/Components/Pages/WorkOrders.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor`
- Create: `src/VoltaMarcada.Web/Components/Shared/WorkOrderItemsEditor.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`

**Consumes:** Task 4 list/detail/options/create/update use cases, the existing customer/equipment command models, and MudBlazor form patterns.

**Produces:** A company-member work-order list plus the first tab of the two-tab detail screen, including quick customer/equipment creation.

- [ ] **Step 1: Add navigation and the server-filtered list**

Add an `Ordens de serviço` navigation link for company members. `WorkOrders.razor` injects only `GetWorkOrdersUseCase`, renders search/status controls and an accessible responsive MudTable, and links each row to `/ordens-servico/registro?id={id}`. Do not add a UI professional filter for employees; the use case already returns only their rows. The primary action reads `Nova OS`.

- [ ] **Step 2: Create the registration page with two tabs**

Use route `/ordens-servico/registro` and query `id`. Before an ID exists, render only the `Dados e itens` tab and save a new work order before navigating to its detail URL. With an ID, load the detail DTO and render a second `Execução` tab using the component from Task 8.

The data tab uses `MudAutocomplete` for customer/equipment/service/professional options; clear equipment when the selected customer changes. Bind professional selection only for owner capability. Render priority, reported problem, item editor, totals, status, save/cancel controls, and localized expected-result messages.

- [ ] **Step 3: Add embedded quick-create dialogs**

Within `WorkOrderRegistration.razor`, use `MudDialog @bind-Visible` dialogs rather than navigation away. The customer dialog uses `CreateCustomerUseCase`, validates the same minimally required customer fields, saves, refreshes customer options, and selects the new customer. The equipment dialog is enabled only after a customer selection, uses `CreateEquipmentUseCase` with that server-validated customer ID, refreshes equipment options, and selects the new equipment.

Do not construct company IDs in either dialog; the existing use cases resolve them. Preserve form state if a dialog is cancelled or reports a validation error.

- [ ] **Step 4: Implement focused item editing**

`WorkOrderItemsEditor` accepts a bindable `List<WorkOrderItemInput>` and read-only flag. It renders description, quantity, Brazilian currency unit price, computed line total, removal, and `Adicionar item`; prohibit empty description/invalid numeric rows before parent save. It renders the calculated work-order total from current inputs but the backend still recalculates it.

- [ ] **Step 5: Add responsive styling and static UI review**

Add only work-order-specific classes to `app.css`: mobile-friendly toolbar actions, two-tab content spacing, item row grids, photo grid placeholders, and status chips. Run:

```powershell
rg -n "ordens-servico|Nova OS|Dados e itens|CreateCustomerUseCase|CreateEquipmentUseCase|WorkOrderItemsEditor" src/VoltaMarcada.Web
```

Expected: list, registration page, quick dialogs, and item editor all use the selected use cases and Brazilian Portuguese UI copy.

## Task 8: Build the Execution Tab with Autosave and Mobile Evidence

**Files:**

- Create: `src/VoltaMarcada.Web/Components/Shared/WorkOrderExecutionPanel.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`

**Consumes:** Task 5 start/checklist/photo/complete use cases, the detail DTO, and the persisted work-order ID from Task 7.

**Produces:** Mobile-friendly execution flows that persist photos immediately, batch checklist state, and prevent an unpersisted or invalid completion action.

- [ ] **Step 1: Render state-aware execution actions**

For pending details, show `Iniciar atendimento` only when `CanStart` is true and call `StartWorkOrderUseCase`; refresh the parent detail after success. For in-progress details, render sections in this exact order: `Fotos antes`, `Checklist`, `Fotos depois`, `Outras fotos`, `Observações da execução`, `Salvar progresso`, and `Concluir serviço`. Completed/cancelled work orders render read-only evidence/checklist/notes and no mutation controls.

- [ ] **Step 2: Add batch checklist state and deterministic autosave**

Keep checkbox state in a local dictionary keyed by checklist-item ID. On change, reset a cancellable 1.5-second debounce operation; its only network call is `SaveWorkOrderChecklistProgressUseCase` with the entire current checklist collection and execution notes. The explicit save button calls the same method immediately.

Before switching away from the execution tab or calling completion, await a shared `FlushProgressAsync` method. If it returns an expected error, leave the user on execution and show the message; never call complete with stale local state.

- [ ] **Step 3: Add immediate image upload and evidence rendering**

For every evidence section, render `InputFile` with `accept="image/*"` and an additional `capture="environment"` attribute. On file selection, invoke `AddWorkOrderPhotoUseCase` immediately with `IBrowserFile.OpenReadStream(10 * 1024 * 1024 + 1)`, render an in-progress indicator, then refresh the persisted detail/photo list. Reject client-side values over 10 MiB before opening the stream, but always surface server validation results.

Render persisted photo thumbnails from the authenticated content endpoint `/work-orders/{workOrderId}/photos/{photoId}/content`; never retain `byte[]` photo content in component state. Show remove actions only while in progress, backed by `RemoveWorkOrderPhotoUseCase`.

- [ ] **Step 4: Complete only after a successful flush**

`CompleteAsync` first calls `FlushProgressAsync`; only then calls `CompleteWorkOrderUseCase`. Map `ChecklistIncomplete`, `BeforePhotoRequired`, `AfterPhotoRequired`, `ProfessionalNotQualified`, and `InvalidStatus` to specific Portuguese feedback, refresh the detail on success, and transition the panel to its read-only completed state.

- [ ] **Step 5: Perform static execution review**

Run:

```powershell
rg -n "capture=\"environment\"|OpenReadStream|FlushProgressAsync|SaveWorkOrderChecklistProgressUseCase|CompleteWorkOrderUseCase" src/VoltaMarcada.Web/Components
rg -n "byte\[\]|FileKey|Photo.*Url" src/VoltaMarcada.Web/Components/Shared/WorkOrderExecutionPanel.razor
```

Expected: evidence uploads immediately with bounded streams, checklist saves are batched, completion flushes first, and the component does not retain image bytes or storage keys.

## Task 9: Final Static Verification and Focused Commit

**Files:**

- Review: every source, migration, endpoint, component, and stylesheet file listed in Tasks 1-8
- Modify: `docs/superpowers/specs/2026-08-21-work-orders-execution-design.md` only if implementation exposes a real approved-design mismatch

**Consumes:** The completed feature.

**Produces:** A statically reviewed Phase 3 implementation committed without unrelated changes.

- [ ] **Step 1: Review end-to-end flows**

Confirm these paths are type-consistent and tenant-safe:

```text
Owner or employee -> CreateWorkOrderCommand -> WorkOrder -> EF records -> list/detail DTO -> Blazor data tab
Service checklist template -> WorkOrderChecklistItemDefinition -> snapshot -> batch progress save -> completion validation
IBrowserFile/IFormFile -> bounded stream -> IFileStorage -> WorkOrderPhoto metadata -> authenticated content endpoint -> thumbnail
```

- [ ] **Step 2: Review owner/employee boundaries**

Confirm every repository detail/list/photo predicate contains `CompanyId`, employee scopes add `ProfessionalId`, employee creation resolves its own qualified professional rather than accepting request assignment, and every owner-only reassignment check happens in the use case rather than only in Razor visibility.

- [ ] **Step 3: Review completion and upload invariants**

Confirm `CompleteWorkOrderUseCase` reloads persisted aggregate state, checks required snapshot items and current persisted photo types, and changes status only after successful validation. Confirm upload allows only the four listed raster formats, enforces 10 MiB/30 total/10 per type, compensates failed persistence with storage deletion, and never publishes storage keys.

- [ ] **Step 4: Run non-executing static checks**

Run:

```powershell
git diff --check
rg -n "DateTime\.(Now|UtcNow)|DateTimeOffset\.(Now|UtcNow)|CompanyId.*Command|FileKey.*Dto" src/VoltaMarcada.Domain src/VoltaMarcada.Application src/VoltaMarcada.Web
rg -n "WorkOrderStatus|WorkOrderChecklistItem|WorkOrderPhoto|MapWorkOrderEndpoints" src
git status --short
```

Expected: no whitespace errors, no direct clocks in new domain/application logic, no tenant IDs in commands or storage keys in DTOs, and all Phase 3 artifacts are wired through the host.

- [ ] **Step 5: Commit only feature-owned files**

Inspect the changed paths, stage only the files named in Tasks 1-8 plus any approved spec correction, verify `git diff --cached --name-only`, then commit:

```powershell
git commit -m "Add work order execution"
```

Do not stage or commit unrelated worktree changes.
