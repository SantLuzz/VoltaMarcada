# Owner Company Area and Employee Invitations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let company owners view their company, pre-register employees, and share secure invitation links that create Employee accounts.

**Architecture:** `EmployeeInvitation` is the durable pre-registration record and stores only a hash of a random invitation token. Explicit Application use cases coordinate tenant-scoped invitation work through small persistence and identity abstractions. Infrastructure owns EF Core, PostgreSQL transaction handling, token generation, and Identity account creation; Web supplies owner-only Razor UI, an owner authorization policy, and the anonymous public invitation-completion form.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App with MudBlazor, EF Core, PostgreSQL, ASP.NET Core Identity, FluentValidation.

**Spec:** `docs/superpowers/specs/2026-08-20-owner-company-and-employee-invitations-design.md`

## Global Constraints

- All source code, migrations, logs, identifiers, comments, and technical documentation are written in English; user-facing UI text is Brazilian Portuguese.
- Use the existing Domain, Application, Infrastructure, and Web project boundaries; Application must not depend on Infrastructure or Web.
- Resolve tenant identity from `ICurrentCompany`; never accept `CompanyId` from the browser for authenticated operations.
- Use `TimeProvider`; do not call `DateTime.Now` or `DateTime.UtcNow` in Domain or Application code.
- Persist a cryptographically random invitation token only as a hash; do not log it or put it in a redirect URL.
- Owner-only UI visibility is supplementary; owner policy must protect routes and every internal mutation.
- Do not add automated tests or run `dotnet build`, `dotnet test`, `dotnet run`, or application hosts unless the user explicitly requests them, per `AGENTS.md`.
- Use `apply_patch` for source and documentation edits. Generate and inspect the EF migration without applying it to a database.

---

### Task 1: Add the employee invitation domain model and EF Core schema

