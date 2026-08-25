# Task 4 Report: Current Context and Onboarding Endpoints

## Changed Files

- `src/VoltaMarcada.Web/Identity/CurrentUser.cs`
- `src/VoltaMarcada.Web/Identity/CurrentCompany.cs`
- `src/VoltaMarcada.Web/Endpoints/AccountEndpoints.cs`
- `src/VoltaMarcada.Web/Endpoints/CompanyOnboardingEndpoints.cs`
- `src/VoltaMarcada.Web/Program.cs`
- `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260818124912_AddCompanyOnboarding.cs`
- `src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260818124912_AddCompanyOnboarding.Designer.cs`
- `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `.superpowers/sdd/2026-08-18-company-onboarding/task-4-report.md`

## Current Context and Dependency Injection

`CurrentUser` reads `ClaimTypes.NameIdentifier` from `IHttpContextAccessor.HttpContext.User`. It reports an unauthenticated request without throwing and returns `Guid.Empty` when the claim is missing or invalid.

`CurrentCompany` lazily reads the current authenticated user's membership from `AppDbContext.CompanyUsers` and returns its `CompanyId`. It intentionally throws for an unauthenticated user or a caller without exactly one membership because the `ICurrentCompany` contract is reserved for handlers already protected by membership checks.

Web composition now registers `IHttpContextAccessor`, `ICurrentUser`, and `ICurrentCompany` as scoped. Endpoint mappings occur after `UseAntiforgery()` and before Razor component mapping.

## Route Behavior

All routes are regular `POST` endpoints with `[FromForm]` request contracts and therefore do not accept a user ID or company ID from form data.

| Route | Behavior |
| --- | --- |
| `/account/register` | Validates a non-empty valid email and matching non-empty passwords, creates `ApplicationUser`, signs it in, then redirects to `/onboarding/company`. Invalid registration redirects to `/account/register?error=...` with Portuguese feedback. |
| `/account/login` | Uses `PasswordSignInAsync`. Invalid or incomplete credentials always redirect to `/account/login?error=E-mail ou senha inválidos.`; no account-existence information is exposed. A signed-in user with a `CompanyUser` membership goes to `/dashboard`; otherwise it goes to `/onboarding/company`. |
| `/account/logout` | Signs out the current Identity session and redirects to `/account/login`. |
| `/onboarding/company` | Binds only company profile fields, constructs `CompleteCompanyOnboardingCommand`, and invokes `CompleteCompanyOnboardingUseCase`. Success redirects to `/dashboard`; unauthenticated input redirects to login; every other explicit result code redirects with Portuguese feedback. |

The current UI pages are supplied by the following task; these handlers provide the compatible HTTP form targets and query-string feedback contract now.

## Build Verification

Command run after the migration was generated:

```text
dotnet build VoltaMarcada.sln --no-restore
```

Result: exit code `0`.

```text
Build succeeded.
6 Warning(s)
0 Error(s)
```

All six warnings were `NU1900` package vulnerability-audit warnings because the configured NuGet feeds could not be reached. No automated tests were added or run, per the explicit phase decision.

## Migration Command and Result

Available global EF tooling was checked first. The installed `dotnet-ef` is `9.0.1`; no EF 10 CLI tool was available. The project runtime/design package is EF Core `10.0.11`.

The required command was then run exactly:

```text
dotnet ef migrations add AddCompanyOnboarding --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web --output-dir Persistence/Migrations
```

Result: exit code `0`.

```text
Build started...
Build succeeded.
The Entity Framework tools version '9.0.1' is older than that of the runtime '10.0.11'.
Done. To undo this action, use 'ef migrations remove'
```

The migration was generated successfully. As this repository had no prior migration or model snapshot, `AddCompanyOnboarding` is an initial schema migration: it creates the Identity tables, expanded `Companies`, `CompanyUsers`, and the expected unique indexes `UX_Companies_Document`, `UX_Companies_Slug`, and `UX_CompanyUsers_UserId_CompanyId`. It does not contain a legacy `AspNetUsers.CompanyId` drop because no previous migration schema existed.

## Concerns

- The generated migration was created by EF CLI 9.0.1 while the runtime is EF 10.0.11. It compiled successfully, but should be reviewed or regenerated with a matching EF 10 CLI before it is applied to a production database.
- The initial migration is appropriate for a fresh database. If an existing deployed schema predates this repository's absent migration history, it needs a separate, carefully reviewed baseline/transition migration before deployment.
- `ICurrentCompany` assumes the current product state where a user has exactly one active company membership. Multi-company membership will require an explicit active-company selection mechanism before this context can be used unchanged.
