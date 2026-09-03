# Phase 10 Equipment History and Value Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn equipment records and operational activity into an owner dashboard that shows today’s work and the maintenance-recurrence value funnel.

**Architecture:** Read-only, company-scoped projections compose existing work orders, checklist progress, photos, maintenance opportunities, service requests, quotes, and appointments. Application owns projection orchestration and business-date calculations; Infrastructure executes bounded EF Core queries; Razor components render DTOs only.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core, PostgreSQL, MudBlazor, xUnit.

**Spec:** `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md` (Phase 10).

## Global Constraints

- Keep all source identifiers, migrations, comments, and technical documentation in English; use Brazilian Portuguese only for UI text.
- Every query resolves `CompanyId` from authenticated context and filters it in the database query.
- Use `TimeProvider` and company scheduling time-zone rules; do not call direct clocks in Domain or Application.
- Preserve existing work-order history, public booking, quote, and maintenance lifecycle behavior.
- Do not add analytics services, data warehouses, third-party dependencies, or an aggregate solely for dashboard totals.

---

## File Structure

- Create `Application/Equipment/EquipmentHistoryContracts.cs` and `GetEquipmentHistoryUseCase.cs` for the technical equipment timeline.
- Create `Application/Dashboard/OwnerDashboardContracts.cs` and `GetOwnerDashboardUseCase.cs` for action and value projections.
- Extend `IEquipmentRepository`, `IMaintenanceOpportunityRepository`, `IQuoteRepository`, `IServiceRequestRepository`, and their EF repositories only with focused company-scoped read projections.
- Modify `EquipmentRegistration.razor` and `Dashboard.razor` to render the DTOs with existing formatting services.
- Create integration/component tests for isolation, chronology, and the dashboard states.

### Task 1: Define and persist read projections for equipment history

**Files:**
- Create: `src/VoltaMarcada.Application/Equipment/EquipmentHistoryContracts.cs`
- Create: `src/VoltaMarcada.Application/Equipment/GetEquipmentHistoryUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IEquipmentRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/EquipmentRepository.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/EquipmentHistoryTests.cs`

**Produces:** `EquipmentHistoryDto`, ordered `EquipmentServiceHistoryItemDto`, and `GetEquipmentHistoryUseCase.ExecuteAsync(Guid equipmentId, CancellationToken)`.

- [ ] **Step 1: Write failing integration tests**

```csharp
[Fact]
public async Task GetEquipmentHistory_ReturnsOnlyCurrentCompanyChronologicalTechnicalRecords()
{
    var result = await useCase.ExecuteAsync(equipmentId);

    Assert.NotNull(result);
    Assert.Equal(equipmentId, result!.EquipmentId);
    Assert.Collection(result.ServiceHistory,
        first => Assert.Equal("OS #000042", first.WorkOrderDisplayNumber),
        second => Assert.Equal("OS #000043", second.WorkOrderDisplayNumber));
    Assert.Null(await useCase.ExecuteAsync(otherCompanyEquipmentId));
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryTests`

Expected: FAIL because the history DTO, query, and use case do not exist.

- [ ] **Step 3: Implement one bounded repository projection**

Add a repository method that loads the requested equipment by both ID and current company, then projects completed work orders linked to it in descending completion date. Each item includes work-order display number, completed date, service name, professional display name, required/completed checklist totals, before/after photo counts, order total, and linked future maintenance opportunity due date. Do not expose storage keys, attachment keys, tokens, internal IDs, or unbounded notes.

- [ ] **Step 4: Implement the use case**

Resolve `ICurrentCompany`, return `null` for an inaccessible equipment record, and map the repository projection without creating synthetic history for old work orders. Use existing display-number formatting and return an empty history list when no completed work order exists.

- [ ] **Step 5: Run the focused test and confirm GREEN**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryTests`

Expected: PASS, including cross-company access and descending chronology.

### Task 2: Build the owner action and value dashboard projection

**Files:**
- Create: `src/VoltaMarcada.Application/Dashboard/OwnerDashboardContracts.cs`
- Create: `src/VoltaMarcada.Application/Dashboard/GetOwnerDashboardUseCase.cs`
- Modify: focused repository contracts and EF repositories listed in File Structure
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/OwnerDashboardTests.cs`