**Files:**
- Create: `src/ServiceSync.Domain/Entities/EmployeeInvitation.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Configurations/EmployeeInvitationConfiguration.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/<timestamp>_AddEmployeeInvitations.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/<timestamp>_AddEmployeeInvitations.Designer.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**Interfaces:**
- Consumes: `BrazilianDocument.TryNormalize`, `DomainException`, and `ICompanyScopedEntity` from the Domain project.
- Produces: `EmployeeInvitation.Create`, `EmployeeInvitation.Revoke`, `EmployeeInvitation.Accept`, `EmployeeInvitation.IsPending`, and `AppDbContext.EmployeeInvitations` for later Application and Infrastructure tasks.

- [ ] **Step 1: Create the protected domain entity**

  Add `EmployeeInvitation` as an `ICompanyScopedEntity`, with private setters and a private parameterless constructor for EF Core. Its `Create` factory accepts company ID, full name, email, normalized document, phone, address, token hash, creator user ID, expiration, and current time. Require non-empty GUIDs; trim required text; normalize the e-mail to lower invariant; require a valid CPF using `BrazilianDocument`; and reject an expiration not later than creation.

  Implement the behavior surface below; it must not expose the raw token:

  ```csharp
  public static EmployeeInvitation Create(
      Guid companyId,
      string fullName,
      string email,
      string document,
      string phone,
      string address,
      string tokenHash,
      Guid createdByUserId,
      DateTimeOffset expiresAtUtc,
      DateTimeOffset now);

  public bool IsPending(DateTimeOffset now);
  public void Revoke(DateTimeOffset now);
  public void Accept(DateTimeOffset now);
  ```

- [ ] **Step 2: Configure persistence and DbContext access**

  Add `DbSet<EmployeeInvitation> EmployeeInvitations` to `AppDbContext`. Create one Fluent API configuration that maps the entity to the solution's existing table-naming convention and configures required, length-bounded fields: `FullName` 200, `Email` 256, `Document` 11, `Phone` 20, `Address` 300, and `TokenHash` 128. Configure foreign keys to `Company`, the Identity `ApplicationUser` creator, and a required index for each foreign key.

  Add the following named indexes:

  ```csharp
  builder.HasIndex(invitation => invitation.TokenHash)
      .HasDatabaseName("UX_EmployeeInvitations_TokenHash")
      .IsUnique();

  builder.HasIndex(invitation => new { invitation.CompanyId, invitation.CreatedAtUtc })
      .HasDatabaseName("IX_EmployeeInvitations_CompanyId_CreatedAtUtc");

  builder.HasIndex(invitation => new { invitation.CompanyId, invitation.Email, invitation.AcceptedAtUtc, invitation.RevokedAtUtc })
      .HasDatabaseName("IX_EmployeeInvitations_CompanyId_Email_State");

  builder.HasIndex(invitation => new { invitation.CompanyId, invitation.Document, invitation.AcceptedAtUtc, invitation.RevokedAtUtc })
      .HasDatabaseName("IX_EmployeeInvitations_CompanyId_Document_State");
  ```

- [ ] **Step 3: Generate the migration and review it statically**

  Generate a descriptive `AddEmployeeInvitations` migration using the solution's installed EF Core tooling. Confirm the generated migration creates the table, all required columns, foreign keys with restrictive deletion, and the four indexes above. Inspect both migration directions and the model snapshot; do not apply the migration to a database or start the application.

- [ ] **Step 4: Review the task boundary**

  Verify that the Domain project has no EF Core or Identity dependency and that the raw token is absent from the entity and migration. Record no test command because the repository explicitly defers automated tests until requested.

### Task 2: Define application contracts, token handling, validation, and invitation use cases

**Files:**
- Create: `src/ServiceSync.Application/Abstractions/Identity/IEmployeeInvitationAccountService.cs`
- Create: `src/ServiceSync.Application/Abstractions/Persistence/IEmployeeInvitationRepository.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/EmployeeInvitationToken.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/CreateEmployeeInvitationCommand.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/CreateEmployeeInvitationCommandValidator.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/CreateEmployeeInvitationResult.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/CreateEmployeeInvitationUseCase.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/GetOwnerCompanyOverviewUseCase.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/RenewEmployeeInvitationUseCase.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/RevokeEmployeeInvitationUseCase.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/GetEmployeeInvitationForAcceptanceUseCase.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/AcceptEmployeeInvitationUseCase.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/EmployeeInvitationDtos.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`

**Interfaces:**
- Consumes: `EmployeeInvitation`, `ICurrentCompany`, `ICurrentUser`, `TimeProvider`, and FluentValidation.
- Produces: explicit use cases for the owner page and the anonymous completion page; all later Infrastructure and Web code calls these types only.

- [ ] **Step 1: Define DTOs and result codes**

  Define immutable records for the owner page and public page:

  ```csharp
  public sealed record CompanyOverviewDto(
      string LegalName, string TradeName, string Document, string WhatsApp,
      string PostalCode, string Address, string StateName, string CityName,
      string? LogoKey, IReadOnlyList<EmployeeDto> Employees,
      IReadOnlyList<EmployeeInvitationDto> Invitations);

  public sealed record EmployeeDto(
      Guid InvitationId, string FullName, string Email, string Document,
      string Phone, string Address, DateTimeOffset AcceptedAtUtc);

  public sealed record EmployeeInvitationDto(
      Guid Id, string FullName, string Email, string Document, string Phone,
      string Address, DateTimeOffset ExpiresAtUtc, DateTimeOffset CreatedAtUtc,
      bool IsPending, bool IsRevoked, bool IsExpired);

  public sealed record PublicEmployeeInvitationDto(
      string FullName, string MaskedEmail);
  ```

  Use result code records for creation, renewal/revocation, and acceptance. Their codes must distinguish invalid input, unauthenticated context, owner-not-authorized, duplicate e-mail, duplicate document, invitation not found, invitation not available, and identity e-mail conflict.

- [ ] **Step 2: Define narrow Infrastructure abstractions**

  In `IEmployeeInvitationRepository`, expose tenant-filtered query and mutation methods, including `GetOwnerCompanyOverviewAsync`, `EmailExistsForPendingInvitationAsync`, `DocumentExistsForPendingOrAcceptedInvitationAsync`, `GetPendingByIdAsync`, `GetPublicInvitationByTokenHashAsync`, `AddAsync`, and `SaveChangesAsync`.

  In `IEmployeeInvitationAccountService`, keep the Identity/EF transaction behind one method:

  ```csharp
  Task<EmployeeInvitationAccountOutcome> CreateAccountAndAcceptAsync(
      string tokenHash,
      string password,
      DateTimeOffset now,
      CancellationToken cancellationToken = default);
  ```

  Its outcome distinguishes `Success`, `InvitationUnavailable`, and `EmailAlreadyRegistered`; it never returns a password, token, document, phone, or address.

- [ ] **Step 3: Implement deterministic token generation**

  Add a small internal Application helper that uses `RandomNumberGenerator.GetBytes(32)` and `SHA256.HashData` to produce URL-safe Base64 tokens and lowercase hexadecimal hashes. Return both only at creation/renewal:

  ```csharp
  internal sealed record EmployeeInvitationToken(string Value, string Hash);
  ```

  The token lifetime is calculated as `timeProvider.GetUtcNow().AddDays(7)`.

- [ ] **Step 4: Implement owner use cases with tenant checks**

  `CreateEmployeeInvitationUseCase` validates the five pre-registration fields, checks authentication, resolves `ICurrentCompany`, verifies the caller is the company owner via the repository, rejects a registered e-mail, checks duplicate pending e-mail and pending/accepted document, then stores a new invitation and returns its raw token once.

  `GetOwnerCompanyOverviewUseCase` requires the current authenticated owner and returns `null` for a missing company or non-owner. `RenewEmployeeInvitationUseCase` loads the invitation using both invitation ID and current `CompanyId`, revokes it when pending, then calls the same creation logic with the stored employee details. `RevokeEmployeeInvitationUseCase` only changes a pending invitation in the caller's company.

- [ ] **Step 5: Implement public-invitation use cases**

  `GetEmployeeInvitationForAcceptanceUseCase` hashes the route token and returns `PublicEmployeeInvitationDto` only when the invitation is pending. It masks the local part of the email, for example `jo***@empresa.com`.

  `AcceptEmployeeInvitationUseCase` validates non-empty, matching passwords and delegates the final revalidation and account creation to `IEmployeeInvitationAccountService`. It returns an explicit result for invalid input, unavailable invitation, e-mail conflict, and success.

- [ ] **Step 6: Register Application use cases and validators**

  Add all six use cases to `AddApplication` as scoped services. Rely on the existing `AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>()` registration for the new create-command validator. No Application use case may inject `AppDbContext`, `UserManager`, `HttpContext`, or `NavigationManager`.

### Task 3: Implement EF Core repository, transactional Identity acceptance, and dependency injection

**Files:**
- Create: `src/ServiceSync.Infrastructure/Persistence/Repositories/EmployeeInvitationRepository.cs`
- Create: `src/ServiceSync.Infrastructure/Identity/EmployeeInvitationAccountService.cs`
- Modify: `src/ServiceSync.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: the interfaces and DTOs from Task 2, `AppDbContext`, `ApplicationUser`, and `UserManager<ApplicationUser>`.
- Produces: production implementations registered for the owner and public invitation use cases.

