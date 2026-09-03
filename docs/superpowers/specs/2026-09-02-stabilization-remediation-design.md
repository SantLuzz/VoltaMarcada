# Stabilization and Remediation Design

## Purpose

Stabilize the current VoltaMarcada application before the next functional
phase. The work corrects verified workflow defects, responsive layout issues,
security gaps, inconsistent formatting, and authorization boundaries while
preserving the existing business rules that are not explicitly changed here.

The Maintenance Opportunity lifecycle refactor is already complete. This
design does not alter its states, conversion behavior, or persistence model.

## Scope

The stabilization phase covers:

- authenticated password change, password recovery, SMTP delivery, login rate
  limiting, and temporary Identity lockout;
- a shared user-facing time-zone and Brazilian formatting abstraction;
- public booking equipment capture and persistence;
- authenticated and public layout responsiveness;
- post-success form-state reset behavior;
- Agenda appointment-editor behavior, external-request assignment, and
  owner/employee visibility;
- Work Order ordering, visual presentation, monetary formatting, and resilient
  mobile photo upload;
- Maintenance Opportunity page spacing, mobile actions, and use of the shared
  time-zone formatter;
- targeted automated regression coverage.

It does not add an external email provider, change the already-correct primary
menu order, redesign the visual identity, create a separate scheduling engine,
or reimplement the Maintenance Opportunity lifecycle.

## Global Constraints

- Source code, migrations, configuration names, logs, and technical documents
  remain in English. User-facing copy remains Brazilian Portuguese.
- MudBlazor remains the required UI component library.
- Tenant data is always resolved from the authenticated company context; client
  identifiers never authorize a tenant operation.
- Timestamps remain persisted in UTC. Scheduling availability continues to use
  the existing professional schedule and availability engine.
- No secrets are added to source control. SMTP credentials are supplied by
  environment configuration or development secrets.
- No database migration is created unless the public equipment flow cannot be
  represented by the existing `AppointmentRequest.EquipmentId` relationship.

## Authentication and Email Delivery

### SMTP delivery

Application defines a narrow email-delivery abstraction for account messages.
Infrastructure implements it with SMTP and strongly typed options. The options
include host, port, sender address, sender display name, TLS mode, username,
and password. Credentials are bound from configuration providers but omitted
from checked-in settings values.

Startup validates the non-secret SMTP shape only when email delivery is
enabled. A development-safe implementation logs message metadata without a
token or reset URL only when delivery is explicitly disabled in development.
Production recovery is not treated as successful without an enabled sender.

### Password change

An authenticated account page accepts current password, new password, and
confirmation. The endpoint resolves the account from the current principal,
uses `UserManager.ChangePasswordAsync`, maps expected Identity validation
errors to user-safe Portuguese messages, and never accepts a user ID from the
form. It invalidates the current security stamp/session as required by the
existing Identity configuration and returns the user to the account page with
success feedback.

### Password recovery

The login page links to a forgot-password page. The request endpoint always
returns the same acknowledgement whether or not the email belongs to an
account. For an existing account it generates an ASP.NET Core Identity reset
token, URL-encodes it into a short-lived application reset link, and sends the
link through the email abstraction. The reset page validates password
confirmation and uses `ResetPasswordAsync`; expired, malformed, and reused
tokens return a safe failure message.

### Login throttling

`appsettings.json` contains a named login-rate-limit section with a fixed
window, permitted attempts, queue limit, and optional partition behavior.
Startup binds and validates the settings, configures ASP.NET Core rate limiting
for `/account/login`, and returns a retry response without permanently
blocking users. Identity login uses `lockoutOnFailure: true` with temporary
lockout options also bound from configuration. Rate limiting protects bursts;
Identity lockout protects repeated credential failures.

## Shared Time Zone and Formatting

A presentation-focused service resolves the current display time zone once per
request/circuit. It uses the time zone of the professional linked to the
authenticated user in the current company; when no professional exists, it
uses `Scheduling:DefaultTimeZoneId`. The service exposes UTC-to-local
conversion and explicit Brazilian Portuguese date, time, date-time, and money
formatting methods.

The service is used by authenticated UI projections and components, including
Agenda, Work Orders, and Maintenance Opportunities. Public pages continue to
display company-public local dates and times using the configured default time
zone, without exposing professionals. Domain and application availability
calculations retain their existing time-zone-aware UTC conversions and are not
duplicated in Razor components.

## Public Booking Equipment

Public booking submits a simple equipment identifier: type/name and
manufacturer. The server resolves the company and customer from the public
company route or secure maintenance token; it never trusts a company or
customer ID posted by the browser.

