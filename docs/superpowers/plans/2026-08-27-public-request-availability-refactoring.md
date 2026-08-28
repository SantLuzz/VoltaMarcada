# Public Request Availability Refactoring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow customers to submit preferred public service times without professional exposure, then confirm an appointment only after an internal proposal and customer acceptance.

**Architecture:** Company business hours produce public preference slots independently from professional calendars. A reusable application availability engine evaluates qualified professionals only inside authenticated request handling, while the existing scheduling transaction performs the final revalidation and creates an external appointment.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, Entity Framework Core, PostgreSQL.

**Spec:** `docs/superpowers/specs/2026-08-27-public-request-availability-refactoring-design.md`

## Global Constraints

- Keep source code, identifiers, migrations, logs, and technical documentation in English.
- Preserve tenant isolation; internal reads and mutations filter by the authenticated company.
- Public operations resolve context only from a company slug or verified token.
- Do not expose or accept a professional identifier in any public page, command, endpoint, or response DTO.
- Public slots are preferences from company business hours; they do not reserve professional availability.
- Keep 30-minute professional scheduling blocks and validate full service duration.
- Do not implement quotes, payments, or appointment types in this phase.
- Do not add automated tests, run builds, tests, the app, or migrations unless explicitly requested.
- Use static inspection and `git diff --check` as implementation verification.

---

### Task 1: Company business hours and scheduling options

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/CompanyBusinessHours.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyBusinessHoursConfiguration.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Company.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs`
- Modify: `src/VoltaMarcada.Application/CompanySettings/CompanyPublicSettingsDto.cs`
- Modify: `src/VoltaMarcada.Application/CompanySettings/UpdateCompanyPublicSettingsCommand.cs`
- Modify: `src/VoltaMarcada.Application/CompanySettings/CompanyPublicSettingsUseCases.cs`
- Modify: `src/VoltaMarcada.Web/Configuration/SchedulingOptions.cs`
- Modify: `src/VoltaMarcada.Web/appsettings.json`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Company.razor`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_AddCompanyBusinessHours.cs`

**Interfaces:**
- Consumes: existing `Company`, owner-scoped company-settings use cases, and `SchedulingOptions` binding.
- Produces: `CompanyBusinessHours`, `Company.PublicRequestIntervalMinutes`, and validated `AvailabilitySearchDays` / `AvailabilityResultLimit` options.

- [ ] **Step 1: Add the domain model and company behavior**

```csharp
public sealed class CompanyBusinessHours
{
    public DayOfWeek DayOfWeek { get; private set; }
    public bool IsOpen { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
}

public void UpdatePublicScheduling(
    int publicRequestIntervalMinutes,
    IReadOnlyCollection<CompanyBusinessHoursInput> businessHours,
    DateTimeOffset now)
```

Validate seven distinct weekdays; require an interval only on open days; require `EndTime > StartTime`; and require a positive public interval that divides 60. Initialize the company setting to 60 for existing and new companies.

- [ ] **Step 2: Persist and migrate the company settings**

Map `CompanyBusinessHours` as a company-owned collection with a unique `(company_id, day_of_week)` index. Add the `public_request_interval_minutes` company column and a business-hours table. The migration backfills a 60-minute interval and creates seven closed weekday records only when a company has no existing business-hour rows.

- [ ] **Step 3: Extend the owner settings contract and UI**

Include public interval and all seven weekday values in the settings query and update command. In `/empresa`, render a settings form with an open/closed switch and start/end selectors per weekday. Bind values only through the authenticated company settings use case.

- [ ] **Step 4: Add bounded scheduling options**

```csharp
public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";
    public int SlotIntervalMinutes { get; init; } = 30;
    public int AvailabilitySearchDays { get; init; } = 7;
    public int AvailabilityResultLimit { get; init; } = 10;
}
```

Validate both new values as positive during option registration and add their defaults to `appsettings.json`.

- [ ] **Step 5: Perform static verification**

Run:

```powershell
git -c safe.directory=D:/Code/VoltaMarcada diff --check
rg -n "PublicRequestIntervalMinutes|AvailabilitySearchDays|AvailabilityResultLimit|CompanyBusinessHours" src
```

Expected: no whitespace errors and all domain, configuration, application, persistence, and web references resolve by inspection.

### Task 2: Unassigned public appointment requests and proposal states

**Files:**
- Modify: `src/VoltaMarcada.Domain/Entities/AppointmentRequest.cs`
- Modify: `src/VoltaMarcada.Domain/Enums/AppointmentRequestStatus.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/AppointmentRequestConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/AppointmentRequestRepository.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IAppointmentRequestRepository.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestCommand.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitContextualAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_MakeAppointmentRequestProfessionalOptional.cs`

**Interfaces:**
- Consumes: company public slot validation from Task 1 and current public customer/equipment resolution.
- Produces: public `AppointmentRequest` records without a professional assignment and explicit proposal-response state transitions.

- [ ] **Step 1: Separate a preference from a selected professional**

Make the request professional, requested UTC interval, and proposed data optional when the request is created. Keep local requested date, local requested start time, service duration, customer, equipment, and tenant identifiers mandatory.

```csharp
public static AppointmentRequest CreatePublicPreference(
    Guid companyId,
    Guid customerId,
    Guid serviceId,
    Guid? equipmentId,
    Guid? maintenanceOpportunityId,
    DateOnly requestedDate,
    TimeOnly requestedStartTime,
    int durationMinutes,
    CustomerProfileProposal? customerProfileProposal,
    DateTimeOffset now);
