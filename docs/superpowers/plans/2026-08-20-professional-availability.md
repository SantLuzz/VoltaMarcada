# Professional Availability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a simple weekly work schedule and exceptional unavailability management to existing professional records.

**Architecture:** Store one tenant-scoped `ProfessionalAvailability` per professional with shared hours and flagged weekdays, plus tenant-scoped `ScheduleBlock` records for full-day ranges and single-day timed exceptions. Expose explicit application use cases behind a focused repository, and reuse one MudBlazor availability panel in the Owner edit screen and Employee read-only profile.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor Web App, MudBlazor, Entity Framework Core, PostgreSQL.

**Spec:** `docs/superpowers/specs/2026-08-20-professional-availability-design.md`

## Global Constraints

- All source identifiers, comments, logs, migration names, and technical validation messages are English; Brazilian Portuguese is used for UI copy.
- `Company` is the tenant boundary. Every professional, availability, and schedule-block query filters by the authenticated current `CompanyId`.
- The Owner manages availability. An Employee can read only the availability of their own linked professional.
- Every selected workday shares one entry, lunch start, lunch end, and exit schedule in this MVP.
- Use `DateOnly` for local business dates, `TimeOnly` for local clock times, and UTC `DateTimeOffset` values from `TimeProvider` for audit timestamps.
- Do not introduce dependencies or frameworks.
- Do not add automated tests because the project is in MVP validation and tests were not requested.
- Do not run `dotnet build`, `dotnet test`, `dotnet run`, migrations, or the application unless separately requested.
- Preserve all existing uncommitted workspace changes and stage only files belonging to a commit when commits are requested.

---

### Task 1: Define the availability domain model

**Files:**
- Create: `src/ServiceSync.Domain/Enums/ProfessionalWorkDays.cs`
- Create: `src/ServiceSync.Domain/Enums/ScheduleBlockType.cs`
- Create: `src/ServiceSync.Domain/Entities/ProfessionalAvailability.cs`
- Create: `src/ServiceSync.Domain/Entities/ScheduleBlock.cs`

**Interfaces:**
- Produces `[Flags] enum ProfessionalWorkDays` with `None`, Monday through Sunday, and `All`.
- Produces `enum ScheduleBlockType` with `TimeOff`, `Vacation`, `Appointment`, `Training`, `Absence`, and `Manual`.
- Produces `ProfessionalAvailability.Create(...)` and `Update(...)`.
- Produces `ScheduleBlock.Create(...)`.

- [ ] **Step 1: Add the workday and block enums**

  Use stable values so persisted weekday flags remain compatible:

  ```csharp
  [Flags]
  public enum ProfessionalWorkDays
  {
      None = 0,
      Monday = 1 << 0,
      Tuesday = 1 << 1,
      Wednesday = 1 << 2,
      Thursday = 1 << 3,
      Friday = 1 << 4,
      Saturday = 1 << 5,
      Sunday = 1 << 6,
      All = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday
  }

  public enum ScheduleBlockType
  {
      TimeOff,
      Vacation,
      Appointment,
      Training,
      Absence,
      Manual
  }
  ```

- [ ] **Step 2: Implement `ProfessionalAvailability` invariants**

  Add private setters, a private EF constructor, and these public behaviors:

  ```csharp
  public static ProfessionalAvailability Create(
      Guid companyId,
      Guid professionalId,
      ProfessionalWorkDays workDays,
      TimeOnly entryTime,
      TimeOnly lunchStartTime,
      TimeOnly lunchEndTime,
      TimeOnly exitTime,
      DateTimeOffset now);

  public void Update(
      ProfessionalWorkDays workDays,
      TimeOnly entryTime,
      TimeOnly lunchStartTime,
      TimeOnly lunchEndTime,
      TimeOnly exitTime,
      DateTimeOffset now);
  ```

  Reject empty IDs, `None`, unsupported flag bits, and any ordering other than `EntryTime < LunchStartTime < LunchEndTime < ExitTime`. Normalize audit timestamps with `ToUniversalTime()`.

