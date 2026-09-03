# Phase 14 Commercial SaaS Launch Readiness Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the deployed product into an operable self-service SaaS launch with coherent onboarding, invitations, trial/license visibility, and platform operating indicators.

**Architecture:** Reuse the onboarding, invitation, and Phase 11 license models instead of creating a second subscription system. Application composes platform-administration projections from company, membership, and license data; Web performs final journey and responsive-layout refinement; operational procedures measure the pilot launch.

**Tech Stack:** .NET 10, C#, ASP.NET Core Identity, Blazor Web App, EF Core, PostgreSQL, MudBlazor, xUnit.

**Spec:** `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md` (Phase 14).

## Global Constraints

- `CompanyLicense` is the single subscription/license source of truth; do not add a parallel `Subscription` entity in this phase.
- Keep self-service registration and invitation acceptance tenant-safe and token-based.
- Platform administration is isolated from a current-company context and requires the platform-administrator role.
- Retain the single R$49.90 monthly plan and manual payment approval until commercial validation justifies a gateway.
- Do not add usage-based billing, plan limits, or advanced permissions without validated requirements.

---

## File Structure

- Reuse account/onboarding, invitation, and licensing use cases; add only missing composed platform metrics.
- Create `Application/PlatformAdministration` DTOs and query use cases plus scoped repositories/projections.
- Extend the administrative layout/page introduced in Phase 11 and the Owner dashboard license panel.
- Add component/integration tests for cross-role access and first-company journey.
- Create an operations checklist for pilot onboarding and feedback collection.

### Task 1: Audit and close the self-service journey

**Files:**
- Modify: account registration/login, onboarding, invitation, dashboard, and shared layout components only where journey review finds a defect
- Test: `tests/VoltaMarcada.IntegrationTests/Endpoints/AccountLoginTests.cs`, onboarding/invitation tests, component tests

- [ ] **Step 1: Write the end-to-end acceptance tests**

Cover: visitor opens landing page, registers, creates company, receives a Phase 11 trial license, reaches dashboard, sees the correct trial state, invites an employee, employee accepts securely, and both roles receive their permitted experience.

- [ ] **Step 2: Fix only validated journey gaps**

Apply focused corrections for local return URLs, invitation expiry/acceptance feedback, Portuguese error text, accessible loading states, and mobile layout. Do not redesign authentication or add a new membership model.

- [ ] **Step 3: Run the journey tests**

Run targeted endpoint/component tests; expected PASS for registration, login, onboarding, invitation, trial/license creation, and role separation.

### Task 2: Add platform operating indicators

**Files:**
- Create: `src/VoltaMarcada.Application/PlatformAdministration/PlatformOverviewContracts.cs`, `GetPlatformOverviewUseCase.cs`
- Create/modify: focused repository projections and platform administration page
- Test: `tests/VoltaMarcada.IntegrationTests/PlatformAdministrationTests.cs`

- [ ] **Step 1: Write failing platform-isolation tests**

```csharp
[Fact]
public async Task PlatformOverview_RequiresPlatformAdministratorAndReturnsAggregateCommercialIndicators()
{
    var overview = await platformAdminUseCase.ExecuteAsync();
    Assert.Equal(10, overview.CompanyCount);
    Assert.Equal(3, overview.ActiveTrialCount);
    Assert.Equal(5, overview.ActiveLicenseCount);
}
```

- [ ] **Step 2: Implement bounded aggregate projections**

Report companies, active trials, active/restricted/terminated licenses, pending manual payments, owners/users, and a clearly defined activity indicator based on companies with at least one work-order event in the selected period. Do not expose tenant customer records or cross-company operational detail.

- [ ] **Step 3: Run focused tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~PlatformAdministrationTests`

Expected: PASS for administrator authorization and deterministic metrics.

### Task 3: Refine commercial UI and launch content

**Files:**
- Modify: shared layout, dashboard, registration/onboarding/invitation/license pages, CSS
- Modify: `landing/index.html`, `landing/styles.css` when Phase 13 feedback requires it
- Test: component-source and responsive browser checklist

- [ ] **Step 1: Define acceptance assertions**

Assert all primary calls to action are visible without hidden navigation, primary forms have clear required/invalid states, and desktop/mobile spacing is consistent across login, registration, onboarding, Owner dashboard, employee dashboard, and renewal.

- [ ] **Step 2: Implement scoped layout corrections**

Use existing shared primitives and CSS. Keep the Owner/Employee separation, show the company trial/license state in the appropriate Owner surface, and remove visual dead ends discovered in the documented journey.

- [ ] **Step 3: Verify no authorization regression**

Run affected component tests and manually validate an anonymous visitor, Owner, Employee, and platform administrator session.

### Task 4: Establish launch operations and pilot feedback loop

**Files:**
- Create: `docs/operations/commercial-launch-checklist.md`
- Modify: `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md`, `Planos negócio/VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md`

- [ ] **Step 1: Document launch readiness criteria**

Include VPS verification, backup recovery status, secret rotation ownership, support contact, trial/payment review routine, platform-admin review, and the first-pilot onboarding script.

- [ ] **Step 2: Document commercial validation data**

Capture pilot-company count, weekly active companies, maintenance opportunities, contacts, requests, bookings, completed return services, recovered revenue, trial conversion, payment-review time, and recurring customer feedback. Keep customer personal data out of the report.

- [ ] **Step 3: Run final verification and commit**

Run `dotnet test VoltaMarcada.sln --no-restore`, `git diff --check`, and the launch checklist review. Commit with `chore: prepare commercial saas launch`.