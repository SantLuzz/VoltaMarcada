# Grid Cell Truncation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep ServiceSync data grids within their available width while exposing every textual value through an accessible tooltip.

**Architecture:** A shared Razor component renders a single-line textual grid value with CSS truncation and a MudBlazor tooltip. A shared table CSS class establishes a fixed layout; every existing MudTable uses that class and the shared component for textual cells.

**Tech Stack:** Blazor Web App, MudBlazor, CSS.

**Spec:** `docs/superpowers/specs/2026-08-20-grid-cell-truncation-design.md`

## Global Constraints

- Use MudBlazor components and existing ServiceSync UI patterns.
- Preserve responsive MudTable behavior and avoid horizontal scrolling.
- Keep source identifiers and technical documentation in English; use Brazilian Portuguese for user-facing labels.
- Do not run builds, tests, or the application host unless explicitly requested; validate through static inspection.

---

### Task 1: Shared text cell and table styling

**Files:**
- Create: `src/ServiceSync.Web/Components/Shared/GridTextCell.razor`
- Modify: `src/ServiceSync.Web/wwwroot/app.css`

**Interfaces:**
- Produces: `GridTextCell` with required `Value` string parameter for use inside a `MudTd`.
- Produces: `servicesync-data-grid` CSS class for `MudTable` instances.

- [x] **Step 1: Add the shared GridTextCell component**

```razor
<MudTooltip Text="@Value">
    <span class="grid-text-cell">@Value</span>
</MudTooltip>

@code {
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;
}
```

- [x] **Step 2: Add shared fixed-layout and truncation CSS**

```css
.servicesync-data-grid .mud-table-root { table-layout: fixed; width: 100%; }
.servicesync-data-grid .mud-table-container { overflow-x: hidden; }
.grid-text-cell { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
```

- [x] **Step 3: Statically inspect component parameter use and CSS selectors**

Run: `rg -n 'GridTextCell|servicesync-data-grid|grid-text-cell' src/ServiceSync.Web`

Expected: the shared component and all three shared CSS selectors are present.

### Task 2: Apply the grid standard to current tables

**Files:**
- Modify: `src/ServiceSync.Web/Components/Pages/Company.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Customers.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/CustomerRegistration.razor`

**Interfaces:**
- Consumes: `GridTextCell` and `servicesync-data-grid` from Task 1.
- Produces: all current MudTable text cells render through `GridTextCell`.

- [x] **Step 1: Add the shared table class to each current MudTable**

```razor
<MudTable Items="@items" Class="servicesync-data-grid" Hover="true" Breakpoint="Breakpoint.Md">
```

- [x] **Step 2: Replace text-only cell values with GridTextCell**

```razor
<MudTd DataLabel="Nome"><GridTextCell Value="@context.Name" /></MudTd>
```

Apply the same pattern to employee, invitation, customer, and customer-address text cells. Pass already-formatted documents, phones, dates, and composed address strings as the `Value`.

- [x] **Step 3: Keep non-text controls intact**

Leave `MudChip`, `MudIconButton`, and action cells unchanged because they already provide labels or explicit action tooltips.

- [x] **Step 4: Statically inspect all MudTable declarations and text cells**

Run: `rg -n -C 2 '<MudTable|GridTextCell' src/ServiceSync.Web/Components/Pages/Company.razor src/ServiceSync.Web/Components/Pages/Customers.razor src/ServiceSync.Web/Components/Pages/CustomerRegistration.razor`

Expected: every MudTable has `servicesync-data-grid` and every textual data cell uses `GridTextCell`.

### Task 3: Make the pattern a UI rule

**Files:**
- Modify: `src/ServiceSync.Web/AGENTS.md`

**Interfaces:**
- Consumes: `GridTextCell` and `servicesync-data-grid` names from Tasks 1 and 2.
- Produces: mandatory UI guidance for future MudTable and MudDataGrid work.

- [x] **Step 1: Add the grid-cell sizing and tooltip rule**

Add a concise rule to the Tables and Data Grids section requiring `servicesync-data-grid` and `GridTextCell` for textual data cells, while retaining explicit tooltips on icon actions and avoiding horizontal scrolling.

- [x] **Step 2: Statically inspect the UI rule and final diff**

Run: `rg -n -C 3 'servicesync-data-grid|GridTextCell|horizontal scrolling' src/ServiceSync.Web/AGENTS.md; git diff --check`

Expected: the UI rule names both shared elements and the diff has no whitespace errors.
