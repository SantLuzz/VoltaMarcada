# Phase 11 License Payments and Manual PIX Renewal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Commercialize the single VoltaMarcada plan through company licenses, manual PIX proof review, and server-enforced license access rules.

**Architecture:** `CompanyLicense` and `LicensePaymentSubmission` are company-scoped persistence entities. Application evaluates license access from dates and guards all tenant writes; Infrastructure owns storage and SMTP; Web exposes owner renewal and platform-administration experiences.

**Tech Stack:** .NET 10, C#, ASP.NET Core Identity, Blazor Web App, EF Core, PostgreSQL, Cloudinary-compatible storage, SMTP, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-03-phase-eleven-license-payments-design.md`.

## Global Constraints

- Preserve the exact state model, local-date boundaries, and fixed monthly amount in the Phase 11 specification.
- Keep Pix and SMTP credentials in configuration or secret providers only.
- Enforce the evaluated write restriction in Application and HTTP boundaries; UI disabling is not authorization.
- Tenant requests never choose a company ID; only platform administrators may review payment submissions.
- Expected business failures use `OperationResult`; unexpected storage, database, and mail failures remain unexpected failures.

---

## File Structure

- Add license domain entities/enums, EF configurations, migrations, and focused repositories.
- Add licensing configuration, access evaluator, write guard, payment and administrative use cases.
- Extend file storage for authenticated proof content and Infrastructure for SMTP delivery.
- Add renewal, blocked-account, and platform-administration Razor pages plus protected endpoints.
- Add deterministic integration tests using a fake time provider and fake storage/mail implementations.

### Task 1: Model and persist licenses and payment submissions

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/CompanyLicense.cs`, `LicensePaymentSubmission.cs`
- Create: `src/VoltaMarcada.Domain/Enums/LicensePaymentSubmissionStatus.cs`
- Create: configurations and migration `AddCompanyLicensesAndPaymentSubmissions`
- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/ICompanyLicenseRepository.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/LicensingPersistenceTests.cs`

- [ ] **Step 1: Write failing license lifecycle tests**

```csharp
[Fact]
public void ApprovePayment_ExtendsFromTheLaterOfPaidThroughAndApprovalDate()
{
    var license = CreatePaidLicense(new DateOnly(2026, 10, 20));
    license.ExtendPaidThrough(new DateOnly(2026, 9, 3));
    Assert.Equal(new DateOnly(2026, 11, 20), license.PaidThroughOn);
}
```

- [ ] **Step 2: Run RED, implement invariants, migration, and company-creation integration**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~LicensingPersistenceTests`

Create one license with every company in the onboarding transaction, enforce one pending submission per company, and map the required indexes. Generate and inspect the migration through EF Core.

- [ ] **Step 3: Run GREEN and commit**

Run the focused test again; expected PASS. Commit with `feat: add company licenses and payment submissions`.

### Task 2: Evaluate access and protect every tenant write

**Files:**
- Create: `Application/Licensing/ILicenseAccessEvaluator.cs`, `CompanyLicenseAccess.cs`, `LicenseWriteGuard.cs`
- Modify: tenant mutation registration/use-case composition points
- Test: `tests/VoltaMarcada.IntegrationTests/LicenseAccessTests.cs`

- [ ] **Step 1: Write boundary tests**

```csharp
[Theory]
[InlineData("TrialRestricted", false, true)]
[InlineData("PaidOverdue", true, true)]
public async Task Evaluator_ReturnsExpectedOwnerAndEmployeeAccess(string state, bool employeeAllowed, bool ownerReadOnly)
{
    var access = await evaluator.EvaluateAsync(companyId);
    Assert.Equal(state, access.State.ToString());
    Assert.Equal(employeeAllowed, access.EmployeeCanAccess);
    Assert.Equal(ownerReadOnly, access.OwnerIsReadOnly);
}
```

- [ ] **Step 2: Implement evaluator and guard**

Use company-local `DateOnly`, all exact trial/overdue thresholds from the spec, and a small guard invoked before each tenant mutation. Keep read-only owner queries available and bypass only the dedicated platform-administration surface.

- [ ] **Step 3: Run focused and solution tests**

Run the focused tests, then `dotnet test VoltaMarcada.sln --no-restore`; expected PASS.

### Task 3: Implement renewal, proof, review, and notification flows

**Files:**
- Create: licensing commands/use cases/endpoints and storage/email abstractions
- Modify: `IFileStorage`, storage implementation, `Program.cs`, and DI registrations
- Test: `tests/VoltaMarcada.IntegrationTests/LicensePaymentFlowTests.cs`

- [ ] **Step 1: Write failing flow tests**

```csharp
[Fact]
public async Task SubmitProof_WhenAnotherPendingSubmissionExists_ReturnsExpectedFailureWithoutUpload()
{
    var result = await submitProof.ExecuteAsync(validUpload);
    Assert.Equal("Licensing.PendingSubmissionExists", result.Error!.Code);
    Assert.Empty(fileStorage.UploadRequests);
}
```

- [ ] **Step 2: Implement server-side PIX, proof, review, and daily mail behavior**

Generate the BR Code from secrets, validate allowed image/PDF proofs before upload, store under the specified authenticated key, delete a just-uploaded proof if metadata save fails, and make approval idempotent. A periodic sender must notify at most once per company/local day.

- [ ] **Step 3: Run GREEN**

Run `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~LicensePaymentFlowTests`; expected PASS.

### Task 4: Deliver owner and platform-administration UI

**Files:**
- Create: renewal, blocked-account, and platform-license pages/layout
- Modify: dashboard, navigation, error mapping, and endpoint mapping
- Test: component and endpoint tests for owner, employee, and administrator routes

- [ ] **Step 1: Write failing authorization/UI tests**

Assert that an employee never sees payment details, a restricted owner reaches renewal but cannot mutate tenant data, and only `PlatformAdministrator` can retrieve proof content and approve/reject a submission.

- [ ] **Step 2: Implement pages and redirects**

Render Portuguese renewal and status information, server-generated QR code data, proof submission status, a month-selectable admin grid, and the exact redirects in the specification. Do not return proof storage URLs.

- [ ] **Step 3: Run final verification and commit**

Run focused tests, `dotnet test VoltaMarcada.sln --no-restore`, and `git diff --check`. Commit with `feat: add manual license renewal and administration`.