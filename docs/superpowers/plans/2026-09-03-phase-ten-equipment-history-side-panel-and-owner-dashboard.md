# Phase 10 Equipment History Side Panel and Owner Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (\`- [ ]\`) syntax for tracking.

**Goal:** Deliver an infinite-scroll equipment technical-history side panel and an Owner-only daily action and maintenance-value dashboard.

**Architecture:** Read-only, company-scoped EF Core projections reuse existing equipment, completed work-order, maintenance opportunity, customer-contact, appointment-request, appointment, quote, and service data. Application defines contracts, role authorization, and company-local period boundaries; Infrastructure executes focused queries; Razor components render DTOs and manage local loading state only.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor Web App, EF Core, PostgreSQL, MudBlazor, xUnit.

**Spec:** \`docs/superpowers/specs/2026-09-03-phase-ten-equipment-history-owner-dashboard-design.md\`

## Global Constraints

- Keep source identifiers, migrations, comments, logs, and technical documentation in English; only UI text is Brazilian Portuguese.
- Resolve \`CompanyId\` and Owner authorization from the authenticated context; never accept tenant identity from the browser.
- Apply \`CompanyId\` inside every database query before projecting equipment history or dashboard values.
- Use \`TimeProvider\` and \`ISchedulingConfiguration.DefaultTimeZoneId\` for business-day and current-month boundaries.
- Use MudBlazor components and the existing theme; do not add a UI library or another dependency.
- Recovered revenue is the sum of \`WorkOrder.TotalAmount\` for completed work orders traceable through \`SourceAppointmentId\` to an \`AppointmentRequest\` with \`MaintenanceOpportunityId\`; it is not payment or quote revenue.
- Potential revenue sums configured recommended-service base prices for open maintenance opportunities only; a missing base price is excluded.
- Do not create a migration, aggregate, analytics store, payment behavior, custom dashboard date filter, or synthetic relationship.
- Keep Employee dashboard behavior limited to its personal scheduling surface.

---

## File Structure

- Create \`src/VoltaMarcada.Application/Equipment/EquipmentHistoryContracts.cs\` and \`GetEquipmentHistoryUseCase.cs\` for the fixed-size paged technical-history read.
- Modify \`IEquipmentRepository.cs\`, \`EquipmentRepository.cs\`, and Application DI to expose and register that read.
- Create \`src/VoltaMarcada.Application/Dashboard/OwnerDashboardContracts.cs\`, \`IOwnerDashboardRepository.cs\`, and \`GetOwnerDashboardUseCase.cs\`.
- Create \`src/VoltaMarcada.Infrastructure/Persistence/Repositories/OwnerDashboardRepository.cs\`; register it in Infrastructure DI.
- Create \`src/VoltaMarcada.Web/Components/Shared/EquipmentHistoryDrawer.razor\` and \`wwwroot/js/equipment-history-scroll.js\`.
- Modify \`Equipment.razor\`, \`Dashboard.razor\`, and \`app.css\`.
- Create \`EquipmentHistoryTests.cs\`, \`OwnerDashboardTests.cs\`, and \`EquipmentHistoryAndDashboardComponentTests.cs\`.

### Task 1: Add the company-scoped paged equipment-history projection

**Files:**

- Create: \`src/VoltaMarcada.Application/Equipment/EquipmentHistoryContracts.cs\`
- Create: \`src/VoltaMarcada.Application/Equipment/GetEquipmentHistoryUseCase.cs\`
- Modify: \`src/VoltaMarcada.Application/Abstractions/Persistence/IEquipmentRepository.cs\`
- Modify: \`src/VoltaMarcada.Infrastructure/Persistence/Repositories/EquipmentRepository.cs\`
- Modify: \`src/VoltaMarcada.Application/DependencyInjection.cs\`
- Test: \`tests/VoltaMarcada.IntegrationTests/EquipmentHistoryTests.cs\`

**Consumes:** \`ICurrentCompany\`, \`IEquipmentRepository\`, completed \`WorkOrder\` records, and their existing child collections.

**Produces:** \`GetEquipmentHistoryUseCase.ExecuteAsync(Guid equipmentId, int offset, CancellationToken)\` returning \`OperationResult<EquipmentHistoryPageDto>\` in batches of exactly twenty items.

- [ ] **Step 1: Write the failing equipment-history integration tests.**

~~~csharp
[Fact]
public async Task ExecuteAsync_ReturnsTheFirstTwentyCompletedRecordsAndTotalForTheCurrentCompany()
{
    var result = await useCase.ExecuteAsync(equipmentId, offset: 0);

    Assert.True(result.IsSuccess);
    Assert.Equal(23, result.Value!.TotalRecordCount);
    Assert.Equal(20, result.Value.Records.Count);
    Assert.Equal(0, result.Value.Offset);
    Assert.Equal(20, result.Value.LoadedRecordCount);
    Assert.True(result.Value.HasMore);
}

[Fact]
public async Task ExecuteAsync_ReturnsTheNextBatchAndNeverDisclosesAnotherCompanyEquipment()
{
    var page = await useCase.ExecuteAsync(equipmentId, offset: 20);
    var inaccessible = await useCase.ExecuteAsync(otherCompanyEquipmentId, offset: 0);

    Assert.True(page.IsSuccess);
    Assert.Equal(3, page.Value!.Records.Count);
    Assert.False(page.Value.HasMore);
    Assert.False(inaccessible.IsSuccess);
}
~~~

Also seed two completed records with the same completion instant and assert the second sort key is work-order ID descending. Verify pending, in-progress, and cancelled orders are excluded.

- [ ] **Step 2: Run the focused test and verify RED.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryTests\`

Expected: FAIL because the contracts, use case, and repository projection do not exist.

- [ ] **Step 3: Define the explicit paged contracts and repository method.**

~~~csharp
public sealed record EquipmentHistoryPageDto(
    EquipmentHistoryHeaderDto Equipment,
    IReadOnlyList<EquipmentHistoryRecordDto> Records,
    int Offset,
    int TotalRecordCount)
{
    public int LoadedRecordCount => Offset + Records.Count;
    public bool HasMore => LoadedRecordCount < TotalRecordCount;
}

public sealed record EquipmentHistoryHeaderDto(
    Guid EquipmentId,
    string Description,
    string CustomerName,
    string? LocationDescription);

public sealed record EquipmentHistoryRecordDto(
    string WorkOrderDisplayNumber,
    string ServiceName,
    string? ProfessionalName,
    DateTimeOffset CompletedAtUtc,
    int RequiredChecklistCount,
    int CompletedChecklistCount,
    int BeforePhotoCount,
    int AfterPhotoCount,
    int AttachmentCount,
    decimal TotalAmount,
    string? ExecutionNotes,
    DateOnly? NextMaintenanceDueDate);

Task<EquipmentHistoryPageProjection?> GetHistoryPageForCompanyAsync(
    Guid equipmentId,
    Guid companyId,
    int offset,
    int pageSize,
    CancellationToken cancellationToken = default);
~~~

Validate \`offset >= 0\` in the use case, use a private \`const int PageSize = 20\`, and resolve the current company before querying. Return the existing explicit not-found operation error when the equipment is inaccessible.

- [ ] **Step 4: Implement one bounded EF Core projection.**

Use \`AsNoTracking()\` and filter the equipment by both identifiers before joining the current-company customer, completed work orders, services, optional professionals, checklist items, photos, attachments, and maintenance opportunities. Count children in SQL, order by \`CompletedAtUtc\` descending then work-order ID descending, apply \`Skip(offset).Take(pageSize)\`, and get the total from the same completed-work-order predicate. Reuse the existing work-order display-number formatter; do not expose IDs, keys, or tokens.

- [ ] **Step 5: Map and register the use case.**

Map the projection without materializing aggregate graphs or creating synthetic history. Add this registration beside the other equipment use cases:

~~~csharp
services.AddScoped<GetEquipmentHistoryUseCase>();
~~~

- [ ] **Step 6: Run the focused test and verify GREEN.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryTests\`

Expected: PASS for completion-only chronology, total count, stable batch order, and company isolation.

- [ ] **Step 7: Commit the projection deliverable.**

~~~powershell
git add src/VoltaMarcada.Application/Equipment src/VoltaMarcada.Application/Abstractions/Persistence/IEquipmentRepository.cs src/VoltaMarcada.Infrastructure/Persistence/Repositories/EquipmentRepository.cs src/VoltaMarcada.Application/DependencyInjection.cs tests/VoltaMarcada.IntegrationTests/EquipmentHistoryTests.cs
git commit -m "feat: add paged equipment history projection"
~~~

### Task 2: Add the Owner daily-action and maintenance-value projection

**Files:**

- Create: \`src/VoltaMarcada.Application/Dashboard/OwnerDashboardContracts.cs\`
- Create: \`src/VoltaMarcada.Application/Dashboard/IOwnerDashboardRepository.cs\`
- Create: \`src/VoltaMarcada.Application/Dashboard/GetOwnerDashboardUseCase.cs\`
- Create: \`src/VoltaMarcada.Infrastructure/Persistence/Repositories/OwnerDashboardRepository.cs\`
- Modify: \`src/VoltaMarcada.Application/DependencyInjection.cs\`
- Modify: \`src/VoltaMarcada.Infrastructure/DependencyInjection.cs\`
- Test: \`tests/VoltaMarcada.IntegrationTests/OwnerDashboardTests.cs\`

**Consumes:** \`MaintenanceOpportunityOwnerAuthorization\`, \`TimeProvider\`, \`ISchedulingConfiguration\`, and existing operational/commercial records.

**Produces:** \`GetOwnerDashboardUseCase.ExecuteAsync(CancellationToken)\` returning an Owner-only \`OperationResult<OwnerDashboardDto>\`.

- [ ] **Step 1: Write failing deterministic dashboard tests.**

~~~csharp
[Fact]
public async Task ExecuteAsync_ReturnsCurrentCompanyActionCountsAndMaintenanceFunnelForTheLocalMonth()
{
    var result = await ownerUseCase.ExecuteAsync();

    Assert.True(result.IsSuccess);
    Assert.Equal(4, result.Value!.Actions.TodayAppointmentCount);
    Assert.Equal(3, result.Value.Actions.OverdueOpportunityCount);
    Assert.Equal(5, result.Value.Actions.DueSoonOpportunityCount);
    Assert.Equal(2, result.Value.Actions.PendingAppointmentRequestCount);
    Assert.Equal(3, result.Value.Actions.QuotesAwaitingResponseCount);
    Assert.Equal(6, result.Value.Actions.OpenWorkOrderCount);
    Assert.Equal(18, result.Value.ValueFunnel.IdentifiedOpportunityCount);
    Assert.Equal(7, result.Value.ValueFunnel.ContactedCustomerCount);
    Assert.Equal(5, result.Value.ValueFunnel.MaintenanceRequestCount);
    Assert.Equal(4, result.Value.ValueFunnel.RecoveredBookingCount);
    Assert.Equal(2, result.Value.ValueFunnel.CompletedServiceCount);
    Assert.Equal(500m, result.Value.ValueFunnel.RecoveredRevenue);
}
~~~

Seed UTC values around the configured local midnight and month start, two sent contacts for one customer, a quote-only conversion, an unrelated completed work order, an eligible completed work order, and a second-company data set. Add a test that an Employee receives the existing forbidden outcome.

- [ ] **Step 2: Run the focused test and verify RED.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~OwnerDashboardTests\`

Expected: FAIL because the Owner projection does not exist.

- [ ] **Step 3: Define the dashboard contracts.**

~~~csharp
public sealed record OwnerDashboardDto(
    OwnerDashboardActionCounts Actions,
    OwnerMaintenanceValueFunnel ValueFunnel);

public sealed record OwnerDashboardActionCounts(
    int TodayAppointmentCount,
    int OverdueOpportunityCount,
    int DueSoonOpportunityCount,
    int PendingAppointmentRequestCount,
    int QuotesAwaitingResponseCount,
    int OpenWorkOrderCount);

public sealed record OwnerMaintenanceValueFunnel(
    int IdentifiedOpportunityCount,
    int ContactedCustomerCount,
    int MaintenanceRequestCount,
    int RecoveredBookingCount,
    int CompletedServiceCount,
    decimal RecoveredRevenue,
    int OpenOpportunityCount,
    decimal EstimatedPotentialRevenue);

public sealed record OwnerDashboardPeriod(
    DateTimeOffset TodayStartAtUtc,
    DateTimeOffset TomorrowStartAtUtc,
    DateTimeOffset MonthStartAtUtc,
    DateTimeOffset NextMonthStartAtUtc,
    DateOnly BusinessDate);

public interface IOwnerDashboardRepository
{
    Task<OwnerDashboardDto> GetAsync(
        Guid companyId,
        OwnerDashboardPeriod period,
        CancellationToken cancellationToken = default);
}
~~~

- [ ] **Step 4: Implement authorization and local period construction.**

Call \`GetOwnerCompanyIdAsync\`; return \`MaintenanceOpportunityErrors.Forbidden\` when it is null. Convert \`TimeProvider.GetUtcNow()\` with the configured timezone, calculate the local current day and current-month boundaries, and convert those boundaries back to UTC. Put this logic in the use case, not the component or repository.

- [ ] **Step 5: Implement the single read-only Owner projection.**

Filter every source set by \`CompanyId\`. Count appointments with a start in the local-day range; overdue opportunities by due date before the business date; actionable due-soon opportunities from the business date through seven days ahead; pending appointment requests; sent quotes with no decision; and pending or in-progress work orders.

For the local month, count created maintenance opportunities, distinct customer IDs on sent contacts, and appointment requests with \`MaintenanceOpportunityId\`. Count recovered bookings where an opportunity conversion type is \`Appointment\`. Trace completed services and revenue by joining a completed work order's \`SourceAppointmentId\` to an appointment request's \`AppointmentId\`, requiring a non-null \`MaintenanceOpportunityId\`; sum only those work-order totals. Do not substitute quotes, appointments by origin, customer IDs, or equipment IDs for the source relationship. For potential revenue, include open opportunities only and sum non-null recommended-service base prices.

- [ ] **Step 6: Register the use case and repository.**

~~~csharp
// VoltaMarcada.Application.DependencyInjection
services.AddScoped<GetOwnerDashboardUseCase>();

// VoltaMarcada.Infrastructure.DependencyInjection
services.AddScoped<IOwnerDashboardRepository, OwnerDashboardRepository>();
~~~

- [ ] **Step 7: Run the focused test and verify GREEN.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~OwnerDashboardTests\`

Expected: PASS for date boundaries, authorization, unique contact counting, tenant isolation, source traceability, recovered revenue, and nullable potential pricing.

- [ ] **Step 8: Commit the dashboard projection deliverable.**

~~~powershell
git add src/VoltaMarcada.Application/Dashboard src/VoltaMarcada.Application/DependencyInjection.cs src/VoltaMarcada.Infrastructure/Persistence/Repositories/OwnerDashboardRepository.cs src/VoltaMarcada.Infrastructure/DependencyInjection.cs tests/VoltaMarcada.IntegrationTests/OwnerDashboardTests.cs
git commit -m "feat: add owner maintenance value dashboard projection"
~~~

### Task 3: Build the lazy equipment-history drawer with infinite scrolling

**Files:**

- Create: \`src/VoltaMarcada.Web/Components/Shared/EquipmentHistoryDrawer.razor\`
- Create: \`src/VoltaMarcada.Web/wwwroot/js/equipment-history-scroll.js\`
- Modify: \`src/VoltaMarcada.Web/Components/Pages/Equipment.razor\`
- Modify: \`src/VoltaMarcada.Web/wwwroot/app.css\`
- Test: \`tests/VoltaMarcada.IntegrationTests/Components/EquipmentHistoryAndDashboardComponentTests.cs\`

**Consumes:** \`GetEquipmentHistoryUseCase\`, \`IComponentOperationExecutor\`, and Task 1 DTOs.

**Produces:** a right-anchored reusable panel that loads records only when opened or near the end of the scroll area.

- [ ] **Step 1: Write failing component-source tests.**

~~~csharp
[Fact]
public void EquipmentList_ProvidesAHistoryActionAndDrawer()
{
    var source = ReadSource("Components/Pages/Equipment.razor");

    Assert.Contains("EquipmentHistoryDrawer", source, StringComparison.Ordinal);
    Assert.Contains("Icons.Material.Filled.History", source, StringComparison.Ordinal);
}

[Fact]
public void EquipmentHistoryDrawer_UsesLazyLoadingAndAVisibleProgressCount()
{
    var source = ReadSource("Components/Shared/EquipmentHistoryDrawer.razor");

    Assert.Contains("GetEquipmentHistoryUseCase", source, StringComparison.Ordinal);
    Assert.Contains("MudProgressCircular", source, StringComparison.Ordinal);
    Assert.Contains("de @historyPage.TotalRecordCount registros", source, StringComparison.Ordinal);
    Assert.Contains("LoadNextPageAsync", source, StringComparison.Ordinal);
}
~~~

- [ ] **Step 2: Run the component test and verify RED.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests\`

Expected: FAIL because no drawer or history trigger exists.

- [ ] **Step 3: Implement the drawer state machine.**

~~~csharp
[Parameter] public Guid? EquipmentId { get; set; }
[Parameter] public bool Open { get; set; }
[Parameter] public EventCallback<bool> OpenChanged { get; set; }

private const int PageSize = 20;
private EquipmentHistoryPageDto? historyPage;
private bool isInitialLoading;
private bool isLoadingMore;
private string? loadError;

private async Task LoadNextPageAsync()
{
    if (EquipmentId is null || isInitialLoading || isLoadingMore ||
        historyPage is { HasMore: false })
    {
        return;
    }

    // Use offset 0 for the first request; otherwise use historyPage.LoadedRecordCount.
}
~~~

Use a temporary \`MudDrawer\` with \`Anchor.End\`, a close action, a centered initial \`MudProgressCircular\`, and an end-of-list circular progress indicator for the next batch. After the first successful response, display \`@historyPage.LoadedRecordCount de @historyPage.TotalRecordCount registros\`. Reset state on closing, equipment changes, and retry; display separate Portuguese empty and retryable error states.

- [ ] **Step 4: Add the bounded scroll helper.**

~~~javascript
export function isNearBottom(element, threshold) {
  return element.scrollTop + element.clientHeight >= element.scrollHeight - threshold;
}
~~~

Attach \`@onscroll\` to the timeline element. Import the module once through \`IJSRuntime\`, call \`isNearBottom(historyTimelineElement, 160)\`, and call \`LoadNextPageAsync\` only when it is true. The loading guards prevent duplicate requests. Dispose the imported module with the component.

- [ ] **Step 5: Connect the list history action and styles.**

Add a localized history \`MudIconButton\` in every equipment row. It must be outside the existing Owner-only edit/deactivate block, because all company members can read equipment. Store \`selectedEquipmentId\`, open the drawer, and retain all existing delete flow behavior. Add focused \`equipment-history-*\` styles for width, scrolling, record layout, loading/error/empty states, and mobile behavior using existing theme colors and spacing.

- [ ] **Step 6: Run the history tests and verify GREEN.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~EquipmentHistoryTests|FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests"\`

Expected: PASS for server pagination and the icon, lazy request, count label, spinner, infinite-scroll guard, error, and empty-state markup.

- [ ] **Step 7: Commit the drawer deliverable.**

~~~powershell
git add src/VoltaMarcada.Web/Components/Shared/EquipmentHistoryDrawer.razor src/VoltaMarcada.Web/Components/Pages/Equipment.razor src/VoltaMarcada.Web/wwwroot/js/equipment-history-scroll.js src/VoltaMarcada.Web/wwwroot/app.css tests/VoltaMarcada.IntegrationTests/Components/EquipmentHistoryAndDashboardComponentTests.cs
git commit -m "feat: add infinite equipment history drawer"
~~~

### Task 4: Render Owner daily actions and the monthly value funnel

**Files:**

- Modify: \`src/VoltaMarcada.Web/Components/Pages/Dashboard.razor\`
- Modify: \`src/VoltaMarcada.Web/wwwroot/app.css\`
- Modify: \`tests/VoltaMarcada.IntegrationTests/Components/EquipmentHistoryAndDashboardComponentTests.cs\`

**Consumes:** \`GetOwnerDashboardUseCase\`, \`OwnerDashboardDto\`, the existing owner calendar, and existing schedule-block alert.

**Produces:** concise Owner-only daily-action and maintenance-results sections above the weekly calendar.

- [ ] **Step 1: Extend the failing dashboard component test.**

~~~csharp
[Fact]
public void Dashboard_UsesTheOwnerProjectionAndKeepsCommercialValuesOwnerOnly()
{
    var source = ReadSource("Components/Pages/Dashboard.razor");

    Assert.Contains("GetOwnerDashboardUseCase", source, StringComparison.Ordinal);
    Assert.Contains("O que precisa da sua atenção hoje", source, StringComparison.Ordinal);
    Assert.Contains("Receita recuperada", source, StringComparison.Ordinal);
    Assert.Contains("if (isOwner)", source, StringComparison.Ordinal);
}
~~~

- [ ] **Step 2: Run the component test and verify RED.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests\`

Expected: FAIL because the Owner projection and new UI sections are absent.

- [ ] **Step 3: Replace scattered Owner KPI loading with the projection.**

Inject \`GetOwnerDashboardUseCase\`. In \`LoadAsync\`, invoke it only when \`isOwner\` and store one \`OwnerDashboardDto?\`. Replace the current external-request, open-work-order, and maintenance-summary fetches with it. Preserve existing appointment-range loading and pending schedule-block request retrieval because they supply calendar rendering and the separate schedule-block alert.

- [ ] **Step 4: Render six action cards.**

Before the calendar, render clickable MudBlazor cards for today appointments, overdue maintenance, due-soon maintenance, pending requests, quotes awaiting response, and open work orders. Link to \`/agenda\`, \`/manutencoes\`, \`/orcamentos\`, or \`/ordens-servico\` as applicable. Keep zero neutral, use warning semantics for due-soon and pending values, and error semantics for overdue values. Render nothing from this group for Employees.

- [ ] **Step 5: Render the maintenance results section.**

Render identified opportunities, distinct contacted customers, maintenance-originated requests, recovered bookings, completed services, recovered revenue, and open opportunities with estimated potential revenue from \`ownerDashboard.ValueFunnel\`. Use \`IBrazilianDisplayFormatter\` for currency and clearly label recovered revenue as work-order value, not payment. Do not add a date picker; label the section as the current month.

- [ ] **Step 6: Complete responsive styles and error behavior.**

Add focused action-grid and value-funnel CSS classes with responsive one- and two-column layout. Retain the established dashboard loading/error pattern. If the Owner projection returns a business failure, show the localized error state and do not render stale data. Employee daily scheduling remains unchanged.

- [ ] **Step 7: Run the dashboard tests and verify GREEN.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~OwnerDashboardTests|FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests"\`

Expected: PASS for markup, role isolation, calculation semantics, and value labels.

- [ ] **Step 8: Commit the dashboard UI deliverable.**

~~~powershell
git add src/VoltaMarcada.Web/Components/Pages/Dashboard.razor src/VoltaMarcada.Web/wwwroot/app.css tests/VoltaMarcada.IntegrationTests/Components/EquipmentHistoryAndDashboardComponentTests.cs
git commit -m "feat: add owner action and value dashboard"
~~~

### Task 5: Perform release-quality verification without a migration

**Files:**

- Test: \`tests/VoltaMarcada.IntegrationTests/EquipmentHistoryTests.cs\`
- Test: \`tests/VoltaMarcada.IntegrationTests/OwnerDashboardTests.cs\`
- Test: \`tests/VoltaMarcada.IntegrationTests/Components/EquipmentHistoryAndDashboardComponentTests.cs\`

**Consumes:** Tasks 1 through 4.

**Produces:** verified migration-free Phase 10 behavior.

- [ ] **Step 1: Verify no schema artifact was introduced.**

Run: \`rg --files src/VoltaMarcada.Infrastructure/Persistence/Migrations | rg "PhaseTen|EquipmentHistory|OwnerDashboard"\`

Expected: no output.

- [ ] **Step 2: Run all Phase 10 tests.**

Run: \`dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~EquipmentHistoryTests|FullyQualifiedName~OwnerDashboardTests|FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests"\`

Expected: PASS.

- [ ] **Step 3: Run the complete solution test suite.**

Run: \`dotnet test VoltaMarcada.sln --no-restore\`

Expected: PASS with no scheduling, work-order, public-booking, quote, or authorization regression.

- [ ] **Step 4: Run static safety checks.**

Run: \`git diff --check\`

Expected: no whitespace error. Inspect each changed query for company predicates, direct clocks in Application/Domain, storage-key exposure, and commercial values rendered outside the Owner branch.

- [ ] **Step 5: Commit a roadmap update only if it is an actual change.**

~~~powershell
git add VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md
git commit -m "docs: mark phase ten delivery complete"
~~~

Run this only when the existing roadmap uses an explicit completion marker and the file was changed; do not create an empty commit.

