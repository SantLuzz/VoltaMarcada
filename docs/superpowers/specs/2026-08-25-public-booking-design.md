# Phase 7 Public Booking Design

> Status: Completed on 2026-08-26.

## Goal

Close VoltaMarcada's digital maintenance recurrence cycle by allowing a customer to submit a public booking request, allowing the company to handle that request in its existing scheduling area, and creating a confirmed appointment only after internal confirmation.

The phase also establishes the company public page and a Company Settings tab as the central place for public profile details and future company settings.

## Scope

The phase includes:

- Company Settings with public-page link, public profile fields, notification email, and a copy action.
- A public company page resolved by the existing unique company slug.
- Public service, professional, date, and compatible-slot selection.
- Customer creation or tenant-scoped matching by Brazilian document during a public booking request.
- `AppointmentRequest` persistence and lifecycle.
- Public contextual maintenance-booking links secured by a random token.
- Handling public requests in the existing `/agenda` area, including confirmation, alternate-time suggestion, and rejection.
- `Appointment` origin tracking for internal versus external appointments.
- An email-notification abstraction for a newly submitted public request, without an SMTP implementation.

The phase explicitly excludes quotes, quote PDFs, public quote approvals, payments, and SMTP credentials or an SMTP sender implementation.

## Existing Foundations

Companies already have a unique `Slug`, logo, trade name, WhatsApp number, and location. Services already have `AllowOnlineBooking`, duration, active state, and professional assignments. Professionals have working schedules, time zones, schedule blocks, and availability rules.

The existing scheduling module already calculates compatible slots, validates availability again at appointment creation, protects against concurrent open-appointment overlap, and creates appointments in a scheduling transaction. The new public flow reuses those rules instead of duplicating scheduling logic.

Maintenance opportunities already progress through `BookingRequested` and `Booked`, and appointment creation can update an opportunity to `Booked` atomically. The public contextual flow extends that behavior.

## Company Settings and Public Profile

The authenticated owner-only `/empresa` page becomes tabbed:

- **Overview** retains the current company information and team-management experience.
- **Settings** contains the public page link, public profile configuration, notification email, and room for future company settings.

The Settings tab shows a computed, copyable URL composed from the application base URL and the company slug. This URL is not stored as a duplicate database value.

`Company` gains the following optional values:

- `PublicDescription`, with a bounded plain-text length.
- `PublicPhone`, with a bounded, normalized phone number.
- `NotificationEmail`, with a valid, bounded email address.

The notification-email field initially shows the company owner's email when no company-specific notification email is configured. Once saved, the company value is the recipient for public-request notifications. The public page displays the existing WhatsApp number, public phone when configured, and public description when configured.

The company slug remains generated during onboarding and is not editable in this phase.

## Public Company Page

The route `/{companySlug}` renders a public page only for an existing company. It shows only the company logo, trade name, configured public description and contacts, and services that are both active and available for online booking.

The booking flow is:

```text
Service
  -> Specific professional or any available professional
  -> Date
  -> Compatible time slot
  -> Customer identification and details
  -> Appointment request submitted
```

When the visitor chooses any available professional, the application finds slots for active professionals qualified for the selected service. Each returned slot retains the applicable professional; the request stores the selected professional.

Public availability uses the service duration and considers professional working hours, breaks, schedule blocks, existing appointments, company isolation, and the professional time zone. A displayed slot is advisory only and is revalidated when the request is accepted.

## Customer Identification and Privacy

The public booking flow requires a valid CPF/CNPJ and the fields required to create a customer: name, WhatsApp, optional email, and a primary address.

The document lookup is restricted to the company resolved by the public page or secure token. The response never reveals whether a matching customer exists and never returns previously stored customer data. This avoids treating a publicly supplied CPF/CNPJ as a data-disclosure key.

On submission:

- If no company customer has the document, the use case creates the customer and the appointment request in one transaction.
- If the document belongs to an existing company customer, the request links to that customer and retains the submitted contact and address values as a proposed profile update.
- Existing customer data is never changed automatically from a public request. The company can compare and apply the proposal from `/agenda` while handling the request.

This keeps customer creation automatic for a new customer while preventing a person who only knows another person's document from reading or overwriting the existing record.

All user supplied values are server-validated, length-bounded, and rendered only as encoded text. No public text field accepts or renders HTML markup.

## Appointment Requests and Appointment Origin

`AppointmentRequest` is a company-scoped aggregate containing:

- Its identifiers, customer, service, and assigned professional.
- Requested local date and time, duration, and the corresponding UTC interval.
- Status, customer profile-update proposal, optional internal notes, rejection reason, and an optional company-proposed replacement interval.
- Timestamps and, when applicable, the confirmed appointment identifier.
- Optional maintenance-opportunity, equipment, and contextual-link associations.

Its states are:

