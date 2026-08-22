# Scheduling and Technician Experience Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Phase 4 appointments, conflict-safe scheduling, approved technician blocks, and role-specific operational dashboards.

**Architecture:** Add company-scoped scheduling aggregates in Domain and explicit scheduling use cases in Application. EF Core repositories enforce tenant and professional access predicates; Blazor components only render results and submit commands. A typed Web configuration provides the slot cadence and allowed IANA zones.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core, PostgreSQL, MudBlazor.

**Spec:** `docs/superpowers/specs/2026-08-22-scheduling-and-technician-experience-design.md`

## Global Constraints

- Keep all source identifiers, migrations, comments, and technical documentation in English.
- Every tenant-owned query and mutation filters by `CompanyId`; authenticated requests never accept a tenant ID as authority.
- Store appointment timestamps in UTC and convert local date/time using the assigned professional's current IANA zone.
- The only initially valid professional zones are `America/Sao_Paulo` and `America/Campo_Grande`; configured default is `America/Sao_Paulo`.
- Use `TimeProvider`; do not call direct clocks in Domain or Application.
- Do not add third-party dependencies, automated tests, builds, test runs, or application hosts unless the user explicitly requests them.

---

## File Structure

- `src/ServiceSync.Domain/Entities/Appointment*.cs`: appointment, reschedule history, and block-request state/invariants.
- `src/ServiceSync.Domain/Enums/Appointment*.cs`: appointment and block-request statuses.
- `src/ServiceSync.Application/Scheduling/*.cs`: DTOs, commands, operation result, authorization, slot calculator, and use cases.
- `src/ServiceSync.Application/Abstractions/Persistence/IAppointmentRepository.cs`: company/access-scoped scheduling persistence contract.
- `src/ServiceSync.Infrastructure/Persistence/Configurations/*Appointment*.cs`: EF mappings and indexes.
- `src/ServiceSync.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`: EF scheduling queries, transaction boundary, and dashboard projections.
- `src/ServiceSync.Web/Configuration/SchedulingOptions.cs`: validated typed configuration model.
- `src/ServiceSync.Web/Components/Pages/Schedule.razor` and `Dashboard.razor`: owner and technician scheduling/dashboard experience.
- `src/ServiceSync.Web/Components/Shared/WorkOrderAppointmentsPanel.razor`: work-order `Agendamentos` tab.

### Task 1: Add scheduling configuration and professional time zone

**Files:**
- Create: `src/ServiceSync.Web/Configuration/SchedulingOptions.cs`
- Modify: `src/ServiceSync.Web/appsettings.json`, `src/ServiceSync.Web/Program.cs`
- Modify: `src/ServiceSync.Domain/Entities/Professional.cs`, `src/ServiceSync.Application/Professionals/ProfessionalCommands.cs`, `ProfessionalDtos.cs`, `ProfessionalCommandValidator.cs`, `CreateProfessionalUseCase.cs`, `UpdateProfessionalUseCase.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Configurations/ProfessionalConfiguration.cs`, `src/ServiceSync.Web/Components/Pages/ProfessionalRegistration.razor`

**Produces:** `SchedulingOptions`, `Professional.TimeZoneId`, and owner-managed professional time-zone selection.

- [ ] Add `SchedulingOptions` with `SlotIntervalMinutes`, `DefaultTimeZoneId`, and `AllowedTimeZoneIds`; bind and validate it at startup.
- [ ] Add `TimeZoneId` to `Professional.Create` and `Professional.Update`, defaulting blank input to `America/Sao_Paulo` in the Application commands and rejecting zones outside options.
- [ ] Include `TimeZoneId` in professional DTOs and render the allowed-zone selector in the professional form.
- [ ] Map the required persisted property and prepare the migration default for existing rows.

### Task 2: Model appointments, histories, and technician block requests

**Files:**
- Create: `src/ServiceSync.Domain/Entities/Appointment.cs`, `AppointmentRescheduleHistory.cs`, `ScheduleBlockRequest.cs`
- Create: `src/ServiceSync.Domain/Enums/AppointmentStatus.cs`, `ScheduleBlockRequestStatus.cs`
- Modify: `src/ServiceSync.Domain/Entities/WorkOrder.cs`

**Produces:** guarded aggregate methods `Create`, `Reschedule`, `Reassign`, `Complete`, `Cancel`, `Approve`, and `Reject`.

- [ ] Add `Appointment` with required company/professional UTC interval and optional customer, equipment, service, and work-order IDs. Reject invalid intervals and invalid state transitions.
- [ ] Add immutable `AppointmentRescheduleHistory` records from `Appointment.Reschedule`.
- [ ] Add `ScheduleBlockRequest` with requested interval, type, notes, status, decision data, and transitions for pending, conflict resolution, approval, and rejection.
- [ ] Extend work-order completion behavior to reject completion when Application reports unfinished linked appointments, while preserving work orders with none.

### Task 3: Persist scheduling data and migrate the schema

**Files:**
- Modify: `src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Configurations/AppointmentConfiguration.cs`, `AppointmentRescheduleHistoryConfiguration.cs`, `ScheduleBlockRequestConfiguration.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`
- Create: `src/ServiceSync.Application/Abstractions/Persistence/IAppointmentRepository.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/AddSchedulingAndBlockRequests.cs` and designer; update model snapshot

**Produces:** EF Core data model and access-scoped scheduling repository.

