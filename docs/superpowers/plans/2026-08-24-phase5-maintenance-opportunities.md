# Phase 5 Maintenance Opportunities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create company-scoped maintenance opportunities when a work order is completed and provide an owner-only center that converts the opportunity into an internal appointment.

**Architecture:** A new Domain aggregate carries the commercial lifecycle. Application use cases own authorization, business-date calculation, state transitions, and the atomic handoffs to work-order completion and appointment creation. Infrastructure persists the aggregate through EF Core. The Web project presents the optional recommendation, the maintenance center, dashboard visibility, scheduling prefill, WhatsApp link, and a safe global exception boundary.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, MudBlazor, Entity Framework Core, PostgreSQL, FluentValidation

**Spec:** `docs/superpowers/specs/2026-08-24-phase5-maintenance-opportunities-design.md`

## Global Constraints

- Source code identifiers, diagnostics, logs, and technical documentation are English; UI messages are Brazilian Portuguese.
- Do not add frameworks, packages, background jobs, public booking, tokens, official WhatsApp integration, or automatic new work-order creation.
- Every opportunity query and mutation filters by the authenticated current company. The browser never supplies a tenant selector.
- The maintenance center, summary, schedule context, and all opportunity actions require `CompanyOwner`; employees may only submit an optional recommendation while completing an accessible work order.
- A selected recommendation requires exactly one due-date source, an active recommended service, and notes.
- Use `DateOnly` for the business due date and UTC `DateTimeOffset` for events. Use `ISchedulingConfiguration.DefaultTimeZoneId` until company-specific time zones exist.
- Expected business failures use `OperationResult`. Unexpected exceptions must not disclose details.
- Preserve unrelated uncommitted changes. Commit only files belonging to each completed task.
- Do not add tests, run builds, tests, application hosts, migrations, or `dotnet ef` commands without explicit user authorization.

---

### Task 1: Add the Maintenance Opportunity Aggregate and Persistence Mapping

**Files:**
- Create: `src/ServiceSync.Domain/Enums/MaintenanceOpportunityStatus.cs`
- Create: `src/ServiceSync.Domain/Entities/MaintenanceOpportunity.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Configurations/MaintenanceOpportunityConfiguration.cs`

**Interfaces:**
- Produces: `MaintenanceOpportunityStatus`, `MaintenanceOpportunity`, and `DbSet<MaintenanceOpportunity> MaintenanceOpportunities`.
- Consumed by: the repository, completion flow, scheduling flow, and maintenance-center queries.

- [ ] **Step 1: Define the stored commercial status**

```csharp
namespace ServiceSync.Domain.Enums;

public enum MaintenanceOpportunityStatus
{
    Pending,
    Contacted,
    BookingRequested,
    Booked,
    Completed,
    Dismissed
}
```

Do not add `DueSoon`; urgency is computed from the due date.

- [ ] **Step 2: Implement the protected aggregate**

```csharp
public sealed class MaintenanceOpportunity : ICompanyScopedEntity
{
    private MaintenanceOpportunity() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid EquipmentId { get; private set; }
    public Guid PreviousWorkOrderId { get; private set; }
    public Guid RecommendedServiceId { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public MaintenanceOpportunityStatus Status { get; private set; }
    public DateTimeOffset? ContactedAtUtc { get; private set; }
    public DateTimeOffset? BookingRequestedAtUtc { get; private set; }
    public DateTimeOffset? BookedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? DismissedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MaintenanceOpportunity Create(
        Guid companyId, Guid customerId, Guid equipmentId, Guid previousWorkOrderId,
        Guid recommendedServiceId, DateOnly dueDate, string notes, DateTimeOffset now);
    public void MarkAsContacted(DateTimeOffset now);
    public void MarkAsBookingRequested(DateTimeOffset now);
    public void MarkAsBooked(DateTimeOffset now);
    public void Complete(DateTimeOffset now);
    public void Dismiss(DateTimeOffset now);
}
```

Require every ID, normalize nonempty notes, store UTC timestamps, and throw `DomainException` for invalid transitions. Permit `Pending -> Contacted -> BookingRequested -> Booked -> Completed`; permit `Dismissed` from an open state; make repeated contact idempotent without replacing the first timestamp.

