# Manual Maintenance Opportunities Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let an owner create a maintenance opportunity for an existing customer and equipment without a prior work order.

**Architecture:** `MaintenanceOpportunity` remains the only aggregate. Its prior-work-order reference is nullable only for the manual creation path; work-order completion keeps supplying it. A dedicated application use case validates company-scoped customer, equipment, and service data; a focused form reuses the existing maintenance-recommendation input pattern.

**Tech Stack:** .NET 10, C#, ASP.NET Core minimal APIs, Blazor Server, EF Core 10, PostgreSQL, xUnit, MudBlazor.

**Spec:** `docs/superpowers/specs/2026-09-09-manual-maintenance-opportunities-design.md`

## Global Constraints

- Code, migrations, tests, and technical text are English; UI text is Brazilian Portuguese.
- Resolve company identity from the authenticated owner; never accept `CompanyId` from the browser.
- Customer, equipment, service, notes, and exactly one due-date strategy are required; equipment must belong to customer.
- Manual opportunities have no prior work order; work-order completion continues to require one.
- The collection POST needs Owner plus operational-license authorization.
- Use `TimeProvider`, the configured scheduling time zone, `OperationResult`, and cancellation tokens.
- Preserve company filters and do not expose a work-order reference on public booking pages.
- Follow test-first red-green-refactor for every production behavior.

---

### Task 1: Persist manual opportunities with no prior work order

**Files:**
- Modify: `src/VoltaMarcada.Domain/Entities/MaintenanceOpportunity.cs`
- Modify: `src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityDtos.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/MaintenanceOpportunityConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_MakeMaintenanceOpportunityPreviousWorkOrderOptional.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityLifecycleTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityManualPersistenceTests.cs`

**Produces:** `Guid? PreviousWorkOrderId`, `CreateManual(...)`, and a nullable list projection completion timestamp.

- [ ] **Step 1: Write failing domain and projection tests**

```csharp
[Fact]
public void CreateManual_creates_pending_opportunity_without_previous_work_order()
{
    var opportunity = MaintenanceOpportunity.CreateManual(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        new DateOnly(2026, 10, 1), "Annual filter replacement", Now);

    Assert.Null(opportunity.PreviousWorkOrderId);
    Assert.Equal(MaintenanceOpportunityStatus.Pending, opportunity.Status);
}

[Fact]
public async Task GetListAsync_returns_null_history_for_manual_opportunity()
{
    await fixture.AddManualOpportunityAsync();
    var item = Assert.Single(await fixture.Repository.GetListAsync(
        fixture.CompanyId, new(null, null), new DateOnly(2026, 9, 9)));
    Assert.Null(item.PreviousWorkOrderCompletedAtUtc);
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~MaintenanceOpportunityLifecycleTests|FullyQualifiedName~MaintenanceOpportunityManualPersistenceTests"`

Expected: compile failure because the manual factory and nullable projection do not exist.

- [ ] **Step 3: Implement the smallest nullable model change**

```csharp
public Guid? PreviousWorkOrderId { get; private set; }

public static MaintenanceOpportunity CreateManual(
    Guid companyId, Guid customerId, Guid equipmentId, Guid recommendedServiceId,
    DateOnly dueDate, string notes, DateTimeOffset now) =>
    Create(companyId, customerId, equipmentId, null, recommendedServiceId, dueDate, notes, now);
```

Change the internal factory input to `Guid?`; validate it only when present.
Keep the existing completion call passing `order.Id`. Configure the property and
relationship as optional. In `GetListAsync`, replace the work-order inner join
with a left join, project `DateTimeOffset? PreviousWorkOrderCompletedAtUtc`,
and remove the fallback to `CreatedAtUtc`.

Generate `MakeMaintenanceOpportunityPreviousWorkOrderOptional` through EF;
only alter the nullable column/FK and update the snapshot.

- [ ] **Step 4: Verify GREEN and schema**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~MaintenanceOpportunityLifecycleTests|FullyQualifiedName~MaintenanceOpportunityManualPersistenceTests"`

Run: `dotnet ef migrations has-pending-model-changes --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web`

Expected: all focused tests pass and EF reports no pending model changes.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: allow maintenance opportunities without work orders"`

### Task 2: Add the owner-scoped manual creation use case

**Files:**
- Create: `src/VoltaMarcada.Application/MaintenanceOpportunities/CreateManualMaintenanceOpportunityUseCase.cs`
- Modify: `src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityDtos.cs`
- Modify: `src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityErrors.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/CompleteWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityManualCreationTests.cs`

**Consumes:** `MaintenanceOpportunityOwnerAuthorization`, customer/equipment/service repositories, `MaintenanceRecommendationInput`, scheduling configuration, and `TimeProvider`.

**Produces:** `CreateManualMaintenanceOpportunityUseCase.ExecuteAsync(CreateManualMaintenanceOpportunityCommand, CancellationToken)`.

- [ ] **Step 1: Write failing validation tests**

