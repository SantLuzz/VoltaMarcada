# Work Order Payment Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Require an auditable charge or no-charge reason when payment control is enabled and provide an owner-only daily Cash page.

**Architecture:** Add small payment entities to the work-order aggregate boundary, persisted through EF Core and exposed through explicit application use cases. Completion persists payment decisions in its existing transaction; Cash is an owner-only query and mutation surface over charge installments.

**Tech Stack:** .NET 10, C#, ASP.NET Core Minimal APIs, Blazor Web App/MudBlazor, EF Core, PostgreSQL, xUnit integration tests.

**Spec:** `docs/superpowers/specs/2026-09-10-work-order-payment-control-design.md`

## Global Constraints

- All code identifiers, migrations, tests, comments, and logs are English; UI text may be Brazilian Portuguese.
- All authenticated tenant operations resolve `CompanyId` from the user context and filter in the query.
- Only Owner can configure payment settings, access Cash, or confirm/edit/delete installments.
- Persist timestamps in UTC and calculate daily Cash dates in `America/Sao_Paulo` centrally.
- Do not add a payment gateway, generic ledger, repository abstraction without need, or a period filter.
- Use `TimeProvider`; do not call `DateTime.Now` or `DateTime.UtcNow` in application/domain code.

---

### Task 1: Payment domain model and company settings

**Files:**
- Create: `src/VoltaMarcada.Domain/Enums/PaymentMethod.cs`
- Create: `src/VoltaMarcada.Domain/Enums/WorkOrderChargeInstallmentStatus.cs`
- Create: `src/VoltaMarcada.Domain/Enums/PixKeyType.cs`
- Create: `src/VoltaMarcada.Domain/Entities/WorkOrderCharge.cs`
- Create: `src/VoltaMarcada.Domain/Entities/WorkOrderChargeInstallment.cs`
- Create: `src/VoltaMarcada.Domain/Entities/WorkOrderChargeInstallmentHistory.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Company.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/WorkOrder.cs`
- Test: `tests/VoltaMarcada.Domain.Tests/WorkOrders/WorkOrderChargeTests.cs`

**Interfaces:**
- Produces `WorkOrderCharge.Create(Guid companyId, Guid workOrderId, IReadOnlyCollection<WorkOrderChargeInstallmentDefinition> installments, DateTimeOffset now)`.
- Produces `WorkOrderChargeInstallment.Confirm(Guid ownerUserId, DateTimeOffset now)`, `Update(...)`, and `Remove(...)`.
- Produces `Company.UpdatePaymentSettings(bool enabled, string? pixKey, PixKeyType? pixKeyType, DateTimeOffset now)`.

- [ ] **Step 1: Write failing domain tests** for exact installment sum, invalid non-positive amount, mandatory Other description, owner confirmation fields, and company Pix key/type pairing.

```csharp
[Fact]
public void Create_rejects_installments_that_do_not_match_the_work_order_total()
{
    Assert.Throws<DomainException>(() => WorkOrderCharge.Create(companyId, workOrderId,
        [new(PaymentMethod.Pix, 99m, null)], 100m, now));
}
```

- [ ] **Step 2: Run** `dotnet test tests/VoltaMarcada.Domain.Tests --filter FullyQualifiedName~WorkOrderChargeTests` and confirm failure.
- [ ] **Step 3: Implement** protected-state entities, enums, definitions, no-charge fields on `WorkOrder`, and company payment/Pix validation.
- [ ] **Step 4: Run the same test command** and confirm pass.
- [ ] **Step 5: Commit** `feat: add work order payment domain model`.

### Task 2: EF Core schema and repositories

**Files:**
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderChargeConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderChargeInstallmentConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderChargeInstallmentHistoryConfiguration.cs`
- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/IWorkOrderChargeRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderChargeRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Create: EF migration `AddWorkOrderPaymentControl`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrders/WorkOrderPaymentPersistenceTests.cs`

**Interfaces:**
- Produces `GetForWorkOrderAsync(Guid workOrderId, Guid companyId, CancellationToken)` and `GetForInstallmentAsync(Guid installmentId, Guid companyId, CancellationToken)`.
- Produces `GetDailyCashAsync(Guid companyId, DateOnly date, CancellationToken)`.

- [ ] **Step 1: Write a failing integration test** that saves a company payment setting, charge, installment, and history, then reloads all values.
- [ ] **Step 2: Run** `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~WorkOrderPaymentPersistenceTests` and confirm failure.
- [ ] **Step 3: Implement** DbSets, mappings, relationships, unique work-order charge index, company/status/date indexes, repository queries, and migration.
- [ ] **Step 4: Run the test and** `dotnet ef database update --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web` against the development database.
- [ ] **Step 5: Commit** `feat: persist work order payment control`.

### Task 3: Payment settings and Pix payload application surface

**Files:**
- Create: `src/VoltaMarcada.Application/Payments/PaymentSettingsContracts.cs`
- Create: `src/VoltaMarcada.Application/Payments/CompanyPaymentSettingsUseCases.cs`
- Modify: `src/VoltaMarcada.Application/CompanySettings/CompanyPublicSettingsDto.cs`
- Modify: `src/VoltaMarcada.Application/CompanySettings/UpdateCompanyPublicSettingsCommand.cs`
- Modify: `src/VoltaMarcada.Application/CompanySettings/CompanyPublicSettingsUseCases.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Payments/CompanyPaymentSettingsTests.cs`

**Interfaces:**
- Produces `GetCompanyPaymentSettingsUseCase.ExecuteAsync()` and `UpdateCompanyPaymentSettingsUseCase.ExecuteAsync(UpdateCompanyPaymentSettingsCommand)`.
- Produces `GetPixPayloadUseCase.ExecuteAsync(Guid workOrderId, decimal amount)` using `IPixPayloadGenerator`.

