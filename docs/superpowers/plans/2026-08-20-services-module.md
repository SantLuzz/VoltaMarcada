# Services Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a tenant-isolated Services management module with enum-driven pricing, item-level mandatory checklist entries, photo requirements, online-request eligibility, and a menu entry.

**Architecture:** Add a Service aggregate and its ServiceChecklistItem children to the Domain project; follow the existing Equipment vertical slice for application use cases, EF Core persistence, minimal API endpoints, and Blazor management pages. The registration page keeps checklist drafts locally, while application validation and domain rules remain the authoritative business validation.

**Tech Stack:** .NET 10, C#, ASP.NET Core minimal APIs, Blazor Web App, MudBlazor, EF Core, PostgreSQL, FluentValidation.

**Spec:** docs/superpowers/specs/2026-08-20-services-design.md

## Global Constraints

- All source identifiers, database objects, logs, comments, and technical documentation are in English; user-facing UI text is Brazilian Portuguese.
- Every tenant-owned operation resolves CompanyId from ICurrentCompany; no authenticated request accepts a company identifier.
- Use Guid IDs, TimeProvider for timestamps, private setters, and explicit domain behavior.
- Fixed and StartingAt require a nonnegative value; QuoteRequired and Hidden do not accept a value.
- DurationMinutes is a positive whole number represented by int and labelled in minutes in the UI.
- Checklist requiredness belongs to ServiceChecklistItem.IsRequired; no service-wide checklist-required flag may be introduced.
- Use MudBlazor for all interactive controls and follow src/VoltaMarcada.Web/AGENTS.md.
- Automated tests, builds, test runs, and application launches are outside this MVP request. Validate by static inspection only.

---

## File Structure

- src/VoltaMarcada.Domain/Enums/ServicePriceType.cs: price display semantics.
- src/VoltaMarcada.Domain/Entities/Service.cs: company-scoped aggregate and checklist lifecycle.
- src/VoltaMarcada.Domain/Entities/ServiceChecklistItem.cs: text, requiredness, and display order for one checklist row.
- src/VoltaMarcada.Application/Services/: commands, DTOs, validation, operation result, and CRUD use cases.
- src/VoltaMarcada.Application/Abstractions/Persistence/IServiceRepository.cs: tenant-safe persistence contract.
- src/VoltaMarcada.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs: service mapping.
- src/VoltaMarcada.Infrastructure/Persistence/Configurations/ServiceChecklistItemConfiguration.cs: checklist mapping.
- src/VoltaMarcada.Infrastructure/Persistence/Repositories/ServiceRepository.cs: EF implementation.
- src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs and dependency-injection files: composition.
- src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260820130000_AddServices.cs and model snapshot: schema.
- src/VoltaMarcada.Web/Endpoints/ServiceEndpoints.cs and Program.cs: HTTP contract/mapping.
- src/VoltaMarcada.Web/Components/Pages/Services.razor: list/search/delete page.
- src/VoltaMarcada.Web/Components/Pages/ServiceRegistration.razor: registration/edit page.
- src/VoltaMarcada.Web/Components/Layout/MainLayout.razor and wwwroot/app.css: menu and shared layout styling.

### Task 1: Define the Services Domain Aggregate

**Files:**

- Create: src/VoltaMarcada.Domain/Enums/ServicePriceType.cs
- Create: src/VoltaMarcada.Domain/Entities/Service.cs
- Create: src/VoltaMarcada.Domain/Entities/ServiceChecklistItem.cs

**Interfaces:**

- Produces: public enum ServicePriceType { Fixed, StartingAt, QuoteRequired, Hidden }.
- Produces: Service.Create(Guid companyId, string name, string? description, int durationMinutes, ServicePriceType priceType, decimal? basePrice, bool allowOnlineBooking, bool requireBeforePhotos, bool requireAfterPhotos, IEnumerable<ServiceChecklistItem> checklistItems, DateTimeOffset now).
- Produces: Service.Update(string name, string? description, int durationMinutes, ServicePriceType priceType, decimal? basePrice, bool allowOnlineBooking, bool requireBeforePhotos, bool requireAfterPhotos, IEnumerable<ServiceChecklistItem> checklistItems, DateTimeOffset now).
- Produces: ServiceChecklistItem.Create(string description, bool isRequired, int displayOrder).

- [ ] **Step 1: Add the price enum and checklist item**

