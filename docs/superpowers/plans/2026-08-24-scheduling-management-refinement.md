# Scheduling Management Refinement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make appointment and schedule-block management more informative and actionable from the Agenda and work-order screens.

**Architecture:** Reuse one appointment creation editor and one appointment-details dialog across all entry points. Keep all state changes in explicit application use cases, and enrich list contracts using company-scoped repository queries.

**Tech Stack:** .NET 10, C#, Blazor Web App, MudBlazor, EF Core, PostgreSQL.

**Spec:** `docs/superpowers/specs/2026-08-22-scheduling-and-technician-experience-design.md`, supplemented by the user-approved agenda-management refinements from 2026-08-24.

## Global Constraints

- Source code and technical documentation remain in English; UI text is Brazilian Portuguese.
- Authenticated operations resolve company scope from the current user and never accept `CompanyId` from UI state.
- Do not run builds, automated tests, app hosts, or database migrations unless explicitly requested.
- Do not add test projects or test files because the project requires tests only when explicitly requested.
- Use `TimeProvider` for current time and preserve existing UTC scheduling behavior.
- Use MudBlazor controls for interactive UI.

---

### Task 1: Correct block-request rejection and include requester names

**Files:**
- Modify: `src/ServiceSync.Domain/Entities/ScheduleBlockRequest.cs`
- Modify: `src/ServiceSync.Application/Abstractions/Persistence/IProfessionalRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Repositories/ProfessionalRepository.cs`
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentChangeUseCases.cs`
- Modify: `src/ServiceSync.Web/Components/Shared/ScheduleBlockRequestPanel.razor`

**Consumes:** `ScheduleBlockRequestStatus.Pending`, `ScheduleBlockRequestStatus.RequiresAppointmentResolution`, and company-scoped professional identifiers.

**Produces:** `ScheduleBlockRequestDto.ProfessionalName` and a valid rejection transition for every request state offered by the owner UI.

- [ ] **Step 1: Align the domain decision invariant with the application access rule.**

Allow `ScheduleBlockRequest.Reject` to decide both pending and resolution-required requests; rejected and approved requests remain immutable.

- [ ] **Step 2: Add a company-scoped batched professional-name lookup.**

Add `GetDisplayNamesAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken)` to `IProfessionalRepository` and return an ID/name dictionary from the tenant-filtered join to company members and users.

- [ ] **Step 3: Enrich block-request DTO mapping.**

Request the distinct professional IDs once, map each request with its display name, and use an empty string only if a corresponding company-scoped professional cannot be resolved.

- [ ] **Step 4: Improve the block-request list feedback.**

Render `Solicitado por: {name}` below the date/status title and show an error snackbar when an approval or rejection result is not successful.

- [ ] **Step 5: Statically inspect every status transition.**

Confirm the owner UI, use case, and domain allow the same actionable states and that name lookup is restricted by `CompanyId`.

### Task 2: Reuse appointment details and creation UI

**Files:**
- Create: `src/ServiceSync.Web/Components/Shared/AppointmentDetailsDialog.razor`
- Create: `src/ServiceSync.Web/Components/Shared/AppointmentEditor.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Dashboard.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/WorkOrderAppointmentsPanel.razor`

**Consumes:** `GetAppointmentDetailsUseCase`, `CreateAppointmentUseCase`, `GetAvailableAppointmentSlotsUseCase`, work-order access scope, and current user permissions.

**Produces:** One details dialog and one creation editor reusable by the dashboard, Agenda, and a work-order appointment panel.

- [ ] **Step 1: Extract the dashboard appointment-details modal.**

Create a component with `AppointmentId`, `Visible`, and `VisibleChanged` parameters. Load the details only when the selected ID changes while visible, retain the current status/customer/notes display, and keep the link to its work order.

- [ ] **Step 2: Extract the Agenda creation modal.**

Move the existing Agenda form state and option-loading logic into `AppointmentEditor`. Preserve owner and technician access restrictions, slot revalidation on save, and current localized messages.

- [ ] **Step 3: Support a fixed work order.**

Add an optional `FixedWorkOrderId` parameter. When supplied, load that order through the access-scoped use case, derive customer/equipment/service/professional context from it, show the association as fixed, and prevent a user from selecting a different work order.

- [ ] **Step 4: Wire all consumers.**

Use the details dialog in Dashboard, Agenda, and the work-order panel. Use the editor in Agenda and add a `Novo agendamento` action to the work-order panel that opens it with the panel's work-order ID.

- [ ] **Step 5: Statically inspect event flow.**

Verify closing the editor resets its state, a successful creation reloads the originating list, and the work-order entry point cannot alter the fixed ID.

### Task 3: Enrich appointment list data and actions

**Files:**
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Abstractions/Persistence/IAppointmentRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`
- Modify: `src/ServiceSync.Application/Scheduling/GetWorkOrderAppointmentsUseCase.cs`
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/WorkOrderAppointmentsPanel.razor`

**Consumes:** appointment IDs, existing company-scoped access predicates, and appointment status transition use cases.

**Produces:** display-ready appointment rows with customer, equipment, service, professional, schedule, status, notes, and permitted actions.

- [ ] **Step 1: Add a company-scoped appointment display projection.**

Return appointment display values for range and work-order queries without N+1 lookups. Scope every join to `CompanyId`, and preserve access filtering by professional where applicable.

- [ ] **Step 2: Extend appointment list DTOs and mapping.**

Include customer, equipment, service, and professional display names in `AppointmentDto`; keep IDs for actions and links.

- [ ] **Step 3: Render detailed Agenda rows.**

Show the appointment date/time, status, customer, service, equipment, professional, and notes. Make the non-action part clickable to open `AppointmentDetailsDialog`. Preserve adjust, start, complete, and cancel actions according to role and status.

- [ ] **Step 4: Render detailed work-order appointment rows.**

Show the same relevant context in the OS tab, open details on selection, expose appropriate start, complete, cancel, and owner-only adjustment actions, and add the new fixed-work-order creation action.

- [ ] **Step 5: Statically inspect authorization and interaction behavior.**

Confirm employee views remain restricted to their professional appointments, destructive/status buttons remain owner or access-scope protected, and action-button clicks do not also open the details dialog.

### Task 4: Add focused appointment editing

**Files:**
- Modify: `src/ServiceSync.Domain/Entities/Appointment.cs`
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentChangeUseCases.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/WorkOrderAppointmentsPanel.razor`

