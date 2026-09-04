# Operational Flow Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stabilize work-order, scheduling, quote, dashboard, and public-request flows without browser reloads or inconsistent state.

**Architecture:** Keep the existing application/domain boundaries intact. Extend work-order financial contracts through the domain entity, make the repository correctly project optional professionals, and let Blazor components own interactive state while JavaScript only stages/uploads files and signals success. Reuse the existing appointment use cases and Brazilian formatter rather than adding new services or dependencies.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core, PostgreSQL, MudBlazor, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-04-operational-flow-stabilization-design.md`

## Global Constraints

- Source and technical identifiers remain English; UI copy may be Brazilian Portuguese.
- Every tenant-owned read/write keeps its existing company filter and authorization scope.
- Do not add a persisted or sequential work-order number; show only the eight-character code derived from the existing `Guid`.
- Do not add packages or infrastructure dependencies.
- Use `IBrazilianDisplayFormatter` for Web UI amounts and `BrazilianCurrencyInput` for editable amounts.
- Do not use document navigation or forced refresh as a photo-upload success mechanism.
- Extend deterministic xUnit tests before each implementation change.

---

### Task 1: Carry work-order discounts and quote-derived financial data through the domain and application contracts

**Files:**
- Modify: `src/VoltaMarcada.Domain/Entities/WorkOrder.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommands.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommandValidators.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/CreateWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/UpdateWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/GetWorkOrderDetailsUseCase.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/AppointmentCommercialFlowTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderEnhancementsTests.cs`

**Interfaces:**
- Consumes: `CreateWorkOrderCommand`, `UpdateWorkOrderCommand`, `WorkOrder.Create`, `WorkOrder.UpdatePendingDetails`.
- Produces: `DiscountAmount` in `WorkOrderDetailDto`; commands that carry a non-negative `decimal DiscountAmount`.

- [ ] **Step 1: Write the failing discount and quote-copy tests**

Add assertions that a quote with service, part, material, travel, and additional items plus a discount produces a work order with the same `SourceQuoteId`, item types, discount, and final total. Add a pending-work-order update test asserting the new discount changes `TotalAmount` without changing the service item.

```csharp
Assert.Equal(quote.DiscountAmount, workOrder.DiscountAmount);
Assert.Equal(quote.TotalAmount, workOrder.TotalAmount);
Assert.Equal(quote.Items.Select(x => x.Type), workOrder.Items.Select(x => MapToWorkOrderType(x.Type)));
```

- [ ] **Step 2: Run the focused tests to verify the new expectations fail**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~AppointmentCommercialFlowTests|FullyQualifiedName~WorkOrderEnhancementsTests"`

Expected: FAIL because update commands/details do not expose `DiscountAmount` and the new test cannot compile or observe it.

- [ ] **Step 3: Extend contracts and domain behavior minimally**

Add `decimal DiscountAmount` after `ServiceUnitPrice` in both create/update command records. Validate it is non-negative. Add the value to `WorkOrderDetailDto`. Change `UpdatePendingDetails` to accept the discount, reject a negative value, assign it, and calculate `TotalAmount` from the existing item total minus the stored discount.

```csharp
public sealed record UpdateWorkOrderCommand(
    Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId,
    WorkOrderPriority Priority, string? ReportedProblem, decimal? ServiceUnitPrice,
    decimal DiscountAmount, IReadOnlyList<WorkOrderItemInput> AdditionalItems);

if (discountAmount < 0)
    throw new DomainException("Work order discount cannot be negative.");

DiscountAmount = discountAmount;
```

Pass the create command discount to `WorkOrder.Create`; pass the update command discount to `UpdatePendingDetails`; map `workOrder.DiscountAmount` into the details DTO. Do not change quote approval creation: it already passes `quote.DiscountAmount` and the quote item definitions directly.

- [ ] **Step 4: Run focused tests and the application build**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~AppointmentCommercialFlowTests|FullyQualifiedName~WorkOrderEnhancementsTests"`

Run: `dotnet build VoltaMarcada.sln --no-restore`

Expected: PASS.

- [ ] **Step 5: Commit the independently verified financial contract change**