Create the four enum values in the stated order. Give ServiceChecklistItem private setters and a private parameterless EF constructor; reject blank descriptions and negative display orders with DomainException, trim descriptions in the factory, and expose IsRequired directly.

- [ ] **Step 2: Implement aggregate creation**

Create Service as ICompanyScopedEntity with private checklist storage and a read-only checklist property. Its factory must assign Guid identity, normalize optional description, set IsActive true, set both timestamps to now.ToUniversalTime(), and validate company ID, name, duration, enum value, price/value combination, and non-null checklist collection.

- [ ] **Step 3: Implement aggregate update and checklist replacement**

Update all editable Service values through one behavior method. Validate exactly as creation does, replace the children collection, and reindex them in submitted order starting from zero, so display order never becomes inconsistent after removal/reordering.

- [ ] **Step 4: Static-review the domain**

Verify no public setter exists, timestamps use no direct system clock, Fixed/StartingAt require base price, QuoteRequired/Hidden reject base price, and mandatory state is only per checklist item.

- [ ] **Step 5: Commit**

~~~powershell
git add src/VoltaMarcada.Domain/Enums/ServicePriceType.cs src/VoltaMarcada.Domain/Entities/Service.cs src/VoltaMarcada.Domain/Entities/ServiceChecklistItem.cs
git commit -m "feat: add services domain model"
~~~

### Task 2: Add Commands, Validation, and Use Cases

**Files:**

- Create: src/VoltaMarcada.Application/Services/ServiceChecklistItemInput.cs
- Create: src/VoltaMarcada.Application/Services/CreateServiceCommand.cs
- Create: src/VoltaMarcada.Application/Services/UpdateServiceCommand.cs
- Create: src/VoltaMarcada.Application/Services/ServiceCommandValidator.cs
- Create: src/VoltaMarcada.Application/Services/ServiceDto.cs
- Create: src/VoltaMarcada.Application/Services/GetServicesQuery.cs
- Create: src/VoltaMarcada.Application/Services/ServiceOperationResult.cs
- Create: src/VoltaMarcada.Application/Services/CreateServiceUseCase.cs
- Create: src/VoltaMarcada.Application/Services/GetServicesUseCase.cs
- Create: src/VoltaMarcada.Application/Services/GetServiceForEditUseCase.cs
- Create: src/VoltaMarcada.Application/Services/UpdateServiceUseCase.cs
- Create: src/VoltaMarcada.Application/Services/DeleteServiceUseCase.cs
- Create: src/VoltaMarcada.Application/Abstractions/Persistence/IServiceRepository.cs
- Modify: src/VoltaMarcada.Application/DependencyInjection.cs

**Interfaces:**

- Consumes: the Task 1 domain factories/behavior.
- Produces: create/update command records with Name, Description, DurationMinutes, PriceType, BasePrice, AllowOnlineBooking, RequireBeforePhotos, RequireAfterPhotos, and IReadOnlyList<ServiceChecklistItemInput> ChecklistItems.
- Produces: ServiceOperationResultCode { Success, InvalidInput, NotFound }.
- Produces: IServiceRepository.GetForCompanyAsync(Guid serviceId, Guid companyId, CancellationToken), GetAllForCompanyAsync(Guid companyId, GetServicesQuery query, CancellationToken), AddAsync(Service service, CancellationToken), Remove(Service service), and SaveChangesAsync(CancellationToken).

- [ ] **Step 1: Create application contracts**

Use immutable record contracts and a shared service-command interface in the Equipment validator style. ServiceDto must include every form value and ordered checklist item with its IsRequired state. GetServicesQuery must normalize blank searches to null.

- [ ] **Step 2: Write shared FluentValidation rules**

Validate the name and description limits, positive integer duration, valid enum, cross-field price rule, checklist count, and nonblank/max-length item descriptions. The validator must reject a value sent with QuoteRequired or Hidden; browser conditional visibility is not validation.

- [ ] **Step 3: Implement create and query behavior**

The create use case validates, resolves CompanyId through ICurrentCompany, turns each submitted checklist input into a domain item in its array order, creates the aggregate with TimeProvider.GetUtcNow(), saves it, and returns Success(id). The list use case scopes with the resolved company and maps repository results to DTOs.

- [ ] **Step 4: Implement edit, update, and delete behavior**

All identifier actions first call GetForCompanyAsync(serviceId, companyId, cancellationToken). Edit returns null when absent. Update validates then supplies replacement domain checklist items to Service.Update and saves. Delete removes the scoped aggregate and saves. Return NotFound for no scoped match.

