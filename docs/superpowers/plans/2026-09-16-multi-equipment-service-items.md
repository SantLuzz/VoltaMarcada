# Multi-Equipment Service Items Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let work orders, quotes, and public requests contain multiple equipment-associated services and general items, and reflect them in PDFs.

**Architecture:** Existing work-order and quote line items gain nullable equipment and service references. A new ordered appointment-request item is the public-flow equivalent. Legacy header fields remain readable, but all new writes, availability checks, conversions, and PDF projections use item collections.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor, EF Core, PostgreSQL, QuestPDF, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-16-multi-equipment-service-items-design.md`

## Global Constraints

- Source code, migrations, logs, comments, and tests use English identifiers; UI strings use Brazilian Portuguese.
- Resolve and enforce `CompanyId` on the server for every authenticated command and query.
- Validate every selected equipment belongs to the selected customer and current company.
- Keep domain invariants in domain entities, business orchestration in explicit use cases, and persistence mappings in Infrastructure.
- Revalidate all requested services, combined duration, professional eligibility, blocks, and collisions immediately before appointment acceptance.
- Use `TimeProvider` for domain/application time and persist timestamps in UTC.
- Add deterministic xUnit coverage and run focused tests, the full suite, build, migration checks, and PDF visual validation.

---

### Task 1: Add item-level equipment and service associations

**Files:**
- Modify: `src/VoltaMarcada.Domain/ValueObjects/WorkOrderItemDefinition.cs`
- Modify: `src/VoltaMarcada.Domain/ValueObjects/QuoteItemDefinition.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/WorkOrderItem.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/QuoteItem.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderItemConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/QuoteItemConfiguration.cs`
- Create: EF migration `AddEquipmentServiceItemAssociations`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderEnhancementsTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/QuoteMaintenanceOpportunityTests.cs`

**Interfaces:**
- Produces `WorkOrderItemDefinition(string Description, decimal Quantity, decimal UnitPrice, WorkOrderItemType Type, Guid? EquipmentId, Guid? ServiceId)`.
- Produces the equivalent `QuoteItemDefinition` and nullable `EquipmentId`/`ServiceId` properties on both entities.

- [ ] **Step 1: Write failing tests for two linked service rows and an unlinked travel row.**

```csharp
var items = new[]
{
    new WorkOrderItemDefinition("Clean A", 1, 120m, WorkOrderItemType.Service, equipmentA, serviceA),
    new WorkOrderItemDefinition("Repair B", 1, 150m, WorkOrderItemType.Service, equipmentB, serviceB),
    new WorkOrderItemDefinition("Travel", 1, 30m, WorkOrderItemType.Travel, null, null)
};
Assert.Equal(equipmentA, workOrder.Items[0].EquipmentId);
Assert.Null(workOrder.Items[2].EquipmentId);
```

- [ ] **Step 2: Run the tests to verify they fail.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~WorkOrderEnhancementsTests|FullyQualifiedName~QuoteMaintenanceOpportunityTests"`

Expected: FAIL because the item definitions and entities have no association members.

- [ ] **Step 3: Add invariant-protected optional IDs and mappings.**

```csharp
if (definition.Type == WorkOrderItemType.Service && definition.ServiceId is not { } serviceId || serviceId == Guid.Empty)
    throw new DomainException("A service work order item requires a service ID.");

EquipmentId = NormalizeOptionalId(definition.EquipmentId);
ServiceId = NormalizeOptionalId(definition.ServiceId);
```

Configure nullable composite foreign keys `(CompanyId, EquipmentId)` and `(CompanyId, ServiceId)`. Generate a migration that backfills the matching legacy header equipment/service into existing service lines and retains legacy columns.

- [ ] **Step 4: Run focused tests and migration inspection.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~WorkOrderEnhancementsTests|FullyQualifiedName~QuoteMaintenanceOpportunityTests"`

Expected: PASS; generated migration has nullable columns, tenant-scoped FKs, indexes, and backfill SQL.

- [ ] **Step 5: Commit.**

```bash
git add src/VoltaMarcada.Domain src/VoltaMarcada.Infrastructure tests/VoltaMarcada.IntegrationTests
git commit -m "feat: associate commercial items with equipment and services"
```

### Task 2: Create and edit work orders and quotes from item collections

