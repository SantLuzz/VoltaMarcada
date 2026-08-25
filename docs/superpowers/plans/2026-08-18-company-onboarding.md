# Company Onboarding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver Identity registration and login, mandatory first-company onboarding, and a protected dashboard.

**Architecture:** Identity owns credentials and cookies. `CompanyUser` is the only user-to-company association. The explicit onboarding use case atomically persists the tenant and its Owner membership; web endpoints and UI never trust a client-supplied company ID.

**Tech Stack:** .NET 10, ASP.NET Core Identity, Blazor Web App, EF Core, PostgreSQL, FluentValidation, MudBlazor.

**Spec:** `docs/superpowers/specs/2026-08-18-company-onboarding-design.md`

## Global Constraints

- All technical names and messages in source are English; UI text is Brazilian Portuguese.
- Use `TimeProvider`, not direct `DateTime` clock calls, outside the web host.
- Company is the tenant boundary; resolve it from the authenticated membership.
- `LogoKey` is optional. Legal name, trade name, document, phone, WhatsApp, city, and state are mandatory.
- Documents are valid CPF/CNPJ, normalized to digits, and unique. Slugs are unique and use the last four document digits only on base-slug collision.
- Do not add automated tests for this phase. Build and manually verify the specified flows.
- No Git metadata is available in this workspace; do not make commits or worktrees until it is available.

---

## File Structure

| Path | Responsibility |
| --- | --- |
| `src/VoltaMarcada.Domain/Entities/Company.cs` | Complete tenant profile and basic invariants. |
| `src/VoltaMarcada.Domain/Entities/CompanyUser.cs` | Explicit user/company membership. |
| `src/VoltaMarcada.Domain/Enums/CompanyUserRole.cs` | Owner and future employee membership roles. |
| `src/VoltaMarcada.Domain/ValueObjects/BrazilianDocument.cs` | CPF/CNPJ normalization and check-digit validation. |
| `src/VoltaMarcada.Application/Abstractions/{Identity,Tenancy,Persistence}/*` | Current user, current company, and onboarding persistence contracts. |
| `src/VoltaMarcada.Application/CompanyOnboarding/*` | Command, result, validator, slug generator, and use case. |
| `src/VoltaMarcada.Infrastructure/Persistence/{AppDbContext.cs,Configurations/*,Repositories/*}` | EF mappings, transaction, indexes, and onboarding persistence. |
| `src/VoltaMarcada.Infrastructure/Identity/{ApplicationUser.cs,ApplicationUserConfiguration.cs}` | Removes redundant direct company relation. |
| `src/VoltaMarcada.Infrastructure/Persistence/Migrations/*AddCompanyOnboarding.cs` | Schema transition. |
| `src/VoltaMarcada.Web/{Identity/*,Endpoints/*,Components/*,Program.cs}` | Request context, HTTP handlers, pages, authorization redirects, and DI. |

### Task 1: Create the domain tenant and membership model

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/CompanyUser.cs`
- Create: `src/VoltaMarcada.Domain/Enums/CompanyUserRole.cs`
- Create: `src/VoltaMarcada.Domain/ValueObjects/BrazilianDocument.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Company.cs`

**Produces:**

```csharp
public static Company Create(string legalName, string tradeName, string document,
    string phone, string whatsApp, string city, string state, string? logoKey,
    string slug, DateTimeOffset now);
public static CompanyUser Create(Guid userId, Guid companyId,
    CompanyUserRole role, DateTimeOffset now);
public static bool BrazilianDocument.TryNormalize(string? value, out string normalizedDocument);
```

- [ ] Add `CompanyUserRole` with `Owner = 1` and `Employee = 2`, then add a membership entity with private setters/EF constructor and invariant validation for non-empty IDs and defined role.
- [ ] Implement `BrazilianDocument.TryNormalize`: retain digits only, reject repeated sequences, and validate either CPF or CNPJ check digits without infrastructure dependencies.
- [ ] Expand `Company` to `LegalName`, `TradeName`, `Document`, `Phone`, `WhatsApp`, `City`, `State`, optional `LogoKey`, `Slug`, `CreatedAtUtc`, and `UpdatedAtUtc`. Trim text, require all non-logo values, require a normalized valid document, and assign timestamps from `now`.
- [ ] Run `dotnet build VoltaMarcada.sln`; expect exit code 0.

### Task 2: Add onboarding application behavior

**Files:**
- Create: `src/VoltaMarcada.Application/Abstractions/Identity/ICurrentUser.cs`
- Create: `src/VoltaMarcada.Application/Abstractions/Tenancy/ICurrentCompany.cs`
- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/ICompanyOnboardingRepository.cs`
- Create: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingCommand.cs`
- Create: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingResult.cs`
- Create: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingCommandValidator.cs`
- Create: `src/VoltaMarcada.Application/CompanyOnboarding/CompanySlugGenerator.cs`
- Create: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingUseCase.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`

