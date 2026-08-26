# Public Booking Implementation Plan

> Status: Completed on 2026-08-26.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver Phase 7 public booking, contextual maintenance links, internal request handling in the existing schedule, and company public settings.

**Architecture:** Public routes resolve tenant context from the company slug or a verified opaque token and call focused Application use cases. `AppointmentRequest` records an external request without reserving a slot; `/agenda` performs the final transactional availability check and creates an external `Appointment` only upon acceptance. New data is represented by protected domain aggregates, EF configurations, tenant-filtered repositories, and a single schema migration.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor Web App, MudBlazor, Entity Framework Core, PostgreSQL, ASP.NET Core Identity, FluentValidation.

**Spec:** `docs/superpowers/specs/2026-08-25-public-booking-design.md`

## Global Constraints

- All source-code identifiers, migrations, validation messages, logs, comments, and technical documentation are English; UI text is Brazilian Portuguese.
- `CompanyId` for authenticated operations always comes from `ICurrentCompany`; public operations resolve it only from a validated slug or token.
- Use `TimeProvider`; do not call `DateTime.Now` or `DateTime.UtcNow` in Domain or Application code.
- Public token values must be cryptographically random, persisted only as a hash, and never logged.
- Public input is server-validated and rendered as encoded text only; no user-provided HTML is accepted or rendered.
- Add no SMTP provider, SMTP credentials, new framework, generic repository, event bus, or background worker.
- Do not run builds, tests, the application, EF commands, or migrations unless the user explicitly requests them. Validate changes by static inspection.

---

## File Structure

| Path | Responsibility |
| --- | --- |
| `Domain/Enums/AppointmentOrigin.cs` | Distinguish internal from public-confirmed appointments. |
| `Domain/Enums/AppointmentRequestStatus.cs` | Request lifecycle states. |
| `Domain/Entities/AppointmentRequest.cs` | Protected external-request aggregate and profile-update proposal. |
| `Domain/Entities/Appointment.cs` | Origin-aware confirmed appointment. |
| `Domain/Entities/Company.cs` | Public-profile and notification-email state. |
| `Domain/Entities/MaintenanceOpportunity.cs` | Current contextual-token state and behavior. |
| `Application/PublicBooking/*` | Public DTOs, validators, errors, token helpers, and use cases. |
| `Application/Scheduling/*` | Request acceptance/suggestion/rejection and request DTOs. |
| `Application/CompanySettings/*` | Owner-scoped public-profile settings use cases. |
| `Application/Abstractions/Persistence/*` | Small public-booking and request repository contracts. |
| `Application/Abstractions/Communication/IPublicBookingNotificationSender.cs` | SMTP-independent future notification contract only. |
| `Infrastructure/Persistence/Configurations/*` | EF mappings for new fields and request entity. |
| `Infrastructure/Persistence/Repositories/*` | Tenant-filtered persistence and scheduling transaction operations. |
| `Infrastructure/Persistence/Migrations/*` | `AddPublicBooking` migration and model snapshot update. |
| `Web/Configuration/PublicBookingOptions.cs` | Configurable contextual-token lifetime. |
| `Web/Components/Pages/PublicCompany.razor` | Public company-page booking flow. |
| `Web/Components/Pages/PublicMaintenanceBooking.razor` | Contextual maintenance booking flow. |
| `Web/Components/Pages/Schedule.razor` | Internal request queue and actions. |
| `Web/Components/Pages/Company.razor` | Owner Settings tab and public URL copy action. |

## Task 1: Public-booking configuration and future notification contract

**Files:**
- Create: `src/VoltaMarcada.Web/Configuration/PublicBookingOptions.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/IPublicBookingConfiguration.cs`
- Create: `src/VoltaMarcada.Application/Abstractions/Communication/IPublicBookingNotificationSender.cs`
- Modify: `src/VoltaMarcada.Web/appsettings.json`
- Modify: `src/VoltaMarcada.Web/Program.cs`

**Interfaces:**
- Produces `IPublicBookingConfiguration.MaintenanceLinkLifetimeDays` for token-generating use cases.
- Produces `IPublicBookingNotificationSender.SendNewAppointmentRequestAsync(string, string, string, string, DateOnly, TimeOnly, string, CancellationToken)` without registering or calling a concrete implementation.

