# Public Request Availability Refactoring Design

> Status: Proposed on 2026-08-27.

## Goal

Refactor the public booking flow so customers request a preferred service time
from the company without seeing or choosing a professional. The company uses a
central availability engine to select a professional and propose a time. A
confirmed appointment is created only after the customer accepts that proposal
and the server revalidates availability.

This phase deliberately does not create quotes. A later phase will introduce
appointment types and quote behavior: service appointments can initiate a
quote directly, while visit appointments can initiate a quote only after the
visit is completed.

## Scope

The phase includes:

- Company business hours with one public operating interval per weekday.
- A company-level public-request interval with an initial default of 60
  minutes.
- Public company and contextual maintenance pages without professional
  selection or professional names.
- A reusable availability engine that evaluates every eligible professional at
  an internally requested time.
- Internal request handling that proposes a selected professional and time.
- Customer acceptance or rejection of a proposal through a secure response
  link.
- Transactional availability revalidation and appointment creation after
  customer acceptance.
- Configurable alternative-search limits in `Scheduling` options.

The phase excludes quotes, quote documents, payment behavior, automatic
professional selection, multiple business-hour intervals per day, and changes
to 30-minute professional schedule blocks.

## Current-State Change

The existing public flow exposes professionals, calculates real professional
availability before submission, and stores a required `ProfessionalId` on an
`AppointmentRequest`. This phase changes public slots into company-accepted
preferences. A public request therefore has no assigned professional and no
real availability reservation.

Professional availability remains the authority for internal appointment
decisions. The new public business hours never modify professional schedules,
schedule blocks, service duration, or appointment collision rules.

## Business Hours and Public Slots

Add a company-owned business-hours model with one record per weekday:

- `DayOfWeek`
- `IsOpen`
- `StartTime`
- `EndTime`

The interval is valid only when `IsOpen` is true and `EndTime` is after
`StartTime`. Store the public-request interval as a company setting named
`PublicRequestIntervalMinutes`, defaulting to 60. Validate it as a positive
whole-minute interval that divides an hour.

The authenticated company settings page manages these values. Public pages use
them to present start times from `StartTime`, inclusive, through the last
interval start before `EndTime`. A closed day has no selectable public times.

## Appointment Request Model

Make `AppointmentRequest.ProfessionalId` optional while the request is
pending. A public request retains the requested local date, local time,
service, customer, equipment, duration, and customer-profile proposal. It does
not store a requested UTC interval because a professional time zone is unknown
until the company selects a professional.

The request persists proposal data separately:

- proposed date and local start time;
- proposed duration;
- proposed professional identifier;
- secure response token hash, expiry, and revocation data.

The domain exposes explicit behaviors to propose a time, accept a proposal,
decline a proposal, and create the confirmed appointment. A customer declining
a proposal transitions the request to a customer-declined terminal state. The
company can still reject a pending request with its internal rejection reason.

## Availability Engine

Replace public-slot-specific availability calculation with an application
service that receives a company, service, requested local date, requested
local start time, and cancellation token. It returns a result containing:

- every eligible professional available at the requested time;
- chronological alternatives when no professional is available at that time.

Eligible professionals must be active, assigned to the service, working on the
date, available for the entire service duration, and free of overlapping open
appointments and schedule blocks. Availability is evaluated in consecutive
30-minute professional-schedule blocks, including the requirement that the
service end does not exceed the professional's work interval.

When the requested time has no eligible professional, search later times on
the same local date and then future local dates in chronological order. Each
alternative contains one time and one eligible professional. The engine uses:

```json
"Scheduling": {
  "AvailabilitySearchDays": 7,
  "AvailabilityResultLimit": 10
}
```

Exact requested-time results are not subject to `AvailabilityResultLimit`;
every eligible professional is returned. The options have positive startup
validation and are never hardcoded in the engine.

## Public and Internal Flow

```text
Customer selects service, equipment, date, and preferred public slot
  -> AppointmentRequest is created without a professional or calendar hold
  -> Owner analyzes the request with the availability engine
  -> Owner chooses an eligible professional and a requested-time result or an alternative
  -> System sends a secure proposal-response link
  -> Customer accepts or rejects the proposed time
  -> On acceptance, server revalidates availability in a transaction
  -> AppointmentRequest becomes Accepted and an external Appointment is created
```

The public customer sees only the company, requested or proposed date/time,
service, and safe context required to act. The customer never sees a
professional identifier or professional name. The public pages state that the
initial selected time is only a preference subject to company confirmation.

The authenticated request queue displays the customer, service, equipment,
requested preference, compatible professionals at that time, and alternatives
when required. It does not automatically choose the first available
professional.

Customer acceptance creates the appointment automatically because the owner
has already selected the professional and proposed time. The final operation
uses the existing scheduling transaction and collision protections. If the
proposal became unavailable, no appointment is created and the request returns
to the owner for a new proposal.

## Contextual Maintenance Requests

Contextual maintenance links retain their secure company, customer, equipment,
maintenance-opportunity, and recommended-service context. They use the same
business-hours public slots and create the same unassigned appointment request
as the public company page. Browser input cannot alter the contextual
identifiers.

The existing maintenance-opportunity transition to `Booked` occurs only when
the accepted proposal successfully creates its appointment.

## Boundaries and Security

The Domain project owns company-business-hours invariants and appointment
request state transitions. Application owns the availability engine, option
contract, tenant-scoped orchestration, and explicit result models.
Infrastructure owns EF Core mappings, repository queries, and the migration.
Web owns the company-settings controls, public forms, owner queue interaction,
and safe Brazilian Portuguese messages.

Public operations resolve a company only from its slug or a validated token.
They do not trust company, professional, customer, equipment, service, or
maintenance-opportunity identifiers supplied by the browser. Every internal
request query and mutation remains scoped to the authenticated company.

## Future Appointment Types

The next phase introduces an appointment type on confirmed appointments, not
on the public preference itself. It branches only after scheduling:

- A service appointment may create its quote directly.
- A visit appointment does not create a quote until the visit is completed.

Keeping quote behavior out of this phase prevents the availability engine and
public-request flow from depending on commercial rules. The current phase must
not prevent adding this type later: appointment confirmation remains separate
from work-order and quote creation.

## Verification

Repository rules prohibit builds, tests, application launches, and migration
execution unless explicitly requested. Implementation verification will use
static review of tenant filters, domain state transitions, option validation,
entity configurations and migration consistency, dependency registration,
public data exposure, and the final transactional revalidation path.