**Consumes:** Task 1 domain APIs and `TimeProvider`.

**Produces:**

```csharp
public interface ICurrentUser { bool IsAuthenticated { get; } Guid UserId { get; } }
public interface ICurrentCompany { Guid CompanyId { get; } }
public sealed record CompleteCompanyOnboardingCommand(string LegalName, string TradeName,
    string Document, string Phone, string WhatsApp, string City, string State, string? LogoKey);
public Task<CompleteCompanyOnboardingResult> ExecuteAsync(
    CompleteCompanyOnboardingCommand command, CancellationToken cancellationToken);
```

- [ ] Define repository methods to check membership, check document/slug existence, and atomically persist `Company` plus `CompanyUser`. Its persistence outcome enum must have `Success`, `DuplicateDocument`, and `DuplicateSlug`.
- [ ] Add the command and FluentValidation validator. Require every non-logo field; invoke `BrazilianDocument.TryNormalize` for the document; return clear result codes including `Unauthenticated`, `AlreadyOnboarded`, `InvalidInput`, `DocumentAlreadyExists`, `SlugAlreadyExists`, and `Success`.
- [ ] Implement slug normalization: lowercase ASCII, remove diacritics, replace non-alphanumeric runs with one hyphen, trim hyphens; use `baseSlug` when available and `$"{baseSlug}-{normalizedDocument[^4..]}"` after collision.
- [ ] Implement the use case: obtain `ICurrentUser`, validate input, normalize document, reject an existing membership/document, create domain objects with `timeProvider.GetUtcNow()`, choose a free slug, and translate transaction results to the explicit result type. Never accept a user ID or company ID in the command.
- [ ] Register `TimeProvider.System` and the use case in application DI, then run `dotnet build VoltaMarcada.sln`; expect exit code 0.

### Task 3: Persist the model, constraints, and transaction