For standard public booking, the submission searches the current company's
active equipment for the resolved customer using normalized type/name and
manufacturer. It reuses a matching item or creates an `Equipment` entity when
none exists, then stores its ID on the `AppointmentRequest`. Contextual
maintenance booking preserves its already-linked equipment and does not let
the public visitor replace it. Agenda request details show the resolved
equipment name and manufacturer.

## Layout and Form Behavior

### Global headers and navigation

The authenticated app bar shows the company and user display name only; it no
longer renders the email address. Its responsive CSS avoids squeezed account
text. The mobile drawer has an explicit close icon inside its header. The
existing navigation order remains unchanged: Dashboard is first and the other
items retain their current alphabetical order.

The public header uses responsive MudBlazor layout and constrained text for
company name, description, WhatsApp, city, and postal code. Small viewports
wrap content into readable rows rather than clip it or rely on horizontal
overflow.

### Forms

After successful employee-invitation generation and customer-address creation,
the owning component replaces its request model and resets the associated
`MudForm` validation state. Failed submissions retain entered values and
validation feedback. The Professionals page places the schedule-block action
in its own line below the explanatory text.

## Agenda

### Appointment editor

The editor is the only UI for creating appointments and closes only after a
successful creation result. It uses appointment type to control its input
model:

- `Visit` and `Other` hide Service, require a positive manual duration, and
  calculate availability using that duration.
- `Service` requires a Work Order. The server resolves customer, equipment,
  professional, service, and duration from that Work Order. The UI does not
  offer an independent Service selector for this type.

The create use case revalidates the selected professional, duration, and slot
inside the existing scheduling transaction. Owners choose a professional;
employees remain limited to their own professional scope and assigned work.

### External requests

Only company owners receive pending external booking requests in the Agenda.
Employees and professionals never see a request until an appointment has been
accepted and assigned to them.

For a pending request, the existing availability analysis produces all eligible
professionals at the requested time. The owner selects one eligible
professional and can accept the requested time directly. Acceptance receives
that selected ID, fetches the request and professional tenant-scoped, checks
qualification and current availability within the existing scheduling
transaction, assigns the request, and creates the appointment only if every
check succeeds. A stale or conflicting slot produces the existing operation
error and does not partially assign or accept the request.

Request cards include customer, service, requested local date/time, and
equipment. The administrative-information card is removed. List actions use
responsive stacks so Cancel and other actions wrap onto a second line on small
viewports.

## Work Orders

The work-order list query orders non-completed records first by their relevant
scheduled date/time, with a deterministic fallback for records without an
appointment. Completed Work Orders are always placed after active records,
regardless of date or time. The list renders an Urgent priority badge in the
theme danger color and uses responsive controls so the Status filter and row
actions are not clipped on mobile.

The Work Order detail header visibly renders the current status. All unit
prices, subtotals, and totals use the shared Brazilian money formatter, which
produces values such as `R$ 150,00` and avoids manually assembled or
mis-encoded symbols. The appointment tab has the required separation before
its scheduling action.

### Mobile photo resilience

Before opening the camera/upload handoff, the browser stores a captured photo
and its Work Order/photo-type context in persistent browser storage. After a
Blazor Server reconnect or page reload, the photo component restores that
pending payload and retries the existing authenticated upload endpoint. It
clears the local payload only after the server confirms persistence. The server
continues to validate Work Order ownership and photo constraints; client-side
storage grants no authorization.

## Maintenance Opportunities

The existing lifecycle, conversion, contact history, and permissions are left
unchanged. The page separates filter controls from the opportunity grid with
responsive MudBlazor spacing, places mobile action buttons one below another,
and formats contact/sent timestamps through the shared display-time-zone
service.

## Error Handling and Security

Expected conditions use existing operation-result or validation feedback:
invalid password, rate limit reached, unavailable slot, closed request,
missing tenant record, invalid reset token, and failed email delivery. No UI
returns internal exception or SMTP details. Reset tokens, booking tokens,
customer data, credentials, and uploaded photo bytes are not logged.

## Verification

Automated tests cover at least:

- password change authorization and Identity error mapping;
- password recovery's neutral response, token reset behavior, and sender
  invocation without token logging;
- configured login limiter and temporary lockout behavior;
- time-zone fallback and professional-specific date/time formatting;
- public equipment reuse, creation, contextual equipment preservation, and
  tenant isolation;
- employee exclusion from external requests and owner acceptance with an
  explicitly selected eligible professional;
- Visit/Other manual-duration scheduling and Service Work Order-derived
  duration/service behavior;
- Work Order active-before-completed ordering and Urgent badge projection;
- resilient photo-upload recovery behavior at the browser/service boundary;
- form reset behavior after successful invitation and address submissions.

The complete test suite and a solution build run after implementation. Desktop
and mobile layouts are manually verified at representative MudBlazor breakpoints
without introducing global overflow hiding.