```bash
git add src/VoltaMarcada.Domain/Entities/WorkOrder.cs src/VoltaMarcada.Application/WorkOrders tests/VoltaMarcada.IntegrationTests/AppointmentCommercialFlowTests.cs tests/VoltaMarcada.IntegrationTests/WorkOrderEnhancementsTests.cs
git commit -m "fix: preserve work order discounts"
```

### Task 2: Make the work-order UI role-correct, state-preserving, and financially complete

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/WorkOrderItemsEditor.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/WorkOrderExecutionPanel.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/js/work-order-photo-staging.js`
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrders.razor`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/WorkOrderPhotoStagingModuleTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/WorkOrderEnhancementsComponentTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderOrderingTests.cs`

**Interfaces:**
- Consumes: `GetCurrentEmployeeProfessionalUseCase.ExecuteAsync()`, `WorkOrderDetailDto.DiscountAmount`, `WorkOrderListItem.CreatedAtUtc`.
- Produces: bound `DiscountAmount` editor value; photo JavaScript callback `window.dispatchEvent(new CustomEvent("volta-marcada:work-order-photo-uploaded", ...))`; short code display from `Guid`.

- [ ] **Step 1: Write source/component and list-projection tests**

Add tests that require the registration page to use `GetCurrentEmployeeProfessionalUseCase`, filter `services` by `ServiceIds` for non-owners, bind a discount currency input, and render icon-only customer/equipment buttons with accessible labels. Add JavaScript source assertions that reject `window.location.assign` and require a photo-uploaded event. Add a list projection assertion for the stored opening timestamp and a component assertion for the short-code expression.

```csharp
Assert.DoesNotContain("window.location.assign", source, StringComparison.Ordinal);
Assert.Contains("volta-marcada:work-order-photo-uploaded", source, StringComparison.Ordinal);
Assert.Contains("Id.ToString(\"N\")[..8].ToUpperInvariant()", pageSource, StringComparison.Ordinal);
```

- [ ] **Step 2: Run the focused tests to verify they fail**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~WorkOrderPhotoStagingModuleTests|FullyQualifiedName~WorkOrderEnhancementsComponentTests|FullyQualifiedName~WorkOrderOrderingTests"`

Expected: FAIL because the current JavaScript reloads the document and the page has neither the professional-service filter nor bound discount UI.

- [ ] **Step 3: Implement the smallest state-safe UI change**

In `WorkOrderRegistration`, resolve the current professional after the access scope and set services as follows. Add a `discountAmount` field hydrated from detail and passed to both work-order commands. Use two `MudIconButton` controls with `Icons.Material.Filled.Add`, `AriaLabel`, and tooltips for quick customer/equipment creation. Inject `ICurrentDisplayTimeZone` and `IBrazilianDisplayFormatter` into `WorkOrders`, render the short code and local formatted `CreatedAtUtc` columns.

```csharp
var activeServices = (await OperationExecutor.ExecuteAsync(
    (GetServicesUseCase useCase, CancellationToken token) => useCase.ExecuteAsync(null, token)))
    .Where(service => service.IsActive);

services = isOwner
    ? activeServices.ToArray()
    : activeServices.Where(service => currentProfessional!.ServiceIds.Contains(service.Id)).ToArray();
```

Update `WorkOrderItemsEditor` with `DiscountAmount`/`DiscountAmountChanged`, a `BrazilianCurrencyInput`, and totals rendered as item subtotal, discount, and `subtotal - discount` through `DisplayFormatter.FormatCurrency`.

Replace the JS success navigation with dispatch of a browser event containing `workOrderId` and `photoType`. In the execution panel, register a managed listener through the imported module, invoke `OnChanged` once after success, and dispose the listener/module. Keep IndexedDB staging and retry behavior. Remove dead upload methods so there is one upload path. Make parent `LoadAsync` retain `activePanelIndex` and do not reset execution-only local state during post-operation refresh.

- [ ] **Step 4: Run focused tests and build**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~WorkOrderPhotoStagingModuleTests|FullyQualifiedName~WorkOrderEnhancementsComponentTests|FullyQualifiedName~WorkOrderOrderingTests"`

