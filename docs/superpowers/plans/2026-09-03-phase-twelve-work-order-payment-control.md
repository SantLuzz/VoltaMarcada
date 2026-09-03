# Phase 12 Work Order Payment Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When enabled by a company, require a recorded payment outcome before a valued work order can be completed, without introducing a payment gateway.

**Architecture:** Company payment settings define whether the rule applies and hold that company’s PIX receiving configuration. A company-scoped `WorkOrderCharge` records pending, paid, or no-charge outcomes; Application coordinates it with work-order completion atomically and applies Owner/Employee permissions.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core, PostgreSQL, MudBlazor, xUnit.

**Spec:** `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md` (Phase 12).

## Global Constraints

- Do not reuse VoltaMarcada license PIX credentials, `CompanyLicense`, or `LicensePaymentSubmission` for customer charges.
- Company settings and every charge query are tenant-scoped; browser data never authorizes a company or payment confirmation.
- A generated or shared PIX charge remains `Pending` until an Owner confirms it manually.
- Do not add a gateway, accounts payable, bank reconciliation, or a general ledger.
- Use explicit business results for missing charge, invalid status, and unauthorized confirmation.

---

## File Structure

- Add `CompanyPaymentSettings` and `WorkOrderCharge` entities/enums with EF configurations and a migration.
- Add focused payment-setting and work-order-charge repositories, commands, DTOs, validators, and use cases.
- Extend completion orchestration and work-order detail DTOs without moving financial rules into Razor.
- Add Owner settings, completion charge modal, and Owner financial-status views.
- Add tenant, authorization, and completion-rule integration tests.

### Task 1: Persist company payment settings and charge outcomes

**Files:**
- Create: domain entities/enums for payment settings, charge status, payment method, and confirmation type
- Create: EF configurations, repositories, and migration `AddWorkOrderPaymentControl`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderChargePersistenceTests.cs`

- [ ] **Step 1: Write failing persistence tests**

```csharp
[Fact]
public async Task Charge_IsScopedToTheCompanyAndWorkOrderAndCannotBeDuplicated()
{
    await repository.AddAsync(WorkOrderCharge.Create(companyId, workOrderId, 250m, PaymentMethod.Pix, now));
    await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(WorkOrderCharge.Create(companyId, workOrderId, 250m, PaymentMethod.Pix, now)));
}
```

- [ ] **Step 2: Implement guarded entities and migration**

Use one charge per work order, immutable original amount, `Pending`/`Paid`/`NoCharge` outcomes, bounded required reason for no charge, and confirmation user/time only after Owner confirmation. Store company PIX configuration separately and encrypted/configured according to the storage strategy selected for the company.

- [ ] **Step 3: Run focused test**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~WorkOrderChargePersistenceTests`

Expected: PASS, including company/work-order composite constraints.

### Task 2: Coordinate charge requirements with completion

**Files:**
- Create: payment-control use cases and operation errors
- Modify: `CompleteWorkOrderUseCase.cs`, work-order contracts, DI
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderPaymentCompletionTests.cs`

- [ ] **Step 1: Write failing completion tests**

```csharp
[Fact]
public async Task Complete_WhenPaymentControlIsEnabledAndValuedOrderHasNoOutcome_ReturnsChargeRequired()
{
    var result = await completeWorkOrder.ExecuteAsync(workOrderId, completion);
    Assert.Equal("WorkOrderCharge.Required", result.Error!.Code);
    Assert.Equal(WorkOrderStatus.InProgress, order.Status);
}
```

- [ ] **Step 2: Implement the ordered transaction**

After optional maintenance recommendation, require either a newly created charge or a valid no-charge reason before completing the work order. Revalidate ownership and amount server-side. Preserve the existing completion behavior unchanged when payment control is disabled or order total is zero.

- [ ] **Step 3: Run focused test**

Run the focused test; expected PASS for disabled setting, pending payment, no-charge reason, and rollback on failure.

### Task 3: Generate/share PIX and enforce roles

**Files:**
- Create: PIX payload builder abstraction and charge actions
- Modify: endpoints, WhatsApp message builder, error mappings
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderChargeAuthorizationTests.cs`

- [ ] **Step 1: Write failing role tests**

Assert that an Employee can create and share a pending charge but receives `WorkOrderCharge.OwnerConfirmationRequired` when confirming payment; assert Owner confirmation records `PaidAt` and `ConfirmedByUserId`.

- [ ] **Step 2: Implement exact server behavior**

Generate the full-valued PIX code from the current company’s configured key, allow copy/share actions, and expose no secret configuration. Require Owner authorization for confirmation and all financial summary access. Never mark a charge paid from a browser callback or WhatsApp action.

- [ ] **Step 3: Run focused test**

Run the authorization test; expected PASS with cross-company denial and no automatic payment transition.

### Task 4: Render settings and completion UX

**Files:**
- Modify: `Company.razor`, `WorkOrderExecutionPanel.razor`, `WorkOrderRegistration.razor`, CSS
- Create: focused charge editor/display component
- Test: component-source tests

- [ ] **Step 1: Write failing component tests**

Assert the company screen contains the Owner-only “Controle de pagamentos” control and the completion panel shows “Cobrança” before enabling final completion when the setting applies.

- [ ] **Step 2: Implement Portuguese UI states**

Provide payment-method selection, PIX QR/copy/share, pending explanation, Owner-only confirmation, and a required no-charge reason. Keep Employee users out of the general financial area.

- [ ] **Step 3: Verify and commit**

Run focused tests, `dotnet test VoltaMarcada.sln --no-restore`, and `git diff --check`. Commit with `feat: require payment outcome before work order completion`.