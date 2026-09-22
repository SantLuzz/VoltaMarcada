# Help and Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Provide authenticated company members with operational help, a WhatsApp CTA, and secure support email submission with image attachments.

**Architecture:** Application owns image-signature validation, tenant-aware sender resolution, and `IEmailSender` composition. Web translates multipart input, enforces member authorization and a user-keyed fixed limiter, while `SupportConfiguration` owns WhatsApp normalization.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs and rate limiting, Blazor, MudBlazor, EF Core, Identity, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-17-help-and-support-design.md`

## Global Constraints

- Use English source identifiers and technical text; visible text is Brazilian Portuguese.
- Resolve the user and company on the server. Never trust client identity, recipient, or reply-to fields.
- Limit images to five files, 5 MiB each, and 20 MiB total; accept only signature-valid PNG, JPEG, or WEBP.
- Recipient is `Smtp:SenderEmail`; reply-to is the stored authenticated user email.
- Require `CompanyMember`; limit to five posts per authenticated user per ten minutes.
- Retain existing SMTP infrastructure and use `apply_patch` for all edits.

---

### Task 1: Support application feature

**Files:**
- Create: `src/VoltaMarcada.Application/Support/SendSupportRequestCommand.cs`
- Create: `src/VoltaMarcada.Application/Support/SupportAttachmentInput.cs`
- Create: `src/VoltaMarcada.Application/Support/SupportAttachmentDetector.cs`
- Create: `src/VoltaMarcada.Application/Support/SupportErrors.cs`
- Create: `src/VoltaMarcada.Application/Support/SendSupportRequestUseCase.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Support/SendSupportRequestUseCaseTests.cs`

**Interfaces:** Produces `Task<OperationResult> SendSupportRequestUseCase.ExecuteAsync(SendSupportRequestCommand command, CancellationToken cancellationToken = default)`, with `SendSupportRequestCommand(string Subject, string Message, IReadOnlyList<SupportAttachmentInput> Attachments)` and `SupportAttachmentInput(string FileName, string DeclaredContentType, byte[] Content)`.

- [ ] **Step 1: Write the failing valid-request test**

```csharp
[Fact]
public async Task ExecuteAsync_UsesStoredIdentityAndReplyTo()
{
    var result = await fixture.UseCase.ExecuteAsync(new("Agenda", "Não carrega", [new("capture.png", "application/octet-stream", PngSignature)]));
    Assert.True(result.IsSuccess);
    var email = Assert.Single(fixture.EmailSender.Messages);
    Assert.Equal(fixture.SmtpSenderEmail, email.RecipientEmail);
    Assert.Equal(fixture.UserEmail, email.ReplyToEmail);
    Assert.Contains(fixture.CompanyTradeName, email.HtmlBody);
    Assert.Equal("image/png", Assert.Single(email.Attachments).ContentType);
}
```

- [ ] **Step 2: Verify the new test is red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SendSupportRequestUseCaseTests.ExecuteAsync_UsesStoredIdentityAndReplyTo`

Expected: FAIL because the Support feature types are absent.

- [ ] **Step 3: Implement the minimal success path**

```csharp
public sealed record SendSupportRequestCommand(string Subject, string Message, IReadOnlyList<SupportAttachmentInput> Attachments);
public sealed record SupportAttachmentInput(string FileName, string DeclaredContentType, byte[] Content);
```

Implement detection from PNG (`89 50 4E 47 0D 0A 1A 0A`), JPEG (`FF D8 FF`), and RIFF/WEBP (`RIFF` offset 0 and `WEBP` offset 8); derive email attachment MIME from detection. Query user and company with the current IDs and tenant membership, HTML-encode email-body values, send via `IEmailSender`, and register the use case.