Run: `dotnet build VoltaMarcada.sln --no-restore`

Expected: PASS.

- [ ] **Step 5: Commit the work-order presentation and photo-state change**

```bash
git add src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor src/VoltaMarcada.Web/Components/Shared/WorkOrderItemsEditor.razor src/VoltaMarcada.Web/Components/Shared/WorkOrderExecutionPanel.razor src/VoltaMarcada.Web/wwwroot/js/work-order-photo-staging.js src/VoltaMarcada.Web/Components/Pages/WorkOrders.razor src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs tests/VoltaMarcada.IntegrationTests
git commit -m "fix: preserve work order screen state"
```

### Task 3: Correct optional-professional commercial retrieval and expose available-professional confirmation

**Files:**
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ServiceRequestRepository.cs`
- Modify: `src/VoltaMarcada.Application/ServiceRequests/ServiceRequestContracts.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/GetAppointmentRequestsUseCase.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Schedule.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`
- Test: `tests/VoltaMarcada.IntegrationTests/AppointmentCommercialFlowTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/AppointmentEditorTests.cs`

**Interfaces:**
- Consumes: public `AppointmentRequest` records with `ProfessionalId == null`; `AnalyzeAppointmentRequestAvailabilityUseCase` results.
- Produces: `ServiceRequestListItem` with nullable/empty professional display data and an agenda card that always offers an eligible-professional selector when the requested time has availability.

- [ ] **Step 1: Write failing retrieval and agenda-rendering tests**

Create a repository integration test with a pending service request whose appointment request has no professional. Assert it is returned for its company. Add a source/component test that asserts `RequestedTimeProfessionals.Count > 0` is evaluated before the commercial-resolution branch and that the confirm button uses the selected eligible professional.

```csharp
var requests = await repository.GetOpenForCompanyAsync(companyId);
Assert.Contains(requests, item => item.AppointmentRequest.Id == publicRequest.Id);
Assert.Contains("request.RequestedTimeProfessionals.Count > 0", scheduleSource, StringComparison.Ordinal);
```

- [ ] **Step 2: Run the focused tests to verify failure**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~AppointmentCommercialFlowTests|FullyQualifiedName~AppointmentEditorTests"`

Expected: FAIL because the repository uses inner joins to professionals and the UI gates available-professional selection behind `!RequiresCommercialResolution`.

- [ ] **Step 3: Implement left joins and availability-first agenda rendering**

Rewrite `GetOpenForCompanyAsync` with `join ... into`/`DefaultIfEmpty()` for professional, company user, and application user; keep the required service request, request, customer, and service joins/tenant predicates. Change `ServiceRequestListItem.ProfessionalName` to nullable or map it to `string.Empty` safely.

In the agenda request card, render the availability select and `Confirmar horário` first whenever `RequestedTimeProfessionals` is non-empty. Render the commercial decision action separately when `RequiresCommercialResolution`; do not make it suppress confirmation. Retain `AcceptAppointmentRequestUseCase` for the final server-side collision revalidation.

Use a `display: grid; grid-template-columns: repeat(2, minmax(0, 1fr));` action class for professional agenda actions and let select/full-row controls span `grid-column: 1 / -1`.

- [ ] **Step 4: Run focused tests and the scheduling suite**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~AppointmentCommercialFlowTests|FullyQualifiedName~AppointmentEditorTests|FullyQualifiedName~GetPublicAvailableSlotsUseCaseTests"`

Expected: PASS.

- [ ] **Step 5: Commit the scheduling correction**

```bash
git add src/VoltaMarcada.Infrastructure/Persistence/Repositories/ServiceRequestRepository.cs src/VoltaMarcada.Application/ServiceRequests src/VoltaMarcada.Application/Scheduling/GetAppointmentRequestsUseCase.cs src/VoltaMarcada.Web/Components/Pages/Schedule.razor src/VoltaMarcada.Web/wwwroot/app.css tests/VoltaMarcada.IntegrationTests
git commit -m "fix: expose available professionals for requests"
```