- [ ] **Step 1: Add options and application-facing configuration contract.**

```csharp
public sealed class PublicBookingOptions
{
    public const string SectionName = "PublicBooking";
    public int MaintenanceLinkLifetimeDays { get; init; } = 30;
}

public interface IPublicBookingConfiguration
{
    int MaintenanceLinkLifetimeDays { get; }
}
```

- [ ] **Step 2: Bind and validate the options at startup.**

```csharp
builder.Services.AddOptions<PublicBookingOptions>()
    .Bind(builder.Configuration.GetSection(PublicBookingOptions.SectionName))
    .Validate(options => options.MaintenanceLinkLifetimeDays > 0,
        "Maintenance link lifetime must be greater than zero.")
    .ValidateOnStart();
builder.Services.AddSingleton<IPublicBookingConfiguration, PublicBookingConfiguration>();
```

Add this non-secret default to `appsettings.json`:

```json
"PublicBooking": { "MaintenanceLinkLifetimeDays": 30 }
```

- [ ] **Step 3: Create the email contract only.**

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

Do not create SMTP options, sender classes, service registrations, credentials, or dispatch calls.

- [ ] **Step 4: Staticaly inspect namespace dependencies and startup validation.**

Confirm the Application contract does not reference Web or Infrastructure, the option has a positive default, and no SMTP configuration appears in tracked configuration files.

## Task 2: Domain changes for public profile, appointment origin, requests, and tokens

**Files:**
- Create: `src/VoltaMarcada.Domain/Enums/AppointmentOrigin.cs`
- Create: `src/VoltaMarcada.Domain/Enums/AppointmentRequestStatus.cs`
- Create: `src/VoltaMarcada.Domain/Entities/AppointmentRequest.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Company.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Appointment.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/MaintenanceOpportunity.cs`

**Interfaces:**
- Produces `Company.UpdatePublicProfile(...)`.
- Produces `AppointmentRequest.Create`, `Accept`, `Reject`, `SuggestNewTime`, `ReturnToPending`, and `ApplyCustomerUpdate` behavior methods.
- Produces `MaintenanceOpportunity.SetBookingToken`, `RevokeBookingToken`, and token-state properties.

- [ ] **Step 1: Define compact enums.**

```csharp
public enum AppointmentOrigin { Internal, External }
public enum AppointmentRequestStatus { Pending, Accepted, Rejected, RescheduleRequested, Cancelled }
```

- [ ] **Step 2: Extend `Company` with public-profile behavior.**

Add nullable `PublicDescription`, `PublicPhone`, and `NotificationEmail` with private setters. Add `UpdatePublicProfile(string? description, string? phone, string? notificationEmail, DateTimeOffset now)` that trims optional values, limits description to 2,000 characters, limits phone to 20 characters, validates email with a bounded, explicit validation helper, and updates `UpdatedAtUtc`.

- [ ] **Step 3: Make appointment origin explicit.**

Add `AppointmentOrigin Origin` to `Appointment.Create` with `AppointmentOrigin.Internal` as the compatibility default. Persist it as `Internal` for all manual callers; accept-request code will pass `External` explicitly.

- [ ] **Step 4: Create the protected request aggregate.**

Store company, customer, service, professional, optional equipment and maintenance-opportunity IDs; requested UTC interval plus local date/time and duration; optional proposed replacement interval; status; optional rejection reason; proposed customer contact/address values; confirmed appointment ID; response-token hash, expiry, and revocation values; and UTC timestamps. Guard all required IDs, valid intervals, allowed state transitions, text lengths, and a proposal that cannot be applied after terminal status.

- [ ] **Step 5: Add contextual-token behavior to `MaintenanceOpportunity`.**

Add `BookingTokenHash`, `BookingTokenExpiresAtUtc`, and `BookingTokenRevokedAtUtc`. `SetBookingToken` accepts a precomputed hash and expiry, requires an open opportunity, clears revocation, and marks the opportunity as booking requested when needed. `RevokeBookingToken` clears access by recording revocation. Neither method accepts or persists a raw token.

- [ ] **Step 6: Staticaly inspect invariants.**

Check all state-changing members have private setters and that no Domain file uses EF Core, ASP.NET Core, `DateTime.Now`, or raw HTML concepts.

## Task 3: Persistence model and tenant-filtered repositories