- [ ] **Step 3: Implement `ScheduleBlock` invariants**

  Add this factory and a `MaxNotesLength` constant of `1000`:

  ```csharp
  public static ScheduleBlock Create(
      Guid companyId,
      Guid professionalId,
      ScheduleBlockType type,
      DateOnly startDate,
      DateOnly endDate,
      bool isFullDay,
      TimeOnly? startTime,
      TimeOnly? endTime,
      string? notes,
      DateTimeOffset now);
  ```

  Reject invalid enum values, reversed dates, timed ranges spanning more than one date, missing timed values, and `StartTime >= EndTime`. Clear both time values for full-day blocks. Trim notes, convert whitespace-only notes to `null`, and reject values longer than `1000` characters.

- [ ] **Step 4: Perform domain static review**

  Confirm the new entities implement `ICompanyScopedEntity`, have no public setters, generate server-side GUIDs, and use `DomainException` for invalid state. Run only:

  ```powershell
  rg -n "public .* set;|DateTime\.(Now|UtcNow)|DateTimeOffset\.(Now|UtcNow)" src/ServiceSync.Domain/Entities/ProfessionalAvailability.cs src/ServiceSync.Domain/Entities/ScheduleBlock.cs
  ```

  Expected: no public setter or direct machine-clock use.

### Task 2: Map persistence and add the schema migration