- [ ] **Step 1: Implement tenant-filtered repository queries**

  Query company overview by company ID and project company data, accepted invitations as `EmployeeDto`, and all invitations as `EmployeeInvitationDto`. Each invitation mutation must filter both `Id` and `CompanyId` before returning an entity. Pending checks must include `AcceptedAtUtc == null`, `RevokedAtUtc == null`, and `ExpiresAtUtc > now`; document checks also include accepted records.

  `IsCurrentUserOwnerAsync` must be a direct query against `CompanyUsers` filtered by user ID, company ID, and `CompanyUserRole.Owner`.

- [ ] **Step 2: Implement the atomic acceptance service**

  In `CreateAccountAndAcceptAsync`, begin an EF Core transaction, load the invitation by its token hash, and call `invitation.IsPending(now)` immediately before user creation. Check `UserManager.FindByEmailAsync(invitation.Email)` before creating an `ApplicationUser` with `UserName` and `Email` set to the invitation email.

  If `UserManager.CreateAsync` succeeds, create `CompanyUser.Create(user.Id, invitation.CompanyId, CompanyUserRole.Employee, now)`, call `invitation.Accept(now)`, save the context, and commit. If Identity rejects a duplicate e-mail or the invitation is not pending, roll back and return the corresponding explicit outcome. Throw unexpected Identity/EF errors rather than concealing them.

- [ ] **Step 3: Register implementations**

  Register `IEmployeeInvitationRepository` and `IEmployeeInvitationAccountService` as scoped services in `AddInfrastructure`. Keep the existing Identity configuration intact. Confirm no concrete Infrastructure implementation is injected directly into Application.

- [ ] **Step 4: Inspect authorization and concurrency paths statically**

  Verify repository filters include `CompanyId`, acceptance checks invitation state inside the transaction, and no raw token enters a database field, log message, exception, or redirect URL.

### Task 4: Add an owner authorization policy and expose it in the application shell

