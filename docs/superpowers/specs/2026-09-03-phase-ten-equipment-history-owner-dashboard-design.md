# Phase 10 Equipment History and Owner Value Dashboard Design

## Goal

Turn the operational data already recorded by VoltaMarcada into a technical equipment history and an Owner-only dashboard that makes daily actions and maintenance-return value visible.

## Scope

This increment includes:

- a lazy-loaded equipment history side panel from the equipment list;
- an Owner dashboard summary for daily operational actions;
- a monthly maintenance-opportunity value funnel and potential-revenue estimate;
- integration and component coverage for chronology, pagination, company isolation, role separation, and calculated values.

It excludes:

- new operational or financial entities;
- payment or receipt confirmation;
- a custom dashboard date-range filter;
- analytics infrastructure, background aggregation, or third-party dependencies;
- changes to Employee dashboard behavior beyond preserving its existing schedule focus.

## Equipment History Side Panel

The equipment list exposes a history icon for each item. Selecting it opens a temporary MudBlazor side panel anchored to the right, without navigating away from the list.

The panel requests the data only after it opens. The first request loads the equipment identity, the total number of completed-work-order records, and the first twenty records. While that request is in progress, the panel shows a centered circular loading indicator. Its header displays the loaded and total count in the form `20 of 87 records`; Brazilian Portuguese UI text is used in the rendered component.

The timeline is ordered by work-order completion date descending. A row presents the service and work-order display number, localized completion date, professional, checklist completed/required count, before and after photo counts, attachment count, total work-order amount, execution notes when available, and the linked future maintenance due date when one exists. It never exposes storage keys, public tokens, or unrelated customer data.

The client uses infinite scrolling. When the timeline approaches its end and more records exist, it requests the next twenty records from the server. A circular progress indicator is rendered at the end of the current list while that request is active. Closing the panel, opening it for another equipment item, or retrying after an error clears prior paging state. The panel offers a Portuguese empty state when no completed work order exists and a retry action for a failed request.

The application contract contains an explicit paged result and continuation information. A focused equipment repository projection applies `EquipmentId` and the current `CompanyId` in the database query before returning the equipment or any history item. Offset paging is sufficient for the initial twenty-record batches because work-order history is immutable after completion; the query uses a stable descending completion date and identifier ordering.

## Owner Daily Actions Dashboard

The Owner dashboard retains its weekly calendar. Above it, it renders a concise daily-action section containing clickable count cards for:

- appointments occurring on the current company-local date;
- overdue maintenance opportunities;
- actionable maintenance opportunities due within the next seven days;
- pending public appointment requests;
- sent quotes awaiting a decision;
- pending or in-progress work orders.

Cards use warning or danger semantics only when action is needed. A zero value remains visible but neutral. Each card points to its corresponding existing operational page and uses an applicable filter where that page supports one. The dashboard remains useful when all values are zero.

Only Owners receive the daily action aggregate and commercial values. Employees keep the existing personal-schedule dashboard and do not receive management or revenue figures.

## Monthly Value Funnel

Below the daily actions, the Owner sees a `Maintenance results` section for the current calendar month in the configured company timezone. This first version has no user-selectable period.

The section contains these deterministic metrics:

- **Opportunities identified:** maintenance opportunities created in the current month.
- **Customers contacted:** distinct customers with at least one contact marked as sent in the current month.
- **Maintenance-originated requests:** appointment requests linked to a maintenance opportunity and created in the current month.
- **Recovered bookings:** maintenance opportunities converted to appointments in the current month.
- **Services performed:** completed work orders whose source appointment can be traced to an appointment request linked to a maintenance opportunity, completed in the current month.
- **Recovered revenue:** the sum of the total amount of those completed work orders. It is not a received-payment amount and never uses a quote total.
- **Open opportunities and estimated potential revenue:** actionable maintenance opportunities together with the sum of the configured base price of their recommended service where a base price exists. Missing prices are excluded rather than treated as zero-value completed revenue.

The projection reads the existing relationships among maintenance opportunities, customer contacts, appointment requests, appointments, quotes, and work orders. It does not infer a relationship merely from a matching customer or equipment. The query exposes the metrics in a dedicated Owner dashboard DTO and calculates company-local date boundaries from `TimeProvider` and the scheduling timezone configuration.

## Architecture and Security

Application owns use cases, DTOs, current-company authorization, local-business-date calculation, and aggregation semantics. Infrastructure adds focused read projections to existing persistence abstractions and executes EF Core queries. Razor components invoke use cases through the existing component operation executor and render only the returned DTOs.

Every projection resolves tenant identity from the authenticated context. All database queries apply `CompanyId` before projection. Equipment history is available only to authenticated company members for their own equipment. The Owner dashboard aggregate is available only to company Owners. No browser field determines a company identifier or authorization outcome.

This design introduces no schema change because all required relationships and fields already exist. If implementation reveals a missing deterministic source link for a metric, that metric must not be inferred; the work pauses for a separate approved persistence design.

## Error Handling and Loading

Read failures do not leave stale values presented as current data. The equipment panel shows its loading, empty, error, and retry states independently from the equipment list. Dashboard failures preserve the existing error presentation and permit retrying the summary load. The API/use-case layer uses explicit not-found or forbidden outcomes for expected inaccessible data and reserves exceptions for unexpected failures.

## Testing

Tests use a deterministic `TimeProvider` and cover:

- equipment history chronology, first and subsequent twenty-record pages, count reporting, and no cross-company disclosure;
- equipment-panel source/component behavior for opening, initial loading, infinite-scroll loading, empty, and retry states;
- current-company date boundaries for each dashboard count;
- unique-customer contact counting and maintenance-source traceability;
- recovered revenue as the sum of linked completed work-order totals only;
- exclusion of unrelated work orders, quotes, and data from other companies;
- Owner access and Employee exclusion for dashboard management and value data.

## Acceptance Criteria

A company member can open an equipment history panel from the list, see a circular loader, see an accurate `loaded of total` count, and scroll to load immutable completed-work-order records twenty at a time without exposing another company's information. An Owner can see current daily actions and the monthly maintenance funnel, including recovered revenue equal to the total of completed maintenance-linked work orders. An Employee remains limited to the personal scheduling experience.
