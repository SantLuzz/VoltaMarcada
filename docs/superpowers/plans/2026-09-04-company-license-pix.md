# Company License and PIX Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build company licensing, manually reviewed PIX renewal, protected proof storage, and platform license administration for Phase 11.

**Architecture:** A separate `CompanyLicense` is created with every company and calculates access from lifecycle configuration and `TimeProvider`. `LicensePayment` represents a proof-backed manual renewal. Central ASP.NET authorization blocks writes before use cases run; platform review and a hosted notification service remain outside tenant context.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Server, EF Core 10, PostgreSQL, ASP.NET Core Identity, xUnit, MudBlazor.

**Spec:** `docs/superpowers/specs/2026-09-04-company-license-pix-design.md`

## Global Constraints

- All code identifiers, migrations, tests, comments, and logs are English; UI copy is Brazilian Portuguese.
- Company identity comes from authenticated membership, never browser input.
- Use UTC timestamps and `TimeProvider`; no direct machine-clock calls in domain/application code.
- `LicenseBilling` PIX credentials are environment configuration or secrets and are never stored in the database.
- Proof retrieval always authenticates and authorizes before `IFileStorage.GetAsync`.
- Use `OperationResult` for expected business failures and xUnit tests written first.

## File structure

| Area | Files |
| --- | --- |
| Domain | `CompanyLicense`, `LicensePayment`, `LicenseOverdueNotification`, lifecycle/payment enums. |
| Application | `Licensing` use cases/contracts/errors and `ILicenseRepository`. |
| Infrastructure | EF configurations, repository, migrations, BR Code generator, hosted service. |
| Web | `LicenseBillingOptions`, license authorization policies, owner/platform endpoints, renewal/blocked/platform pages. |
| Tests | `tests/VoltaMarcada.IntegrationTests/Licensing/*` and targeted component tests. |

---

## Increment 1: License foundation and access policy

### Task 1: Add the domain lifecycle

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/CompanyLicense.cs`
- Create: `src/VoltaMarcada.Domain/Enums/CompanyLicenseAccessMode.cs`
- Create: `src/VoltaMarcada.Domain/Enums/CompanyLicenseStatus.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/CompanyLicenseTests.cs`

**Produces:** `CompanyLicense.Create(Guid companyId, DateTimeOffset now, int trialDays)`, `GetAccessMode(DateTimeOffset now, LicenseLifecyclePolicy policy)`, and `Renew(DateTimeOffset approvedAtUtc)`.

- [ ] **Step 1: Write failing boundary tests**

```csharp
[Fact]
public void Returns_read_only_after_trial_tolerance()
{
    var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);
    Assert.Equal(CompanyLicenseAccessMode.OwnerReadOnly,
        license.GetAccessMode(new DateTimeOffset(2026, 9, 19, 0, 0, 0, TimeSpan.Zero), LicenseLifecyclePolicy.Default));
}

[Fact]
public void Renew_preserves_unexpired_paid_days()
{
    var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);
    license.Renew(new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero));
    license.Renew(new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero));
    Assert.Equal(new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero), license.PaidUntilUtc);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CompanyLicenseTests`

Expected: compile failure because license types are missing.

- [ ] **Step 3: Implement the minimum model**

```csharp
public enum CompanyLicenseAccessMode { Operational, OwnerReadOnly, Terminated }
public sealed record LicenseLifecyclePolicy(int TrialToleranceDays, int TrialTerminationDays, int PaidReadOnlyDays, int PaidTerminationDays)
{
    public static LicenseLifecyclePolicy Default { get; } = new(3, 10, 7, 20);
}
```

Use private setters, normalize UTC, implement 14-day trial/tolerance/termination and paid thresholds from the spec, and renew with `max(PaidUntilUtc, approvedAtUtc).AddMonths(1)`.

- [ ] **Step 4: Verify green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CompanyLicenseTests`

Expected: PASS, including terminated trial day 10 and paid-overdue day 20 tests.

- [ ] **Step 5: Commit**

Run: `git add src/VoltaMarcada.Domain tests/VoltaMarcada.IntegrationTests/Licensing/CompanyLicenseTests.cs && git commit -m "feat: add company license lifecycle"`

### Task 2: Persist the license and create it atomically at onboarding

**Files:**
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/ICompanyOnboardingRepository.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CompanyOnboardingRepository.cs`
- Modify: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingUseCase.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyLicenseConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_AddCompanyLicenses.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/CompanyLicensePersistenceTests.cs`

