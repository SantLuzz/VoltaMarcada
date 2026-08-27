# AGENTS.md

## Project Overview

VoltaMarcada is a multi-tenant SaaS platform for service companies and independent technicians.

The product is designed around the complete service lifecycle:

Customer -> Equipment -> Work Order -> Service Execution -> Maintenance Reminder -> Customer Contact -> Booking Request -> Appointment -> New Work Order

The core product differentiator is not only work order management. VoltaMarcada must help service companies identify future maintenance opportunities, contact customers at the right time, and convert maintenance reminders into new appointments and work orders.

The initial target market is small service companies such as HVAC, refrigeration, technical assistance, appliance maintenance, electrical services, solar maintenance, pool maintenance, IT support, and equipment maintenance.

The first validation niche is HVAC and refrigeration companies.

---

## Technology Stack

Use the following stack unless an explicit task says otherwise:

- .NET 10
- C#
- ASP.NET Core
- Blazor Web App
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- xUnit when automated tests are introduced after MVP validation
- FluentValidation when validation complexity justifies it

Do not introduce additional frameworks, architectural styles, messaging systems, ORMs, frontend frameworks, or infrastructure dependencies without a clear requirement.

Prefer built-in .NET and ASP.NET Core capabilities before adding third-party dependencies.

---

## Language Rules

All source code must be written in English.

This includes:

- namespaces
- classes
- interfaces
- records
- structs
- enums
- methods
- properties
- fields
- variables
- constants
- database table names
- database column names
- migration names
- test names
- exception names
- validation messages used internally
- comments
- XML documentation
- logs
- technical documentation
- branch names
- suggested commit messages

Never create Portuguese identifiers in source code.

Examples:

```csharp
public sealed class MaintenanceReminder
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public DateTime DueDate { get; private set; }
}
```

Do not write:

```csharp
public sealed class LembreteManutencao
{
    public DateTime DataPrevista { get; set; }
}
```

User-facing UI text may be written in Brazilian Portuguese because the initial product market is Brazil.

Keep domain terminology and source code identifiers in English even when UI labels are localized.

---

## Solution Structure

Keep the solution organized as follows:

```text
VoltaMarcada.slnx

src/
    VoltaMarcada.Domain/
    VoltaMarcada.Application/
    VoltaMarcada.Infrastructure/
    VoltaMarcada.Web/

tests/
    VoltaMarcada.Domain.Tests/
    VoltaMarcada.Application.Tests/
    VoltaMarcada.IntegrationTests/
```

Do not create additional projects unless there is a concrete technical reason.

### VoltaMarcada.Domain

Contains enterprise and domain rules only.

Allowed responsibilities:

- Entities
- Value Objects
- Enums
- Domain Exceptions
- Domain Services when behavior truly spans multiple domain objects
- Domain Events when they provide clear value

The Domain project must not depend on:

- EF Core
- ASP.NET Core
- Identity
- PostgreSQL
- infrastructure libraries
- UI frameworks

### VoltaMarcada.Application

Contains application behavior and use cases.

Allowed responsibilities:

- Use Cases
- Commands and Queries when useful
- DTOs
- Interfaces for infrastructure concerns
- Application validation
- Mapping
- Authorization rules that depend on application context

Application must depend on Domain, but must not depend directly on Infrastructure or Web.

Prefer explicit use cases over generic service classes.

Prefer:

```text
CreateCustomer
CreateWorkOrder
CompleteWorkOrder
CreateMaintenanceReminder
GetUpcomingMaintenanceReminders
CreateBookingRequest
AcceptBookingRequest
AnalyzeAppointmentRequestAvailability
GetAvailableAppointmentSlots
RescheduleAppointment
```

Avoid vague classes such as:

```text
CustomerService
WorkOrderService
MaintenanceService
GeneralManager
HelperService
```

### VoltaMarcada.Infrastructure

Contains technical implementations.

Responsibilities include:

- EF Core DbContext
- PostgreSQL persistence
- Entity configurations
- Repository implementations when repositories are actually needed
- ASP.NET Core Identity integration
- PDF generation
- storage
- external integrations
- date/time provider implementation
- email or messaging integrations

### VoltaMarcada.Web

Contains the web host and presentation layer.

Responsibilities include:

- Blazor pages and components
- HTTP endpoints
- authentication UI
- authorization configuration
- dependency injection composition
- public booking pages
- dashboard pages
- work order pages
- quote pages

Do not place business rules directly inside Razor components.

---

## Architectural Principles

Use pragmatic Clean Architecture principles.

Do not over-engineer the solution.

The project should optimize for:

1. clarity
2. maintainability
3. explicit business behavior
4. testability
5. fast product iteration

Avoid architecture for architecture's sake.

Do not introduce unnecessary layers such as generic repositories, unit of work abstractions over EF Core, mediator libraries, event buses, or CQRS infrastructure unless a real requirement justifies them.

EF Core DbContext can be used directly by application use cases through an application persistence abstraction if that produces simpler code.

Prefer small, explicit interfaces over broad abstractions.

---

