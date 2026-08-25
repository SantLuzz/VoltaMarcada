# Professional Daily Availability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow each professional to have independently configured working hours for every active weekday, with an optional lunch interval and a UI copy action.

**Architecture:** Keep `ProfessionalAvailability` as the tenant-scoped aggregate for one professional, but replace its shared weekday flags and times with owned `ProfessionalDailySchedule` children. The owner saves all daily entries atomically; the Blazor panel provides the client-only action that copies one active day's values to all other active days.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, Entity Framework Core, PostgreSQL, MudBlazor, FluentValidation.

**Spec:** `docs/superpowers/specs/2026-08-21-professional-daily-availability-design.md`

## Global Constraints

- All source identifiers, migration names, logs, comments, and technical documentation must be in English; UI text remains Brazilian Portuguese.
- Resolve the tenant from `ICurrentCompany`; do not accept `CompanyId` from the browser.
- Preserve tenant filtering and the Company Owner mutation / linked Employee read-only authorization model.
- Use `Guid`, `DateOnly`, `TimeOnly`, and `TimeProvider` consistently with existing code; domain/application logic must not use wall-clock APIs directly.
- Do not add packages, projects, tests, builds, test runs, application launches, or migration execution unless the user explicitly requests them.
- Preserve unrelated dirty-worktree changes. Stage and commit only files created or changed for this feature.

---

## File Structure

- Create `src/VoltaMarcada.Domain/ValueObjects/ProfessionalDailyScheduleDefinition.cs`: immutable schedule input used by the availability aggregate.
- Create `src/VoltaMarcada.Domain/Entities/ProfessionalDailySchedule.cs`: tenant-scoped child entity representing one weekday's work period and optional lunch interval.
- Modify `src/VoltaMarcada.Domain/Entities/ProfessionalAvailability.cs`: own and validate the complete set of daily schedules.
- Delete `src/VoltaMarcada.Domain/Enums/ProfessionalWorkDays.cs`: the flags enum becomes obsolete after all consumers use `DayOfWeek` entries.
- Create `src/VoltaMarcada.Infrastructure/Persistence/Configurations/ProfessionalDailyScheduleConfiguration.cs`: map the child table, keys, relationships, nullable lunch fields, and unique weekday constraint.
- Modify `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`: expose the new set for explicit EF Core mapping and migration discovery.
- Modify `src/VoltaMarcada.Infrastructure/Persistence/Configurations/ProfessionalAvailabilityConfiguration.cs`: configure the aggregate-to-child navigation and remove old scalar mappings.
- Modify `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ProfessionalAvailabilityRepository.cs`: include daily schedules whenever an availability is read.
- Create migration and designer `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260821100000_ReplaceSharedProfessionalAvailabilityWithDailySchedules.cs` and `.Designer.cs`; modify `AppDbContextModelSnapshot.cs`.
- Modify `src/VoltaMarcada.Application/Professionals/ProfessionalAvailabilityCommands.cs`, `ProfessionalAvailabilityDtos.cs`, and `ProfessionalAvailabilityCommandValidators.cs`: replace the shared schedule contracts with per-day contracts and collection validation.
- Modify `src/VoltaMarcada.Application/Professionals/SaveProfessionalAvailabilityUseCase.cs` and `GetProfessionalAvailabilityUseCase.cs`: map the full day collection to and from the aggregate.
- Modify `src/VoltaMarcada.Web/Endpoints/ProfessionalEndpoints.cs`: receive the list of daily schedule requests.
- Modify `src/VoltaMarcada.Web/Components/Shared/ProfessionalAvailabilityPanel.razor`: render, validate, save, copy, and display individual weekdays.

## Task 1: Model Daily Schedules in the Domain

**Files:**

- Create: `src/VoltaMarcada.Domain/ValueObjects/ProfessionalDailyScheduleDefinition.cs`
- Create: `src/VoltaMarcada.Domain/Entities/ProfessionalDailySchedule.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/ProfessionalAvailability.cs`
- Delete: `src/VoltaMarcada.Domain/Enums/ProfessionalWorkDays.cs`

**Consumes:** The existing `ProfessionalAvailability` identifiers, audit timestamps, and `DomainException` pattern.

**Produces:** `ProfessionalDailyScheduleDefinition`, `ProfessionalDailySchedule`, and collection-based `ProfessionalAvailability.Create` / `Update` methods for application mapping.