- [ ] **Step 3: Map the entity with company-scoped foreign keys**

```csharp
builder.ToTable("MaintenanceOpportunities");
builder.HasKey(x => x.Id);
builder.Property(x => x.Id).ValueGeneratedNever();
builder.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
builder.HasAlternateKey(x => new { x.CompanyId, x.Id })
    .HasName("AK_MaintenanceOpportunities_CompanyId_Id");
builder.HasIndex(x => new { x.CompanyId, x.Status, x.DueDate })
    .HasDatabaseName("IX_MaintenanceOpportunities_CompanyId_Status_DueDate");
builder.HasIndex(x => new { x.CompanyId, x.CustomerId })
    .HasDatabaseName("IX_MaintenanceOpportunities_CompanyId_CustomerId");
```

Add restrictive relationships to `Customer`, `Equipment`, `WorkOrder`, and `Service`, each using the principal `(CompanyId, Id)` key. Add the DbSet to `AppDbContext`.

- [ ] **Step 4: Verify the aggregate and mapping statically**

Run: `rg -n "MaintenanceOpportunity|public set;|DateTime\.Now|DateTime\.UtcNow" src/ServiceSync.Domain src/ServiceSync.Infrastructure/Persistence`

Expected: only private entity state setters and no direct clock call in new Domain/Application code.

- [ ] **Step 5: Commit the isolated change**

```bash
git add src/ServiceSync.Domain/Enums/MaintenanceOpportunityStatus.cs src/ServiceSync.Domain/Entities/MaintenanceOpportunity.cs src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs src/ServiceSync.Infrastructure/Persistence/Configurations/MaintenanceOpportunityConfiguration.cs
git commit -m "feat: add maintenance opportunity aggregate"
```

### Task 2: Add Tenant-Scoped Repository, DTOs, Authorization, and Queries

**Files:**
- Create: `src/ServiceSync.Application/Abstractions/Persistence/IMaintenanceOpportunityRepository.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs`
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityDtos.cs`
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityErrors.cs`
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityOwnerAuthorization.cs`
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/GetMaintenanceOpportunitiesUseCase.cs`
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/GetMaintenanceOpportunitySummaryUseCase.cs`
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/GetMaintenanceOpportunityForSchedulingUseCase.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`
- Modify: `src/ServiceSync.Infrastructure/DependencyInjection.cs`
- Modify: `src/ServiceSync.Web/Errors/OperationErrorMessages.cs`

**Interfaces:**
- Produces: owner-scoped opportunity list, summary, and schedule-context contracts.
- Consumed by: action use cases, the schedule, dashboard, and maintenance center.

- [ ] **Step 1: Define the repository contract and transaction boundary**

```csharp
public interface IMaintenanceOpportunityRepository
{
    Task AddAsync(MaintenanceOpportunity opportunity, CancellationToken cancellationToken = default);
    Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceOpportunityListItem>> GetListAsync(Guid companyId, GetMaintenanceOpportunitiesQuery query, DateOnly today, CancellationToken cancellationToken = default);
    Task<MaintenanceOpportunitySummary> GetSummaryAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken = default);
    Task<IMaintenanceOpportunityTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IMaintenanceOpportunityTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

The implementation uses the scoped `AppDbContext`. Direct entity loads must filter on both ID and company. List projections join only tenant-filtered customer, equipment, service, and work-order queryables.

- [ ] **Step 2: Define contracts and errors**

```csharp
public enum MaintenanceOpportunityUrgency { Overdue, NextSevenDays, NextThirtyDays, Future }
public sealed record GetMaintenanceOpportunitiesQuery(
    MaintenanceOpportunityStatus? Status,
    MaintenanceOpportunityUrgency? Urgency);
public sealed record MaintenanceOpportunitySummary(
    int OverdueCount, int NextSevenDaysCount, int NextThirtyDaysCount);
public sealed record MaintenanceOpportunityScheduleContext(
    Guid OpportunityId, Guid CustomerId, Guid EquipmentId, Guid RecommendedServiceId);