## Multi-Tenancy

VoltaMarcada is multi-tenant from the beginning.

`Company` is the tenant boundary.

All business data that belongs to a tenant must include a `CompanyId`.

Examples include:

- Customer
- CustomerAddress
- Equipment
- Professional
- Service
- Appointment
- AppointmentRequest
- WorkOrder
- Quote
- MaintenanceReminder

Never trust `CompanyId` received from the browser, route, query string, or request body for authenticated tenant operations.

Resolve the current tenant from the authenticated user context.

Use abstractions similar to:

```csharp
public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}

public interface ICurrentCompany
{
    Guid CompanyId { get; }
}
```

Every tenant-scoped query and mutation must enforce company isolation.

Never allow records from one company to be read, updated, linked, or deleted by another company.

When loading a tenant-owned entity, filter by both its identifier and `CompanyId` whenever practical.

Example:

```csharp
var customer = await dbContext.Customers
    .SingleOrDefaultAsync(
        x => x.Id == customerId && x.CompanyId == currentCompany.CompanyId,
        cancellationToken);
```

Do not load by ID first and check tenant ownership later when the ownership filter can be applied directly in the query.

---

## Authentication and Users

Use ASP.NET Core Identity.

The initial user roles are:

- Owner
- Employee

Future roles may include:

- Administrator
- Technician
- Receptionist
- Financial

Do not implement future roles until needed.

The initial registration flow is:

1. Create user account.
2. Create company.
3. Link the user to the company.
4. Assign the user as company owner.
5. Redirect to onboarding or dashboard.

Authentication and company creation must be treated as a single consistent business flow.

---

## Core Domain Model

The initial core domain should include the following concepts.

### Company

Represents the tenant.

Expected information may include:

- Id
- LegalName
- TradeName
- Document
- Phone
- WhatsApp
- City
- State
- LogoUrl
- Slug
- CreatedAt
- UpdatedAt

The public `Slug` must be unique.

### CompanyBusinessHours

Defines the hours in which a company accepts public service requests. Each
company may configure one interval per day of the week, including whether the
company serves customers on that day and its local start and end times.

Company business hours determine the dates and times exposed on public pages.
They must not replace, modify, or be inferred from an individual
professional's availability.

The company also configures the public request interval. Its initial default
is 60 minutes and it is independent from the 30-minute blocks used by the
internal professional schedule.

### CompanyUser

Links an authenticated user to a company and stores company-level membership information.

### Customer

Represents a person or business receiving services.

A customer can be created manually or originate from a public booking request.

A customer does not require a work order.

A work order does not require the customer to have originated from a public booking flow.

### CustomerAddress

A customer may have multiple addresses.

Do not store only one address directly on `Customer`.

Example address labels may include home, office, store, or another service location.

### Equipment

Represents equipment owned or used by a customer.

Typical fields may include:

- CustomerId
- CompanyId
- Type
- Manufacturer
- Model
- SerialNumber
- Capacity
- LocationDescription
- InstallationDate
- Notes

Equipment is a central part of VoltaMarcada because maintenance history and future maintenance reminders may be linked to a specific equipment item.

### Professional

Represents a technician or service professional.

A professional may optionally be linked to a system user.

Do not require every professional to have login access.

### Service

Represents a service offered by the company.

Expected fields include:

- Name
- Description
- DurationMinutes
- PriceType
- BasePrice
- AllowOnlineBooking
- Active

Recommended price type enum:

```csharp
public enum ServicePriceType
{
    Hidden,
    Fixed,
    StartingAt,
    QuoteRequired
}
```

A service price must not be mandatory.

### ProfessionalService

Represents which professionals can perform which services.

Do not assume every professional can perform every service.

### ProfessionalAvailability

Defines recurring working availability.

Example:

```text
Monday
08:00-12:00
13:00-18:00
```

### ScheduleBlock

Represents unavailable time that overrides regular availability.

Examples:

- vacation
- personal appointment
- training
- manual block
- holiday

### Appointment

Represents a confirmed or internally created calendar commitment.

An appointment may exist independently from a work order.

An appointment may optionally reference:

- Customer
- Equipment
- Service
- Professional
- WorkOrder
- AppointmentRequest

A work order may have multiple appointments.

This is required to support flows such as:

- diagnostic visit
- equipment pickup
- service execution
- return visit
- installation

### AppointmentRequest

Represents a booking request that has not necessarily been confirmed yet.

Public booking must create an `AppointmentRequest`, not immediately create a confirmed `Appointment` unless the company explicitly adopts an automatic confirmation mode in the future.

The requested date and time are the customer's preference. A public request
does not select, expose, or assign a professional. A professional is selected
only by an authorized internal user after availability analysis.

Initial statuses should support concepts equivalent to:

- Pending
- Accepted
- Rejected
- RescheduleRequested
- Cancelled

### AppointmentRescheduleHistory

Never lose the previous appointment time when rescheduling.

Persist history containing at least:

- PreviousStartAt
- PreviousEndAt
- NewStartAt
- NewEndAt
- Reason
- ChangedByUserId when applicable
- CreatedAt

