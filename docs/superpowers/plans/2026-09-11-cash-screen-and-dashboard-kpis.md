# Cash Screen and Dashboard Financial KPIs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move financial operations to an owner-only daily Cash page with manual entries, automatic work-order charges, and dashboard financial KPIs.

**Architecture:** Generalize the existing work-order charge persistence so a charge can have an optional work-order link and a business date. Application use cases expose daily and all-pending cash queries plus owner-only mutations. The work-order completion flow creates one pending charge automatically; the web layer removes all financial controls from the work-order panel and exposes Cash plus dashboard links.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor Web App, EF Core, PostgreSQL, ASP.NET Core Identity, xUnit, MudBlazor.

**Spec:** `docs/superpowers/specs/2026-09-11-cash-screen-design.md`

## Global Constraints

- Preserve company isolation in every query and mutation.
- Cash management is owner-only.
- Persist timestamps in UTC and derive the business date through the company timezone.
- Do not add frameworks or dependencies.
- Keep source identifiers and migration names in English; UI text is Brazilian Portuguese.
- Do not commit while the current workspace contains user-owned uncommitted work.

---

### Task 1: Generalize Cash Charge Persistence

**Files:**
- Modify: `src/VoltaMarcada.Domain/Entities/WorkOrderCharge.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/WorkOrderChargeInstallment.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IWorkOrderChargeRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderChargeConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderChargeRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AddCashEntryBusinessDate.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/CashEntryPersistenceTests.cs`

**Consumes:** `WorkOrderCharge`, `WorkOrderChargeInstallment`, `IWorkOrderChargeRepository`.

**Produces:** Optional `WorkOrderId`, `DateOnly BusinessDate`, `CreateManualAsync`, `GetDailyAsync`, `GetPendingAsync`, and daily summary persistence methods.

- [ ] **Step 1: Write failing persistence tests**

```csharp
[Fact]
public async Task Repository_ShouldReturnOnlyCurrentCompanyEntriesForSelectedBusinessDate()
{
    var selectedDate = new DateOnly(2026, 9, 11);
    await repository.AddAsync(WorkOrderCharge.CreateManual(companyId, 100m, PaymentMethod.Pix, selectedDate, now));
    await repository.AddAsync(WorkOrderCharge.CreateManual(otherCompanyId, 200m, PaymentMethod.Cash, selectedDate, now));

    var entries = await repository.GetDailyAsync(companyId, selectedDate);

    Assert.Single(entries);
    Assert.Equal(100m, entries[0].Amount);
}
```

- [ ] **Step 2: Run the persistence test and verify it fails**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CashEntryPersistenceTests`

Expected: compilation failure because the daily cash API does not exist.

- [ ] **Step 3: Implement the aggregate and repository changes**

```csharp
public static WorkOrderCharge CreateManual(
    Guid companyId, decimal amount, PaymentMethod? paymentMethod,
    DateOnly businessDate, DateTimeOffset now)
```

Make `WorkOrderId` nullable, preserve existing work-order rows, add `BusinessDate`, add indexes on `(CompanyId, BusinessDate)` and `(CompanyId, Status, BusinessDate)`, and query installments through their parent charge.

- [ ] **Step 4: Add and apply the EF Core migration**

Run: `dotnet ef migrations add AddCashEntryBusinessDate --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web`

Verify existing rows receive `BusinessDate` derived from their existing creation timestamp before enforcing non-null.

- [ ] **Step 5: Run focused persistence tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CashEntryPersistenceTests`

Expected: PASS.

### Task 2: Cash Application Use Cases

**Files:**
- Create: `src/VoltaMarcada.Application/Payments/CashDtos.cs`
- Create: `src/VoltaMarcada.Application/Payments/GetCashEntriesUseCase.cs`
- Create: `src/VoltaMarcada.Application/Payments/CreateManualCashEntryUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Payments/ManageWorkOrderChargeInstallmentUseCases.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/CashUseCaseTests.cs`

**Consumes:** Task 1 repository API and owner authorization.

