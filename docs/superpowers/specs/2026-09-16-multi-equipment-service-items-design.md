# Multi-Equipment Service Items Design

> Status: Approved for implementation on 2026-09-16.

## Goal

Allow a work order, quote, and public appointment request to cover several customer equipment items and several services in one operational flow. A service may be associated with a specific equipment item, while general commercial lines such as a visit fee or travel remain possible without equipment.

Every document generated from a work order or quote must present this relationship clearly.

## Scope

This change includes:

- Multiple equipment-and-service entries in work orders and quotes.
- Optional equipment association for general service, part, material, travel, and custom lines.
- Multiple equipment-and-service entries in public booking requests.
- Scheduling duration and professional eligibility derived from all requested services.
- Quote PDFs and every emitted work-order PDF showing items grouped by equipment and a separate general-items group.
- Compatible migration of existing single-equipment and single-service records.

This change does not introduce inventory, payment changes, automatic appointment confirmation, or customer self-service editing of an existing request.

## Domain Model

### Service Items

`WorkOrderItem` and `QuoteItem` become the commercial and operational line-item source of truth. Each line retains its snapshot description, quantity, unit price, type, and display order. It gains nullable `EquipmentId` and nullable `ServiceId` references.

`ServiceId` is required only for a line whose type is `Service`; it is optional for non-service lines. `EquipmentId` is always optional. When provided, it must identify active equipment of the work order or quote customer within the same company. A service line can therefore be associated with an equipment item, and a general line can remain unassociated.

The aggregate holds the item collection and protects replacement and updates while its lifecycle permits them. Totals remain calculated from immutable line snapshots and are never accepted from the browser.

The legacy `WorkOrder.EquipmentId`, `WorkOrder.ServiceId`, `Quote.EquipmentId`, and quote primary-service fields remain only as transitional compatibility references where required by existing flows. New behavior must use the ordered items. Read models expose the collection and derive a concise summary from it rather than treating a header reference as authoritative.

### Public Request Items

`AppointmentRequest` owns an ordered collection of requested service items. Each requested item has a required public-service identifier and an equipment reference. For a public visitor, equipment details are submitted as part of each item, then matched to an existing customer equipment item or created after server-side validation. The request persists only company-scoped equipment identifiers, never untrusted browser identifiers.

The same equipment can have more than one requested service. The UI adds and removes equipment/service rows without exposing professionals. A request must contain at least one service item.

## Work-Order and Quote Flow

Internal creation and editing present a customer selector followed by an editable item list. An operator adds an equipment/service item, chooses one of that customer's active equipment records when applicable, and may add an unlinked general line. The existing quick equipment creation flow remains available.

Checklist behavior is explicit: every service item snapshots its configured checklist items into the work order. Completion validates the combined required checklist set and each service-specific photo requirement. An assigned professional must be qualified for every service item. A single appointment created from a work order represents the entire combined visit and uses the summed duration of its service items.

Quote approval copies every item, including its equipment association and service identity, into the generated work order. The process remains atomic and tenant-scoped.

## Scheduling and Public Booking

Requested duration equals the sum of unique requested service-item durations, including repeated services when they appear on separate lines. The availability engine evaluates every service in the request and returns only professionals qualified for all of them. Its existing collision and consecutive-block checks apply to the combined duration.

Public booking continues to expose company public-request slots only. The selected time is a preference; it does not reveal or select a professional. On internal analysis and final acceptance, the server revalidates the full item set, professional eligibility, and total interval.

When an accepted request generates a work order, it copies all item associations. A conventional single-equipment appointment reference may use the first equipment item only as a display fallback; it is not used to discard or authorize the others.

## Documents

`QuotePdfDocument` and the work-order document contract receive resolved line equipment display data. Generators must not query persistence directly.

Documents render one group per linked equipment, containing its identification (type, manufacturer, model, serial number, and location when available) and its item rows. Lines without an equipment association render in a final `General services and items` group. Each row shows description, quantity, unit price, line total, and relevant category. Existing totals, discounts, notes, customer details, and company branding remain unchanged.

This rule applies to every current or future work-order PDF output, including download, print, customer sharing, and quote-conversion paths.

## Persistence and Migration

New item columns and request-item tables use company-scoped composite foreign keys to equipment and services. Appropriate indexes support `(CompanyId, WorkOrderId, DisplayOrder)`, `(CompanyId, QuoteId, DisplayOrder)`, and appointment-request item lookup.

The migration backfills each existing work order's required header service and equipment into its matching service line. If no matching service line exists, it creates one using the service snapshot and existing commercial data. Existing quotes receive an equivalent service line. Existing appointment requests receive one request item from their legacy service and equipment values.

Legacy columns are retained during this release so old routes and historical records remain readable. New writes populate item collections and may keep a first-item header reference only for transitional consumers. A later, separately approved cleanup may remove obsolete header columns after all readers are migrated.

## Security and Validation

All authenticated commands resolve the current company server-side and validate every customer, equipment, and service selection against it. Equipment must belong to the selected customer. Inactive services cannot be newly selected, but historical snapshots stay readable.

Public submissions validate each input server-side, resolve the company only from the public slug or secure contextual token, and never accept persisted equipment IDs as authorization. Contextual booking tokens restrict items to their authorized customer and company context.

Expected validation failures return operation results and safe Portuguese UI messages. Final appointment creation revalidates the complete set to protect against concurrent scheduling changes.

## Application and Web Boundaries

The Domain project owns item invariants, totals, and protected collection behavior. Application use cases validate ownership, resolve item inputs, compute duration and service eligibility, and coordinate atomic conversion. Infrastructure owns EF configurations, migrations, and document implementations. Razor components only collect item input and render operation outcomes.

The internal work-order and quote screens use a reusable equipment/service-item editor. The public booking page uses a deliberately simpler version that permits equipment rows and services but never displays professional information or internal prices.

## Verification

Implementation must include deterministic automated tests for:

- multiple items with distinct equipment and multiple services on one work order and quote;
- general lines without equipment;
- customer and tenant ownership validation for every selected equipment and service;
- full-duration calculation and professional eligibility across all requested services;
- public request persistence and conversion with multiple equipment items;
- quote-to-work-order copying of all associations;
- PDF document grouping and equipment display data;
- migration backfill and compatibility reads.

Run the focused and relevant integration test suite, build the solution, and inspect generated PDF output before completion.