**Files:**
- Create: `src/ServiceSync.Web/Authorization/CompanyOwnerAuthorizationHandler.cs`
- Modify: `src/ServiceSync.Web/Authorization/AuthorizationPolicies.cs`
- Modify: `src/ServiceSync.Web/Program.cs`
- Modify: `src/ServiceSync.Web/Components/Layout/MainLayout.razor`

**Interfaces:**
- Consumes: `CompanyUser`, `CompanyUserRole.Owner`, the authenticated name-identifier claim, and `AppDbContext`.
- Produces: `AuthorizationPolicies.CompanyOwner`, usable by the owner page and owner-only endpoints.

- [ ] **Step 1: Add a distinct owner requirement and handler**

  Create `CompanyOwnerRequirement : IAuthorizationRequirement` and an authorization handler modeled after `CompanyMemberAuthorizationHandler`. Parse the `ClaimTypes.NameIdentifier` GUID, then issue one EF Core query requiring a `CompanyUsers` record with that user ID and `CompanyUserRole.Owner`. Do not treat the sidebar state or an incoming company ID as authorization.

- [ ] **Step 2: Register the policy**

  Add `public const string CompanyOwner = "CompanyOwner";` to `AuthorizationPolicies`. In `Program.cs`, register the handler and configure `CompanyOwner` to require an authenticated user and the new requirement. Keep the existing `CompanyMember` policy unchanged.

- [ ] **Step 3: Render the sidebar item only for owners**

  Extend `MainLayout`'s existing initialization query to retrieve whether the current membership is `Owner` for the resolved company. Add a `MudNavLink` to `/empresa` labelled `Empresa`, using the business icon, only when `isOwner` is true. The link is a convenience only; it must not be the sole access control.

- [ ] **Step 4: Check non-owner behavior statically**

  Ensure an Employee does not receive the sidebar link and that the policy does not authorize Employees merely because they have a company membership.

### Task 5: Build the owner company and invitation management page

**Files:**
- Create: `src/ServiceSync.Web/Components/Pages/Company.razor`
- Modify: `src/ServiceSync.Web/wwwroot/app.css`

**Interfaces:**
- Consumes: `AuthorizationPolicies.CompanyOwner`, `GetOwnerCompanyOverviewUseCase`, `CreateEmployeeInvitationUseCase`, `RenewEmployeeInvitationUseCase`, and `RevokeEmployeeInvitationUseCase`.
- Produces: the protected `/empresa` owner screen and the only UI that reveals a freshly generated link.

- [ ] **Step 1: Create the protected page skeleton**

  Add `@page "/empresa"` and `[Authorize(Policy = AuthorizationPolicies.CompanyOwner)]`. In `OnInitializedAsync`, call `GetOwnerCompanyOverviewUseCase`; present an error alert if it returns no owner overview. Use `PageHeader` and simple outlined `MudPaper` sections matching `CustomerRegistration.razor`.

- [ ] **Step 2: Render read-only company information**

  Show legal name, trade name, CPF/CNPJ, WhatsApp, address, postal code, city, state, and logo when present. Do not add edit controls or a company update use case.

- [ ] **Step 3: Add the basic employee pre-registration form**

  Bind a local form model with required `FullName`, `Email`, `Document`, `Phone`, and `Address`. Reuse `BrazilianDocumentInput` and `BrazilianPhoneInput`; use a plain `MudTextField` for address. On save, invoke `CreateEmployeeInvitationUseCase` and map each explicit result code to a Portuguese alert. Disable the submit action while saving.

- [ ] **Step 4: Display a newly generated link without leaking it through navigation**

  Build `"/convites/funcionarios/" + rawToken` only in component memory after a successful create or renew. Display it in a read-only text field with a clipboard action implemented through the existing Blazor/JavaScript interop pattern or a minimal new non-sensitive helper. Clear that local value when the page is reloaded and never include it in query strings, logs, or snackbar messages.

- [ ] **Step 5: Show employees and invitations**

  Render an active-employees table with name, e-mail, CPF, phone, address, and accepted date. Render an invitations table with employee data, creation and expiry dates, and status chips for pending, expired, revoked, and accepted. Pending rows expose `Copiar novo link`/renew and `Revogar` actions. Confirm the revoke action with the existing MudBlazor dialog service before calling its use case. Reload the overview after every successful mutation.

- [ ] **Step 6: Add only focused styling**

  Add CSS scoped by company page class names for the section spacing, compact read-only details, link field, and responsive tables. Do not change the global navigation layout or unrelated page styling.

### Task 6: Add the anonymous public invitation completion page and endpoint