### WorkOrder

Represents the operational service record.

A work order may be created manually without a previous booking request, appointment, or quote.

This is a required business rule.

A customer may contact the company directly through WhatsApp, phone, walk-in, referral, or another channel. The company must be able to create a customer and work order immediately.

A work order may optionally be associated with:

- Customer
- Equipment
- Professional
- Quote
- one or more Appointments

Suggested status concepts:

- Pending
- Scheduled
- InProgress
- Completed
- Cancelled

Do not tightly couple work order creation to the public booking flow.

### WorkOrderItem

Represents services, parts, materials, or other billable work order items when needed.

### Quote

Represents a commercial estimate.

A quote is optional.

A work order does not require a quote.

Suggested statuses:

- Draft
- Sent
- Approved
- Rejected
- Expired

### QuoteItem

Represents quote line items.

### MaintenanceReminder

This is one of the most important domain concepts in VoltaMarcada.

When a work order is completed, the technician may recommend a future maintenance date or interval.

Example:

```text
Service completed: 2026-08-18
Recommended maintenance interval: 90 days
Expected maintenance date: 2026-11-16
```

The system creates a maintenance reminder linked to the appropriate company, customer, equipment, previous work order, and optionally recommended service.

Suggested fields:

- Id
- CompanyId
- CustomerId
- EquipmentId
- PreviousWorkOrderId
- ServiceId
- DueDate
- Status
- Notes
- CreatedAt
- UpdatedAt

Suggested status concepts:

- Pending
- DueSoon
- Contacted
- BookingRequested
- Booked
- Completed
- Dismissed

Do not call a future maintenance reminder a reschedule.

A reschedule changes an existing appointment.

A maintenance reminder generates a future return opportunity and normally creates a new appointment and a new work order.

---

## Core Business Flow

The primary recurring maintenance flow is:

```text
Work Order Completed
        -> Maintenance Reminder Created
        -> Reminder Becomes Due Soon
        -> Company Contacts Customer
        -> Company Sends Contextual Booking Link
        -> Customer Selects Preferred Time
        -> Appointment Request Created
        -> Company Analyzes Availability and Selects a Professional or Alternative
        -> Customer Confirms the Proposed Time
        -> Appointment Created
        -> New Work Order Created or Linked
        -> Service Completed
        -> New Maintenance Reminder Created
```

This recurring cycle is the core product differentiator.

Do not design features that force all work orders to originate from this flow.

---

## Manual Workflows Must Always Be Supported

The application must support all of the following workflows.

### Direct Work Order

```text
Customer contacts company by WhatsApp
-> Company creates or selects customer
-> Company creates work order
```

No appointment or quote is required.

### Manual Appointment

```text
Customer contacts company
-> Company creates or selects customer
-> Company creates appointment
-> Work order may be created before or after
```

### Quote First

```text
Customer
-> Quote
-> Approval
-> Work Order
```

### Public Booking

```text
Public company page
-> Booking request
-> Internal availability analysis and proposal
-> Customer confirmation
-> Appointment
-> Optional work order
```

### Maintenance Return

```text
Maintenance reminder
-> Customer contact
-> Contextual booking link or manual booking
-> Appointment
-> New work order
```

---

## Scheduling Rules

Real professional availability, used for internal scheduling, appointment
request analysis, rescheduling, and appointment confirmation, must consider
at least:

- professional availability
- service duration
- existing appointments
- schedule blocks
- company boundaries
- selected date

Do not offer a time slot if the full service duration overlaps another appointment or unavailable period.

Example:

A service requires 120 minutes.

If an existing appointment is scheduled from 10:00 to 12:00, do not offer any candidate start time whose resulting interval overlaps 10:00-12:00.

Slot calculation belongs in Application/domain-oriented logic, not Razor components.

Prefer a use case such as:

```text
GetAvailableAppointmentSlots
```

The availability engine must receive the service, requested date, and
requested time; identify every eligible professional; and validate consecutive
30-minute blocks for the full service duration. It must return all
professionals available at the requested time, never select the first one
automatically.

If no eligible professional is available at the requested time, search other
times on the same date and then future dates in chronological order. Return
time-and-professional alternatives while honoring configurable limits:

```json
"Scheduling": {
  "AvailabilitySearchDays": 7,
  "AvailabilityResultLimit": 10
}
```

The limits must come from configuration, not hardcoded values. Every final
appointment creation or acceptance must revalidate the selected professional
and interval on the server.

### Public Request Availability

Public request slots are generated from company business hours and the public
request interval. They express when the company accepts a preferred time; they
do not represent real-time professional availability and must not expose any
professional to the customer.

The public page must clearly state that the selected time is a preference that
will be analyzed and confirmed by the company. It must use the same equipment
model and validation rules as internal requests, not a simplified public-only
model.

The result should represent valid start times, not arbitrary calendar cells.

---

## Rescheduling Rules

Rescheduling applies only to an existing appointment.

A reschedule must:

1. validate the new interval
2. ensure the professional is available
3. ensure no appointment collision exists
4. preserve the previous schedule in history
5. update the appointment
6. record who performed the change when available

Do not create a new work order only because an appointment was rescheduled.

---

## Maintenance Reminder Rules

When completing a work order, allow the user to optionally create a maintenance reminder.

Possible inputs:

- predefined interval such as 30, 60, 90, or 180 days
- custom interval
- explicit due date
- recommended service
- notes

The user must never be forced to create a maintenance reminder.

A reminder should normally be linked to equipment when the service concerns a specific equipment item.

Maintenance reminders should be visible by operational urgency, including:

- overdue
- due soon
- future
- contacted
- booked
- completed

The dashboard should prioritize actionable reminders rather than merely displaying totals.

---

## Contextual Booking Links

A maintenance reminder may generate a secure contextual booking token.

A contextual booking link may already know:

- Company
- Customer
- Equipment
- Recommended Service
- MaintenanceReminder

The public user must not be able to change identifiers to access another company's or another customer's private context.

Use random, non-guessable tokens.

Do not expose internal database IDs as the only authorization mechanism for public links.

Public pages must reveal only the data necessary for the intended action.

The contextual booking link should allow the customer to select a preferred day
and time from public request slots and submit an `AppointmentRequest`.

The initial version should require company confirmation before an appointment becomes confirmed.

---

## Public Company Page

Each company may have a public page using its unique slug.

Example route concept:

```text
/{companySlug}
```

The page may show:

- company name
- logo
- description
- services available for online booking
- optional public prices
- booking flow

Do not expose inactive services or services where `AllowOnlineBooking` is false.

---

## WhatsApp Integration Strategy

For the MVP, do not integrate the official WhatsApp Business API unless explicitly requested.

Prefer opening WhatsApp with a pre-filled message and a secure VoltaMarcada link.

Typical actions include:

- send quote
- send maintenance reminder
- send booking link

Keep message construction isolated behind an application or infrastructure abstraction if it starts becoming reused.

Do not put URL formatting logic throughout UI components.

---

## Quote and PDF Rules

Quotes should support:

- services
- parts
- materials
- travel fees
- discounts
- notes
- expiration date
- total

PDF generation must be behind an abstraction such as:

```csharp
public interface IQuotePdfGenerator
{
    Task<byte[]> GenerateAsync(QuoteDocument document, CancellationToken cancellationToken);
}
```

Do not couple Application directly to a PDF library.

A quote should also support a secure public URL so the customer can view and approve it without requiring authentication.

---

## Date and Time Rules

Avoid direct calls to `DateTime.Now`, `DateTime.UtcNow`, or equivalent APIs inside domain and application logic.

Use a time abstraction such as `TimeProvider` or an application interface.

Persist timestamps in UTC unless a specific field represents a local business date or local clock time by definition.

Company scheduling must eventually respect the company's configured timezone.

Do not assume all companies operate in the server timezone.

When handling pure dates, consider `DateOnly`.

When handling pure local times, consider `TimeOnly`.

Be explicit about UTC versus local time.

---

## Entity Design Rules

Prefer entities that protect their state.

Avoid public setters on domain entities unless there is a strong reason.

Prefer behavior methods such as:

```csharp
workOrder.Start(now);
workOrder.Complete(now);
appointment.Reschedule(newStartAt, newEndAt, reason, changedByUserId, now);
maintenanceReminder.MarkAsContacted(now);
```

instead of letting application code arbitrarily mutate properties.

Keep constructors and factory methods focused on valid states.

Validate invariants close to the domain when possible.

Do not create an anemic model solely to make EF Core mapping easier.

At the same time, do not move every trivial assignment into unnecessary domain methods.

Use pragmatic judgment.

---

## Identifier Rules

Prefer `Guid` for aggregate and entity identifiers unless a specific requirement says otherwise.

Use server-generated identifiers.

Do not expose sequential public identifiers as security tokens.

Human-readable numbers such as work order numbers and quote numbers should be separate from entity IDs.

Example:

```text
Id: 4a082827-...
Number: 42
Display: OS #000042
```

Work order and quote number generation must be scoped correctly per company if the business requires company-specific numbering.

---

## EF Core Rules

Use Fluent API configurations in Infrastructure.

Prefer one configuration class per significant entity.

Example:

```text
Persistence/
    VoltaMarcadaDbContext.cs
    Configurations/
        CompanyConfiguration.cs
        CustomerConfiguration.cs
        EquipmentConfiguration.cs
        WorkOrderConfiguration.cs
```

Add appropriate unique indexes and foreign key indexes.

Examples likely to require unique constraints:

- Company.Slug
- user/company membership combinations

Indexes should reflect actual query patterns.

Do not add indexes blindly.

Use migrations for schema changes.

Migration names must be descriptive and written in English.

Example:

```text
AddMaintenanceReminders
AddAppointmentRescheduleHistory
AddCompanySlug
```

---

## Database Naming

Database objects must use English names.

Choose one naming convention and apply it consistently.

If PostgreSQL snake_case naming conventions are configured globally, use them consistently.