**Consumes:** owner access, a scheduled appointment, and tenant-scoped linked customer/equipment/service records.

**Produces:** owner-only editing of appointment notes. Scheduling is intentionally handled by the existing `Ajustar` operation, which avoids accidental loss of reschedule history.

- [ ] **Step 1: Add an appointment notes update behavior.**

Permit updates only while the appointment is scheduled, normalize notes with the existing domain invariant, and update the UTC audit timestamp.

- [ ] **Step 2: Add the command and owner-only use case.**

Return `NotFound`, `Forbidden`, or `InvalidStatus` instead of expected-flow exceptions.

- [ ] **Step 3: Add an Edit dialog.**

Expose a compact MudBlazor dialog for notes in Agenda and the OS panel. Label it `Editar`; keep date, time, professional, and work-order association in `Ajustar`.

- [ ] **Step 4: Statically inspect the behavior boundary.**

Verify Edit cannot change scheduling fields or associations, while Ajustar continues to create reschedule history when time changes.

### Task 5: Static delivery verification

**Files:**
- Modify: `docs/superpowers/plans/2026-08-24-scheduling-management-refinement.md`

- [ ] **Step 1: Run formatting and reference checks permitted by the repository.**

Run `git diff --check`; search the changed application and UI files for direct clocks, unauthenticated tenant selection, missing registrations, and obsolete dialog markup.

- [ ] **Step 2: Review the changed-file diff.**

Match each requested behavior to a concrete route, use case, component, or display projection. Record any limitation caused by the no-build/no-test project constraint.