**Files:**
- Create: `src/ServiceSync.Web/Components/Pages/EmployeeInvitationAcceptance.razor`
- Create: `src/ServiceSync.Web/Endpoints/EmployeeInvitationEndpoints.cs`
- Modify: `src/ServiceSync.Web/Program.cs`

**Interfaces:**
- Consumes: `GetEmployeeInvitationForAcceptanceUseCase`, `AcceptEmployeeInvitationUseCase`, `PasswordInput`, `SignInManager<ApplicationUser>`, and `EmployeeInvitationAccountOutcome` result mapping.
- Produces: anonymous `GET /convites/funcionarios/{token}` rendering and a protected-by-token `POST /convites/funcionarios/{token}` account completion endpoint.

- [ ] **Step 1: Implement the public read-only invitation page**

  Create the route `@page "/convites/funcionarios/{Token}"`, mark it `[AllowAnonymous]`, and use `AuthLayout`. During initialization call `GetEmployeeInvitationForAcceptanceUseCase` with the route token. When unavailable, show a generic invalid/expired invitation message; when available, show the employee's full name and masked email. Never render CPF, phone, address, company identifier, invitation ID, or internal state.

- [ ] **Step 2: Add the password completion form**

  The form posts the route token, password, and confirmation only. Reuse `PasswordInput`; do not include hidden company, employee, email, or invitation ID fields. Preserve generic error feedback for unavailable links and identity email conflicts.

- [ ] **Step 3: Map the anonymous endpoint**

  In `EmployeeInvitationEndpoints`, map `POST /convites/funcionarios/{token}` without an authorization requirement. Bind a form request containing only password and confirmation, pass the route token to `AcceptEmployeeInvitationUseCase`, and map explicit failures back to the same route with a user-facing Portuguese error. On success, find the user by the accepted invitation e-mail only inside the secure server flow, call `SignInManager.SignInAsync`, and redirect to `/`.

  Prefer extending `AcceptEmployeeInvitationUseCase`'s success result with the created user ID so the endpoint can load the exact newly created Identity user by ID; do not trust browser data or re-query by unvalidated e-mail.

- [ ] **Step 4: Register endpoint routing**

  Call `app.MapEmployeeInvitationEndpoints()` beside the existing account, onboarding, and customer endpoint registrations. Confirm that the public page remains anonymous while `/empresa` stays protected by `CompanyOwner`.

- [ ] **Step 5: Review public data exposure statically**

  Inspect the Razor markup and endpoint request type to confirm no hidden internal IDs or personally sensitive pre-registration fields are posted or displayed. Confirm every invalid-token branch uses the same generic message.

### Task 7: Final static verification and migration review

**Files:**
- Modify as needed only to correct issues discovered in Tasks 1–6.
- Review: `docs/superpowers/specs/2026-08-20-owner-company-and-employee-invitations-design.md`
- Review: `docs/superpowers/plans/2026-08-20-owner-company-and-employee-invitations.md`

**Interfaces:**
- Consumes: the completed feature and its approved specification.
- Produces: a coherent, statically reviewed implementation ready for the user-requested build or test phase.

- [ ] **Step 1: Trace each specification requirement**

  Confirm the implementation provides read-only company information, owner-only navigation and policy, basic employee fields, copyable seven-day secure links, renewal, revocation, active employee/invitation history, anonymous token completion, Employee role assignment, masked public data, and atomic account/membership/invitation changes.

- [ ] **Step 2: Review security-sensitive queries and output**

  Search invitation code for `CompanyId`, `Token`, `TokenHash`, `Log`, `Redirect`, and `QueryString`. Verify owner data access filters by the resolved company, raw tokens are neither stored nor logged, public responses do not contain private data, and no form accepts `CompanyId` or an employee/invitation identifier as authority.

- [ ] **Step 3: Inspect the migration and compile-sensitive contracts**

  Check the migration's table, field lengths, foreign keys, and indexes against Task 1. Manually cross-check constructor signatures, result codes, DI registrations, endpoint names, and Razor injections. Do not run build, test, migration application, or web host because the repository instructions prohibit those commands without an explicit user request.

- [ ] **Step 4: Commit the implementation as focused commits**

  Use focused commits after logical milestones, for example:

  ```text
  feat: add employee invitation domain and persistence
  feat: add employee invitation application flow
  feat: add owner company and employee invitation screens
  ```

  Include only files belonging to each milestone; preserve unrelated user changes.