**Files:**
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommands.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderCommandValidators.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/CreateWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/UpdateWorkOrderUseCase.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs`
- Modify: `src/VoltaMarcada.Application/Quotes/QuoteContracts.cs`
- Modify: `src/VoltaMarcada.Application/Quotes/CreateQuoteUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Quotes/UpdateQuoteUseCase.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/QuoteRepository.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderOrderingTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/QuoteMaintenanceOpportunityTests.cs`

**Interfaces:**
- Produces explicit work-order and quote item inputs that carry `EquipmentId`, `ServiceId`, description, quantity, price, and type.
- Produces item DTOs containing resolved equipment display data and service identity.

- [ ] **Step 1: Write failing use-case tests for valid multi-item persistence and invalid customer equipment.**

```csharp
var command = validCommand with
{
    Items =
    [
        new WorkOrderServiceItemInput(equipmentA, serviceA, "Maintenance", 1, 100m),
        new WorkOrderServiceItemInput(equipmentB, serviceB, "Repair", 1, 150m)
    ]
};
var result = await useCase.ExecuteAsync(command);
Assert.True(result.Succeeded);
Assert.Equal(2, result.Value.Items.Count(item => item.ServiceId is not null));
```

- [ ] **Step 2: Run the focused tests to verify they fail.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~WorkOrderOrderingTests|FullyQualifiedName~QuoteMaintenanceOpportunityTests"`

Expected: FAIL because commands only accept a single equipment/service header pair.

- [ ] **Step 3: Resolve every row server-side before aggregate mutation.**

```csharp
foreach (var item in command.Items)
{
    if (item.EquipmentId is { } equipmentId)
        await EnsureEquipmentBelongsToCustomerAsync(equipmentId, customer.Id, scope.CompanyId, cancellationToken);
    if (item.ServiceId is { } serviceId)
        await EnsureActiveCompanyServiceAsync(serviceId, scope.CompanyId, cancellationToken);
}
```

Reject a cross-company or wrong-customer equipment selection, inactive service, or service row without `ServiceId`. Derive old header display fallback values from the first linked item only.

- [ ] **Step 4: Run focused tests.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~WorkOrderOrderingTests|FullyQualifiedName~QuoteMaintenanceOpportunityTests|FullyQualifiedName~WorkOrderEnhancementsTests"`

Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add src/VoltaMarcada.Application src/VoltaMarcada.Infrastructure tests/VoltaMarcada.IntegrationTests
git commit -m "feat: create work orders and quotes from service items"
```

### Task 3: Replace internal single selectors with an item editor

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Shared/EquipmentServiceItemsEditor.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/WorkOrderItemsEditor.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/QuoteRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Endpoints/WorkOrderEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Endpoints/QuoteEndpoints.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/WorkOrderEnhancementsComponentTests.cs`

**Interfaces:**
- Consumes Task 2 item input/DTO contracts.
- Produces an editor that can add/remove equipment-service rows and separate general rows.

- [ ] **Step 1: Write failing component tests for the reusable editor and list-based endpoint payloads.**

```csharp
Assert.Contains("EquipmentServiceItemsEditor", workOrderSource);
Assert.Contains("Adicionar equipamento e serviço", editorSource);
Assert.Contains("Items", endpointSource);
```

- [ ] **Step 2: Run the component test to verify it fails.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~WorkOrderEnhancementsComponentTests"`

Expected: FAIL because the editor does not exist.

- [ ] **Step 3: Implement customer-filtered row editing.**

```razor
<MudSelect T="Guid?" Label="Equipamento" @bind-Value="item.EquipmentId">...</MudSelect>
<MudSelect T="Guid?" Label="Serviço" @bind-Value="item.ServiceId" Required="@item.IsService">...</MudSelect>
<MudButton OnClick="@(() => Remove(item))">Remover</MudButton>
```

Clear selected equipment rows when the customer changes. Preserve quick equipment creation and attach the created ID to the active row. Do not require equipment for a general service, part, material, travel, or custom row.

- [ ] **Step 4: Run component, work-order, and quote tests.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~WorkOrderEnhancementsComponentTests|FullyQualifiedName~WorkOrderOrderingTests|FullyQualifiedName~QuoteMaintenanceOpportunityTests"`

Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add src/VoltaMarcada.Web tests/VoltaMarcada.IntegrationTests
git commit -m "feat: edit multiple equipment service items"
```

### Task 4: Add public request items and combined scheduling validation

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/AppointmentRequestItem.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/AppointmentRequest.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestCommand.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitContextualAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/AnalyzeAppointmentRequestAvailabilityUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/AcceptAppointmentRequestUseCase.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/AppointmentRequestItemConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/VoltaMarcadaDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/AppointmentRequestRepository.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/PublicBookingEquipmentTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/GetPublicAvailableSlotsUseCaseTests.cs`