```

The list DTO includes opportunity ID, customer name and WhatsApp number, equipment description, prior completion timestamp, service name, due date, notes, status, and urgency. Define `InvalidInput`, `NotFound`, `InvalidStatus`, and `Forbidden` in `MaintenanceOpportunityErrors`, then add safe Portuguese messages.

- [ ] **Step 3: Calculate the business date once**

```csharp
var timeZone = TimeZoneInfo.FindSystemTimeZoneById(configuration.DefaultTimeZoneId);
var today = DateOnly.FromDateTime(
    TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
```

Use the same date in list and summary. Only `Pending`, `Contacted`, `BookingRequested`, and `Booked` are open. Overdue is before today; next seven is today through `today.AddDays(7)`; next thirty is today through `today.AddDays(30)`; later open items are future.

- [ ] **Step 4: Enforce owner authorization before query execution**

Implement the authorization helper using the current user, current company, and existing company-membership ownership pattern. Its method returns the company ID only for an owner; every maintenance query returns `Forbidden` otherwise. Apply it to list, summary, and schedule-context use cases.

- [ ] **Step 5: Register and statically verify**

Register the repository, authorization helper, and all three query use cases as scoped services.

Run: `rg -n "ServiceSync\.Web|GetForCompanyAsync\(|CompanyId" src/ServiceSync.Application/MaintenanceOpportunities src/ServiceSync.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs`

Expected: no Web reference in Application; all entity lookups use the company-filtered repository API.

- [ ] **Step 6: Commit the repository and query layer**

```bash
git add src/ServiceSync.Application/Abstractions/Persistence/IMaintenanceOpportunityRepository.cs src/ServiceSync.Application/MaintenanceOpportunities src/ServiceSync.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs src/ServiceSync.Application/DependencyInjection.cs src/ServiceSync.Infrastructure/DependencyInjection.cs src/ServiceSync.Web/Errors/OperationErrorMessages.cs
git commit -m "feat: add maintenance opportunity queries"
```

### Task 3: Complete Work Orders with an Optional Atomic Recommendation

**Files:**
- Modify: `src/ServiceSync.Application/WorkOrders/WorkOrderCommands.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/WorkOrderErrors.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/CompleteWorkOrderUseCase.cs`
- Modify: `src/ServiceSync.Web/Endpoints/WorkOrderEndpoints.cs`
- Modify: `src/ServiceSync.Web/Errors/OperationErrorMessages.cs`

**Interfaces:**
- Produces: optional `CompleteWorkOrderCommand` recommendation input and a single transaction for work-order completion plus opportunity creation.
- Consumes: the Task 2 repository and the existing service, authorization, photo, checklist, and appointment checks.

- [ ] **Step 1: Add the completion request records**

```csharp
public sealed record MaintenanceRecommendationInput(
    int? IntervalDays,
    DateOnly? DueDate,
    Guid RecommendedServiceId,
    string? Notes);

public sealed record CompleteWorkOrderCommand(
    MaintenanceRecommendationInput? MaintenanceRecommendation = null);
```

Exactly one of interval and date is required when the recommendation exists; interval must be positive; service ID and notes are required. Add `WorkOrderErrors.InvalidMaintenanceRecommendation` and its Portuguese error message.

- [ ] **Step 2: Validate the selected service before state mutation**

Load the recommended service with `GetForCompanyAsync`; require that it exists and is active. Preserve all current completion validation. Return an explicit invalid-recommendation result for all request-shape and service-selection failures.

- [ ] **Step 3: Persist both aggregate changes atomically**

```csharp
await using var transaction =
    await maintenanceOpportunityRepository.BeginTransactionAsync(cancellationToken);

order.Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, timeProvider.GetUtcNow());
await maintenanceOpportunityRepository.AddAsync(
    MaintenanceOpportunity.Create(scope.CompanyId, order.CustomerId, order.EquipmentId,
        order.Id, recommendation.RecommendedServiceId, dueDate, recommendation.Notes!,
        timeProvider.GetUtcNow()),
    cancellationToken);