Do not manually mix PascalCase, camelCase, and snake_case database object names.

---

## Validation

Validation belongs at the appropriate layer.

Examples:

Domain invariant:

```text
Appointment end must be after appointment start.
```

Application validation:

```text
The selected customer must belong to the current company.
```

UI validation:

```text
Required field feedback and formatting guidance.
```

Do not rely only on frontend validation.

All commands and public requests must be validated server-side.

---

## Error Handling

Do not use exceptions for expected control flow.

Use explicit result models when an operation can reasonably fail due to business conditions.

Examples:

- appointment slot no longer available
- customer not found in current company
- booking token expired
- quote cannot be approved in current state

Exceptions are appropriate for unexpected failures and invariant violations.

Do not expose internal exception messages or stack traces to users.

---

## Concurrency

Scheduling is concurrency-sensitive.

A slot shown as available may become unavailable before confirmation.

Always revalidate appointment availability on the server immediately before creating or accepting the final appointment.

Do not rely on the availability result previously rendered to the browser.

Where necessary, use database constraints, transactions, or concurrency controls to prevent double booking.

Do not implement a complex distributed locking system unless real deployment requirements justify it.

---

## Security

Security rules are mandatory.

Always enforce:

- authentication for internal company features
- tenant isolation
- authorization for company operations
- secure random public tokens
- server-side validation
- no trust in hidden form fields for authorization
- no secrets committed to source control

Secrets must come from configuration providers, environment variables, development secrets, or the hosting environment.

Never hardcode:

- passwords
- connection strings
- API keys
- JWT secrets
- SMTP credentials
- storage credentials

Do not log sensitive customer data unnecessarily.

---

## Testing Strategy

Automated tests are not required during the initial MVP validation.

When the product reaches a stabilization phase or a specific task requests tests, use xUnit and keep tests deterministic. Do not depend on the machine clock; inject a time provider instead.

---

## UI and UX Principles

The dashboard should answer:

> What does this company need to act on today?

Prioritize actionable information such as:

- today's appointments
- overdue maintenance reminders
- maintenance reminders due soon
- pending booking requests
- quotes awaiting customer response
- open work orders

Avoid dashboards that only show vanity metrics.

The product should feel simple enough for small service businesses that currently operate mainly through WhatsApp.

Do not require users to understand ERP terminology.

Keep common actions obvious:

- New Customer
- New Work Order
- New Appointment
- Send Booking Link
- Create Quote
- Complete Work Order
- Schedule Maintenance

---

## MVP Scope

The MVP should focus on validating the recurring service cycle.

Included areas:

1. authentication
2. company registration
3. tenant isolation
4. customer management
5. customer addresses
6. equipment management
7. professionals
8. services
9. professional availability
10. work orders
11. appointments
12. rescheduling and history
13. maintenance reminders
14. maintenance reminder dashboard
15. public company page
16. company business hours and public request granularity
17. public booking requests without professional selection
18. centralized availability analysis and alternatives
19. contextual maintenance booking links
20. quotes
21. quote PDF
22. WhatsApp share links

The following are explicitly outside the initial MVP unless requested:

- inventory management
- NFS-e or NF-e issuance
- accounting
- payroll
- complex accounts payable
- official WhatsApp Business API
- native mobile application
- online payment processing
- advanced digital signatures
- route optimization
- internal chat
- AI features
- complex CRM automation
- complex commissions
- marketplace features

Do not implement out-of-scope modules merely because they are common in ERP systems.

---

## Recommended Development Order

Follow this sequence unless current requirements justify changing it.

### Phase 1 - Foundation

- Create solution structure
- Configure project references
- Configure PostgreSQL
- Configure EF Core
- Configure ASP.NET Core Identity
- Implement registration and login
- Implement Company
- Implement CompanyUser
- Implement current user/company context
- Establish tenant isolation

### Phase 2 - Operational Base

- Customers
- Customer addresses
- Equipment
- Professionals
- Services
- Professional services
- Availability
- Schedule blocks

### Phase 3 - Work Orders

- Manual work order creation
- Work order details
- Equipment association
- Professional association
- Work order status transitions
- Completion flow

### Phase 4 - Scheduling

- Internal calendar
- Available slot calculation
- Manual appointment creation
- Work order appointments
- Multiple appointments per work order
- Appointment rescheduling
- Reschedule history

### Phase 5 - Maintenance Recurrence

- Maintenance reminder creation on work order completion
- Due date calculation
- Upcoming reminders
- Overdue reminders
- Contact status
- Maintenance dashboard

### Phase 6 - Public Booking

- Public company page
- Public services
- Slot availability
- Appointment requests
- Accept/reject flow
- Contextual maintenance booking token
- Contextual booking page

### Phase 7 - Public Request Refactoring and Availability Engine

- Company business hours with one operating interval per day
- Public request granularity, initially 60 minutes
- Public request flow without professional exposure or selection
- Preferred-time messaging and internal professional assignment
- Centralized availability engine over professional schedules
- All eligible professionals at the requested time
- Chronological alternative time-and-professional results
- Configurable availability search days and result limit
- Reuse by public requests, internal scheduling, rescheduling, and maintenance recurrence

