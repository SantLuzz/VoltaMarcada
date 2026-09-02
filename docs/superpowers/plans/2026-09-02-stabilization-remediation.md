# Stabilization and Remediation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stabilize authentication, presentation, booking, scheduling, Work Order, and Maintenance Opportunity flows without changing their established business lifecycle.

**Architecture:** Keep domain and application rules in their current layers. Add narrow infrastructure and Web presentation abstractions, then refactor UI consumers to use them; availability remains centralized in `AppointmentAvailability` and the existing scheduling transaction.

**Tech Stack:** .NET 10, C#, ASP.NET Core Identity, Blazor Web App, MudBlazor, EF Core, PostgreSQL, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-02-stabilization-remediation-design.md`

## Global Constraints

- Source code, migrations, configuration names, logs, and technical documents remain in English; user-facing copy remains Brazilian Portuguese.
- MudBlazor is mandatory for UI controls.
- Authenticated tenant data is resolved from the current company, not client-posted IDs.
- UTC persistence and the existing availability engine remain unchanged.
- SMTP secrets are supplied only through environment configuration or development secrets.
- Do not alter the completed Maintenance Opportunity lifecycle or the established menu ordering.

---

### Task 1: Establish an authenticated SMTP password-management flow

**Files:**
- Create: `src/VoltaMarcada.Application/Abstractions/Communication/IAccountEmailSender.cs`
- Create: `src/VoltaMarcada.Infrastructure/Communication/SmtpAccountEmailSender.cs`
- Create: `src/VoltaMarcada.Infrastructure/Communication/SmtpOptions.cs`
- Create: `src/VoltaMarcada.Web/Configuration/LoginRateLimitOptions.cs`
- Create: `src/VoltaMarcada.Web/Components/Pages/Account/ForgotPassword.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/Account/ResetPassword.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/Account/ChangePassword.razor`
- Modify: `src/VoltaMarcada.Web/Endpoints/AccountEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Modify: `src/VoltaMarcada.Web/appsettings.json`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Account/Login.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/Identity/PasswordManagementTests.cs`

**Interfaces:**
- Produces `IAccountEmailSender.SendPasswordResetAsync(string recipientEmail, string recipientName, Uri resetUrl, CancellationToken cancellationToken)`.
- Produces POST endpoints `/account/forgot-password`, `/account/reset-password`, and `/account/change-password`.
- Produces `LoginRateLimitOptions` bound from `LoginRateLimit` in `appsettings.json`.

- [ ] **Step 1: Write failing password-management integration tests.**

```csharp
[Fact]
public async Task ForgotPassword_returns_the_same_confirmation_for_an_unknown_email()
{
    var response = await Client.PostAsync("/account/forgot-password", Form("unknown@example.com"));
    response.EnsureSuccessStatusCode();
    Assert.Contains("Se o e-mail estiver cadastrado", await response.Content.ReadAsStringAsync());
}

[Fact]
public async Task ChangePassword_uses_the_authenticated_user_and_rejects_an_invalid_current_password()
{
    await AuthenticateAsync();
    var response = await Client.PostAsync("/account/change-password", ChangePasswordForm("invalid", "NewPassword1!"));
    Assert.Contains("senha atual", await response.Content.ReadAsStringAsync());
}
```

- [ ] **Step 2: Run `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~PasswordManagementTests` and confirm the tests fail because endpoints are absent.**
- [ ] **Step 3: Add the account-email abstraction and SMTP implementation.** Bind `SmtpOptions`, validate host/port/sender when enabled, register it in Infrastructure, and keep credentials out of `appsettings.json`.
- [ ] **Step 4: Implement token-generating forgot/reset endpoints and MudBlazor pages.** Generate Identity reset tokens only for existing users, URL-encode the token, send the configured application URL, return the same acknowledgement for every email, and call `ResetPasswordAsync` from the reset endpoint.
- [ ] **Step 5: Implement the authenticated change-password endpoint and page.** Resolve the principal from `HttpContext.User`, call `ChangePasswordAsync`, map expected Identity failures, and never bind a user ID.
- [ ] **Step 6: Configure named login rate limiting and temporary Identity lockout.** Bind fixed-window permit/queue settings from `LoginRateLimit`, apply the named limiter to the login endpoint, set `lockoutOnFailure: true`, and configure lockout duration/max failures from settings.
- [ ] **Step 7: Run the focused tests and then `dotnet test tests/VoltaMarcada.IntegrationTests`.**
- [ ] **Step 8: Commit with `git commit -m "feat: add secure password management"`.**

### Task 2: Centralize display time zones and Brazilian currency formatting

**Files:**
- Create: `src/VoltaMarcada.Web/Formatting/ICurrentDisplayTimeZone.cs`
- Create: `src/VoltaMarcada.Web/Formatting/CurrentDisplayTimeZone.cs`
- Create: `src/VoltaMarcada.Web/Formatting/IBrazilianDisplayFormatter.cs`
- Create: `src/VoltaMarcada.Web/Formatting/BrazilianDisplayFormatter.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Schedule.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/MaintenanceOpportunities.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/DisplayFormattingTests.cs`