await repository.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Calculate `dueDate` from the configured business date plus interval, or take the supplied explicit date. Use the old one-save completion path unchanged when no recommendation is supplied.

- [ ] **Step 4: Keep the endpoint body optional**

```csharp
group.MapPost("/{workOrderId:guid}/complete", async (
    Guid workOrderId,
    CompleteWorkOrderCommand? command,
    CompleteWorkOrderUseCase useCase,
    CancellationToken cancellationToken) =>
    ToOperationHttpResult(await useCase.ExecuteAsync(
        workOrderId, command ?? new CompleteWorkOrderCommand(), cancellationToken)));
```

Do not alter existing successful response payloads or company-member authorization.

- [ ] **Step 5: Verify the transaction and commit**

Run: `rg -n "MaintenanceRecommendation|BeginTransactionAsync|MaintenanceOpportunity\.Create|InvalidMaintenanceRecommendation" src/ServiceSync.Application/WorkOrders src/ServiceSync.Web/Endpoints/WorkOrderEndpoints.cs`

Expected: validation occurs before creation; both mutations are inside the transaction; no recommendation follows the existing completion behavior.

```bash
git add src/ServiceSync.Application/WorkOrders/WorkOrderCommands.cs src/ServiceSync.Application/WorkOrders/WorkOrderErrors.cs src/ServiceSync.Application/WorkOrders/CompleteWorkOrderUseCase.cs src/ServiceSync.Web/Endpoints/WorkOrderEndpoints.cs src/ServiceSync.Web/Errors/OperationErrorMessages.cs
git commit -m "feat: create maintenance opportunities from work orders"
```

### Task 4: Add Commercial Actions and Atomically Book from the Existing Schedule

**Files:**
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityActionUseCases.cs`
- Modify: `src/ServiceSync.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`
- Modify: `src/ServiceSync.Web/Endpoints/AppointmentEndpoints.cs`
- Create: `src/ServiceSync.Web/Endpoints/MaintenanceOpportunityEndpoints.cs`
- Modify: `src/ServiceSync.Web/Program.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`

**Interfaces:**
- Produces: owner action use cases, owner endpoints, and `CreateAppointmentCommand.MaintenanceOpportunityId`.
- Consumes: Task 2 repository and existing serializable appointment transaction.

- [ ] **Step 1: Implement focused action use cases**

Create `MarkMaintenanceOpportunityAsContactedUseCase`, `MarkMaintenanceOpportunityAsBookingRequestedUseCase`, `MarkMaintenanceOpportunityAsBookedUseCase`, `CompleteMaintenanceOpportunityUseCase`, and `DismissMaintenanceOpportunityUseCase`. Each: resolves owner access; loads by ID and company; returns `NotFound` or `Forbidden` explicitly; calls the matching entity method with `TimeProvider.GetUtcNow()`; converts a predictable invalid state to `InvalidStatus`; saves via the shared scoped context.

- [ ] **Step 2: Extend the appointment contract**

```csharp
public sealed record CreateAppointmentCommand(
    Guid ProfessionalId, DateOnly Date, TimeOnly StartTime, int DurationMinutes,
    Guid? CustomerId, Guid? EquipmentId, Guid? ServiceId, Guid? WorkOrderId,
    string? Notes, Guid? MaintenanceOpportunityId = null);
```

Extend the HTTP request by the same nullable ID. When it is present, `CreateAppointmentUseCase` requires owner access, loads the opportunity by company, requires `BookingRequested`, and replaces browser-provided customer/equipment/service values with the stored values.

- [ ] **Step 3: Reuse the existing scheduling transaction for booked state**

Inject `IMaintenanceOpportunityRepository` into `CreateAppointmentUseCase`. After the existing slot revalidation and appointment add, call `opportunity.MarkAsBooked(timeProvider.GetUtcNow())` before `TrySaveAppointmentChangesAsync`. The shared scoped DbContext persists both changes in `BeginSchedulingTransactionAsync`. A conflict or unavailable slot returns before booking and leaves the opportunity in `BookingRequested`.

- [ ] **Step 4: Create owner endpoints**

```csharp
var group = endpoints.MapGroup("/maintenance-opportunities")
    .RequireAuthorization(AuthorizationPolicies.CompanyOwner);