**Files:**
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/AppointmentRequestConfiguration.cs`
- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/IAppointmentRequestRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/AppointmentRequestRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/AppointmentConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/MaintenanceOpportunityConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260825180000_AddPublicBooking.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260825180000_AddPublicBooking.Designer.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**Interfaces:**
- Produces request lookup methods filtered by `(requestId, companyId)` and public response-token hash.
- Produces `GetCompanyBySlugAsync`, active public services, public qualified professionals, and document-matching operations that stay inside the resolved company.

- [ ] **Step 1: Map fields and relationships with Fluent API.**

Map `AppointmentRequests` with required company/customer/service/professional FKs and restrictive deletes. Use an alternate key `(CompanyId, Id)`. Index `(CompanyId, Status, RequestedStartAtUtc)` for agenda queues and `(CompanyId, CustomerId)` for customer context. Map nullable proposal fields, reason, and optional equipment/opportunity/appointment FKs with max lengths that match domain validation.

- [ ] **Step 2: Extend existing mappings.**

Add public-profile columns to `Companies`, required `Origin` to `Appointments`, and nullable token-hash/expiry/revocation columns plus a token-hash index to `MaintenanceOpportunities`. Configure all existing rows to migrate to `AppointmentOrigin.Internal`.

- [ ] **Step 3: Add repository contracts and implementations.**

Use focused methods such as:

```csharp
Task<AppointmentRequest?> GetForCompanyAsync(Guid requestId, Guid companyId, CancellationToken cancellationToken);
Task<AppointmentRequest?> GetForPublicResponseAsync(string responseTokenHash, CancellationToken cancellationToken);
Task<IReadOnlyList<AppointmentRequestListItem>> GetPendingForCompanyAsync(Guid companyId, CancellationToken cancellationToken);
Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
```

Define `AppointmentRequestListItem` in the same abstraction file with the request entity plus preloaded customer, service, professional, and equipment display values needed by the agenda. Keep it persistence-facing; Task 7 maps it to an Application DTO.

Every authenticated request query includes `CompanyId` in SQL. The document match must use company plus normalized document and must not return customer data to a public DTO.

- [ ] **Step 4: Extend the existing appointment scheduling transaction.**

Ensure acceptance can add an `Appointment`, change an `AppointmentRequest`, apply an authorized customer update, and mark a maintenance opportunity booked using the same `AppDbContext` transaction and existing overlap-conflict handling.

- [ ] **Step 5: Write the descriptive migration and synchronize the model snapshot.**

Use the English migration name `AddPublicBooking`; include only Phase 7 tables, columns, indexes, foreign keys, and the backfill/default for appointment origin.

- [ ] **Step 6: Staticaly compare migration against configuration.**

Verify every persisted property has a column, every query index is intentional, FKs preserve tenant boundaries, and the snapshot reflects the configuration without running EF commands.

## Task 4: Owner company settings use cases and endpoints

**Files:**
- Create: `src/VoltaMarcada.Application/CompanySettings/CompanySettingsDtos.cs`
- Create: `src/VoltaMarcada.Application/CompanySettings/UpdateCompanyPublicProfileCommand.cs`
- Create: `src/VoltaMarcada.Application/CompanySettings/UpdateCompanyPublicProfileUseCase.cs`
- Create: `src/VoltaMarcada.Application/CompanySettings/GetCompanyPublicSettingsUseCase.cs`
- Create: `src/VoltaMarcada.Application/CompanySettings/CompanySettingsErrors.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Create: `src/VoltaMarcada.Web/Endpoints/CompanySettingsEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs`

**Interfaces:**
- Produces owner-scoped `CompanyPublicSettingsDto` and `UpdateCompanyPublicProfileUseCase.ExecuteAsync`.

- [ ] **Step 1: Define DTOs and validation.**

Use a request contract with `PublicDescription`, `PublicPhone`, and `NotificationEmail`; return `Slug`, names, current public fields, and the owner-email fallback. Enforce the same length and syntax rules as the Domain entity before mutating state.

- [ ] **Step 2: Implement owner-scoped settings use cases.**

Resolve `ICurrentCompany`, verify owner authorization using the existing company-owner approach, load by company ID, call `UpdatePublicProfile`, save changes, and return explicit `OperationResult` failures for missing/invalid data.