**Interfaces:**
- Produces `ICurrentDisplayTimeZone.GetTimeZoneAsync(CancellationToken)`.
- Produces `IBrazilianDisplayFormatter.FormatDateTime(DateTimeOffset utcValue, TimeZoneInfo timeZone)` and `FormatCurrency(decimal value)`.

- [ ] **Step 1: Write failing formatter tests for a linked professional time zone, default fallback, and `R$ 150,00`.**
- [ ] **Step 2: Run `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~DisplayFormattingTests` and confirm the shared services do not exist.**
- [ ] **Step 3: Implement the scoped time-zone resolver.** Query the current company member's linked professional and return its configured time zone; otherwise resolve `Scheduling:DefaultTimeZoneId`.
- [ ] **Step 4: Implement the formatter with `CultureInfo.GetCultureInfo("pt-BR")` and explicit UTC conversion.**
- [ ] **Step 5: Replace local display conversions in Agenda, Work Order, and Maintenance Opportunity pages with the two services.** Do not change availability calculation calls.
- [ ] **Step 6: Run the focused tests and full integration suite.**
- [ ] **Step 7: Commit with `git commit -m "feat: centralize Brazilian display formatting"`.**

### Task 3: Persist public-request equipment safely

**Files:**
- Modify: `src/VoltaMarcada.Application/PublicBooking/PublicCompanyDtos.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/PublicBooking/SubmitContextualAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/IEquipmentRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/EquipmentRepository.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicMaintenanceBooking.razor`
- Modify: `src/VoltaMarcada.Application/Scheduling/GetAppointmentRequestsUseCase.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/PublicBookingEquipmentTests.cs`

**Interfaces:**
- Extends the public booking command with required equipment type/name and optional manufacturer.
- Produces repository lookup `GetActiveForCustomerByTypeAndManufacturerAsync(Guid companyId, Guid customerId, string type, string? manufacturer, CancellationToken cancellationToken)`.
- Extends `AppointmentRequestDto` with a friendly equipment description.

- [ ] **Step 1: Write failing tests that a public request creates missing equipment, reuses matching equipment, and preserves contextual equipment.**
- [ ] **Step 2: Run `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~PublicBookingEquipmentTests` and confirm the new command fields/lookup are missing.**
- [ ] **Step 3: Add normalized equipment lookup and server-side create-or-reuse logic.** Scope every lookup by company and customer; create an active Equipment only in a standard public request with no match.
- [ ] **Step 4: Bind MudBlazor type/name and manufacturer inputs on the public company page.** Contextual maintenance booking renders the known equipment and does not expose replacement inputs.
- [ ] **Step 5: Include the resolved equipment description in external request projections and Agenda detail cards.**
- [ ] **Step 6: Run focused and full integration tests.**
- [ ] **Step 7: Commit with `git commit -m "feat: persist public booking equipment"`.**

### Task 4: Correct global layouts and post-success form state

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor.css`
- Modify: `src/VoltaMarcada.Web/Components/Layout/PublicLayout.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Company.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/CustomerRegistration.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Professionals.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/MainLayoutTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/FormResetTests.cs`

**Interfaces:**
- Maintains existing navigation links and their order.
- Uses `MudForm.ResetValidation()` only after a successful mutation.

- [ ] **Step 1: Write failing component tests for hidden account email, visible mobile drawer close action, and clean post-success validation state.**
- [ ] **Step 2: Run the component tests and confirm the current markup fails the expectations.**
- [ ] **Step 3: Update MainLayout to show only the user name and add the drawer close icon.** Keep Dashboard first and every existing link order unchanged.
- [ ] **Step 4: Use responsive MudGrid/MudStack and CSS wrapping in public and authenticated headers.** Remove no overflow globally; fix the individual layout constraints instead.
- [ ] **Step 5: Replace employee-invitation and customer-address request models after success, then reset their MudForm validation.** Place the professional schedule-block action on a separate row.
- [ ] **Step 6: Run focused component tests and the full suite.**
- [ ] **Step 7: Commit with `git commit -m "fix: stabilize responsive forms and navigation"`.**

### Task 5: Complete Agenda types, direct external acceptance, and request permissions

**Files:**
- Modify: `src/VoltaMarcada.Application/Scheduling/AcceptAppointmentRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/GetAppointmentRequestsUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Scheduling/SchedulingContracts.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Schedule.razor`
- Modify: `src/VoltaMarcada.Web/Components/Shared/AppointmentEditor.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/AppointmentRequestAcceptanceTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/AppointmentEditorTests.cs`

**Interfaces:**
- Changes acceptance to `ExecuteAsync(Guid requestId, Guid selectedProfessionalId, bool applyCustomerProfileProposal, CancellationToken cancellationToken = default)`.
- Produces `AppointmentRequestDto.EquipmentDescription` and no open request data for non-owners.

- [ ] **Step 1: Write failing tests for owner direct acceptance with an eligible selected professional and employee request invisibility.**
- [ ] **Step 2: Run the tests and confirm the current acceptance requires an already assigned professional.**
- [ ] **Step 3: Change acceptance to load the selected professional tenant-scoped, validate service qualification and availability in the existing transaction, assign the request, and create/accept atomically.**
- [ ] **Step 4: Return an empty request list for non-owners and wrap the request panel in owner-only UI.** Render a MudSelect of exact eligible professionals and send the selected ID to acceptance.
- [ ] **Step 5: Write failing editor tests for Visit/Other manual duration and Service Work Order-only context.**
- [ ] **Step 6: Implement type-driven editor fields.** Hide Service for Visit/Other, require manual duration, require Work Order for Service, and resolve all Service context on the server; close the dialog only after a success result.
- [ ] **Step 7: Remove the administrative card and make row actions wrap on small viewports.**
- [ ] **Step 8: Run focused scheduling tests and the full suite.**
- [ ] **Step 9: Commit with `git commit -m "fix: complete agenda request assignment"`.**

### Task 6: Stabilize Work Order lists, details, and photo recovery

**Files:**
- Modify: `src/VoltaMarcada.Application/WorkOrders/GetWorkOrdersUseCase.cs`
- Modify: `src/VoltaMarcada.Application/WorkOrders/WorkOrderDtos.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/WorkOrderRepository.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrders.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor`
- Create: `src/VoltaMarcada.Web/wwwroot/js/work-order-photo-staging.js`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`
- Test: `tests/VoltaMarcada.IntegrationTests/WorkOrderOrderingTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/WorkOrderPresentationTests.cs`