```

- [ ] **Step 2: Add proposal and customer-response behavior**

```csharp
public void Propose(
    DateOnly date,
    TimeOnly startTime,
    int durationMinutes,
    Guid professionalId,
    string responseTokenHash,
    DateTimeOffset responseTokenExpiresAtUtc,
    DateTimeOffset now);

public void AcceptProposal(Guid appointmentId, DateTimeOffset now);
public void DeclineProposal(DateTimeOffset now);
```

Use `RescheduleRequested` for an active company proposal and add a terminal `CustomerDeclined` state. Preserve company rejection for internal rejection of a pending request.

- [ ] **Step 3: Update persistence and public submission orchestration**

Make the professional and requested UTC database columns nullable. Remove professional identifiers from public submission commands. Validate that the submitted date/time is one of the company business-hour public slots before persisting a request. Contextual submissions retain token-derived service, customer, equipment, and opportunity identifiers.

- [ ] **Step 4: Perform static verification**

Run:

```powershell
git -c safe.directory=D:/Code/VoltaMarcada diff --check
rg -n "CreatePublicPreference|CustomerDeclined|Propose\(|AcceptProposal|DeclineProposal" src
```

Expected: no public command accepts a professional identifier and nullable persistence matches the domain model.

### Task 3: Centralized professional availability analysis

**Files:**
- Create: `src/VoltaMarcada.Application/Scheduling/AppointmentRequestAvailabilityAnalysis.cs`
- Create: `src/VoltaMarcada.Application/Scheduling/AnalyzeAppointmentRequestAvailabilityUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/AppointmentAvailability.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/ISchedulingConfiguration.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`

**Interfaces:**
- Consumes: company-scoped request, active service assignments, professional schedules, blocks, open appointments, and Task 1 scheduling options.
- Produces: exact-time eligible professionals and bounded chronological `(date, time, professional)` alternatives for owner use.

- [ ] **Step 1: Define explicit analysis contracts**

```csharp
public sealed record AvailableProfessionalDto(Guid ProfessionalId, string ProfessionalName);
public sealed record AppointmentAlternativeDto(Guid ProfessionalId, string ProfessionalName, DateOnly Date, TimeOnly StartTime);
public sealed record AppointmentRequestAvailabilityAnalysis(
    IReadOnlyList<AvailableProfessionalDto> RequestedTimeProfessionals,
    IReadOnlyList<AppointmentAlternativeDto> Alternatives);
