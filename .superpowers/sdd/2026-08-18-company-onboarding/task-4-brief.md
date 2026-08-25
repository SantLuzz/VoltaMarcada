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

## Required follow-up from Task 3

After Web DI registration, rerun `dotnet ef migrations add AddCompanyOnboarding --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web --output-dir Persistence/Migrations` using EF 10 tooling. Report whether the migration was generated; do not change model behavior merely to accommodate design-time construction.