```csharp
[Fact]
public async Task ExecuteAsync_creates_manual_opportunity_for_current_company()
{
    var result = await fixture.UseCase.ExecuteAsync(
        new(fixture.CustomerId, fixture.EquipmentId,
            new(90, null, fixture.ServiceId, "Annual maintenance")));

    Assert.True(result.IsSuccess);
    Assert.Null(Assert.Single(fixture.Repository.Added).PreviousWorkOrderId);
}

[Fact]
public async Task ExecuteAsync_rejects_equipment_owned_by_another_customer()
{
    var result = await fixture.UseCase.ExecuteAsync(
        new(fixture.CustomerId, fixture.OtherCustomerEquipmentId,
            new(null, new DateOnly(2026, 12, 1), fixture.ServiceId, "Annual maintenance")));

    Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
}
```

Add separate tests for non-owner, cross-company records, inactive service,
blank notes, and invalid interval/date combinations. These must fail if any
company-scoped lookup or equipment/customer comparison is removed.

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~MaintenanceOpportunityManualCreationTests`

Expected: compile failure because the command and use case do not exist.

- [ ] **Step 3: Implement the use case and shared date resolver**

```csharp
public sealed record CreateManualMaintenanceOpportunityCommand(
    Guid CustomerId, Guid EquipmentId, MaintenanceRecommendationInput Recommendation);

var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, companyId!.Value, cancellationToken);
var equipment = await equipmentRepository.GetForCompanyAsync(command.EquipmentId, companyId.Value, cancellationToken);
var service = await serviceRepository.GetForCompanyAsync(command.Recommendation.RecommendedServiceId, companyId.Value, cancellationToken);
if (customer is null || equipment is null || equipment.CustomerId != customer.Id || service is null || !service.IsActive)
    return OperationResult<Guid>.Failure(MaintenanceOpportunityErrors.InvalidInput);
```

Return `Forbidden` when there is no owner company. Extract the current private
due-date validation from `CompleteWorkOrderUseCase` into one focused helper and
use it from both flows, so custom intervals and explicit dates keep identical
business-time-zone semantics. Add `InvalidInput` returns for invalid notes and
dates; persist through `IMaintenanceOpportunityRepository` and register the
use case in `AddApplication`.

- [ ] **Step 4: Verify GREEN and existing OS completion**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~MaintenanceOpportunityManualCreationTests|FullyQualifiedName~CompleteWorkOrder"`

Expected: manual validation passes and existing completion tests still observe
a nonempty previous work-order ID.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: create maintenance opportunities manually"`

### Task 3: Add the protected API boundary

**Files:**
- Modify: `src/VoltaMarcada.Web/Endpoints/MaintenanceOpportunityEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityManualEndpointsTests.cs`

**Produces:** `POST /maintenance-opportunities` returning 201 with `opportunityId`.

- [ ] **Step 1: Write failing endpoint tests**

```csharp
[Fact]
public async Task Post_creates_manual_opportunity_for_operational_owner()
{
    var response = await fixture.PostAsync(fixture.OwnerClient, fixture.ValidRequest);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Null((await fixture.GetStoredOpportunityAsync()).PreviousWorkOrderId);
}

[Theory]
[InlineData(CompanyUserRole.Employee, CompanyLicenseAccessMode.Operational, HttpStatusCode.Forbidden)]
[InlineData(CompanyUserRole.Owner, CompanyLicenseAccessMode.OwnerReadOnly, HttpStatusCode.Forbidden)]
public async Task Post_requires_owner_and_operational_license(CompanyUserRole role, CompanyLicenseAccessMode access, HttpStatusCode expected)
{
    Assert.Equal(expected, (await fixture.PostAsync(fixture.CreateClient(role, access), fixture.ValidRequest)).StatusCode);
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~MaintenanceOpportunityManualEndpointsTests`

Expected: 404 because the collection POST route is absent.

- [ ] **Step 3: Map the command with existing result handling**

```csharp
group.MapPost("/", async (CreateManualMaintenanceOpportunityCommand command,
    CreateManualMaintenanceOpportunityUseCase useCase, CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(command, cancellationToken)).ToHttpResult(
        opportunityId => Results.Created($"/maintenance-opportunities/{opportunityId}", new { opportunityId })))
    .RequireOperationalCompanyLicense();
```

Keep the group Owner policy and add the localized invalid-input message if it is
not already mapped. Do not return a cross-company existence detail.

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~MaintenanceOpportunityManualEndpointsTests`

Expected: operational owner gets 201; employee/read-only owner are denied;
invalid inputs return validation problems.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: expose manual maintenance opportunity creation"`

### Task 4: Build the manual owner experience and correct history display

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Shared/ManualMaintenanceOpportunityForm.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/MaintenanceOpportunities.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/ManualMaintenanceOpportunityFormTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/MaintenanceOpportunitiesRenderingTests.cs`

**Consumes:** customer, equipment, service lookup use cases and the manual creation use case.

**Produces:** **Nova oportunidade** action with customer-dependent equipment and an explicit **Sem OS anterior** state.

- [ ] **Step 1: Write failing component tests**

```csharp
[Fact]
public async Task Form_renders_required_manual_opportunity_fields()
{
    var html = await renderer.RenderAsync<ManualMaintenanceOpportunityForm>();
    Assert.Contains("Cliente", html, StringComparison.Ordinal);
    Assert.Contains("Equipamento", html, StringComparison.Ordinal);
    Assert.Contains("Serviço recomendado", html, StringComparison.Ordinal);
    Assert.Contains("Data recomendada", html, StringComparison.Ordinal);
}