**Files:**
- Create: `src/ServiceSync.Infrastructure/Persistence/Configurations/ProfessionalAvailabilityConfiguration.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Configurations/ScheduleBlockConfiguration.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260820190000_AddProfessionalAvailability.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260820190000_AddProfessionalAvailability.Designer.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**Interfaces:**
- Produces `DbSet<ProfessionalAvailability> ProfessionalAvailabilities`.
- Produces `DbSet<ScheduleBlock> ScheduleBlocks`.
- Produces optional one-to-one availability and one-to-many schedule blocks for each professional.

- [ ] **Step 1: Configure `ProfessionalAvailability`**

  Map table `ProfessionalAvailabilities`, all required values, and `TimeOnly` columns as PostgreSQL `time without time zone`. Add alternate key `(CompanyId, Id)`, unique index `(CompanyId, ProfessionalId)`, and the tenant-scoped relationship:

  ```csharp
  builder.HasOne<Professional>()
      .WithOne()
      .HasForeignKey<ProfessionalAvailability>(availability =>
          new { availability.CompanyId, availability.ProfessionalId })
      .HasPrincipalKey<Professional>(professional =>
          new { professional.CompanyId, professional.Id })
      .OnDelete(DeleteBehavior.Cascade)
      .IsRequired();
  ```

- [ ] **Step 2: Configure `ScheduleBlock`**

  Map `StartDate` and `EndDate` as `date`, nullable times as `time without time zone`, notes to `1000` characters, and audit time as `timestamp with time zone`. Add alternate key `(CompanyId, Id)`, index `(CompanyId, ProfessionalId, StartDate, EndDate)`, and a tenant-scoped cascading foreign key to `Professional`.

- [ ] **Step 3: Add DbSets and hand-written migration artifacts**

  Follow the current repository convention of checked-in migration, designer, and snapshot files. The `Up` method creates both tables and indexes without changing existing Professional rows. The `Down` method drops `ScheduleBlocks` before `ProfessionalAvailabilities`. Do not execute EF migration commands.

- [ ] **Step 4: Statically compare mapping and migration intent**

  Verify table names, enum integer columns, PostgreSQL date/time types, alternate keys, tenant-scoped foreign keys, cascade behavior, and index names are identical across configurations, migration, designer, and snapshot.

### Task 3: Add application contracts, validation, and explicit use cases

**Files:**
- Create: `src/ServiceSync.Application/Abstractions/Persistence/IProfessionalAvailabilityRepository.cs`
- Create: `src/ServiceSync.Application/Professionals/ProfessionalAvailabilityCommands.cs`
- Create: `src/ServiceSync.Application/Professionals/ProfessionalAvailabilityDtos.cs`
- Create: `src/ServiceSync.Application/Professionals/ProfessionalAvailabilityCommandValidators.cs`
- Create: `src/ServiceSync.Application/Professionals/ProfessionalAvailabilityOperationResult.cs`
- Create: `src/ServiceSync.Application/Professionals/GetProfessionalAvailabilityUseCase.cs`
- Create: `src/ServiceSync.Application/Professionals/SaveProfessionalAvailabilityUseCase.cs`
- Create: `src/ServiceSync.Application/Professionals/CreateScheduleBlockUseCase.cs`
- Create: `src/ServiceSync.Application/Professionals/DeleteScheduleBlockUseCase.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`

**Interfaces:**
- `GetProfessionalAvailabilityUseCase.ExecuteAsync(Guid professionalId, CancellationToken)` returns `ProfessionalAvailabilityDto?`.
- `SaveProfessionalAvailabilityUseCase.ExecuteAsync(Guid professionalId, SaveProfessionalAvailabilityCommand, CancellationToken)` returns `ProfessionalAvailabilityOperationResult`.
- `CreateScheduleBlockUseCase.ExecuteAsync(Guid professionalId, CreateScheduleBlockCommand, CancellationToken)` returns `ProfessionalAvailabilityOperationResult` with the created block ID.
- `DeleteScheduleBlockUseCase.ExecuteAsync(Guid professionalId, Guid scheduleBlockId, CancellationToken)` returns `ProfessionalAvailabilityOperationResult`.

- [ ] **Step 1: Define commands and DTOs**

  Use these contracts:

  ```csharp
  public sealed record SaveProfessionalAvailabilityCommand(
      ProfessionalWorkDays WorkDays,
      TimeOnly EntryTime,
      TimeOnly LunchStartTime,
      TimeOnly LunchEndTime,
      TimeOnly ExitTime);

  public sealed record CreateScheduleBlockCommand(
      ScheduleBlockType Type,
      DateOnly StartDate,
      DateOnly EndDate,
      bool IsFullDay,
      TimeOnly? StartTime,
      TimeOnly? EndTime,
      string? Notes);

  public sealed record ProfessionalAvailabilityDto(
      Guid ProfessionalId,
      WeeklyScheduleDto? WeeklySchedule,
      IReadOnlyList<ScheduleBlockDto> ScheduleBlocks);
  ```

  `WeeklyScheduleDto` mirrors weekday and time values. `ScheduleBlockDto` contains block ID, type, date range, full-day marker, nullable times, and notes. DTO mapping occurs in Application, not Razor.

- [ ] **Step 2: Define repository operations and result codes**

  The focused repository exposes:

  ```csharp
  Task<bool> ProfessionalExistsAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default);
  Task<ProfessionalAvailability?> GetAvailabilityAsync(Guid professionalId, Guid companyId, bool trackChanges, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<ScheduleBlock>> GetScheduleBlocksAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default);
  Task<ScheduleBlock?> GetScheduleBlockAsync(Guid scheduleBlockId, Guid professionalId, Guid companyId, CancellationToken cancellationToken = default);
  Task AddAvailabilityAsync(ProfessionalAvailability availability, CancellationToken cancellationToken = default);
  Task AddScheduleBlockAsync(ScheduleBlock scheduleBlock, CancellationToken cancellationToken = default);
  void RemoveScheduleBlock(ScheduleBlock scheduleBlock);
  Task SaveChangesAsync(CancellationToken cancellationToken = default);
  ```

  Result codes are `Success`, `InvalidInput`, `ProfessionalNotFound`, `ScheduleBlockNotFound`, and `Forbidden`, with optional `ScheduleBlockId` on success.

- [ ] **Step 3: Add FluentValidation command rules**

  Validate supported non-empty workday flags and strict weekly time ordering. Validate block enum membership, dates, full-day/timed consistency, same-date timed blocks, time ordering, and maximum notes length. Keep the same rules in the domain to protect direct callers.

- [ ] **Step 4: Implement tenant-safe, role-aware reads**

  `GetProfessionalAvailabilityUseCase` resolves current `CompanyId` and checks `ProfessionalOwnerAuthorization`. An Owner may load any professional found by `(professionalId, companyId)`. For a non-Owner, call the existing `IProfessionalRepository.GetForCurrentUserAsync(currentUser.UserId, companyId, cancellationToken)` and require its professional ID to equal `professionalId`. Then load schedule and blocks, order blocks by `StartDate`, `StartTime`, then creation time, and map them to DTOs. A missing, cross-company, or different-employee professional returns `null`.

- [ ] **Step 5: Implement Owner-only mutations**

  Each mutation resolves `CompanyId`, calls `ProfessionalOwnerAuthorization.IsOwnerAsync`, checks professional ownership, validates its command, invokes the entity factory/update method with `TimeProvider.GetUtcNow()`, and saves once. Delete loads a block using all three identifiers before removal.

- [ ] **Step 6: Register application services**

  Add the four use cases as scoped registrations. Validators remain assembly-scanned by the existing FluentValidation registration.

### Task 4: Implement the EF Core repository and HTTP endpoints

**Files:**
- Create: `src/ServiceSync.Infrastructure/Persistence/Repositories/ProfessionalAvailabilityRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/DependencyInjection.cs`
- Modify: `src/ServiceSync.Web/Endpoints/ProfessionalEndpoints.cs`

**Interfaces:**
- Implements every `IProfessionalAvailabilityRepository` operation with ID plus `CompanyId` predicates.
- Adds read/save/block endpoints beneath `/professionals/{professionalId}/availability`.

- [ ] **Step 1: Implement repository queries**

  Use `AsNoTracking()` for read models and tracked queries only for weekly update and block deletion. Every predicate includes `CompanyId`; `GetScheduleBlockAsync` additionally filters `ProfessionalId`.

- [ ] **Step 2: Register the repository**

  Add:

  ```csharp
  services.AddScoped<IProfessionalAvailabilityRepository, ProfessionalAvailabilityRepository>();
  ```

- [ ] **Step 3: Add endpoint contracts and mappings**

  Add CompanyMember-authorized read and Owner-authorized mutation routes:

  ```text
  GET    /professionals/{professionalId}/availability
  PUT    /professionals/{professionalId}/availability
  POST   /professionals/{professionalId}/availability/blocks
  DELETE /professionals/{professionalId}/availability/blocks/{scheduleBlockId}
  ```

  Request records contain no `CompanyId`. Map `InvalidInput` to 400, missing professional/block to 404, forbidden to 403, and successful creation to 201 with the block ID.

- [ ] **Step 4: Review endpoint security**

  Confirm the Employee cannot use the general read endpoint to inspect another professional. The application read use case must enforce that Owners may read any current-company professional while Employees may read only the professional returned by `GetForCurrentUserAsync`; endpoint policy alone is not the ownership check.

### Task 5: Build a reusable MudBlazor availability panel

**Files:**
- Create: `src/ServiceSync.Web/Components/Shared/ProfessionalAvailabilityPanel.razor`

**Interfaces:**
- Parameters: `Guid ProfessionalId` and `bool ReadOnly`.
- Owner mode invokes save/create/delete use cases directly, matching existing interactive server page conventions.
- Read-only mode loads the same DTO but renders no enabled inputs or mutation actions.

- [ ] **Step 1: Build weekly schedule presentation**

  Use a bordered `MudPaper`, a `MudChipSet` or MudBlazor checkboxes for seven weekdays, and four outlined `MudTimePicker` controls in a responsive `MudGrid`. Portuguese labels are `Entrada`, `Início do almoço`, `Fim do almoço`, and `Saída`. Default an absent schedule to Monday-Friday, 08:00, 12:00, 13:00, and 18:00 without persisting until the Owner saves.

- [ ] **Step 2: Add weekly save behavior**

  Disable duplicate submissions, render inline validation for no weekdays and invalid time order, call `SaveProfessionalAvailabilityUseCase`, update the local DTO after success, and show a success snackbar. Read-only mode renders weekday chips and formatted time ranges rather than editable fields.

- [ ] **Step 3: Build the unavailability list and empty state**

  Use a responsive `MudTable` with type, period, notes, and actions. Translate all enum values to Brazilian Portuguese. Show an explanatory empty state when no blocks exist. Format full-day periods as inclusive dates and timed records as date plus time range.

- [ ] **Step 4: Add the unavailability form**

  Present an inline bordered section rather than a new page. Use `MudSelect` for type, `MudDatePicker` for dates, `MudSwitch` for `Dia inteiro`, conditional `MudTimePicker` controls, and an outlined multiline `MudTextField` for notes. Reset the form after successful creation, reload `ProfessionalAvailabilityDto`, and render the repository-defined sorted order.

- [ ] **Step 5: Add removal confirmation and feedback**

  Use `IDialogService.ShowMessageBox` before deletion. Disable the affected action while removing, call `DeleteScheduleBlockUseCase`, remove the DTO locally on success, and show localized snackbar feedback. Do not render the action in read-only mode.

- [ ] **Step 6: Review component states and accessibility**

  Verify loading, error, empty, validation, saving, creating, deleting, and read-only states. Every icon-only action has a tooltip and accessible label. Desktop uses compact columns; mobile collapses to a single column without horizontal form scrolling.

### Task 6: Integrate availability into Owner and Employee professional screens

**Files:**
- Modify: `src/ServiceSync.Web/Components/Pages/ProfessionalRegistration.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Professionals.razor`

**Interfaces:**
- Owner edit mode renders `<ProfessionalAvailabilityPanel ProfessionalId="..." ReadOnly="false" />`.
- Employee profile renders `<ProfessionalAvailabilityPanel ProfessionalId="..." ReadOnly="true" />` only for the authenticated user's linked professional DTO.

- [ ] **Step 1: Add Owner tabs without changing create behavior**

  Wrap the current form in `MudTabs`. Always show `Dados e serviços`. Show `Disponibilidade` only when `ProfessionalId` exists. In create mode, place supporting text below the form explaining that availability becomes available after the professional is saved; keep the existing redirect to `/profissionais` after creation.

- [ ] **Step 2: Keep page actions context-sensitive**

  The existing page-header save button continues saving only professional data. The availability panel has its own explicit save button. Disable only the operation currently running so availability loading does not block unrelated page navigation.

- [ ] **Step 3: Extend the Employee profile**

  Add a read-only availability section beneath the employee's existing linked services/status card. Pass only `currentProfessional.Id`, which was resolved by `GetCurrentEmployeeProfessionalUseCase`; do not accept an Employee-supplied professional query ID.

- [ ] **Step 4: Perform static UI flow review**

  Trace Owner create, Owner edit, absent schedule, saved schedule, create/remove block, Employee linked profile, and Employee unlinked profile. Confirm Portuguese copy renders correctly as UTF-8 and the UI uses only MudBlazor controls.

### Task 7: Final static verification

**Files:**
- Review every file created or modified in Tasks 1-6.

- [ ] **Step 1: Check formatting and patch integrity**

  Run:

  ```powershell
  git diff --check
  git status --short
  ```

  Expected: no whitespace errors; unrelated pre-existing changes remain present and untouched.

- [ ] **Step 2: Scan tenant and clock boundaries**

  Run targeted `rg` searches for availability queries lacking `CompanyId`, browser-facing `CompanyId` request fields, direct clock access, public domain setters, raw HTML form inputs, and Portuguese source identifiers. Review each result manually rather than relying only on zero matches.

- [ ] **Step 3: Compare implementation to the approved specification**

  Confirm one shared weekly schedule, selectable workdays, mandatory lunch range, six unavailability types, full-day inclusive ranges, single-day timed blocks, Owner mutation, Employee own-record read access, optional availability for existing professionals, and future slot-calculation compatibility.

- [ ] **Step 4: Report verification limits**

  State explicitly that no build, automated test, application launch, or migration execution was performed, because repository instructions prohibit those actions without a separate request.
