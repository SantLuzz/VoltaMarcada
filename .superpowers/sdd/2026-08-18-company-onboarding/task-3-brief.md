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

## Prior ruling that binds this task

Do not add a sequential or further suffix when the document-suffixed slug already exists. Preserve `DuplicateSlug` so Application returns explicit user-facing conflict; the approved spec treats this as impossible under the unique-document invariant.
