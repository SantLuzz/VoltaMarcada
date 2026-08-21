# Work Order Pricing and Form UX Design

## Goal

Make work-order forms predictable: GUID-backed dropdowns never expose identifiers, long selections do not change layout, reported problems are optional, and the selected service always has an explicit financial line whose price follows its catalog pricing rule.

## Scope

- Apply explicit empty-state labels to every current GUID-backed `MudSelect`.
- Keep customer and equipment selection controls stable when labels are long, truncate their displayed value, and expose the complete value in a tooltip.
- Make `ReportedProblem` optional throughout the work-order model and database.
- Use `BrazilianCurrencyInput` for unit prices.
- Model a protected service financial line separately from user-added work-order items.

## Pricing Rules

Each work order owns exactly one service line with quantity `1` and description equal to the selected service name. The browser never supplies its description or decides whether it exists.

| Service price type | Initial price | Editability | Server behavior |
| --- | --- | --- | --- |
| `Fixed` | `BasePrice` | Locked | Ignore submitted service amount and persist `BasePrice`. |
| `StartingAt` | `BasePrice` | Editable | Require and persist the submitted non-negative service amount. |
| `Hidden` | Empty | Editable | Require and persist the submitted non-negative service amount. |
| `QuoteRequired` | Empty | Editable | Require and persist the submitted non-negative service amount. |

The financial editor may also contain zero or more additional items, such as materials, parts, and travel fees. Additional items remain editable and removable. A service line cannot be edited or removed through the browser. Changing the selected service while a work order is pending replaces its service line and price state according to the newly selected service.

## Domain and Persistence

`WorkOrderItemType` contains `Additional` and `Service`. `WorkOrderItem` persists this type, and `WorkOrderItemDefinition` carries it when aggregate children are built. `WorkOrder` rejects an item collection unless it contains exactly one `Service` item and every other item is `Additional`.

Create and update commands contain `decimal? ServiceUnitPrice` plus `IReadOnlyList<WorkOrderItemInput> AdditionalItems`. The application loads the service inside the tenant scope and constructs the service definition itself. It uses the fixed catalog price for `Fixed`; for all other price types it rejects a missing value. The existing item validator applies only to additional items.

`ReportedProblem` becomes nullable in the domain, command contracts, DTOs, configuration, and database. Empty UI input is normalized to `null`. A descriptive migration makes `WorkOrders.ReportedProblem` nullable and adds the non-null `ItemType` column to `WorkOrderItems`, defaulting existing lines to `Additional`.

## UI Behavior

Single-choice GUID selectors render a human placeholder or an explicit empty option, matching their binding type. Multi-select GUID controls render a placeholder when their selection is empty. No selector may show `Guid.Empty` or a GUID value as fallback text.

The work-order customer and equipment control rows use a constrained flex layout: the selector may shrink, the action button may not, selected text uses an ellipsis, and a `MudTooltip` supplies the complete selected name. The behavior is local to work-order controls.

The item editor renders the generated service line first. It labels it as the selected service. `BrazilianCurrencyInput` displays and edits the service price when allowed; it is disabled for fixed prices. Additional items use the same currency component. The parent form validates service price presence before save whenever the selected service is not fixed.

## Security and Validation

The service is always loaded by both service ID and current company before price calculation. A browser may not use a price from another service or override a fixed catalog price. Work-order totals remain calculated by the aggregate from persisted line items. No `CompanyId` is accepted from the browser.

## Verification

The repository currently defers automated tests unless explicitly requested. Verify through static inspection: domain invariants, command/use-case price construction, nullable EF configuration and migration, GUID-select placeholders, Razor bindings, CSS selector scoping, and `git diff --check`. Do not run builds, tests, migrations, or the application.