### Task 4: Turn dashboard appointment details into status-aware actions and focused navigation

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Shared/AppointmentDetailsDialog.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Dashboard.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Schedule.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/AppointmentCard.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/AppointmentEditorTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/EquipmentHistoryAndDashboardComponentTests.cs`

**Interfaces:**
- Consumes: `StartAppointmentUseCase.ExecuteAsync(Guid)`, `CompleteAppointmentUseCase.ExecuteAsync(Guid)`, `CancelAppointmentUseCase.ExecuteAsync(Guid)`.
- Produces: `AppointmentDetailsDialog` callbacks for refresh and navigation; `/agenda?appointmentId={id}` focus behavior; status-card class/label mapping.

- [ ] **Step 1: Write failing dialog/action and card tests**

Add tests asserting that scheduled appointments render start, view-schedule, and cancel actions; in-progress appointments render complete/view/cancel; completed/cancelled appointments omit start/cancel. Assert the dashboard card maps `Scheduled` to `Pendente`, `InProgress` to `Iniciou`, `Completed` to `Concluído`, and `Cancelled` to `Cancelado`, with status classes present in CSS.

```csharp
Assert.Contains("OnStart", dialogSource, StringComparison.Ordinal);
Assert.DoesNotContain("StartAppointmentAsync", closedStatusMarkup);
Assert.Contains("dashboard-appointment-card-completed", css);
```

- [ ] **Step 2: Run the focused tests to verify failure**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~AppointmentEditorTests|FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests"`

Expected: FAIL because the dialog only renders quote/work-order links and cards use a single static class/label.

- [ ] **Step 3: Implement status-aware dialog callbacks and navigation focus**

Give the dialog `EventCallback<Guid>` parameters for start, complete, cancel, and view-in-schedule, render actions only for legal statuses, and disable action buttons during an operation. The dashboard host will call the existing scheduling use cases through `OperationExecutor`, show mapped errors, close/refresh on success, and navigate to `/agenda?appointmentId={id}` for the view action.

Add `[SupplyParameterFromQuery(Name = "appointmentId")]` to `Schedule`; on initialization, load the date range containing the supplied appointment, set `selectedAppointmentId`, and open the existing details dialog. Preserve normal authorization by relying on `GetAppointmentDetailsUseCase`/`GetAppointmentsUseCase` results.

Map card class and text from `Appointment.Status`.

```csharp
private static string GetStatusLabel(AppointmentStatus status) => status switch
{
    AppointmentStatus.InProgress => "Iniciou",
    AppointmentStatus.Completed => "Concluído",
    AppointmentStatus.Cancelled => "Cancelado",
    _ => "Pendente"
};
```

- [ ] **Step 4: Run focused tests and build**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~AppointmentEditorTests|FullyQualifiedName~EquipmentHistoryAndDashboardComponentTests|FullyQualifiedName~OwnerDashboardTests"`

Run: `dotnet build VoltaMarcada.sln --no-restore`

Expected: PASS.

- [ ] **Step 5: Commit the dashboard scheduling action change**

```bash
git add src/VoltaMarcada.Web/Components/Shared/AppointmentDetailsDialog.razor src/VoltaMarcada.Web/Components/Pages/Dashboard.razor src/VoltaMarcada.Web/Components/Pages/Schedule.razor src/VoltaMarcada.Web/Components/Shared/AppointmentCard.razor src/VoltaMarcada.Web/wwwroot/app.css tests/VoltaMarcada.IntegrationTests
git commit -m "fix: make dashboard appointment actions status-aware"
```

### Task 5: Standardize quote currency display and finish public-submission state reliably

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/QuoteRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Quotes.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicQuote.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicMaintenanceBooking.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/js/form-submission-loading.js`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/DisplayFormattingTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/CompanyRenderingTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/WorkOrderEnhancementsComponentTests.cs`

**Interfaces:**
- Consumes: `IBrazilianDisplayFormatter.FormatCurrency(decimal)` and existing public booking `isSubmitting`/`isSubmitted` state.
- Produces: all quote totals in `R$ 0,00` format; a loading script scoped to `form[data-submission-loading]`; explicit success/error UI after public booking submission.

- [ ] **Step 1: Write failing rendering and script-scope tests**