### Phase 8 - Quotes

- Quote creation
- Quote items
- Quote statuses
- Quote PDF
- Public quote token
- Public approval/rejection
- WhatsApp share action

### Phase 9 - Enhancements

- Work order photos
- before/after photos
- attachments
- richer history
- simple customer confirmation flows

---

## Definition of the First Sellable MVP

The product can be considered commercially testable when this flow works reliably:

```text
Company creates account
-> Company creates customer
-> Company creates equipment
-> Company creates work order
-> Technician completes service
-> Technician schedules future maintenance
-> VoltaMarcada surfaces the upcoming maintenance
-> Company contacts customer
-> Company sends contextual booking link
-> Customer selects preferred slot
-> Company accepts booking request
-> Appointment is created
-> New work order can be created
```

This flow matters more than building many secondary modules.

---

## Coding Style

Follow modern C# conventions.

Prefer:

- file-scoped namespaces
- nullable reference types enabled
- async APIs for I/O
- `CancellationToken` propagation
- `DateOnly` and `TimeOnly` when semantically appropriate
- `TimeProvider` for current time
- small focused methods
- explicit names
- immutable DTOs when useful
- records for request/response models when appropriate

Avoid:

- `async void`
- `.Result`
- `.Wait()`
- unnecessary static mutable state
- magic strings
- magic numbers
- giant service classes
- business logic in controllers or Razor components
- generic catch blocks that silently swallow exceptions

Use `ConfigureAwait(false)` only when there is a clear project-level reason. Do not scatter it by habit in ASP.NET Core code.

---

## Naming Conventions

Use standard .NET naming conventions.

Examples:

```text
CreateWorkOrderUseCase
CompleteWorkOrderUseCase
GetAvailableAppointmentSlotsUseCase
CreateMaintenanceReminderUseCase
```

If the project later standardizes on omitting `UseCase` suffixes, apply that consistently instead of mixing styles.

Commands should describe actions.

Queries should describe requested information.

Boolean names should read naturally:

```csharp
IsActive
AllowOnlineBooking
IsCompleted
HasConflict
```

Avoid abbreviations unless they are universally understood in the domain.

---

## API and Endpoint Guidelines

Do not create endpoints that expose persistence models directly.

Use dedicated request and response contracts.

Validate tenant ownership server-side.

Use appropriate HTTP status codes.

Public endpoints and authenticated endpoints must be clearly separated.

Public token-based endpoints must validate the token before loading contextual private data.

Do not expose `CompanyId` as a way to select the tenant for authenticated business operations.

---

## Logging

Use structured logging.

Prefer:

```csharp
logger.LogInformation(
    "Work order {WorkOrderId} completed for company {CompanyId}",
    workOrder.Id,
    companyId);
```

Avoid string interpolation in structured log messages.

Do not log passwords, tokens, secrets, or unnecessary personally identifiable information.

---

## Comments and Documentation

Code should generally explain itself through naming and structure.

Add comments only when they explain:

- non-obvious business reasoning
- important trade-offs
- unusual technical constraints
- temporary compatibility decisions

Do not write comments that merely restate the code.

All comments must be in English.

---

## Change Discipline

When implementing a task:

1. Understand the relevant domain flow before changing code.
2. Inspect existing patterns and follow them.
3. Prefer the smallest coherent change that satisfies the requirement.
4. Do not refactor unrelated areas without a concrete reason.
5. Add or update automated tests only when the task explicitly requires them.
6. Perform the relevant non-executing validation before declaring the task complete.
7. Keep migrations and schema changes intentional.
8. Mention important assumptions in the final implementation summary.

Do not silently introduce breaking changes.

## Build and Application Execution

Do not run builds or start the application during normal task execution.

This includes commands and tools such as:

- `dotnet build`
- `dotnet test`, because it builds projects by default
- `dotnet run`
- `dotnet watch`
- launching the web application, development server, or any background host process

Use static inspection and targeted code review to validate changes unless the user explicitly requests a build, test run, or application launch.

---

## Product Decision Filter

Before adding a new feature, evaluate whether it helps at least one of these goals:

1. Help the company execute services better.
2. Help organize customers and equipment.
3. Help identify and recover returning customers.
4. Help convert maintenance reminders into appointments.
5. Reduce administrative work for service companies.

If a feature does not strongly support one of these goals, it is probably not part of the MVP.

---

## Product Principle

The most important product principle is:

> VoltaMarcada should not merely record what already happened. It should tell the service company which customers should be contacted next.

The system should become the operational memory of the company.

A technician should not need to remember that a customer's equipment is due for maintenance.

VoltaMarcada should surface that opportunity, help the company contact the customer, provide a booking path, and connect the resulting appointment back to the customer's equipment and service history.

Preserve this principle when making architectural, UX, and domain decisions.

# VoltaMarcada Frontend UI / UX Design System

The VoltaMarcada frontend must look and behave like a modern, professional, clean, and consistent SaaS application.