group.MapGet("/", async (MaintenanceOpportunityStatus? status,
    MaintenanceOpportunityUrgency? urgency, GetMaintenanceOpportunitiesUseCase useCase,
    CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(new(status, urgency), cancellationToken))
        .ToHttpResult(value => Results.Ok(value)));
group.MapGet("/summary", async (GetMaintenanceOpportunitySummaryUseCase useCase,
    CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(cancellationToken)).ToHttpResult(value => Results.Ok(value)));
group.MapGet("/{opportunityId:guid}/schedule-context", async (Guid opportunityId,
    GetMaintenanceOpportunityForSchedulingUseCase useCase, CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(value => Results.Ok(value)));
group.MapPost("/{opportunityId:guid}/contact", async (Guid opportunityId,
    MarkMaintenanceOpportunityAsContactedUseCase useCase, CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(() => Results.Ok()));
group.MapPost("/{opportunityId:guid}/booking-request", async (Guid opportunityId,
    MarkMaintenanceOpportunityAsBookingRequestedUseCase useCase, CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(() => Results.Ok()));
group.MapPost("/{opportunityId:guid}/complete", async (Guid opportunityId,
    CompleteMaintenanceOpportunityUseCase useCase, CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(() => Results.Ok()));
group.MapPost("/{opportunityId:guid}/dismiss", async (Guid opportunityId,
    DismissMaintenanceOpportunityUseCase useCase, CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, cancellationToken)).ToHttpResult(() => Results.Ok()));
```

Register `MapMaintenanceOpportunityEndpoints()` in `Program.cs`.

- [ ] **Step 5: Verify scheduling ownership and commit**

Run: `rg -n "MaintenanceOpportunityId|BookingRequested|MarkAsBooked|BeginSchedulingTransactionAsync|CompanyOwner" src/ServiceSync.Application/Scheduling src/ServiceSync.Web/Endpoints`

Expected: only owner-authorized contextual creation can book an opportunity, and the booked state is in the appointment transaction.

```bash
git add src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityActionUseCases.cs src/ServiceSync.Application/Scheduling/SchedulingContracts.cs src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs src/ServiceSync.Web/Endpoints/AppointmentEndpoints.cs src/ServiceSync.Web/Endpoints/MaintenanceOpportunityEndpoints.cs src/ServiceSync.Web/Program.cs src/ServiceSync.Application/DependencyInjection.cs
git commit -m "feat: schedule maintenance opportunities"
```

### Task 5: Build the Recommendation, Maintenance Center, Dashboard, and Navigation UI

**Files:**
- Modify: `src/ServiceSync.Web/Components/Shared/WorkOrderExecutionPanel.razor`
- Create: `src/ServiceSync.Web/Components/Pages/MaintenanceOpportunities.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Schedule.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Dashboard.razor`
- Modify: `src/ServiceSync.Web/Components/Layout/MainLayout.razor`
- Create: `src/ServiceSync.Web/MaintenanceOpportunities/MaintenanceOpportunityWhatsAppLinkBuilder.cs`

**Interfaces:**
- Produces: optional completion recommendation, owner center, secure schedule prefill, dashboard counts, navigation, and a reusable WhatsApp formatter.
- Consumes: all Task 2–4 use cases and DTOs.

- [ ] **Step 1: Add the optional recommendation panel**

Use `MudSwitch` labelled “Recomendar próxima manutenção?”. When it is enabled, render a required `MudRadioGroup` for 30/60/90/180/custom/date, exactly one required `MudNumericField<int>` or `MudDatePicker`, a required outlined service `MudSelect<Guid>`, and required outlined multiline `MudTextField` for notes. Load active services via `GetServicesUseCase`; default to `WorkOrder.ServiceId`. Build `CompleteWorkOrderCommand` only after local validation; retain server validation as the authority. Preserve progress flushing, action loading, and current success/error handling.

- [ ] **Step 2: Create the owner-protected central page**

```razor
@page "/manutencoes"
@attribute [Authorize(Policy = AuthorizationPolicies.CompanyOwner)]
@inject GetMaintenanceOpportunitiesUseCase GetMaintenanceOpportunitiesUseCase
@inject GetMaintenanceOpportunitySummaryUseCase GetMaintenanceOpportunitySummaryUseCase

<PageHeader Title="Manutenções"
            Description="Acompanhe clientes que podem gerar novos atendimentos." />
```

Load summary and list together. Use MudBlazor cards for “Vencidas”, “Nos próximos 7 dias”, and “Nos próximos 30 dias”; use status and urgency filters; show loading, empty, and responsive data states. Each item displays customer, equipment, previous maintenance, due date, recommended service, and a state chip. Render only valid actions; disable actions while saving; confirm dismissal with `MudDialog`; reload rows and counts after success.

- [ ] **Step 3: Isolate the WhatsApp URL construction**

```csharp
public static class MaintenanceOpportunityWhatsAppLinkBuilder
{
    public static string Create(string phoneNumber, string message) =>
        $"https://wa.me/{NormalizePhoneNumber(phoneNumber)}?text={Uri.EscapeDataString(message)}";
}
```

The page first calls `MarkMaintenanceOpportunityAsContactedUseCase`, refreshes state, then opens the generated link with `IJSRuntime.InvokeVoidAsync("open", link, "_blank")`. Build the Portuguese message from safe loaded DTO data; do not normalize phones or URL-encode in Razor.

- [ ] **Step 4: Prefill the existing schedule without trusting query values**

Add `[SupplyParameterFromQuery(Name = "maintenanceOpportunityId")] public Guid? MaintenanceOpportunityId { get; set; }` to `Schedule.razor`. When present, load `GetMaintenanceOpportunityForSchedulingUseCase`; populate and lock customer/equipment/service; clear work-order selection; leave professional, date, and slot selection editable. On creation, pass only `MaintenanceOpportunityId` to the command. On the central’s schedule action, await the booking-request use case then navigate to `/agenda?maintenanceOpportunityId={id}`.

- [ ] **Step 5: Add owner dashboard and menu visibility**

Add “Manutenções” to the owner branch in `MainLayout`. In the owner dashboard, load the summary with existing operational counts and show one compact linked MudBlazor block leading to `/manutencoes`. Do not alter the employee dashboard.

- [ ] **Step 6: Verify UI requirements and commit**

Run: `rg -n "@page \"/manutencoes\"|CompanyOwner|Mud(Dialog|Switch|RadioGroup|Select|DatePicker)|maintenanceOpportunityId|window.open" src/ServiceSync.Web/Components src/ServiceSync.Web/MaintenanceOpportunities`

Expected: the page is owner-protected, every new input is MudBlazor, dismissal uses a dialog, and the schedule query carries only the opportunity ID.

```bash
git add src/ServiceSync.Web/Components/Shared/WorkOrderExecutionPanel.razor src/ServiceSync.Web/Components/Pages/MaintenanceOpportunities.razor src/ServiceSync.Web/Components/Pages/Schedule.razor src/ServiceSync.Web/Components/Pages/Dashboard.razor src/ServiceSync.Web/Components/Layout/MainLayout.razor src/ServiceSync.Web/MaintenanceOpportunities/MaintenanceOpportunityWhatsAppLinkBuilder.cs
git commit -m "feat: add maintenance opportunity center"
```

### Task 6: Write the Migration and Activate the Internal-Error Boundary in Every Environment

**Files:**
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260824150000_AddMaintenanceOpportunities.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260824150000_AddMaintenanceOpportunities.Designer.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `src/ServiceSync.Web/Program.cs`
- Verify: `src/ServiceSync.Web/Errors/UnexpectedExceptionHandler.cs`

**Interfaces:**
- Produces: reversible database schema and generic RFC 7807 HTTP 500 responses for unexpected failures in every environment.

- [ ] **Step 1: Write the descriptive EF migration manually**

```csharp
migrationBuilder.CreateTable(
    name: "MaintenanceOpportunities",
    columns: table => new
    {
        Id = table.Column<Guid>(type: "uuid", nullable: false),
        CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
        CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
        EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
        PreviousWorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
        RecommendedServiceId = table.Column<Guid>(type: "uuid", nullable: false),
        DueDate = table.Column<DateOnly>(type: "date", nullable: false),
        Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
        Status = table.Column<int>(type: "integer", nullable: false),
        ContactedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
        BookingRequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
        BookedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
        CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
        DismissedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
        CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
        UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_MaintenanceOpportunities", x => x.Id);
        table.UniqueConstraint("AK_MaintenanceOpportunities_CompanyId_Id", x => new { x.CompanyId, x.Id });
        table.ForeignKey("FK_MaintenanceOpportunities_Customers_CompanyId_CustomerId", x => new { x.CompanyId, x.CustomerId }, "Customers", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
        table.ForeignKey("FK_MaintenanceOpportunities_Equipment_CompanyId_EquipmentId", x => new { x.CompanyId, x.EquipmentId }, "Equipment", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
        table.ForeignKey("FK_MaintenanceOpportunities_WorkOrders_CompanyId_PreviousWorkOrderId", x => new { x.CompanyId, x.PreviousWorkOrderId }, "WorkOrders", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
        table.ForeignKey("FK_MaintenanceOpportunities_Services_CompanyId_RecommendedServiceId", x => new { x.CompanyId, x.RecommendedServiceId }, "Services", new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.Restrict);
    });
migrationBuilder.CreateIndex("IX_MaintenanceOpportunities_CompanyId_Status_DueDate", "MaintenanceOpportunities", new[] { "CompanyId", "Status", "DueDate" });
migrationBuilder.CreateIndex("IX_MaintenanceOpportunities_CompanyId_CustomerId", "MaintenanceOpportunities", new[] { "CompanyId", "CustomerId" });
```

Write `Down` as `migrationBuilder.DropTable(name: "MaintenanceOpportunities");`. Update both the designer and snapshot to match the final configuration exactly. Do not execute a migration generator or a migration.

- [ ] **Step 2: Activate the registered exception handler middleware unconditionally**

```csharp
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
```

Place this before normal request middleware. Keep `AddExceptionHandler<UnexpectedExceptionHandler>()` and `AddProblemDetails()`. Confirm the handler logs the exception with trace ID, returns a generic Portuguese `ProblemDetails` title and `traceId` with status 500, and returns false for `OperationCanceledException`.

- [ ] **Step 3: Perform final static verification**

Run: `git diff --check`

Expected: no whitespace errors.

Run: `rg -n "UseExceptionHandler|exception\.Message|Exception\.Message|StackTrace|MaintenanceOpportunities|IX_MaintenanceOpportunities" src/ServiceSync.Web src/ServiceSync.Infrastructure/Persistence`

Expected: exception middleware is unconditional; raw exception details are absent from presentation; configuration, migration, and snapshot carry the same table/index names.

Run: `rg -n "MaintenanceOpportunity.*CompanyId|GetForCompanyAsync\(" src/ServiceSync.Application src/ServiceSync.Infrastructure`

Expected: opportunity entity reads are tenant-filtered and no request model exposes a company selector.

- [ ] **Step 4: Commit schema and exception boundary**

```bash
git add src/ServiceSync.Infrastructure/Persistence/Migrations src/ServiceSync.Web/Program.cs src/ServiceSync.Web/Errors/UnexpectedExceptionHandler.cs
git commit -m "feat: persist maintenance opportunities safely"
```

## Plan Review Checklist

- [ ] The plan implements every approved requirement: aggregate, mandatory optional recommendation, urgency counts, owner-only center/actions, WhatsApp, internal scheduling, dashboard, safe error boundary, and migration.
- [ ] Cross-task contracts agree on `MaintenanceOpportunity`, `MaintenanceOpportunityStatus`, `MaintenanceRecommendationInput`, `MaintenanceOpportunityId`, and `IMaintenanceOpportunityRepository`.
- [ ] No task introduces unapproved scope or requires a prohibited execution command.
- [ ] No task authorizes changes to unrelated local work.