- [ ] **Step 3: Add authenticated HTTP endpoint and error mappings.**

Accept dedicated request DTOs rather than entities. Never accept `CompanyId`; require `CompanyOwner` authorization and map error codes to safe Portuguese messages.

- [ ] **Step 4: Staticaly inspect authorization and fallback behavior.**

Confirm no employee can update settings, notification email falls back only for display, and no endpoint exposes unrelated membership data.

## Task 5: Public booking queries and request submission use cases

**Files:**
- Create: `src/VoltaMarcada.Application/PublicBooking/PublicBookingDtos.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/PublicBookingErrors.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/GetPublicCompanyPageUseCase.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/GetPublicAvailableSlotsUseCase.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestUseCase.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/PublicBookingCommandValidator.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/ICompanyRepository.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/ICustomerRepository.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IServiceRepository.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IProfessionalRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CompanyRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CustomerRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ServiceRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/ProfessionalRepository.cs`

**Interfaces:**
- Produces DTOs with only public company/service/professional/slot fields.
- Produces `SubmitPublicAppointmentRequestCommand` without `CompanyId`, customer IDs, equipment IDs, opportunity IDs, or raw internal IDs supplied by the browser beyond selected public option IDs validated server-side.

- [ ] **Step 1: Build public-page and option queries.**

Resolve the company by normalized slug; return only active services with `AllowOnlineBooking == true` and active qualified professionals. Return a not-found result when the slug is invalid without revealing tenant information.

- [ ] **Step 2: Reuse availability for public compatible slots.**

For a selected public service and date, validate the service belongs to the resolved company and use the existing `AppointmentAvailability` for every eligible professional. A specific-professional selection filters to that professional; any-available merges slots while preserving professional ID and display name. Do not call the authenticated `GetAvailableAppointmentSlotsUseCase` because it requires user access scope.

- [ ] **Step 3: Submit customer and request atomically.**

Normalize CPF/CNPJ, verify selected service/professional eligibility, validate that the selected interval is still structurally valid, then locate the customer by `(companyId, document)`. For a new document, create the customer with a required primary address. For an existing document, link it and create an immutable proposal from submitted values. Persist the request without creating or reserving an appointment.

- [ ] **Step 4: Prevent document enumeration.**

Do not create a public "customer found" endpoint or return customer information from submit responses. Use one generic successful response for new and existing documents.

- [ ] **Step 5: Staticaly inspect public DTOs and error branches.**

Confirm public responses contain no company/customer/opportunity tokens, raw stack traces, or existing customer profile values.

## Task 6: Contextual maintenance links and public response tokens

**Files:**
- Create: `src/VoltaMarcada.Application/PublicBooking/PublicTokenGenerator.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/GenerateMaintenanceBookingLinkUseCase.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/RevokeMaintenanceBookingLinkUseCase.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/GetContextualMaintenanceBookingUseCase.cs`
- Create: `src/VoltaMarcada.Application/PublicBooking/RespondToAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/MaintenanceOpportunities/*`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Modify: persistence contracts/repositories from Task 3

**Interfaces:**
- Produces `GenerateMaintenanceBookingLinkUseCase.ExecuteAsync(Guid opportunityId, CancellationToken)` returning raw token once.
- Produces `PublicTokenGenerator.Generate()` and `PublicTokenGenerator.Hash(string rawToken)`.

- [ ] **Step 1: Implement token generation and hashing.**

Use `RandomNumberGenerator.GetBytes(32)` and base64url encoding for the raw token. Hash UTF-8 token text with SHA-256 for lookup; accept tokens only after constant-time hash comparison or an indexed hash lookup. Do not log raw values.

- [ ] **Step 2: Generate/revoke contextual links for company owners.**

Resolve current company and owner scope, load opportunity filtered by company, calculate expiry from `IPublicBookingConfiguration`, persist the hash only, and return the raw token only in the result used to construct the copyable URL. Revoke through the aggregate and return a safe explicit result.

- [ ] **Step 3: Build contextual public query and submission path.**

Validate token state and expiry before loading company, existing customer, equipment, and recommended service. Return only display context required for booking. Reuse public slot selection but do not accept IDs that could change the contextual customer/equipment/service/opportunity.

- [ ] **Step 4: Add secure request-response tokens.**