- [ ] **Step 4: Verify the valid request is green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SendSupportRequestUseCaseTests.ExecuteAsync_UsesStoredIdentityAndReplyTo`

Expected: PASS.

- [ ] **Step 5: Write failing tests for every invalid condition**

```csharp
[Fact]
public async Task ExecuteAsync_RejectsContentThatOnlyPretendsToBePng()
{
    var result = await fixture.UseCase.ExecuteAsync(new("Ajuda", "Arquivo inválido", [new("photo.png", "image/png", "not image"u8.ToArray())]));
    Assert.False(result.IsSuccess);
    Assert.Empty(fixture.EmailSender.Messages);
}
```

Cover unauthenticated user, missing company association, blank subject/message, missing stored email, six files, a 5 MiB-plus-one file, a 20 MiB-plus-one aggregate, every truncated signature, and a mismatched declared type with valid binary bytes.

- [ ] **Step 6: Verify these tests are red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SendSupportRequestUseCaseTests`

Expected: FAIL for validation paths not implemented.

- [ ] **Step 7: Add `SupportErrors` and minimal validation**

```csharp
private const int MaximumAttachmentCount = 5;
private const int MaximumAttachmentBytes = 5 * 1024 * 1024;
private const int MaximumTotalAttachmentBytes = 20 * 1024 * 1024;
```

Validate text and authentication first; validate attachment count, individual bytes, cumulative `long` bytes, then signature; return explicit `OperationResult` errors before any send. Filter all data loads by user/company membership.

- [ ] **Step 8: Verify all application tests are green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SendSupportRequestUseCaseTests`

Expected: PASS.

- [ ] **Step 9: Commit Task 1**

Run: `git add src/VoltaMarcada.Application/Support src/VoltaMarcada.Application/DependencyInjection.cs tests/VoltaMarcada.IntegrationTests/Support/SendSupportRequestUseCaseTests.cs; git commit -m "feat: add support request use case"`

### Task 2: Support WhatsApp configuration

**Files:**
- Create: `src/VoltaMarcada.Web/Configuration/SupportOptions.cs`
- Create: `src/VoltaMarcada.Web/Configuration/SupportConfiguration.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Configuration/SupportConfigurationTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs`

**Interfaces:** Produces `ISupportConfiguration` with `string? WhatsAppUrl { get; }`; consumes `IOptions<SupportOptions>`.

- [ ] **Step 1: Write failing normalization tests**

```csharp
[Theory]
[InlineData("(67) 99999-1234", "https://wa.me/5567999991234")]
[InlineData("+55 67 99999-1234", "https://wa.me/5567999991234")]
public void WhatsAppUrl_NormalizesBrazilianNumbers(string input, string expected) => Assert.Equal(expected, CreateConfiguration(input).WhatsAppUrl);
```

Also test empty, too-short, and non-Brazilian international values return null.

- [ ] **Step 2: Verify the tests are red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SupportConfigurationTests`

Expected: FAIL because configuration types are absent.

- [ ] **Step 3: Implement the configuration and startup registration**

```csharp
public sealed class SupportOptions { public const string SectionName = "Support"; public string WhatsAppNumber { get; init; } = string.Empty; }
public interface ISupportConfiguration { string? WhatsAppUrl { get; } }
```

Strip non-digits; prepend `55` to local 10/11 digits; retain only 12/13 digits beginning `55`; otherwise expose null. Bind options and register the singleton in `Program.cs`.

- [ ] **Step 4: Verify configuration tests are green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SupportConfigurationTests`

Expected: PASS.

- [ ] **Step 5: Run the existing production support source contract**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~ProductionSupportConfiguration_ShouldMapWhatsAppNumberWithoutExampleSecrets`

Expected: PASS, confirming compose, example environment, and sanitized appsettings retain the integrated support configuration.

- [ ] **Step 6: Commit Task 2**

Run: `git add src/VoltaMarcada.Web/Configuration/SupportOptions.cs src/VoltaMarcada.Web/Configuration/SupportConfiguration.cs src/VoltaMarcada.Web/Program.cs tests/VoltaMarcada.IntegrationTests/Configuration/SupportConfigurationTests.cs tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs; git commit -m "feat: configure support whatsapp link"`

### Task 3: Multipart endpoint and per-user limit