**Consumes:** `CompanyLicense.Create`. **Produces:** `CreateCompanyAndOwnerAsync(Company, CompanyUser, CompanyLicense, CancellationToken)`.

- [ ] **Step 1: Write the failing onboarding test**

```csharp
[Fact]
public async Task CompleteCompanyOnboarding_creates_a_trial_license()
{
    var companyId = await fixture.CompleteCompanyOnboardingAsync();
    await using var db = fixture.CreateDbContext();
    var license = await db.CompanyLicenses.SingleAsync(x => x.CompanyId == companyId);
    Assert.Equal(CompanyLicenseStatus.Trial, license.Status);
    Assert.Equal(fixture.Clock.GetUtcNow().AddDays(14), license.TrialEndsAtUtc);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CompanyLicensePersistenceTests`

Expected: compile failure on missing `CompanyLicenses`.

- [ ] **Step 3: Implement mapping and transaction change**

```csharp
public DbSet<CompanyLicense> CompanyLicenses => Set<CompanyLicense>();
await dbContext.Companies.AddAsync(company, cancellationToken);
await dbContext.CompanyUsers.AddAsync(ownerMembership, cancellationToken);
await dbContext.CompanyLicenses.AddAsync(license, cancellationToken);
await dbContext.SaveChangesAsync(cancellationToken);
```

Map a required unique `CompanyId` FK with restricted deletion. Build the license in onboarding from `TimeProvider` and configured trial days before the existing transactional repository call. Generate the descriptive EF migration.

- [ ] **Step 4: Verify green and model snapshot**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~CompanyLicensePersistenceTests`

Run: `dotnet ef migrations has-pending-model-changes --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web`

Expected: PASS and no pending model changes.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: create trial license during onboarding"`

### Task 3: Add central access authorization and dashboard status