- [ ] **Step 5: Register use cases**

Register the five use cases as scoped in AddApplication without disturbing existing registrations.

- [ ] **Step 6: Static-review application boundaries**

Trace every command field to its validator and domain call. Confirm CompanyId never appears on a command, all I/O accepts and forwards cancellation tokens, and TimeProvider is the only time source.

- [ ] **Step 7: Commit**

~~~powershell
git add src/VoltaMarcada.Application/Services src/VoltaMarcada.Application/Abstractions/Persistence/IServiceRepository.cs src/VoltaMarcada.Application/DependencyInjection.cs
git commit -m "feat: add service management use cases"
~~~

### Task 3: Persist Services and Checklist Items

**Files:**

- Create: src/VoltaMarcada.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Configurations/ServiceChecklistItemConfiguration.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Repositories/ServiceRepository.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs
- Modify: src/VoltaMarcada.Infrastructure/DependencyInjection.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260820130000_AddServices.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260820130000_AddServices.Designer.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs

**Interfaces:**

- Consumes: IServiceRepository, Service, and ServiceChecklistItem.
- Produces: the Services and ServiceChecklistItems schema and an IServiceRepository implementation.

- [ ] **Step 1: Add DbSets and configurations**

Expose DbSet<Service> Services and DbSet<ServiceChecklistItem> ServiceChecklistItems. Configure name as required/max 200, description optional/max 2,000, decimal(18,2) BasePrice, timestamps, booleans, and enum. Add index (CompanyId, Name), company restriction delete behavior, checklist description/order mapping, index (ServiceId, DisplayOrder), and cascade deletion from service to its children only.

- [ ] **Step 2: Implement the EF repository**

Use AsNoTracking for list queries and Include(service => service.ChecklistItems) for scoped editing loads. Filter every query using CompanyId; the identifier lookup must combine Id and CompanyId in one predicate. Search with escaped PostgreSQL ILike against name and description, and return rows ordered by service name.

- [ ] **Step 3: Register the repository**

Register IServiceRepository to ServiceRepository as scoped next to the existing repositories.

- [ ] **Step 4: Generate and inspect the migration**

Generate the descriptive AddServices migration using the existing EF workflow. Confirm it creates both tables, decimal precision, column constraints, company FK, checklist cascade FK, indexes, designer, and updated model snapshot.

- [ ] **Step 5: Static-review persistence**

Compare configurations, migration, and snapshot. Confirm child ordering is retained, no FK can cross tenant boundaries, and list/single queries include tenant predicates.

- [ ] **Step 6: Commit**

~~~powershell
git add src/VoltaMarcada.Infrastructure/Persistence src/VoltaMarcada.Infrastructure/DependencyInjection.cs
git commit -m "feat: persist services and checklist items"
~~~

### Task 4: Expose Authenticated Service Endpoints

**Files:**

- Create: src/VoltaMarcada.Web/Endpoints/ServiceEndpoints.cs
- Modify: src/VoltaMarcada.Web/Program.cs

**Interfaces:**

- Consumes: all five service use cases and service command records.
- Produces: protected routes POST /services/, GET /services/?search=, GET /services/{serviceId}/edit, PUT /services/{serviceId}, and DELETE /services/{serviceId}.

- [ ] **Step 1: Define request contracts and mappings**

Add nested ServiceRequest and ServiceChecklistItemRequest records that mirror command fields but never contain CompanyId. Convert the submitted checklist array to the corresponding application input list without losing order or IsRequired.

- [ ] **Step 2: Map protected CRUD endpoints**

Create a /services group requiring AuthorizationPolicies.CompanyMember. Mirror Equipment endpoint status behavior: 201 with edit URL on creation, 200 for list/edit/update, 204 for delete, 400 for invalid input, and 404 for an absent scoped record. User-safe error messages are Portuguese.

- [ ] **Step 3: Register endpoint mapping**

Call app.MapServiceEndpoints() with the existing endpoint mappings in Program.cs.

- [ ] **Step 4: Static-review HTTP security**

Confirm authorization is attached to the group, CompanyId is absent from all public request/route values, persistence entities never cross the endpoint boundary, and no endpoint bypasses use cases.

- [ ] **Step 5: Commit**

~~~powershell
git add src/VoltaMarcada.Web/Endpoints/ServiceEndpoints.cs src/VoltaMarcada.Web/Program.cs
git commit -m "feat: add service endpoints"
~~~

