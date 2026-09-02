# Pending work for 2026-09-02

## Scheduling from maintenance opportunities

- When an appointment is created from a maintenance opportunity without a linked work order, set its type to `Visit` (technical visit).
- Keep `Service` (service execution) available only when a work order is linked or created before the appointment.
- Add a regression test for this path.

## Quote form formatting and localization

- Use the existing Brazilian currency input/mask for quote discount and quote item unit-price fields.
- Show Brazilian Portuguese labels for quote item types in the UI instead of enum values such as `Service` and `Part`.
- Verify decimal entry, currency display, persistence, and the PDF output.
