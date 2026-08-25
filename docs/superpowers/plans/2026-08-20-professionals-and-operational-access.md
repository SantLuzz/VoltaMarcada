# Professionals and Operational Access Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add owner-managed Professionals linked to CompanyMembers and convert Customer, Equipment, and Service management to logical deactivation with read-only Employee access.

**Architecture:** Follow the existing explicit use-case/repository/MudBlazor patterns. Professional owns a mandatory, unique CompanyUser link and a many-to-many ProfessionalService link; status changes mutate `IsActive` rather than deleting rows. Owner-only mutations are enforced in application operations and endpoints, while Employee pages expose only tenant-scoped read models.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor Web App, MudBlazor, EF Core, PostgreSQL, ASP.NET Core Identity.

**Spec:** `docs/superpowers/specs/2026-08-20-professionals-and-operational-access-design.md`

## Global Constraints

- All source identifiers, comments, logs, migration names, and validation messages remain in English; Brazilian Portuguese is used only for UI copy.
- Company is the tenant boundary. Every query and mutation filters by the authenticated current `CompanyId`.
- Employee cannot create, update, deactivate, reactivate, or delete Customer, Equipment, Service, or Professional records.
- No physical deletion is exposed for Customer, Equipment, Service, or Professional; status changes use `IsActive`.
- Do not run `dotnet build`, `dotnet test`, `dotnet run`, or start the application unless separately requested. Use static review and targeted non-executing checks.
- Do not introduce new frameworks, ORMs, messaging systems, or generic repositories.

---

### Task 1: Add domain status and Professional aggregates

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/Professional.cs`
- Create: `src/VoltaMarcada.Domain/Entities/ProfessionalService.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Customer.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Equipment.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Service.cs`

**Interfaces:**
- Produces `Professional.Create(companyId, companyUserId, serviceIds, now)`, `Professional.Update(companyUserId, serviceIds, now)`, `Professional.Activate(now)`, and `Professional.Deactivate(now)`.
- Produces `ProfessionalService.Create(companyId, professionalId, serviceId)` and a read-only relationship collection.
- Produces `Customer.IsActive`, `Equipment.IsActive`, and behavior methods `Activate`/`Deactivate`; retain Service's existing `IsActive` and add the same behavior methods.

- [ ] **Step 1: Define Professional invariants**

  Require non-empty CompanyId and CompanyUserId, at least one distinct service ID, active status on creation, UTC timestamps, and no public setters. Use `DomainException` for invalid state.

- [ ] **Step 2: Define the ProfessionalService link**

  Store CompanyId, ProfessionalId, and ServiceId with a server-generated Guid. Keep the relationship company-scoped so cross-tenant links cannot be represented validly.

- [ ] **Step 3: Add logical status to existing aggregates**

  Add `IsActive` defaulting to `true` for Customer and Equipment. Add `Activate`/`Deactivate` methods to Customer, Equipment, and Service, updating `UpdatedAtUtc` with the supplied `DateTimeOffset`.

- [ ] **Step 4: Static review**

  Verify all constructors remain private, timestamps are normalized to UTC, and no domain method accepts a browser-provided CompanyId as an authorization decision.

### Task 2: Map persistence and create the migration

**Files:**
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/ProfessionalConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/ProfessionalServiceConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/EquipmentConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/ServiceConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260820_AddProfessionalsAndLogicalDeactivation.cs`

**Interfaces:**
- Produces `AppDbContext.Professionals` and `AppDbContext.ProfessionalServices`.
- Produces a migration that creates `Professionals`, `ProfessionalServices`, `Customers.IsActive`, and `Equipment.IsActive`, with existing rows active.

- [ ] **Step 1: Add DbSets and Fluent mappings**

  Map required fields and UTC timestamps. Add a unique index on `(CompanyId, CompanyUserId)`, indexes on `(CompanyId, IsActive)` and `(CompanyId, ServiceId)`, and restrictive tenant foreign keys. Map ProfessionalService foreign keys to the same-company alternate keys where needed.

- [ ] **Step 2: Preserve dependent data**

  Configure professional-service rows to be retained only while their Professional exists, using cascade from Professional to links; keep Company, CompanyUser, and Service foreign keys restrictive so a parent cannot be physically deleted accidentally.

- [ ] **Step 3: Write the migration**

  Add descriptive English migration operations. Add `IsActive` columns with `defaultValue: true`; create Professional tables and constraints without dropping existing data.

- [ ] **Step 4: Static migration review**

  Check that the migration snapshot intent matches the Fluent configuration and that all new foreign keys include company isolation where a relationship crosses tenant-owned data.

### Task 3: Add repository contracts and tenant-safe implementations