**Produces:** `GetCashEntriesQuery(DateOnly? BusinessDate, bool PendingOnly)`, `CashSummaryDto`, `CashEntryDto`, and `CreateManualCashEntryCommand(Guid? WorkOrderId, decimal Amount, PaymentMethod? paymentMethod, string? OtherPaymentMethodDescription, DateOnly BusinessDate)`.

- [ ] **Step 1: Write failing use-case tests**

```csharp
[Fact]
public async Task GetCashEntries_ShouldIgnoreDateWhenPendingOnlyIsSelected()
{
    await AddEntry(companyId, new DateOnly(2026, 9, 10), pending: true);
    await AddEntry(companyId, new DateOnly(2026, 9, 11), pending: true);
    await AddEntry(companyId, new DateOnly(2026, 9, 11), pending: false);

    var result = await useCase.ExecuteAsync(new GetCashEntriesQuery(new DateOnly(2026, 9, 11), true));

    Assert.Equal(2, result.Entries.Count);
    Assert.Equal(2m, result.Summary.PendingAmount);
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CashUseCaseTests`

Expected: compilation failure because the cash query and manual creation use cases do not exist.

- [ ] **Step 3: Implement owner-only cash queries and manual creation**

Validate positive amounts, valid payment methods, description for `Other`, optional work-order tenant ownership, and selected business date. Return total, pending, and confirmed amounts for daily mode; in pending-only mode return all pending entries and their all-days summary.

- [ ] **Step 4: Adapt confirm, update, and delete use cases**

Keep their existing server-side owner authorization. Return cash-list DTO data after mutation or let the page reload through `GetCashEntriesUseCase`.

- [ ] **Step 5: Run focused use-case tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CashUseCaseTests`

Expected: PASS.

### Task 3: Automatic Pending Charge on Work-Order Completion

**Files:**
- Modify: `src/VoltaMarcada.Application/WorkOrders/CompleteWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommands.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderPaymentDecisionInput.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderPaymentDomainTests.cs`

**Consumes:** Generalized charge aggregate and company timezone/business-date resolution.

**Produces:** A successful positive-value work-order completion creates one pending full-value charge without payment input from the work-order UI.

- [ ] **Step 1: Write failing completion tests**

```csharp
[Fact]
public async Task CompleteWorkOrder_ShouldCreateOnePendingCashEntryForItsFullAmount()
{
    var result = await useCase.ExecuteAsync(workOrder.Id, new CompleteWorkOrderCommand(null, null));

    Assert.True(result.IsSuccess);
    Assert.Single(repository.AddedCharges);
    Assert.Equal(workOrder.TotalAmount, repository.AddedCharges[0].TotalAmount);
}
```

- [ ] **Step 2: Run test and verify it fails**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~WorkOrderPaymentDomainTests`

Expected: failure because the completion flow requires payment details.

- [ ] **Step 3: Implement automatic charge creation**

For payment-controlled companies and a positive total, create one pending installment with no payment method; require the method only when Cash confirms the entry. Preserve no-charge behavior and prevent duplicate work-order charges.

- [ ] **Step 4: Run focused completion tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~WorkOrderPaymentDomainTests`

Expected: PASS.

### Task 4: Owner Cash Page and Navigation

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Pages/Cash.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`
- Modify: `src/VoltaMarcada.Web/Program.cs` if route/authorization registration requires it
- Test: `tests/VoltaMarcada.IntegrationTests/CashPageComponentTests.cs`

**Consumes:** Task 2 DTOs and use cases, `BrazilianCurrencyInput`, owner authorization policy.

**Produces:** `/caixa?date=YYYY-MM-DD` daily view and `/caixa?mode=pending` all-pending view.

- [ ] **Step 1: Write failing page-source and authorization tests**

```csharp
[Fact]
public void CashPage_ShouldExposeDailyDateAndPendingOnlyFilter()
{
    var source = File.ReadAllText(CashPagePath);
    Assert.Contains("@page \"/caixa\"", source);
    Assert.Contains("A confirmar", source);
    Assert.Contains("BrazilianCurrencyInput", source);
}
```