**Produces:** `OwnerDashboardDto` with today’s appointments, overdue/due-soon opportunities, pending service requests, quotes awaiting response, and a maintenance-conversion funnel.

- [ ] **Step 1: Write failing dashboard projection tests**

```csharp
[Fact]
public async Task GetOwnerDashboard_CalculatesActionCountsAndRecoveryFunnelForCurrentCompanyOnly()
{
    var dashboard = await useCase.ExecuteAsync();

    Assert.Equal(4, dashboard.TodayAppointmentCount);
    Assert.Equal(3, dashboard.OverdueOpportunityCount);
    Assert.Equal(2, dashboard.PendingServiceRequestCount);
    Assert.Equal(3, dashboard.QuotesAwaitingResponseCount);
    Assert.Equal(27, dashboard.ValueFunnel.BookedCount);
    Assert.Equal(8450m, dashboard.ValueFunnel.RecoveredRevenue);
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~OwnerDashboardTests`

Expected: FAIL because no composed owner dashboard exists.

- [ ] **Step 3: Implement deterministic scoped calculations**

Use the configured company-local date. Count only actionable records: appointments on the local day, overdue and due-soon maintenance opportunities, pending public service requests, and sent quotes still awaiting a customer decision. Build funnel counts from maintenance opportunities and their conversion links: identified, contacted, request-originated, booked, completed, and recovered revenue. Recovered revenue is the sum of completed work-order totals linked by the maintenance conversion, never a quote amount or a potential estimate.

- [ ] **Step 4: Register and verify the use case**

Register `GetOwnerDashboardUseCase` in Application DI. Confirm every repository query filters by company before projection and no dashboard operation changes domain state.

- [ ] **Step 5: Run the focused test and confirm GREEN**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~OwnerDashboardTests`

Expected: PASS for local-day boundaries, empty states, excluded other-company data, and each funnel transition.

### Task 3: Render actionable UI surfaces

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/EquipmentRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Dashboard.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/EquipmentHistoryAndDashboardComponentTests.cs`

**Produces:** a technical history section on equipment detail and a concise Owner dashboard; employee dashboard behavior remains distinct.

- [ ] **Step 1: Write failing component-source tests**

```csharp
[Fact]
public void Dashboard_ShouldRenderActionCountsAndMaintenanceValueFunnel()
{
    var source = ReadSource("Components/Pages/Dashboard.razor");
    Assert.Contains("GetOwnerDashboardUseCase", source, StringComparison.Ordinal);
    Assert.Contains("Manutenções", source, StringComparison.Ordinal);
    Assert.Contains("Receita recuperada", source, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the focused test and confirm RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests`

Expected: FAIL because neither surface consumes the new DTOs.

- [ ] **Step 3: Implement the equipment history panel**

Render the equipment identity and one compact item per completed work order: local date, service, OS number, technician, checklist completion, before/after evidence counts, value, and next maintenance date when present. Render a Portuguese empty state and retain all existing equipment edit behavior.

- [ ] **Step 4: Implement the Owner dashboard**

Render the “what needs attention today” blocks before the value funnel. Link counts to the existing agenda, maintenance, requests, and quotes pages; present potential revenue as a distinct future measure and recovered revenue as realized revenue. Do not expose owner financial metrics to employees.

- [ ] **Step 5: Run component and full verification**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests`

Then run: `dotnet test VoltaMarcada.sln --no-restore`

Expected: PASS with no cross-role regression.

### Task 4: Review migration-free delivery

**Files:**
- Modify: `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md`
- Modify: `Planos negócio/VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md`

- [ ] **Step 1: Confirm no schema change is necessary**

Verify that every required datum already exists in work orders, checklist items, photos, opportunities, appointments, requests, and quotes. If a missing conversion link prevents deterministic recovered-revenue calculation, stop and write a separate design before adding persistence.

- [ ] **Step 2: Run static safety review**

Run: `git diff --check`

Inspect every new query for `CompanyId` filtering, direct-clock calls, N+1 loading, storage-key exposure, and employee access to Owner-only metrics.

- [ ] **Step 3: Commit the independent deliverable**

```powershell
git add src tests VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md "Planos negócio/VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md"
git commit -m "feat: add equipment history and owner value dashboard"
```