**Files:**
- Create: `src/VoltaMarcada.Web/Endpoints/SupportEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Endpoints/SupportEndpointsTests.cs`

**Interfaces:** Produces `POST /support/requests` with multipart fields `subject`, `message`, and repeated `attachments`; consumes `SendSupportRequestUseCase` and `CompanyMember` authorization.

- [ ] **Step 1: Write failing authorization and success tests**

```csharp
[Fact]
public async Task PostAsync_RejectsUnauthenticatedRequests() => Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.AnonymousClient.PostAsync("/support/requests", fixture.ValidMultipartContent())).StatusCode);
[Fact]
public async Task PostAsync_AcceptsCompanyMember() => Assert.Equal(HttpStatusCode.OK, (await fixture.MemberClient.PostAsync("/support/requests", fixture.ValidMultipartContent())).StatusCode);
```

Seed a member and an unrelated company, use a capturing `IEmailSender`, and assert the captured reply-to is the stored member email rather than any posted identity field.

- [ ] **Step 2: Verify endpoint tests are red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SupportEndpointsTests`

Expected: FAIL with `404 Not Found` for the valid endpoint request.

- [ ] **Step 3: Implement endpoint mapping and bounded form translation**

```csharp
endpoints.MapPost("/support/requests", SendAsync).RequireAuthorization(AuthorizationPolicies.CompanyMember).RequireRateLimiting("support-requests");
```

Call `ReadFormAsync` once, map only subject/message/files, limit every stream at 5 MiB plus one byte, invoke the use case, map its explicit validation result through the existing HTTP result extension, and call `app.MapSupportEndpoints()`.

- [ ] **Step 4: Register the fixed limiter**

```csharp
options.AddPolicy("support-requests", context => RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.NewGuid().ToString("N"), _ => new FixedWindowRateLimiterOptions { AutoReplenishment = true, PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
```

- [ ] **Step 5: Verify authorization and success tests are green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SupportEndpointsTests`

Expected: PASS.

- [ ] **Step 6: Write failing rate-limit and invalid-image tests**

```csharp
[Fact]
public async Task PostAsync_ReturnsTooManyRequestsOnSixthPost()
{
    for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.OK, (await fixture.MemberClient.PostAsync("/support/requests", fixture.ValidMultipartContent())).StatusCode);
    Assert.Equal(HttpStatusCode.TooManyRequests, (await fixture.MemberClient.PostAsync("/support/requests", fixture.ValidMultipartContent())).StatusCode);
}
```

Also assert another member has an independent allowance and invalid image bytes return validation without sending email.

- [ ] **Step 7: Verify new endpoint tests are red, then make them green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SupportEndpointsTests`

Expected: PASS after completing test-host limiter configuration and operation-result mapping.

- [ ] **Step 8: Commit Task 3**

Run: `git add src/VoltaMarcada.Web/Endpoints/SupportEndpoints.cs src/VoltaMarcada.Web/Program.cs tests/VoltaMarcada.IntegrationTests/Endpoints/SupportEndpointsTests.cs; git commit -m "feat: add support request endpoint"`

### Task 4: Help UI, navigation, and docs

**Files:**
- Create: `src/VoltaMarcada.Web/Components/Pages/Help.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/MainLayout.razor`
- Create: `docs/help-and-support-flows.md`
- Modify: `README.md`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/HelpPageTests.cs`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/MainLayoutTests.cs`

**Interfaces:** Consumes `ISupportConfiguration.WhatsAppUrl` and form names `subject`, `message`, `attachments`; produces protected `/ajuda` and drawer `Ajuda e suporte`.

- [ ] **Step 1: Write failing page and layout source contract tests**

```csharp
Assert.Contains("@page \"/ajuda\"", source);
Assert.Contains("Cadastros", source);
Assert.Contains("Clientes e equipamentos", source);
Assert.Contains("Ordens de serviço", source);
Assert.Contains("Agenda", source);
Assert.Contains("Manutenção recorrente", source);
Assert.Contains("Orçamentos", source);
Assert.Contains("Solicitações públicas", source);
Assert.Contains("MudForm", source);
```

Add layout assertions for `Href="/ajuda"` and `Ajuda e suporte`.

- [ ] **Step 2: Verify the component tests are red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~HelpPageTests|FullyQualifiedName~MainLayoutTests"`

Expected: FAIL because page and navigation do not exist.

- [ ] **Step 3: Implement `Help.razor` and navigation**

```razor
@page "/ajuda"
@attribute [Authorize(Policy = AuthorizationPolicies.CompanyMember)]
@inject ISupportConfiguration SupportConfiguration
<MudForm @ref="supportForm"><MudTextField @bind-Value="subject" Label="Assunto" Required="true" /><MudTextField @bind-Value="message" Label="Mensagem" Lines="6" Required="true" /></MudForm>
```

Provide seven concise Portuguese guidance sections, conditional WhatsApp CTA, selected image display, client-side count/size guidance, pending and generic outcome states. Post `MultipartFormDataContent` to `/support/requests`; add a drawer `MudNavLink` with the required label.

- [ ] **Step 4: Verify component tests are green**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~HelpPageTests|FullyQualifiedName~MainLayoutTests"`

Expected: PASS.

- [ ] **Step 5: Write docs and README link**

Document the seven help topics, WhatsApp environment variable, image limits/types, server-resolved privacy context, reply-to behavior, and per-user rate limit. Add `- [Help and support flows](docs/help-and-support-flows.md)` in the README documentation section.

- [ ] **Step 6: Commit Task 4**

Run: `git add src/VoltaMarcada.Web/Components/Pages/Help.razor src/VoltaMarcada.Web/Components/Layout/MainLayout.razor docs/help-and-support-flows.md README.md tests/VoltaMarcada.IntegrationTests/Components/HelpPageTests.cs tests/VoltaMarcada.IntegrationTests/Components/MainLayoutTests.cs; git commit -m "feat: add help and support page"`

### Task 5: Email contract regression and complete verification

**Files:**
- Modify: `tests/VoltaMarcada.IntegrationTests/Communication/EmailMessageTests.cs`
- Modify: `tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs`

**Interfaces:** Consumes `EmailMessage` reply-to/attachments and current production support configuration; produces regression evidence.

- [ ] **Step 1: Write or retain the failing email-header contract test**

```csharp
var message = new EmailMessage("support@example.com", "Support", "<p>Body</p>", "member@example.com\r\nBcc:x@example.com", [new("capture\r\n.png", "image/png\r\n", [1])]);
Assert.Equal("member@example.comBcc:x@example.com", message.ReplyToEmail);
Assert.Equal("capture.png", message.Attachments[0].FileName);
```

- [ ] **Step 2: Run email and deployment regression tests**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~EmailMessageTests|FullyQualifiedName~ProductionDeploymentSourceTests"`

Expected: PASS with reply-to/attachment and `SUPPORT_WHATSAPP_NUMBER` contracts intact.

- [ ] **Step 3: Run full automated verification**

Run: `dotnet test VoltaMarcada.sln`

Expected: PASS.

- [ ] **Step 4: Run release build and diff hygiene checks**

Run: `dotnet build VoltaMarcada.sln -c Release --no-restore; git diff --check; git status --short`

Expected: build succeeds, diff has no whitespace errors, and only planned files are changed.

- [ ] **Step 5: Commit Task 5**

Run: `git add tests/VoltaMarcada.IntegrationTests/Communication/EmailMessageTests.cs tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs; git commit -m "test: cover support email contracts"`

## Plan self-review

- Coverage: Tasks 1–5 cover all application, endpoint, rate-limit, configuration, UI, documentation, and regression requirements in the specification.
- No deferred work markers or generic validation directions remain; each behavior has a test-first action and verification command.
- Contracts are consistent: the use case takes `SendSupportRequestCommand`; the endpoint provides its three defined form fields; UI consumes the defined configuration interface.
