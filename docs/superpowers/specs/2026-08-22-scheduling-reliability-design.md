# Scheduling Reliability Design

## Scope

This change resolves the remaining Phase 4 scheduling findings without changing the intentional slot-listing behavior. It covers concurrent booking protection, server-side professional qualification validation, and professional-local time display.

## Concurrent Booking Protection

PostgreSQL will enforce that open appointments for the same company and professional do not overlap. The migration will install the `btree_gist` extension when needed and add a partial exclusion constraint over `CompanyId`, `ProfessionalId`, and the UTC appointment range. The constraint applies only to `Scheduled` and `InProgress` appointments, so completed and cancelled appointments do not reserve time.

Application availability checks remain in place for immediate feedback. Persistence will translate an exclusion-constraint violation into the existing unavailable-slot result, so a competing request receives a clear business response instead of an internal error.

## Professional Qualification

Appointment creation and reassignment will validate that the selected professional is assigned to the appointment service. This validation is enforced in application use cases and therefore also protects HTTP callers that bypass the UI. Work-order-linked appointments continue to use the work order service as the authoritative service.

## Professional-Local Calendar Display

Appointment data returned to the UI will include the responsible professional's time-zone identifier. Dashboard and schedule rendering will convert an appointment's UTC range using that time zone. In the owner calendar, each appointment is grouped under its professional-local date and displays its professional-local time. The date range used to query appointments will include a small UTC boundary buffer so appointments near a local day boundary are not omitted.

## Error Handling

When a booking loses a concurrency race, the user receives the existing unavailable-time message and the client reloads the available slots. Other database errors remain unexpected errors and are not misclassified.

## Verification

Static review will confirm the migration, provider-specific constraint name, qualification checks, DTO propagation, and UI conversion paths. The repository policy does not allow builds or application startup unless explicitly requested.