**Files:**
- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/IProfessionalRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ProfessionalRepository.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/ICustomerRepository.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IEquipmentRepository.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IServiceRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CustomerRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/EquipmentRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ServiceRepository.cs`

**Interfaces:**
- `IProfessionalRepository` exposes `GetAllForCompanyAsync`, `GetForCompanyAsync`, `GetForCurrentMemberAsync`, `GetAvailableCompanyMembersAsync`, `GetAvailableServicesAsync`, `AddAsync`, `ReplaceServices`, `SaveChangesAsync`, and `ExistsForCompanyMemberAsync`.
- Existing repositories expose `SetActiveAsync` or equivalent load-and-behavior operations and no longer expose a physical `Remove` operation to application callers.

- [ ] **Step 1: Implement Professional queries**

  Load member display data and service summaries with `CompanyId` predicates. `GetForCurrentMemberAsync` must filter both `CompanyUserId` and `CompanyId`.

- [ ] **Step 2: Implement available selector queries**

  Return only accepted/current-company members not already linked to another professional and only active/current-company services. Never accept a CompanyId from a query or form model.

- [ ] **Step 3: Replace delete repository calls**

  Remove physical-delete use from Customer, Equipment, and Service operations. Keep addresses/checklist relationships intact and introduce active-status-aware list/query helpers.

- [ ] **Step 4: Static tenant-isolation review**

  Inspect every new and modified predicate for both entity ID and current CompanyId, including relationship joins and selector lists.

### Task 4: Implement Professional use cases and status operations

**Files:**
- Create: `src/VoltaMarcada.Application/Professionals/ProfessionalDto.cs`
- Create: `src/VoltaMarcada.Application/Professionals/ProfessionalCommands.cs`
- Create: `src/VoltaMarcada.Application/Professionals/ProfessionalOperationResult.cs`
- Create: `src/VoltaMarcada.Application/Professionals/ProfessionalCommandValidator.cs`
- Create: `src/VoltaMarcada.Application/Professionals/CreateProfessionalUseCase.cs`
- Create: `src/VoltaMarcada.Application/Professionals/GetProfessionalsUseCase.cs`
- Create: `src/VoltaMarcada.Application/Professionals/GetProfessionalForEditUseCase.cs`
- Create: `src/VoltaMarcada.Application/Professionals/GetCurrentEmployeeProfessionalUseCase.cs`
- Create: `src/VoltaMarcada.Application/Professionals/UpdateProfessionalUseCase.cs`
- Create: `src/VoltaMarcada.Application/Professionals/ChangeProfessionalStatusUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Customers/DeleteCustomerUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Equipment/DeleteEquipmentUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Services/DeleteServiceUseCase.cs`

**Interfaces:**
- `CreateProfessionalCommand(Guid CompanyUserId, IReadOnlyCollection<Guid> ServiceIds)`.
- `UpdateProfessionalCommand(Guid ProfessionalId, Guid CompanyUserId, IReadOnlyCollection<Guid> ServiceIds)`.
- `ChangeProfessionalStatusCommand(Guid ProfessionalId, bool IsActive)`.
- All mutation use cases return an explicit operation result with `Success`, `NotFound`, `InvalidInput`, `DuplicateCompanyMember`, `InvalidCompanyMember`, `InvalidService`, and `Forbidden` outcomes.

- [ ] **Step 1: Validate Professional commands**

  Reject empty member/service IDs, duplicate service IDs, members outside the current company, already-linked members, inactive services, and records from another company.

- [ ] **Step 2: Implement create/update**

  Resolve the company through `ICurrentCompany`, authorize Owner, load all links using tenant-safe repository methods, call domain behavior, replace service links, and save once.

- [ ] **Step 3: Implement current Employee read use case**

  Resolve the authenticated user and current company. Return the linked professional or an explicit `NotLinked` result; never return another member's Professional.

- [ ] **Step 4: Convert existing delete use cases**

  Keep the existing public operation entry point only as a compatibility wrapper if required by callers, but make it call a status-change operation and return success without removing rows. Add Owner authorization and retain `HasLinkedEquipment` only if the UI still needs to explain legacy behavior; otherwise replace it with status success.

### Task 5: Enforce Owner-only mutation in endpoints and authorization

**Files:**
- Modify: `src/VoltaMarcada.Web/Authorization/AuthorizationPolicies.cs`
- Modify: `src/VoltaMarcada.Web/Authorization/CompanyMemberAuthorizationHandler.cs`
- Create or modify: `src/VoltaMarcada.Web/Authorization/CompanyOwnerAuthorizationHandler.cs`
- Modify: `src/VoltaMarcada.Web/Endpoints/CustomerEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Endpoints/EquipmentEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Endpoints/ServiceEndpoints.cs`
- Create: `src/VoltaMarcada.Web/Endpoints/ProfessionalEndpoints.cs`

**Interfaces:**
- Produces an Owner policy usable by all mutating endpoint handlers.
- Professional endpoints expose tenant-safe read/list, create, update, and status-change contracts without persistence models or CompanyId request fields.

- [ ] **Step 1: Add Owner policy**

  Register a policy that requires authenticated membership with `CompanyUserRole.Owner`; preserve the existing CompanyMember policy for read pages.

- [ ] **Step 2: Protect existing mutations**

  Require Owner policy on Customer, Equipment, and Service create/update/status endpoints. Keep list/detail endpoints available to CompanyMember users.

- [ ] **Step 3: Add Professional endpoints**

  Map list/detail routes for members and Owner-only mutation routes. Translate explicit application results to the established HTTP status conventions without exposing exception details.

- [ ] **Step 4: Static endpoint review**

  Confirm no mutation route trusts a hidden `CompanyId`, route company slug, or browser role field, and that Employee direct requests receive authorization failure.

### Task 6: Build Professional management and Employee read-only pages

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Pages/Professionals.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/ProfessionalRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`
- Modify: `src/VoltaMarcada.Web/Components/_Imports.razor` if shared imports are required