```

- [ ] **Step 2: Implement exact-time analysis**

For every active professional assigned to the requested service, use `AppointmentAvailability` with the full service duration. Return all successful candidates at the requested local time. Never choose or persist a professional in this use case.

- [ ] **Step 3: Implement chronological alternatives**

Only when exact-time candidates are empty, scan later 30-minute internal slots on the requested date and then subsequent dates up to `AvailabilitySearchDays`. For every valid candidate, append a date/time/professional alternative until `AvailabilityResultLimit` is reached. Use each professional time zone when calculating UTC intervals and exclude past local times.

- [ ] **Step 4: Register and inspect reuse boundaries**

Register the analyzer in the composition root and ensure it depends only on Application abstractions plus the existing availability service. Do not call it from public pages to render public slots.

- [ ] **Step 5: Perform static verification**

Run:

```powershell
git -c safe.directory=D:/Code/VoltaMarcada diff --check
rg -n "AnalyzeAppointmentRequestAvailabilityUseCase|AppointmentRequestAvailabilityAnalysis|AvailabilityResultLimit" src
```

Expected: the analyzer is available to authenticated scheduling workflows and public pages remain independent of professional availability.

### Task 4: Public preference forms and safe proposal response

**Files:**
- Modify: `src/VoltaMarcada.Application/PublicBooking/GetPublicAvailableSlotsUseCase.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/PublicCompanyDtos.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/AppointmentRequestResponseUseCases.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicMaintenanceBooking.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicAppointmentRequestResponse.razor`

**Interfaces:**
- Consumes: Task 1 public business-hour slots and Task 2 unassigned request/proposal state.
- Produces: public UI and DTOs that never contain professional data and use a proposal response only to accept or decline the owner-selected time.

- [ ] **Step 1: Replace public availability slots**

Change public slot DTOs to contain only `DateOnly` and `TimeOnly`. Generate times from company business hours and `PublicRequestIntervalMinutes`, rather than `ProfessionalId`, professional names, 30-minute internals, appointments, or schedule blocks.

- [ ] **Step 2: Remove professional selection from both public request pages**

Remove professional selectors, `AnyProfessional`, professional-bound slot keys, and professional IDs from the public company and contextual maintenance forms. Display the required Brazilian Portuguese preference notice and submit only service/context, date, time, and existing customer details.

- [ ] **Step 3: Restrict the response page to the proposal**

Show the company, service, and proposed date/time without a professional name. Provide `Accept proposed time` and `Decline proposal` actions; remove the customer ability to browse or submit other slots. A stale or unavailable proposal displays safe Brazilian Portuguese text without disclosing internal data.

- [ ] **Step 4: Perform static verification**

Run:

```powershell
rg -n "Professional|AnyProfessional|ProfessionalId|ProfessionalName" src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor src/VoltaMarcada.Web/Components/Pages/PublicMaintenanceBooking.razor src/VoltaMarcada.Web/Components/Pages/PublicAppointmentRequestResponse.razor
```

Expected: no professional selection, identifier, or name remains in a public page. Inspect response DTOs to confirm the same boundary.

### Task 5: Owner proposal handling and transactional confirmation

**Files:**
- Modify: `src/VoltaMarcada.Application/Scheduling/GetAppointmentRequestsUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/SuggestAppointmentRequestTimeUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/AcceptAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/AppointmentRequestResponseUseCases.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Schedule.razor`
- Modify: `src/VoltaMarcada.Web/Endpoints/AppointmentEndpoints.cs`

**Interfaces:**
- Consumes: Task 2 proposal state and Task 3 availability analysis.
- Produces: owner-selected proposals and automatic external appointment creation after customer acceptance, with final transactional revalidation.

- [ ] **Step 1: Enrich the authenticated request queue**

For pending requests, query Task 3's analysis result and display all exact-time compatible professionals. If none are available, display up to the configured alternative combinations. Keep all queries filtered by `ICurrentCompany.CompanyId`.

- [ ] **Step 2: Replace internal confirmation with proposal creation**

The owner chooses one exact-time professional or one returned alternative. Revalidate it immediately, generate and hash a secure response token, and call `AppointmentRequest.Propose`. Do not create an appointment at proposal time.

- [ ] **Step 3: Confirm automatically after customer acceptance**

The response use case loads only the hash-validated proposal token. In the existing scheduling transaction, reload the selected service and professional, revalidate eligibility, blocks, duration, and appointment overlap, then create an external appointment and call `AcceptProposal` atomically. If validation fails, leave the request unaccepted, revoke the stale response token, and return an explicit owner-action result.

- [ ] **Step 4: Mark contextual maintenance bookings only after acceptance**

When the transaction creates an appointment from a contextual request, move the maintenance opportunity to `Booked` in that same transaction. A pending request or customer-declined proposal never changes it to `Booked`.

- [ ] **Step 5: Perform static verification**

Run:

```powershell
git -c safe.directory=D:/Code/VoltaMarcada diff --check
rg -n "AnalyzeAppointmentRequestAvailabilityUseCase|Propose\(|AcceptProposal|CustomerDeclined|Create.*Appointment" src
```

Expected: no appointment is created when a public preference is submitted or an owner proposal is sent; creation occurs only in the customer-acceptance transaction after revalidation.

## Plan Self-Review

- Spec coverage: Tasks 1-5 cover business hours, public granularity, unassigned requests, professional analysis, alternatives, customer proposal response, transactional acceptance, contextual maintenance, security, and the exclusion of quotes.
- Placeholder scan: no placeholders or deferred implementation instructions are present.
- Type consistency: public request commands omit `ProfessionalId`; owner analysis and proposal contracts use it internally; `AppointmentRequest` persists it only after proposal selection.
