# Platform Administrator Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep platform administrators outside company-scoped screens and provide their platform administration entry point.

**Architecture:** The authenticated platform administrator is redirected after login to `/plataforma`, a role-protected landing page using a dedicated layout. Company-scoped route authorization rejects platform administrators before any `ICurrentCompany` access and redirects them to the platform entry point. The existing monthly payments dashboard stays at `/plataforma/licencas` and is linked from the platform navigation.

**Tech Stack:** .NET 10, ASP.NET Core Identity and authorization policies, Blazor Web App, MudBlazor, xUnit.

**Spec:** `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md`, Phase 11 — Administração VoltaMarcada.

## Global Constraints

- Platform administrators are not tenant members and must never require a `CompanyId`.
- Owner and Employee behavior and tenant isolation remain unchanged.
- Source code identifiers and technical messages remain English; UI remains Brazilian Portuguese.
- No secrets or configuration values are changed.

---

### Task 1: Redirect and route protection

**Files:**

- Modify: `src/VoltaMarcada.Web/Endpoints/AccountEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Authorization/CompanyMemberAuthorizationHandler.cs`
- Modify: `src/VoltaMarcada.Web/Authorization/CompanyMemberAuthorizationResultHandler.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Endpoints/AccountLoginTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Licensing/OperationalLicenseAuthorizationTests.cs`

**Interfaces:**

- Consumes: `PlatformRoles.PlatformAdministrator`, `AuthorizationPolicies.CompanyMember`.
- Produces: platform administrators are sent to `/plataforma` at login and on direct company-route navigation.

- [ ] **Step 1: Write failing tests**

Add `Login_RedirectsPlatformAdministratorToPlatformDashboard` that asserts `POST /account/login` yields `/plataforma`; add a direct company-route test asserting a platform administrator receives a redirect to `/plataforma`.

- [ ] **Step 2: Run focused tests to verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~AccountLoginTests|FullyQualifiedName~OperationalLicenseAuthorizationTests"`

Expected: failures assert the current `/` redirect and tenant route access behavior.

- [ ] **Step 3: Implement minimal route separation**

In `LoginAsync`, return `Results.Redirect("/plataforma")` before membership handling for a platform administrator. Do not let `CompanyMemberAuthorizationHandler` succeed a tenant requirement for that role. In `CompanyMemberAuthorizationResultHandler`, redirect an authenticated platform administrator whose `CompanyMemberRequirement` failed to `/plataforma`.

- [ ] **Step 4: Run focused tests to verify GREEN**

Run the command from Step 2. Expected: all selected tests pass.

### Task 2: Platform layout and dashboard entry point

**Files:**

- Create: `src/VoltaMarcada.Web/Components/Layout/PlatformLayout.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/PlatformDashboard.razor`
- Modify: `src/VoltaMarcada.Web/Components/Pages/PlatformLicenses.razor`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/PlatformDashboardRenderingTests.cs`

**Interfaces:**

- Consumes: `AuthorizationPolicies.PlatformAdministrator` and the existing `/plataforma/licencas` page.
- Produces: `/plataforma` platform-only entry page, a dedicated platform navigation, and a return link to the platform dashboard from license management.

- [ ] **Step 1: Write failing rendering tests**

Render `PlatformDashboard` with an authorized platform administrator and assert its title, navigation link to `/plataforma/licencas`, and absence of company navigation labels such as `Clientes` and `Agenda`.

- [ ] **Step 2: Run focused test to verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~PlatformDashboardRenderingTests`

Expected: compilation failure because the platform dashboard component does not yet exist.

- [ ] **Step 3: Implement the platform-only pages**

Create `/plataforma` with `[Authorize(Policy = AuthorizationPolicies.PlatformAdministrator)]`, `@layout PlatformLayout`, a concise administration header, and link to `/plataforma/licencas`. Create `PlatformLayout` with only platform navigation and logout. Add the same layout and a dashboard navigation item to `PlatformLicenses`.

- [ ] **Step 4: Run focused test to verify GREEN**

Run the command from Step 2. Expected: selected test passes.

### Task 3: Phase status and full verification

**Files:**

- Modify: `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md`
- Test: `tests/VoltaMarcada.IntegrationTests/`

- [ ] **Step 1: Reopen the phase during implementation**

Replace the completed marker on Phase 11 with `**Status: Em andamento.**` before final verification.

- [ ] **Step 2: Run full verification**

Run: `dotnet test VoltaMarcada.sln --no-restore`

Expected: zero failed tests.

- [ ] **Step 3: Mark Phase 11 as completed only after green verification**

Restore `**Status: Concluída.**` after the full suite has passed, then run `git diff --check`.

- [ ] **Step 4: Commit the focused implementation**

Commit only the plan, Phase 11 status, platform routing/layout/dashboard, and their tests with message `feat: separate platform administrator dashboard`.
