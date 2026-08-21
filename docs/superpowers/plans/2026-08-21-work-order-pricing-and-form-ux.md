# Work Order Pricing and Form UX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Standardize work-order GUID selection UX and make service pricing authoritative and visible in financial items.

**Architecture:** The application constructs one protected service item after loading the selected company service, while the UI submits only a candidate service price and user-added additional items. The domain records item type and enforces exactly one service line. Razor and CSS provide localized, stable selection controls without moving business logic into the presentation layer.

**Tech Stack:** .NET 10, C#, Blazor Web App, MudBlazor, Entity Framework Core, PostgreSQL, FluentValidation.

**Spec:** `docs/superpowers/specs/2026-08-21-work-order-pricing-and-form-ux-design.md`

## Global Constraints

- Source code and technical documentation remain in English; UI copy remains Brazilian Portuguese.
- Work-order mutations resolve tenant and service ownership server-side.
- Do not add dependencies, projects, generic repositories, or frontend frameworks.
- Do not add automated tests, run builds, test runs, migration execution, or the application unless explicitly requested.
- Preserve unrelated uncommitted worktree changes.

### Task 1: Add Protected Service Financial Items

**Files:**
- Create: `src/ServiceSync.Domain/Enums/WorkOrderItemType.cs`
- Modify: `src/ServiceSync.Domain/ValueObjects/WorkOrderItemDefinition.cs`
- Modify: `src/ServiceSync.Domain/Entities/WorkOrderItem.cs`
- Modify: `src/ServiceSync.Domain/Entities/WorkOrder.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Configurations/WorkOrderItemConfiguration.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/<timestamp>_AddServiceWorkOrderItems.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

- [ ] Add `WorkOrderItemType { Additional, Service }`; persist it on `WorkOrderItem` and include it in `WorkOrderItemDefinition`.
- [ ] Require exactly one service item in `WorkOrder.ReplaceItems`; reject duplicate service items and any non-additional secondary line.
- [ ] Configure `ItemType` as required and create a migration that sets existing item rows to `Additional`.
- [ ] Make `ReportedProblem` nullable in the aggregate/configuration and add the same migration operation for `WorkOrders.ReportedProblem`.

### Task 2: Resolve Service Price Server-Side

**Files:**
- Modify: `src/ServiceSync.Application/WorkOrders/WorkOrderCommands.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/WorkOrderCommandValidators.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/CreateWorkOrderUseCase.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/UpdateWorkOrderUseCase.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/WorkOrderDtos.cs`
- Modify: `src/ServiceSync.Application/WorkOrders/GetWorkOrderDetailsUseCase.cs`

- [ ] Replace browser-submitted generic items with `ServiceUnitPrice` plus `AdditionalItems` in create/update contracts.
- [ ] After tenant-scoped service lookup, construct the service line from service name and price type. Use catalog price for fixed services; reject a null amount otherwise.
- [ ] Construct every user item with `WorkOrderItemType.Additional`; expose item type and service pricing state through detail DTOs.
- [ ] Permit null reported-problem input and preserve the null value in work-order DTOs.

### Task 3: Update Work-Order Forms and Financial Editor

**Files:**
- Modify: `src/ServiceSync.Web/Components/Pages/WorkOrderRegistration.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/WorkOrderItemsEditor.razor`
- Modify: `src/ServiceSync.Web/wwwroot/app.css`

- [ ] Remove `ReportedProblem` required UI and client save check.
- [ ] Add explicit empty entries/placeholders to all current GUID dropdowns, including the embedded customer modal and professional registration controls.
- [ ] Render one service row in `WorkOrderItemsEditor`; use `BrazilianCurrencyInput`, disable it for fixed service prices, and leave additional lines editable/removable.
- [ ] On service change, seed `StartingAt` with `BasePrice`, fixed price with `BasePrice`, and other price types with empty value.
- [ ] Constrain customer/equipment control rows, truncate selected text, and wrap selections in full-value `MudTooltip` instances.

### Task 4: Static Verification

**Files:** Review all files from Tasks 1-3.

- [ ] Inspect all `WorkOrderItemDefinition` construction to confirm exactly one server-created service line.
- [ ] Inspect every `MudSelect` with a GUID generic argument to confirm a human empty state.
- [ ] Run `git diff --check`, search for stale `ReportedProblem` required checks, and inspect changed migrations/snapshot consistency.