- [ ] **Step 1: Add the immutable daily schedule definition**

```csharp
namespace VoltaMarcada.Domain.ValueObjects;

public sealed record ProfessionalDailyScheduleDefinition(
    DayOfWeek Weekday,
    TimeOnly EntryTime,
    TimeOnly ExitTime,
    TimeOnly? LunchStartTime,
    TimeOnly? LunchEndTime);
```

- [ ] **Step 2: Add `ProfessionalDailySchedule` as an EF-compatible child entity**

```csharp
public sealed class ProfessionalDailySchedule : ICompanyScopedEntity
{
    private ProfessionalDailySchedule() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ProfessionalAvailabilityId { get; private set; }
    public DayOfWeek Weekday { get; private set; }
    public TimeOnly EntryTime { get; private set; }
    public TimeOnly ExitTime { get; private set; }
    public TimeOnly? LunchStartTime { get; private set; }
    public TimeOnly? LunchEndTime { get; private set; }

    internal static ProfessionalDailySchedule Create(
        Guid companyId,
        Guid professionalAvailabilityId,
        ProfessionalDailyScheduleDefinition definition) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProfessionalAvailabilityId = professionalAvailabilityId,
            Weekday = definition.Weekday,
            EntryTime = definition.EntryTime,
            ExitTime = definition.ExitTime,
            LunchStartTime = definition.LunchStartTime,
            LunchEndTime = definition.LunchEndTime
        };
}
```

Keep validation in the aggregate so duplicate weekdays are checked across the full collection. Give the child a server-generated `Guid` and no public setters.

- [ ] **Step 3: Replace scalar weekly fields with the owned collection**

In `ProfessionalAvailability`, remove `WorkDays`, `EntryTime`, `LunchStartTime`, `LunchEndTime`, and `ExitTime`. Add a private `List<ProfessionalDailySchedule>` and a public `IReadOnlyCollection<ProfessionalDailySchedule> DailySchedules`.

Use collection-based signatures:

```csharp
public static ProfessionalAvailability Create(
    Guid companyId,
    Guid professionalId,
    IReadOnlyCollection<ProfessionalDailyScheduleDefinition> dailySchedules,
    DateTimeOffset now);

public void Update(
    IReadOnlyCollection<ProfessionalDailyScheduleDefinition> dailySchedules,
    DateTimeOffset now);
```

`Update` must validate first, clear the existing child collection only after validation succeeds, recreate every child with the aggregate ID and company ID, and then update `UpdatedAtUtc` with `now.ToUniversalTime()`.

- [ ] **Step 4: Implement full-collection invariants**

Reject an empty collection, an invalid `DayOfWeek` value, duplicate weekdays, `EntryTime >= ExitTime`, exactly one lunch boundary being present, or a lunch period outside the work period. The accepted ranges are:

```csharp
definition.LunchStartTime is null && definition.LunchEndTime is null
    || definition.EntryTime < definition.LunchStartTime
       && definition.LunchStartTime < definition.LunchEndTime
       && definition.LunchEndTime < definition.ExitTime
```

Use English `DomainException` messages consistent with the existing entity.

- [ ] **Step 5: Remove the obsolete flags enum**

Delete `ProfessionalWorkDays.cs` only after the compilation references in this feature have been replaced in later tasks. Do not leave compatibility properties or aliases that keep the old persistence shape alive.

- [ ] **Step 6: Perform static domain validation**

Run:

```powershell
rg -n "ProfessionalWorkDays|DateTime\.(Now|UtcNow)|DateTimeOffset\.(Now|UtcNow)|public .* set;" src/VoltaMarcada.Domain
```

Expected: no `ProfessionalWorkDays` references and no new public setters or direct clock use in the new availability model.

## Task 2: Persist, Load, and Migrate the Daily Collection

**Files:**

- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/ProfessionalDailyScheduleConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/ProfessionalAvailabilityConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ProfessionalAvailabilityRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260821100000_ReplaceSharedProfessionalAvailabilityWithDailySchedules.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260821100000_ReplaceSharedProfessionalAvailabilityWithDailySchedules.Designer.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**Consumes:** The aggregate and child entity from Task 1.

**Produces:** Tenant-safe loading of daily schedules and a schema migration that preserves existing shared schedules.