**Interfaces:**
- Owner list uses `GetProfessionalsUseCase`; form uses create/update/status use cases; Employee mode uses `GetCurrentEmployeeProfessionalUseCase`.
- Multi-select binds `HashSet<Guid>` or equivalent to active Service IDs and submits a distinct read-only collection.

- [ ] **Step 1: Add menu and role-aware route behavior**

  Add “Profissionais” to the authenticated menu. Resolve Owner to the management list and Employee to the current-member profile route/state.

- [ ] **Step 2: Build Owner list**

  Follow `Services.razor` loading/error/empty patterns. Render linked employee, service summary, status, edit, and activate/deactivate actions. Use confirmation dialogs for status changes.

- [ ] **Step 3: Build Owner form**

  Add a required employee `MudSelect` and a `MudSelect` with `MultiSelection="true"` for active services. Disable employee selection while editing if changing the linked member is not supported by the current command; otherwise submit the explicit CompanyUserId. Add status control and server feedback.

- [ ] **Step 4: Build Employee read-only state**

  Render the linked Professional data with disabled controls and no submit/action buttons. For `NotLinked`, render a clear Portuguese message and no management links.

- [ ] **Step 5: Static UI review**

  Verify no Employee-only path renders create/edit/delete/status actions and that the multi-select cannot submit service IDs that were not loaded from the current company.

### Task 7: Convert Customer, Equipment, and Service UI to status management and read-only Employee views

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/Customers.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/CustomerRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Equipment.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/EquipmentRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Services.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/ServiceRegistration.razor`

**Interfaces:**
- Existing list pages display active status and invoke Owner-only activate/deactivate operations.
- Employee list pages retain read access but omit mutation buttons; registration pages are either unreachable through the UI or render loaded data disabled with no save action.

- [ ] **Step 1: Add status to DTOs and list rows**

  Include `IsActive` in Customer, Equipment, and Service read DTOs and render Portuguese status labels/chips.

- [ ] **Step 2: Replace delete dialogs**

  Replace “Excluir” copy and handlers with “Inativar”/“Reativar” status dialogs. Keep all records queryable for Owner management and filter only operational selector queries to active records.

- [ ] **Step 3: Add Employee read-only gating**

  Use the authenticated membership role to hide create/edit/status actions and disable form controls. Ensure direct form requests cannot invoke a save handler because the endpoint/application use case also requires Owner.

- [ ] **Step 4: Static UI/flow review**

  Trace each list action, form submit, and direct URL for Owner and Employee and verify the resulting operation is either allowed or rejected server-side.

### Task 8: Register dependencies and perform final static verification

**Files:**
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

- [ ] **Step 1: Register repositories and use cases**

  Add scoped registrations matching the existing module conventions and ensure endpoint/page injection resolves without service-locator code.

- [ ] **Step 2: Register authorization policy and endpoint mappings**

  Wire Owner policy and Professional endpoints in the same composition root used by existing endpoints.

- [ ] **Step 3: Run non-executing checks**

  Use `git diff --check`, `rg` scans for remaining physical delete calls and Employee mutation links, and inspect the migration/model snapshot diff. Do not run a build or application host.

- [ ] **Step 4: Review against the specification**

  Confirm every spec section is represented: mandatory CompanyMember link, unique relationship, multi-select active services, logical deactivation, retained dependents, Owner-only mutation, Employee direct Professional route, unlinked Employee state, and tenant-safe server checks.

