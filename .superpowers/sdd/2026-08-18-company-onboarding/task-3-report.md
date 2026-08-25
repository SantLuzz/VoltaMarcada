# Task 3 Report: Company onboarding persistence

## Files changed

- `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
  - Added the `CompanyUsers` DbSet.
- `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs`
  - Mapped the expanded company fields, required values, timestamps, sensible maximum lengths, and optional `LogoKey`.
  - Added unique indexes for `Document` and `Slug`.
- `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyUserConfiguration.cs`
  - Added the `CompanyUsers` table mapping, required ID/role/timestamp properties, restrictive FKs to `ApplicationUser` and `Company`, and the membership unique index.
- `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CompanyOnboardingRepository.cs`
  - Implemented membership/document/slug checks and the atomic company plus owner-membership persistence operation.
- `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
  - Registered `ICompanyOnboardingRepository` as scoped.
- `src/VoltaMarcada.Infrastructure/Identity/ApplicationUser.cs`
  - Removed `CompanyId` and `Company`; membership is now the sole company association.
- `src/VoltaMarcada.Infrastructure/Identity/ApplicationUserConfiguration.cs`
  - Removed the direct company relationship mapping while preserving the Identity user table mapping.

No migration files were created because EF tooling could not construct the design-time host.

## Constraints and transaction behavior

The explicitly named unique indexes are:

- `UX_Companies_Document` on `Companies.Document`
- `UX_Companies_Slug` on `Companies.Slug`
- `UX_CompanyUsers_UserId_CompanyId` on `(CompanyUsers.UserId, CompanyUsers.CompanyId)`

`CompanyOnboardingRepository.CreateCompanyAndOwnerAsync` starts an EF Core transaction, stages the company and owner membership, saves both changes, and commits only after the save succeeds. A PostgreSQL `DbUpdateException` from `UX_Companies_Document` returns `DuplicateDocument`; one from `UX_Companies_Slug` returns `DuplicateSlug`. It explicitly rolls back before returning either expected outcome. All other database failures are re-thrown. The duplicate-slug outcome is intentionally preserved; no extra sequential suffix is generated after a document-suffixed slug collision.

## Migration status

Attempted command:

```text
dotnet ef migrations add AddCompanyOnboarding --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web --output-dir Persistence/Migrations
```

The initial attempt built successfully but exceeded the 120-second execution timeout before producing migration files.

Retry command:

```text
dotnet ef migrations add AddCompanyOnboarding --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web --output-dir Persistence/Migrations --no-build --verbose
```

The retry failed because the startup host validates `CompleteCompanyOnboardingUseCase`, whose `ICurrentUser` dependency is not registered yet:

```text
Unable to resolve service for type 'VoltaMarcada.Application.Abstractions.Identity.ICurrentUser'
while attempting to activate 'VoltaMarcada.Application.CompanyOnboarding.CompleteCompanyOnboardingUseCase'.
```

EF then could not create `AppDbContext` because `DbContextOptions<AppDbContext>` was unavailable from the fallback provider. The command also warned that local `dotnet-ef` 9.0.1 is older than the EF runtime 10.0.11. No `Persistence/Migrations` files were created, so the requested inspection of generated `CompanyUsers`, expanded `Companies`, dropped legacy `CompanyId`, and the three indexes remains blocked.

## Build output

`dotnet build VoltaMarcada.sln` completed with exit code `0`:

```text
Build succeeded.
12 Warning(s)
0 Error(s)
```

All warnings were `NU1900` package-vulnerability audit warnings caused by unavailable NuGet package feeds; there were no compiler or build errors.

## Concerns

- Migration generation must be retried after an `ICurrentUser` implementation/registration (or an `IDesignTimeDbContextFactory<AppDbContext>`) is available. It should be generated with an EF 10 tool version matching the project runtime.
- There is no prior migration in the repository. Once EF tooling is unblocked, inspect whether the first generated migration represents a clean initial schema or whether an existing deployed schema needs a separately baselined migration to physically remove legacy `AspNetUsers.CompanyId`.