**Files:**
- Create: `src/VoltaMarcada.Application/Licensing/LicenseAccessContracts.cs`
- Create: `src/VoltaMarcada.Application/Licensing/GetCurrentCompanyLicenseUseCase.cs`
- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/ILicenseRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/LicenseRepository.cs`
- Create: `src/VoltaMarcada.Web/Authorization/OperationalLicenseRequirement.cs`
- Create: `src/VoltaMarcada.Web/Authorization/OperationalLicenseAuthorizationHandler.cs`
- Modify: `src/VoltaMarcada.Web/Authorization/AuthorizationPolicies.cs`
- Modify: `src/VoltaMarcada.Web/Authorization/CompanyMemberAuthorizationResultHandler.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Modify: company mutation mappings in `src/VoltaMarcada.Web/Endpoints/*Endpoints.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Dashboard.razor`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/OperationalLicenseAuthorizationTests.cs`

**Produces:** policy `AuthorizationPolicies.OperationalCompanyLicense` and `CompanyLicenseAccessDto` for the dashboard.

- [ ] **Step 1: Write failing role/access authorization tests**

```csharp
[Theory]
[InlineData(CompanyUserRole.Owner, CompanyLicenseAccessMode.OwnerReadOnly, StatusCodes.Status403Forbidden)]
[InlineData(CompanyUserRole.Employee, CompanyLicenseAccessMode.OwnerReadOnly, StatusCodes.Status403Forbidden)]
[InlineData(CompanyUserRole.Owner, CompanyLicenseAccessMode.Operational, StatusCodes.Status200OK)]
public async Task Work_order_mutation_requires_operational_license(CompanyUserRole role, CompanyLicenseAccessMode mode, int expected)
{
    var response = await fixture.SendCreateWorkOrderAsync(role, mode);
    Assert.Equal(expected, (int)response.StatusCode);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~OperationalLicenseAuthorizationTests`

Expected: restricted mutation cases fail because membership currently grants writes.

- [ ] **Step 3: Implement server-side access enforcement**

```csharp
public sealed class OperationalLicenseRequirement : IAuthorizationRequirement;
public static RouteHandlerBuilder RequireOperationalCompanyLicense(this RouteHandlerBuilder builder) =>
    builder.RequireAuthorization(AuthorizationPolicies.OperationalCompanyLicense);
```

The handler loads membership and license together, uses `TimeProvider`, succeeds only for `Operational`, and does not grant platform administrators tenant mutation access. Apply it to every company POST/PUT/PATCH/DELETE mapping; retain `CompanyMember` for reads. Redirect restricted owners to `/licenca/renovar` and employees to `/conta-bloqueada`. Render days remaining and `Aderir` (last 3 trial days) or `Renovar licença` (7 days before paid expiry) from the DTO.

- [ ] **Step 4: Verify green and no regression**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~OperationalLicenseAuthorizationTests|FullyQualifiedName~OwnerDashboard"`

Expected: restricted writes denied, normal writes work, dashboard tests pass.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: enforce company license access"`

## Increment 2: PIX renewal and proof submission

### Task 4: Add payment state, static BR Code, and proof submission

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/LicensePayment.cs`
- Create: `src/VoltaMarcada.Domain/Enums/LicensePaymentStatus.cs`
- Create: `src/VoltaMarcada.Application/Licensing/LicensePaymentContracts.cs`
- Create: `src/VoltaMarcada.Application/Licensing/SubmitLicensePaymentUseCase.cs`
- Create: `src/VoltaMarcada.Application/Licensing/GetLicenseRenewalUseCase.cs`
- Create: `src/VoltaMarcada.Application/Abstractions/Payments/IPixPayloadGenerator.cs`
- Create: `src/VoltaMarcada.Infrastructure/Payments/BrCodePixPayloadGenerator.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/LicensePaymentConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/LicenseRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_AddLicensePayments.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/LicensePaymentSubmissionTests.cs`

**Produces:** `SubmitLicensePaymentUseCase.ExecuteAsync(LicensePaymentProofUpload, CancellationToken)` and `IPixPayloadGenerator.Generate(PixPaymentRequest)`.

- [ ] **Step 1: Write failing payment and BR Code tests**

```csharp
[Fact]
public async Task Submit_creates_pending_payment_and_company_scoped_proof()
{
    var result = await fixture.SubmitPaymentAsync("proof.png", "image/png", 1024);
    Assert.True(result.IsSuccess);
    Assert.Equal(LicensePaymentStatus.PendingReview, result.Value!.Status);
    Assert.Contains($"pagamentos/{fixture.CompanyId}", fixture.Storage.LastUpload!.Folder);
}

[Fact]
public void Generate_returns_payload_with_valid_crc()
{
    var payload = generator.Generate(new PixPaymentRequest("key@example.com", "VoltaMarcada", "SAO PAULO", 49.90m));
    Assert.EndsWith("6304" + BrCodeCrc16.Calculate(payload.CopyAndPasteCode[..^4]), payload.CopyAndPasteCode);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~LicensePaymentSubmissionTests`

Expected: missing payment/generator compile failures.

- [ ] **Step 3: Implement minimal secure submission**

```csharp
if (await repository.HasPendingPaymentAsync(currentCompany.CompanyId, cancellationToken))
    return OperationResult<LicensePaymentSubmissionDto>.Failure(LicenseErrors.PendingPaymentAlreadyExists);
var payment = LicensePayment.Create(currentCompany.CompanyId, billing.MonthlyPrice, now);
var stored = await fileStorage.UploadAsync(new FileStorageUploadRequest(upload.File, $"pagamentos/{currentCompany.CompanyId}"), cancellationToken);
payment.AttachProof(stored.Key, upload.File.ContentType, upload.File.Length, now);
```

Accept PDF/JPEG/PNG/WebP only after matching content type, extension, nonzero size, and configured maximum. Store `pagamentos/{companyId}/{paymentId}.{extension}`, delete the upload if the insert loses the partial-unique-index race, and map only one `PendingReview` payment per company. Generate EMV/BR Code and CRC-16 locally; do not add a gateway.

- [ ] **Step 4: Verify green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~LicensePaymentSubmissionTests`

Run: `dotnet ef migrations has-pending-model-changes --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web`

Expected: valid submission succeeds; invalid size/type and duplicate pending proof return expected errors; no pending model changes.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: submit PIX license payment proofs"`

### Task 5: Add owner renewal and protected content endpoints

**Files:**
- Create: `src/VoltaMarcada.Web/Endpoints/LicenseEndpoints.cs`
- Create: `src/VoltaMarcada.Web/Components/Pages/LicenseRenewal.razor`
- Create: `src/VoltaMarcada.Web/Components/Pages/BlockedAccount.razor`
- Modify: `src/VoltaMarcada.Web/Components/Routes.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/LicenseEndpointsTests.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Components/LicenseRenewalRenderingTests.cs`

**Produces:** owner-only `GET /license/payments/{paymentId}/proof` and renewal screen.

- [ ] **Step 1: Write failing proof-isolation and page tests**

```csharp
[Fact]
public async Task GetProof_returns_not_found_for_another_company_owner()
{
    var response = await fixture.SendGetProofAsOtherCompanyOwnerAsync();
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}

[Fact]
public void Renewal_page_shows_pix_and_pending_status()
{
    var markup = RazorComponentRenderer.Render<LicenseRenewal>(fixture.PendingRenewal());
    Assert.Contains("PIX Copia e Cola", markup);
    Assert.Contains("Comprovante enviado", markup);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~LicenseEndpointsTests|FullyQualifiedName~LicenseRenewalRenderingTests"`

Expected: route/component discovery failure.

- [ ] **Step 3: Implement routes and accessible UI**

```csharp
group.MapGet("/payments/{paymentId:guid}/proof", async (Guid paymentId, GetLicensePaymentProofUseCase useCase, CancellationToken ct) =>
{
    var proof = await useCase.ExecuteAsync(paymentId, ct);
    return proof is null ? Results.NotFound() : Results.File(proof.Content, proof.ContentType);
});
```

Use owner policy for renewal/proof reads, operational-license policy for upload, and query payment by both ID and current company. Show QR image, copy-and-paste payload, R$49,90 configured price, manual-analysis notice, and pending state. Make blocked page reachable without company-member authorization.

- [ ] **Step 4: Verify green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~LicenseEndpointsTests|FullyQualifiedName~LicenseRenewalRenderingTests"`

Expected: only company owner can retrieve proof and renewal UI renders required state.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: add license renewal experience"`

## Increment 3: Platform review and notifications

### Task 6: Implement platform-only review and atomic renewal

**Files:**
- Create: `src/VoltaMarcada.Application/Licensing/GetPlatformLicensePaymentsUseCase.cs`
- Create: `src/VoltaMarcada.Application/Licensing/ReviewLicensePaymentUseCase.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/LicenseRepository.cs`
- Create: `src/VoltaMarcada.Web/Endpoints/PlatformLicenseEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Authorization/AuthorizationPolicies.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/LicensePaymentReviewTests.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/PlatformLicenseEndpointsTests.cs`

**Produces:** `ReviewLicensePaymentUseCase.ExecuteAsync(Guid, LicensePaymentReviewCommand, CancellationToken)` and `/platform/licenses/payments` review routes.

- [ ] **Step 1: Write failing review and isolation tests**

```csharp
[Fact]
public async Task Approve_renews_license_and_records_administrator()
{
    var result = await fixture.ApprovePendingPaymentAsync();
    var payment = await fixture.GetPaymentAsync();
    var license = await fixture.GetLicenseAsync();
    Assert.True(result.IsSuccess);
    Assert.Equal(LicensePaymentStatus.Approved, payment.Status);
    Assert.Equal(fixture.PlatformAdministratorId, payment.DecidedByUserId);
    Assert.Equal(fixture.ApprovedAtUtc.AddMonths(1), license.PaidUntilUtc);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~LicensePaymentReviewTests|FullyQualifiedName~PlatformLicenseEndpointsTests"`

Expected: missing review use case/routes.

- [ ] **Step 3: Implement explicit review state transitions**

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
var payment = await dbContext.LicensePayments.SingleOrDefaultAsync(x => x.Id == paymentId, cancellationToken);
if (payment is null || payment.Status != LicensePaymentStatus.PendingReview) return LicensePaymentReviewPersistenceOutcome.NotPending;
payment.Approve(platformAdministratorId, now);
license.Renew(now);
await dbContext.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

Require a rejection reason, retain actor/date/reason, use a dedicated platform-administrator policy only, and list safe DTOs containing company, owner/e-mail, expiry, payment, and review fields without `ICurrentCompany`.

- [ ] **Step 4: Verify green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~LicensePaymentReviewTests|FullyQualifiedName~PlatformLicenseEndpointsTests"`

Expected: only platform admin can review, duplicate approval fails, and rejected payment permits a new submission.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: review license payments as platform admin"`

### Task 7: Build the platform dashboard and secure proof viewer

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Pages/PlatformLicenses.razor`
- Modify: `src/VoltaMarcada.Web/Components/Routes.razor`
- Modify: `src/VoltaMarcada.Web/Endpoints/PlatformLicenseEndpoints.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Components/PlatformLicensesRenderingTests.cs`

- [ ] **Step 1: Write a failing page test**

```csharp
[Fact]
public void PlatformLicenses_renders_payment_review_data()
{
    var markup = RazorComponentRenderer.Render<PlatformLicenses>(fixture.PendingPayment());
    Assert.Contains("Pagamentos de licença", markup);
    Assert.Contains("Empresa Exemplo", markup);
    Assert.Contains("owner@example.com", markup);
    Assert.Contains("Aprovar", markup);
    Assert.Contains("Rejeitar", markup);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~PlatformLicensesRenderingTests`

Expected: component discovery failure.

- [ ] **Step 3: Implement dashboard and platform proof endpoint**

```razor
<MudTable Items="payments">
    <HeaderContent><MudTh>Empresa</MudTh><MudTh>Owner</MudTh><MudTh>Vencimento</MudTh><MudTh>Status</MudTh><MudTh>Ações</MudTh></HeaderContent>
    <RowTemplate><MudTd>@context.CompanyTradeName</MudTd><MudTd>@context.OwnerEmail</MudTd><MudTd>@context.Status</MudTd></RowTemplate>
</MudTable>
```

Provide pending/date filters, proof link backed by a platform-authenticated endpoint, rejection modal with required reason, and refresh/error feedback. Never return a storage key or PIX secret.

- [ ] **Step 4: Verify green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter "FullyQualifiedName~PlatformLicensesRenderingTests|FullyQualifiedName~PlatformLicenseEndpointsTests"`

Expected: dashboard renders, regular company users cannot view platform proof.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: add platform license dashboard"`

### Task 8: Send daily overdue paid-license notifications

**Files:**
- Create: `src/VoltaMarcada.Domain/Entities/LicenseOverdueNotification.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/LicenseOverdueNotificationConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/LicenseRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Licensing/OverdueLicenseNotificationService.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/<timestamp>_AddLicenseOverdueNotifications.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Licensing/OverdueLicenseNotificationServiceTests.cs`

**Produces:** one successful-notification record per license/owner/local date and a registered `BackgroundService`.

- [ ] **Step 1: Write failing idempotency and failure tests**

```csharp
[Fact]
public async Task ExecuteAsync_sends_once_per_owner_per_day()
{
    await service.ExecuteAsync(CancellationToken.None);
    await service.ExecuteAsync(CancellationToken.None);
    Assert.Single(emailSender.Messages);
    Assert.Single(await fixture.GetNotificationsAsync());
}

[Fact]
public async Task ExecuteAsync_does_not_record_when_email_fails()
{
    emailSender.ThrowOnSend = true;
    await service.ExecuteAsync(CancellationToken.None);
    Assert.Empty(await fixture.GetNotificationsAsync());
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~OverdueLicenseNotificationServiceTests`

Expected: missing service/notification record compile failure.

- [ ] **Step 3: Implement daily service**

```csharp
foreach (var recipient in await repository.GetOverduePaidLicenseOwnersAsync(now, cancellationToken))
{
    if (await repository.HasNotificationForDayAsync(recipient.LicenseId, recipient.OwnerUserId, sentOn, cancellationToken)) continue;
    await emailSender.SendAsync(new EmailMessage(recipient.OwnerEmail, "Licença VoltaMarcada vencida", recipient.Body), cancellationToken);
    await repository.RecordOverdueNotificationAsync(recipient.LicenseId, recipient.OwnerUserId, sentOn, now, cancellationToken);
}
```

Query paid licenses overdue fewer than 20 days; calculate `sentOn` in configured Brazilian business timezone; log failure per recipient and continue. Map a unique `(CompanyLicenseId, OwnerUserId, SentOn)` index and schedule the cancellable service for the next daily run.

- [ ] **Step 4: Verify green and regression suite**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~OverdueLicenseNotificationServiceTests`

Run: `dotnet test VoltaMarcada.sln`

Expected: no duplicate success record, failed sends remain retryable, full solution passes.

- [ ] **Step 5: Commit**

Run: `git add src tests && git commit -m "feat: notify overdue license owners"`

## Plan self-review

- Spec coverage: Tasks 1–3 implement lifecycle, onboarding, dashboard warning, read-only/blocking behavior, and server-side operational-write enforcement. Tasks 4–5 implement static PIX, proof security, and owner renewal. Tasks 6–7 implement platform review/audit/dashboard. Task 8 implements daily paid-license overdue warnings and idempotency.
- Placeholder scan: EF migration timestamp is generated by the CLI at execution; all production behaviors, tests, interfaces, and acceptance commands are explicit.
- Type consistency: domain types precede persistence; repository/contracts precede use-case/UI consumers; platform review consumes the payment types created in Increment 2.

## Execution handoff

Plan complete and saved to `docs/superpowers/plans/2026-09-04-company-license-pix.md`.

1. **Subagent-Driven (recommended)** — fresh agent per task, with review gates.
2. **Inline Execution** — execute this plan in the current session with checkpoints.
