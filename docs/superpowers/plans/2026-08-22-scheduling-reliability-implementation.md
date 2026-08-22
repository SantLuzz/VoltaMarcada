# Scheduling Reliability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make appointment booking concurrency-safe, enforce professional service qualification server-side, and display calendar appointments in the responsible professional's local time.

**Architecture:** PostgreSQL becomes the final authority for open-appointment overlap through a partial exclusion constraint. Application use cases retain availability checks for feedback and translate the known persistence conflict to an existing scheduling result. Appointment query DTOs carry the professional time zone, and the presentation layer converts UTC ranges using that supplied time zone.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor Web App, EF Core, Npgsql/PostgreSQL.

**Spec:** `docs/superpowers/specs/2026-08-22-scheduling-reliability-design.md`

## Global Constraints

- Preserve the intentional slot-listing behavior; do not change `GetAvailableAppointmentSlotsUseCase` iteration.
- Keep the company boundary in every query and constraint.
- Persist appointment timestamps in UTC and use the professional `TimeZoneId` only for local conversion.
- Do not introduce packages, frameworks, or infrastructure dependencies.
- Do not run builds, tests, application hosts, or migrations unless explicitly requested.
- Source code and technical documentation must use English identifiers and text.

---

## File Structure

- `src/ServiceSync.Infrastructure/Persistence/Migrations/20260822140000_AddOpenAppointmentOverlapConstraint.cs`: installs PostgreSQL `btree_gist` and adds/removes the open-interval exclusion constraint.
- `src/ServiceSync.Application/Abstractions/Persistence/IAppointmentRepository.cs`: exposes a persistence outcome for an appointment save that may lose a booking race.
- `src/ServiceSync.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`: translates PostgreSQL exclusion violations and clears failed tracked changes.
- `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`: maps persistence conflicts and validates the selected professional against an appointment service.
- `src/ServiceSync.Application/Scheduling/AppointmentChangeUseCases.cs`: validates reassignment and maps persistence conflicts for reschedules and adjustments.
- `src/ServiceSync.Application/WorkOrders/CreateWorkOrderUseCase.cs`: uses the appointment-aware save path when a new work order also creates an appointment.
- `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`: carries professional time zone data and adds the qualified-professional failure result.
- `src/ServiceSync.Application/Abstractions/Persistence/IProfessionalRepository.cs` and `src/ServiceSync.Infrastructure/Persistence/Repositories/ProfessionalRepository.cs`: return time-zone IDs for a set of company professionals in one query.
- `src/ServiceSync.Application/Scheduling/GetWorkOrderAppointmentsUseCase.cs` and `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`: project professional time zones into appointment DTOs.
- `src/ServiceSync.Web/Scheduling/AppointmentCalendarTime.cs`: converts appointment UTC timestamps and local calendar date ranges using configured professional time zones.
- `src/ServiceSync.Web/Components/Pages/Dashboard.razor`, `src/ServiceSync.Web/Components/Pages/Schedule.razor`, and `src/ServiceSync.Web/Components/Shared/AppointmentCard.razor`: render professional-local dates and times.

### Task 1: Enforce open appointment intervals in PostgreSQL

**Files:**
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260822140000_AddOpenAppointmentOverlapConstraint.cs`
- Modify: `src/ServiceSync.Application/Abstractions/Persistence/IAppointmentRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Repositories/AppointmentRepository.cs`
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentChangeUseCases.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/CreateWorkOrderUseCase.cs`

**Interfaces:**
- Produces `AppointmentPersistenceOutcome` with `Saved` and `Conflict` values.
- Produces `Task<AppointmentPersistenceOutcome> TrySaveAppointmentChangesAsync(CancellationToken cancellationToken = default)` on `IAppointmentRepository`.
- Consumes the existing `SchedulingOperationResultCode.AppointmentConflict` result.

- [ ] **Step 1: Add the PostgreSQL exclusion migration**

Create the migration with `Up` SQL that uses a half-open UTC range and applies only to open statuses. `0` and `1` are `Scheduled` and `InProgress` respectively.

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
    migrationBuilder.Sql("""
        ALTER TABLE \"Appointments\"
        ADD CONSTRAINT \"EX_Appointments_OpenProfessionalInterval\"
        EXCLUDE USING gist
        (\"CompanyId\" WITH =, \"ProfessionalId\" WITH =,
         tstzrange(\"StartAtUtc\", \"EndAtUtc\", '[)') WITH &&)
        WHERE (\"Status\" IN (0, 1));
        """);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("ALTER TABLE \"Appointments\" DROP CONSTRAINT \"EX_Appointments_OpenProfessionalInterval\";");
}
```

- [ ] **Step 2: Add a known persistence outcome**

In `IAppointmentRepository.cs`, add:

```csharp
Task<AppointmentPersistenceOutcome> TrySaveAppointmentChangesAsync(
    CancellationToken cancellationToken = default);

