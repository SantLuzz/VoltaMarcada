# Multiple Equipment and Quote Price Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Support multiple equipment selections through quote and work-order items, render the correct equipment summaries, include every equipment item in quote PDFs, and preserve commercial-resolution prices.

**Architecture:** Item-level equipment remains the source of truth. Legacy aggregate equipment fields are derived from the first linked service item for compatibility. A single application-level summary mapper resolves the compact display label while the PDF document receives every distinct item-level equipment description.

**Tech Stack:** .NET 10, C#, Blazor, EF Core, QuestPDF, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-18-multiple-equipment-and-quote-price-design.md`

## Global Constraints

- Keep source-code identifiers and technical text in English.
- Keep user-facing UI text in Brazilian Portuguese.
- Do not change tenant scoping or the existing aggregate equipment schema.
- Use `BrazilianCurrencyInput` for editable Brazilian currency values.

---

### Task 1: Equipment display mapping

**Files:**
- Modify: `src/VoltaMarcada.Application/Quotes/GetQuotesUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Quotes/QuoteContracts.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/QuotePdfUseCaseTests.cs`

- [ ] Write a failing test asserting two distinct item equipment descriptions produce `2 equipamentos selecionados` in the compact quote DTO.
- [ ] Run the focused test and confirm it fails because the DTO mapper only maps the aggregate equipment.
- [ ] Add a mapper that uses distinct item equipment identifiers, returns the sole description for one item, and returns the Portuguese count for multiple items.
- [ ] Run the focused test and confirm it passes.

### Task 2: Item-first quote and work-order registration

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/QuoteRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/QuoteMaintenanceOpportunityTests.cs`

- [ ] Write a failing creation-flow test with linked item equipment and empty legacy equipment input.
- [ ] Run the focused test and confirm it fails for the old form-level equipment requirement.
- [ ] Remove creation-time top-level equipment inputs, derive the legacy equipment and primary service from the first linked service item, and seed source flows into the primary item.
- [ ] Run focused quote and work-order tests.

### Task 3: Quote PDF equipment list

**Files:**
- Modify: `src/VoltaMarcada.Application/Abstractions/Documents/IQuotePdfGenerator.cs`
- Modify: `src/VoltaMarcada.Application/Quotes/QuotePdfDocumentFactory.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Documents/QuestPdfQuotePdfGenerator.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/QuotePdfUseCaseTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/QuestPdfQuotePdfGeneratorTests.cs`

- [ ] Write a failing factory test asserting all distinct item equipment descriptions are present in the document.
- [ ] Run it and confirm it fails because the document carries only line items.
- [ ] Extend the document with equipment descriptions, resolve them from tenant-scoped equipment, and render a dedicated `Equipamentos` section in the PDF.
- [ ] Run the PDF tests and confirm they pass.

### Task 4: Commercial-resolution currency behavior

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/Schedule.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/ServiceRequestResolutionTests.cs`

- [ ] Write a failing test proving a non-fixed service persists the submitted commercial-resolution price to the generated quote item.
- [ ] Run it and confirm it fails or exposes the missing scenario.
- [ ] Replace the modal numeric field with `BrazilianCurrencyInput`; initialize and lock it at the catalogue price for fixed services and keep it editable otherwise.
- [ ] Run the focused service-request tests and confirm they pass.

### Task 5: Full verification

**Files:**
- Modify: `docs/superpowers/plans/2026-09-18-multiple-equipment-and-quote-price.md`

- [ ] Run the affected integration test suite and the web-project build.
- [ ] Run the solution test suite and document any pre-existing unrelated failures separately.
- [ ] Review the final diff to confirm only the approved behaviors changed.