- [ ] **Step 1: Map `ProfessionalDailySchedule`**

Configure the `ProfessionalDailySchedules` table with required `Id`, `CompanyId`, `ProfessionalAvailabilityId`, `Weekday`, `EntryTime`, and `ExitTime`; nullable lunch fields use `time without time zone`. Use `ValueGeneratedNever()` for `Id` and a unique index named `UX_ProfessionalDailySchedules_CompanyId_AvailabilityId_Weekday` on `(CompanyId, ProfessionalAvailabilityId, Weekday)`.

Add a tenant-scoped foreign key from `(CompanyId, ProfessionalAvailabilityId)` to the parent alternate key `(CompanyId, Id)`, with cascade delete. Configure the parent's `DailySchedules` navigation in `ProfessionalAvailabilityConfiguration` and remove mappings for the five former shared schedule columns.

- [ ] **Step 2: Update the context and repository query**

Add:

```csharp
public DbSet<ProfessionalDailySchedule> ProfessionalDailySchedules => Set<ProfessionalDailySchedule>();
```

to `AppDbContext`. Change `GetAvailabilityAsync` to include the collection before its company-scoped `SingleOrDefaultAsync`:

```csharp
query = query.Include(availability => availability.DailySchedules);
```

Retain the `(professionalId, companyId)` predicate exactly as it is. No independent child lookup is needed for the initial feature.

- [ ] **Step 3: Create the descriptive migration**

Create `ReplaceSharedProfessionalAvailabilityWithDailySchedules`. In `Up`:

1. Create `ProfessionalDailySchedules` with its composite foreign key and unique index.
2. Insert one row for each bit selected in legacy `WorkDays` using seven explicit PostgreSQL `INSERT ... SELECT` statements. The mapping is Monday `1`, Tuesday `2`, Wednesday `4`, Thursday `8`, Friday `16`, Saturday `32`, and Sunday `64`.
3. Carry `CompanyId`, `Id` as `ProfessionalAvailabilityId`, `EntryTime`, `ExitTime`, `LunchStartTime`, and `LunchEndTime` into every new row; generate each new child ID in SQL using `md5(random()::text || clock_timestamp()::text)::uuid`, avoiding any extension dependency.
4. Drop the legacy `WorkDays`, `EntryTime`, `LunchStartTime`, `LunchEndTime`, and `ExitTime` columns after all rows have been inserted.

Keep `ScheduleBlocks` untouched. In `Down`, recreate the legacy columns and populate them only after verifying with PostgreSQL SQL that each availability's daily rows have identical work and lunch periods. Raise a database exception if a distinct daily schedule would otherwise be discarded; never silently collapse the values.

- [ ] **Step 4: Update the model snapshot and generated designer**

Ensure the generated migration designer and `AppDbContextModelSnapshot` represent the child table, its unique index, and its composite foreign key, and no longer describe the legacy schedule columns on `ProfessionalAvailability`.

- [ ] **Step 5: Perform static persistence and migration review**

Run:

```powershell
rg -n "ProfessionalDailySchedules|UX_ProfessionalDailySchedules|WorkDays|LunchStartTime|LunchEndTime" src/VoltaMarcada.Infrastructure
```

Expected: daily table, relationship, index, and one-way legacy data expansion are present; old columns appear only in the migration's compatibility SQL.

## Task 3: Replace Application and HTTP Contracts

**Files:**

- Modify: `src/VoltaMarcada.Application/Professionals/ProfessionalAvailabilityCommands.cs`
- Modify: `src/VoltaMarcada.Application/Professionals/ProfessionalAvailabilityDtos.cs`
- Modify: `src/VoltaMarcada.Application/Professionals/ProfessionalAvailabilityCommandValidators.cs`
- Modify: `src/VoltaMarcada.Application/Professionals/SaveProfessionalAvailabilityUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Professionals/GetProfessionalAvailabilityUseCase.cs`
- Modify: `src/VoltaMarcada.Web/Endpoints/ProfessionalEndpoints.cs`

**Consumes:** `ProfessionalDailyScheduleDefinition` and the collection-backed aggregate from Task 1; repository loading from Task 2.

**Produces:** A tenant-safe save and read contract that represents all active weekdays explicitly.

- [ ] **Step 1: Define explicit collection contracts**

Replace the old command with:

```csharp
public sealed record SaveProfessionalAvailabilityCommand(
    IReadOnlyList<SaveProfessionalDailyScheduleCommand> DailySchedules);

public sealed record SaveProfessionalDailyScheduleCommand(
    DayOfWeek Weekday,
    TimeOnly EntryTime,
    TimeOnly ExitTime,
    TimeOnly? LunchStartTime,
    TimeOnly? LunchEndTime);
```

Replace `WeeklyScheduleDto` with `DailyScheduleDto` and expose `IReadOnlyList<DailyScheduleDto>? DailySchedules` from `ProfessionalAvailabilityDto`. Map DTOs in `ProfessionalAvailabilityDtos.cs`, ordered by Monday through Sunday, using `(int)((schedule.Weekday + 6) % 7)`.

- [ ] **Step 2: Validate the submitted collection**

`SaveProfessionalAvailabilityCommandValidator` must require a non-empty collection and use `RuleForEach` to validate individual rows. Add collection-level rules for unique valid weekdays and per-row rules matching the domain invariant: valid entry/exit ordering, both lunch values null or both populated, and a lunch interval strictly inside the work period.

Use the existing English internal validation messages. The Razor UI will translate failures to concise Brazilian Portuguese feedback.

- [ ] **Step 3: Map application contracts to the aggregate**

In `SaveProfessionalAvailabilityUseCase`, map command rows to `ProfessionalDailyScheduleDefinition` and pass that collection to `ProfessionalAvailability.Create` or `availability.Update`. Preserve the current authorization check, `ICurrentCompany` resolution, repository ownership predicate, `TimeProvider.GetUtcNow()`, and single `SaveChangesAsync` call.

In `GetProfessionalAvailabilityUseCase`, map the loaded aggregate's `DailySchedules` to the replacement DTO. Preserve schedule-block mapping and all existing Owner/linked-Employee authorization behavior.

- [ ] **Step 4: Change the HTTP request payload**

Replace `SaveProfessionalAvailabilityRequest` with:

```csharp
public sealed record SaveProfessionalAvailabilityRequest(
    IReadOnlyList<SaveProfessionalDailyScheduleRequest> DailySchedules);

public sealed record SaveProfessionalDailyScheduleRequest(
    DayOfWeek Weekday,
    TimeOnly EntryTime,
    TimeOnly ExitTime,
    TimeOnly? LunchStartTime,
    TimeOnly? LunchEndTime);
```

Map every request row to `SaveProfessionalDailyScheduleCommand`. Keep the owner-only `PUT /{professionalId}/availability` endpoint and its existing result mapping unchanged.

- [ ] **Step 5: Perform static contract review**

Run:

```powershell
rg -n "WeeklyScheduleDto|ProfessionalWorkDays|SaveProfessionalAvailabilityCommand\(|DailyScheduleDto|SaveProfessionalDailySchedule" src/VoltaMarcada.Application src/VoltaMarcada.Web/Endpoints
```

Expected: all availability commands and DTOs use the daily schedule collection; no shared-schedule contracts remain.

## Task 4: Rework the Availability Panel

**Files:**

- Modify: `src/VoltaMarcada.Web/Components/Shared/ProfessionalAvailabilityPanel.razor`

**Consumes:** `DailyScheduleDto` and the revised save command from Task 3.

**Produces:** Owner editing and Employee read-only displays that support per-day schedules, optional lunches, and copying active days.

- [ ] **Step 1: Replace the shared form model**

Replace `WeeklyScheduleFormModel` with a seven-row `DailyScheduleFormModel` collection. Each row stores `DayOfWeek`, localized label, `IsEnabled`, `EntryTime`, `ExitTime`, `HasLunchBreak`, `LunchStartTime`, and `LunchEndTime`. Its `Load` method creates Monday-through-Sunday rows, loads existing DTO values, and defaults an absent schedule to active Monday-Friday (`08:00–12:00`, `13:00–18:00`) and inactive weekend rows.

- [ ] **Step 2: Render per-day owner controls responsively**

Change the title to **Horários de trabalho** and render each weekday in Monday-to-Sunday order. Each row must contain an enabled `MudCheckBox`, entry and exit `MudTimePicker` controls, and a `MudSwitch` labeled **Possui intervalo**. Render the lunch pickers only when `HasLunchBreak` is true. Disable all time inputs and the copy action while saving, and disable time inputs when the row is inactive.