public enum AppointmentPersistenceOutcome
{
    Saved,
    Conflict
}
```

- [ ] **Step 3: Translate only the exclusion violation**

In `AppointmentRepository`, catch `DbUpdateException` whose inner `PostgresException` has SQL state `PostgresErrorCodes.ExclusionViolation` and constraint name `EX_Appointments_OpenProfessionalInterval`. Clear the change tracker before returning `Conflict`; rethrow every other persistence exception.

```csharp
public async Task<AppointmentPersistenceOutcome> TrySaveAppointmentChangesAsync(
    CancellationToken cancellationToken = default)
{
    try
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        return AppointmentPersistenceOutcome.Saved;
    }
    catch (DbUpdateException exception) when (
        exception.InnerException is PostgresException postgresException &&
        postgresException.SqlState == PostgresErrorCodes.ExclusionViolation &&
        postgresException.ConstraintName == "EX_Appointments_OpenProfessionalInterval")
    {
        dbContext.ChangeTracker.Clear();
        return AppointmentPersistenceOutcome.Conflict;
    }
}
```

- [ ] **Step 4: Use the conflict-aware save in every appointment mutation**

After adding or changing an appointment, call `TrySaveAppointmentChangesAsync`. When it returns `Conflict`, return `SchedulingOperationResult.Failure(SchedulingOperationResultCode.AppointmentConflict)`. In `CreateWorkOrderUseCase`, call this method only when `command.Appointment` is not null; otherwise retain `workOrderRepository.SaveChangesAsync`.

```csharp
var persistenceOutcome = await appointmentRepository.TrySaveAppointmentChangesAsync(cancellationToken);
return persistenceOutcome == AppointmentPersistenceOutcome.Conflict
    ? SchedulingOperationResult.Failure(SchedulingOperationResultCode.AppointmentConflict)
    : SchedulingOperationResult.Success(appointment.Id);
```

- [ ] **Step 5: Statically verify the persistence path**

Confirm the constraint name is identical in migration and repository, that only open statuses are included, and that create, reschedule, reassign, adjust, and work-order-with-appointment flows use the conflict-aware save path. Run `git diff --check` only.

### Task 2: Enforce professional service qualification in appointment use cases

**Files:**
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentChangeUseCases.cs`
- Modify: `src/ServiceSync.Web/Endpoints/AppointmentEndpoints.cs`
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`

**Interfaces:**
- Produces `SchedulingOperationResultCode.ProfessionalNotQualified`.
- Consumes `Professional.ProfessionalServices` and the appointment's effective `ServiceId`.

- [ ] **Step 1: Add a precise scheduling result**

Add `ProfessionalNotQualified` to `SchedulingOperationResultCode` immediately after `ProfessionalUnavailable`.

```csharp
ProfessionalUnavailable,
ProfessionalNotQualified,
AppointmentConflict,
```

- [ ] **Step 2: Validate creation against the effective service**

In `CreateAppointmentUseCase`, after resolving a work order or direct service, reject a selected professional when the effective non-null service is absent from `professional.ProfessionalServices`.

```csharp
if (normalized.ServiceId is { } serviceId &&
    professional.ProfessionalServices.All(item => item.ServiceId != serviceId))
{
    return SchedulingOperationResult.Failure(
        SchedulingOperationResultCode.ProfessionalNotQualified);
}
```

- [ ] **Step 3: Validate reassignment against the appointment service**

In `ReassignAppointmentUseCase`, after loading the new professional, enforce the same check using `appointment.ServiceId`. Preserve appointments without a service as reassignable.

```csharp
if (appointment.ServiceId is { } serviceId &&
    professional.ProfessionalServices.All(item => item.ServiceId != serviceId))
{
    return SchedulingOperationResult.Failure(
        SchedulingOperationResultCode.ProfessionalNotQualified);
}
```

- [ ] **Step 4: Surface the business result consistently**

Map `ProfessionalNotQualified` to HTTP 400 with `"O profissional não está habilitado para este serviço."` in `AppointmentEndpoints.ToResult`. Add the same message to `GetAppointmentCreationError` and `GetAppointmentOperationError` in `Schedule.razor`.

- [ ] **Step 5: Statically verify API bypass protection**

Confirm the validation executes inside use cases, not only in Razor filters, and check formatting with `git diff --check`.

### Task 3: Carry professional time zones into calendar rendering

**Files:**
- Modify: `src/ServiceSync.Application/Abstractions/Persistence/IProfessionalRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Repositories/ProfessionalRepository.cs`
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`
- Modify: `src/ServiceSync.Application/Scheduling/GetWorkOrderAppointmentsUseCase.cs`
- Create: `src/ServiceSync.Web/Scheduling/AppointmentCalendarTime.cs`
- Modify: `src/ServiceSync.Web/Components/Pages/Dashboard.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/AppointmentCard.razor`

**Interfaces:**
- Produces `Task<IReadOnlyDictionary<Guid, string>> GetTimeZoneIdsAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default)`.
- Extends `AppointmentDto` and `AppointmentDetailsDto` with `string ProfessionalTimeZoneId`.
- Produces `AppointmentCalendarTime.ToLocalDateTime(DateTimeOffset value, string timeZoneId)` and `AppointmentCalendarTime.GetUtcRange(...)` for presentation code.