The UI must prioritize:

- clarity
- usability
- visual hierarchy
- consistency
- responsiveness
- accessibility
- low cognitive load
- fast task completion

VoltaMarcada is primarily used by service technicians and small service businesses.

The interface must feel simple, organized, trustworthy, and professional even when the underlying workflow is complex.

Frontend work is not complete simply because the functionality works.

The visual quality, consistency, responsiveness, component reuse, loading states, validation states, and overall UX are part of the implementation requirements.

---

# UI Refactoring Rule

Whenever modifying an existing frontend page, first inspect its current visual structure.

If the page does not comply with the VoltaMarcada UI rules defined in this document, refactor the relevant layout, spacing, components, form controls, and visual hierarchy to conform to the design system before completing the task.

Existing bad UI is not a reason to propagate the same pattern.

Do not preserve inconsistent styling just because it already exists.

Every new page and every modified page must follow the same:

- visual language
- spacing system
- component usage
- color palette
- typography
- responsive behavior
- interaction patterns

When working on an existing page, improve obviously inconsistent UI when it is reasonably related to the requested change.

Do not create page-specific visual patterns unless there is a strong UX reason.

---

# MudBlazor is Mandatory

MudBlazor is the default and mandatory UI component library for the VoltaMarcada frontend.

Whenever MudBlazor provides a component for a UI element, use the MudBlazor component.

Never manually recreate a UI or form control that already exists in MudBlazor.

All user-editable fields must use an appropriate MudBlazor input component.

Raw HTML form fields are forbidden by default.

Before implementing any form field or interactive UI element, check whether an equivalent MudBlazor component already exists.

Custom input components are only allowed when MudBlazor cannot satisfy the requirement.

Even custom reusable VoltaMarcada input components should internally use MudBlazor components whenever possible.

## Component Mapping

Prefer:

- Text input -> `MudTextField`
- Email input -> `MudTextField`
- Password input -> `MudTextField`
- Multiline text -> `MudTextField` with multiline configuration
- Numeric input -> `MudNumericField`
- Select -> `MudSelect`
- Autocomplete -> `MudAutocomplete`
- Checkbox -> `MudCheckBox`
- Radio -> `MudRadioGroup`
- Date input -> `MudDatePicker`
- Time input -> `MudTimePicker`
- Date range -> MudBlazor date range components
- Switch -> `MudSwitch`
- Button -> `MudButton`
- Icon button -> `MudIconButton`
- Table -> `MudTable` or `MudDataGrid`
- Dialog -> `MudDialog`
- Snackbar -> MudBlazor Snackbar
- Tabs -> `MudTabs`
- Menu -> `MudMenu`
- Chip -> `MudChip`
- Tooltip -> `MudTooltip`
- Pagination -> `MudPagination`
- Loading -> MudBlazor progress components
- Icons -> MudBlazor icons
- Navigation -> MudBlazor navigation components when appropriate

Do not use raw HTML controls such as:

`<input>`

`<select>`

`<textarea>`

`<button>`

unless there is a documented technical reason and MudBlazor cannot satisfy the requirement.

Do not introduce another UI component library for functionality already available in MudBlazor.

---

# VoltaMarcada Official Logo

The official VoltaMarcada logo asset is located at:

`src/VoltaMarcada.Web/wwwroot/logos/logo.png`

This is the canonical VoltaMarcada logo and must be reused throughout the application.

Because the file is located under `wwwroot`, Blazor components should reference it using:

`/logos/logo.png`

Do not duplicate the logo into another folder.

Do not create alternative versions of the logo unless explicitly requested.

Do not recreate the VoltaMarcada logo using:

- plain text
- CSS
- MudBlazor icons
- generic icons
- generated SVGs
- alternative images

when the official asset can be used.

Always preserve the original aspect ratio.

Never stretch or distort the logo.

---

# Visual Identity

The VoltaMarcada visual identity is based on blue tones derived from the official logo.

## Primary Colors

Primary:

`#0969F0`

Primary Hover:

`#0758CC`

Accent:

`#08BDF2`

Navy:

`#071D49`

## Neutral Colors

Application Background:

`#F6F8FC`

Surface / Cards:

`#FFFFFF`

Border:

`#E2E8F0`

Primary Text:

`#172033`

Secondary Text:

`#64748B`

## Semantic Colors

Success:

`#16A34A`

Warning:

`#F59E0B`

Danger:

`#DC2626`

These colors must be centralized in the application theme.

Prefer configuring them through the VoltaMarcada `MudTheme`.

Do not hardcode these colors repeatedly across individual pages.

Do not introduce random colors without a strong semantic or design reason.

---

# MudBlazor Theme

VoltaMarcada should maintain a centralized MudBlazor theme.

The theme should be the primary source of visual configuration for:

- primary color
- secondary/accent color
- background
- surfaces
- typography
- border appearance
- semantic colors
- default component appearance where supported

Prefer theme-level customization over page-level CSS.

Do not solve global design problems with duplicated page-specific CSS.

---