### Task 5: Add Management Pages and the Menu Item

**Files:**

- Create: src/VoltaMarcada.Web/Components/Pages/Services.razor
- Create: src/VoltaMarcada.Web/Components/Pages/ServiceRegistration.razor
- Modify: src/VoltaMarcada.Web/Components/Layout/MainLayout.razor
- Modify: src/VoltaMarcada.Web/wwwroot/app.css

**Interfaces:**

- Consumes: service use cases, ServiceDto, ServiceOperationResultCode, ServicePriceType, PageHeader, EmptyState, GridTextCell, and MudBlazor controls.
- Produces: /servicos and /servicos/registro?id={serviceId} management flows and the Serviços navigation item.

- [ ] **Step 1: Add the navigation entry**

Add Serviços beside Clientes and Equipamentos in the authenticated MudBlazor drawer, linking to /servicos with an appropriate service icon.

- [ ] **Step 2: Implement the list page**

Follow the Equipment page’s loading, error, empty-state, search, delete-dialog, snackbar, and editing patterns. Show name, duration as N min, a price label derived from enum/value, online-request status with explicit text, and edit/delete actions. Use GridTextCell for longer values.

- [ ] **Step 3: Implement form shell and core fields**

Follow the separate create/edit screen pattern with PageHeader, cancel/save actions, MudForm, query-string ID loading, validation, snackbar, and redirect. Use outlined MudTextField fields for name/description, MudNumericField<int> labelled Duração aproximada (min) with minimum 1, and MudSelect<ServicePriceType> with Portuguese display labels.

- [ ] **Step 4: Implement price and operational rules controls**

Render the monetary MudNumericField<decimal?> only for Fixed and StartingAt and make it required then. On changing to QuoteRequired or Hidden, clear BasePrice in the form model. Add independent MudSwitch controls for Permitir solicitação online, Exigir fotos antes do atendimento, and Exigir fotos depois do atendimento.

- [ ] **Step 5: Implement checklist interaction**

Add a draft text MudTextField, Obrigatório MudCheckBox, and Adicionar item MudButton. The add handler must prevent blank items with visual feedback, append an in-memory row retaining its own required flag, reset draft controls, and render each row with required/optional text and a labelled remove MudIconButton. Map the remaining collection to ServiceChecklistItemInput at save and restore all items from the edit DTO.

- [ ] **Step 6: Add scoped styling and static UI review**

Add only necessary styles for consistent form spacing, checklist row layout, toolbar wrapping, and mobile stacking. Check every control is MudBlazor, form actions disable while saving, accessible labels are present, and Portuguese copy correctly explains duration, price, checklist, and photo rules.

- [ ] **Step 7: Commit**

~~~powershell
git add src/VoltaMarcada.Web/Components/Pages/Services.razor src/VoltaMarcada.Web/Components/Pages/ServiceRegistration.razor src/VoltaMarcada.Web/Components/Layout/MainLayout.razor src/VoltaMarcada.Web/wwwroot/app.css
git commit -m "feat: add services management pages"
~~~

### Task 6: Final Static Verification

**Files:**

- Modify if required: only files from Tasks 1–5.

**Interfaces:**

- Consumes: completed domain, application, infrastructure, endpoints, and pages.
- Produces: a coherent implementation satisfying the approved design without executing builds, tests, or the application.

- [ ] **Step 1: Inspect final change scope**

Run git diff --check and git status --short. Preserve all pre-existing user changes and confirm only intended Services files plus composition/navigation/style/migration files changed.

- [ ] **Step 2: Trace business rules end to end**

For each price type, inspect UI behavior, request mapping, FluentValidation, and domain guards. Verify duration is positive int and each ServiceChecklistItem.IsRequired independently reaches persistence.

- [ ] **Step 3: Trace tenant isolation end to end**

Inspect use cases, repository predicates, endpoint authorization, and component authorization. Confirm every authenticated operation resolves current company server-side and all scoped identifier lookups include CompanyId.

- [ ] **Step 4: Verify mapping consistency**

Compare EF configuration with migration and model snapshot: tables, nullability, decimal precision, indexes, company restriction, and checklist cascade must match.

- [ ] **Step 5: Report verification accurately**

Report the static checks performed and clearly note that builds, test runs, and application launch were intentionally not run because project instructions prohibit them unless the user explicitly requests them.