**Interfaces:**
- The Work Order projection exposes scheduled sort timestamp and status/priority display data.
- Browser module exports `stagePhoto`, `restoreStagedPhoto`, and `clearStagedPhoto` keyed by Work Order and photo type.

- [ ] **Step 1: Write failing ordering tests with active and completed Work Orders at interleaved dates.**
- [ ] **Step 2: Run the ordering tests and confirm completed orders are not guaranteed last.**
- [ ] **Step 3: Update the tenant-scoped list query/projection so active records sort by appointment date/time with deterministic fallback, and completed records sort last.**
- [ ] **Step 4: Write failing component tests for an Urgent danger badge, visible detail status, and Brazilian money output.**
- [ ] **Step 5: Implement the MudChip priority/status presentation, shared money formatter calls, responsive Status/action layout, and appointment-tab spacing.**
- [ ] **Step 6: Write a browser-boundary test/specification for staged photo restoration after reload.**
- [ ] **Step 7: Stage captured file bytes and metadata in IndexedDB before upload; restore and upload after reconnect; clear only after the existing authenticated upload succeeds.**
- [ ] **Step 8: Run focused Work Order tests, then the full suite.**
- [ ] **Step 9: Commit with `git commit -m "fix: stabilize work order presentation and photos"`.**

### Task 7: Apply remaining Maintenance Opportunity presentation fixes

**Files:**
- Modify: `src/VoltaMarcada.Web/Components/Pages/MaintenanceOpportunities.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/MaintenanceOpportunitiesPresentationTests.cs`

**Interfaces:**
- Consumes `ICurrentDisplayTimeZone` and `IBrazilianDisplayFormatter` from Task 2.

- [ ] **Step 1: Write failing component tests for separated filters, vertical mobile actions, and time-zone-formatted contact timestamps.**
- [ ] **Step 2: Run the tests and confirm the page still uses the old layout/formatting.**
- [ ] **Step 3: Refactor only the MudBlazor layout and timestamp rendering.** Keep lifecycle actions, conversion behavior, and owner authorization unchanged.
- [ ] **Step 4: Run focused component tests and the full suite.**
- [ ] **Step 5: Commit with `git commit -m "fix: improve maintenance opportunity responsiveness"`.**

### Task 8: Perform cross-feature verification

**Files:**
- Modify: all files above only when a verification failure identifies a root cause.
- Test: `tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj`

- [ ] **Step 1: Run `dotnet build VoltaMarcada.sln --no-restore`.** Resolve compiler errors one at a time with a reproducing focused test where possible.
- [ ] **Step 2: Run `dotnet test VoltaMarcada.sln --no-build`.** Record every failure and use systematic root-cause investigation before changing implementation.
- [ ] **Step 3: Start the Web application with development settings and manually verify desktop and narrow mobile layouts for Login, public booking, Agenda, Work Orders, and Maintenance Opportunities.**
- [ ] **Step 4: Confirm account reset delivery with an SMTP development configuration stored in user secrets, not repository settings.**
- [ ] **Step 5: Confirm browser camera capture, forced reconnect/reload, restored pending photo, and single persisted Work Order photo.**
- [ ] **Step 6: Review `git diff --check`, `git status --short`, and test/build output before the final commit.**
- [ ] **Step 7: Commit with `git commit -m "test: verify stabilization remediation"`.**