# Typography

Use `Inter` as the primary application font whenever available.

Typography must remain consistent throughout the application.

## Page Title

- Size: approximately 28px
- Weight: 700
- Primary text color

## Section Title

- Size: approximately 20px
- Weight: 600

## Card Title

- Size: approximately 16px
- Weight: 600

## Body

- Size: 14px or 16px
- Weight: 400

## Secondary Text

- Size: approximately 14px
- Secondary text color

Avoid unnecessary typography variations.

Do not create arbitrary font sizes for individual pages.

Use MudBlazor typography components or shared typography rules whenever appropriate.

---

# Spacing System

VoltaMarcada must use a consistent spacing system.

Prefer an 8px-based spacing system.

Allowed spacing values should generally come from:

- 4px
- 8px
- 12px
- 16px
- 24px
- 32px
- 48px

Avoid arbitrary spacing values unless strictly necessary.

The most common spacing values should be:

- 8px for small internal gaps
- 16px between related elements
- 24px between groups
- 32px between major page sections

Whitespace is intentional.

Do not compress screens simply to fit more information.

Pages should feel comfortable and visually balanced.

---

# Application Layout

Authenticated desktop application pages should use a consistent application shell.

The standard structure should normally be:

- Sidebar
- Topbar
- Page Header
- Main Content

Main content should use:

- comfortable horizontal padding
- comfortable vertical padding
- consistent maximum width where appropriate

Recommended desktop page padding:

`24px` to `32px`

Recommended maximum content width:

approximately `1440px`

Never place page content directly against browser edges.

Avoid large unused areas when content could be presented more effectively.

---

# Page Header

Every management page should have a consistent page header.

The page header should normally contain:

- Page title
- Short description
- Primary action aligned to the right when applicable

Example:

```text
Clientes                              + Novo cliente


The VoltaMarcada frontend must look like a modern professional SaaS application.

### Visual identity

Primary: #0969F0
Primary Hover: #0758CC
Accent: #08BDF2
Navy: #071D49

Background: #F6F8FC
Surface: #FFFFFF
Border: #E2E8F0

Text Primary: #172033
Text Secondary: #64748B

Success: #16A34A
Warning: #F59E0B
Danger: #DC2626

### Typography

Use Inter as the primary font.

Page title:
- 28px
- 700 weight

Section title:
- 20px
- 600 weight

Card title:
- 16px
- 600 weight

Body:
- 14px or 16px
- 400 weight

Secondary text:
- 14px
- muted color

### Spacing

Use an 8px-based spacing system whenever possible.

Allowed spacing values:

4px
8px
12px
16px
24px
32px
48px

Never use arbitrary spacing unless strictly necessary.

### Layout

Desktop pages must use a consistent application shell:

Sidebar
Topbar
Page header
Main content

Main content must have:

max-width: 1440px
padding: 24px to 32px

Never place content directly against screen edges.

### Page Header

Every management page should have:

Title
Short description
Primary action aligned to the right

Example:

Clientes                         [+ Novo cliente]
Gerencie seus clientes e equipamentos.

### Cards

Cards must use:

white background
1px subtle border
12px border radius
16px or 24px internal padding
very subtle shadow when appropriate

Avoid excessive shadows.

### Forms

Forms must never look like raw HTML forms.

Use:

clear labels
consistent field heights
help/error text
logical grouping
adequate vertical spacing

Prefer 2-column layouts on desktop when appropriate.

Use 1-column layout on mobile.

Primary actions should be visually obvious.

### Tables

Tables must have:

comfortable row height
clear headers
subtle separators
hover state
empty state
loading state
responsive behavior

Actions should preferably use an action menu instead of several buttons.

### Buttons

Primary action:
filled primary color

Secondary action:
outlined or neutral

Destructive action:
danger styling

Avoid displaying more than one primary button in the same section.

### Status

Never communicate status using color alone.

Use badges with text.

Examples:

Agendada
Em andamento
Concluída
Cancelada
Aguardando aprovação

### Empty States

Never leave an empty table or blank card.

Provide:

short explanation
optional icon
clear CTA

Example:

Nenhum cliente cadastrado

Cadastre seu primeiro cliente para começar a criar
ordens de serviço.

[Adicionar cliente]

### Responsive Design

All screens must support:

Desktop
Tablet
Mobile

Do not create desktop-only layouts.

Tables should adapt appropriately on small screens.

### UX principles

Prioritize:

clarity
visual hierarchy
low cognitive load
fast task completion
consistent navigation
accessible contrast
predictable interactions

VoltaMarcada is used by service technicians and small businesses.

The UI must feel simple even when the underlying feature is complex.

### Important

Before implementing a new page:

1. Identify the primary user goal.
2. Define the information hierarchy.
3. Reuse existing components.
4. Follow the VoltaMarcada design system.
5. Consider loading, empty, error and success states.
6. Consider desktop and mobile layouts.
7. Only then implement the page.

Do not create isolated visual styles for individual pages.

Do not introduce new colors, spacing conventions,
button styles or card styles without a strong reason.