- [ ] Add DbSets, composite company foreign keys, and indexes for professional timeline queries, work-order appointments, reschedule history, and block-request owner review.
- [ ] Implement repository projections for date-range appointments, work-order appointments, conflicts, owner dashboard, technician dashboard, and block-request review.
- [ ] Implement `HasOpenAppointmentsAsync(workOrderId, companyId)` and call it from work-order completion before the aggregate state transition.
- [ ] Add a descriptive migration that creates all scheduling tables and backfills the professional time zone with `America/Sao_Paulo`.

### Task 4: Implement scheduling application behavior

**Files:**
- Create: `src/ServiceSync.Application/Scheduling/SchedulingCommands.cs`, `SchedulingDtos.cs`, `SchedulingOperationResult.cs`, `SchedulingAuthorization.cs`, `GetAvailableAppointmentSlotsUseCase.cs`
- Create: `CreateAppointmentUseCase.cs`, `GetAppointmentsUseCase.cs`, `GetWorkOrderAppointmentsUseCase.cs`, `RescheduleAppointmentUseCase.cs`, `ReassignAppointmentUseCase.cs`, `CompleteAppointmentUseCase.cs`, `CancelAppointmentUseCase.cs`
- Create: `CreateScheduleBlockRequestUseCase.cs`, `GetScheduleBlockRequestsUseCase.cs`, `ApproveScheduleBlockRequestUseCase.cs`, `RejectScheduleBlockRequestUseCase.cs`, `GetSchedulingDashboardUseCase.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`, `src/ServiceSync.Application/WorkOrders/CompleteWorkOrderUseCase.cs`, `WorkOrderOperationResult.cs`

**Produces:** explicit use cases that return business result codes instead of expected-flow exceptions.

- [ ] Resolve owner/technician access scopes from current user and company. For employees, obtain the active professional profile and constrain every repository call with it.
- [ ] Build slots from daily schedule minus lunch, effective blocks, and scheduled appointment intervals, emitting only starts aligned to `SlotIntervalMinutes` whose complete duration fits.
- [ ] Create owner appointments with an explicit duration or selected service duration. Create technician appointments only for an assigned work order and copy the work order's links.
- [ ] Revalidate conflicts on create, reschedule, reassign, request approval, and appointment completion. Map conflicts, invalid intervals, inaccessible records, and unfinished appointments to explicit result codes.
- [ ] Implement block-request conflict detection. Approval atomically checks conflicts again, creates the effective `ScheduleBlock`, and records the owner decision; an unresolved request remains actionable rather than moving appointments.

### Task 5: Expose scheduling HTTP operations and work-order integration

**Files:**
- Create: `src/ServiceSync.Web/Endpoints/AppointmentEndpoints.cs`, `ScheduleBlockRequestEndpoints.cs`
- Modify: `src/ServiceSync.Web/Program.cs`, `src/ServiceSync.Web/Endpoints/WorkOrderEndpoints.cs`
- Modify: `src/ServiceSync.Web/Components/Pages/WorkOrderRegistration.razor`
- Create: `src/ServiceSync.Web/Components/Shared/WorkOrderAppointmentsPanel.razor`

**Produces:** authenticated endpoint contracts and the `Agendamentos` work-order tab.

- [ ] Map protected endpoints for range lookup, slots, creation, reschedule, reassignment, completion, cancellation, block-request creation, review, approval, and rejection; request contracts contain no `CompanyId`.
- [ ] Translate application result codes to suitable HTTP results without leaking internal exceptions.
- [ ] Add the work-order `Agendamentos` tab showing linked appointments in current-professional local time, statuses, history, and open-appointment completion warning.
- [ ] Render owner actions for add/reschedule/reassign/cancel; render constrained technician add and own-completion actions only when the work-order assigned professional matches access scope.

### Task 6: Deliver role-specific dashboard and calendar UX

**Files:**
- Modify: `src/ServiceSync.Web/Components/Pages/Dashboard.razor`, `src/ServiceSync.Web/Components/Layout/MainLayout.razor`, `src/ServiceSync.Web/wwwroot/app.css`
- Create: `src/ServiceSync.Web/Components/Pages/Schedule.razor`, `src/ServiceSync.Web/Components/Shared/AppointmentEditor.razor`, `DateRangeNavigator.razor`, `ScheduleBlockRequestPanel.razor`

**Produces:** owner administration and a distinct mobile-first technician dashboard.

- [ ] Replace the empty dashboard with role-specific content. Owner sees date-range appointments, work-order activity, counts, and pending/conflicting block requests.
- [ ] Provide owner schedule range controls with current-week default, previous/next week navigation, arbitrary start/end date range, professional filter, and appointment creation entry point.
- [ ] Provide technician `Today`, `This week`, and arbitrary date-range controls; show only their appointments with next appointment highlighted and direct links to address, equipment, and work order.
- [ ] Add technician block-request form and owner review panel. Pending requests do not affect available slots; conflict-requiring requests name affected appointments.
- [ ] Add an Agenda navigation entry for company members and apply responsive styles that prioritize the technician's next action on small screens.

### Task 7: Static verification and documentation status

**Files:**
- Modify: `ServiceSync_Plano_Negocio_Desenvolvimento_v2.md`, `Planos negócio/ServiceSync_Plano_Negocio_Desenvolvimento_v2.md`, `Planos negócio/plano_de_negocio.md`

**Produces:** reviewed Phase 4 delivery and updated roadmap status.

- [ ] Inspect every changed route, dependency registration, migration, and Razor injection for unresolved names or missing use-case wiring.
- [ ] Run `git diff --check` and inspect diffs for direct clocks, tenant IDs accepted from requests, unfiltered tenant queries, and accidental unrelated changes.
- [ ] Mark Phase 4 as completed in all maintained roadmap copies only after the preceding static review finds no outstanding wiring issue.