- [ ] **Step 1: Add a single-query time-zone lookup**

Add the repository method to `IProfessionalRepository` and implement it with a company-filtered projection in `ProfessionalRepository`.

```csharp
public async Task<IReadOnlyDictionary<Guid, string>> GetTimeZoneIdsAsync(
    Guid companyId,
    IReadOnlyCollection<Guid> professionalIds,
    CancellationToken cancellationToken = default) =>
    await dbContext.Professionals.AsNoTracking()
        .Where(item => item.CompanyId == companyId && professionalIds.Contains(item.Id))
        .ToDictionaryAsync(item => item.Id, item => item.TimeZoneId, cancellationToken);
```

- [ ] **Step 2: Add time zone to appointment contracts and query mapping**

Extend both appointment DTO records with `ProfessionalTimeZoneId`. In `GetAppointmentsUseCase` and `GetWorkOrderAppointmentsUseCase`, obtain one dictionary for the distinct professional IDs, then map every item with its matching time zone. In `GetAppointmentDetailsUseCase`, inject `IProfessionalRepository`, load the appointment professional using `CompanyId`, and include its `TimeZoneId`.

```csharp
var timeZoneIds = await professionalRepository.GetTimeZoneIdsAsync(
    scope.CompanyId,
    items.Select(item => item.ProfessionalId).Distinct().ToArray(),
    cancellationToken);

return items.Select(item => new AppointmentDto(
    item.Id, item.ProfessionalId, item.CustomerId, item.EquipmentId,
    item.ServiceId, item.WorkOrderId, item.StartAtUtc, item.EndAtUtc,
    item.Status, item.Notes, timeZoneIds[item.ProfessionalId])).ToArray();
```

- [ ] **Step 3: Create presentation-only time conversion helpers**

Create `AppointmentCalendarTime.cs`. It must convert a UTC instant with `TimeZoneInfo.ConvertTime`, and calculate a UTC query range from local date boundaries across all allowed professional time zones.

```csharp
public static DateTime ToLocalDateTime(DateTimeOffset value, string timeZoneId) =>
    TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).DateTime;

public static (DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc) GetUtcRange(
    DateOnly startDate,
    DateOnly endDateExclusive,
    IEnumerable<string> timeZoneIds)
{
    var ranges = timeZoneIds.Select(timeZoneId =>
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return (
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startDate.ToDateTime(TimeOnly.MinValue), timeZone)),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endDateExclusive.ToDateTime(TimeOnly.MinValue), timeZone)));
    }).ToArray();

    return (ranges.Min(range => range.Item1), ranges.Max(range => range.Item2));
}
```

- [ ] **Step 4: Replace server-local rendering in every calendar surface**

Inject `ISchedulingConfiguration` into dashboard and schedule pages. Use `AppointmentCalendarTime.GetUtcRange` for dashboard and schedule range loads. In `Dashboard.razor`, use the professional-local date to group cards and professional-local start time in the details dialog. In `Schedule.razor` and `AppointmentCard.razor`, replace `ToLocalTime()` with `AppointmentCalendarTime.ToLocalDateTime` using the DTO time zone.

```csharp
private IReadOnlyList<AppointmentDto> GetAppointmentsForDay(DateOnly day) => appointments
    .Where(appointment => DateOnly.FromDateTime(
        AppointmentCalendarTime.ToLocalDateTime(
            appointment.StartAtUtc,
            appointment.ProfessionalTimeZoneId)) == day)
    .OrderBy(appointment => appointment.StartAtUtc)
    .ToArray();
```

- [ ] **Step 5: Statically verify date and time semantics**

Search scheduling and dashboard Razor components for remaining `Appointment.StartAtUtc.ToLocalTime()` and `Appointment.EndAtUtc.ToLocalTime()` calls. Confirm date grouping and displayed times use `ProfessionalTimeZoneId`, and run `git diff --check`.

### Task 4: Final static review

**Files:**
- Modify: files from Tasks 1-3 only when review identifies an inconsistency.

**Interfaces:**
- Consumes the final migration, repository outcomes, scheduling results, and time-zone DTOs.

- [ ] **Step 1: Check migration ordering and reversibility**

Confirm the migration timestamp is later than `20260822130000_RemoveApplicationUserDocumentUniqueIndex`, `Down` drops only the exclusion constraint, and the extension remains installed because it may be shared by other database objects.

- [ ] **Step 2: Check result handling**

Confirm conflict outcomes reach `AppointmentEndpoints.ToResult` and the Blazor schedule messages without becoming HTTP 500 or an unhandled component exception.

- [ ] **Step 3: Check company and authorization boundaries**

Confirm the overlap constraint includes `CompanyId`, time-zone lookups filter by `CompanyId`, and appointment use cases preserve owner/employee access checks.

- [ ] **Step 4: Run non-executing verification**

Run `git diff --check` and targeted `rg` searches for the exclusion constraint name, `ProfessionalNotQualified`, `ProfessionalTimeZoneId`, and remaining appointment `ToLocalTime()` usages. Do not run a build, tests, application host, or database migration.