When the company suggests another compatible time, generate a separate random response token hash for the request. A customer response validates this token, may accept the proposed time or choose another compatible slot, then returns the request to `Pending`; it never creates an appointment.

- [ ] **Step 5: Staticaly inspect token lifetime and transitions.**

Confirm renewing invalidates prior maintenance tokens, revocation and expiry reject the public page, and any contextual confirmed request moves the opportunity to `Booked` only during Task 7 acceptance.

## Task 7: Internal request handling in the existing scheduling module

**Files:**
- Create: `src/VoltaMarcada.Application/Scheduling/AppointmentRequestDtos.cs`
- Create: `src/VoltaMarcada.Application/Scheduling/GetAppointmentRequestsUseCase.cs`
- Create: `src/VoltaMarcada.Application/Scheduling/AcceptAppointmentRequestUseCase.cs`
- Create: `src/VoltaMarcada.Application/Scheduling/SuggestAppointmentRequestTimeUseCase.cs`
- Create: `src/VoltaMarcada.Application/Scheduling/RejectAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/SchedulingErrors.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Web/Endpoints/AppointmentEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs`

**Interfaces:**
- Produces owner/company-member safe request queue DTOs.
- Produces acceptance endpoint that returns created `Appointment` ID or an explicit conflict result.

- [ ] **Step 1: Query the company request queue through current access scope.**

Use `WorkOrderAuthorization.GetAccessScopeAsync`; owner sees company requests, employee visibility follows existing professional restriction. Include only request/customer/service/professional fields required by `/agenda`, the profile-update proposal, and current/proposed times.

- [ ] **Step 2: Accept within the scheduling transaction.**

Load request by company and access scope, require `Pending`, validate the professional is active and qualified, reuse `AppointmentAvailability`, convert to UTC with `TryGetUtcInterval`, and start the existing scheduling transaction. Create `Appointment` with `AppointmentOrigin.External`, transition request to accepted, optionally apply the customer proposal after explicit owner choice, mark contextual opportunity booked, and save using `TrySaveAppointmentChangesAsync`. Map a database overlap conflict to `SchedulingErrors.AppointmentConflict` without changing the request.

- [ ] **Step 3: Suggest a compatible replacement time.**

Require a pending request, validate selected professional/service and availability using the same rules, generate the response token from Task 6, call `SuggestNewTime`, and persist. Return the raw response token once so the Web layer can compose a copyable link.

- [ ] **Step 4: Reject safely.**

Require a pending or reschedule-requested item, bound the optional reason, transition to rejected, persist, and return an explicit success result. Do not delete requests or customer context.

- [ ] **Step 5: Staticaly inspect atomicity.**

Trace each acceptance branch to confirm all mutations occur after final availability validation and inside one transaction, while suggestion/rejection never create appointments.

## Task 8: Public routing and rate limiting

**Files:**
- Modify: `src/VoltaMarcada.Web/Program.cs`

**Interfaces:**
- Produces the named `public-booking` rate-limit policy applied to interactive public components.

- [ ] **Step 1: Keep public contracts in Application and pages separate from persistence entities.**

The public Razor components call public Application use cases directly, matching the existing interactive-server component pattern. Bind page form models to dedicated Application commands and DTOs; never bind Domain entities. Do not include `CompanyId`, arbitrary customer IDs, equipment IDs, maintenance opportunity IDs, or raw persisted token hash fields.

- [ ] **Step 2: Register and apply the public-booking rate-limit policy.**

Use ASP.NET Core's built-in fixed-window limiter under a named `public-booking` policy keyed by remote IP. Register `AddRateLimiter`, call `UseRateLimiter`, and apply the policy to the Razor Components endpoint so initial public-page and circuit-connection requests are throttled without adding a second public API surface.

- [ ] **Step 3: Preserve antiforgery and safe failures.**

Keep `app.UseAntiforgery()` before component execution. Map expected operation errors in components to Portuguese feedback; never expose document match status or raw token values. Ensure sensitive route parameters are not written into structured logs.

- [ ] **Step 4: Staticaly inspect route precedence.**

Ensure specific authenticated routes are mapped before the one-segment public-company page and that known internal routes cannot be treated as arbitrary company slugs.