- [ ] **Step 2: Run test and verify it fails**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CashPageComponentTests`

Expected: failure because `Cash.razor` does not exist.

- [ ] **Step 3: Implement the page**

Use `[Authorize(Policy = AuthorizationPolicies.CompanyOwner)]`. Default the query date from `TimeProvider`; ignore the date query when `mode=pending`; show the selected date, summaries, entry table, manual-entry dialog, optional work-order selector, and confirm/edit/delete actions. Use localized labels and `BrazilianCurrencyInput` for amounts.

- [ ] **Step 4: Add owner-only navigation**

Add `Caixa` to the owner navigation section with `Href="/caixa"` and `Icons.Material.Filled.PointOfSale`.

- [ ] **Step 5: Run focused page tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CashPageComponentTests`

Expected: PASS.

### Task 5: Remove Financial UI From Work Orders

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Shared/WorkOrderExecutionPanel.razor`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/GetWorkOrderDetailsUseCase.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/WorkOrderEnhancementsComponentTests.cs`

**Consumes:** Task 3 automatic charge creation.

**Produces:** Work-order completion UI with no payment modal, installment editor, charge table, or payment action.

- [ ] **Step 1: Write failing component test**

```csharp
[Fact]
public void WorkOrderScreen_ShouldNotRenderPaymentControls()
{
    var source = ReadSource("Components/Shared/WorkOrderExecutionPanel.razor");
    Assert.DoesNotContain("Cobrança da OS", source);
    Assert.DoesNotContain("paymentInstallments", source);
}
```

- [ ] **Step 2: Run test and verify it fails**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~WorkOrderEnhancementsComponentTests`

Expected: failure because payment controls still render in the work-order panel.

- [ ] **Step 3: Remove work-order payment presentation and command construction**

Call completion with no payment decision. Remove DTO projection only if no remaining work-order consumer needs charge installments.

- [ ] **Step 4: Run focused component tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~WorkOrderEnhancementsComponentTests`

Expected: PASS.

### Task 6: Dashboard Cash KPIs

**Files:**
- Modify: `src/VoltaMarcada.Application/Dashboard/OwnerDashboardDtos.cs`
- Modify: `src/VoltaMarcada.Application/Dashboard/GetOwnerDashboardUseCase.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Dashboard.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/OwnerDashboardTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/DashboardCashKpiComponentTests.cs`

**Consumes:** Task 2 summary query.

**Produces:** Daily cash total and confirmed KPIs plus all-days pending KPI, linking to Cash URLs.

- [ ] **Step 1: Write failing dashboard tests**

```csharp
[Fact]
public async Task Dashboard_ShouldExposeDailyCashAndAllPendingAmounts()
{
    var dashboard = await useCase.ExecuteAsync();

    Assert.Equal(460m, dashboard.Cash.TodayTotal);
    Assert.Equal(120m, dashboard.Cash.AllPendingTotal);
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~OwnerDashboardTests|FullyQualifiedName~DashboardCashKpiComponentTests"`

Expected: compilation failure because the cash dashboard DTO does not exist.

- [ ] **Step 3: Implement KPI query and clickable cards**

Use local today for daily totals. Link daily cards to `/caixa?date={today:yyyy-MM-dd}` and pending card to `/caixa?mode=pending`.

- [ ] **Step 4: Run focused dashboard tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~OwnerDashboardTests|FullyQualifiedName~DashboardCashKpiComponentTests"`

Expected: PASS.

### Task 7: Full Verification

**Files:**
- Modify only files required to fix verification failures from Tasks 1-6.

- [ ] **Step 1: Build the solution**

Run: `dotnet build VoltaMarcada.sln --no-restore`

Expected: build succeeds with no new warnings or errors.

- [ ] **Step 2: Run all integration tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore`

Expected: all tests pass.

- [ ] **Step 3: Validate the primary flows manually**

1. Complete a positive-value OS and verify one pending entry appears in today’s Cash page.
2. Confirm, edit, and delete pending Cash entries as owner.
3. Create a manual entry with and without a work-order link.
4. Verify Cash pending mode ignores the selected date.
5. Click every dashboard cash KPI and confirm the expected Cash filter is active.
6. Sign in as an employee and verify Cash navigation and routes are unauthorized.