```text
Pending -> Accepted
        -> Rejected
        -> RescheduleRequested -> Pending
        -> Cancelled
```

The customer cannot directly create an `Appointment`. A request does not reserve the selected slot and does not block the calendar.

`Appointment` gains an `AppointmentOrigin` enum:

- `Internal` for appointments created manually by the company.
- `External` for appointments confirmed from public requests.

New manual appointments use `Internal`; accepting a request creates `External` atomically with the request transition. Existing appointments are migrated as `Internal`.

## Scheduling Management

There is no separate internal requests module. The authenticated `/agenda` page is the central scheduling experience and adds a request queue alongside the calendar. It distinguishes internal and external confirmed appointments and shows external pending requests.

For each request, authorized company users can:

- **Confirm:** revalidate availability within the scheduling transaction, optionally apply the proposed customer update, create the external appointment, set the request to `Accepted`, and mark its maintenance opportunity as `Booked` when present.
- **Suggest another time:** choose a compatible replacement slot. The request becomes `RescheduleRequested` and the system exposes a secure customer link for responding.
- **Reject:** record an optional reason and set the request to `Rejected`.

When the customer accepts a suggested time or selects another available time through the secure request link, the request returns to `Pending`; company confirmation remains mandatory.

All tenant-scoped internal queries filter by the authenticated user's current company. Employees retain the existing scheduling access restrictions.

## Contextual Maintenance Booking

A maintenance opportunity can generate a contextual booking link. A public contextual page knows its company, existing customer, equipment, recommended service, and opportunity only after validating the link token server-side.

It displays the maintenance context and allows selection of professional, date, and compatible time. The customer association, equipment, service, and maintenance opportunity cannot be changed by URL or form identifiers.

The `MaintenanceOpportunity` aggregate stores one current link state:

- `BookingTokenHash`.
- `BookingTokenExpiresAtUtc`.
- `BookingTokenRevokedAtUtc`.

The raw token is a 32-byte cryptographically random value. It is stored only as a SHA-256 hash and is never logged. Generating a replacement token invalidates the preceding token; the company can also revoke it explicitly. Token lifetime is configured through `PublicBooking:MaintenanceLinkLifetimeDays` in `appsettings`, defaults to 30 days, and must be positive at startup.

Generating a contextual link marks the opportunity as `BookingRequested`. Confirming its request marks it as `Booked` in the same transaction that creates the appointment.

## Public Security and Reliability

Public operations resolve company context only from a slug or verified token. They never trust a `CompanyId`, customer identifier, equipment identifier, opportunity identifier, or service identifier supplied by a browser.

Public request, profile update, and public response endpoints have antiforgery protection, input validation, and named rate-limiting policies. Public error responses use safe Brazilian Portuguese text and never disclose whether a document exists, token details, internal identifiers, or exception data.

The final internal acceptance always revalidates service eligibility, professional assignment, availability, schedule blocks, and concurrent appointment collisions. A conflict leaves the request unaccepted and asks the company to choose another time.

## Email Notification Contract

Application defines a narrow interface similar to:

```csharp
public interface IPublicBookingNotificationSender
{
    Task SendNewAppointmentRequestAsync(
        string recipientEmail,
        string companyDisplayName,
        string customerName,
        string serviceName,
        DateOnly requestedDate,
        TimeOnly requestedStartTime,
        string agendaUrl,
        CancellationToken cancellationToken);
}
```

The notification includes only the information needed by the company to act: recipient, company display name, customer name, selected service, requested date/time, and an authenticated agenda URL. It does not include public token values, passwords, or unnecessary customer data.

Phase 7 creates only this contract; it registers no SMTP implementation, dispatches no message, and stores no SMTP credentials. A later Infrastructure implementation will fulfill the interface and will be invoked only after a request has been persisted, so an email-delivery failure can never roll back or report failure for the customer request.

## Persistence and Application Boundaries

Infrastructure adds Fluent configurations, repository methods, mappings, and descriptive migrations for the new entity and fields. Public and authenticated flows use explicit application use cases and dedicated request/response DTOs; no persistence model is exposed directly from endpoints or Razor components.

The Domain project remains independent of EF Core, ASP.NET Core, storage, and email providers. The Application layer owns the notification abstraction, public booking orchestration, ownership checks, and explicit operation errors. The Web layer owns public routes, forms, authenticated agenda interactions, and safe presentation.

## Verification

In accordance with repository rules, this implementation does not run builds, tests, the application, or migrations unless explicitly requested. Static validation covers:

- Domain state transitions and protected entity state.
- Tenant-filtered repository queries and secure token handling.
- Service/professional eligibility and final availability revalidation.
- Entity configurations, migration, and model-snapshot consistency.
- Dependency registrations and endpoint contracts.
- Razor routes, settings tabs, public loading/error states, encoded text rendering, and request queue actions.
- The absence of SMTP credentials and an SMTP implementation.