- [ ] **Step 1: Write failing tests** for Owner update, Employee forbidden, invalid Pix key/type pair, and Pix payload amount.
- [ ] **Step 2: Run the focused tests** and confirm failure.
- [ ] **Step 3: Implement** owner authorization, validation, and payload construction from company trade name/city/settings.
- [ ] **Step 4: Run focused tests** and confirm pass.
- [ ] **Step 5: Commit** `feat: add company payment settings`.

### Task 4: Atomic work-order completion with payment decision

**Files:**
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommands.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderErrors.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/CompleteWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IWorkOrderRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrders/CompleteWorkOrderPaymentTests.cs`

**Interfaces:**
- Extends `CompleteWorkOrderCommand` with `WorkOrderPaymentDecisionInput? PaymentDecision`.
- `WorkOrderPaymentDecisionInput` is either installments or a required `NoChargeReason`.

- [ ] **Step 1: Write failing tests** for control disabled, zero total, missing decision, valid full/split payment, valid no-charge, sum mismatch, and maintenance recommendation transaction rollback.
- [ ] **Step 2: Run focused completion tests** and confirm failure.
- [ ] **Step 3: Implement** server-side payment decision validation and persistence inside the existing maintenance transaction before `SaveChangesAsync`.
- [ ] **Step 4: Run focused completion tests** and confirm pass.
- [ ] **Step 5: Commit** `feat: require payment decision for work order completion`.

### Task 5: Cash query and owner payment mutations

**Files:**
- Create: `src/VoltaMarcada.Application/Payments/CashContracts.cs`
- Create: `src/VoltaMarcada.Application/Payments/GetDailyCashUseCase.cs`
- Create: `src/VoltaMarcada.Application/Payments/ConfirmChargeInstallmentUseCase.cs`
- Create: `src/VoltaMarcada.Application/Payments/UpdateChargeInstallmentUseCase.cs`
- Create: `src/VoltaMarcada.Application/Payments/DeleteChargeInstallmentUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Payments/PaymentErrors.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Payments/DailyCashTests.cs`

**Interfaces:**
- Produces `DailyCashDto(DateOnly Date, decimal ReceivedAmount, decimal ReceivableAmount, decimal NoChargeAmount, IReadOnlyList<CashInstallmentDto> Installments)`.
- Produces Owner-only confirmation/update/delete commands with required mutation reason for update/delete.

- [ ] **Step 1: Write failing tests** for São Paulo daily grouping, paid/pending/no-charge totals, Owner confirmation, Employee forbidden, update history, delete history, recomposition rejection, and cross-company denial.
- [ ] **Step 2: Run focused Cash tests** and confirm failure.
- [ ] **Step 3: Implement** centralized daily query conversion, owner authorization, immutable audit creation, and charge-total/no-charge revalidation.
- [ ] **Step 4: Run focused Cash tests** and confirm pass.
- [ ] **Step 5: Commit** `feat: add daily cash operations`.

### Task 6: HTTP endpoints and application composition

**Files:**
- Create: `src/VoltaMarcada.Web/Endpoints/PaymentEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Endpoints/WorkOrderEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Modify: `src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Payments/PaymentEndpointsTests.cs`

- [ ] **Step 1: Write failing endpoint tests** for payment completion payload, Pix payload access, daily Cash Owner access, and Employee 403 responses.
- [ ] **Step 2: Run focused endpoint tests** and confirm failure.
- [ ] **Step 3: Map** company settings, charge payload, daily Cash, confirm, update, and delete routes; apply existing company-member and owner policies.
- [ ] **Step 4: Run focused endpoint tests** and confirm pass.
- [ ] **Step 5: Commit** `feat: expose work order payment endpoints`.

### Task 7: Company settings, completion dialog, and Cash UI

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Shared/WorkOrderPaymentDialog.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/Cash.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Company.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/WorkOrderExecutionPanel.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/NavMenu.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/css/app.css`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/WorkOrderPaymentRenderingTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/CashRenderingTests.cs`

- [ ] **Step 1: Write failing component tests** for Owner payment settings, payment dialog Pix display without closing, split total validation, Employee Cash navigation absence, and daily Cash navigation/actions.
- [ ] **Step 2: Run focused component tests** and confirm failure.
- [ ] **Step 3: Implement** Portuguese UI copy, payment/Pix settings controls, same-dialog QR/copy/share behavior, and daily Cash date navigation with confirm/edit/delete dialogs.
- [ ] **Step 4: Run focused component tests** and confirm pass.
- [ ] **Step 5: Build** `dotnet build VoltaMarcada.sln` and manually verify desktop and narrow viewport layout.
- [ ] **Step 6: Commit** `feat: add payment completion and cash screens`.

### Task 8: Regression verification and documentation

**Files:**
- Modify: `README.md` only if a user-facing navigation/setup note is required by the completed UI.
- Test: all affected test projects.

- [ ] **Step 1: Run** `dotnet test VoltaMarcada.sln`.
- [ ] **Step 2: Run** `dotnet build VoltaMarcada.sln` and `git diff --check`.
- [ ] **Step 3: Verify** migration list includes `AddWorkOrderPaymentControl` and no connection strings or keys appear in Git diff.
- [ ] **Step 4: Commit** any documentation-only change as `docs: document work order payment control`.

## Self-Review

Spec coverage: Tasks 1-2 cover data and migration; Task 3 covers settings/Pix; Task 4 covers atomic completion; Task 5 covers Cash, audit, tenancy, and permissions; Tasks 6-7 cover API and UI; Task 8 verifies the complete feature. No generic finance module, gateway, or period filter is planned.

Placeholder scan: no deferred requirements or unspecified test work remain. Type names introduced by each task are consumed only by later tasks.
