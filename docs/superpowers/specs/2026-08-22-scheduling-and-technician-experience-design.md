# Scheduling and Technician Experience Design

**Date:** 2026-08-22

## Goal

Deliver Phase 4: an operational scheduling system that lets a company manage appointments safely, gives technicians a focused mobile-first view of their work, and keeps work-order execution aligned with every related appointment.

## Scope

The phase introduces appointments, appointment rescheduling history, technician block requests, availability-based slot calculation, owner and technician scheduling dashboards, and a work-order scheduling tab.

The owner dashboard includes only data from modules currently implemented: appointments, work orders, pending block requests, customers, equipment, and services. Quotes, public booking requests, maintenance reminders, and opportunities remain out of scope until their modules exist.

## Scheduling Configuration and Time Zones

`VoltaMarcada.Web/appsettings.json` defines a typed `SchedulingOptions` section:

```json
"Scheduling": {
  "SlotIntervalMinutes": 30,
  "DefaultTimeZoneId": "America/Sao_Paulo",
  "AllowedTimeZoneIds": [
    "America/Sao_Paulo",
    "America/Campo_Grande"
  ]
}
```

`Professional` stores a required IANA time-zone identifier. Existing professionals receive `America/Sao_Paulo` in the migration. The professional setting is validated against the configured allowlist; it is used directly whenever the system displays appointments, interprets local date/time inputs, or calculates slots for that professional.

Appointments persist only `StartAtUtc` and `EndAtUtc`. They do not snapshot a time zone. If the professional changes their time-zone preference, all their appointment displays and future scheduling calculations use the new preference.

## Appointment Model

`Appointment` is a company-scoped entity, independent from `WorkOrder`. It has:

- `Id`, `CompanyId`, `ProfessionalId`, `StartAtUtc`, `EndAtUtc`, `Status`, `Notes`, `CreatedAtUtc`, and `UpdatedAtUtc`;
- optional references to `Customer`, `Equipment`, `Service`, and `WorkOrder`;
- valid states `Scheduled`, `Completed`, and `Cancelled`.

An owner may create a manual appointment by choosing a professional, local date/time, and a duration. A selected service supplies the duration; a manual duration is permitted when no service applies. Customer, equipment, and work-order links are optional for owner-created appointments.

When a technician creates an appointment, it must be inside a work order where the technician is the assigned professional. The appointment must link to that work order and inherits its customer, equipment, service, and professional; the technician provides only a valid local date/time. A technician cannot create a detached appointment, schedule another professional, or alter the work-order details through this flow.

A work order may have zero or more appointments. If it has appointments, the work order can be completed only after every linked appointment is `Completed` or `Cancelled`. A work order without appointments retains its existing direct-completion flow.

## Availability and Slot Calculation

`GetAvailableAppointmentSlotsUseCase` receives a professional, local date, and requested duration. It returns valid local start times at `SchedulingOptions.SlotIntervalMinutes` intervals.

A slot is valid only if its complete interval:

1. falls within the professional's recurring work schedule;
2. does not overlap the configured lunch interval;
3. does not overlap an effective `ScheduleBlock`;
4. does not overlap a `Scheduled` appointment for that professional.

All local values are translated using the professional's current IANA time zone. The server repeats the same validation immediately before appointment creation, rescheduling, approval of a block request, and work-order completion. A stale or newly conflicting request produces an explicit business result rather than silently changing another appointment.

## Rescheduling and Completion

Owners can reschedule, reassign, or cancel appointments. Reassignment validates the existing appointment interval against the receiving professional's current schedule, blocks, and appointments. The assigned technician can complete only their own appointments. Completing an appointment does not complete its work order.

Rescheduling a scheduled appointment validates the new interval and appends an immutable `AppointmentRescheduleHistory` record containing the previous and new UTC intervals, reason, changing user when available, and creation timestamp. The appointment retains its identity and its work-order link.

## Technician Block Requests

Owners may continue to create and remove effective `ScheduleBlock` records directly. Technicians cannot create effective blocks. They create `ScheduleBlockRequest` records for their own professional profile, with type, date/time range, full-day option, and notes.

Block requests have `Pending`, `RequiresAppointmentResolution`, `Approved`, and `Rejected` states. A request without appointment conflicts is pending owner action. A request that overlaps scheduled appointments is marked `RequiresAppointmentResolution` and lists the affected appointments for the owner.

The owner must explicitly reschedule, reassign, or cancel every conflicting appointment before approval becomes available. Approval repeats the conflict query in the same server operation and then creates the effective `ScheduleBlock`. If a new conflict appears, the request returns to `RequiresAppointmentResolution`. No appointment is moved automatically.

Each request preserves the requested interval, notes, decision status, deciding user when available, decision time, and rejection reason when supplied.

## Permissions and Tenant Isolation

Owner users can list and access all appointments and block requests in their company; they create, reschedule, reassign, cancel, and complete appointments, manage effective blocks, and approve or reject block requests.

Employee users can list and access only appointments where `ProfessionalId` is their active professional profile. They can complete those appointments, create only their own work-order-bound appointments, and create block requests only for themselves. Every lookup and mutation includes `CompanyId` and the role/profile predicate in its query; no tenant identifier is accepted from the browser as authorization input.

## User Experience

### Owner Dashboard and Schedule

The owner dashboard presents:

- appointments in the selected period;
- open and in-progress work orders;
- pending and conflict-requiring block requests;
- customer, equipment, and service operational counts and entry points.

The scheduling view defaults to the current week, supports previous/next week navigation, and lets the owner select any start/end date range. It can filter by professional and supports manual appointment creation from the selected period.

### Technician Dashboard and Schedule

The technician receives a dedicated mobile-first dashboard rather than the owner dashboard with hidden menus. It emphasizes the next appointment and shows the technician's schedule for today, the current week, or a selected date range.

Each appointment exposes the relevant customer, service, address, equipment, work order, checklist, photos, execution notes, and relevant equipment history through existing work-order views. The dashboard provides only the technician's own appointments and work orders.

### Work-Order Schedule Tab

The work-order registration page gains an `Agendamentos` tab. It lists linked appointments chronologically with status, local time, detail access, and rescheduling history.

Owners can add, reschedule, reassign, and cancel linked appointments from this tab. The assigned technician can add an appointment under the constrained technician flow and complete their own linked appointments. The tab makes any open appointments visible before a user attempts work-order completion.

## Persistence and Application Boundaries

Domain contains appointment, reschedule-history, and block-request entities, their enums, and state-transition invariants. Application contains explicit create, list, get slots, reschedule, complete, cancel, block-request, and approval use cases plus persistence abstractions. Infrastructure owns EF Core configurations, repositories, and the descriptive migration. Razor components invoke use cases only and do not contain slot or authorization rules.

The migration adds tables and company-scoped indexes for appointment timeline queries, work-order appointment lookups, appointment reschedule history, and block-request review. It also adds the professional time-zone property with the configured default for existing data.

## Error Handling and Concurrency

Expected business outcomes are explicit: professional unavailable, appointment conflict, invalid interval, appointment not found in the current access scope, work order has open appointments, block request requires appointment resolution, and block request is no longer approvable because a new conflict exists.

Mutating scheduling use cases re-query the current schedule immediately before persistence. They use a transaction where creation of a block and approval of its request must occur together. The phase does not introduce distributed locks or external scheduling dependencies.

## Verification Scope

This is MVP validation work. Per repository rules, implementation uses targeted static inspection and does not add or run automated tests, builds, or application hosts unless explicitly requested.