Use `MudGrid` / `MudItem` breakpoints so fields are horizontal on medium screens and stacked on small screens. Keep the existing single **Salvar horários** button, loading state, localized errors, success snackbar, and surrounding schedule-block UI unchanged.

- [ ] **Step 3: Implement the client-only copy action**

Add an outlined action labelled **Aplicar aos demais dias ativos** to each enabled row. Its handler copies `EntryTime`, `ExitTime`, `HasLunchBreak`, `LunchStartTime`, and `LunchEndTime` from that row into every other enabled row. It must not change `IsEnabled`, must not save automatically, and must not modify inactive rows.

- [ ] **Step 4: Validate and submit daily rows**

Replace `ValidateSchedule` with validation that obtains enabled rows, requires at least one, and validates each enabled row. Report the weekday name in Portuguese for a missing time or invalid interval. Construct `SaveProfessionalAvailabilityCommand` from enabled rows only, setting lunch times to `null` when `HasLunchBreak` is false.

```csharp
new SaveProfessionalAvailabilityCommand(
    scheduleRows.Where(row => row.IsEnabled)
        .Select(row => new SaveProfessionalDailyScheduleCommand(
            row.Weekday,
            TimeOnly.FromTimeSpan(row.EntryTime!.Value),
            TimeOnly.FromTimeSpan(row.ExitTime!.Value),
            row.HasLunchBreak ? TimeOnly.FromTimeSpan(row.LunchStartTime!.Value) : null,
            row.HasLunchBreak ? TimeOnly.FromTimeSpan(row.LunchEndTime!.Value) : null))
        .ToArray());
```

- [ ] **Step 5: Update read-only presentation**

Replace selected-day chips plus two shared range fields with a compact list of the active day rows. Display one range without lunch as `08:00 às 12:00`; display a lunch schedule as `08:00 às 12:00 e 13:00 às 18:00`. Group only consecutive equal schedules. Do not expose mutation controls in `ReadOnly` mode.

- [ ] **Step 6: Perform static Razor review**

Run:

```powershell
rg -n "Horário padrão|WeeklySchedule|WorkDays|Aplicar aos demais dias ativos|Possui intervalo|ReadOnly" src/VoltaMarcada.Web/Components/Shared/ProfessionalAvailabilityPanel.razor
```

Expected: no shared-schedule fields remain, required Portuguese UI labels are present, and the read-only guard still protects all actions.

## Task 5: Final Static Verification and Focused Commit

**Files:**

- Review: every file listed in Tasks 1 through 4
- Modify: `docs/superpowers/specs/2026-08-21-professional-daily-availability-design.md` only if implementation reveals a genuine design mismatch

**Consumes:** Completed domain, persistence, application, endpoint, and UI changes.

**Produces:** A statically reviewed implementation committed separately from unrelated workspace changes.

- [ ] **Step 1: Review the end-to-end data flow**

Confirm this path is type-consistent: Blazor enabled rows → `SaveProfessionalAvailabilityCommand` → `ProfessionalDailyScheduleDefinition` → aggregate replacement → EF child rows → `DailyScheduleDto` → Blazor loading and read-only display.

- [ ] **Step 2: Review tenant and authorization boundaries**

Confirm that availability reads retain `(professionalId, companyId)` filtering, the composite foreign key contains `CompanyId`, and the Owner-only endpoint and application authorization remain unchanged. Confirm no `CompanyId` was added to any browser request.

- [ ] **Step 3: Review migration safety**

Inspect the migration SQL to confirm one row is created for every legacy workday bit, all legacy time values are carried forward, schedule blocks are untouched, and destructive rollback is rejected rather than silently losing distinct day schedules.

- [ ] **Step 4: Run non-executing static checks**

Run:

```powershell
git diff --check
rg -n "ProfessionalWorkDays|WeeklyScheduleDto|\.WorkDays|\.EntryTime.*availability|\.LunchStartTime.*availability" src
git status --short
```

Expected: no obsolete availability API references, no whitespace errors, and unrelated pre-existing changes remain unstaged and unmodified.

- [ ] **Step 5: Commit only feature-owned files**

Stage the files explicitly listed in Tasks 1 through 4 and the new migration files, inspect `git diff --cached --name-only`, then commit with:

```powershell
git commit -m "Add daily professional availability"
```

Do not stage or commit any unrelated dirty-worktree files.