**Files:**
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyUserConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CompanyOnboardingRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Identity/ApplicationUser.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Identity/ApplicationUserConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/*AddCompanyOnboarding.cs`

**Consumes:** `ICompanyOnboardingRepository`, `Company`, and `CompanyUser`.

- [ ] Add `DbSet<CompanyUser>` and map the expanded company properties. Configure named unique indexes for `Document` and `Slug`, sensible maximum lengths, and optional `LogoKey`.
- [ ] Map `CompanyUser` with required restrictive FKs to Identity user and Company, and a named unique index on `(UserId, CompanyId)`.
- [ ] Remove `CompanyId` and `Company` from `ApplicationUser`, then remove its direct-company mapping so membership remains the only source of truth.
- [ ] Implement repository persistence inside an EF transaction. Add company and owner membership, save, commit, and map known PostgreSQL unique-constraint names to the explicit duplicate outcomes. Re-throw unexpected database failures.
- [ ] Register the repository, scaffold `AddCompanyOnboarding` with `dotnet ef migrations add AddCompanyOnboarding --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web --output-dir Persistence/Migrations`, and inspect that it creates `CompanyUsers`, expands `Companies`, drops old `CompanyId`, and creates all three unique indexes.
- [ ] Run `dotnet build VoltaMarcada.sln`; expect exit code 0.

### Task 4: Add current-context services and endpoint behavior

**Files:**
- Create: `src/VoltaMarcada.Web/Identity/CurrentUser.cs`
- Create: `src/VoltaMarcada.Web/Identity/CurrentCompany.cs`
- Create: `src/VoltaMarcada.Web/Endpoints/AccountEndpoints.cs`
- Create: `src/VoltaMarcada.Web/Endpoints/CompanyOnboardingEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`

**Consumes:** Task 2 contracts, Task 3 storage, `UserManager<ApplicationUser>`, and `SignInManager<ApplicationUser>`.

- [ ] Implement `CurrentUser` from `ClaimTypes.NameIdentifier` in `IHttpContextAccessor.HttpContext.User`; unauthenticated state must not throw. Implement `CurrentCompany` by querying `AppDbContext.CompanyUsers` by this user ID; only a caller already protected by membership should request `CompanyId`.
- [ ] Map `POST /account/register`: validate email and matching password, create `ApplicationUser`, sign in with Identity, and redirect to `/onboarding/company`. Map `POST /account/login`: use `PasswordSignInAsync`, redirect to dashboard when membership exists otherwise onboarding, and give the same Portuguese error for invalid credentials regardless of account existence.
- [ ] Map `POST /onboarding/company`: bind only profile fields, invoke `CompleteCompanyOnboardingUseCase`, map result codes to Portuguese feedback, redirect success to dashboard and unauthenticated requests to login. Do not bind any tenant/user identifier.
- [ ] Add a logout POST handler; register `IHttpContextAccessor`, `ICurrentUser`, and `ICurrentCompany` scoped; map all endpoints after antiforgery middleware and before Razor components.
- [ ] Run `dotnet build VoltaMarcada.sln`; expect exit code 0.

### Task 5: Build account, onboarding, dashboard, and authorization UI

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Pages/Account/Register.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/Account/Login.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/CompanyOnboarding.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/Dashboard.razor`
- Create: `src/VoltaMarcada.Web/Components/Shared/RedirectToLogin.razor`
- Create: `src/VoltaMarcada.Web/Components/Shared/RedirectToCompanyOnboarding.razor`
- Modify: `src/VoltaMarcada.Web/Components/Routes.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Home.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`

**Consumes:** Task 4 endpoints and contexts.

- [ ] Add public registration/login pages using regular HTML POST forms and antiforgery tokens (not Blazor circuit events) so Identity can set cookies in their HTTP response. Use Portuguese labels `E-mail`, `Senha`, and `Confirmar senha`; render endpoint error codes through a safe localized mapping.
- [ ] Add `/onboarding/company` with `[Authorize]` and an antiforgery-protected form containing the eight mandatory fields plus optional `LogoKey`. Submit only named profile fields to the onboarding endpoint and explain that logo is the external storage key.
- [ ] Add `[Authorize]` `/dashboard` as a minimal tenant placeholder. Before display, redirect an authenticated user with no membership to onboarding. Use authorization-aware routing to send unauthenticated protected-page visitors to `/account/login`, accepting a return URL only when it is local.
- [ ] Replace starter Home with entry routing: visitor to login, authenticated non-member to onboarding, authenticated member to dashboard. Keep the layout minimal and hide tenant navigation until membership exists.
- [ ] Run `dotnet build VoltaMarcada.sln`, then `dotnet run --project src/VoltaMarcada.Web`; expect clean build and a running local URL.

### Task 6: Apply migration and manually accept the complete flow

**Files:**
- Modify only files needed to correct defects observed in these checks.

- [ ] Start PostgreSQL with `docker compose up -d`, then apply schema using `dotnet ef database update --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web`; expect successful migration.
- [ ] In a private browser session, register a new account. Confirm automatic login reaches onboarding, dashboard cannot be reached before company registration, and omitted mandatory fields produce Brazilian Portuguese feedback.
- [ ] Complete onboarding once with valid CPF and once with valid CNPJ in separate accounts. Confirm masked input is accepted, stored document has digits only, each account reaches dashboard, and each has an Owner membership.
- [ ] Submit an existing document with another account and confirm predictable rejection. Register a different valid document with the same trade name and confirm the slug receives its four-digit document suffix.
- [ ] Log out and confirm protected pages send visitors to login. Login with an onboarded account and confirm dashboard. Login with a registered non-onboarded account and confirm onboarding.
- [ ] Run final `dotnet build VoltaMarcada.sln`; expect exit code 0.

## Plan Self-Review

- Account registration/login, company onboarding, Owner membership, dashboard, context resolution, server-side boundaries, required fields, optional logo, CPF/CNPJ, unique constraints, slug collision, migration, and manual flows are each assigned to Tasks 1–6.
- No task asks for automated tests, complying with the explicit product decision.
- All referenced types are defined in Task 1 or Task 2 before their consuming task.