[Fact]
public async Task Page_renders_manual_history_label_when_work_order_is_missing()
{
    var html = await renderer.RenderAsync<MaintenanceOpportunities>(manualOpportunity);
    Assert.Contains("Sem OS anterior", html, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~ManualMaintenanceOpportunityFormTests|FullyQualifiedName~MaintenanceOpportunitiesRenderingTests"`

Expected: form discovery fails and page lacks manual-history label.

- [ ] **Step 3: Implement the focused form**

```razor
<MudButton Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" OnClick="OpenCreateDialog">
    Nova oportunidade
</MudButton>

@if (item.PreviousWorkOrderCompletedAtUtc is { } completedAtUtc)
{
    <MudText Typo="Typo.caption">Última manutenção: @FormatDate(completedAtUtc)</MudText>
}
else
{
    <MudText Typo="Typo.caption">Sem OS anterior</MudText>
}
```

The child form loads customers and active services on open. On customer change,
load active equipment with `GetCustomerEquipmentUseCase` and clear any stale
equipment. Offer 30/60/90/180-day shortcuts, custom interval, and explicit
date; submit only one strategy. On success, close the dialog, show feedback,
and invoke a parent reload callback. Keep server validation authoritative.

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~ManualMaintenanceOpportunityFormTests|FullyQualifiedName~MaintenanceOpportunitiesRenderingTests|FullyQualifiedName~MaintenanceOpportunityManualEndpointsTests"`

Expected: required controls and manual-history state render; protected endpoint
still enforces the mutation rule.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: add manual maintenance opportunity form"`

### Task 5: Regression-test every downstream opportunity flow

**Files:**
- Modify: `tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityAppointmentTests.cs`
- Modify: `tests/VoltaMarcada.IntegrationTests/QuoteMaintenanceOpportunityTests.cs`
- Modify: `tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityLifecycleTests.cs`
- Modify: `tests/VoltaMarcada.IntegrationTests/PublicMaintenanceBookingTests.cs`

**Produces:** Evidence that manual opportunities retain contact, booking link,
appointment, quote, and discard behavior without an inner-work-order assumption.

- [ ] **Step 1: Write failing manual-origin regression tests**

```csharp
[Fact]
public async Task CreateAsync_converts_manual_opportunity_to_service_appointment()
{
    var opportunity = await fixture.AddManualOpportunityAsync();
    var result = await fixture.CreateAppointmentFromOpportunityAsync(opportunity.Id);
    Assert.True(result.IsSuccess);
    Assert.Equal(MaintenanceOpportunityStatus.Converted, (await fixture.GetOpportunityAsync(opportunity.Id)).Status);
}

[Fact]
public async Task GenerateMaintenanceBookingLink_supports_manual_opportunity()
{
    var opportunity = await fixture.AddManualOpportunityAsync();
    var result = await fixture.GenerateBookingLinkAsync(opportunity.Id);
    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
}
```

Add equivalent manual-origin assertions for quote conversion, WhatsApp contact
preparation, and `CloseWithoutConversion`; assert observable status, converted
ID, or contact history rather than mock calls.

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~MaintenanceOpportunityAppointmentTests|FullyQualifiedName~QuoteMaintenanceOpportunityTests|FullyQualifiedName~PublicMaintenanceBookingTests|FullyQualifiedName~MaintenanceOpportunityLifecycleTests"`

Expected: any unreviewed inner join or nonnullable DTO assumption fails by
excluding the manual opportunity.

- [ ] **Step 3: Correct only proven nullable-history assumptions**

Trace each failure to a `PreviousWorkOrderId` dependency. Preserve customer,
equipment, service, company, lifecycle, authorization, and token predicates;
never create a fallback work order or weaken the completion flow.

- [ ] **Step 4: Verify full release state**

Run: `dotnet test VoltaMarcada.sln --no-restore`

Run: `dotnet ef migrations has-pending-model-changes --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web`

Run: `git diff --check`

Expected: all tests pass, EF finds no pending model changes, and the diff has no whitespace errors.

- [ ] **Step 5: Commit**

Run: `git add tests && git commit -m "test: cover manual maintenance opportunity flows"`

## Plan self-review

- Task 1 covers nullable aggregate, migration, and correct list history.
- Task 2 covers owner/company validation and shared interval/date behavior.
- Task 3 covers HTTP authorization and errors.
- Task 4 covers manual creation UX and display behavior.
- Task 5 covers contact, public booking, appointment, quote, and discard regressions.
- All named types are introduced before their consumers; no placeholder or deferred requirement remains.
