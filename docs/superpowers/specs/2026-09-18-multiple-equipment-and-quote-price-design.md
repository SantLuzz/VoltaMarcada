# Multiple Equipment and Quote Price Design

## Goal

Allow a work order and quote to be composed of items associated with multiple equipment items, show a concise equipment summary in compact views, list every equipment item in quote PDFs, and preserve the price entered while resolving a service request into a quote.

## Equipment ownership

Item rows are the source of truth for equipment selection. The work order and quote registration pages will no longer show a top-level equipment selector during creation. Each service, part, or material row can select the equipment it concerns.

The existing aggregate-level equipment identifier remains for backward compatibility with appointments, quotes approved into work orders, historical records, and compact APIs. On create and update it is derived from the first item that has both an equipment and a service. Existing aggregate values remain the fallback when no item-level association exists.

## Presentation

Application DTO mapping will calculate the distinct item-level equipment descriptions. Compact surfaces, including lists and schedule context, show the actual equipment description when there is one and `N equipamentos selecionados` when there is more than one. Detail screens preserve the item-level associations rather than collapsing them.

## Quote documents

The quote document model will receive every distinct equipment description referenced by quote items. The PDF shows those descriptions in a dedicated equipment section; it never replaces them with the compact count.

## Service request commercial resolution

The commercial-resolution dialog uses the existing Brazilian currency input. For a fixed-price service, the input is populated from the configured price and disabled because the service catalogue is authoritative. For other price types, the value stays editable and is persisted in the created quote's primary service item.

## Validation and tests

Tests cover the equipment summary for one and multiple selected equipment items, the quote document equipment list, and the fixed versus editable price behavior in the service-request resolution flow. Build and the relevant test projects are run after the change.