## Task 9: Razor user experiences

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/PublicMaintenanceBooking.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/PublicAppointmentRequestResponse.razor`
- Create: `src/VoltaMarcada.Web/Components/Shared/PublicBookingFlow.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor.css`
- Create: `src/VoltaMarcada.Web/Components/Pages/PublicMaintenanceBooking.razor.css`
- Create: `src/VoltaMarcada.Web/Components/Pages/PublicAppointmentRequestResponse.razor.css`
- Create: `src/VoltaMarcada.Web/Components/Shared/PublicBookingFlow.razor.css`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Company.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Schedule.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/AppointmentCard.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/AppointmentDetailsDialog.razor`

**Interfaces:**
- Consumes public DTOs/use cases and settings/request action DTOs from Tasks 4–8.
- Produces a company owner settings tab, public booking submission, request queue actions, and visual internal/external origin labels.

- [ ] **Step 1: Add the Company Settings tab.**

Keep current company/team sections in Overview. Add Settings with fields for public description, public phone, and notification email; show the computed `NavigationManager.BaseUri + slug` in a read-only field with the existing clipboard helper. Respect owner authorization, loading/error/saving states, and Portuguese feedback.

- [ ] **Step 2: Implement a reusable public booking flow.**

Use a sequential MudBlazor form: service, professional/any available, date, compatible slot, document/details/address, submit. Fetch only public DTOs, disable the submit button during requests, show field-level Portuguese validation, and render all names/descriptions using normal encoded component parameters.

- [ ] **Step 3: Build public company and contextual pages.**

`/{companySlug}` uses the public company query and includes no authenticated layout/navigation. `/agendar/manutencao/{token}` shows only safe maintenance context and reuses the booking flow with locked customer/equipment/service context. Expired, invalid, or revoked tokens use the existing safe not-found/error presentation.

- [ ] **Step 4: Add the schedule request queue.**

Keep the calendar as the primary schedule view. Add an adjacent/responsive section for external requests, showing status, customer, service, professional, requested time, optional proposed profile update, and confirm/suggest/reject actions. Suggestion opens a validated slot selector and exposes the generated response link once for copying. Confirmation offers the owner an explicit checkbox/button to apply the profile proposal.

- [ ] **Step 5: Make appointment origin visible.**

Show a compact `Interno` or `Externo` chip on confirmed appointment cards/details. Preserve current appointment rendering and accessibility labels.

- [ ] **Step 6: Staticaly inspect public versus internal presentation.**

Confirm public pages do not use `AuthorizeRouteView` or expose application navigation, internal agenda remains authorized, text fields never use markup rendering, and loading/empty/error states exist for all async operations.

## Task 10: Maintenance-center links, migration integration, and final static review

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/MaintenanceOpportunities.razor`
- Modify: `src/VoltaMarcada.Web/Endpoints/MaintenanceOpportunityEndpoints.cs`
- Modify: `src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityDtos.cs`
- Modify: `src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityActionUseCases.cs`

**Interfaces:**
- Consumes the contextual-link use cases from Task 6.
- Produces owner-visible generate/copy/renew/revoke maintenance-link actions.

- [ ] **Step 1: Add contextual-link actions to maintenance opportunities.**

For owner-authorized applicable opportunities, expose generate/copy, renew, and revoke controls. Use the returned raw token only to build the link immediately; clear it from component state when the data refreshes or another action starts. Keep the existing internal schedule handoff for customers who arrange a time directly.

- [ ] **Step 2: Reconcile action state rules.**

Ensure generating a link sets `BookingRequested`; direct internal scheduling remains able to mark the opportunity booked through existing appointment creation; completed/dismissed opportunities cannot generate links; and the action UI offers only valid transitions.

- [ ] **Step 3: Perform full static verification.**

Review Domain/Application/Infrastructure/Web dependency directions; every tenant query; every public-token path; endpoint authorization and rate-limit registration; `PublicBooking` option validation; Fluent mappings versus `AddPublicBooking` migration/snapshot; DI registrations; Razor routes and action wiring; Portuguese UI feedback; and source-control changes limited to Phase 7. Do not run build, tests, application, or EF tools.

- [ ] **Step 4: Prepare handoff summary.**

Record implemented routes, the setting key `PublicBooking:MaintenanceLinkLifetimeDays`, intentionally absent SMTP implementation, static checks completed, and verification commands deliberately not run due repository policy.