**Interfaces:**
- Produces `PublicAppointmentRequestItemInput(Guid ServiceId, string EquipmentType, string? EquipmentManufacturer)`.
- Produces `AppointmentRequest.Items` and `DurationMinutes` equal to the sum of all requested service lines.

- [ ] **Step 1: Write failing tests for two public equipment rows and a professional qualified for all requested services.**

```csharp
var command = validCommand with
{
    Items =
    [
        new PublicAppointmentRequestItemInput(serviceA, "Split", "Brand A"),
        new PublicAppointmentRequestItemInput(serviceB, "Freezer", "Brand B")
    ]
};
var result = await submit.ExecuteAsync(command);
Assert.Equal(durationA + durationB, result.Value.DurationMinutes);
Assert.Equal(2, result.Value.Items.Count);
```

- [ ] **Step 2: Run public booking tests to verify failure.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~PublicBookingEquipmentTests|FullyQualifiedName~GetPublicAvailableSlotsUseCaseTests"`

Expected: FAIL because public requests accept one service/equipment pair.

- [ ] **Step 3: Persist ordered request items and calculate the complete schedule.**

```csharp
var serviceIds = request.Items.Select(item => item.ServiceId).Distinct().ToArray();
var durationMinutes = services.Sum(service => service.DurationMinutes);
var eligible = professionals.Where(professional => serviceIds.All(professional.ServiceIds.Contains));
```

Resolve or create each equipment only under the customer and company determined server-side. Never expose professionals publicly. Introduce `AddAppointmentRequestItems` migration with a company-scoped request foreign key and a deterministic backfill from existing request headers.

- [ ] **Step 4: Run public-flow, availability, and commercial acceptance tests.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~PublicBookingEquipmentTests|FullyQualifiedName~GetPublicAvailableSlotsUseCaseTests|FullyQualifiedName~AppointmentCommercialFlowTests"`

Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add src/VoltaMarcada.Domain src/VoltaMarcada.Application src/VoltaMarcada.Infrastructure src/VoltaMarcada.Web tests/VoltaMarcada.IntegrationTests
git commit -m "feat: request multiple equipment services publicly"
```

### Task 5: Keep associations in conversion and PDFs, then verify

**Files:**
- Modify: `src/VoltaMarcada.Application/Quotes/ApproveQuoteUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Quotes/QuotePdfDocumentFactory.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Documents/IQuotePdfGenerator.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Documents/QuestPdfQuotePdfGenerator.cs`
- Modify: any work-order PDF contract/generator found in `src/VoltaMarcada.Application` and `src/VoltaMarcada.Infrastructure`
- Test: `tests/VoltaMarcada.IntegrationTests/AppointmentCommercialFlowTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/QuotePdfUseCaseTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/QuestPdfQuotePdfGeneratorTests.cs`

**Interfaces:**
- Produces `QuotePdfEquipmentGroup(string Heading, IReadOnlyList<QuotePdfLine> Lines)` plus a `GeneralLines` collection.
- Produces quote-converted work orders with every source item association unchanged.

- [ ] **Step 1: Write failing conversion and PDF grouping tests.**

```csharp
Assert.Contains(workOrder.Items, item => item.EquipmentId == equipmentA && item.ServiceId == serviceA);
Assert.Contains(workOrder.Items, item => item.EquipmentId == equipmentB && item.ServiceId == serviceB);
Assert.Equal("Split - Brand A Model A", document.EquipmentGroups[0].Heading);
Assert.Single(document.GeneralLines);
```

- [ ] **Step 2: Run tests to verify failure.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~AppointmentCommercialFlowTests|FullyQualifiedName~QuotePdfUseCaseTests|FullyQualifiedName~QuestPdfQuotePdfGeneratorTests"`

Expected: FAIL because conversion and PDF projection use flat/header associations.

- [ ] **Step 3: Copy all item definitions and project resolved equipment groups.**

```csharp
var definitions = quote.Items.Select(item => new WorkOrderItemDefinition(
    item.Description, item.Quantity, item.UnitPrice, MapType(item.Type), item.EquipmentId, item.ServiceId));
```

Build document groups in the application factory from equipment type, manufacturer, model, serial number, and location. The QuestPDF generator receives only document data and renders each equipment table followed by `General services and items`; apply the same contract to any OS PDF generator.

- [ ] **Step 4: Run focused tests, the full suite, build, and PDF visual verification.**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests`

Run: `dotnet build VoltaMarcada.sln`

Expected: PASS. Inspect a generated PDF to confirm separate equipment headings, general items, discounts, and totals are legible.

- [ ] **Step 5: Commit the completed feature.**

```bash
git add src tests docs
git commit -m "feat: support multiple equipment service items"
```

