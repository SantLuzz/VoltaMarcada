# Phase 4 Completion Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the approved Phase 4 gaps except for slot-list iteration, which remains unchanged by request.

**Architecture:** Keep appointment availability and tenant authorization in Application. Add persistence support only where the existing scheduling repository already owns it. Razor components render the new data and invoke explicit use cases; they do not calculate conflicts or authorize operations.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core, PostgreSQL, MudBlazor.

**Spec:** `docs/superpowers/specs/2026-08-22-scheduling-and-technician-experience-design.md`

## Global Constraints

- Do not modify `GetAvailableAppointmentSlotsUseCase` iteration behavior.
- Preserve company-scoped repository predicates and server-side authorization.
- Use `TimeProvider` for current timestamps in Domain and Application.
- Do not add tests or run builds, test runs, migrations, or application hosts unless explicitly requested.
- Do not introduce packages or projects.

---

### Task 1: Make block approval conflict-safe

**Files:**
- Modify: `src/ServiceSync.Application/Abstractions/Persistence/IAppointmentRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`
- Modify: `src/ServiceSync.Application/Scheduling/BlockRequestDecisionUseCases.cs`

**Consumes:** company-scoped appointment conflict queries and `ScheduleBlockRequest` state transitions.

**Produces:** one repository operation that rechecks conflicts and persists the block-request decision and effective block in a serializable transaction.

- [ ] Add an application-facing repository method that accepts the company, professional, requested UTC interval, request, effective block, and cancellation token.
- [ ] Implement it with a serializable EF Core transaction: query open appointment overlap again, mark the request as requiring resolution and save when one exists; otherwise approve the request, add the effective block, and save before committing.
- [ ] Replace the two-step approval workflow with the atomic repository method and map the conflict result to `SchedulingErrors.BlockRequestRequiresAppointmentResolution`.
- [ ] Statistically inspect that every query contains `CompanyId`, failure leaves the request actionable, and the success path commits both records.

### Task 2: Detect and expose block-request conflicts at creation

**Files:**
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentChangeUseCases.cs`
- Modify: `src/ServiceSync.Application/Abstractions/Persistence/IAppointmentRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`
- Modify: `src/ServiceSync.Web/Components/Shared/ScheduleBlockRequestPanel.razor`

**Consumes:** employee access scope, professional time zone, and appointment projections.

**Produces:** newly-created conflicted requests with `RequiresAppointmentResolution` plus an owner-visible affected-appointment summary; only pending requests show approval.

- [ ] Add a company/professional-scoped query for appointments that overlap an interval and a DTO field containing affected appointment summaries.
- [ ] During employee request creation, translate the requested local interval, query conflicts, and set `RequiresAppointmentResolution` before persistence when conflicts exist.
- [ ] Map affected appointment details for each resolution-required request without leaking records outside the company.
- [ ] Render the affected appointment summaries and hide the approval action while resolution is required; preserve rejection.
- [ ] Statistically inspect that conflicted requests are actionable after the conflicting appointments are adjusted and no browser identifier selects a company.

### Task 3: Support manual appointments without a service

**Files:**
- Modify: `src/ServiceSync.Web/Components/Shared/AppointmentEditor.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`

**Consumes:** `CreateAppointmentCommand.DurationMinutes` and existing slot lookup use case.

**Produces:** owner-only manual duration input used when no service is selected, while service duration remains authoritative when a service is selected.

- [ ] Add an owner-only numeric duration input to both creation entry points, available when no service is selected.
- [ ] Use selected service duration when a service exists; otherwise require a positive manual duration for loading slots and creating an appointment.
- [ ] Keep employee creation work-order-bound, using the work order service duration.
- [ ] Statistically inspect that the command still has no `CompanyId`, a manual appointment can omit `ServiceId`, and no fixed work-order field is editable.

### Task 4: Close planned owner and work-order UX gaps

**Files:**
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/WorkOrderAppointmentsPanel.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Dashboard.razor`
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/GetWorkOrderAppointmentsUseCase.cs`

**Consumes:** appointment list projection, reschedule history, owner scope, and dashboard data already supplied by scheduling use cases.

**Produces:** an owner professional filter, chronological reschedule history in the work-order panel, and owner operational appointment/work-order/block-request indicators.

- [ ] Add an owner professional filter to Agenda and constrain only the displayed list through the existing company-scoped query/use-case path.
- [ ] Extend the work-order appointment contract with immutable reschedule-history records and render them chronologically below each appointment.
- [ ] Render owner dashboard indicators for appointments, open work orders, and pending/resolution-required block requests using existing scoped data.
- [ ] Statistically inspect action-button propagation, employee-only appointment visibility, and localized UI text.

### Task 5: Static delivery verification

**Files:**
- Modify: `docs/superpowers/plans/2026-08-24-phase4-completion-fixes.md`

- [ ] Run `git diff --check`.
- [ ] Search changed Domain/Application files for direct clocks and authenticated endpoint contracts for `CompanyId`.
- [ ] Inspect changed dependency registrations and Razor injections for unresolved wiring by static review.
- [ ] Record that no build or tests were run under the project constraint; do not mark Phase 4 complete unless this review has no remaining blocker.
