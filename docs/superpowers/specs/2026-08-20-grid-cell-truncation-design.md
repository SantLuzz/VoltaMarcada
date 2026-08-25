# Grid Cell Truncation Design

## Goal

Prevent horizontal scrolling in VoltaMarcada data grids while keeping every cell value accessible.

## Shared Component

Add a presentation-only shared component for textual grid cells. It receives a text value, constrains the visible content to one line, and renders an ellipsis when the value exceeds its cell width. A MudBlazor tooltip exposes the full value on hover or keyboard focus.

The component does not format domain values. Each page remains responsible for producing its display string before passing it to the component.

## Table Behavior

Apply a shared table class that uses the available container width and a fixed table layout. Text cells may shrink and truncate; action controls and status chips retain their usable width. The existing MudTable responsive breakpoint remains enabled so smaller viewports use MudBlazor's responsive table presentation instead of horizontal scrolling.

Use the shared text-cell component for every textual data value in the existing employee, invitation, customer, and customer-address grids. Continue to use explicit tooltips for icon-only actions. Do not truncate or replace status chips and actions.

## UI Rule

Every new `MudTable` and `MudDataGrid` must use the shared fixed-width grid styling. Textual data cells must use the shared truncation-and-tooltip component. This rule keeps data grids readable at constrained widths without hiding the complete value from users.

## Validation

Perform static inspection to confirm all current table text cells use the shared component and that the UI guidance documents the rule. Per project constraints, do not run a build or application host unless explicitly requested.