Add source tests requiring the three quote pages to inject/use `IBrazilianDisplayFormatter` and rejecting `ToString("C")` for their displayed totals. Add source assertions that the loading script selects only `form[data-submission-loading]`, and that both public booking components reset `isSubmitting` in `finally` while showing a `Severity.Success` confirmation.

```csharp
Assert.DoesNotContain("ToString(\"C\")", quotePageSource, StringComparison.Ordinal);
Assert.Contains("form[data-submission-loading]", scriptSource, StringComparison.Ordinal);
Assert.Contains("finally { isSubmitting = false; }", publicPageSource, StringComparison.Ordinal);
```

- [ ] **Step 2: Run the focused tests to verify failure**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~DisplayFormattingTests|FullyQualifiedName~CompanyRenderingTests|FullyQualifiedName~WorkOrderEnhancementsComponentTests"`

Expected: FAIL because quote pages use default-culture formatting and the global script targets every form.

- [ ] **Step 3: Use the shared formatter and scope native-form loading**

Inject `IBrazilianDisplayFormatter` in quote registration/list/public quote pages and replace every UI `ToString("C")` with `DisplayFormatter.FormatCurrency(value)`. Leave `QuestPdfQuotePdfGenerator` unchanged.

Change the JS registration to query only explicitly opted-in forms. Do not add that attribute to Blazor public booking forms. Keep their button state bound only to `isSubmitting`; on success set `isSubmitted = true`, on error set a localized message, and let the existing `finally` re-enable controls.

```javascript
document.querySelectorAll("form[data-submission-loading]").forEach(form => {
  form.addEventListener("submit", disableNativeSubmitControls);
});
```

- [ ] **Step 4: Run focused tests and full solution verification**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore`

Run: `dotnet build VoltaMarcada.sln --no-restore`

Expected: PASS.

- [ ] **Step 5: Commit the display and submission-state correction**

```bash
git add src/VoltaMarcada.Web/Components/Pages/QuoteRegistration.razor src/VoltaMarcada.Web/Components/Pages/Quotes.razor src/VoltaMarcada.Web/Components/Pages/PublicQuote.razor src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor src/VoltaMarcada.Web/Components/Pages/PublicMaintenanceBooking.razor src/VoltaMarcada.Web/wwwroot/js/form-submission-loading.js tests/VoltaMarcada.IntegrationTests
git commit -m "fix: format quotes and complete public submissions"
```

### Task 6: Review cross-flow regressions and hand off verified work

**Files:**
- Modify if required by a failing test: only files named in Tasks 1–5.
- Test: `tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj`

**Interfaces:**
- Consumes: all contracts and UI flows changed in Tasks 1–5.
- Produces: a clean build and test result demonstrating no regression across affected modules.

- [ ] **Step 1: Inspect the aggregate diff for scope and tenant safety**

Run: `git diff HEAD~5 -- src tests`

Verify that every changed repository query retains company predicates, no browser reload remains in photo code, no quote UI uses default-culture currency formatting, and no persistence migration was introduced for the short code.

- [ ] **Step 2: Run all integration tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore`

Expected: PASS with no skipped failures.

- [ ] **Step 3: Build the complete solution**

Run: `dotnet build VoltaMarcada.sln --no-restore`

Expected: `Build succeeded` with zero errors.

- [ ] **Step 4: Perform focused manual browser verification**

Verify these paths with an owner and a professional account:

1. A professional sees only assigned services in new OS and may upload a photo without losing the active tab or checklist state.
2. A quote-approved OS shows copied items, discount, and the same final total.
3. A public request with available professionals lets an owner choose one and confirm; a request without an assigned professional opens commercial resolution without the false warning.
4. Dashboard cards use the expected status color/text and dialog actions match the appointment status; `Ver na agenda` focuses the chosen appointment.
5. Quote totals show `R$` and public requests complete with the confirmation alert while an error leaves the button usable.

- [ ] **Step 5: Commit any verification-only correction and report evidence**

```bash
git status --short
git log --oneline -5
```

If Steps 1–4 require a code correction, first add a focused failing test, make the minimal fix, rerun the focused test plus this task's full test/build commands, then commit with a `fix:` message. Report the exact test/build output in the handoff.
